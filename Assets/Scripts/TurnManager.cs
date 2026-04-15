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

        Debug.Log($"Turno passou para {GetCurrentPlayer().playerName}");

        // Verificar se completa o round (quando J2 passa vez e volta para J1)
        if (previousPlayer == 2 && currentPlayerNumber == 1)
        {
            currentRound++;
            Debug.Log($"Round {currentRound} iniciado!");

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
}
