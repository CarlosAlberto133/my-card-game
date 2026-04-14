using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    [Header("Player 1 UI")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player1GoldText;

    [Header("Player 2 UI")]
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player2GoldText;

    [Header("Turn & Round Info")]
    public TextMeshProUGUI turnInfoText;
    public TextMeshProUGUI roundText;

    [Header("Botões")]
    public Button startGameButton;
    public Button endTurnButton;
    public TextMeshProUGUI startGameButtonText;

    void Start()
    {
        // Configurar botões
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            endTurnButton.gameObject.SetActive(false); // Escondido no início
        }
    }

    void Update()
    {
        if (TurnManager.Instance == null) return;

        // Atualiza UI do Jogador 1
        if (player1NameText != null)
        {
            player1NameText.text = TurnManager.Instance.player1.playerName;
        }
        if (player1GoldText != null)
        {
            player1GoldText.text = $"Ouro: {TurnManager.Instance.player1.gold}";
        }

        // Atualiza UI do Jogador 2
        if (player2NameText != null)
        {
            player2NameText.text = TurnManager.Instance.player2.playerName;
        }
        if (player2GoldText != null)
        {
            player2GoldText.text = $"Ouro: {TurnManager.Instance.player2.gold}";
        }

        // Atualiza informação de turno e round baseado no estado do jogo
        if (TurnManager.Instance.gameState == GameState.Lobby)
        {
            UpdateLobbyUI();
        }
        else
        {
            UpdateGameUI();
        }

        // Controlar visibilidade dos botões
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(TurnManager.Instance.gameState == GameState.Lobby);
        }
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(TurnManager.Instance.gameState == GameState.Playing);
        }
    }

    void UpdateLobbyUI()
    {
        if (turnInfoText != null)
        {
            PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            turnInfoText.text = $"Aguardando: {currentPlayer.playerName}\nClique em 'Iniciar Partida'";
        }

        if (roundText != null)
        {
            roundText.text = "LOBBY";
        }
    }

    void UpdateGameUI()
    {
        if (turnInfoText != null)
        {
            PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();
            turnInfoText.text = $"Turno: {currentPlayer.playerName}\nCartas: {currentPlayer.cardsBoughtThisTurn}/1";
        }

        if (roundText != null)
        {
            roundText.text = $"ROUND {TurnManager.Instance.currentRound}";
        }
    }

    void OnStartGameButtonClicked()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerReadyToStart(TurnManager.Instance.currentPlayerNumber);
        }
    }

    void OnEndTurnButtonClicked()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }
    }
}
