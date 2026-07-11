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

        CreateQuitButton();
        CreateShopButton();
    }

    private TextMeshProUGUI shopButtonText;

    // Cria o botão "Loja" via código (ao lado do botão Sair)
    void CreateShopButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject btnObj = new GameObject("ShopToggleButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-172f, -12f); // à esquerda do botão Sair
        rt.sizeDelta = new Vector2(130f, 40f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.35f, 0.55f, 0.92f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(OnShopButtonClicked);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        shopButtonText = txtObj.AddComponent<TextMeshProUGUI>();
        shopButtonText.text = "Loja";
        shopButtonText.fontSize = 20f;
        shopButtonText.alignment = TextAlignmentOptions.Center;
        shopButtonText.color = Color.white;
        shopButtonText.raycastTarget = false;
    }

    void OnShopButtonClicked()
    {
        // Cria o controlador da janela da loja na primeira vez
        if (ShopOverlayView.Instance == null)
        {
            new GameObject("ShopOverlayView").AddComponent<ShopOverlayView>();
        }

        ShopOverlayView.Instance.Toggle();

        if (shopButtonText != null)
        {
            shopButtonText.text = ShopOverlayView.Instance.IsOpen ? "Fechar Loja" : "Loja";
        }
    }

    // Cria o botão "Sair do Jogo" via código (canto superior direito)
    void CreateQuitButton()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameUIManager] Nenhum Canvas encontrado, botão Sair não criado");
            return;
        }

        GameObject btnObj = new GameObject("QuitGameButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12f, -12f);
        rt.sizeDelta = new Vector2(150f, 40f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.55f, 0.12f, 0.12f, 0.92f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(QuitGame);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Sair do Jogo";
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    public void QuitGame()
    {
        Debug.Log("[GameUIManager] Saindo do jogo...");

        // Desconecta do Photon antes de fechar (avisa o oponente)
        if (PhotonNetwork.connected)
        {
            PhotonNetwork.Disconnect();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
