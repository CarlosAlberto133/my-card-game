using ExitGames.Client.Photon;
using Photon;
using UnityEngine;

public class PhotonGameManager : UnityEngine.MonoBehaviour
{
    public static PhotonGameManager Instance { get; private set; }

    // Informações do jogo
    public int myPlayerNumber = 0; // 1 ou 2
    public int opponentPlayerNumber = 0;
    public bool isMyTurn = false;

    // Referências
    private TurnManager turnManager;

    void Awake()
    {
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
        turnManager = TurnManager.Instance;
        
        // Sincroniza quem é P1 e P2
        SyncPlayers();
    }

    void SyncPlayers()
    {
        // Jogador que criou a sala é P1
        // Jogador que entrou é P2
        if (PhotonNetwork.isMasterClient)
        {
            myPlayerNumber = 1;
            opponentPlayerNumber = 2;
            Debug.Log("[PhotonGame] Eu sou o PLAYER 1 (criador da sala)");
        }
        else
        {
            myPlayerNumber = 2;
            opponentPlayerNumber = 1;
            Debug.Log("[PhotonGame] Eu sou o PLAYER 2 (entrei na sala)");
        }

        // Notifica o TurnManager sobre os jogadores
        if (turnManager != null)
        {
            turnManager.SetPlayers(myPlayerNumber, opponentPlayerNumber);
        }

        Debug.Log($"[PhotonGame] Sincronizado: Eu={myPlayerNumber}, Oponente={opponentPlayerNumber}");
    }

    // Método para sincronizar ataque via RPC
    public void RPC_CardAttack(int attackerCardID, int targetCardID, int damageAmount, int attackerPlayerNumber)
    {
        Debug.Log($"[PhotonGame] RPC Recebido: Carta {attackerCardID} atacou {targetCardID} com {damageAmount} de dano");

        // Se o atacante foi este jogador, não faz nada (já foi processado localmente)
        if (attackerPlayerNumber == myPlayerNumber)
            return;

        // Busca as cartas no tabuleiro
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        CardDisplay attacker = board.FindCardByInstanceID(attackerCardID);
        CardDisplay target = board.FindCardByInstanceID(targetCardID);

        if (attacker == null || target == null) return;

        // Aplica o dano (sincroniza visualmente)
        target.TakeDamage(damageAmount);
        Debug.Log($"[PhotonGame] Sincronização: {target.card.cardName} recebeu {damageAmount} de dano");
    }

    // Método para sincronizar movimento via RPC
    public void RPC_CardMove(int cardID, int newRow, int newColumn, int moverPlayerNumber)
    {
        Debug.Log($"[PhotonGame] RPC Recebido: Carta {cardID} moveu para ({newRow}, {newColumn}) [P{moverPlayerNumber}]");

        // Se quem se moveu foi este jogador, não faz nada (já foi processado localmente)
        if (moverPlayerNumber == myPlayerNumber)
            return;

        // Busca a carta no tabuleiro
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        CardDisplay card = board.FindCardByInstanceID(cardID);
        if (card == null) return;

        // Tira da tile antiga
        if (card.currentTile != null)
            card.currentTile.FreeTile();

        // Coloca na tile nova
        CardTile newTile = board.GetTile(newRow, newColumn);
        if (newTile != null)
        {
            newTile.OccupyTile(card.gameObject);
            card.currentTile = newTile;
            card.transform.position = newTile.transform.position;
            Debug.Log($"[PhotonGame] Sincronização: {card.card.cardName} moveu para ({newRow}, {newColumn})");
        }
    }

    // Método para sincronizar compra de carta via RPC
    public void RPC_BuyCard(int cardID, int cost, int buyerPlayerNumber)
    {
        Debug.Log($"[PhotonGame] RPC Recebido: Carta {cardID} foi comprada por {cost} ouro [P{buyerPlayerNumber}]");

        // Se o comprador foi este jogador, não faz nada (já foi processado localmente)
        if (buyerPlayerNumber == myPlayerNumber)
            return;

        // Aqui você pode atualizar a loja visualmente se necessário
        // Por enquanto apenas log para confirmação
        Debug.Log($"[PhotonGame] Sincronização: Player {buyerPlayerNumber} comprou carta {cardID}");
    }

    // Método para chamar RPC de ataque
    public void SendAttackRPC(int attackerCardID, int targetCardID, int damageAmount, int attackerPlayerNumber)
    {
        if (!PhotonNetwork.connected)
        {
            Debug.LogWarning("[PhotonGame] Não conectado ao Photon! RPC não enviado.");
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_CardAttack", PhotonTargets.All, attackerCardID, targetCardID, damageAmount, attackerPlayerNumber);
            Debug.Log($"[PhotonGame] Enviado RPC: Ataque {attackerCardID} -> {targetCardID} ({damageAmount} dano) [P{attackerPlayerNumber}]");
        }
    }

    // Método para chamar RPC de movimento
    public void SendMoveRPC(int cardID, int newRow, int newColumn, int moverPlayerNumber)
    {
        if (!PhotonNetwork.connected)
        {
            Debug.LogWarning("[PhotonGame] Não conectado ao Photon! RPC não enviado.");
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_CardMove", PhotonTargets.All, cardID, newRow, newColumn, moverPlayerNumber);
            Debug.Log($"[PhotonGame] Enviado RPC: Movimento {cardID} -> ({newRow}, {newColumn}) [P{moverPlayerNumber}]");
        }
    }

    // Método para chamar RPC de compra
    public void SendBuyCardRPC(int cardID, int cost, int buyerPlayerNumber)
    {
        if (!PhotonNetwork.connected)
        {
            Debug.LogWarning("[PhotonGame] Não conectado ao Photon! RPC não enviado.");
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_BuyCard", PhotonTargets.All, cardID, cost, buyerPlayerNumber);
            Debug.Log($"[PhotonGame] Enviado RPC: Compra {cardID} ({cost} ouro) [P{buyerPlayerNumber}]");
        }
    }
}
