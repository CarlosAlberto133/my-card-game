using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ============================ MODO TREINO (vs BOT) ============================
// Partida offline contra um bot que controla o Jogador 2.
//
// Como funciona: o botão "Treinar vs Bot" do lobby liga PhotonNetwork.offlineMode
// e cria uma sala local. Nesse modo os MESMOS RPCs do multiplayer executam
// localmente na hora — o bot joga chamando exatamente os métodos Send*RPC que o
// jogador humano usa (comprar, posicionar, mover, atacar, passar a vez). Nada
// do código de rede/lockstep muda; só existe UM cliente, então não há desync.
//
// NUNCA ativo em partidas online: BotMode.Enabled é desligado sempre que a cena
// do lobby carrega, e só religa pelo botão de treino.
public static class BotMode
{
    public static bool Enabled = false;

    // 0 = Fácil (guloso e distraído), 1 = Médio (tático), 2 = Difícil (planeja
    // tríades, defende a torre, corrida de dano letal, personalidade)
    public static int Difficulty = 1;

    public const int BotPlayerNumber = 2;

    public static bool IsBot(int playerNumber)
    {
        return Enabled && playerNumber == BotPlayerNumber;
    }
}

public class BotController : MonoBehaviour
{
    public static BotController Instance { get; private set; }

    // Pausas "humanas" entre as ações do bot (o jogador vê o que aconteceu)
    const float ThinkDelay = 1.2f;
    const float ActionDelay = 0.8f;

    bool openingRunning = false; // Fase inicial de compras em andamento
    bool turnRunning = false;    // Turno do bot em andamento

    // Aleatoriedade PRÓPRIA do bot (System.Random: nunca toca o stream global
    // do UnityEngine.Random — regra da casa). Ações do bot são "input de
    // jogador", não precisam ser determinísticas: só existe um cliente.
    System.Random rng;

    // Personalidade (só no Difícil): quanto maior, mais o bot prefere socar a
    // torre a trocar com unidades que não morrem. Sorteada por partida — o
    // mesmo bot ora joga agressivo, ora controlador.
    float aggression = 0.5f;

    void Awake()
    {
        Instance = this;
        rng = new System.Random(System.Environment.TickCount);
        aggression = BotMode.Difficulty >= 2
            ? 0.35f + (float)rng.NextDouble() * 0.5f
            : 0.4f;
        Debug.Log($"[Bot] Dificuldade {BotMode.Difficulty}, agressividade {aggression:F2}");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!BotMode.Enabled) return;

        TurnManager tm = TurnManager.Instance;
        if (tm == null) return;

        // Fase inicial de compras (simultânea): compra as cartas e clica "Pronto".
        // Depois de um "Jogar Novamente" o player2Ready volta a false e o bot
        // compra de novo sozinho.
        if (tm.gameState == GameState.Lobby && !tm.player2Ready && !openingRunning)
        {
            openingRunning = true;
            StartCoroutine(OpeningBuysRoutine());
        }

        // Turno do bot durante a partida
        if (tm.gameState == GameState.Playing &&
            tm.currentPlayerNumber == BotMode.BotPlayerNumber &&
            !turnRunning && !GameOver())
        {
            turnRunning = true;
            StartCoroutine(BotTurnRoutine());
        }
    }

    bool GameOver()
    {
        TurnManager tm = TurnManager.Instance;
        return tm == null || tm.player1 == null || tm.player2 == null ||
               tm.player1.IsDefeated() || tm.player2.IsDefeated();
    }

    bool StillMyTurn()
    {
        TurnManager tm = TurnManager.Instance;
        return BotMode.Enabled && tm != null && tm.gameState == GameState.Playing &&
               tm.currentPlayerNumber == BotMode.BotPlayerNumber && !GameOver();
    }

    HandManager GetBotHand()
    {
        HandManager[] hands = FindObjectsOfType<HandManager>();
        foreach (HandManager hm in hands)
            if (hm.playerNumber == BotMode.BotPlayerNumber) return hm;
        return null;
    }

    bool HandFull()
    {
        HandManager hand = GetBotHand();
        return hand != null && hand.IsHandFull();
    }

    List<CardDisplay> MyBoardCards()
    {
        return BoardManager.Instance != null
            ? BoardManager.Instance.GetCardsByOwner(BotMode.BotPlayerNumber)
            : new List<CardDisplay>();
    }

    List<CardDisplay> EnemyBoardCards()
    {
        return BoardManager.Instance != null
            ? BoardManager.Instance.GetCardsByOwner(1)
            : new List<CardDisplay>();
    }

    // Espera popups de decisão (do humano ou do próprio bot) resolverem antes
    // da próxima ação — mesma regra que trava os cliques do jogador
    IEnumerator WaitDecisionsClear()
    {
        float t = 0f;
        while (t < 15f && GameManager.IsDecisionPending())
        {
            t += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ================== AVALIAÇÃO DE AMEAÇA ==================

    // Quão perigosa é uma carta inimiga: ataque pesa mais; Healer/Mago/Arqueiro
    // valem prioridade extra (sustain e efeitos machucam a longo prazo) e quem
    // já está perto da torre do bot é urgência
    int ThreatOf(CardDisplay e)
    {
        if (e == null) return 0;
        int t = e.currentAttack * 4;
        if (e.card != null)
        {
            if (e.card.cardClass == CardClass.Healer) t += 6;
            else if (e.card.cardClass == CardClass.Mago) t += 4;
            else if (e.card.cardClass == CardClass.Arqueiro) t += 3;
        }
        if (e.currentTile != null && BoardManager.Instance != null &&
            e.currentTile.row >= BoardManager.Instance.rows - 3)
            t += 8; // invadindo o lado do bot
        return t;
    }

    // ================== FASE INICIAL DE COMPRAS ==================

    IEnumerator OpeningBuysRoutine()
    {
        // Espera a loja do bot existir (a seed precisa chegar e spawnar as cartas)
        float waited = 0f;
        while (CardManager.Instance == null ||
               CardManager.Instance.GetShopCard(0, BotMode.BotPlayerNumber) == null)
        {
            waited += 0.25f;
            if (waited > 30f) { openingRunning = false; yield break; }
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(ThinkDelay);

        TurnManager tm = TurnManager.Instance;
        PlayerData bot = tm != null ? tm.GetPlayer(BotMode.BotPlayerNumber) : null;
        if (bot == null) { openingRunning = false; yield break; }

        // Compra enquanto puder (limite de 5 da fase inicial, ouro e mão)
        int safety = 20;
        while (safety-- > 0 && tm.gameState == GameState.Lobby &&
               bot.CanBuyCardInLobby() && !HandFull())
        {
            int idx = PickShopIndex(bot);
            if (idx < 0) break;

            PhotonGameManager.Instance.SendBuyCardRPC(idx, BotMode.BotPlayerNumber);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }

        // "Pronto": mesmo efeito do RPC_PlayerReady do jogador humano (executa
        // local — em offline só existe este cliente)
        if (tm.gameState == GameState.Lobby && PhotonGameManager.Instance != null)
            PhotonGameManager.Instance.RPC_PlayerReady(BotMode.BotPlayerNumber);

        openingRunning = false;
    }

    // ================== COMPRAS ==================

    // Escolha da loja por dificuldade:
    //   Fácil   — carta comprável ALEATÓRIA
    //   Médio   — maior tier que o ouro alcança (guloso)
    //   Difícil — maior tier + bônus por avançar/fechar uma TRÍADE tier 2
    int PickShopIndex(PlayerData bot)
    {
        if (CardManager.Instance == null) return -1;

        bool freeBuy = bot.freePurchases > 0;
        List<int> affordable = new List<int>();
        int best = -1, bestScore = int.MinValue;

        for (int i = 0; i < CardManager.LobbyShopSize; i++)
        {
            GameObject go = CardManager.Instance.GetShopCard(i, BotMode.BotPlayerNumber);
            if (go == null) continue;

            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd == null || cd.card == null) continue;
            if (cd.isInHand || cd.isOnBoard) continue; // já comprada

            // Custo real (com o desconto da Healer 3 [2/3], se o bot a tiver)
            if (!freeBuy && !bot.HasEnoughGold(CardDisplay.DiscountedCost(cd.card, bot))) continue;
            affordable.Add(i);

            int score = (int)cd.card.tier * 10;
            if (BotMode.Difficulty >= 2)
                score += TriadBonus(cd.card) + rng.Next(3); // desempate variado

            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        if (affordable.Count == 0) return -1;
        if (BotMode.Difficulty == 0) return affordable[rng.Next(affordable.Count)];
        return best;
    }

    // Membros das TRÍADES tier 2 por classe (mesmos statlines base que o
    // CardEffectSimple usa para detectar o combo: 3 membros DISTINTOS em campo).
    // Formato {ataque, escudo, vida} — ver [Check*Combo] no CardEffectSimple.
    static readonly Dictionary<CardClass, int[][]> TriadMembers =
        new Dictionary<CardClass, int[][]>
    {
        { CardClass.Arqueiro, new[] { new[] {2,0,3}, new[] {3,0,2}, new[] {2,0,2} } },
        { CardClass.Healer,   new[] { new[] {1,0,4}, new[] {0,0,4}, new[] {0,0,3} } },
        { CardClass.Mago,     new[] { new[] {2,0,4}, new[] {2,0,3}, new[] {3,0,3} } },
        { CardClass.Tank,     new[] { new[] {1,2,5}, new[] {1,3,4}, new[] {0,3,5} } },
    };

    static int TriadMemberIndex(Card card)
    {
        if (card == null || card.tier != CardTier.Tier2) return -1;
        int[][] members;
        if (!TriadMembers.TryGetValue(card.cardClass, out members)) return -1;
        for (int i = 0; i < members.Length; i++)
            if (card.attack == members[i][0] && card.shield == members[i][1] &&
                card.health == members[i][2]) return i;
        return -1;
    }

    // Bônus de compra: quanto mais membros distintos da tríade o bot já tem
    // (campo + mão), mais vale comprar o que falta. Duplicata não ajuda.
    int TriadBonus(Card card)
    {
        int idx = TriadMemberIndex(card);
        if (idx < 0) return 0;

        bool[] owned = new bool[3];
        foreach (CardDisplay c in MyBoardCards())
        {
            if (c == null || c.card == null || c.card.cardClass != card.cardClass) continue;
            int m = TriadMemberIndex(c.card);
            if (m >= 0) owned[m] = true;
        }
        HandManager hand = GetBotHand();
        if (hand != null)
        {
            for (int i = 0; i < hand.GetCardCount(); i++)
            {
                GameObject go = hand.GetCardAtIndex(i);
                CardDisplay c = go != null ? go.GetComponent<CardDisplay>() : null;
                if (c == null || c.card == null || c.card.cardClass != card.cardClass) continue;
                int m = TriadMemberIndex(c.card);
                if (m >= 0) owned[m] = true;
            }
        }

        if (owned[idx]) return 0; // já tem este membro
        int others = 0;
        for (int i = 0; i < 3; i++)
            if (i != idx && owned[i]) others++;
        return others == 2 ? 45 : others == 1 ? 18 : 6;
    }

    // ================== TURNO DO BOT ==================

    IEnumerator BotTurnRoutine()
    {
        yield return BotTurnBody();

        // Passa a vez (mesmo RPC do humano) — o WaitDecisionsClear do fim do
        // corpo garante que nenhum popup ficou aberto
        if (StillMyTurn() && PhotonGameManager.Instance != null)
            PhotonGameManager.Instance.SendEndTurnRPC();

        turnRunning = false;
    }

    IEnumerator BotTurnBody()
    {
        yield return new WaitForSeconds(ThinkDelay);
        yield return WaitDecisionsClear();
        if (!StillMyTurn()) yield break;

        TurnManager tm = TurnManager.Instance;
        PlayerData bot = tm.GetPlayer(BotMode.BotPlayerNumber);

        // 0) Magia da torre: no round de oferta, compra 1 se der (prioriza a
        //    carta de classe — o GetOffer põe ela primeiro). Sem troca de slot.
        if (TowerSystem.IsOfferRound(tm.currentRound) &&
            !TowerSystem.HasBoughtThisWindow(BotMode.BotPlayerNumber, tm.currentRound) &&
            TowerSystem.EquippedOf(BotMode.BotPlayerNumber).Count < 2 &&
            bot.gold >= TowerCard.GoldCost + 2) // guarda um troco pra loja normal
        {
            int[] towerOffer = TowerSystem.GetOffer(BotMode.BotPlayerNumber, tm.currentRound);
            if (towerOffer.Length > 0)
            {
                PhotonGameManager.Instance.SendBuyTowerCardRPC(BotMode.BotPlayerNumber, towerOffer[0], -1);
                yield return new WaitForSeconds(ActionDelay);
                yield return WaitDecisionsClear();
            }
        }

        // 1) Compras do turno (até o limite por turno)
        int buySafety = 6;
        while (buySafety-- > 0 && StillMyTurn() && !HandFull() &&
               (bot.CanBuyCard() || bot.freePurchases > 0))
        {
            int idx = PickShopIndex(bot);
            if (idx < 0) break;

            PhotonGameManager.Instance.SendBuyCardRPC(idx, BotMode.BotPlayerNumber);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }

        // 2) Posiciona cartas da mão (a mais forte primeiro; a fileira/coluna
        //    depende da classe e da dificuldade)
        HandManager hand = GetBotHand();
        int placeSafety = 10;
        while (placeSafety-- > 0 && StillMyTurn() && hand != null && hand.GetCardCount() > 0)
        {
            int bestIdx = -1, bestAtk = -1;
            CardDisplay bestCard = null;
            for (int i = 0; i < hand.GetCardCount(); i++)
            {
                GameObject go = hand.GetCardAtIndex(i);
                CardDisplay cd = go != null ? go.GetComponent<CardDisplay>() : null;
                if (cd == null || cd.card == null) continue;
                if (cd.card.attack > bestAtk) { bestAtk = cd.card.attack; bestIdx = i; bestCard = cd; }
            }
            if (bestIdx < 0) break;

            CardTile tile = ChoosePlacementTile(bestCard != null ? bestCard.card : null);
            if (tile == null) break;

            PhotonGameManager.Instance.SendPlaceCardRPC(bestIdx, BotMode.BotPlayerNumber, tile.row, tile.column);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear(); // Efeitos de entrada podem abrir popup/escolha
        }

        // 3) Combate + movimento
        if (BotMode.Difficulty == 0)
        {
            yield return EasyCombat();
        }
        else
        {
            // Ataques disponíveis AGORA (foco de fogo: melhor golpe primeiro,
            // reavaliado depois de cada golpe), depois avanço, depois os
            // ataques que o movimento liberou
            yield return SmartCombat();
            yield return MovePhase();
            yield return SmartCombat();
        }

        yield return WaitDecisionsClear();
    }

    // ================== COMBATE (Fácil: guloso e distraído) ==================

    IEnumerator EasyCombat()
    {
        List<CardDisplay> myCards = new List<CardDisplay>(MyBoardCards());

        foreach (CardDisplay cd in myCards)
        {
            if (!StillMyTurn()) yield break;
            if (cd == null || cd.currentTile == null) continue; // morreu no meio do turno

            // "Distraído": às vezes esquece de agir com uma carta
            if (rng.NextDouble() < 0.30) continue;

            yield return TryAttackGreedy(cd);

            if (StillMyTurn() && cd != null && cd.currentTile != null && cd.CanMovePeek())
            {
                CardTile step = ChooseStepTile(cd);
                if (step != null)
                {
                    PhotonGameManager.Instance.SendMoveCardRPC(
                        cd.currentTile.row, cd.currentTile.column, step.row, step.column);
                    yield return new WaitForSeconds(ActionDelay);
                    yield return WaitDecisionsClear();
                }
            }

            yield return TryAttackGreedy(cd);
        }
    }

    // Ataque simples de uma carta (lógica antiga): mata se der, senão bate no
    // mais fraco; sem alvo, soca a torre se estiver no alcance
    IEnumerator TryAttackGreedy(CardDisplay cd)
    {
        if (!StillMyTurn() || cd == null || cd.currentTile == null || !cd.CanAttackPeek())
            yield break;

        List<CardDisplay> enemies = cd.GetAdjacentEnemies();
        if (enemies != null && enemies.Count > 0)
        {
            CardDisplay target = null;
            bool bestKillable = false;
            int bestScore = int.MinValue;
            foreach (CardDisplay e in enemies)
            {
                if (e == null || e.currentTile == null) continue;
                int ehp = e.currentShield + e.currentHealth;
                bool killable = ehp <= cd.currentAttack;
                int score = killable ? 1000 + e.currentAttack : -ehp;
                if ((killable && !bestKillable) ||
                    (killable == bestKillable && score > bestScore))
                {
                    target = e;
                    bestKillable = killable;
                    bestScore = score;
                }
            }

            if (target != null && target.currentTile != null)
            {
                PhotonGameManager.Instance.SendTargetedAttackRPC(
                    cd.currentTile.row, cd.currentTile.column,
                    target.currentTile.row, target.currentTile.column);
                yield return new WaitForSeconds(ActionDelay);
                yield return WaitDecisionsClear();
                yield break;
            }
        }

        if (cd.currentTile.row <= TowerReachOf(cd) - 1)
        {
            PhotonGameManager.Instance.SendTowerAttackRPC(
                cd.currentTile.row, cd.currentTile.column, 1);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }
    }

    // ============ COMBATE (Médio/Difícil: foco de fogo global) ============

    // Executa SEMPRE o melhor golpe disponível no tabuleiro inteiro e reavalia
    // — abates confirmados primeiro (gastando o menor ataque suficiente),
    // depois pressão no alvo mais perigoso ou na torre. No Difícil, se o dano
    // disponível na torre já MATA, tudo vai na torre (corrida de dano letal).
    IEnumerator SmartCombat()
    {
        int safety = 40;
        while (safety-- > 0 && StillMyTurn())
        {
            yield return WaitDecisionsClear();
            if (!StillMyTurn()) yield break;

            // Corrida letal (Difícil): soma do ataque das cartas no alcance da
            // torre ≥ vida da torre → ignora trocas e fecha o jogo
            if (BotMode.Difficulty >= 2)
            {
                CardDisplay finisher = LethalTowerHitter();
                if (finisher != null)
                {
                    PhotonGameManager.Instance.SendTowerAttackRPC(
                        finisher.currentTile.row, finisher.currentTile.column, 1);
                    yield return new WaitForSeconds(ActionDelay);
                    continue;
                }
            }

            CardDisplay attacker, target;
            bool hitTower;
            if (!PickBestStrike(out attacker, out target, out hitTower)) break;

            if (hitTower)
                PhotonGameManager.Instance.SendTowerAttackRPC(
                    attacker.currentTile.row, attacker.currentTile.column, 1);
            else
                PhotonGameManager.Instance.SendTargetedAttackRPC(
                    attacker.currentTile.row, attacker.currentTile.column,
                    target.currentTile.row, target.currentTile.column);

            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }
    }

    // Se o dano somado das cartas prontas no alcance da torre mata o jogador,
    // devolve uma delas para começar a sequência (senão null)
    CardDisplay LethalTowerHitter()
    {
        TurnManager tm = TurnManager.Instance;
        PlayerData human = tm != null ? tm.GetPlayer(1) : null;
        if (human == null || human.health <= 0) return null;

        int damage = 0;
        CardDisplay first = null;
        foreach (CardDisplay c in MyBoardCards())
        {
            if (c == null || c.currentTile == null || !c.CanAttackPeek()) continue;
            if (c.currentAttack <= 0) continue;
            if (c.currentTile.row > TowerReachOf(c) - 1) continue;
            damage += c.currentAttack;
            if (first == null) first = c;
        }
        return damage >= human.health ? first : null;
    }

    // Melhor golpe no tabuleiro inteiro: pares (atacante, alvo) pontuados —
    // abate > pressão em ameaça > torre. Atacante com 0 de ataque não age.
    bool PickBestStrike(out CardDisplay attacker, out CardDisplay target, out bool hitTower)
    {
        attacker = null;
        target = null;
        hitTower = false;
        int bestScore = int.MinValue;

        foreach (CardDisplay c in MyBoardCards())
        {
            if (c == null || c.currentTile == null || !c.CanAttackPeek()) continue;
            if (c.currentAttack <= 0) continue; // golpe de 0 não faz nada

            // Torre como opção deste atacante (se está no alcance dela)
            if (c.currentTile.row <= TowerReachOf(c) - 1)
            {
                int towerScore = 30 + (int)(aggression * 50f);
                if (towerScore > bestScore)
                {
                    bestScore = towerScore;
                    attacker = c;
                    target = null;
                    hitTower = true;
                }
            }

            List<CardDisplay> enemies = c.GetAdjacentEnemies();
            if (enemies == null) continue;

            foreach (CardDisplay e in enemies)
            {
                if (e == null || e.currentTile == null) continue;

                int ehp = e.currentShield + e.currentHealth;
                bool kill = c.currentAttack >= ehp;

                // Abate: mata a MAIOR ameaça gastando o MENOR ataque (uma carta
                // de 6 não desperdiça o golpe num alvo de 1 se uma de 3 resolve).
                // Pressão: amassa a maior ameaça, preferindo quem está mais
                // perto de cair (tanque cheio de escudo perde para a torre).
                int score = kill
                    ? 100000 + ThreatOf(e) * 100 - c.currentAttack * 10
                    : ThreatOf(e) * 4 - ehp * 2;

                if (score > bestScore)
                {
                    bestScore = score;
                    attacker = c;
                    target = e;
                    hitTower = false;
                }
            }
        }
        return attacker != null;
    }

    // ================== MOVIMENTO ==================

    IEnumerator MovePhase()
    {
        List<CardDisplay> myCards = new List<CardDisplay>(MyBoardCards());

        foreach (CardDisplay cd in myCards)
        {
            if (!StillMyTurn()) yield break;
            if (cd == null || cd.currentTile == null || !cd.CanMovePeek()) continue;

            CardTile step = ChooseStepTile(cd);
            if (step == null) continue;

            PhotonGameManager.Instance.SendMoveCardRPC(
                cd.currentTile.row, cd.currentTile.column, step.row, step.column);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }
    }

    // Mesma regra do GameManager.TowerReach (privado lá)
    int TowerReachOf(CardDisplay cd)
    {
        if (cd != null && cd.card != null &&
            (cd.card.cardClass == CardClass.Arqueiro || cd.card.cardClass == CardClass.Mago))
            return 2;
        return 1;
    }

    // Passo de movimento: 1 casa ortogonal livre que mais aproxima do objetivo.
    // Objetivo = inimigo mais próximo (no Difícil, invasores perto da torre do
    // bot têm prioridade — a carta volta para DEFENDER); sem inimigos, avança
    // para a torre do jogador (fileira 0).
    CardTile ChooseStepTile(CardDisplay cd)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cd.currentTile == null) return null;

        int myRow = cd.currentTile.row;
        int myCol = cd.currentTile.column;

        // Objetivo e distância em que a carta quer PARAR (alcance de ataque)
        int goalRow = 0, goalCol = myCol, stopAt = 0;
        CardDisplay chosen = null;
        int chosenScore = int.MaxValue;

        foreach (CardDisplay e in EnemyBoardCards())
        {
            if (e == null || e.currentTile == null) continue;
            int d = Mathf.Abs(e.currentTile.row - myRow) + Mathf.Abs(e.currentTile.column - myCol);

            // Difícil: inimigo invadindo o lado do bot "parece mais perto" —
            // os defensores convergem para ele em vez de seguir avançando
            int score = d;
            if (BotMode.Difficulty >= 2 && e.currentTile.row >= board.rows - 3)
                score -= 4;

            if (score < chosenScore) { chosenScore = score; chosen = e; }
        }

        bool ranged = cd.card != null &&
            (cd.card.cardClass == CardClass.Arqueiro || cd.card.cardClass == CardClass.Mago);

        if (chosen != null)
        {
            goalRow = chosen.currentTile.row;
            goalCol = chosen.currentTile.column;
            stopAt = ranged ? 2 : 1; // Ranged não precisa colar no alvo
        }

        int currentDist = Mathf.Abs(goalRow - myRow) + Mathf.Abs(goalCol - myCol);
        if (chosen != null && currentDist <= stopAt) return null; // Já no alcance

        // Candidatos (regras v4.2, espelham GameManager.IsValidMovement):
        // cruz de 1 e de 2 casas (a de 2 exige o meio livre) para todos;
        // diagonal de 1 casa para Tank/Healer. Escolhe o que mais aproxima.
        bool diagonals = cd.card != null &&
            (cd.card.cardClass == CardClass.Tank || cd.card.cardClass == CardClass.Healer);

        var steps = new List<int[]> {
            new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 },
            new[] { -2, 0 }, new[] { 2, 0 }, new[] { 0, -2 }, new[] { 0, 2 }
        };
        if (diagonals)
        {
            steps.Add(new[] { -1, -1 }); steps.Add(new[] { -1, 1 });
            steps.Add(new[] { 1, -1 }); steps.Add(new[] { 1, 1 });
        }

        CardTile best = null;
        int bestDist = currentDist; // Só anda se ficar estritamente mais perto

        foreach (int[] s in steps)
        {
            int r = myRow + s[0], c = myCol + s[1];
            CardTile tile = board.GetTile(r, c);
            if (tile == null || tile.occupiedCard != null) continue;

            // Passo de 2 casas: a casa do meio precisa estar livre
            if (Mathf.Abs(s[0]) == 2 || Mathf.Abs(s[1]) == 2)
            {
                CardTile mid = board.GetTile(myRow + s[0] / 2, myCol + s[1] / 2);
                if (mid == null || mid.occupiedCard != null) continue;
            }

            int d = Mathf.Abs(goalRow - r) + Mathf.Abs(goalCol - c);
            if (d < bestDist)
            {
                bestDist = d;
                best = tile;
            }
        }
        return best;
    }

    // ================== POSICIONAMENTO ==================

    // Fileira por classe (Médio+): Tank na frente, Arqueiro/Mago/Healer atrás.
    // Coluna: no Difícil, primeiro a coluna do invasor mais avançado (defesa
    // de lane); senão centro para fora. Fácil: regra antiga (frente, centro).
    CardTile ChoosePlacementTile(Card card)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return null;

        int frontRow = board.rows - 2;
        int backRow = board.rows - 1;

        bool backliner = BotMode.Difficulty >= 1 && card != null &&
            (card.cardClass == CardClass.Arqueiro ||
             card.cardClass == CardClass.Mago ||
             card.cardClass == CardClass.Healer);

        int[] rowsToTry = backliner
            ? new[] { backRow, frontRow }
            : new[] { frontRow, backRow };

        // Ordem de colunas: lane ameaçada primeiro (Difícil), depois centro-fora
        List<int> cols = new List<int>();
        if (BotMode.Difficulty >= 2)
        {
            int threatCol = ThreatenedColumn();
            if (threatCol >= 0) cols.Add(threatCol);
        }
        int center = board.columns / 2;
        for (int offset = 0; offset <= center; offset++)
        {
            foreach (int col in new int[] { center - offset, center + offset })
            {
                if (col < 0 || col >= board.columns || cols.Contains(col)) continue;
                cols.Add(col);
            }
        }

        foreach (int row in rowsToTry)
            foreach (int col in cols)
            {
                CardTile tile = board.GetTile(row, col);
                if (tile != null && tile.occupiedCard == null) return tile;
            }
        return null;
    }

    // Coluna do inimigo mais avançado em direção à torre do bot (só conta se
    // já cruzou o meio); empate = o de maior ameaça
    int ThreatenedColumn()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return -1;

        int bestCol = -1, bestRow = -1, bestThreat = -1;
        foreach (CardDisplay e in EnemyBoardCards())
        {
            if (e == null || e.currentTile == null) continue;
            if (e.currentTile.row < board.rows / 2) continue; // ainda longe

            int threat = ThreatOf(e);
            if (e.currentTile.row > bestRow ||
                (e.currentTile.row == bestRow && threat > bestThreat))
            {
                bestRow = e.currentTile.row;
                bestCol = e.currentTile.column;
                bestThreat = threat;
            }
        }
        return bestCol;
    }

    // ================== ESCOLHAS DE ALVO DE EFEITO ==================
    // O GameManager chama estes hooks quando uma carta DO BOT pede escolha de
    // alvo (congelar, quebrar armadura, efeitos 4-9). O bot escolhe sozinho e
    // envia pelo MESMO RPC_EffectTarget do jogador humano. Com um pequeno
    // atraso: a escolha síncrona re-entraria no fluxo do efeito em andamento.

    public static void AutoChooseFreezeTarget(CardDisplay mageCard)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.FreezeChoiceRoutine(mageCard));
    }

    IEnumerator FreezeChoiceRoutine(CardDisplay mage)
    {
        yield return new WaitForSeconds(0.6f);
        if (mage == null || mage.currentTile == null || BoardManager.Instance == null) yield break;

        // Congelar tira um ATAQUE do turno inimigo: alvo = maior ataque
        // (Fácil: "maior carta" genérica, como antes)
        List<CardDisplay> enemies = EnemyBoardCards();
        CardDisplay target = BotMode.Difficulty >= 1
            ? HighestAttackOf(enemies)
            : StrongestOf(enemies);
        if (target == null || target.currentTile == null) yield break;

        PhotonGameManager.Instance.SendEffectTargetRPC(1,
            mage.currentTile.row, mage.currentTile.column,
            target.currentTile.row, target.currentTile.column);
    }

    public static void AutoChooseShieldBreakTargets(CardDisplay mageCard)
    {
        if (Instance != null) Instance.StartCoroutine(Instance.ShieldBreakChoiceRoutine(mageCard));
    }

    IEnumerator ShieldBreakChoiceRoutine(CardDisplay mage)
    {
        yield return new WaitForSeconds(0.6f);
        if (mage == null || mage.currentTile == null || BoardManager.Instance == null) yield break;

        // Até 2 inimigos distintos, com mais armadura primeiro (é quebra de armadura)
        List<CardDisplay> enemies = new List<CardDisplay>(EnemyBoardCards());
        enemies.RemoveAll(e => e == null || e.currentTile == null);
        enemies.Sort((a, b) => b.currentShield.CompareTo(a.currentShield));

        int need = Mathf.Min(2, enemies.Count);
        for (int i = 0; i < need; i++)
        {
            if (mage == null || mage.currentTile == null) yield break;
            CardDisplay t = enemies[i];
            if (t == null || t.currentTile == null) continue;

            PhotonGameManager.Instance.SendEffectTargetRPC(2,
                mage.currentTile.row, mage.currentTile.column,
                t.currentTile.row, t.currentTile.column);
            yield return new WaitForSeconds(0.4f);
        }
    }

    public static void AutoChooseEffectTarget(CardDisplay source, int effectType, List<CardDisplay> candidates)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.EffectChoiceRoutine(source, effectType, candidates));
    }

    // Alvo dos efeitos de TORRE do bot (Tempestade/Nevasca): mesma ideia dos
    // efeitos de carta, mas a escolha volta pelo RPC_TowerEffectTarget
    public static void AutoChooseTowerEffectTarget(int ownerPlayer, int towerCardId, List<CardDisplay> candidates)
    {
        if (Instance != null)
            Instance.StartCoroutine(Instance.TowerEffectChoiceRoutine(ownerPlayer, towerCardId, candidates));
    }

    IEnumerator TowerEffectChoiceRoutine(int player, int towerCardId, List<CardDisplay> candidates)
    {
        yield return new WaitForSeconds(0.6f);
        if (candidates == null) yield break;
        candidates.RemoveAll(c => c == null || c.currentTile == null);
        if (candidates.Count == 0) yield break;

        // Nevasca (congelar) → maior ataque; Tempestade (dano) → maior ameaça
        CardDisplay target;
        if (BotMode.Difficulty == 0)
            target = StrongestOf(candidates);
        else if (towerCardId == TowerCards.Nevasca)
            target = HighestAttackOf(candidates);
        else
            target = HighestThreatOf(candidates);

        if (target == null || target.currentTile == null) yield break;

        PhotonGameManager.Instance.SendTowerEffectTargetRPC(player, towerCardId,
            target.currentTile.row, target.currentTile.column);
    }

    IEnumerator EffectChoiceRoutine(CardDisplay source, int effectType, List<CardDisplay> candidates)
    {
        yield return new WaitForSeconds(0.6f);
        if (source == null || source.currentTile == null || candidates == null) yield break;

        candidates.RemoveAll(c => c == null || c.currentTile == null);

        // Alvo por efeito (Médio+; Fácil usa o "mais forte" genérico):
        //   4 = copiar stats de inimigo  → melhor conjunto de stats
        //   5 = destruir tier inferior   → a maior AMEAÇA destrutível
        //   6 = remover bônus            → quem MAIS GANHOU stats sobre a base
        //   7 = duplicar stats de aliado → o maior ataque (dobrar dano)
        //   8 = invulnerabilidade        → o aliado mais valioso
        //   9 = +3 armadura em Mago      → o mago mais forte
        //   10/13/14 = danos escolhidos  → a maior ameaça (v4.2: eram aleatórios)
        //   11/12/15/16 = congelamentos  → o maior ataque (tira um golpe do turno)
        CardDisplay target;
        if (BotMode.Difficulty == 0)
        {
            target = StrongestOf(candidates);
        }
        else
        {
            switch (effectType)
            {
                case 5: target = HighestThreatOf(candidates); break;
                case 6: target = BiggestBonusOf(candidates); break;
                case 7: target = HighestAttackOf(candidates); break;
                case 8: target = HighestThreatOf(candidates); break;
                case 10: case 13: case 14: case 17: target = HighestThreatOf(candidates); break;
                case 11: case 12: case 15: case 16: target = HighestAttackOf(candidates); break;
                default: target = StrongestOf(candidates); break; // 4, 9 e novos
            }
        }
        if (target == null || target.currentTile == null) yield break;

        PhotonGameManager.Instance.SendEffectTargetRPC(effectType,
            source.currentTile.row, source.currentTile.column,
            target.currentTile.row, target.currentTile.column);
    }

    // "Melhor" alvo genérico: vale para buff em aliado (fortalece o mais forte)
    // e para efeito ofensivo (neutraliza o inimigo mais perigoso)
    CardDisplay StrongestOf(List<CardDisplay> cards)
    {
        CardDisplay best = null;
        int bestScore = int.MinValue;

        if (cards == null) return null;
        foreach (CardDisplay c in cards)
        {
            if (c == null || c.currentTile == null) continue;
            int score = c.currentAttack * 2 + c.currentShield + c.currentHealth;
            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best;
    }

    CardDisplay HighestAttackOf(List<CardDisplay> cards)
    {
        CardDisplay best = null;
        int bestScore = int.MinValue;

        if (cards == null) return null;
        foreach (CardDisplay c in cards)
        {
            if (c == null || c.currentTile == null) continue;
            int score = c.currentAttack * 100 + c.currentShield + c.currentHealth;
            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best;
    }

    CardDisplay HighestThreatOf(List<CardDisplay> cards)
    {
        CardDisplay best = null;
        int bestScore = int.MinValue;

        if (cards == null) return null;
        foreach (CardDisplay c in cards)
        {
            if (c == null || c.currentTile == null) continue;
            int score = ThreatOf(c);
            if (score > bestScore) { bestScore = score; best = c; }
        }
        return best;
    }

    // Quem mais GANHOU stats em relação à base (para "remover bônus");
    // ninguém com bônus → cai no mais forte
    CardDisplay BiggestBonusOf(List<CardDisplay> cards)
    {
        CardDisplay best = null;
        int bestBonus = 0;

        if (cards == null) return null;
        foreach (CardDisplay c in cards)
        {
            if (c == null || c.currentTile == null || c.card == null) continue;
            int bonus = (c.currentAttack - c.card.attack)
                      + (c.currentShield - c.card.shield)
                      + (c.currentHealth - c.card.health);
            if (bonus > bestBonus) { bestBonus = bonus; best = c; }
        }
        return best != null ? best : StrongestOf(cards);
    }
}
