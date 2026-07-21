using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Constrói os popups do lobby por código (mesmo estilo procedural do resto do jogo):
// - Sala de espera do anfitrião (aguardando jogador 2 / iniciar partida)
// - Sala de espera do convidado (aguardando o anfitrião iniciar)
// - Lista de salas disponíveis (para o Join Game)
// Nenhuma lógica de rede aqui: o PhotonLobbyManager comanda tudo via callbacks.
public class LobbyUI
{
    // Paleta (igual ao tema espacial do jogo/site)
    static readonly Color PanelBg = new Color(0.055f, 0.075f, 0.13f, 0.98f);
    static readonly Color PanelBorder = new Color(0.96f, 0.77f, 0.32f, 0.35f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color GoldTextDark = new Color(0.12f, 0.09f, 0.02f);
    static readonly Color Slate = new Color(0.17f, 0.22f, 0.34f);
    static readonly Color TextLight = new Color(0.92f, 0.94f, 0.98f);
    static readonly Color TextMuted = new Color(0.55f, 0.6f, 0.72f);
    static readonly Color Green = new Color(0.42f, 0.85f, 0.6f);

    private GameObject overlay;      // fundo escuro que bloqueia cliques atrás dos popups
    private GameObject hostPanel;
    private GameObject guestPanel;
    private GameObject listPanel;
    private GameObject botPanel;

    // Popup do treino: mapa escolhido + callback (dificuldade, mapa)
    private Toggle botMapTable;
    private Toggle botMapForest;
    private Toggle botMapSpace;
    private Toggle botMapTeste;
    private Action<int, int> onBotStart;

    private TMP_Text hostTitle;
    private TMP_Text hostStatus;
    private Button startButton;
    private Toggle mapTableToggle;
    private Toggle mapForestToggle;
    private Toggle mapSpaceToggle;
    private Toggle mapTesteToggle;

    // Mapa escolhido pelo anfitrião: 1 = Mesa de RPG (padrão), 2 = Floresta,
    // 0 = Espaço, 3 = Teste (mesmos códigos da room property "theme")
    public int SelectedMapTheme
    {
        get
        {
            if (mapSpaceToggle != null && mapSpaceToggle.isOn) return 0;
            if (mapForestToggle != null && mapForestToggle.isOn) return 2;
            if (mapTesteToggle != null && mapTesteToggle.isOn) return 3;
            return 1;
        }
    }
    private TMP_Text guestTitle;
    private Transform listContent;
    private TMP_Text listEmptyLabel;
    private TMP_Text toastLabel;

    // Chamado quando o jogador fecha a lista de salas (para o manager atualizar o estado)
    public Action OnListClosed;

    public LobbyUI(Canvas canvas)
    {
        if (canvas == null)
        {
            Debug.LogError("[LobbyUI] Canvas não encontrado no lobby!");
            return;
        }

        Transform root = canvas.transform;

        // Overlay em tela cheia (escurece e bloqueia o menu atrás)
        overlay = MakeImage(root, "LobbyOverlay", Vector2.zero, new Color(0f, 0f, 0f, 0.65f));
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;
        overlay.transform.SetAsLastSibling();

        BuildHostPanel();
        BuildGuestPanel();
        BuildListPanel();
        BuildBotPanel();

        // Toast de status (fora do overlay, sempre visível na base da tela)
        GameObject toastGO = MakeText(root, "LobbyToast", "", 17, Gold, TextAlignmentOptions.Center);
        RectTransform trt = toastGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0f);
        trt.anchorMax = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 46f);
        trt.sizeDelta = new Vector2(700f, 34f);
        toastLabel = toastGO.GetComponent<TMP_Text>();

        overlay.SetActive(false);
    }

    // ================== POPUPS ==================

    void BuildHostPanel()
    {
        hostPanel = MakePanel(540f, 460f);

        hostTitle = MakeText(hostPanel.transform, "Title", "Sala", 32, Gold, TextAlignmentOptions.Center,
            new Vector2(0f, 180f), new Vector2(500f, 44f)).GetComponent<TMP_Text>();
        hostTitle.fontStyle = FontStyles.Bold;

        MakeImage(hostPanel.transform, "Divider", new Vector2(460f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 152f);

        MakeText(hostPanel.transform, "You", "Você está na sala  (anfitrião)", 19, TextLight,
            TextAlignmentOptions.Center, new Vector2(0f, 112f), new Vector2(500f, 30f));

        hostStatus = MakeText(hostPanel.transform, "Status", "Aguardando o jogador 2 entrar...", 19, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 76f), new Vector2(500f, 30f)).GetComponent<TMP_Text>();

        // Escolha do mapa (só o anfitrião vê este painel). Padrão: Mesa de RPG.
        MakeText(hostPanel.transform, "MapLabel", "Mapa da partida:", 17, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 38f), new Vector2(500f, 26f));
        mapTableToggle = MakeCheckbox(hostPanel.transform, "Mesa de RPG", new Vector2(-180f, 4f), true, 150f);
        mapForestToggle = MakeCheckbox(hostPanel.transform, "Floresta", new Vector2(-40f, 4f), false, 120f);
        mapSpaceToggle = MakeCheckbox(hostPanel.transform, "Espaço", new Vector2(78f, 4f), false, 105f);
        mapTesteToggle = MakeCheckbox(hostPanel.transform, "Teste", new Vector2(185f, 4f), false, 100f);

        // Comportamento de "rádio": sempre exatamente UM mapa marcado
        WireMapRadio(mapTableToggle, mapForestToggle, mapSpaceToggle, mapTesteToggle);
        WireMapRadio(mapForestToggle, mapTableToggle, mapSpaceToggle, mapTesteToggle);
        WireMapRadio(mapSpaceToggle, mapTableToggle, mapForestToggle, mapTesteToggle);
        WireMapRadio(mapTesteToggle, mapTableToggle, mapForestToggle, mapSpaceToggle);

        startButton = MakeButton(hostPanel.transform, "Iniciar Partida", new Vector2(0f, -74f),
            new Vector2(260f, 56f), Gold, GoldTextDark, 21, null);
        startButton.interactable = false;

        MakeButton(hostPanel.transform, "Sair da Sala", new Vector2(0f, -156f),
            new Vector2(260f, 44f), Slate, TextLight, 17, null).name = "LeaveButton";
    }

    void BuildGuestPanel()
    {
        guestPanel = MakePanel(540f, 320f);

        guestTitle = MakeText(guestPanel.transform, "Title", "Sala", 32, Gold, TextAlignmentOptions.Center,
            new Vector2(0f, 108f), new Vector2(500f, 44f)).GetComponent<TMP_Text>();
        guestTitle.fontStyle = FontStyles.Bold;

        MakeImage(guestPanel.transform, "Divider", new Vector2(460f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 80f);

        MakeText(guestPanel.transform, "You", "Você entrou na sala!", 19, Green,
            TextAlignmentOptions.Center, new Vector2(0f, 38f), new Vector2(500f, 30f));

        MakeText(guestPanel.transform, "Status", "Aguardando o anfitrião iniciar a partida...", 19, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 2f), new Vector2(500f, 30f));

        MakeButton(guestPanel.transform, "Sair da Sala", new Vector2(0f, -104f),
            new Vector2(260f, 44f), Slate, TextLight, 17, null).name = "LeaveButton";
    }

    void BuildListPanel()
    {
        listPanel = MakePanel(560f, 470f);

        TMP_Text title = MakeText(listPanel.transform, "Title", "Salas Disponíveis", 30, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 190f), new Vector2(520f, 42f)).GetComponent<TMP_Text>();
        title.fontStyle = FontStyles.Bold;

        MakeText(listPanel.transform, "Sub", "Clique em uma sala para entrar", 16, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 156f), new Vector2(520f, 26f));

        GameObject content = new GameObject("RoomsContent", typeof(RectTransform));
        content.transform.SetParent(listPanel.transform, false);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchoredPosition = new Vector2(0f, -6f);
        crt.sizeDelta = new Vector2(520f, 280f);
        listContent = content.transform;

        listEmptyLabel = MakeText(listPanel.transform, "Empty",
            "Nenhuma sala aberta no momento.\nPeça para um amigo criar uma!", 18, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(480f, 60f)).GetComponent<TMP_Text>();

        MakeButton(listPanel.transform, "Fechar", new Vector2(0f, -196f),
            new Vector2(220f, 44f), Slate, TextLight, 17, CloseList);
    }

    // Popup do "Treinar vs Bot": escolha de dificuldade (clique inicia) e mapa
    void BuildBotPanel()
    {
        botPanel = MakePanel(560f, 430f);

        TMP_Text title = MakeText(botPanel.transform, "Title", "Treinar vs Bot", 30, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 168f), new Vector2(520f, 42f)).GetComponent<TMP_Text>();
        title.fontStyle = FontStyles.Bold;

        MakeImage(botPanel.transform, "Divider", new Vector2(480f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 138f);

        MakeText(botPanel.transform, "DiffLabel", "Escolha a dificuldade para começar:", 17, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 104f), new Vector2(520f, 26f));

        MakeButton(botPanel.transform, "Fácil", new Vector2(-180f, 56f),
            new Vector2(150f, 54f), Slate, Green, 20, () => StartBot(0));
        MakeButton(botPanel.transform, "Médio", new Vector2(0f, 56f),
            new Vector2(150f, 54f), Gold, GoldTextDark, 20, () => StartBot(1));
        MakeButton(botPanel.transform, "Difícil", new Vector2(180f, 56f),
            new Vector2(150f, 54f), new Color(0.52f, 0.18f, 0.14f), TextLight, 20, () => StartBot(2));

        MakeText(botPanel.transform, "DiffHint",
            "Fácil: distraído  ·  Médio: tático  ·  Difícil: planeja tríades e defende", 14, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 14f), new Vector2(520f, 24f));

        MakeText(botPanel.transform, "MapLabel", "Mapa do treino:", 17, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, -34f), new Vector2(520f, 26f));
        botMapTable = MakeCheckbox(botPanel.transform, "Mesa de RPG", new Vector2(-185f, -68f), true, 150f);
        botMapForest = MakeCheckbox(botPanel.transform, "Floresta", new Vector2(-45f, -68f), false, 120f);
        botMapSpace = MakeCheckbox(botPanel.transform, "Espaço", new Vector2(73f, -68f), false, 105f);
        botMapTeste = MakeCheckbox(botPanel.transform, "Teste", new Vector2(180f, -68f), false, 100f);
        WireMapRadio(botMapTable, botMapForest, botMapSpace, botMapTeste);
        WireMapRadio(botMapForest, botMapTable, botMapSpace, botMapTeste);
        WireMapRadio(botMapSpace, botMapTable, botMapForest, botMapTeste);
        WireMapRadio(botMapTeste, botMapTable, botMapForest, botMapSpace);

        MakeButton(botPanel.transform, "Voltar", new Vector2(0f, -158f),
            new Vector2(220f, 44f), Slate, TextLight, 17, () => HideAll());
    }

    void StartBot(int difficulty)
    {
        int map = (botMapSpace != null && botMapSpace.isOn) ? 0
                : (botMapForest != null && botMapForest.isOn) ? 2
                : (botMapTeste != null && botMapTeste.isOn) ? 3 : 1;
        HideAll();
        if (onBotStart != null) onBotStart(difficulty, map);
    }

    // ================== API PARA O MANAGER ==================

    public void ShowBotSetup(Action<int, int> onStart)
    {
        HideAll();
        onBotStart = onStart;
        overlay.SetActive(true);
        botPanel.transform.parent.gameObject.SetActive(true);
    }

    public void ShowHostRoom(string roomName, Action onStart, Action onLeave)
    {
        HideAll();
        overlay.SetActive(true);
        hostPanel.transform.parent.gameObject.SetActive(true);
        hostTitle.text = roomName;

        startButton.onClick.RemoveAllListeners();
        if (onStart != null) startButton.onClick.AddListener(() => onStart());
        WireLeave(hostPanel, onLeave);
        SetHostStatus(false);
    }

    public void SetHostStatus(bool opponentPresent)
    {
        if (hostStatus == null) return;
        if (opponentPresent)
        {
            hostStatus.text = "Jogador 2 entrou! Pode iniciar a partida.";
            hostStatus.color = Green;
        }
        else
        {
            hostStatus.text = "Aguardando o jogador 2 entrar...";
            hostStatus.color = TextMuted;
        }
        if (startButton != null) startButton.interactable = opponentPresent;
    }

    public void ShowGuestRoom(string roomName, Action onLeave)
    {
        HideAll();
        overlay.SetActive(true);
        guestPanel.transform.parent.gameObject.SetActive(true);
        guestTitle.text = roomName;
        WireLeave(guestPanel, onLeave);
    }

    public void ShowRoomList()
    {
        HideAll();
        overlay.SetActive(true);
        listPanel.transform.parent.gameObject.SetActive(true);
    }

    // Recria os botões de sala (chamado a cada atualização da lista do Photon)
    public void PopulateRoomList(RoomInfo[] rooms, Action<string> onJoin)
    {
        if (listContent == null) return;

        foreach (Transform child in listContent)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        int shown = 0;
        if (rooms != null)
        {
            foreach (RoomInfo room in rooms)
            {
                if (!room.IsOpen || room.PlayerCount >= room.MaxPlayers) continue;
                if (shown >= 5) break; // cabe até 5 salas no painel

                string roomName = room.Name;
                string label = string.Format("{0}      {1}/{2} jogadores", room.Name, room.PlayerCount, room.MaxPlayers);
                Button row = MakeButton(listContent, label, new Vector2(0f, 110f - shown * 58f),
                    new Vector2(480f, 50f), Slate, TextLight, 18,
                    () => { if (onJoin != null) onJoin(roomName); });
                row.name = "Room_" + roomName;
                shown++;
            }
        }

        if (listEmptyLabel != null) listEmptyLabel.gameObject.SetActive(shown == 0);
    }

    public void HideAll()
    {
        if (overlay == null) return;
        overlay.SetActive(false);
        hostPanel.transform.parent.gameObject.SetActive(false);
        guestPanel.transform.parent.gameObject.SetActive(false);
        listPanel.transform.parent.gameObject.SetActive(false);
        if (botPanel != null) botPanel.transform.parent.gameObject.SetActive(false);
    }

    public void Toast(string message)
    {
        if (toastLabel != null) toastLabel.text = message;
    }

    void CloseList()
    {
        HideAll();
        if (OnListClosed != null) OnListClosed();
    }

    void WireLeave(GameObject panel, Action onLeave)
    {
        Transform leave = panel.transform.Find("LeaveButton");
        if (leave == null) return;
        Button b = leave.GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        if (onLeave != null) b.onClick.AddListener(() => onLeave());
    }

    // ================== HELPERS DE CONSTRUÇÃO ==================

    // Painel central com borda dourada; retorna o painel interno (a borda fica desativada junto)
    GameObject MakePanel(float width, float height)
    {
        GameObject border = MakeImage(overlay.transform, "PanelBorder", new Vector2(width + 4f, height + 4f), PanelBorder);
        GameObject panel = MakeImage(border.transform, "Panel", new Vector2(width, height), PanelBg);
        border.SetActive(false);
        return panel;
    }

    GameObject MakeImage(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    GameObject MakeText(Transform parent, string name, string text, int fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return go;
    }

    GameObject MakeText(Transform parent, string name, string text, int fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        GameObject go = MakeText(parent, name, text, fontSize, color, alignment);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return go;
    }

    // Liga um toggle do grupo de mapas aos outros dois (comportamento de rádio:
    // marcar um desmarca os demais; desmarcar o único marcado o re-marca)
    void WireMapRadio(Toggle self, params Toggle[] others)
    {
        self.onValueChanged.AddListener(on =>
        {
            if (on)
            {
                foreach (Toggle other in others)
                    if (other != null) other.SetIsOnWithoutNotify(false);
            }
            else
            {
                // Desmarcou o único marcado: re-marca (sempre há UM mapa ativo)
                foreach (Toggle other in others)
                    if (other != null && other.isOn) return;
                self.SetIsOnWithoutNotify(true);
            }
        });
    }

    // Checkbox no estilo do lobby: caixinha + marca dourada + rótulo à direita
    Toggle MakeCheckbox(Transform parent, string label, Vector2 position, bool startOn, float width = 210f)
    {
        GameObject go = new GameObject("Chk_" + label, typeof(RectTransform), typeof(Image), typeof(Toggle));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 34f);

        // Fundo invisível: área de clique confortável (o Image recebe o raycast)
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        GameObject box = MakeImage(go.transform, "Box", new Vector2(26f, 26f), Slate);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0f, 0.5f);
        boxRt.anchorMax = new Vector2(0f, 0.5f);
        boxRt.pivot = new Vector2(0f, 0.5f);
        boxRt.anchoredPosition = new Vector2(4f, 0f);

        GameObject check = MakeImage(box.transform, "Check", new Vector2(16f, 16f), Gold);

        // Rótulo logo à direita da caixinha (posição relativa à largura do toggle)
        float labelW = width - 44f;
        MakeText(go.transform, "Label", label, 17, TextLight, TextAlignmentOptions.Left,
            new Vector2(-width / 2f + 36f + labelW / 2f, 0f), new Vector2(labelW, 30f));

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = check.GetComponent<Image>();
        toggle.isOn = startOn;
        return toggle;
    }

    Button MakeButton(Transform parent, string label, Vector2 position, Vector2 size,
        Color background, Color textColor, int fontSize, Action onClick)
    {
        GameObject go = MakeImage(parent, "Btn_" + label, size, background);
        go.GetComponent<RectTransform>().anchoredPosition = position;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();

        MakeText(go.transform, "Label", label, fontSize, textColor, TextAlignmentOptions.Center,
            Vector2.zero, size);

        if (onClick != null) button.onClick.AddListener(() => onClick());
        return button;
    }
}
