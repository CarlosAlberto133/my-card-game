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
    private bool player1Ready = false;
    private bool player2Ready = false;

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

        // TEMPORÁRIO: Inicia o jogo automaticamente para testes
        gameState = GameState.Playing;
        currentRound = 1;
        Debug.Log("Jogo iniciado automaticamente (modo teste)");
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
        if (gameState != GameState.Lobby) return;

        if (playerNumber == 1)
        {
            player1Ready = true;
            Debug.Log("Jogador 1 está pronto!");
        }
        else if (playerNumber == 2)
        {
            player2Ready = true;
            Debug.Log("Jogador 2 está pronto!");
        }

        // Se ambos prontos, iniciar jogo
        if (player1Ready && player2Ready)
        {
            StartGame();
        }
        else
        {
            // Passar para o próximo jogador aguardar
            currentPlayerNumber = (playerNumber == 1) ? 2 : 1;
        }
    }

    private void StartGame()
    {
        Debug.Log("Iniciando partida!");
        gameState = GameState.Playing;
        currentRound = 1;
        currentPlayerNumber = 1;

        // Resetar dados dos jogadores
        player1.gold = 10;
        player1.cardsBoughtThisTurn = 0;
        player2.gold = 10;
        player2.cardsBoughtThisTurn = 0;

        // Notificar CardManager
        if (CardManager.Instance != null)
        {
            CardManager.Instance.OnGameStart();
        }
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
        // Reseta o turno do jogador atual
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
