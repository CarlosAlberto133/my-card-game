using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Regras do Jogo")]
    public int maxPlacementRows = 2; // Apenas 2 primeiras fileiras
    public int movementRange = 1; // Distância de movimento em formato de +

    private GameObject selectedCard;
    private CardDisplay selectedCardDisplay;
    private CardTile currentTile; // Tile onde a carta selecionada está
    private HandManager handManager;
    private BoardManager boardManager;
    private bool isMovingCard = false; // Se está movendo uma carta do tabuleiro

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
        handManager = FindObjectOfType<HandManager>();
        boardManager = FindObjectOfType<BoardManager>();
    }

    // Seleciona uma carta da mão para colocar no tabuleiro
    public void SelectCardFromHand(GameObject card, CardDisplay cardDisplay)
    {
        CancelSelection(); // Cancela qualquer seleção anterior

        selectedCard = card;
        selectedCardDisplay = cardDisplay;
        isMovingCard = false;

        Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada da mão. Clique em um tile válido (2 primeiras fileiras) para colocar.");
    }

    // Seleciona uma carta que já está no tabuleiro para mover
    public void SelectCardFromBoard(GameObject card, CardDisplay cardDisplay, CardTile tile)
    {
        CancelSelection(); // Cancela qualquer seleção anterior

        selectedCard = card;
        selectedCardDisplay = cardDisplay;
        currentTile = tile;
        isMovingCard = true;

        Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada para mover. Clique em um tile adjacente (+) para mover.");
    }

    // Tenta colocar a carta selecionada em um tile
    public bool TryPlaceCardOnTile(CardTile tile)
    {
        if (selectedCard == null || selectedCardDisplay == null)
        {
            Debug.Log("Nenhuma carta selecionada.");
            return false;
        }

        // Se está movendo uma carta do tabuleiro
        if (isMovingCard)
        {
            return TryMoveCard(tile);
        }

        // Se está colocando uma carta da mão
        // Verifica se o tile está nas 2 primeiras fileiras
        if (tile.row >= maxPlacementRows)
        {
            Debug.Log($"Tile inválido! Você só pode colocar cartas nas {maxPlacementRows} primeiras fileiras.");
            return false;
        }

        // Verifica se o tile já está ocupado
        if (tile.occupiedCard != null)
        {
            Debug.Log("Este tile já está ocupado!");
            return false;
        }

        // Coloca a carta no tile
        PlaceCard(tile);
        return true;
    }

    // Tenta mover uma carta do tabuleiro
    bool TryMoveCard(CardTile targetTile)
    {
        if (currentTile == null)
        {
            Debug.Log("Erro: tile atual não encontrado!");
            return false;
        }

        // Não pode mover para o mesmo tile
        if (targetTile == currentTile)
        {
            Debug.Log("Você já está neste tile!");
            CancelSelection();
            return false;
        }

        // Verifica se o tile está ocupado
        if (targetTile.occupiedCard != null)
        {
            Debug.Log("Este tile já está ocupado!");
            return false;
        }

        // Verifica se o movimento é válido (formato de +)
        if (!IsValidMovement(currentTile, targetTile))
        {
            Debug.Log("Movimento inválido! Você só pode mover em formato de + (cima, baixo, esquerda, direita).");
            return false;
        }

        // Move a carta
        MoveCard(targetTile);
        return true;
    }

    // Verifica se o movimento é válido (formato de +)
    bool IsValidMovement(CardTile from, CardTile to)
    {
        int rowDiff = Mathf.Abs(to.row - from.row);
        int colDiff = Mathf.Abs(to.column - from.column);

        // Movimento em cruz: ou move em linha reta na horizontal ou vertical, mas não diagonal
        // E deve estar dentro do alcance (movementRange)
        if (rowDiff == 0 && colDiff > 0 && colDiff <= movementRange)
        {
            // Movimento horizontal
            return true;
        }
        else if (colDiff == 0 && rowDiff > 0 && rowDiff <= movementRange)
        {
            // Movimento vertical
            return true;
        }

        return false;
    }

    // Move a carta de um tile para outro
    void MoveCard(CardTile targetTile)
    {
        // Libera o tile atual
        currentTile.FreeTile();

        // Move a carta para a nova posição
        Vector3 cardPosition = targetTile.transform.position + new Vector3(0, 0.6f, 0);
        selectedCard.transform.position = cardPosition;

        // Ocupa o novo tile
        targetTile.OccupyTile(selectedCard);

        Debug.Log($"Carta '{selectedCardDisplay.card.cardName}' movida de [{currentTile.row},{currentTile.column}] para [{targetTile.row},{targetTile.column}]");

        // Limpa a seleção
        selectedCard = null;
        selectedCardDisplay = null;
        currentTile = null;
        isMovingCard = false;
    }

    void PlaceCard(CardTile tile)
    {
        // Remove da mão
        if (handManager != null)
        {
            handManager.RemoveCardFromHand(selectedCard);
        }

        // Move a carta para a posição do tile
        Vector3 cardPosition = tile.transform.position + new Vector3(0, 0.6f, 0); // Um pouco acima do tile
        selectedCard.transform.position = cardPosition;

        // Marca o tile como ocupado
        tile.OccupyTile(selectedCard);

        // Atualiza o estado da carta
        selectedCardDisplay.isInHand = false;
        selectedCardDisplay.isOnBoard = true;

        Debug.Log($"Carta '{selectedCardDisplay.card.cardName}' colocada no tile [{tile.row}, {tile.column}]");

        // Limpa a seleção
        selectedCard = null;
        selectedCardDisplay = null;
    }

    // Cancela a seleção atual
    public void CancelSelection()
    {
        selectedCard = null;
        selectedCardDisplay = null;
        Debug.Log("Seleção cancelada.");
    }

    public bool HasSelectedCard()
    {
        return selectedCard != null;
    }
}
