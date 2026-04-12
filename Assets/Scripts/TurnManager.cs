using UnityEngine;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public PlayerData player1;
    public PlayerData player2;

    public int currentPlayerNumber = 1; // Começa com jogador 1

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
        player1 = new PlayerData(1);
        player2 = new PlayerData(2);
    }

    void Update()
    {
        // Pressione Espaço para passar a vez (temporário para testes)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            EndTurn();
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

        // Passa para o próximo jogador
        currentPlayerNumber = currentPlayerNumber == 1 ? 2 : 1;

        Debug.Log($"Turno passou para {GetCurrentPlayer().playerName}");
    }

    public bool IsPlayerTurn(int playerNum)
    {
        return currentPlayerNumber == playerNum;
    }
}
