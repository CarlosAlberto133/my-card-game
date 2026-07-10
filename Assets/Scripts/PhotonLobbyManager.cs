using ExitGames.Client.Photon;
using Photon;
using UnityEngine;
using UnityEngine.UI;

public class PhotonLobbyManager : UnityEngine.MonoBehaviour
{
    public Button createRoomButton;
    public Button joinRoomButton;
    public RoomListPanel roomListPanel;
    private bool isConnected = false;
    private float showRoomListDelay = 0f;
    private float readyDelay = 0f;

    void Start()
    {
        // Conecta ao Photon quando a cena carrega
        if (!PhotonNetwork.connected)
        {
            PhotonNetwork.ConnectUsingSettings("1.0");
            Debug.Log("[Lobby] Conectando ao Photon...");
        }
        else
        {
            isConnected = true;
            readyDelay = 3f;
        }

        // Atribui os botões aos métodos
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(CreateRoom);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(JoinRoom);
    }

    void Update()
    {
        // Verifica se conectou
        if (!isConnected && PhotonNetwork.connected)
        {
            isConnected = true;
            Debug.Log("[Lobby] Conectado ao Photon com sucesso!");
            readyDelay = 5f; // Aguarda 5 segundos antes de permitir operações
        }

        // Aguarda delay para ficar pronto
        if (readyDelay > 0)
        {
            readyDelay -= Time.deltaTime;
        }

        // Aguarda delay para mostrar lista de salas
        if (showRoomListDelay > 0)
        {
            showRoomListDelay -= Time.deltaTime;
            if (showRoomListDelay <= 0)
            {
                if (roomListPanel != null)
                    roomListPanel.ShowRoomList();
            }
        }
    }

    // Cria uma nova sala
    void CreateRoom()
    {
        if (!isConnected)
        {
            Debug.LogWarning("[Lobby] Ainda não conectou ao Photon!");
            return;
        }

        if (readyDelay > 0)
        {
            Debug.LogWarning("[Lobby] Ainda não está pronto para criar sala! Aguarde...");
            return;
        }

        // Usa nome fixo para funcionar entre Build e Editor
        string roomName = "CardGame_TestRoom";
        bool success = PhotonNetwork.CreateRoom(roomName, null, null, null);

        Debug.Log($"[Lobby] Criando sala: {roomName}, Success: {success}");

        if (!success)
        {
            Debug.LogWarning("[Lobby] Falha ao criar sala! Pode já existir. Tentando entrar...");
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    // Entra na sala conhecida
    void JoinRoom()
    {
        if (!isConnected)
        {
            Debug.LogWarning("[Lobby] Ainda não conectou ao Photon!");
            return;
        }

        if (readyDelay > 0)
        {
            Debug.LogWarning("[Lobby] Ainda não está pronto para entrar na sala! Aguarde...");
            return;
        }

        // Usa mesmo nome fixo para funcionar entre Build e Editor
        string roomName = "CardGame_TestRoom";
        bool success = PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[Lobby] Tentando entrar na sala {roomName}, Success: {success}");
    }

    // Callback quando entra em uma sala
    public void OnJoinedRoom()
    {
        Debug.Log("[Lobby] Entrou na sala com sucesso!");

        // Aguarda 2 segundos e carrega a cena do jogo
        Invoke("LoadGameScene", 2f);
    }

    // Carrega a cena do jogo
    void LoadGameScene()
    {
        Debug.Log("[Lobby] Carregando SampleScene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
