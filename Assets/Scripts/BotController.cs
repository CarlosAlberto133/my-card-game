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

    void Awake()
    {
        Instance = this;
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
            int idx = PickBestAffordableShopIndex(bot);
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

    // Melhor carta comprável da loja do bot: maior tier que o ouro alcança.
    // freeBuy (Healer 5) ignora o ouro.
    int PickBestAffordableShopIndex(PlayerData bot)
    {
        if (CardManager.Instance == null) return -1;

        bool freeBuy = bot.freePurchases > 0;
        int best = -1, bestTier = 0;

        for (int i = 0; i < CardManager.LobbyShopSize; i++)
        {
            GameObject go = CardManager.Instance.GetShopCard(i, BotMode.BotPlayerNumber);
            if (go == null) continue;

            CardDisplay cd = go.GetComponent<CardDisplay>();
            if (cd == null || cd.card == null) continue;
            if (cd.isInHand || cd.isOnBoard) continue; // já comprada

            if (!freeBuy && !bot.HasEnoughGold(cd.card.GetGoldCost())) continue;

            int tier = (int)cd.card.tier;
            if (tier > bestTier)
            {
                bestTier = tier;
                best = i;
            }
        }
        return best;
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

        // 1) Compras do turno (até o limite por turno, priorizando tier alto)
        int buySafety = 6;
        while (buySafety-- > 0 && StillMyTurn() && !HandFull() &&
               (bot.CanBuyCard() || bot.freePurchases > 0))
        {
            int idx = PickBestAffordableShopIndex(bot);
            if (idx < 0) break;

            PhotonGameManager.Instance.SendBuyCardRPC(idx, BotMode.BotPlayerNumber);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear();
        }

        // 2) Posiciona cartas da mão (a mais forte primeiro) nas fileiras do bot
        HandManager hand = GetBotHand();
        int placeSafety = 10;
        while (placeSafety-- > 0 && StillMyTurn() && hand != null && hand.GetCardCount() > 0)
        {
            CardTile tile = FindFreePlacementTile();
            if (tile == null) break;

            int bestIdx = -1, bestAtk = -1;
            for (int i = 0; i < hand.GetCardCount(); i++)
            {
                GameObject go = hand.GetCardAtIndex(i);
                CardDisplay cd = go != null ? go.GetComponent<CardDisplay>() : null;
                if (cd == null || cd.card == null) continue;
                if (cd.card.attack > bestAtk) { bestAtk = cd.card.attack; bestIdx = i; }
            }
            if (bestIdx < 0) break;

            PhotonGameManager.Instance.SendPlaceCardRPC(bestIdx, BotMode.BotPlayerNumber, tile.row, tile.column);
            yield return new WaitForSeconds(ActionDelay);
            yield return WaitDecisionsClear(); // Efeitos de entrada podem abrir popup/escolha
        }

        // 3) Combate: cada carta do bot em campo ataca e avança
        if (BoardManager.Instance != null)
        {
            List<CardDisplay> myCards =
                new List<CardDisplay>(BoardManager.Instance.GetCardsByOwner(BotMode.BotPlayerNumber));

            foreach (CardDisplay cd in myCards)
            {
                if (!StillMyTurn()) yield break;
                if (cd == null || cd.currentTile == null) continue; // morreu no meio do turno
                yield return ActWithCard(cd);
            }
        }

        yield return WaitDecisionsClear();
    }

    IEnumerator ActWithCard(CardDisplay cd)
    {
        // Ataca primeiro se já tem alvo no alcance
        yield return TryAttackWith(cd);

        // Move em direção ao objetivo (inimigo mais próximo, senão a torre)
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

        // Depois de andar tenta atacar de novo (mover e atacar são independentes)
        yield return TryAttackWith(cd);
    }

    IEnumerator TryAttackWith(CardDisplay cd)
    {
        if (!StillMyTurn() || cd == null || cd.currentTile == null || !cd.CanAttackPeek())
            yield break;

        List<CardDisplay> enemies = cd.GetAdjacentEnemies();
        if (enemies != null && enemies.Count > 0)
        {
            CardDisplay target = PickAttackTarget(cd, enemies);
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

        // Sem carta no alcance: ataca a torre do jogador se estiver perto
        // (P2 avança para a fileira 0; Magos/Arqueiros alcançam da fileira 1)
        int reach = TowerReachOf(cd);
        if (cd.currentTile.row <= reach - 1)
        {
            PhotonGameManager.Instance.SendTowerAttackRPC(
                cd.currentTile.row, cd.currentTile.column, 1);
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

    // Alvo do ataque: prioriza quem MORRE com este golpe (maior ataque primeiro
    // entre os matáveis); senão, o de menor vida efetiva (escudo + vida)
    CardDisplay PickAttackTarget(CardDisplay attacker, List<CardDisplay> enemies)
    {
        CardDisplay best = null;
        bool bestKillable = false;
        int bestScore = int.MinValue;

        foreach (CardDisplay e in enemies)
        {
            if (e == null || e.currentTile == null) continue;

            int ehp = e.currentShield + e.currentHealth;
            bool killable = ehp <= attacker.currentAttack;

            // Matável: quanto mais forte o alvo, melhor. Não matável: quanto
            // mais perto de morrer, melhor.
            int score = killable ? 1000 + e.currentAttack : -ehp;

            if ((killable && !bestKillable) ||
                (killable == bestKillable && score > bestScore))
            {
                best = e;
                bestKillable = killable;
                bestScore = score;
            }
        }
        return best;
    }

    // Posição para cartas novas: fileiras do P2 (as 2 últimas), preferindo a da
    // frente e as colunas centrais
    CardTile FindFreePlacementTile()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return null;

        int totalRows = board.rows;
        int center = board.columns / 2;

        // Fileira da frente primeiro (mais perto do combate)
        int[] rowsToTry = { totalRows - 2, totalRows - 1 };
        foreach (int row in rowsToTry)
        {
            for (int offset = 0; offset <= center; offset++)
            {
                foreach (int col in new int[] { center - offset, center + offset })
                {
                    if (col < 0 || col >= board.columns) continue;
                    CardTile tile = board.GetTile(row, col);
                    if (tile != null && tile.occupiedCard == null) return tile;
                }
            }
        }
        return null;
    }

    // Passo de movimento: 1 casa ortogonal livre que mais aproxima do objetivo.
    // Objetivo = inimigo mais próximo (parando no alcance de ataque da carta);
    // sem inimigos, avança em direção à torre do jogador (fileira 0).
    CardTile ChooseStepTile(CardDisplay cd)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cd.currentTile == null) return null;

        int myRow = cd.currentTile.row;
        int myCol = cd.currentTile.column;

        // Objetivo e distância em que a carta quer PARAR (alcance de ataque)
        int goalRow = 0, goalCol = myCol, stopAt = 0;
        CardDisplay nearest = null;
        int nearestDist = int.MaxValue;

        foreach (CardDisplay e in board.GetCardsByOwner(1))
        {
            if (e == null || e.currentTile == null) continue;
            int d = Mathf.Abs(e.currentTile.row - myRow) + Mathf.Abs(e.currentTile.column - myCol);
            if (d < nearestDist) { nearestDist = d; nearest = e; }
        }

        bool ranged = cd.card != null &&
            (cd.card.cardClass == CardClass.Arqueiro || cd.card.cardClass == CardClass.Mago);

        if (nearest != null)
        {
            goalRow = nearest.currentTile.row;
            goalCol = nearest.currentTile.column;
            stopAt = ranged ? 2 : 1; // Ranged não precisa colar no alvo
        }

        int currentDist = Mathf.Abs(goalRow - myRow) + Mathf.Abs(goalCol - myCol);
        if (nearest != null && currentDist <= stopAt) return null; // Já no alcance

        // Candidatos: 4 vizinhos ortogonais livres; escolhe o que mais aproxima
        int[][] steps = {
            new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 }
        };

        CardTile best = null;
        int bestDist = currentDist; // Só anda se ficar estritamente mais perto

        foreach (int[] s in steps)
        {
            int r = myRow + s[0], c = myCol + s[1];
            CardTile tile = board.GetTile(r, c);
            if (tile == null || tile.occupiedCard != null) continue;

            int d = Mathf.Abs(goalRow - r) + Mathf.Abs(goalCol - c);
            if (d < bestDist)
            {
                bestDist = d;
                best = tile;
            }
        }
        return best;
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

        CardDisplay target = StrongestOf(BoardManager.Instance.GetCardsByOwner(1));
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
        List<CardDisplay> enemies = new List<CardDisplay>(BoardManager.Instance.GetCardsByOwner(1));
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

    IEnumerator EffectChoiceRoutine(CardDisplay source, int effectType, List<CardDisplay> candidates)
    {
        yield return new WaitForSeconds(0.6f);
        if (source == null || source.currentTile == null || candidates == null) yield break;

        candidates.RemoveAll(c => c == null || c.currentTile == null);
        CardDisplay target = StrongestOf(candidates);
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
}
