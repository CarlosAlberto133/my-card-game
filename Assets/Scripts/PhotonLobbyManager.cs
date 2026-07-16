using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Fluxo do lobby:
// - Create Room  -> cria sala com nome único e abre popup de espera (anfitrião)
// - Join Game    -> abre popup com a lista de salas abertas; clicar em uma entra nela
// - Anfitrião inicia a partida quando o jogador 2 entrar (via room property "start",
//   sem RPC novo — não mexe no RpcList nem na sincronização do jogo)
// - Fechar Jogo  -> sai do aplicativo
public class PhotonLobbyManager : UnityEngine.MonoBehaviour
{
    public Button createRoomButton;
    public Button joinRoomButton;
    public RoomListPanel roomListPanel; // legado (painel antigo da cena; não é mais usado)

    private LobbyUI ui;
    private bool loadingGame = false;
    private bool retriedCreate = false;

    private enum State { Idle, Browsing, HostWaiting, GuestWaiting }
    private State state = State.Idle;
    private bool startingBotMode = false;
    private Button quitButtonRef;
    private Button botButtonRef;

    void Start()
    {
        // Voltando de um treino contra o bot: desliga o modo offline do Photon
        // ANTES de qualquer coisa, para reconectar ao multiplayer normal
        BotMode.Enabled = false;
        if (PhotonNetwork.offlineMode)
        {
            if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
            PhotonNetwork.offlineMode = false;
        }

        // Cenário de taverna/mesa de RPG (tema do jogo) montado por código
        LobbyDecor.Build();

        // Precisamos estar no lobby do Photon para receber a lista de salas
        PhotonNetwork.autoJoinLobby = true;

        if (!PhotonNetwork.connected)
        {
            // gameVersion separa builds incompatíveis no matchmaking. "3.9" =
            // rebalanceamento geral de status das 80 cartas (identidade de
            // classe: tank tanque, arqueiro vidro, healer suporte). "3.8" =
            // travar loja + partidas/logs no Supabase.
            // Builds antigos simulariam outro resultado → desync.
            PhotonNetwork.ConnectUsingSettings("3.9");
            Debug.Log("[Lobby] Conectando ao Photon...");
        }
        else if (PhotonNetwork.inRoom)
        {
            // Voltou para o lobby ainda dentro de uma sala: sai dela primeiro
            PhotonNetwork.LeaveRoom();
        }
        else if (!PhotonNetwork.insideLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        // Botões extras ANTES dos popups: quem entra por último no Canvas
        // desenha por cima, e os popups precisam cobrir os botões
        CreateQuitButton();
        CreateBotTrainingButton();

        // Constrói os popups (ficam ocultos até serem usados)
        Canvas canvas = createRoomButton != null
            ? createRoomButton.GetComponentInParent<Canvas>()
            : FindObjectOfType<Canvas>();
        ui = new LobbyUI(canvas);
        ui.OnListClosed = () => { state = State.Idle; };

        // Título do jogo + painel de perfil (estatísticas da conta do launcher)
        LobbyProfileUI.Build(canvas);

        // Menu no tema da taverna: tabuleta de madeira + botões arredondados
        // com moldura dourada, empilhados numa coluna organizada
        LayoutMenuColumn();

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(CreateRoom);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OpenRoomList);

        UpdateMainButtons();

        if (!PhotonNetwork.insideLobby)
            ui.Toast("Conectando ao servidor...");
    }

    // Habilita Create/Join somente quando estamos no lobby do Photon
    void UpdateMainButtons()
    {
        bool ready = PhotonNetwork.insideLobby;
        if (createRoomButton != null) createRoomButton.interactable = ready;
        if (joinRoomButton != null) joinRoomButton.interactable = ready;
    }

    // Botão "Fechar Jogo" clonado do Create Room para manter o mesmo visual
    void CreateQuitButton()
    {
        if (createRoomButton == null) return;

        GameObject clone = Instantiate(createRoomButton.gameObject, createRoomButton.transform.parent);
        clone.name = "QuitButton";

        RectTransform sourceRt = createRoomButton.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -116f);

        // Fica logo após o Create Room na hierarquia — nunca por cima dos popups,
        // que são adicionados no fim do Canvas
        clone.transform.SetSiblingIndex(createRoomButton.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "Fechar Jogo";
        }
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Fechar Jogo";
        }

        Button quitButton = clone.GetComponent<Button>();
        quitButton.interactable = true; // funciona mesmo sem conexão
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(QuitGame);
        quitButtonRef = quitButton;
    }

    void QuitGame()
    {
        Debug.Log("[Lobby] Fechando o jogo...");
        Application.Quit();
    }

    // Monta a coluna do menu no tema taverna: uma tabuleta de madeira atrás e
    // os 4 botões (Criar Sala em dourado; os demais em madeira escura) com
    // cantos arredondados, moldura dourada e realce ao passar o mouse.
    // Ancora tudo na posição original do Create Room (referência da cena).
    void LayoutMenuColumn()
    {
        if (createRoomButton == null) return;

        RectTransform baseRt = createRoomButton.GetComponent<RectTransform>();
        Vector2 basePos = baseRt.anchoredPosition;
        Vector2 btnSize = new Vector2(340f, 62f);
        float gap = 80f;

        // Tabuleta de fundo (desenhada antes dos botões = atrás deles)
        GameObject board = new GameObject("MenuBoard", typeof(RectTransform), typeof(Image));
        board.transform.SetParent(createRoomButton.transform.parent, false);
        board.transform.SetSiblingIndex(createRoomButton.transform.GetSiblingIndex());
        RectTransform brt = board.GetComponent<RectTransform>();
        brt.anchorMin = baseRt.anchorMin;
        brt.anchorMax = baseRt.anchorMax;
        brt.pivot = baseRt.pivot;
        brt.anchoredPosition = basePos + new Vector2(0f, -gap * 1.5f);
        brt.sizeDelta = new Vector2(btnSize.x + 64f, gap * 3f + btnSize.y + 60f);
        Image boardImg = board.GetComponent<Image>();
        LobbySprites.MakeRounded(boardImg, new Color(0.10f, 0.07f, 0.045f, 0.90f));
        boardImg.raycastTarget = false;
        LobbySprites.AddRing(board.transform, new Color(0.96f, 0.77f, 0.32f, 0.35f));

        StyleMenuButton(createRoomButton, "Criar Sala", true, basePos, btnSize, baseRt);
        StyleMenuButton(joinRoomButton, "Procurar Salas", false, basePos + new Vector2(0f, -gap), btnSize, baseRt);
        StyleMenuButton(botButtonRef, "Treinar vs Bot", false, basePos + new Vector2(0f, -gap * 2f), btnSize, baseRt);
        StyleMenuButton(quitButtonRef, "Fechar Jogo", false, basePos + new Vector2(0f, -gap * 3f), btnSize, baseRt);
    }

    // Reconstrói o visual de um botão: sprite arredondado, moldura dourada,
    // rótulo em português e cores de hover/clique via ColorBlock
    void StyleMenuButton(Button b, string label, bool primary, Vector2 pos, Vector2 size, RectTransform anchorRef)
    {
        if (b == null) return;

        Color gold = new Color(0.96f, 0.77f, 0.32f);
        Color woodDark = new Color(0.16f, 0.115f, 0.075f, 0.98f);
        Color textDark = new Color(0.14f, 0.10f, 0.03f);
        Color textLight = new Color(0.95f, 0.91f, 0.82f);
        Color fill = primary ? gold : woodDark;

        // Mesmo referencial de âncora do Create Room (posições consistentes)
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = anchorRef.anchorMin;
        rt.anchorMax = anchorRef.anchorMax;
        rt.pivot = anchorRef.pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = b.GetComponent<Image>();
        if (img != null)
        {
            LobbySprites.MakeRounded(img, fill);

            // Moldura dourada (só uma; o layout roda 1x por carga da cena)
            if (b.transform.Find("Ring") == null)
                LobbySprites.AddRing(b.transform, primary
                    ? new Color(1f, 0.92f, 0.66f, 0.9f)
                    : new Color(0.96f, 0.77f, 0.32f, 0.55f));

            // Hover/clique: clareia/escurece a cor do próprio botão
            b.targetGraphic = img;
            b.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = b.colors;
            cb.normalColor = fill;
            cb.highlightedColor = Color.Lerp(fill, Color.white, 0.18f);
            cb.pressedColor = Color.Lerp(fill, Color.black, 0.22f);
            cb.selectedColor = fill;
            cb.disabledColor = new Color(fill.r, fill.g, fill.b, 0.35f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
        }

        TMP_Text tmp = b.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
            tmp.color = primary ? textDark : textLight;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 22f;
            tmp.characterSpacing = 2f;
        }
        else
        {
            Text legacy = b.GetComponentInChildren<Text>();
            if (legacy != null)
            {
                legacy.text = label;
                legacy.color = primary ? textDark : textLight;
                legacy.fontStyle = FontStyle.Bold;
            }
        }
    }

    // ================== TREINO CONTRA O BOT ==================

    // Botão "Treinar vs Bot" clonado do Create Room (mesmo visual), na mesma
    // coluna dele: 2 posições abaixo (-232), logo após o "Fechar Jogo" (-116).
    // NÃO usar a posição do Join como referência: dependendo do layout da cena,
    // "join - 116" cai em cima do Create Room e esconde o botão.
    void CreateBotTrainingButton()
    {
        Button source = createRoomButton != null ? createRoomButton : joinRoomButton;
        if (source == null) return;

        GameObject clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = "BotTrainingButton";

        RectTransform sourceRt = source.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -232f);
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "Treinar vs Bot";
        }
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Treinar vs Bot";
        }

        Button botButton = clone.GetComponent<Button>();
        botButton.interactable = true; // treino é offline: funciona sem conexão
        botButton.onClick.RemoveAllListeners();
        botButton.onClick.AddListener(StartTrainingVsBot);
        botButtonRef = botButton;
    }

    // Inicia uma partida offline contra o bot: liga o offlineMode do Photon
    // (os mesmos RPCs executam localmente — o jogo não percebe diferença) e
    // carrega a cena da partida direto, sem sala online
    void StartTrainingVsBot()
    {
        if (loadingGame || startingBotMode) return;
        startingBotMode = true;
        BotMode.Enabled = true;

        ui.Toast("Preparando o treino contra o bot...");
        StartCoroutine(BotModeRoutine());
    }

    System.Collections.IEnumerator BotModeRoutine()
    {
        // O offlineMode só pode ligar DESCONECTADO do Photon
        if (PhotonNetwork.connected && !PhotonNetwork.offlineMode)
        {
            PhotonNetwork.Disconnect();
            float t = 0f;
            while (PhotonNetwork.connected && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        PhotonNetwork.offlineMode = true;

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        PhotonNetwork.CreateRoom("Treino vs Bot", options, null);

        // Mapa: mesmo padrão do multiplayer (Mesa de RPG). A sala offline aceita
        // room properties normalmente — a cena do jogo lê "theme" como sempre
        Hashtable props = new Hashtable();
        props["theme"] = ui != null ? ui.SelectedMapTheme : 1;
        if (PhotonNetwork.room != null) PhotonNetwork.room.SetCustomProperties(props);

        LoadGameScene();
    }

    // ================== CRIAR SALA ==================

    void CreateRoom()
    {
        if (!PhotonNetwork.insideLobby)
        {
            ui.Toast("Ainda conectando ao servidor, aguarde...");
            return;
        }

        string roomName = "Sala #" + Random.Range(100, 1000);
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;

        bool success = PhotonNetwork.CreateRoom(roomName, options, null);
        Debug.Log($"[Lobby] Criando sala: {roomName}, Success: {success}");
        if (success) ui.Toast("Criando sala...");
    }

    // Nome sorteado já existia (raro): tenta uma vez com outro número
    void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        Debug.LogWarning("[Lobby] Falha ao criar sala.");
        if (!retriedCreate)
        {
            retriedCreate = true;
            CreateRoom();
        }
        else
        {
            retriedCreate = false;
            ui.Toast("Não foi possível criar a sala. Tente novamente.");
        }
    }

    // ================== LISTA DE SALAS / ENTRAR ==================

    void OpenRoomList()
    {
        if (!PhotonNetwork.insideLobby)
        {
            ui.Toast("Ainda conectando ao servidor, aguarde...");
            return;
        }

        state = State.Browsing;
        ui.Toast("");
        ui.ShowRoomList();
        RefreshRoomList();
    }

    void RefreshRoomList()
    {
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        Debug.Log($"[Lobby] Salas na lista: {rooms.Length}");
        ui.PopulateRoomList(rooms, JoinSpecificRoom);
    }

    void JoinSpecificRoom(string roomName)
    {
        Debug.Log($"[Lobby] Entrando na sala: {roomName}");
        ui.Toast("Entrando na sala...");
        PhotonNetwork.JoinRoom(roomName);
    }

    void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        Debug.LogWarning("[Lobby] Falha ao entrar na sala.");
        ui.Toast("Não foi possível entrar (a sala pode ter acabado de fechar).");
        if (state == State.Browsing) RefreshRoomList();
    }

    // ================== CALLBACKS DO PHOTON ==================

    void OnJoinedLobby()
    {
        Debug.Log("[Lobby] Entrou no lobby do Photon.");
        UpdateMainButtons();
        if (state == State.Idle) ui.Toast("");
        if (state == State.Browsing) RefreshRoomList();
    }

    // O Photon atualiza a lista de salas sozinho enquanto estamos no lobby
    void OnReceivedRoomListUpdate()
    {
        if (state == State.Browsing) RefreshRoomList();
    }

    void OnJoinedRoom()
    {
        // Sala offline do treino contra o bot: o fluxo próprio cuida de tudo
        // (nada de popup de anfitrião/espera)
        if (BotMode.Enabled) return;

        string roomName = PhotonNetwork.room != null ? PhotonNetwork.room.Name : "Sala";
        Debug.Log($"[Lobby] Entrou na sala: {roomName} (master: {PhotonNetwork.isMasterClient})");
        ui.Toast("");

        if (PhotonNetwork.isMasterClient)
        {
            // Criador da sala: espera o jogador 2 e inicia (vira o P1 da partida)
            state = State.HostWaiting;
            ui.ShowHostRoom(roomName, StartMatch, LeaveRoom);
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
        }
        else
        {
            // Convidado: espera o anfitrião iniciar (vira o P2 da partida)
            state = State.GuestWaiting;
            ui.ShowGuestRoom(roomName, LeaveRoom);
        }
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log($"[Lobby] Jogador entrou na sala: {newPlayer.ID}");
        if (state == State.HostWaiting)
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
    }

    void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log($"[Lobby] Jogador saiu da sala: {otherPlayer.ID}");
        if (state == State.HostWaiting)
        {
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
            ui.Toast("O jogador 2 saiu da sala.");
        }
    }

    // Se o anfitrião sair, o convidado não deve herdar a sala: volta para o lobby
    void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        if (state == State.GuestWaiting)
        {
            Debug.Log("[Lobby] O anfitrião saiu; deixando a sala.");
            LeaveRoom();
            ui.Toast("O anfitrião fechou a sala.");
        }
    }

    void LeaveRoom()
    {
        state = State.Idle;
        ui.HideAll();
        if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
        UpdateMainButtons(); // reabilitados quando OnJoinedLobby disparar de novo
    }

    void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] Falha ao conectar: {cause}");
        ui.Toast("Falha na conexão. Verifique sua internet e reabra o jogo.");
    }

    void OnConnectionFail(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] Conexão perdida: {cause}");
        ui.Toast("Conexão perdida com o servidor.");
    }

    // ================== INICIAR PARTIDA ==================

    // Anfitrião clicou em "Iniciar Partida"
    void StartMatch()
    {
        if (PhotonNetwork.room == null || PhotonNetwork.room.PlayerCount < 2)
        {
            ui.Toast("Aguarde o jogador 2 entrar!");
            return;
        }

        Debug.Log("[Lobby] Iniciando a partida!");

        // Tranca a sala: ninguém mais entra e ela some da lista
        PhotonNetwork.room.IsOpen = false;
        PhotonNetwork.room.IsVisible = false;

        // Room property avisa os DOIS clientes para carregar o jogo
        // (chega ao anfitrião também, mas LoadGameScene é protegido contra repetição).
        // "theme" leva o mapa escolhido pelo anfitrião (0 = Espaço, 1 = Mesa de RPG)
        Hashtable props = new Hashtable();
        props["start"] = 1;
        props["theme"] = ui != null ? ui.SelectedMapTheme : 1;
        PhotonNetwork.room.SetCustomProperties(props);

        LoadGameScene();
    }

    // Dispara nos dois clientes quando o anfitrião marca "start" na sala
    void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey("start"))
        {
            Debug.Log("[Lobby] Partida iniciada pelo anfitrião!");
            LoadGameScene();
        }
    }

    void LoadGameScene()
    {
        if (loadingGame) return; // evita carregar duas vezes (clique + callback)
        loadingGame = true;
        Debug.Log("[Lobby] Carregando SampleScene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
