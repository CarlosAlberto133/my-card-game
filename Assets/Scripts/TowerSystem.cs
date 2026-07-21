using System.Collections.Generic;
using UnityEngine;

// SISTEMA DE TORRES (v4.2): cada jogador escolhe uma torre de classe no início
// da partida (TowerSelectUI) e pode equipar até 3 CARTAS MÁGICAS compradas na
// loja da torre, que abre nos rounds 3, 6, 9... (TowerMagicShopUI).
//
// Sincronização (lockstep do Photon):
// - Escolha da torre: room properties ("tower1"/"tower2") ANTES do jogo andar —
//   os dois clientes leem os mesmos valores. Nenhum RPC novo nessa etapa.
// - Compra de carta mágica: RPC_BuyTowerCard (roda nos DOIS clientes, com a
//   mesma validação sobre o mesmo estado → decisão idêntica).
// - Efeitos periódicos: processados dentro do RPC_EndTurn (troca de round),
//   idênticos nos dois clientes. Sorteios usam System.Random semeado por
//   (seed da partida, round, jogador, carta) — nada de estado compartilhado.
public static class TowerSystem
{
    // Slots de cartas mágicas por torre (v4.2: subiu de 2 para 3)
    public const int MaxEquipped = 3;

    // -1 = torre ainda não escolhida. Índices 1 e 2 (0 não usado).
    static int[] towerOf = { -1, -1, -1 };
    static List<int>[] equipped = { null, new List<int>(), new List<int>() };
    static List<int>[] equippedRound = { null, new List<int>(), new List<int>() };
    static HashSet<int>[] gone = { null, new HashSet<int>(), new HashSet<int>() };
    static int[] towerMaxBonus = { 0, 0, 0 };
    static bool[] resurgenceUsed = { false, false, false };
    static int[] lastBuyRound = { -99, -99, -99 };
    static GameObject[] towerVisual = { null, null, null };

    // Oferta da janela atual (cacheada na virada do round — igual nos 2 clientes)
    static int offerRound = -1;
    static int[][] offers = { null, null, null };

    public static int TowerClassOf(int player) =>
        (player == 1 || player == 2) ? towerOf[player] : -1;
    public static bool BothChosen => towerOf[1] >= 0 && towerOf[2] >= 0;
    public static List<int> EquippedOf(int player) => equipped[player];
    public static bool HasEquipped(int player, int cardId) =>
        (player == 1 || player == 2) && equipped[player].Contains(cardId);
    public static bool HasBoughtThisWindow(int player, int round) => lastBuyRound[player] == round;

    // ── Ciclo de vida ────────────────────────────────────────────────────
    public static void ResetForMatch()
    {
        for (int p = 1; p <= 2; p++)
        {
            towerOf[p] = -1;
            equipped[p].Clear();
            equippedRound[p].Clear();
            gone[p].Clear();
            towerMaxBonus[p] = 0;
            resurgenceUsed[p] = false;
            lastBuyRound[p] = -99;
            if (towerVisual[p] != null) { Object.Destroy(towerVisual[p]); towerVisual[p] = null; }
        }
        offerRound = -1;
        offers[1] = offers[2] = null;
    }

    public static void SetTower(int player, int classIdx)
    {
        if (player < 1 || player > 2 || classIdx < 0 || classIdx > 3) return;
        if (towerOf[player] == classIdx) return;
        towerOf[player] = classIdx;
        SpawnTowerVisual(player, (CardClass)classIdx);
        Debug.Log($"[TowerSystem] P{player} escolheu a torre {TowerCards.TowerName(classIdx)} ({TowerCards.ClassLabel(classIdx)})");
    }

    public static int MaxTowerHealth(int player) => PlayerData.MaxTowerHealth + towerMaxBonus[player];

    // ── Loja da torre (rounds 3, 6, 9...) ────────────────────────────────
    public static bool IsOfferRound(int round) => round >= 3 && round % 3 == 0;

    // Oferta do jogador na janela atual: [carta de classe, universal, universal]
    // (a de classe vem primeiro; pode ter menos de 3 se o pool secou)
    public static int[] GetOffer(int player, int round)
    {
        if (round != offerRound || offers[player] == null) return new int[0];
        return offers[player];
    }

    static void RollOffers(int round)
    {
        offerRound = round;
        int seed = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.currentGameSeed : 12345;
        for (int p = 1; p <= 2; p++)
        {
            var result = new List<int>();
            if (towerOf[p] >= 0)
            {
                var rng = new System.Random(seed * 486187739 + round * 1000003 + p * 7919);

                // 1 carta da CLASSE da torre (fora as já equipadas/descartadas)
                var classPool = TowerCards.OfClass(towerOf[p])
                    .FindAll(c => !equipped[p].Contains(c.id) && !gone[p].Contains(c.id));
                if (classPool.Count > 0)
                    result.Add(classPool[rng.Next(classPool.Count)].id);

                // 2 universais distintas (idem)
                var uniPool = TowerCards.Universals()
                    .FindAll(c => !equipped[p].Contains(c.id) && !gone[p].Contains(c.id));
                for (int k = 0; k < 2 && uniPool.Count > 0; k++)
                {
                    int idx = rng.Next(uniPool.Count);
                    result.Add(uniPool[idx].id);
                    uniPool.RemoveAt(idx);
                }
            }
            offers[p] = result.ToArray();
        }
    }

    // Compra/equipa (roda nos DOIS clientes via RPC_BuyTowerCard — validação
    // idêntica sobre estado idêntico). replaceSlot: -1 = slot livre; 0/1 = troca.
    public static void BuyCard(int player, int cardId, int replaceSlot)
    {
        if (player < 1 || player > 2) return;
        TurnManager tm = TurnManager.Instance;
        if (tm == null) return;
        int round = tm.currentRound;

        var card = TowerCards.Get(cardId);
        PlayerData p = tm.GetPlayer(player);
        if (card == null || p == null) { Debug.LogWarning($"[TowerSystem] Compra inválida (carta {cardId})"); return; }

        // Validações — mesmas nos dois clientes
        if (!IsOfferRound(round) || System.Array.IndexOf(GetOffer(player, round), cardId) < 0)
        { Debug.LogWarning($"[TowerSystem] P{player}: carta {cardId} não está na oferta do round {round}"); return; }
        if (lastBuyRound[player] == round)
        { Debug.LogWarning($"[TowerSystem] P{player}: já comprou nesta janela"); return; }
        if (p.gold < TowerCard.GoldCost)
        { Debug.LogWarning($"[TowerSystem] P{player}: ouro insuficiente"); return; }
        if (equipped[player].Contains(cardId)) return;

        if (equipped[player].Count < MaxEquipped)
        {
            equipped[player].Add(cardId);
            equippedRound[player].Add(round);
        }
        else if (replaceSlot >= 0 && replaceSlot < equipped[player].Count)
        {
            gone[player].Add(equipped[player][replaceSlot]); // descartada não volta
            equipped[player][replaceSlot] = cardId;
            equippedRound[player][replaceSlot] = round;
        }
        else
        {
            Debug.LogWarning($"[TowerSystem] P{player}: slots cheios e sem slot de troca");
            return;
        }

        p.gold -= TowerCard.GoldCost;
        lastBuyRound[player] = round;
        Debug.Log($"[TowerSystem] P{player} equipou '{card.cardName}' na torre (round {round})");

        ApplyOnEquip(player, cardId);
        TowerMagicShopUI.RefreshAfterPurchase(player);
    }

    // ── Efeitos imediatos (ao equipar) ───────────────────────────────────
    static void ApplyOnEquip(int player, int cardId)
    {
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(player) : null;
        if (p == null) return;

        switch (cardId)
        {
            case TowerCards.Muralha:
                towerMaxBonus[player] += 10;
                p.health += 10;
                break;
            case TowerCards.Muros:
                towerMaxBonus[player] += 5;
                p.health += 5;
                break;
            case TowerCards.Bencao:
                p.health = Mathf.Min(p.health + 6, MaxTowerHealth(player));
                break;
            case TowerCards.Recrutamento:
                p.AddGold(6); // v4.2: era 3 — custava 3, "lucro zero" não fazia sentido
                break;
            case TowerCards.Sobrecarga:
                BuffClassOnBoard(player, CardClass.Mago, 1);
                break;
            case TowerCards.PontaAfiada:
                BuffClassOnBoard(player, CardClass.Arqueiro, 1);
                break;
            case TowerCards.Ressurgimento:
                CheckResurgence(player);
                break;
        }
    }

    static void BuffClassOnBoard(int player, CardClass cls, int atk)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        foreach (var c in board.GetCardsByOwner(player))
        {
            if (c == null || c.card == null || c.card.cardClass != cls) continue;
            c.currentAttack += atk;
            c.UpdateDisplay();
        }
    }

    // ── Virada de round (dentro do RPC_EndTurn — roda nos 2 clientes) ────
    public static void OnRoundChanged(int round)
    {
        // Efeitos periódicos, na ordem fixa P1 → P2, slot 0 → 1 (determinístico)
        for (int p = 1; p <= 2; p++)
        {
            for (int s = 0; s < equipped[p].Count; s++)
            {
                int id = equipped[p][s];
                int since = round - equippedRound[p][s];

                // Diagnóstico dos periódicos (P{jogador}, carta, rounds desde o
                // equipar) — deixa visível no log POR QUE disparou ou não
                var cardDef = TowerCards.Get(id);
                Debug.Log($"[TowerSystem] Round {round}: P{p} '{cardDef?.cardName}' " +
                          $"equipada há {since} round(s)");

                if (since <= 0) continue;

                switch (id)
                {
                    case TowerCards.Guarnicao: GarrisonShield(p); break;
                    case TowerCards.Cofres: GiveGold(p, 1); break;
                    case TowerCards.Sentinelas: TowerShot(p, round, id, 1); break;
                    case TowerCards.FonteDaVida:
                        if (since % 2 == 0) HealMostWounded(p, 2);
                        break;
                    case TowerCards.Tempestade:
                        if (since % 2 == 0) StartTowerTargetSelection(p, id,
                            "Tempestade Arcana: escolha o alvo do raio (2 de dano + 1 nos adjacentes)");
                        break;
                    case TowerCards.Nevasca:
                        if (since % 2 == 0) StartTowerTargetSelection(p, id,
                            "Nevasca: escolha um inimigo para CONGELAR");
                        break;
                    case TowerCards.Canhoneira:
                        if (since % 2 == 0) CannonMostAdvanced(p, 2);
                        break;
                }
            }
        }

        // Janela da loja mágica: abre nos rounds 3/6/9..., fecha nos demais
        if (IsOfferRound(round))
        {
            RollOffers(round);
            TowerMagicShopUI.OnOfferWindowOpened(round);
        }
        else
        {
            offers[1] = offers[2] = null;
            offerRound = -1;
            TowerMagicShopUI.OnOfferWindowClosed();
        }
    }

    // ── Ganchos reativos ─────────────────────────────────────────────────

    // Torre do targetPlayer levou dano de um ataque (ExecuteTowerAttack)
    public static void OnTowerDamaged(int targetPlayer, CardDisplay attacker)
    {
        if (targetPlayer < 1 || targetPlayer > 2) return;

        // Represália: o atacante leva 2 de volta
        if (HasEquipped(targetPlayer, TowerCards.Represalia) && attacker != null &&
            attacker.card != null && attacker.currentHealth > 0)
        {
            Debug.Log($"[TowerSystem] Represália: {attacker.card.cardName} leva 2 de dano de volta!");
            FloatingTextFX.ShowAboveCard(attacker, "REPRESÁLIA!", FloatingTextFX.EffectColor, 4.2f);
            attacker.TakeDamage(2);
        }

        CheckResurgence(targetPlayer);
    }

    static void CheckResurgence(int player)
    {
        if (!HasEquipped(player, TowerCards.Ressurgimento) || resurgenceUsed[player]) return;
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(player) : null;
        if (p == null || p.health >= 15) return;

        resurgenceUsed[player] = true;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        int buffed = 0;
        foreach (var c in board.GetCardsByOwner(player))
        {
            if (c == null || c.card == null) continue;
            c.currentAttack += 2; // v4.2: era +1 — fraco demais para um efeito 1x
            c.UpdateDisplay();
            buffed++;
        }
        Debug.Log($"[TowerSystem] Ressurgimento: torre do P{player} abaixo de 15 — {buffed} aliado(s) com +2 ATK!");
    }

    // Carta entrou em campo (ExecutePlaceCard/PlaceCard — 1x por colocação)
    public static void OnCardPlaced(CardDisplay cd)
    {
        if (cd == null || cd.card == null || cd.ownerPlayerNumber == 0) return;
        int owner = cd.ownerPlayerNumber;
        int enemy = owner == 1 ? 2 : 1;

        // Buffs da própria torre
        if (HasEquipped(owner, TowerCards.Estandarte))
        {
            cd.currentHealth += 1;
            cd.UpdateDisplay();
        }
        if (HasEquipped(owner, TowerCards.Sobrecarga) && cd.card.cardClass == CardClass.Mago)
        {
            cd.currentAttack += 1;
            cd.UpdateDisplay();
        }
        if (HasEquipped(owner, TowerCards.PontaAfiada) && cd.card.cardClass == CardClass.Arqueiro)
        {
            cd.currentAttack += 1;
            cd.UpdateDisplay();
        }

        // Emboscada da torre INIMIGA: 1 de dano em quem entra
        if (HasEquipped(enemy, TowerCards.Emboscada))
        {
            Debug.Log($"[TowerSystem] Emboscada: {cd.card.cardName} toma 1 de dano ao entrar!");
            cd.TakeDamage(1);
        }
    }

    // Um aliado do player foi curado de verdade (CardDisplay.Heal)
    public static void OnAllyHealed(int player)
    {
        if (!HasEquipped(player, TowerCards.VinculoSagrado)) return;
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(player) : null;
        if (p == null || p.health >= MaxTowerHealth(player)) return;
        p.health += 1;
    }

    // Mercado Negro: -1 na primeira compra do turno (soma no DiscountedCost)
    public static int FirstBuyDiscount(PlayerData buyer)
    {
        if (buyer == null || buyer.cardsBoughtThisTurn > 0) return 0;
        return HasEquipped(buyer.playerNumber, TowerCards.MercadoNegro) ? 1 : 0;
    }

    // ── Efeitos periódicos (implementação) ───────────────────────────────

    static void GarrisonShield(int player)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        int rows = board.rows;
        int granted = 0;
        foreach (var c in board.GetCardsByOwner(player))
        {
            if (c == null || c.card == null || c.currentTile == null) continue;
            bool homeRows = player == 1 ? c.currentTile.row <= 1 : c.currentTile.row >= rows - 2;
            if (!homeRows) continue;

            // Teto anti-tartaruga: a torre reforça cada unidade até +4 no total
            // (sem isso, acampar nas fileiras de casa dava armadura infinita)
            if (c.garrisonShieldGrants >= 4) continue;
            c.garrisonShieldGrants++;

            c.currentShield += 1;
            c.UpdateDisplay();
            granted++;
        }
        if (granted > 0)
            Debug.Log($"[TowerSystem] Guarnição P{player}: +1 armadura em {granted} aliado(s) nas fileiras de casa");
    }

    static void GiveGold(int player, int amount)
    {
        PlayerData p = TurnManager.Instance != null ? TurnManager.Instance.GetPlayer(player) : null;
        if (p != null)
        {
            p.AddGold(amount);
            Debug.Log($"[TowerSystem] Cofres Reais P{player}: +{amount} ouro");
        }
    }

    static void HealMostWounded(int player, int amount)
    {
        CardDisplay best = MostWoundedAllyOf(player);
        if (best != null)
        {
            best.Heal(amount);
            Debug.Log($"[TowerSystem] Fonte da Vida P{player}: curou {best.card.cardName} em {amount}");
        }
    }

    static CardDisplay MostWoundedAllyOf(int player)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return null;
        CardDisplay best = null;
        int bestMissing = 0;
        foreach (var ally in board.GetCardsByOwner(player))
        {
            if (ally == null || ally.card == null || ally.currentHealth <= 0) continue;
            int missing = (ally.card.health + ally.maxHealthBonus) - ally.currentHealth;
            if (missing > bestMissing) { bestMissing = missing; best = ally; }
        }
        return best;
    }

    static System.Random EffectRng(int player, int round, int cardId)
    {
        int seed = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.currentGameSeed : 12345;
        return new System.Random(seed * 92821 + round * 68917 + player * 4451 + cardId * 197);
    }

    static void TowerShot(int player, int round, int cardId, int damage)
    {
        int enemy = player == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        var enemies = board.GetCardsByOwner(enemy);
        if (enemies.Count == 0) return;
        var rng = EffectRng(player, round, cardId);
        CardDisplay target = enemies[rng.Next(enemies.Count)];
        if (target == null || target.card == null) return;
        Debug.Log($"[TowerSystem] Sentinelas P{player}: {damage} de dano em {target.card.cardName}");
        FloatingTextFX.ShowAboveCard(target, "SENTINELAS!", FloatingTextFX.EffectColor, 3.6f);
        target.TakeDamage(damage);
    }

    // ── Tempestade/Nevasca com alvo ESCOLHIDO (v4.2 — eram sorteio) ──────
    // O dono da torre clica no alvo (mesma seleção dos efeitos de mago);
    // a escolha viaja por RPC_TowerEffectTarget e aplica nos 2 clientes.

    static void StartTowerTargetSelection(int player, int cardId, string prompt)
    {
        int enemy = player == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var enemies = board.GetCardsByOwner(enemy)
            .FindAll(c => c != null && c.card != null && c.currentTile != null);
        if (enemies.Count == 0)
        {
            Debug.Log($"[TowerSystem] {TowerCards.Get(cardId)?.cardName} P{player}: sem inimigos em campo neste round");
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.StartTowerEffectTargetSelection(player, cardId, enemies, prompt);
    }

    // Aplica o efeito da torre no alvo escolhido (chega por RPC nos 2 clientes)
    public static void ApplyTowerEffectOnTarget(int player, int cardId, CardDisplay target)
    {
        if (target == null || target.card == null) return;

        switch (cardId)
        {
            case TowerCards.Tempestade: TowerBlastAt(player, target); break;

            case TowerCards.Nevasca:
                Debug.Log($"[TowerSystem] Nevasca P{player}: congelou {target.card.cardName}");
                FloatingTextFX.ShowAboveCard(target, "NEVASCA!", FloatingTextFX.EffectColor, 3.6f);
                // forceSingleTurn: a escolha chega por RPC em momento
                // imprevisível vs. a troca de turno — forçar 1 turno dá a
                // mesma duração nos 2 clientes
                target.Freeze(true);
                break;
        }
    }

    static void TowerBlastAt(int player, CardDisplay center)
    {
        if (center == null || center.currentTile == null) return;
        int enemy = player == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Tile capturado ANTES do dano; respingo coletado antes de aplicar
        CardTile centerTile = center.currentTile;
        Debug.Log($"[TowerSystem] Tempestade Arcana P{player}: 2 de dano em {center.card.cardName} + 1 nos adjacentes");
        FloatingTextFX.ShowAboveCard(center, "TEMPESTADE!", FloatingTextFX.EffectColor, 3.6f);
        center.TakeDamage(2);

        var splash = new List<CardDisplay>();
        foreach (var c in board.GetCardsByOwner(enemy))
        {
            if (c == null || c.card == null || c.currentTile == null) continue;
            int dr = Mathf.Abs(c.currentTile.row - centerTile.row);
            int dc = Mathf.Abs(c.currentTile.column - centerTile.column);
            if (dr == 0 && dc == 0) continue;
            if (dr <= 1 && dc <= 1) splash.Add(c);
        }
        foreach (var t in splash) t.TakeDamage(1);
    }

    // Canhoneira: acerta o inimigo MAIS AVANÇADO em direção à torre do dono
    // (desempate por coluna — determinístico, sem sorteio)
    static void CannonMostAdvanced(int player, int damage)
    {
        int enemy = player == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        CardDisplay best = null;
        foreach (var c in board.GetCardsByOwner(enemy))
        {
            if (c == null || c.card == null || c.currentTile == null) continue;
            if (best == null) { best = c; continue; }
            bool moreAdvanced = player == 1
                ? c.currentTile.row < best.currentTile.row ||
                  (c.currentTile.row == best.currentTile.row && c.currentTile.column < best.currentTile.column)
                : c.currentTile.row > best.currentTile.row ||
                  (c.currentTile.row == best.currentTile.row && c.currentTile.column < best.currentTile.column);
            if (moreAdvanced) best = c;
        }
        if (best == null) return;
        Debug.Log($"[TowerSystem] Canhoneira P{player}: 2 de dano em {best.card.cardName} (mais avançado)");
        FloatingTextFX.ShowAboveCard(best, "CANHONEIRA!", FloatingTextFX.EffectColor, 3.6f);
        best.TakeDamage(damage);
    }

    // ── Visual da torre (figura grande atrás da fileira de casa) ─────────

    static void SpawnTowerVisual(int player, CardClass cls)
    {
        if (towerVisual[player] != null) { Object.Destroy(towerVisual[player]); towerVisual[player] = null; }

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        string path = cls == CardClass.Tank ? "Models/personagem_tank"
                    : cls == CardClass.Mago ? "Models/personagem_mago"
                    : cls == CardClass.Healer ? "Models/personagem_healer"
                    : "Models/personagem_arqueiro";
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null) { Debug.LogWarning($"[TowerSystem] Modelo {path} não encontrado"); return; }

        // Fileira de casa: P1 = row 0, P2 = row rows-1. Ajustes calibrados com
        // o Carlos em jogo: coluna 3 (índice MENOR = mais à esquerda na tela)
        // e 2,5 tiles para FORA da borda, atrás das cartas da mão.
        const int TowerColumn = 3;
        const float TowerOutwardTiles = 3.0f;

        int rows = board.rows;
        int homeRow = player == 1 ? 0 : rows - 1;
        int innerRow = player == 1 ? 1 : rows - 2;
        CardTile edge = board.GetTile(homeRow, TowerColumn);
        CardTile inner = board.GetTile(innerRow, TowerColumn);
        if (edge == null || inner == null) return;

        Vector3 edgeCenter = edge.transform.position;
        float tileDist = Vector3.Distance(edgeCenter, inner.transform.position);
        Vector3 outward = (edgeCenter - inner.transform.position).normalized;
        Vector3 pos = edgeCenter + outward * (tileDist * TowerOutwardTiles);

        GameObject go = Object.Instantiate(prefab);
        go.name = $"TowerVisual_P{player}_{cls}";

        // 1º ROTACIONA (de frente pro tabuleiro), 2º escala, 3º posiciona pelos
        // bounds — rotacionar depois girava em torno do pivô do modelo e a
        // torre saía do centro
        Vector3 look = -outward; look.y = 0f;
        if (look.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(look) * go.transform.rotation;

        // Bem maior que as figuras de carta (presença de TORRE)
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        bool hasB = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!hasB) { bounds = r.bounds; hasB = true; }
            else bounds.Encapsulate(r.bounds);
        }
        float targetHeight = 10f;
        if (hasB && bounds.size.y > 0.001f)
            go.transform.localScale = go.transform.localScale * (targetHeight / bounds.size.y);

        // Posiciona pelos BOUNDS: centro em cima do ponto-alvo, pés no chão
        Bounds nb = new Bounds(go.transform.position, Vector3.zero);
        hasB = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            if (!hasB) { nb = r.bounds; hasB = true; }
            else nb.Encapsulate(r.bounds);
        }
        float floorY = edgeCenter.y;
        Vector3 offset = pos - nb.center;
        offset.y = floorY - nb.min.y;
        go.transform.position += offset;

        towerVisual[player] = go;
    }
}
