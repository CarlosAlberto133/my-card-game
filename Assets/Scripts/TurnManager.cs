using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Lobby,      // Aguardando jogadores clicarem "Iniciar Partida"
    Playing     // Jogo em andamento
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public PlayerData player1;
    public PlayerData player2;

    public int currentPlayerNumber = 1;
    public GameState gameState = GameState.Lobby;
    public int currentRound = 0;

    // Controle de quem está pronto para iniciar
    public bool player1Ready = false;
    public bool player2Ready = false;

    // Momento em que a partida começou (para calcular a duração no upload
    // da partida — MatchReporter). 0 = ainda não começou.
    public float matchStartRealtime = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Inicializa os jogadores
        player1 = new PlayerData("Jogador 1");
        player2 = new PlayerData("Jogador 2");

        // Começa no LOBBY - aguardando jogadores iniciarem
        gameState = GameState.Lobby;
        currentRound = 0;

        // O gate é static: limpa restos de uma partida anterior (recarregar cena)
        DuplicateEffectGate.ResetTurn();

        Debug.Log("Aguardando jogadores clicarem em 'Iniciar Partida'");
    }

    public void SetPlayers(int myPlayerNumber, int opponentPlayerNumber)
    {
        Debug.Log($"[TurnManager] Jogadores sincronizados: Eu={myPlayerNumber}, Oponente={opponentPlayerNumber}");
        // Os jogadores já estão inicializados, apenas log para confirmação
        // No futuro, você pode usar myPlayerNumber para determinar a câmera, controles, etc.
    }

    void Update()
    {
        // ESPAÇO passa a vez
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestEndTurn();
        }
    }

    // Pede para passar a vez. Em multiplayer, valida se é seu turno e sincroniza via RPC.
    public void RequestEndTurn()
    {
        // Fase inicial de compras é SIMULTÂNEA: não existe passar a vez
        if (gameState == GameState.Lobby)
        {
            Debug.Log("[TurnManager] Fase de compras: não há turnos, compre e clique em Iniciar Partida!");
            return;
        }

        // Não deixa passar a vez com decisão de efeito pendente: o dano/efeito
        // daquela decisão ainda vai ser aplicado e mudaria o estado fora de ordem
        if (GameManager.IsDecisionPending())
        {
            Debug.Log("[TurnManager] Aguarde a decisão de efeito ser resolvida antes de passar a vez!");
            return;
        }

        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            if (currentPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
            {
                Debug.Log("[TurnManager] Não é seu turno, não pode passar a vez!");
                return;
            }
            PhotonGameManager.Instance.SendEndTurnRPC();
        }
        else
        {
            EndTurn();
        }
    }

    // Marca um jogador como pronto (multiplayer: chamado via RPC nos dois clientes)
    public void SetPlayerReady(int playerNumber)
    {
        if (gameState != GameState.Lobby)
        {
            Debug.LogWarning($"SetPlayerReady ignorado, estado atual: {gameState}");
            return;
        }

        if (playerNumber == 1) player1Ready = true;
        else if (playerNumber == 2) player2Ready = true;

        Debug.Log($"[TurnManager] Jogador {playerNumber} pronto! (P1: {player1Ready}, P2: {player2Ready})");

        if (player1Ready && player2Ready)
        {
            StartGame();
        }
    }

    public void OnPlayerReadyToStart(int playerNumber)
    {
        Debug.Log($"OnPlayerReadyToStart chamado. Estado atual: {gameState}");

        if (gameState != GameState.Lobby)
        {
            Debug.LogWarning($"Não está no Lobby! Estado: {gameState}");
            return;
        }

        // Se nenhum jogador está pronto, marca o primeiro
        if (!player1Ready && !player2Ready)
        {
            player1Ready = true;
            Debug.Log($"Primeiro clique! Jogador 1 marcado como pronto. Clique novamente para iniciar.");
        }
        // Se já tem um pronto, marca o segundo e inicia
        else if (player1Ready && !player2Ready)
        {
            player2Ready = true;
            Debug.Log($"Segundo clique! Ambos jogadores prontos. Iniciando jogo...");
            StartGame();
        }
        // Se já marcou o P2 mas não o P1 (caso improvável)
        else if (!player1Ready && player2Ready)
        {
            player1Ready = true;
            Debug.Log($"Ambos jogadores prontos! Iniciando jogo...");
            StartGame();
        }
        // Se ambos já estão prontos (não deveria chegar aqui)
        else
        {
            Debug.Log("Ambos já estavam prontos, iniciando...");
            StartGame();
        }
    }

    private void StartGame()
    {
        Debug.Log("========== INICIANDO PARTIDA ==========");
        Debug.Log("Ambos jogadores prontos!");

        gameState = GameState.Playing;
        currentRound = 1;
        currentPlayerNumber = 1;

        // Marca o início (duração da partida) e libera o upload do fim dela
        matchStartRealtime = Time.realtimeSinceStartup;
        MatchReporter.ResetForNewMatch();
        MatchLogRecorder.MarkRound(1); // marcador para o report recortar o round

        // Sistema de torres: zera o estado e abre a tela de escolha (bloqueia
        // o input do jogo até os DOIS jogadores confirmarem a torre)
        TowerSystem.ResetForMatch();
        TowerSelectUI.Open();

        // Resetar flags de ready
        player1Ready = false;
        player2Ready = false;

        // Resetar contador de cartas compradas neste turno
        player1.cardsBoughtThisTurn = 0;
        player2.cardsBoughtThisTurn = 0;
        player1.storeResetsThisTurn = 0;
        player2.storeResetsThisTurn = 0;

        // A fase de compras usa teto de 20 de ouro; a partida volta ao teto 10:
        // sobra acima de 10 vira 10, abaixo de 10 fica como está
        if (player1.gold > 10) player1.gold = 10;
        if (player2.gold > 10) player2.gold = 10;

        Debug.Log($"Estado mudou para: {gameState}");
        Debug.Log($"Round: {currentRound}");
        Debug.Log($"Jogador atual: {currentPlayerNumber}");

        // Descarta as filas da fase inicial (a partida volta a sortear do pool)
        CardPool cardPool = FindObjectOfType<CardPool>();
        if (cardPool != null)
        {
            cardPool.ClearLobbyQueues();
        }

        // Notificar CardManager para mover cartas para a direita
        if (CardManager.Instance != null)
        {
            Debug.Log("Notificando CardManager...");
            CardManager.Instance.OnGameStart();
        }
        else
        {
            Debug.LogError("CardManager.Instance é NULL!");
        }

        Debug.Log("========== PARTIDA INICIADA ==========");
    }

    public PlayerData GetCurrentPlayer()
    {
        return currentPlayerNumber == 1 ? player1 : player2;
    }

    public PlayerData GetPlayer(int playerNum)
    {
        return playerNum == 1 ? player1 : player2;
    }

    public void EndTurn()
    {
        // No Lobby, reseta cartas compradas e passa a vez (não conta rounds)
        if (gameState == GameState.Lobby)
        {
            GetCurrentPlayer().ResetTurn(); // ✅ Permite comprar 1 carta no próximo turno
            currentPlayerNumber = currentPlayerNumber == 1 ? 2 : 1;
            Debug.Log($"Lobby: Turno passou para {GetCurrentPlayer().playerName}");
            return;
        }

        // Durante o jogo, reseta o turno e conta rounds
        GetCurrentPlayer().ResetTurn();

        // Rastrear quem passou a vez
        int previousPlayer = currentPlayerNumber;

        // Round completa quando J2 passa a vez e volta para J1
        bool roundCompleted = previousPlayer == 2;

        // IMPORTANTE: os efeitos de fim de turno rodam ANTES da troca de jogador.
        // Efeitos que congelam/stunam aqui (Mage 5, Archer 4) precisam ver o
        // "jogador atual" ainda como quem acabou de jogar — com a troca antes,
        // a vítima recebia duração de "congelada no próprio turno" (2) e perdia
        // DOIS turnos em vez de um.

        // Nenhum modo de seleção de alvo pode sobreviver à passagem de turno —
        // um modo preso sequestrava TODOS os cliques do tabuleiro (o jogador
        // não conseguia mais mover nenhuma carta)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CancelAllTargetSelections();
        }

        // Efeitos de fim de turno — CADA UM blindado: uma exceção aqui num
        // cliente só abortava o EndTurn daquele lado e dessincronizava o turno
        // (o outro sintoma de "não consigo mover nada"). Turno/round/ouro/loja
        // precisam avançar SEMPRE, nos dois clientes.

        // Healer tier-4 (ATK 2, HP 4): ganhar ouro quando o turno do OPONENTE acaba
        try { ActivateHealerTier4OpponentTurnEnd(previousPlayer); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] HealerTier4: {e}"); }

        // Efeitos "por turno" abaixo: só as cartas de QUEM acabou de jogar
        // (previousPlayer). Antes rodavam para os DOIS jogadores em todo fim de
        // turno = frequência dobrada e "efeito do inimigo ativando no meu turno"

        // Tank 4 tier-4 (ATK 3, Shield 6, HP 7): +1 armadura a aliados por turno
        try { ActivateTankTier4Effect4Periodic(previousPlayer); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] TankTier4E4: {e}"); }

        // Healer 5 tier-5 (ATK 2, HP 7): cura todos aliados a cada turno
        try { ActivateHealerTier5Effect2Periodic(previousPlayer); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] HealerTier5E2: {e}"); }

        // Mage 5 tier-5 (ATK 5, HP 5): congela inimigo aleatório uma vez por round
        try { ActivateMageTier5Effect1Periodic(); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] MageTier5E1: {e}"); }

        // Mage 5 tier-5 (ATK 5, HP 6): aumenta ATK de todos Magos ao resetar turno
        try { ActivateMageTier5Effect3Periodic(previousPlayer); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] MageTier5E3: {e}"); }

        // (Tank 5 ATK 3/Sh 6/HP 9 agora é dirigido pelo contador da carta — sem hook)

        // Tique central: durações de status (congelada/stun/águia/invulnerável),
        // contadores de efeito periódico e resets por turno
        try { TickBoardOnTurnEnd(previousPlayer, roundCompleted); }
        catch (System.Exception e) { Debug.LogError($"[EndTurn] TickBoard: {e}"); }

        // Troca o jogador SÓ DEPOIS dos efeitos de fim de turno (ver nota acima)
        currentPlayerNumber = previousPlayer == 1 ? 2 : 1;

        Debug.Log($"Turno passou para {GetCurrentPlayer().playerName}");

        // Verificar se completa o round (quando J2 passa vez e volta para J1)
        if (roundCompleted)
        {
            currentRound++;
            MatchLogRecorder.MarkRound(currentRound); // marcador p/ recorte do report

            // Torres: efeitos periódicos + janela da loja mágica (roda dentro
            // do RPC de fim de turno — idêntico nos dois clientes)
            try { TowerSystem.OnRoundChanged(currentRound); }
            catch (System.Exception e) { Debug.LogError($"[EndTurn] TowerSystem: {e}"); }
            Debug.Log($"Round {currentRound} iniciado!");

            // Checa efeitos periódicos das cartas (como heal do healer a cada 2 rounds)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckPeriodicCardEffects();
            }

            // Ambos jogadores ganham 3 de ouro (máximo 10)
            player1.AddGold(3);
            player2.AddGold(3);

            // Refresh das cartas na loja
            if (CardManager.Instance != null)
            {
                CardManager.Instance.RefreshShop();
            }
        }
    }

    public bool IsPlayerTurn(int playerNum)
    {
        return currentPlayerNumber == playerNum;
    }

    // Limite de resets da loja na fase inicial de compras (por jogador)
    public const int LobbyMaxStoreResets = 2;

    public bool TryResetStore()
    {
        // Compatibilidade: sem jogador explícito, usa o do turno atual
        return TryResetStore(currentPlayerNumber);
    }

    public bool TryResetStore(int playerNumber)
    {
        PlayerData player = GetPlayer(playerNumber);
        bool lobbyPhase = gameState == GameState.Lobby;

        // Quem já clicou em "Iniciar Partida" não faz mais nada na fase inicial
        if (lobbyPhase)
        {
            bool alreadyReady = (playerNumber == 1 && player1Ready) ||
                                (playerNumber == 2 && player2Ready);
            if (alreadyReady)
            {
                Debug.Log($"[TurnManager] {player.playerName} já está pronto — aguarde o oponente!");
                return false;
            }
        }

        // Custo e limite por fase: início = 1 ouro, até 2 resets na fase toda;
        // partida = 2 ouro, 1 reset por turno
        int cost = lobbyPhase ? 1 : 2;
        int maxResets = lobbyPhase ? LobbyMaxStoreResets : 1;

        // Tenta pagar pelo reset
        if (!player.PayForStoreReset(cost, maxResets))
        {
            return false;
        }

        // Reset bem-sucedido, atualizar loja. Em multiplayer cada jogador tem a
        // própria loja: rerola SÓ a de quem pagou (executa nos dois clientes via RPC)
        if (CardManager.Instance != null)
        {
            if (PhotonNetwork.inRoom)
                CardManager.Instance.RefreshShopForPlayer(playerNumber);
            else
                CardManager.Instance.RefreshShop();
            Debug.Log($"Loja resetada por {player.playerName}! Custo: {cost} ouro");
        }
        else
        {
            Debug.LogError("CardManager não encontrado!");
            return false;
        }

        return true;
    }

    // Reinício completo da partida (chamado por PhotonGameManager.DoRestart nos
    // DOIS clientes, com a nova seed já aplicada). A ordem importa: destruir tudo
    // e devolver as cartas ao deck ANTES de respawnar a loja da fase inicial.
    public void RestartGame()
    {
        Debug.Log("========== REINICIANDO JOGO ==========");

        // 1. Destroi as cartas das MÃOS dos dois jogadores. O reset antigo NÃO
        //    fazia isso — as cartas na mão vazavam para a nova partida.
        foreach (HandManager hm in FindObjectsOfType<HandManager>())
            hm.ClearHand();

        // 2. Destroi as cartas do TABULEIRO. ClearAllTiles só zerava as
        //    referências dos tiles — os GameObjects continuavam na cena.
        BoardManager board = BoardManager.Instance;
        if (board != null)
        {
            foreach (CardDisplay c in board.GetAllCards())
                if (c != null) Destroy(c.gameObject);
            board.ClearAllTiles();
        }

        // 3. Destroi as cartas da LOJA, zera os contadores de spawn e volta o
        //    layout/posição da loja para o do lobby (fase inicial no centro).
        if (CardManager.Instance != null)
            CardManager.Instance.ResetForRestart();

        // 4. Devolve TODAS as cartas ao deck. Sem isso o pool fica esvaziado
        //    (cartas que foram para mão/tabuleiro seguem marcadas como usadas) e
        //    a nova loja nasce com poucas ou nenhuma carta.
        CardPool pool = FindObjectOfType<CardPool>();
        if (pool != null)
            pool.ResetPool();

        // 5. Reseta jogadores e estado do jogo (volta ao Lobby)
        player1 = new PlayerData("Jogador 1");
        player2 = new PlayerData("Jogador 2");
        gameState = GameState.Lobby;
        currentRound = 0;
        currentPlayerNumber = 1;
        player1Ready = false;
        player2Ready = false;
        DuplicateEffectGate.ResetTurn();

        // 6. Esconde a tela de vitória
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.HideVictoryScreen();

        // 7. Respawna a loja da fase inicial (com a nova seed já aplicada)
        if (CardManager.Instance != null)
            CardManager.Instance.SpawnRandomCards();

        Debug.Log("========== JOGO REINICIADO - AGUARDANDO NO LOBBY ==========");
    }

    // Sinaliza a fase 2 do tique (efeitos de contador disparando). Stuns/freezes
    // criados nessa fase recebem duração 1 — nada os desconta na mesma passada
    public static bool TickingCounterEffects = false;

    // Tique central de fim de turno: durações de status, contadores de efeito
    // periódico e resets por turno. Roda dentro do RPC_EndTurn = idêntico nos
    // dois clientes (a ordem de GetAllCards é fixa por linha/coluna)
    void TickBoardOnTurnEnd(int endedTurnPlayer, bool roundCompleted)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var cards = board.GetAllCards();

        try
        {
            // FASE 1: durações de status (para TODAS as cartas, ANTES de
            // qualquer efeito de contador disparar)
            foreach (var card in cards)
            {
                if (card == null || card.card == null) continue;
                card.TickStatusDurations(endedTurnPlayer, roundCompleted);
            }

            // FASE 2: contadores de efeito periódico/cooldown (podem stunar/
            // congelar novos alvos — esses status duram 1 turno da vítima)
            TickingCounterEffects = true;
            try
            {
                foreach (var card in cards)
                {
                    if (card == null || card.card == null) continue;
                    card.TickEffectCounter(roundCompleted);
                }
            }
            finally
            {
                TickingCounterEffects = false;
            }
        }
        finally
        {
            // FASE 3: resets "1x por turno" — DEPOIS dos efeitos de contador
            // (rodando antes, o dano da fase 2 reativava intercepto/árvore no
            // mesmo turno). No finally: mesmo que um efeito da fase 2 estoure
            // exceção, os resets e o gate de duplicadas NUNCA pulam um turno
            foreach (var card in cards)
            {
                if (card == null || card.card == null) continue;

                card.treeDefenseActive = false; // Dodge de árvore do Archer (por turno)
                card.treeDefensePopupShown = false;
                card.tankTier4Effect2LastUsedRound = -1; // Intercepto do Tank 4: 1x por TURNO
            }

            // Regra das duplicadas: nova designação a cada turno
            DuplicateEffectGate.ResetTurn();
        }
    }

    // Healer 4 (ATK 2, HP 4): "Receba 1 ouro sempre que o turno do OPONENTE acabar".
    // O beneficiário é o jogador que NÃO passou a vez (antes estava invertido:
    // dava ouro a quem acabou o próprio turno)
    void ActivateHealerTier4OpponentTurnEnd(int endedTurnPlayer)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int beneficiary = endedTurnPlayer == 1 ? 2 : 1;
        var beneficiaryCards = board.GetCardsByOwner(beneficiary);
        if (beneficiaryCards.Count == 0) return;

        // Procura por Healer 4 (ATK 2, HP 4) no campo de quem NÃO jogou
        foreach (var card in beneficiaryCards)
        {
            if (card != null && card.card != null && card.card.cardClass == CardClass.Healer &&
                card.card.attack == 2 && card.card.health == 4 && card.card.tier == CardTier.Tier4)
            {
                if (!DuplicateEffectGate.TryActivate(card)) continue;
                CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    effect.ActivateGoldOnOpponentTurnEnd();
                }
            }
        }
    }

    void ActivateTankTier4Effect4Periodic(int ownerPlayer)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Só os Tank 4 (3/7/8) de quem acabou de jogar o turno
        for (int playerNum = ownerPlayer; playerNum <= ownerPlayer; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card != null && card.card.cardClass == CardClass.Tank &&
                    card.card.attack == 3 && card.card.shield == 7 && card.card.health == 8 &&
                    card.card.tier == CardTier.Tier4)
                {
                    if (!DuplicateEffectGate.TryActivate(card)) continue;
                    CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                    if (effect != null)
                    {
                        effect.ActivateTankTier4Effect4Periodic();
                    }
                }
            }
        }
    }

    void ActivateHealerTier5Effect2Periodic(int ownerPlayer)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Só os Healer 5 (2/7) de quem acabou de jogar o turno
        for (int playerNum = ownerPlayer; playerNum <= ownerPlayer; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card != null && card.card.cardClass == CardClass.Healer &&
                    card.card.attack == 2 && card.card.health == 7 &&
                    card.card.tier == CardTier.Tier5)
                {
                    if (!DuplicateEffectGate.TryActivate(card)) continue;
                    CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                    if (effect != null)
                    {
                        effect.ActivatePeriodicAllyHeal();
                    }
                }
            }
        }
    }

    void ActivateMageTier5Effect1Periodic()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Mage 5 tier-5 (ATK 5, HP 5) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card != null && card.card.cardClass == CardClass.Mago &&
                    card.card.attack == 5 && card.card.health == 5 &&
                    card.card.tier == CardTier.Tier5)
                {
                    // Verifica se já foi usado neste round
                    if (card.mageTier5Effect1LastUsedRound != currentRound)
                    {
                        if (!DuplicateEffectGate.TryActivate(card)) continue;
                        CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            effect.ActivateRandomFreeze();
                            card.mageTier5Effect1LastUsedRound = currentRound;
                        }
                    }
                }
            }
        }
    }

    void ActivateMageTier5Effect3Periodic(int ownerPlayer)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Só os Mage 5 (5/6) de quem acabou de jogar o turno
        for (int playerNum = ownerPlayer; playerNum <= ownerPlayer; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card != null && card.card.cardClass == CardClass.Mago &&
                    card.card.attack == 5 && card.card.health == 6 &&
                    card.card.tier == CardTier.Tier5)
                {
                    if (!DuplicateEffectGate.TryActivate(card)) continue;
                    CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                    if (effect != null)
                    {
                        effect.ActivateMageBoostPerTurn();
                    }
                }
            }
        }
    }

}
