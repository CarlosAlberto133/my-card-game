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

    void Start()
    {
        // Mesmo fundo espacial do jogo (versão de frente para a câmera do lobby)
        SpaceBackground.EnsureFacingCamera();

        // Precisamos estar no lobby do Photon para receber a lista de salas
        PhotonNetwork.autoJoinLobby = true;

        if (!PhotonNetwork.connected)
        {
            // gameVersion separa builds incompatíveis no matchmaking. "3.6" =
            // porcentagens de tier na loja (TierOdds), reinício sincronizado
            // (novos RPCs RPC_RestartGame/RPC_RequestRestart), Archer 1/3 e a
            // tela de vitória por código. "3.5" = pacote grande de correções de
            // efeitos. Builds antigos simulariam outro resultado → desync.
            PhotonNetwork.ConnectUsingSettings("3.6");
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

        // Botão "Fechar Jogo" ANTES dos popups: quem entra por último no Canvas
        // desenha por cima, e os popups precisam cobrir o botão
        CreateQuitButton();

        // Constrói os popups (ficam ocultos até serem usados)
        Canvas canvas = createRoomButton != null
            ? createRoomButton.GetComponentInParent<Canvas>()
            : FindObjectOfType<Canvas>();
        ui = new LobbyUI(canvas);
        ui.OnListClosed = () => { state = State.Idle; };

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
    }

    void QuitGame()
    {
        Debug.Log("[Lobby] Fechando o jogo...");
        Application.Quit();
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
