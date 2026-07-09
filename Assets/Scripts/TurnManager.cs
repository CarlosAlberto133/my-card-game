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
        Debug.Log("Aguardando jogadores clicarem em 'Iniciar Partida'");
    }

    void Update()
    {
        // ESPAÇO passa a vez
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            EndTurn();
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

        // Resetar flags de ready
        player1Ready = false;
        player2Ready = false;

        // Resetar contador de cartas compradas neste turno
        player1.cardsBoughtThisTurn = 0;
        player2.cardsBoughtThisTurn = 0;

        Debug.Log($"Estado mudou para: {gameState}");
        Debug.Log($"Round: {currentRound}");
        Debug.Log($"Jogador atual: {currentPlayerNumber}");

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

        // Passa para o próximo jogador
        currentPlayerNumber = currentPlayerNumber == 1 ? 2 : 1;

        // Ativa Healer tier-4 (ATK 4, HP 3) efeito: ganhar ouro ao fim do turno do oponente
        ActivateHealerTier4OpponentTurnEnd(previousPlayer);

        // Ativa Tank 4 tier-4 (ATK 5, Shield 10, HP 10) efeito: +1 armadura a aliados
        ActivateTankTier4Effect4Periodic();

        // Ativa Healer 5 tier-5 (ATK 6, HP 3) efeito: cura todos aliados a cada turno
        ActivateHealerTier5Effect2Periodic();

        // Ativa Mage 5 tier-5 (ATK 8, HP 4) efeito: congela inimigos aleatórios uma vez por round
        ActivateMageTier5Effect1Periodic();

        // Ativa Mage 5 tier-5 (ATK 7, HP 5) efeito: aumenta ATK de todos Magos ao resetar turno
        ActivateMageTier5Effect3Periodic();

        // Ativa Tank 5 tier-5 (ATK 2, Shield 6, HP 8) efeito: concede armadura a cada 2 turnos
        ActivateTankTier5Effect2Periodic();

        // Reseta efeito de árvore, popup, descongelamento, desestunamento, marca de águia e efeitos de Healer tier-2
        BoardManager board = BoardManager.Instance;
        if (board != null)
        {
            foreach (var card in board.GetAllCards())
            {
                if (card != null)
                {
                    card.treeDefenseActive = false;
                    card.treeDefensePopupShown = false;
                    card.CheckAndUnfreeze(); // Descongelamento (Mage 3)
                    card.CheckAndUnstun(); // Desestunamento (Archer tier-2)
                    card.CheckAndUneagleMark(); // Remove marca de águia (Archer 3 tier-3)
                    card.healerShieldUseCount = 0; // Reset usos de +armadura (Healer 2 ATK 1, HP 3)
                }
            }
        }

        Debug.Log($"Turno passou para {GetCurrentPlayer().playerName}");

        // Verificar se completa o round (quando J2 passa vez e volta para J1)
        if (previousPlayer == 2 && currentPlayerNumber == 1)
        {
            currentRound++;
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

    public bool TryResetStore()
    {
        PlayerData currentPlayer = GetCurrentPlayer();

        // Determinar custo baseado no estado do jogo
        int cost = gameState == GameState.Lobby ? 1 : 2;

        // Tenta pagar pelo reset
        if (!currentPlayer.PayForStoreReset(cost))
        {
            return false;
        }

        // Reset bem-sucedido, atualizar loja
        if (CardManager.Instance != null)
        {
            CardManager.Instance.RefreshShop();
            Debug.Log($"Loja resetada! Custo: {cost} ouro");
        }
        else
        {
            Debug.LogError("CardManager não encontrado!");
            return false;
        }

        return true;
    }

    public void RestartGame()
    {
        Debug.Log("========== REINICIANDO JOGO ==========");

        // Resetar dados dos jogadores
        player1 = new PlayerData("Jogador 1");
        player2 = new PlayerData("Jogador 2");

        // Resetar estado do jogo
        gameState = GameState.Lobby;
        currentRound = 0;
        currentPlayerNumber = 1;

        // Resetar flags
        player1Ready = false;
        player2Ready = false;

        // Limpar todas as cartas do jogo
        if (CardManager.Instance != null)
        {
            CardManager.Instance.DestroyAllCards();
        }

        // Limpar o tabuleiro
        if (BoardManager.Instance != null)
        {
            BoardManager.Instance.ClearAllTiles();
        }

        // Esconder tela de vitória
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideVictoryScreen();
        }

        // Spawnar novas cartas na loja
        if (CardManager.Instance != null)
        {
            CardManager.Instance.SpawnRandomCards();
        }

        Debug.Log("========== JOGO REINICIADO - AGUARDANDO NO LOBBY ==========");
    }

    void ActivateHealerTier4OpponentTurnEnd(int opponentPlayerNumber)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var opponentAllies = board.GetCardsByOwner(opponentPlayerNumber);
        if (opponentAllies.Count == 0) return;

        // Procura por Healer 4 (ATK 4, HP 3) no campo do oponente
        foreach (var card in opponentAllies)
        {
            if (card != null && card.card.cardClass == CardClass.Healer &&
                card.card.attack == 4 && card.card.health == 3 && card.card.tier == CardTier.Tier4)
            {
                CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    effect.ActivateGoldOnOpponentTurnEnd();
                }
            }
        }
    }

    void ActivateTankTier4Effect4Periodic()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Tank 4 tier-4 (ATK 5, Shield 10, HP 10) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card.cardClass == CardClass.Tank &&
                    card.card.attack == 5 && card.card.shield == 10 && card.card.health == 10 &&
                    card.card.tier == CardTier.Tier4)
                {
                    CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                    if (effect != null)
                    {
                        effect.ActivateTankTier4Effect4Periodic();
                    }
                }
            }
        }
    }

    void ActivateHealerTier5Effect2Periodic()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Healer 5 tier-5 (ATK 6, HP 3) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card.cardClass == CardClass.Healer &&
                    card.card.attack == 6 && card.card.health == 3 &&
                    card.card.tier == CardTier.Tier5)
                {
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

        // Procura por Mage 5 tier-5 (ATK 8, HP 4) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card.cardClass == CardClass.Mago &&
                    card.card.attack == 8 && card.card.health == 4 &&
                    card.card.tier == CardTier.Tier5)
                {
                    // Verifica se já foi usado neste round
                    if (card.mageTier5Effect1LastUsedRound != currentRound)
                    {
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

    void ActivateMageTier5Effect3Periodic()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Mage 5 tier-5 (ATK 7, HP 5) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card.cardClass == CardClass.Mago &&
                    card.card.attack == 7 && card.card.health == 5 &&
                    card.card.tier == CardTier.Tier5)
                {
                    CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                    if (effect != null)
                    {
                        effect.ActivateMageBoostPerTurn();
                    }
                }
            }
        }
    }

    void ActivateTankTier5Effect2Periodic()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Tank 5 tier-5 (ATK 2, Shield 6, HP 8) em ambos os jogadores
        for (int playerNum = 1; playerNum <= 2; playerNum++)
        {
            var playerAllies = board.GetCardsByOwner(playerNum);
            if (playerAllies.Count == 0) continue;

            foreach (var card in playerAllies)
            {
                if (card != null && card.card.cardClass == CardClass.Tank &&
                    card.card.attack == 2 && card.card.shield == 6 && card.card.health == 8 &&
                    card.card.tier == CardTier.Tier5)
                {
                    // Verifica se já foi usado há 2 turnos atrás
                    if (currentRound - card.tankTier5Effect2LastArmorRound >= 2)
                    {
                        CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            effect.ActivatePeriodicShieldTier5Effect2();
                            card.tankTier5Effect2LastArmorRound = currentRound;
                        }
                    }
                }
            }
        }
    }
}
