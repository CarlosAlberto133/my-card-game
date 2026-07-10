using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Player 1 UI")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player1GoldText;
    public TextMeshProUGUI player1HealthText;

    [Header("Player 2 UI")]
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player2GoldText;
    public TextMeshProUGUI player2HealthText;

    [Header("Turn & Round Info")]
    public TextMeshProUGUI turnInfoText;
    public TextMeshProUGUI roundText;

    [Header("Botões")]
    public Button startGameButton;
    public Button endTurnButton;
    public Button resetStoreButton;
    public TextMeshProUGUI startGameButtonText;

    [Header("Tela de Vitória")]
    public GameObject victoryPanel;
    public TextMeshProUGUI victoryMessageText;
    public Button restartButton;

    [Header("Decision Popup")]
    public GameObject decisionPopupPanel;
    public TextMeshProUGUI decisionMessageText;
    public Button decisionYesButton;
    public TextMeshProUGUI decisionYesButtonText;
    public Button decisionNoButton;
    public TextMeshProUGUI decisionNoButtonText;

    private System.Action onDecisionYes;
    private System.Action onDecisionNo;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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

        if (resetStoreButton != null)
        {
            resetStoreButton.onClick.AddListener(OnResetStoreButtonClicked);
            Debug.Log($"ResetStoreButton encontrado e configurado. Ativo: {resetStoreButton.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogError("ResetStoreButton é NULL!");
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
        if (player1HealthText != null)
        {
            player1HealthText.text = $"Vida: {TurnManager.Instance.player1.health}/10";
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
        if (player2HealthText != null)
        {
            player2HealthText.text = $"Vida: {TurnManager.Instance.player2.health}/10";
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

        if (resetStoreButton != null)
        {
            // Botão "Reset Store" aparece sempre
            resetStoreButton.gameObject.SetActive(true);
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

        // Em multiplayer, marca ESTE jogador como pronto nos dois clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            PhotonGameManager.Instance.SendPlayerReadyRPC();
            return;
        }

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

    void OnResetStoreButtonClicked()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager.Instance é NULL!");
            return;
        }

        // Em multiplayer, valida o turno e sincroniza o reset nos dois clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            if (TurnManager.Instance.currentPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
            {
                Debug.Log("[GameUI] Não é seu turno, não pode resetar a loja!");
                return;
            }
            PhotonGameManager.Instance.SendResetStoreRPC();
            return;
        }

        TurnManager.Instance.TryResetStore();
    }

    void OnEndTurnButtonClicked()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RequestEndTurn();
        }
    }

    public void ShowVictoryScreen(int winnerPlayerNumber)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        if (victoryMessageText != null)
        {
            victoryMessageText.text = $"Parabéns, jogador {winnerPlayerNumber} venceu!";
        }

        if (restartButton != null && !restartButton.onClick.GetPersistentEventCount().Equals(0) == false)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }

        Debug.Log($"Tela de vitória mostrada para Jogador {winnerPlayerNumber}");
    }

    public void HideVictoryScreen()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    void OnRestartButtonClicked()
    {
        Debug.Log("Botão Restart foi clicado!");

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.RestartGame();
        }
    }

    public void ShowDecisionPopup(string message, string yesButtonText, System.Action onYes, string noButtonText, System.Action onNo)
    {
        if (decisionPopupPanel == null)
        {
            Debug.LogWarning("Decision Popup Panel não está configurado!");
            onYes?.Invoke();
            return;
        }

        // Armazena as callbacks
        onDecisionYes = onYes;
        onDecisionNo = onNo;

        // Atualiza mensagem
        if (decisionMessageText != null)
        {
            decisionMessageText.text = message;
        }

        // Atualiza textos dos botões
        if (decisionYesButtonText != null)
        {
            decisionYesButtonText.text = yesButtonText;
        }
        if (decisionNoButtonText != null)
        {
            decisionNoButtonText.text = noButtonText;
        }

        // Setup listeners
        if (decisionYesButton != null)
        {
            decisionYesButton.onClick.RemoveAllListeners();
            decisionYesButton.onClick.AddListener(OnDecisionYesClicked);
        }
        if (decisionNoButton != null)
        {
            decisionNoButton.onClick.RemoveAllListeners();
            decisionNoButton.onClick.AddListener(OnDecisionNoClicked);
        }

        // Mostra o popup
        decisionPopupPanel.SetActive(true);
        Debug.Log($"[DecisionPopup] Mostrando: {message}");
    }

    void OnDecisionYesClicked()
    {
        decisionPopupPanel.SetActive(false);
        onDecisionYes?.Invoke();
    }

    void OnDecisionNoClicked()
    {
        decisionPopupPanel.SetActive(false);
        onDecisionNo?.Invoke();
    }
}
