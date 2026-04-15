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
            Debug.Log($"StartGameButton encontrado e configurado. Ativo: {startGameButton.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogError("StartGameButton é NULL!");
        }

        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
            Debug.Log($"EndTurnButton encontrado e configurado. Ativo: {endTurnButton.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogError("EndTurnButton é NULL!");
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
            // Botão "Iniciar Partida" só no lobby
            bool shouldShow = TurnManager.Instance.gameState == GameState.Lobby;
            startGameButton.gameObject.SetActive(shouldShow);
        }

        if (endTurnButton != null)
        {
            // Botão "Passar a Vez" aparece sempre
            endTurnButton.gameObject.SetActive(true);
        }
    }

    void UpdateLobbyUI()
    {
        if (turnInfoText != null)
        {
            PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();

            // Conta quantos jogadores estão prontos
            int readyCount = 0;
            if (TurnManager.Instance.player1Ready) readyCount++;
            if (TurnManager.Instance.player2Ready) readyCount++;

            string readyMsg = readyCount == 0 ? "Clique 2x em 'Iniciar Partida'" : "Clique mais 1x para iniciar!";

            turnInfoText.text = $"Turno: {currentPlayer.playerName}\nCartas: {currentPlayer.cardsBoughtThisTurn}/1\n{readyMsg}";
        }

        if (roundText != null)
        {
            roundText.text = "FASE DE COMPRA";
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
        Debug.Log("Botão 'Iniciar Partida' foi clicado!");

        if (TurnManager.Instance != null)
        {
            Debug.Log($"Chamando OnPlayerReadyToStart para jogador {TurnManager.Instance.currentPlayerNumber}");
            TurnManager.Instance.OnPlayerReadyToStart(TurnManager.Instance.currentPlayerNumber);
        }
        else
        {
            Debug.LogError("TurnManager.Instance é NULL!");
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
