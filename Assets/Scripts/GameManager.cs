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

    void Update()
    {
        // Verifica tecla T para atacar a torre
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            TryAttackTower();
        }
    }

    // Seleciona uma carta da mão para colocar no tabuleiro
    public void SelectCardFromHand(GameObject card, CardDisplay cardDisplay)
    {
        // Verifica se a carta pertence ao jogador atual
        if (TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;
            if (cardDisplay.ownerPlayerNumber != 0 && cardDisplay.ownerPlayerNumber != currentPlayerNumber)
            {
                Debug.Log($"Esta carta pertence ao Jogador {cardDisplay.ownerPlayerNumber}! Você é o Jogador {currentPlayerNumber}.");
                return;
            }
        }

        CancelSelection(); // Cancela qualquer seleção anterior

        selectedCard = card;
        selectedCardDisplay = cardDisplay;
        isMovingCard = false;

        HighlightValidTiles(); // Destaca tiles válidos/inválidos
        Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada da mão. Clique em um tile válido (2 primeiras fileiras) para colocar.");
    }

    // Seleciona uma carta que já está no tabuleiro para mover
    public void SelectCardFromBoard(GameObject card, CardDisplay cardDisplay, CardTile tile)
    {
        // Verifica se a carta pertence ao jogador atual
        if (TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;
            if (cardDisplay.ownerPlayerNumber != 0 && cardDisplay.ownerPlayerNumber != currentPlayerNumber)
            {
                Debug.Log($"Esta carta pertence ao Jogador {cardDisplay.ownerPlayerNumber}! Você é o Jogador {currentPlayerNumber}.");
                return;
            }
        }

        // Verifica se a carta pode se mover neste round
        if (!cardDisplay.CanMoveThisRound())
        {
            Debug.Log($"Esta carta já se moveu neste round! Ela só pode se mover novamente no próximo round.");
            return;
        }

        CancelSelection(); // Cancela qualquer seleção anterior

        selectedCard = card;
        selectedCardDisplay = cardDisplay;
        currentTile = tile;
        isMovingCard = true;

        HighlightValidTiles(); // Destaca tiles válidos/inválidos

        // Verifica se a carta está na última linha e pode atacar a torre
        int totalRows = boardManager != null ? boardManager.rows : 12;
        bool canAttackTower = false;

        if (cardDisplay.ownerPlayerNumber == 1 && tile.row == totalRows - 1)
        {
            canAttackTower = true;
            Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada. Pressione 'T' para atacar a torre do Jogador 2!");
        }
        else if (cardDisplay.ownerPlayerNumber == 2 && tile.row == 0)
        {
            canAttackTower = true;
            Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada. Pressione 'T' para atacar a torre do Jogador 1!");
        }

        if (!canAttackTower)
        {
            Debug.Log($"Carta '{cardDisplay.card.cardName}' selecionada para mover. Clique em um tile adjacente (+) para mover.");
        }
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
        // Verifica se o tile está nas fileiras válidas PARA O JOGADOR
        int playerNumber = selectedCardDisplay.ownerPlayerNumber;
        int totalRows = boardManager != null ? boardManager.rows : 12;
        bool isValidRow = false;

        if (playerNumber == 1)
        {
            // Jogador 1: primeiras 2 fileiras (0 e 1)
            isValidRow = tile.row < maxPlacementRows;
            if (!isValidRow)
            {
                Debug.Log($"Jogador 1 só pode colocar cartas nas {maxPlacementRows} primeiras fileiras (0-{maxPlacementRows - 1})!");
            }
        }
        else if (playerNumber == 2)
        {
            // Jogador 2: últimas 2 fileiras (10 e 11 em um tabuleiro 12x12)
            int minRow = totalRows - maxPlacementRows; // 12 - 2 = 10
            isValidRow = tile.row >= minRow;
            if (!isValidRow)
            {
                Debug.Log($"Jogador 2 só pode colocar cartas nas {maxPlacementRows} últimas fileiras ({minRow}-{totalRows - 1})!");
            }
        }
        else
        {
            Debug.LogError($"Carta sem dono válido! ownerPlayerNumber: {playerNumber}");
            return false;
        }

        if (!isValidRow)
        {
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

        // Move a carta para a nova posição (altura maior para não enterrar no chão)
        Vector3 cardPosition = targetTile.transform.position + new Vector3(0, 1.5f, 0);
        selectedCard.transform.position = cardPosition;

        // Ocupa o novo tile
        targetTile.OccupyTile(selectedCard);

        // Atualiza a referência do tile na carta
        selectedCardDisplay.currentTile = targetTile;

        // Marca que a carta se moveu neste round
        if (TurnManager.Instance != null)
        {
            selectedCardDisplay.lastMovedRound = TurnManager.Instance.currentRound;
            Debug.Log($"Carta '{selectedCardDisplay.card.cardName}' movida de [{currentTile.row},{currentTile.column}] para [{targetTile.row},{targetTile.column}]. Não poderá se mover novamente neste round.");
        }
        else
        {
            Debug.Log($"Carta '{selectedCardDisplay.card.cardName}' movida de [{currentTile.row},{currentTile.column}] para [{targetTile.row},{targetTile.column}]");
        }

        // Limpa os destaques
        ClearTileHighlights();

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

        // Move a carta para a posição do tile (altura maior para não enterrar no chão)
        Vector3 cardPosition = tile.transform.position + new Vector3(0, 1.5f, 0);
        selectedCard.transform.position = cardPosition;

        // Marca o tile como ocupado
        tile.OccupyTile(selectedCard);

        // Atualiza o estado da carta
        selectedCardDisplay.isInHand = false;
        selectedCardDisplay.isOnBoard = true;
        selectedCardDisplay.currentTile = tile; // Armazena referência do tile

        Debug.Log($"Carta '{selectedCardDisplay.card.cardName}' colocada no tile [{tile.row}, {tile.column}]");

        // Limpa os destaques
        ClearTileHighlights();

        // Limpa a seleção
        selectedCard = null;
        selectedCardDisplay = null;
    }

    // Cancela a seleção atual
    public void CancelSelection()
    {
        ClearTileHighlights(); // Remove os destaques dos tiles
        selectedCard = null;
        selectedCardDisplay = null;
        currentTile = null;
        isMovingCard = false;
        Debug.Log("Seleção cancelada.");
    }

    public bool HasSelectedCard()
    {
        return selectedCard != null;
    }

    // Verifica se um tile é válido para a seleção atual
    public bool IsValidTileForCurrentSelection(CardTile tile)
    {
        if (selectedCard == null || selectedCardDisplay == null) return false;

        // Se está movendo uma carta do tabuleiro
        if (isMovingCard)
        {
            if (currentTile == null) return false;
            if (tile == currentTile) return false; // Não pode mover para o mesmo lugar
            if (tile.occupiedCard != null) return false; // Não pode mover para tile ocupado
            return IsValidMovement(currentTile, tile); // Verifica movimento em +
        }
        else
        {
            // Se está colocando da mão
            if (tile.occupiedCard != null) return false; // Não pode colocar em tile ocupado

            // Verifica fileiras válidas por jogador
            int playerNumber = selectedCardDisplay.ownerPlayerNumber;
            int totalRows = boardManager != null ? boardManager.rows : 12;

            if (playerNumber == 1)
            {
                // Jogador 1: primeiras 2 fileiras (0 e 1)
                return tile.row < maxPlacementRows;
            }
            else if (playerNumber == 2)
            {
                // Jogador 2: últimas 2 fileiras (10 e 11)
                int minRow = totalRows - maxPlacementRows;
                return tile.row >= minRow;
            }

            return false;
        }
    }

    // Destaca todos os tiles do tabuleiro (verde = válido, vermelho = inválido)
    void HighlightValidTiles()
    {
        if (boardManager == null) return;

        CardTile[] allTiles = FindObjectsOfType<CardTile>();
        foreach (CardTile tile in allTiles)
        {
            bool isValid = IsValidTileForCurrentSelection(tile);
            tile.SetHighlight(isValid);
        }
    }

    // Remove o destaque de todos os tiles
    void ClearTileHighlights()
    {
        CardTile[] allTiles = FindObjectsOfType<CardTile>();
        foreach (CardTile tile in allTiles)
        {
            tile.ClearHighlight();
        }
    }

    // Tenta atacar uma carta inimiga se ela for adjacente à carta selecionada
    public bool TryAttackEnemyCard(CardDisplay targetCard)
    {
        // Verifica se há uma carta selecionada no tabuleiro
        if (selectedCard == null || selectedCardDisplay == null)
        {
            Debug.Log("Nenhuma carta selecionada para atacar.");
            return false;
        }

        // Só pode atacar se a carta selecionada estiver no tabuleiro
        if (!selectedCardDisplay.isOnBoard)
        {
            Debug.Log("A carta selecionada não está no tabuleiro.");
            return false;
        }

        // Verifica se a carta alvo é inimiga
        if (targetCard.ownerPlayerNumber == selectedCardDisplay.ownerPlayerNumber)
        {
            Debug.Log("Não pode atacar suas próprias cartas!");
            return false;
        }

        if (targetCard.ownerPlayerNumber == 0)
        {
            Debug.Log("Não pode atacar cartas da loja!");
            return false;
        }

        // Verifica se a carta alvo está na lista de inimigos adjacentes
        System.Collections.Generic.List<CardDisplay> adjacentEnemies = selectedCardDisplay.GetAdjacentEnemies();

        if (!adjacentEnemies.Contains(targetCard))
        {
            Debug.Log($"{targetCard.card.cardName} não é adjacente a {selectedCardDisplay.card.cardName}!");
            return false;
        }

        // Executa o ataque
        Debug.Log($"Atacando {targetCard.card.cardName} com {selectedCardDisplay.card.cardName}!");

        // Verifica se pode atacar neste round
        if (!selectedCardDisplay.CanAttackThisRound())
        {
            Debug.Log($"{selectedCardDisplay.card.cardName} já atacou neste round!");
            return false;
        }

        int damageDealt = selectedCardDisplay.currentAttack;
        targetCard.TakeDamage(damageDealt);

        // Marca que atacou neste round
        if (TurnManager.Instance != null)
        {
            selectedCardDisplay.lastAttackedRound = TurnManager.Instance.currentRound;
        }

        // Cancela a seleção após o ataque
        CancelSelection();

        return true;
    }

    // Tenta atacar a torre (nexus) do jogador inimigo
    public bool TryAttackTower()
    {
        // Verifica se há uma carta selecionada no tabuleiro
        if (selectedCard == null || selectedCardDisplay == null || !isMovingCard)
        {
            Debug.Log("Nenhuma carta selecionada para atacar a torre.");
            return false;
        }

        // Verifica se a carta pode atacar neste round
        if (!selectedCardDisplay.CanAttackThisRound())
        {
            Debug.Log($"{selectedCardDisplay.card.cardName} já atacou neste round!");
            return false;
        }

        // Verifica se a carta está na posição correta para atacar a torre
        int totalRows = boardManager != null ? boardManager.rows : 12;
        int playerNumber = selectedCardDisplay.ownerPlayerNumber;
        bool canAttackTower = false;
        int targetPlayerNumber = 0;

        if (playerNumber == 1 && currentTile.row == totalRows - 1)
        {
            // Jogador 1 na última linha (11) pode atacar torre do Jogador 2
            canAttackTower = true;
            targetPlayerNumber = 2;
        }
        else if (playerNumber == 2 && currentTile.row == 0)
        {
            // Jogador 2 na primeira linha (0) pode atacar torre do Jogador 1
            canAttackTower = true;
            targetPlayerNumber = 1;
        }

        if (!canAttackTower)
        {
            Debug.Log($"{selectedCardDisplay.card.cardName} não está em posição para atacar a torre!");
            return false;
        }

        // Executa o ataque à torre
        PlayerData targetPlayer = TurnManager.Instance.GetPlayer(targetPlayerNumber);
        int damage = selectedCardDisplay.currentAttack;

        Debug.Log($">>> {selectedCardDisplay.card.cardName} ataca a torre do {targetPlayer.playerName} causando {damage} de dano!");

        targetPlayer.TakeDamage(damage);

        // Marca que atacou neste round
        selectedCardDisplay.lastAttackedRound = TurnManager.Instance.currentRound;

        // Verifica se o jogador foi derrotado
        if (targetPlayer.IsDefeated())
        {
            Debug.Log($"==========================================");
            Debug.Log($"🏆 {TurnManager.Instance.GetCurrentPlayer().playerName} VENCEU! 🏆");
            Debug.Log($"==========================================");

            // Mostra a tela de vitória
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowVictoryScreen(TurnManager.Instance.currentPlayerNumber);
            }
        }

        // Cancela a seleção após o ataque
        CancelSelection();

        return true;
    }
}
