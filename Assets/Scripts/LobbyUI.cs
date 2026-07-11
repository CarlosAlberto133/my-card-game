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

    private TMP_Text hostTitle;
    private TMP_Text hostStatus;
    private Button startButton;
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
        hostPanel = MakePanel(540f, 380f);

        hostTitle = MakeText(hostPanel.transform, "Title", "Sala", 32, Gold, TextAlignmentOptions.Center,
            new Vector2(0f, 140f), new Vector2(500f, 44f)).GetComponent<TMP_Text>();
        hostTitle.fontStyle = FontStyles.Bold;

        MakeImage(hostPanel.transform, "Divider", new Vector2(460f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 112f);

        MakeText(hostPanel.transform, "You", "Você está na sala  (anfitrião)", 19, TextLight,
            TextAlignmentOptions.Center, new Vector2(0f, 70f), new Vector2(500f, 30f));

        hostStatus = MakeText(hostPanel.transform, "Status", "Aguardando o jogador 2 entrar...", 19, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 32f), new Vector2(500f, 30f)).GetComponent<TMP_Text>();

        startButton = MakeButton(hostPanel.transform, "Iniciar Partida", new Vector2(0f, -46f),
            new Vector2(260f, 56f), Gold, GoldTextDark, 21, null);
        startButton.interactable = false;

        MakeButton(hostPanel.transform, "Sair da Sala", new Vector2(0f, -126f),
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

    // ================== API PARA O MANAGER ==================

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
