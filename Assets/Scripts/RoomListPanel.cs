using ExitGames.Client.Photon;
using Photon;
using UnityEngine;
using UnityEngine.UI;

public class RoomListPanel : UnityEngine.MonoBehaviour
{
    public Transform roomListContent; // Container onde colocar os botões das salas
    public Button roomButtonPrefab; // Prefab do botão da sala
    public Button closeButton; // Botão para fechar o painel
    private GameObject panelGO;

    void Start()
    {
        panelGO = gameObject;
        panelGO.SetActive(false); // Começa inativo

        if (closeButton != null)
            closeButton.onClick.AddListener(() => panelGO.SetActive(false));
    }

    public void ShowRoomList()
    {
        Debug.Log("[RoomList] ShowRoomList chamado!");

        // Limpa os botões antigos
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // Pega a lista de salas
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        Debug.Log($"[RoomList] Salas encontradas: {rooms.Length}");

        foreach (RoomInfo room in rooms)
        {
            Debug.Log($"[RoomList] Sala: {room.Name}, Open: {room.IsOpen}, Players: {room.PlayerCount}/{room.MaxPlayers}");
        }

        // Ativa o painel
        panelGO.SetActive(true);

        if (rooms.Length == 0)
        {
            Debug.Log("[RoomList] Nenhuma sala disponível!");
            return;
        }

        // Cria um botão para cada sala
        foreach (RoomInfo room in rooms)
        {
            if (room.IsOpen && room.PlayerCount < room.MaxPlayers) // Só mostra salas abertas e com espaço
            {
                Button newButton = Instantiate(roomButtonPrefab, roomListContent);
                Text buttonText = newButton.GetComponentInChildren<Text>();

                if (buttonText != null)
                    buttonText.text = $"{room.Name} ({room.PlayerCount}/{room.MaxPlayers})";

                // Ao clicar, entra na sala
                string roomName = room.Name;
                newButton.onClick.AddListener(() => JoinSpecificRoom(roomName));

                Debug.Log($"[RoomList] Sala adicionada: {room.Name}");
            }
        }

        panelGO.SetActive(true);
    }

    void JoinSpecificRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        Debug.Log($"[RoomList] Entrando na sala: {roomName}");
        panelGO.SetActive(false);
    }
}
