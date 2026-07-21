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

    // Overlay de vitória construído por código (o painel serializado da cena
    // renderizava quebrado). Criado sob demanda em ShowVictoryScreen.
    private GameObject victoryOverlay;
    private TextMeshProUGUI victoryOverlayTitle;
    private TextMeshProUGUI victoryOverlaySubtitle;
    private TextMeshProUGUI victoryOverlayStats;

    // Popup de decisão construído por código (padroniza com o modal de
    // configurações — o serializado da cena era um retângulo cru)
    private GameObject decisionOverlay;
    private TextMeshProUGUI decisionOverlayMsg;
    private TextMeshProUGUI decisionYesLabelCode;
    private TextMeshProUGUI decisionNoLabelCode;

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

        // "Sair do Jogo" e "Sair da partida" agora ficam DENTRO do popup da
        // engrenagem (MusicManager) — não há mais botões soltos no canto
        CreateShopButton();
        CreateLogsButton();
        CreateShopLockToggle();
        StyleGameButtons();
        StyleTopHud();
    }

    // Repagina os botões da partida com o visual das tabuletas do lobby:
    // sprite arredondado, moldura dourada, cor por função e realce no hover.
    void StyleGameButtons()
    {
        Color green = new Color(0.20f, 0.52f, 0.32f);
        Color gold = new Color(0.90f, 0.70f, 0.24f);
        Color bronze = new Color(0.46f, 0.33f, 0.15f);
        Color slate = new Color(0.18f, 0.23f, 0.35f);
        Color blue = new Color(0.13f, 0.37f, 0.55f);
        Color darkTxt = new Color(0.12f, 0.09f, 0.02f);
        Color lightTxt = new Color(0.96f, 0.93f, 0.86f);

        StyleButton(startGameButton, green, lightTxt);        // Iniciar Partida
        StyleButton(endTurnButton, gold, darkTxt);            // Passar a Vez (ação principal)
        StyleButton(resetStoreButton, bronze, lightTxt);      // Reset Store

        if (resetStoreButton != null)
        {
            Transform logs = resetStoreButton.transform.parent.Find("LogsButton");
            if (logs != null) StyleButton(logs.GetComponent<Button>(), slate, lightTxt);
        }
        if (endTurnButton != null)
        {
            Transform shop = endTurnButton.transform.parent.Find("ShopToggleButton");
            if (shop != null) StyleButton(shop.GetComponent<Button>(), blue, lightTxt);
        }
    }

    void StyleButton(Button b, Color fill, Color textColor)
    {
        if (b == null) return;

        RectTransform rt = b.GetComponent<RectTransform>();
        Vector2 size = rt != null ? rt.rect.size : Vector2.zero;
        if (size.x < 1f || size.y < 1f) size = rt != null ? rt.sizeDelta : new Vector2(160f, 40f);
        // Escala da borda do 9-slice conforme o tamanho: em botões BAIXOS as
        // bordas de 20px se sobrepunham e distorciam o sprite (o "bugado")
        float ppu = Mathf.Clamp(60f / Mathf.Max(1f, Mathf.Min(size.x, size.y)), 1f, 3f);

        Image img = b.GetComponent<Image>();
        if (img != null)
        {
            LobbySprites.MakeRounded(img, fill);
            img.pixelsPerUnitMultiplier = ppu;

            if (b.transform.Find("Ring") == null)
            {
                GameObject ring = LobbySprites.AddRing(b.transform, new Color(0.96f, 0.77f, 0.32f, 0.45f));
                Image ringImg = ring != null ? ring.GetComponent<Image>() : null;
                if (ringImg != null) ringImg.pixelsPerUnitMultiplier = ppu;
            }

            b.targetGraphic = img;
            b.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = b.colors;
            cb.normalColor = fill;
            cb.highlightedColor = Color.Lerp(fill, Color.white, 0.18f);
            cb.pressedColor = Color.Lerp(fill, Color.black, 0.20f);
            cb.selectedColor = fill;
            cb.disabledColor = new Color(fill.r, fill.g, fill.b, 0.40f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
        }

        TMP_Text tmp = b.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
        {
            tmp.color = textColor;
            tmp.fontStyle |= FontStyles.Bold;
            // Auto-ajuste: se o rótulo não couber (ex.: "Reset Store"), encolhe
            // até caber em vez de ficar espremido nas bordas
            RectTransform ttr = tmp.rectTransform;
            if (ttr.anchorMin != ttr.anchorMax) // só se o texto preenche o botão
            {
                ttr.offsetMin = new Vector2(8f, ttr.offsetMin.y);
                ttr.offsetMax = new Vector2(-8f, ttr.offsetMax.y);
            }
            float baseSize = tmp.fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = baseSize;
            tmp.fontSizeMin = Mathf.Max(9f, baseSize * 0.6f);
        }
        else
        {
            Text legacy = b.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.color = textColor;
                legacy.fontStyle = FontStyle.Bold;
                legacy.resizeTextForBestFit = true;
                legacy.resizeTextMinSize = 9;
            }
        }
    }

    // Visual de "placa": 3 cards transparentes atrás das infos do topo —
    // Jogador 1 (esquerda), Turno/Round (centro) e Jogador 2 (direita) — além
    // de cor por tipo (nome dourado, ouro âmbar, vida verde). Cada card cobre a
    // ÁREA do seu grupo (calculada pelas posições reais dos textos na cena).
    void StyleTopHud()
    {
        Color name = new Color(0.96f, 0.80f, 0.40f);
        Color goldC = new Color(1f, 0.86f, 0.42f);
        Color life = new Color(0.55f, 0.90f, 0.62f);
        Color info = new Color(0.90f, 0.93f, 0.98f);

        ColorText(player1NameText, name, true);
        ColorText(player1GoldText, goldC, false);
        ColorText(player1HealthText, life, false);
        ColorText(player2NameText, name, true);
        ColorText(player2GoldText, goldC, false);
        ColorText(player2HealthText, life, false);
        ColorText(turnInfoText, info, true);
        ColorText(roundText, new Color(0.96f, 0.77f, 0.32f), true);

        // Os cards são desenhados a partir da ÁREA dos textos — que só existe
        // depois que a UI é preenchida. Espera 2 frames antes de medir.
        StartCoroutine(BuildHudCardsDeferred());
    }

    System.Collections.IEnumerator BuildHudCardsDeferred()
    {
        yield return null;
        yield return null;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;

        // Remove cards antigos (caso o layout mude e a gente reconstrua)
        foreach (string n in new[] { "HudCardLeft", "HudCardMid", "HudCardRight" })
        {
            Transform old = canvas.transform.Find(n);
            if (old != null) Destroy(old.gameObject);
        }

        // Laterais cobrem a CAIXA dos textos; o do meio se ajusta ao TEXTO real
        // (tight) — a caixa dele na cena é maior que o conteúdo
        AddGroupCard(canvas, "HudCardLeft", new Vector2(26f, 16f), false,
            player1NameText, player1GoldText, player1HealthText);
        AddGroupCard(canvas, "HudCardMid", new Vector2(18f, 12f), true,
            turnInfoText, roundText);
        AddGroupCard(canvas, "HudCardRight", new Vector2(26f, 16f), false,
            player2NameText, player2GoldText, player2HealthText);
    }

    void ColorText(TextMeshProUGUI tmp, Color color, bool bold)
    {
        if (tmp == null) return;
        tmp.color = color;
        if (bold) tmp.fontStyle |= FontStyles.Bold;
    }

    // Um card transparente cobrindo o grupo de textos. tight=false usa a CAIXA
    // (RectTransform) de cada texto; tight=true usa os limites RENDERIZADOS
    // (textBounds) — bem mais justo, para o card do meio abraçar só o texto.
    void AddGroupCard(Canvas canvas, string cardName, Vector2 pad, bool tight,
        params TextMeshProUGUI[] members)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue);
        bool any = false;

        foreach (var m in members)
        {
            if (m == null) continue;
            Vector3[] worldCorners = tight ? RenderedCorners(m) : BoxCorners(m);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                min = Vector3.Min(min, worldCorners[i]);
                max = Vector3.Max(max, worldCorners[i]);
            }
            any = true;
        }
        if (!any) return;

        GameObject card = new GameObject(cardName, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(canvas.transform, false);
        card.transform.SetAsFirstSibling(); // atrás dos textos do HUD

        RectTransform crt = card.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        float sf = canvas.scaleFactor > 0.001f ? canvas.scaleFactor : 1f;
        crt.position = (min + max) * 0.5f;
        crt.sizeDelta = new Vector2((max.x - min.x) / sf, (max.y - min.y) / sf) + pad * 2f;

        Image img = card.GetComponent<Image>();
        LobbySprites.MakeRounded(img, new Color(0.07f, 0.055f, 0.04f, 0.42f)); // transparente
        img.raycastTarget = false;
    }

    static Vector3[] BoxCorners(TextMeshProUGUI m)
    {
        Vector3[] c = new Vector3[4];
        m.rectTransform.GetWorldCorners(c);
        return c;
    }

    // 4 cantos do TEXTO renderizado (não da caixa) em espaço de mundo
    static Vector3[] RenderedCorners(TextMeshProUGUI m)
    {
        m.ForceMeshUpdate();
        Bounds b = m.textBounds;
        // Se o texto ainda está vazio, cai para a caixa
        if (b.size.x < 1f || b.size.y < 1f) return BoxCorners(m);

        Vector3 c = b.center, e = b.extents;
        RectTransform rt = m.rectTransform;
        return new[]
        {
            rt.TransformPoint(new Vector3(c.x - e.x, c.y - e.y, 0f)),
            rt.TransformPoint(new Vector3(c.x + e.x, c.y - e.y, 0f)),
            rt.TransformPoint(new Vector3(c.x - e.x, c.y + e.y, 0f)),
            rt.TransformPoint(new Vector3(c.x + e.x, c.y + e.y, 0f)),
        };
    }

    // ── Checkbox "Travar cartas" (acima do botão Passar a Vez) ───────────
    // Marcado: vira o MODO de seleção — clicar numa carta da SUA loja trava/
    // destrava ela (aura dourada) em vez de comprar. Cartas travadas não
    // renovam no refresh. O modo em si é local; cada trava vai por RPC.
    private GameObject shopLockObj;
    private UnityEngine.UI.Toggle shopLockToggle;
    private TextMeshProUGUI shopLockLabel;
    private bool shopLockMode = false;

    // CardDisplay consulta no clique: true = clique em carta da loja trava
    public bool IsShopLockMode()
    {
        return shopLockMode && shopLockObj != null && shopLockObj.activeInHierarchy;
    }

    void CreateShopLockToggle()
    {
        if (endTurnButton == null) return;
        RectTransform btnRt = endTurnButton.GetComponent<RectTransform>();
        if (btnRt == null) return;

        shopLockObj = new GameObject("ShopLockToggle",
            typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.Toggle));
        shopLockObj.transform.SetParent(btnRt.parent, false);

        RectTransform rt = shopLockObj.GetComponent<RectTransform>();
        rt.anchorMin = btnRt.anchorMin;
        rt.anchorMax = btnRt.anchorMax;
        rt.pivot = btnRt.pivot;
        float btnH = btnRt.sizeDelta.y > 1f ? btnRt.sizeDelta.y : 40f;
        float btnW = btnRt.sizeDelta.x > 1f ? btnRt.sizeDelta.x : 160f;
        rt.sizeDelta = new Vector2(Mathf.Min(btnW, 220f), 32f);
        // Logo acima do botão (metades das alturas + folga)
        rt.anchoredPosition = btnRt.anchoredPosition + new Vector2(0f, (btnH + 32f) * 0.5f + 6f);

        Image bg = shopLockObj.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.20f, 0.85f);

        // Caixinha + marca
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(shopLockObj.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0f, 0.5f);
        boxRt.anchorMax = new Vector2(0f, 0.5f);
        boxRt.pivot = new Vector2(0f, 0.5f);
        boxRt.anchoredPosition = new Vector2(6f, 0f);
        boxRt.sizeDelta = new Vector2(22f, 22f);
        box.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.34f, 1f);

        GameObject check = new GameObject("Check", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(box.transform, false);
        RectTransform chkRt = check.GetComponent<RectTransform>();
        chkRt.anchorMin = new Vector2(0.18f, 0.18f);
        chkRt.anchorMax = new Vector2(0.82f, 0.82f);
        chkRt.offsetMin = Vector2.zero;
        chkRt.offsetMax = Vector2.zero;
        check.GetComponent<Image>().color = new Color(0.96f, 0.77f, 0.32f); // dourado

        // Rótulo
        GameObject lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(shopLockObj.transform, false);
        RectTransform lblRt = lbl.GetComponent<RectTransform>();
        lblRt.anchorMin = new Vector2(0f, 0f);
        lblRt.anchorMax = new Vector2(1f, 1f);
        lblRt.offsetMin = new Vector2(34f, 0f);
        lblRt.offsetMax = new Vector2(-4f, 0f);
        TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = "Travar cartas";
        tmp.fontSize = 16f;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        shopLockLabel = tmp;

        shopLockToggle = shopLockObj.GetComponent<UnityEngine.UI.Toggle>();
        shopLockToggle.targetGraphic = bg;
        shopLockToggle.graphic = check.GetComponent<Image>();
        shopLockToggle.isOn = false;
        shopLockToggle.onValueChanged.AddListener(OnShopLockToggled);

        shopLockObj.SetActive(false); // só aparece com a partida rolando
    }

    void OnShopLockToggled(bool on)
    {
        // Modo local de seleção: com ele ligado, o clique nas cartas da loja
        // trava/destrava (a trava em si é sincronizada por RPC no CardDisplay).
        // As travas já feitas CONTINUAM valendo com o modo desligado — ele só
        // muda o que o clique faz
        shopLockMode = on;
        if (shopLockLabel != null)
        {
            shopLockLabel.text = on ? "Clique p/ travar" : "Travar cartas";
            shopLockLabel.color = on ? new Color(0.96f, 0.77f, 0.32f) : Color.white;
        }
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

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        // Empilha ACIMA do "Travar loja" (que fica acima do Passar a Vez):
        //   Loja  /  Travar loja  /  Passar a Vez
        RectTransform endRt = endTurnButton != null ? endTurnButton.GetComponent<RectTransform>() : null;
        if (endRt != null)
        {
            btnObj.transform.SetParent(endRt.parent, false);
            rt.anchorMin = endRt.anchorMin;
            rt.anchorMax = endRt.anchorMax;
            rt.pivot = endRt.pivot;
            float btnH = endRt.sizeDelta.y > 1f ? endRt.sizeDelta.y : 40f;
            float btnW = endRt.sizeDelta.x > 1f ? endRt.sizeDelta.x : 160f;
            float lockOffset = (btnH + 32f) * 0.5f + 6f;          // centro do "Travar loja"
            float shopH = 44f;
            float shopOffset = lockOffset + (32f + shopH) * 0.5f + 6f; // acima dele
            rt.sizeDelta = new Vector2(Mathf.Min(btnW, 220f), shopH);
            rt.anchoredPosition = endRt.anchoredPosition + new Vector2(0f, shopOffset);
        }
        else
        {
            // Fallback: canto superior direito (comportamento antigo)
            btnObj.transform.SetParent(canvas.transform, false);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-172f, -12f);
            rt.sizeDelta = new Vector2(130f, 40f);
        }

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
    // ===== BOTÃO E PAINEL DE LOGS =====
    // Raio-X para o jogador: o que pode agir, status ativos, contadores de
    // efeito e o que falta para tríades/combos ativarem. 100% local (só leitura
    // de estado), nenhum impacto na sincronização.
    private GameObject logsPanel;
    private TMPro.TextMeshProUGUI logsText;
    private float lastLogsRefresh;

    // Clona o botão Reset Store para manter o mesmo visual, ao lado dele
    void CreateLogsButton()
    {
        if (resetStoreButton == null) return;

        GameObject clone = Instantiate(resetStoreButton.gameObject, resetStoreButton.transform.parent);
        clone.name = "LogsButton";

        RectTransform sourceRt = resetStoreButton.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition +
            new Vector2(sourceRt.sizeDelta.x + 10f, 0f); // Ao lado (à direita — à esquerda ficava sobre o Passar a Vez)

        TextMeshProUGUI tmpLabel = clone.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpLabel != null) tmpLabel.text = "Logs";
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Logs";
        }

        Button logsButton = clone.GetComponent<Button>();
        logsButton.interactable = true;
        logsButton.onClick.RemoveAllListeners();
        logsButton.onClick.AddListener(ToggleLogsPanel);
    }

    void ToggleLogsPanel()
    {
        if (logsPanel != null && logsPanel.activeSelf)
        {
            logsPanel.SetActive(false);
            return;
        }

        if (logsPanel == null) BuildLogsPanel();
        if (logsPanel == null) return;

        RefreshLogsPanel();
        logsPanel.SetActive(true);
        logsPanel.transform.SetAsLastSibling();
    }

    void BuildLogsPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        logsPanel = new GameObject("LogsPanel", typeof(RectTransform), typeof(Image));
        logsPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = logsPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(640f, 680f);
        logsPanel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.13f, 0.96f);

        // Título
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(logsPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -10f);
        titleRt.sizeDelta = new Vector2(0f, 36f);
        TMPro.TextMeshProUGUI title = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        title.text = "LOGS DA PARTIDA";
        title.fontSize = 26f;
        title.fontStyle = TMPro.FontStyles.Bold;
        title.alignment = TMPro.TextAlignmentOptions.Center;
        title.color = new Color(0.96f, 0.77f, 0.32f);

        // Botão fechar (X)
        GameObject closeObj = new GameObject("CloseButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(logsPanel.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-8f, -8f);
        closeRt.sizeDelta = new Vector2(34f, 34f);
        closeObj.GetComponent<Image>().color = new Color(0.75f, 0.22f, 0.20f, 0.95f);
        closeObj.GetComponent<Button>().onClick.AddListener(() => logsPanel.SetActive(false));

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = Vector2.zero;
        closeTxtRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TMPro.TextMeshProUGUI>();
        closeTxt.text = "X";
        closeTxt.fontSize = 20f;
        closeTxt.fontStyle = TMPro.FontStyles.Bold;
        closeTxt.alignment = TMPro.TextAlignmentOptions.Center;
        closeTxt.color = Color.white;

        // Viewport com máscara (área de rolagem)
        GameObject viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(logsPanel.transform, false);
        RectTransform viewRt = viewport.GetComponent<RectTransform>();
        viewRt.anchorMin = new Vector2(0f, 0f);
        viewRt.anchorMax = new Vector2(1f, 1f);
        viewRt.offsetMin = new Vector2(14f, 58f); // deixa espaço para o botão Exportar
        viewRt.offsetMax = new Vector2(-14f, -52f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

        // Conteúdo: texto com altura automática
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.offsetMin = new Vector2(4f, contentRt.offsetMin.y);
        contentRt.offsetMax = new Vector2(-4f, contentRt.offsetMax.y);

        logsText = content.AddComponent<TMPro.TextMeshProUGUI>();
        logsText.fontSize = 19f;
        logsText.alignment = TMPro.TextAlignmentOptions.TopLeft;
        logsText.richText = true;
        logsText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Rolagem com o scroll do mouse / arrasto
        ScrollRect scroll = logsPanel.AddComponent<ScrollRect>();
        scroll.viewport = viewRt;
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 25f;

        // Botão "Exportar": salva o histórico COMPLETO da partida (todos os
        // eventos e erros desde a abertura do jogo) em um .txt e abre a pasta
        GameObject exportObj = new GameObject("ExportButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        exportObj.transform.SetParent(logsPanel.transform, false);
        RectTransform exportRt = exportObj.GetComponent<RectTransform>();
        exportRt.anchorMin = new Vector2(0.5f, 0f);
        exportRt.anchorMax = new Vector2(0.5f, 0f);
        exportRt.pivot = new Vector2(0.5f, 0f);
        exportRt.anchoredPosition = new Vector2(0f, 10f);
        exportRt.sizeDelta = new Vector2(300f, 38f);
        exportObj.GetComponent<Image>().color = new Color(0.16f, 0.42f, 0.24f, 0.95f);

        GameObject exportTxtObj = new GameObject("Text", typeof(RectTransform));
        exportTxtObj.transform.SetParent(exportObj.transform, false);
        RectTransform exportTxtRt = exportTxtObj.GetComponent<RectTransform>();
        exportTxtRt.anchorMin = Vector2.zero;
        exportTxtRt.anchorMax = Vector2.one;
        exportTxtRt.offsetMin = Vector2.zero;
        exportTxtRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI exportTxt = exportTxtObj.AddComponent<TMPro.TextMeshProUGUI>();
        exportTxt.text = "Exportar todos os logs (.txt)";
        exportTxt.fontSize = 18f;
        exportTxt.fontStyle = TMPro.FontStyles.Bold;
        exportTxt.alignment = TMPro.TextAlignmentOptions.Center;
        exportTxt.color = Color.white;

        exportObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            string path = MatchLogRecorder.ExportToFile(BuildLogsReport());
            if (path != null)
            {
                exportTxt.text = "Salvo! Abrindo a pasta...";
                MatchLogRecorder.RevealInExplorer(path);
            }
            else
            {
                exportTxt.text = "Falha ao salvar :(";
            }
        });

        logsPanel.SetActive(false);
    }

    void RefreshLogsPanel()
    {
        lastLogsRefresh = Time.unscaledTime;
        if (logsText != null)
        {
            logsText.text = BuildLogsReport();
        }
    }

    // ===== Inspeção de carta (botão direito): efeito, foto e stats ampliados =====

    private GameObject cardPreviewPanel;
    private TMPro.TextMeshProUGUI previewName;
    private TMPro.TextMeshProUGUI previewClassTier;
    private TMPro.TextMeshProUGUI previewEffect;
    private TMPro.TextMeshProUGUI previewAtk;
    private TMPro.TextMeshProUGUI previewDef;
    private TMPro.TextMeshProUGUI previewHp;
    private Image previewArt;
    private Image previewHeader;
    private int previewOpenedFrame = -1;

    public void ShowCardPreview(CardDisplay cd)
    {
        if (cd == null || cd.card == null) return;
        if (cardPreviewPanel == null) BuildCardPreviewPanel();
        if (cardPreviewPanel == null) return;

        Card c = cd.card;
        previewName.text = c.cardName;
        previewClassTier.text = $"{c.cardClass}  •  Tier {(int)c.tier}";
        previewClassTier.color = PreviewClassColor(c.cardClass);
        previewHeader.color = Color.Lerp(new Color(0.10f, 0.10f, 0.16f),
                                         PreviewClassColor(c.cardClass), 0.45f);
        previewEffect.text = string.IsNullOrEmpty(c.effectDescription)
            ? "Sem efeito" : c.effectDescription;
        previewAtk.text = cd.currentAttack.ToString();
        previewDef.text = cd.currentShield.ToString();
        previewHp.text = cd.currentHealth.ToString();

        if (c.artwork != null)
        {
            previewArt.sprite = c.artwork;
            previewArt.color = Color.white;
            previewArt.preserveAspect = true;
        }
        else
        {
            previewArt.sprite = null;
            previewArt.color = new Color(0.25f, 0.25f, 0.30f);
        }

        previewOpenedFrame = Time.frameCount;
        cardPreviewPanel.SetActive(true);
        cardPreviewPanel.transform.SetAsLastSibling();
    }

    public void HideCardPreview()
    {
        if (cardPreviewPanel != null) cardPreviewPanel.SetActive(false);
    }

    Color PreviewClassColor(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank: return new Color(0.70f, 0.70f, 0.70f);
            case CardClass.Mago: return new Color(0.60f, 0.42f, 0.95f);
            case CardClass.Healer: return new Color(0.35f, 0.90f, 0.55f);
            case CardClass.Arqueiro: return new Color(0.95f, 0.65f, 0.35f);
            default: return Color.white;
        }
    }

    void BuildCardPreviewPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Painel encostado no lado direito da tela
        cardPreviewPanel = new GameObject("CardPreviewPanel", typeof(RectTransform), typeof(Image));
        cardPreviewPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = cardPreviewPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 0.5f);
        panelRt.anchorMax = new Vector2(1f, 0.5f);
        panelRt.pivot = new Vector2(1f, 0.5f);
        panelRt.anchoredPosition = new Vector2(-16f, 0f);
        panelRt.sizeDelta = new Vector2(400f, 660f);
        cardPreviewPanel.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.13f, 0.97f);

        // Faixa do cabeçalho (tingida pela classe no ShowCardPreview)
        GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(cardPreviewPanel.transform, false);
        RectTransform headerRt = headerObj.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = Vector2.zero;
        headerRt.sizeDelta = new Vector2(0f, 78f);
        previewHeader = headerObj.GetComponent<Image>();

        previewName = MakePreviewText(cardPreviewPanel.transform, "Name",
            new Vector2(0f, -8f), new Vector2(-70f, 34f), 26f, true, TMPro.TextAlignmentOptions.Center);
        previewName.color = Color.white;

        previewClassTier = MakePreviewText(cardPreviewPanel.transform, "ClassTier",
            new Vector2(0f, -44f), new Vector2(-20f, 26f), 18f, true, TMPro.TextAlignmentOptions.Center);

        // Botão fechar (X)
        GameObject closeObj = new GameObject("CloseButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(cardPreviewPanel.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-8f, -8f);
        closeRt.sizeDelta = new Vector2(34f, 34f);
        closeObj.GetComponent<Image>().color = new Color(0.75f, 0.22f, 0.20f, 0.95f);
        closeObj.GetComponent<Button>().onClick.AddListener(HideCardPreview);

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = Vector2.zero;
        closeTxtRt.offsetMax = Vector2.zero;
        TMPro.TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TMPro.TextMeshProUGUI>();
        closeTxt.text = "X";
        closeTxt.fontSize = 20f;
        closeTxt.fontStyle = TMPro.FontStyles.Bold;
        closeTxt.alignment = TMPro.TextAlignmentOptions.Center;
        closeTxt.color = Color.white;

        // Foto da carta
        GameObject artObj = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
        artObj.transform.SetParent(cardPreviewPanel.transform, false);
        RectTransform artRt = artObj.GetComponent<RectTransform>();
        artRt.anchorMin = new Vector2(0.5f, 1f);
        artRt.anchorMax = new Vector2(0.5f, 1f);
        artRt.pivot = new Vector2(0.5f, 1f);
        artRt.anchoredPosition = new Vector2(0f, -90f);
        artRt.sizeDelta = new Vector2(360f, 240f);
        previewArt = artObj.GetComponent<Image>();

        // Rótulo e texto do efeito
        TMPro.TextMeshProUGUI effectLabel = MakePreviewText(cardPreviewPanel.transform, "EffectLabel",
            new Vector2(0f, -340f), new Vector2(-40f, 24f), 16f, true, TMPro.TextAlignmentOptions.Left);
        effectLabel.text = "EFEITO";
        effectLabel.color = new Color(0.96f, 0.77f, 0.32f);

        previewEffect = MakePreviewText(cardPreviewPanel.transform, "EffectText",
            new Vector2(0f, -366f), new Vector2(-40f, 180f), 19f, false, TMPro.TextAlignmentOptions.TopLeft);
        previewEffect.color = new Color(0.92f, 0.92f, 0.88f);
        previewEffect.textWrappingMode = TMPro.TextWrappingModes.Normal;
        previewEffect.enableAutoSizing = true;
        previewEffect.fontSizeMin = 13f;
        previewEffect.fontSizeMax = 20f;

        // Stats: três blocos coloridos na base
        previewAtk = MakePreviewStatBlock("ATAQUE", new Color(0.58f, 0.20f, 0.12f), 0);
        previewDef = MakePreviewStatBlock("DEFESA", new Color(0.13f, 0.30f, 0.55f), 1);
        previewHp = MakePreviewStatBlock("VIDA", new Color(0.12f, 0.40f, 0.19f), 2);

        cardPreviewPanel.SetActive(false);
    }

    TMPro.TextMeshProUGUI MakePreviewText(Transform parent, string name, Vector2 pos,
        Vector2 size, float fontSize, bool bold, TMPro.TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TMPro.TextMeshProUGUI tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = bold ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
        tmp.alignment = align;
        return tmp;
    }

    TMPro.TextMeshProUGUI MakePreviewStatBlock(string label, Color color, int column)
    {
        GameObject block = new GameObject("Stat_" + label, typeof(RectTransform), typeof(Image));
        block.transform.SetParent(cardPreviewPanel.transform, false);
        RectTransform rt = block.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2((column - 1) * 124f, 16f);
        rt.sizeDelta = new Vector2(112f, 74f);
        block.GetComponent<Image>().color = color;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(block.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0.5f, 1f);
        labelRt.anchoredPosition = new Vector2(0f, -6f);
        labelRt.sizeDelta = new Vector2(0f, 18f);
        TMPro.TextMeshProUGUI labelTmp = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 13f;
        labelTmp.fontStyle = TMPro.FontStyles.Bold;
        labelTmp.alignment = TMPro.TextAlignmentOptions.Center;
        labelTmp.color = new Color(1f, 1f, 1f, 0.85f);

        GameObject valueObj = new GameObject("Value", typeof(RectTransform));
        valueObj.transform.SetParent(block.transform, false);
        RectTransform valueRt = valueObj.GetComponent<RectTransform>();
        valueRt.anchorMin = Vector2.zero;
        valueRt.anchorMax = Vector2.one;
        valueRt.offsetMin = new Vector2(0f, 0f);
        valueRt.offsetMax = new Vector2(0f, -18f);
        TMPro.TextMeshProUGUI valueTmp = valueObj.AddComponent<TMPro.TextMeshProUGUI>();
        valueTmp.fontSize = 34f;
        valueTmp.fontStyle = TMPro.FontStyles.Bold;
        valueTmp.alignment = TMPro.TextAlignmentOptions.Center;
        valueTmp.color = Color.white;
        return valueTmp;
    }

    // ===== GERADOR DO RELATÓRIO =====
    const string H = "#F5C451";   // dourado (títulos)
    const string OK = "#55E07A";  // verde (pode agir)
    const string NO = "#FF6B5E";  // vermelho (bloqueado)
    const string TU = "#FFD84D";  // amarelo (turnos)
    const string RO = "#FF73C0";  // rosa (rounds)

    string BuildLogsReport()
    {
        TurnManager tm = TurnManager.Instance;
        if (tm == null) return "Sem partida em andamento.";

        int localPlayer = (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
            ? PhotonGameManager.Instance.myPlayerNumber
            : tm.currentPlayerNumber;
        if (localPlayer == 0) localPlayer = 1;

        PlayerData me = tm.GetPlayer(localPlayer);
        bool lobbyPhase = tm.gameState == GameState.Lobby;
        var sb = new System.Text.StringBuilder(2048);

        // ---- Status geral ----
        sb.AppendLine($"<color={H}><b>== SEU STATUS ==</b></color>");
        sb.AppendLine($"Ouro: {me.gold}{(lobbyPhase ? " (teto 20 nesta fase)" : "")}   Vida da torre: {me.health}");

        if (lobbyPhase)
        {
            sb.AppendLine($"Fase inicial: compras {me.cardsBoughtThisTurn}/{PlayerData.MaxCardsInLobby} — " +
                          $"resets da loja {me.storeResetsThisTurn}/{TurnManager.LobbyMaxStoreResets}");
        }
        else
        {
            bool myTurn = tm.currentPlayerNumber == localPlayer;
            sb.AppendLine(myTurn
                ? $"<color={OK}>É o SEU turno</color> — compras {me.cardsBoughtThisTurn}/{PlayerData.MaxCardsPerTurn}, " +
                  $"reset da loja {(me.CanResetStore() ? "disponível (2 ouro)" : "já usado")}"
                : $"<color={NO}>Turno do oponente</color> — aguarde para agir");
        }

        HandManager myHand = FindHandManagerFor(localPlayer);
        if (myHand != null)
            sb.AppendLine($"Cartas na mão: {myHand.GetCardCount()}/{myHand.maxCardsInHand}");
        if (me.freePurchases > 0)
            sb.AppendLine($"<color={OK}>Compra GRÁTIS pendente: {me.freePurchases} (não gasta ouro nem limite)</color>");
        sb.AppendLine();

        // ---- Chances de tier na loja (TierOdds) ----
        string oddsTitle = lobbyPhase
            ? "== CHANCES DA LOJA (FASE INICIAL) =="
            : $"== CHANCES DA LOJA (ROUND {tm.currentRound}) ==";
        sb.AppendLine($"<color={H}><b>{oddsTitle}</b></color>");
        var oddsLine = new System.Text.StringBuilder(96);
        for (int t = 1; t <= 5; t++)
        {
            int chance = TierOdds.GetChance((CardTier)t, lobbyPhase, tm.currentRound);
            oddsLine.Append($"T{t}: {chance}%");
            if (t < 5) oddsLine.Append("   ");
        }
        sb.AppendLine(oddsLine.ToString());
        if (!lobbyPhase)
            sb.AppendLine("<i>Tiers altos ficam mais comuns a cada round (estabiliza no round 10).</i>");
        sb.AppendLine();

        // ---- Cartas em campo ----
        BoardManager board = BoardManager.Instance;
        var myCards = board != null
            ? board.GetCardsByOwner(localPlayer)
            : new System.Collections.Generic.List<CardDisplay>();

        sb.AppendLine($"<color={H}><b>== SUAS CARTAS EM CAMPO ({myCards.Count}) ==</b></color>");
        if (myCards.Count == 0) sb.AppendLine("Nenhuma carta no tabuleiro ainda.");

        int totalRows = board != null ? board.rows : 10;
        foreach (var c in myCards)
        {
            if (c == null || c.card == null) continue;
            AppendCardLine(sb, c, totalRows, lobbyPhase);
        }
        sb.AppendLine();

        // ---- Efeitos para ativar ----
        sb.AppendLine($"<color={H}><b>== EFEITOS PARA ATIVAR ==</b></color>");
        AppendTriadStatus(sb, myCards);
        AppendComboWatch(sb, myCards, localPlayer);

        return sb.ToString();
    }

    void AppendCardLine(System.Text.StringBuilder sb, CardDisplay c,
        int totalRows, bool lobbyPhase)
    {
        string pos = c.currentTile != null ? $"({c.currentTile.row},{c.currentTile.column})" : "(?)";
        sb.AppendLine($"<b>• {c.card.cardName}</b> {pos}  ATK {c.currentAttack} / DEF {c.currentShield} / HP {c.currentHealth}");

        // Status ativos (com duração)
        if (c.isFrozen)
            sb.AppendLine($"   <color={TU}>CONGELADA — descongela em {c.freezeTurnsLeft} turno(s) dela</color>");
        if (c.isStunned)
            sb.AppendLine($"   <color={TU}>ATORDOADA — libera em {c.stunTurnsLeft} turno(s) dela</color>");
        if (c.eagleMarked)
            sb.AppendLine($"   <color={TU}>MARCADA pela águia — não ataca por {c.eagleTurnsLeft} turno(s)</color>");
        if (c.invulnerableRoundsLeft > 0)
            sb.AppendLine($"   <color={RO}>INVULNERÁVEL por {c.invulnerableRoundsLeft} round(s)</color>");

        // Ações disponíveis (fora da fase inicial)
        if (!lobbyPhase)
        {
            bool canMove = c.CanMoveThisRound();
            bool canAttack = c.CanAttackThisRound();
            int targets = canAttack ? c.GetAdjacentEnemies().Count : 0;

            string moveTxt = canMove ? $"<color={OK}>pode MOVER</color>" : $"<color={NO}>já moveu</color>";
            string atkTxt;
            if (!canAttack) atkTxt = $"<color={NO}>não pode atacar</color>";
            else if (targets > 0) atkTxt = $"<color={OK}>pode ATACAR ({targets} alvo(s) no alcance)</color>";
            else atkTxt = "pode atacar, sem alvos no alcance";
            sb.AppendLine($"   {moveTxt}  |  {atkTxt}");

            // Torre ao alcance? (última fileira; Mago/Arqueiro também da penúltima)
            if (canAttack && c.currentTile != null)
            {
                bool longRange = c.card.cardClass == CardClass.Arqueiro || c.card.cardClass == CardClass.Mago;
                int reach = longRange ? 2 : 1;
                bool towerInReach = (c.ownerPlayerNumber == 1 && c.currentTile.row >= totalRows - reach) ||
                                    (c.ownerPlayerNumber == 2 && c.currentTile.row <= reach - 1);
                if (towerInReach)
                    sb.AppendLine($"   <color={OK}>pode atacar a TORRE inimiga (tecla T ou clique além do tabuleiro)</color>");
            }
        }

        // Contador de efeito periódico / cooldown
        if (c.effectCounter > 0)
        {
            string unit = c.effectCounterIsRound ? "round(s)" : "turno(s)";
            string cor = c.effectCounterIsRound ? RO : TU;
            sb.AppendLine(c.effectPeriod > 0
                ? $"   <color={cor}>efeito automático dispara em {c.effectCounter} {unit}</color>"
                : $"   <color={cor}>habilidade recarrega em {c.effectCounter} {unit}</color>");
        }
        else if (c.card.cardClass == CardClass.Healer && c.card.tier == CardTier.Tier1 &&
                 c.card.attack == 0 && c.card.health == 4)
        {
            sb.AppendLine($"   <color={OK}>anular ataque: PRONTO</color>");
        }

        // Descrição resumida do efeito da carta
        if (!string.IsNullOrEmpty(c.card.effectDescription))
        {
            string desc = c.card.effectDescription.Replace("\n", " ");
            if (desc.Length > 80) desc = desc.Substring(0, 80) + "…";
            sb.AppendLine($"   <i><color=#9AA3C0>{desc}</color></i>");
        }
    }

    // Progresso das tríades tier-2 (uma por classe)
    void AppendTriadStatus(System.Text.StringBuilder sb, System.Collections.Generic.List<CardDisplay> myCards)
    {
        sb.AppendLine("<b>Tríades (as 3 em campo ativam o bônus):</b>");
        AppendOneTriad(sb, myCards, "Arqueiros (+5 ATK a todos os aliados)", CardClass.Arqueiro,
            new int[][] { new[] { 2, 0, 2 }, new[] { 3, 0, 2 }, new[] { 2, 0, 3 } });
        AppendOneTriad(sb, myCards, "Healers (ouro e vida da torre no máximo)", CardClass.Healer,
            new int[][] { new[] { 1, 0, 4 }, new[] { 0, 0, 4 }, new[] { 0, 0, 3 } });
        AppendOneTriad(sb, myCards, "Magos (invoca Mago Lendário)", CardClass.Mago,
            new int[][] { new[] { 2, 0, 4 }, new[] { 2, 0, 3 }, new[] { 3, 0, 3 } });
        AppendOneTriad(sb, myCards, "Tanks (+10 armadura a todos os aliados)", CardClass.Tank,
            new int[][] { new[] { 1, 2, 4 }, new[] { 1, 3, 3 }, new[] { 0, 3, 4 } });
    }

    // members: cada item = {ATK, DEF, HP} da carta da tríade
    void AppendOneTriad(System.Text.StringBuilder sb,
        System.Collections.Generic.List<CardDisplay> myCards,
        string label, CardClass cls, int[][] members)
    {
        int present = 0;
        bool activated = false;
        var missing = new System.Collections.Generic.List<string>();

        foreach (int[] m in members)
        {
            bool found = false;
            foreach (var c in myCards)
            {
                if (c == null || c.card == null) continue;
                if (c.card.cardClass == cls && c.card.tier == CardTier.Tier2 &&
                    c.card.attack == m[0] && c.card.shield == m[1] && c.card.health == m[2])
                {
                    found = true;
                    if (cls == CardClass.Healer ? c.healerComboActivated : c.archerComboActivated)
                        activated = true;
                    break;
                }
            }

            if (found) present++;
            else missing.Add(m[1] > 0
                ? $"(ATK {m[0]}, DEF {m[1]}, HP {m[2]})"
                : $"(ATK {m[0]}, HP {m[2]})");
        }

        if (activated)
            sb.AppendLine($"• {label}: <color={OK}>ATIVADA!</color>");
        else if (present == 0)
            sb.AppendLine($"• {label}: 0/3 em campo");
        else
            sb.AppendLine($"• {label}: {present}/3 — falta {string.Join(", ", missing)}");
    }

    // Combos de cartas específicas que estão em campo (o que falta para ativar)
    void AppendComboWatch(System.Text.StringBuilder sb,
        System.Collections.Generic.List<CardDisplay> myCards, int localPlayer)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasTank = board.HasClassOnBoard(localPlayer, CardClass.Tank);
        bool hasHealer = board.HasClassOnBoard(localPlayer, CardClass.Healer);
        bool hasMage = board.HasClassOnBoard(localPlayer, CardClass.Mago);
        bool hasArcher = board.HasClassOnBoard(localPlayer, CardClass.Arqueiro);

        bool any = false;
        foreach (var c in myCards)
        {
            if (c == null || c.card == null) continue;
            Card card = c.card;

            // Tank 4 (3/7/8): +2 de dano + armadura/turno com as 3 classes (v4.2)
            if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier4 &&
                card.attack == 3 && card.shield == 7 && card.health == 8)
            {
                any = true;
                sb.AppendLine(ComboLine("Tank 4 (3/7/8) — +2 de dano + armadura/turno",
                    Missing(hasHealer, "Healer", hasMage, "Mago", hasArcher, "Arqueiro"), false));
            }
            // Tank 4 (2/6/7): +5 HP +2 DEF com Arqueiro e Mago (1x)
            else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier4 &&
                     card.attack == 2 && card.shield == 6 && card.health == 7)
            {
                any = true;
                sb.AppendLine(ComboLine("Tank 4 (2/6/7) — +5 HP e +2 DEF",
                    Missing(hasArcher, "Arqueiro", hasMage, "Mago"), c.tankTier4Effect1Used));
            }
            // Tank 4 (2/6/8): Arqueiros atacam 2x com as 4 classes (aura round sim, round não)
            else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier4 &&
                     card.attack == 2 && card.shield == 6 && card.health == 8)
            {
                any = true;
                sb.AppendLine(ComboLine("Tank 4 (2/6/8) — Arqueiros atacam 2x (round sim, round não)",
                    Missing(hasTank, "Tank", hasHealer, "Healer", hasMage, "Mago", hasArcher, "Arqueiro"),
                    false));
            }
            // Healer 4 (3/4): +3 em todos os status com Tank+Arqueiro+Mago (1x)
            else if (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier4 &&
                     card.attack == 3 && card.health == 4)
            {
                any = true;
                sb.AppendLine(ComboLine("Healer 4 (3/4) — +3 todos os status dos aliados",
                    Missing(hasTank, "Tank", hasArcher, "Arqueiro", hasMage, "Mago"), c.healerTier4Effect4Used));
            }
        }

        if (!any)
            sb.AppendLine("<i>Nenhum combo de carta específica em campo no momento.</i>");
    }

    string ComboLine(string label, string missing, bool used)
    {
        if (used) return $"• {label}: <color={OK}>já ativado nesta partida</color>";
        if (string.IsNullOrEmpty(missing)) return $"• {label}: <color={OK}>condições OK!</color>";
        return $"• {label}: falta <color={NO}>{missing}</color> em campo";
    }

    // Monta a lista de classes faltantes a partir de pares (temClasse, nome)
    string Missing(params object[] pairs)
    {
        var missing = new System.Collections.Generic.List<string>();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            if (!(bool)pairs[i]) missing.Add((string)pairs[i + 1]);
        }
        return string.Join(" + ", missing);
    }

    HandManager FindHandManagerFor(int playerNumber)
    {
        foreach (HandManager hm in FindObjectsOfType<HandManager>())
        {
            if (hm.playerNumber == playerNumber) return hm;
        }
        return null;
    }

    // "Sair do Jogo" — usado pelo botão dentro do overlay de vitória e pelo popup
    // de configurações (MusicManager tem sua própria cópia, para funcionar também
    // no lobby onde o GameUIManager não existe)
    public void QuitGame()
    {
        Debug.Log("[GameUIManager] Saindo do jogo...");

        // Se fechar no meio de uma partida, grava o log pendente (sobe no próximo
        // boot). Pós-partida (tela de vitória) já foi salvo — aqui é no-op.
        MatchReporter.SaveAbandonedNow();

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

        // Painel de logs aberto: atualiza o relatório 1x por segundo
        if (logsPanel != null && logsPanel.activeSelf &&
            Time.unscaledTime - lastLogsRefresh > 1f)
        {
            RefreshLogsPanel();
        }

        // Inspeção de carta aberta: qualquer clique fora do painel fecha
        // (ignora o mesmo frame em que abriu — o clique que abre não fecha)
        if (cardPreviewPanel != null && cardPreviewPanel.activeSelf &&
            Time.frameCount != previewOpenedFrame &&
            UnityEngine.InputSystem.Mouse.current != null)
        {
            var previewMouse = UnityEngine.InputSystem.Mouse.current;
            if (previewMouse.leftButton.wasPressedThisFrame ||
                previewMouse.rightButton.wasPressedThisFrame)
            {
                RectTransform previewRt = cardPreviewPanel.GetComponent<RectTransform>();
                Canvas previewCanvas = cardPreviewPanel.GetComponentInParent<Canvas>();
                Camera previewCam = (previewCanvas != null &&
                    previewCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? previewCanvas.worldCamera : null;
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                        previewRt, previewMouse.position.ReadValue(), previewCam))
                {
                    HideCardPreview();
                }
            }
        }

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
            player1HealthText.text = $"Vida: {TurnManager.Instance.player1.health}/{TowerSystem.MaxTowerHealth(1)}";
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
            player2HealthText.text = $"Vida: {TurnManager.Instance.player2.health}/{TowerSystem.MaxTowerHealth(2)}";
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

        // Checkbox "Travar loja": só com a partida rolando; ao voltar pro lobby
        // (revanche), desmarca — as travas são zeradas no reinício
        if (shopLockObj != null)
        {
            bool playing = TurnManager.Instance.gameState == GameState.Playing;
            if (shopLockObj.activeSelf != playing) shopLockObj.SetActive(playing);
            if (!playing && shopLockToggle != null && shopLockToggle.isOn)
                shopLockToggle.SetIsOnWithoutNotify(false);
        }
    }

    // Retorna os dados do jogador LOCAL (deste cliente), não do jogador do turno.
    // Na fase de compra (lobby) as compras são simultâneas, então cada cliente
    // precisa ver o próprio contador — GetCurrentPlayer() mostraria o mesmo para
    // os dois. Fallback para o jogador do turno se a sincronização ainda não veio.
    PlayerData GetLocalPlayer()
    {
        if (PhotonGameManager.Instance != null && PhotonGameManager.Instance.myPlayerNumber != 0)
            return TurnManager.Instance.GetPlayer(PhotonGameManager.Instance.myPlayerNumber);
        return TurnManager.Instance.GetCurrentPlayer();
    }

    void UpdateLobbyUI()
    {
        if (turnInfoText != null)
        {
            // Fase de compra inicial: mostrar as compras do PRÓPRIO jogador contra o
            // teto do lobby (5), não o limite de 2 por turno da partida.
            PlayerData me = GetLocalPlayer();

            // Conta quantos jogadores estão prontos
            int readyCount = 0;
            if (TurnManager.Instance.player1Ready) readyCount++;
            if (TurnManager.Instance.player2Ready) readyCount++;

            string readyMsg = readyCount == 0 ? "Clique 2x em 'Iniciar Partida'" : "Clique mais 1x para iniciar!";

            turnInfoText.text = $"{me.playerName}\nCartas: {me.cardsBoughtThisTurn}/{PlayerData.MaxCardsInLobby}\n{readyMsg}";
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
            turnInfoText.text = $"Turno: {currentPlayer.playerName}\nCartas: {currentPlayer.cardsBoughtThisTurn}/{PlayerData.MaxCardsPerTurn}";
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

        // Em multiplayer, sincroniza o reset nos dois clientes. Na fase inicial
        // (compras simultâneas) não há turno para validar — o reset é sempre do
        // jogador local; durante a partida, só no seu turno
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            bool lobbyPhase = TurnManager.Instance.gameState == GameState.Lobby;
            if (!lobbyPhase &&
                TurnManager.Instance.currentPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
            {
                Debug.Log("[GameUI] Não é seu turno, não pode resetar a loja!");
                return;
            }
            PhotonGameManager.Instance.SendResetStoreRPC(PhotonGameManager.Instance.myPlayerNumber);
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
        // O painel serializado da cena renderizava quebrado — escondemos ele e
        // usamos um overlay construído 100% por código (sempre limpo)
        if (victoryPanel != null) victoryPanel.SetActive(false);

        if (victoryOverlay == null) BuildVictoryOverlay();
        if (victoryOverlay == null) return; // sem canvas — nada a fazer

        // Descobre se o JOGADOR LOCAL venceu (em multiplayer) para personalizar
        int localPlayer = (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
            ? PhotonGameManager.Instance.myPlayerNumber
            : winnerPlayerNumber;
        if (localPlayer == 0) localPlayer = winnerPlayerNumber;
        bool localWon = localPlayer == winnerPlayerNumber;

        if (victoryOverlayTitle != null)
        {
            victoryOverlayTitle.text = localWon ? "VITÓRIA!" : "DERROTA";
            victoryOverlayTitle.color = localWon
                ? new Color(0.96f, 0.77f, 0.32f)   // dourado
                : new Color(1f, 0.42f, 0.37f);     // vermelho
        }
        if (victoryOverlaySubtitle != null)
            victoryOverlaySubtitle.text = localWon
                ? "Você derrubou a torre inimiga!"
                : "Sua torre foi destruída.";

        // Resumo da partida: duração, rounds e mapa (usa os mesmos dados que o
        // MatchReporter envia ao banco)
        if (victoryOverlayStats != null)
        {
            int rounds = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 0;
            int secs = (TurnManager.Instance != null && TurnManager.Instance.matchStartRealtime > 0f)
                ? Mathf.Max(0, (int)(Time.realtimeSinceStartup - TurnManager.Instance.matchStartRealtime))
                : 0;
            string dur = secs >= 60 ? $"{secs / 60}min {secs % 60}s" : $"{secs}s";
            string map = BoardThemeManager.Current == BoardTheme.Tabletop ? "Mesa de RPG"
                       : BoardThemeManager.Current == BoardTheme.Forest ? "Floresta"
                       : BoardThemeManager.Current == BoardTheme.Space ? "Espaço"
                       : BoardThemeManager.Current == BoardTheme.Teste ? "Teste" : "—";
            victoryOverlayStats.text = $"Duração {dur}   ·   {rounds} rounds   ·   {map}";
        }

        victoryOverlay.SetActive(true);
        victoryOverlay.transform.SetAsLastSibling(); // fica por cima de tudo

        Debug.Log($"Tela de vitória mostrada. Vencedor: Jogador {winnerPlayerNumber}");

        // Sobe a partida (duração/rounds/mapa/log) pra conta logada no launcher.
        // Fora do lockstep: HTTP depois da partida decidida; sem login, não faz nada.
        MatchReporter.ReportMatchEnd(winnerPlayerNumber);
    }

    public void HideVictoryScreen()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (victoryOverlay != null) victoryOverlay.SetActive(false);
    }

    // Constrói o overlay de vitória: painel escurecido em tela cheia, título,
    // subtítulo e botões "Jogar Novamente" e "Sair do Jogo".
    void BuildVictoryOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameUIManager] Nenhum Canvas — overlay de vitória não criado");
            return;
        }

        // Fundo em tela cheia (bloqueia cliques atrás)
        victoryOverlay = new GameObject("VictoryOverlay",
            typeof(RectTransform), typeof(Image));
        victoryOverlay.transform.SetParent(canvas.transform, false);
        RectTransform ort = victoryOverlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero;
        ort.offsetMax = Vector2.zero;
        victoryOverlay.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.88f);

        // Caixa central
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(victoryOverlay.transform, false);
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(560f, 360f);
        box.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 0.98f);

        // Título
        victoryOverlayTitle = MakeOverlayLabel(box.transform, "VITÓRIA!", 64f,
            new Vector2(0f, 110f), new Vector2(520f, 90f), FontStyles.Bold);

        // Subtítulo
        victoryOverlaySubtitle = MakeOverlayLabel(box.transform, "", 26f,
            new Vector2(0f, 48f), new Vector2(520f, 40f), FontStyles.Normal);
        victoryOverlaySubtitle.color = new Color(0.85f, 0.88f, 0.95f);

        // Resumo da partida (duração/rounds/mapa)
        victoryOverlayStats = MakeOverlayLabel(box.transform, "", 17f,
            new Vector2(0f, 12f), new Vector2(520f, 28f), FontStyles.Normal);
        victoryOverlayStats.color = new Color(0.62f, 0.66f, 0.76f);

        // Botão "Jogar Novamente"
        MakeOverlayButton(box.transform, "Jogar Novamente",
            new Color(0.15f, 0.45f, 0.28f, 1f), new Vector2(0f, -55f),
            OnRestartButtonClicked);

        // Botão "Sair do Jogo"
        MakeOverlayButton(box.transform, "Sair do Jogo",
            new Color(0.45f, 0.14f, 0.14f, 1f), new Vector2(0f, -130f),
            QuitGame);

        victoryOverlay.SetActive(false);
    }

    TextMeshProUGUI MakeOverlayLabel(Transform parent, string text, float size,
        Vector2 anchoredPos, Vector2 sizeDelta, FontStyles style)
    {
        GameObject obj = new GameObject("Label", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    void MakeOverlayButton(Transform parent, string label, Color color,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Button_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300f, 58f);
        btnObj.GetComponent<Image>().color = color;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    void OnRestartButtonClicked()
    {
        Debug.Log("Botão 'Jogar Novamente' clicado!");

        // Reinício sincronizado nos dois clientes (o Master gera a nova seed).
        // Em modo offline, RequestRestart reinicia direto.
        if (PhotonGameManager.Instance != null)
            PhotonGameManager.Instance.RequestRestart();
        else if (TurnManager.Instance != null)
            TurnManager.Instance.RestartGame();
    }

    // Fila de decisões: o popup é um painel ÚNICO. Antes, uma segunda decisão
    // chegando antes da primeira ser respondida SOBRESCREVIA as callbacks — a
    // decisão antiga nunca resolvia e o dano/efeito daquele ataque se perdia
    // para sempre ("ataquei o tank e não aconteceu nada"). Agora cada decisão
    // espera a vez na fila.
    class PendingDecision
    {
        public string message;
        public string yesText;
        public string noText;
        public System.Action onYes;
        public System.Action onNo;
    }

    private readonly System.Collections.Generic.Queue<PendingDecision> decisionQueue =
        new System.Collections.Generic.Queue<PendingDecision>();
    private bool decisionShowing = false;

    // Há um popup de decisão aberto (ou na fila) NESTE cliente?
    public bool HasOpenDecision
    {
        get { return decisionShowing || decisionQueue.Count > 0; }
    }

    // ===== Faixa "oponente decidindo" (aviso para quem está esperando) =====
    private GameObject waitingBanner;
    private TMPro.TextMeshProUGUI waitingBannerText;

    public void ShowWaitingBanner(string message)
    {
        if (waitingBanner == null)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            waitingBanner = new GameObject("WaitingBanner");
            waitingBanner.transform.SetParent(canvas.transform, false);
            RectTransform rt = waitingBanner.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -72f);
            rt.sizeDelta = new Vector2(640f, 48f);

            UnityEngine.UI.Image bg = waitingBanner.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.08f, 0.09f, 0.16f, 0.92f);
            bg.raycastTarget = false; // Só aviso: não pode roubar cliques da UI

            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(waitingBanner.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 4f);
            trt.offsetMax = new Vector2(-12f, -4f);

            waitingBannerText = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            waitingBannerText.fontSize = 22f;
            waitingBannerText.fontStyle = TMPro.FontStyles.Bold;
            waitingBannerText.alignment = TMPro.TextAlignmentOptions.Center;
            waitingBannerText.color = new Color(0.96f, 0.77f, 0.32f);
            waitingBannerText.raycastTarget = false;
        }

        waitingBannerText.text = message;
        waitingBanner.SetActive(true);
        waitingBanner.transform.SetAsLastSibling();
    }

    public void HideWaitingBanner()
    {
        if (waitingBanner != null) waitingBanner.SetActive(false);
    }

    public void ShowDecisionPopup(string message, string yesButtonText, System.Action onYes, string noButtonText, System.Action onNo)
    {
        if (decisionPopupPanel == null)
        {
            Debug.LogWarning("Decision Popup Panel não está configurado!");
            onYes?.Invoke();
            return;
        }

        decisionQueue.Enqueue(new PendingDecision
        {
            message = message,
            yesText = yesButtonText,
            noText = noButtonText,
            onYes = onYes,
            onNo = onNo
        });

        if (!decisionShowing)
        {
            ShowNextDecision();
        }
        else
        {
            Debug.Log($"[DecisionPopup] Em fila (aguardando resposta do atual): {message}");
        }
    }

    void ShowNextDecision()
    {
        if (decisionQueue.Count == 0)
        {
            decisionShowing = false;
            return;
        }

        decisionShowing = true;
        PendingDecision decision = decisionQueue.Dequeue();

        // Armazena as callbacks
        onDecisionYes = decision.onYes;
        onDecisionNo = decision.onNo;

        // Popup por código (padronizado com o modal de configurações). O
        // painel serializado da cena fica SEMPRE oculto.
        if (decisionPopupPanel != null) decisionPopupPanel.SetActive(false);
        if (decisionOverlay == null) BuildDecisionOverlay();

        if (decisionOverlay != null)
        {
            if (decisionOverlayMsg != null) decisionOverlayMsg.text = decision.message;
            if (decisionYesLabelCode != null) decisionYesLabelCode.text = decision.yesText;
            if (decisionNoLabelCode != null) decisionNoLabelCode.text = decision.noText;
            decisionOverlay.SetActive(true);
            decisionOverlay.transform.SetAsLastSibling();
        }
        else
        {
            // Fallback: se não houve Canvas para o overlay, usa o serializado
            if (decisionMessageText != null) decisionMessageText.text = decision.message;
            if (decisionYesButtonText != null) decisionYesButtonText.text = decision.yesText;
            if (decisionNoButtonText != null) decisionNoButtonText.text = decision.noText;
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
            if (decisionPopupPanel != null) decisionPopupPanel.SetActive(true);
        }
        Debug.Log($"[DecisionPopup] Mostrando: {decision.message}");
    }

    // Popup de decisão de efeito, no MESMO estilo do modal de configurações:
    // fundo escurecido, caixa arredondada com moldura dourada, botão "sim" em
    // dourado e "não" em ardósia. Construído 1x sob demanda.
    void BuildDecisionOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        decisionOverlay = new GameObject("DecisionOverlay", typeof(RectTransform), typeof(Image));
        decisionOverlay.transform.SetParent(canvas.transform, false);
        RectTransform ort = decisionOverlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        decisionOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(decisionOverlay.transform, false);
        RectTransform brt = box.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(480f, 260f);
        LobbySprites.MakeRounded(box.GetComponent<Image>(), new Color(0.07f, 0.05f, 0.035f, 0.99f));
        LobbySprites.AddRing(box.transform, new Color(0.96f, 0.77f, 0.32f, 0.5f));

        decisionOverlayMsg = MakeOverlayLabel(box.transform, "", 22f,
            new Vector2(0f, 52f), new Vector2(430f, 120f), FontStyles.Bold);

        // "Sim" (dourado) à esquerda, "Não" (ardósia) à direita
        decisionYesLabelCode = MakeDecisionButton(box.transform, "Sim",
            new Color(0.96f, 0.77f, 0.32f), new Color(0.12f, 0.09f, 0.02f),
            new Vector2(-116f, -72f), OnDecisionYesClicked);
        decisionNoLabelCode = MakeDecisionButton(box.transform, "Não",
            new Color(0.17f, 0.22f, 0.34f), Color.white,
            new Vector2(116f, -72f), OnDecisionNoClicked);

        decisionOverlay.SetActive(false);
    }

    TextMeshProUGUI MakeDecisionButton(Transform parent, string label, Color bg, Color fg,
        Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200f, 56f);
        LobbySprites.MakeRounded(go.GetComponent<Image>(), bg);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        GameObject txt = new GameObject("L", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        RectTransform trt = txt.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 24f; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = fg; tmp.raycastTarget = false;
        return tmp;
    }

    void OnDecisionYesClicked()
    {
        if (decisionPopupPanel != null) decisionPopupPanel.SetActive(false);
        if (decisionOverlay != null) decisionOverlay.SetActive(false);
        decisionShowing = false;
        System.Action callback = onDecisionYes;
        onDecisionYes = null;
        onDecisionNo = null;
        // finally: se a callback estourar exceção, a fila de decisões NÃO
        // pode ficar presa (decisionShowing=true sem popup = clique travado)
        try { callback?.Invoke(); }
        finally { ShowNextDecision(); } // A callback pode ter enfileirado novas decisões
    }

    void OnDecisionNoClicked()
    {
        if (decisionPopupPanel != null) decisionPopupPanel.SetActive(false);
        if (decisionOverlay != null) decisionOverlay.SetActive(false);
        decisionShowing = false;
        System.Action callback = onDecisionNo;
        onDecisionYes = null;
        onDecisionNo = null;
        try { callback?.Invoke(); }
        finally { ShowNextDecision(); }
    }
}
