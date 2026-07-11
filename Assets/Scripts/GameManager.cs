using UnityEngine;
using System.Collections.Generic;

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

        // Fundo espacial (céu escuro + estrelas + meteoros), criado por código
        SpaceBackground.Ensure();
    }

    void Update()
    {
        // ESC cancela seleção de alvo de efeito (congelar / quebrar armadura / alvo genérico)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsWaitingForFreezeTarget()) CancelFreezeSelection();
            if (IsWaitingForShieldBreakTargets()) CancelShieldBreakSelection();
            if (IsWaitingForEffectTarget()) CancelEffectTargetSelection();
        }

        // Verifica tecla T para atacar a torre
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
        {
            TryAttackTower();
        }

        // Detecta clique "para frente" além do tabuleiro para atacar a torre
        if (isMovingCard && selectedCardDisplay != null &&
            UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            CheckForwardClickAttack();
        }
    }

    // Detecta se o jogador clicou além da última linha (sentido de avanço) para atacar a torre
    void CheckForwardClickAttack()
    {
        if (selectedCardDisplay == null || currentTile == null || boardManager == null) return;

        int totalRows = boardManager.rows;
        int playerNum = selectedCardDisplay.ownerPlayerNumber;

        // Só age se a carta estiver na última linha do lado adversário
        bool isAtLastRow = (playerNum == 1 && currentTile.row == totalRows - 1) ||
                           (playerNum == 2 && currentTile.row == 0);
        if (!isAtLastRow) return;

        // Obtém a posição do clique no mundo (plano Y = 0)
        Vector3 worldClick = GetMouseWorldPosition();
        if (worldClick == Vector3.zero) return;

        // Calcula os limites Z do tabuleiro
        float totalDepth = (boardManager.rows * boardManager.tileSize) +
                           ((boardManager.rows - 1) * boardManager.tileSpacing);
        float halfDepth = totalDepth / 2f;
        float boardCenterZ = boardManager.transform.position.z;

        // Verifica se o clique ficou além da borda do tabuleiro no sentido de avanço
        bool clickedBeyondBoard = (playerNum == 1 && worldClick.z > boardCenterZ + halfDepth) ||
                                  (playerNum == 2 && worldClick.z < boardCenterZ - halfDepth);

        if (clickedBeyondBoard)
        {
            TryAttackTower();
        }
    }

    // Converte a posição do mouse para coordenadas de mundo no plano do chão (Y = 0)
    Vector3 GetMouseWorldPosition()
    {
        Camera cam = GetCameraForMousePosition();
        if (cam == null) return Vector3.zero;

        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    // Retorna a câmera que está renderizando a área onde o mouse está (suporte a split screen)
    Camera GetCameraForMousePosition()
    {
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        foreach (Camera cam in Camera.allCameras)
        {
            if (!cam.enabled) continue;
            Rect viewRect = cam.rect;
            float minX = viewRect.x * Screen.width;
            float maxX = (viewRect.x + viewRect.width) * Screen.width;
            float minY = viewRect.y * Screen.height;
            float maxY = (viewRect.y + viewRect.height) * Screen.height;

            if (mousePos.x >= minX && mousePos.x <= maxX &&
                mousePos.y >= minY && mousePos.y <= maxY)
            {
                return cam;
            }
        }
        return Camera.main;
    }

    // Seleciona uma carta da mão para colocar no tabuleiro
    public void SelectCardFromHand(GameObject card, CardDisplay cardDisplay)
    {
        // Em multiplayer, só pode mexer nas SUAS cartas
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            cardDisplay.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[GameManager] Esta carta não é sua!");
            return;
        }

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
    }

    // Seleciona uma carta que já está no tabuleiro para mover
    public void SelectCardFromBoard(GameObject card, CardDisplay cardDisplay, CardTile tile)
    {
        // Se está aguardando escolha de alvo de efeito genérico (aliado OU inimigo),
        // o clique é a escolha — vem antes de qualquer checagem de dono
        if (IsWaitingForEffectTarget())
        {
            TryApplyEffectTarget(cardDisplay);
            return;
        }

        // Em multiplayer, só pode mexer nas SUAS cartas (exceto seleção de alvo de efeito)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            !IsWaitingForFreezeTarget() && !IsWaitingForShieldBreakTargets() &&
            cardDisplay.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[GameManager] Esta carta não é sua!");
            return;
        }

        // Se está aguardando seleção de alvo para congelar (Mage 3)
        if (IsWaitingForFreezeTarget())
        {
            TryFreezeEnemy(cardDisplay);
            return;
        }

        // Se está aguardando seleção de 2 inimigos para quebra de armadura (Mage 2 ATK 4, HP 3)
        if (IsWaitingForShieldBreakTargets())
        {
            TryBreakShield(cardDisplay);
            return;
        }

        // Se está selecionando um Tank para receber +2 armadura do Healer 2 (ATK 1, HP 3)
        if (TryApplyHealerShield(cardDisplay))
        {
            return;
        }

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
        }
        else if (cardDisplay.ownerPlayerNumber == 2 && tile.row == 0)
        {
            canAttackTower = true;
        }

        if (!canAttackTower)
        {
        }
    }

    // Tenta colocar a carta selecionada em um tile
    public bool TryPlaceCardOnTile(CardTile tile)
    {
        if (selectedCard == null || selectedCardDisplay == null)
        {
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

        // Em multiplayer, envia RPC — a colocação executa nos DOIS clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            HandManager ownerHand = GetHandManagerForPlayer(playerNumber);
            int handIndex = ownerHand != null ? ownerHand.GetCardIndex(selectedCard) : -1;
            if (handIndex < 0)
            {
                Debug.LogError("[GameManager] Carta não encontrada na mão!");
                return false;
            }

            PhotonGameManager.Instance.SendPlaceCardRPC(handIndex, playerNumber, tile.row, tile.column);
            CancelSelection(); // A execução real vem pelo RPC
            return true;
        }

        // Coloca a carta no tile
        PlaceCard(tile);
        return true;
    }

    // Busca o HandManager de um jogador específico
    HandManager GetHandManagerForPlayer(int playerNum)
    {
        HandManager[] allHandManagers = FindObjectsOfType<HandManager>();
        foreach (HandManager hm in allHandManagers)
        {
            if (hm.playerNumber == playerNum)
                return hm;
        }
        return null;
    }

    // Executa a colocação de fato (chamado via RPC nos dois clientes, ou localmente em offline)
    public void ExecutePlaceCard(int handIndex, int ownerPlayerNumber, int row, int column)
    {
        HandManager ownerHand = GetHandManagerForPlayer(ownerPlayerNumber);
        if (ownerHand == null)
        {
            Debug.LogError($"[GameManager] HandManager do jogador {ownerPlayerNumber} não encontrado!");
            return;
        }

        GameObject cardObject = ownerHand.GetCardAtIndex(handIndex);
        if (cardObject == null)
        {
            Debug.LogError($"[GameManager] Carta no índice {handIndex} da mão não encontrada!");
            return;
        }

        CardTile tile = boardManager != null ? boardManager.GetTile(row, column) : null;
        if (tile == null)
        {
            Debug.LogError($"[GameManager] Tile ({row}, {column}) não encontrado!");
            return;
        }

        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();

        // Remove da mão do dono
        ownerHand.RemoveCardFromHand(cardObject);

        // Move a carta para a posição do tile
        Vector3 cardPosition = tile.transform.position + new Vector3(0, CardDisplay.BoardYOffset, 0);
        cardObject.transform.position = cardPosition;
        cardObject.transform.rotation = CardDisplay.BoardRotation; // Deitada sobre o tile
        cardObject.transform.localScale = Vector3.one * CardDisplay.BoardScale;

        // Marca o tile como ocupado
        tile.OccupyTile(cardObject);

        // Atualiza o estado da carta
        cardDisplay.isInHand = false;
        cardDisplay.isOnBoard = true;
        cardDisplay.currentTile = tile;

        // Aplica o efeito da carta ao entrar no tabuleiro
        cardDisplay.ApplyCardEffect("onEnter");

        Debug.Log($"[GameManager] {cardDisplay.card.cardName} colocada em ({row}, {column}) pelo P{ownerPlayerNumber}");
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
            return false;
        }

        // Em multiplayer, envia RPC — o movimento executa nos DOIS clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            PhotonGameManager.Instance.SendMoveCardRPC(currentTile.row, currentTile.column, targetTile.row, targetTile.column);
            CancelSelection(); // A execução real vem pelo RPC
            return true;
        }

        // Move a carta
        MoveCard(targetTile);
        return true;
    }

    // Executa o movimento de fato (chamado via RPC nos dois clientes)
    public void ExecuteMoveCard(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();

        CardTile fromTile = boardManager.GetTile(fromRow, fromCol);
        CardTile toTile = boardManager.GetTile(toRow, toCol);

        if (fromTile == null || toTile == null || fromTile.occupiedCard == null)
        {
            Debug.LogError($"[GameManager] Movimento inválido: ({fromRow},{fromCol}) -> ({toRow},{toCol})");
            return;
        }

        GameObject cardObject = fromTile.occupiedCard;
        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();

        // Libera o tile atual
        fromTile.FreeTile();

        // Move a carta para a nova posição
        Vector3 cardPosition = toTile.transform.position + new Vector3(0, CardDisplay.BoardYOffset, 0);
        cardObject.transform.position = cardPosition;
        cardObject.transform.rotation = CardDisplay.BoardRotation; // Deitada sobre o tile

        // Ocupa o novo tile
        toTile.OccupyTile(cardObject);
        cardDisplay.currentTile = toTile;

        // Marca que a carta se moveu neste round
        if (TurnManager.Instance != null)
        {
            cardDisplay.lastMovedRound = TurnManager.Instance.currentRound;
        }

        Debug.Log($"[GameManager] {cardDisplay.card.cardName} moveu de ({fromRow},{fromCol}) para ({toRow},{toCol})");
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
        Vector3 cardPosition = targetTile.transform.position + new Vector3(0, CardDisplay.BoardYOffset, 0);
        selectedCard.transform.position = cardPosition;
        selectedCard.transform.rotation = CardDisplay.BoardRotation; // Deitada sobre o tile

        // Ocupa o novo tile
        targetTile.OccupyTile(selectedCard);

        // Atualiza a referência do tile na carta
        selectedCardDisplay.currentTile = targetTile;

        // Marca que a carta se moveu neste round
        if (TurnManager.Instance != null)
        {
            selectedCardDisplay.lastMovedRound = TurnManager.Instance.currentRound;
        }
        else
        {
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
        Vector3 cardPosition = tile.transform.position + new Vector3(0, CardDisplay.BoardYOffset, 0);
        selectedCard.transform.position = cardPosition;
        selectedCard.transform.rotation = CardDisplay.BoardRotation; // Deitada sobre o tile
        selectedCard.transform.localScale = Vector3.one * CardDisplay.BoardScale;

        // Marca o tile como ocupado
        tile.OccupyTile(selectedCard);

        // Atualiza o estado da carta
        selectedCardDisplay.isInHand = false;
        selectedCardDisplay.isOnBoard = true;
        selectedCardDisplay.currentTile = tile; // Armazena referência do tile


        // Aplica o efeito da carta ao entrar no tabuleiro
        selectedCardDisplay.ApplyCardEffect("onEnter");

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
            return false;
        }

        // Só pode atacar se a carta selecionada estiver no tabuleiro
        if (!selectedCardDisplay.isOnBoard)
        {
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
            return false;
        }

        // Executa o ataque

        // Verifica se pode atacar neste round
        if (!selectedCardDisplay.CanAttackThisRound())
        {
            Debug.Log($"{selectedCardDisplay.card.cardName} já atacou neste round!");
            return false;
        }

        // Em multiplayer, envia RPC — o ataque com alvo executa nos DOIS clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            if (TurnManager.Instance != null &&
                TurnManager.Instance.currentPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
            {
                Debug.Log("[GameManager] Não é seu turno, não pode atacar!");
                return false;
            }

            if (selectedCardDisplay.currentTile == null || targetCard.currentTile == null)
            {
                Debug.LogError("[GameManager] Tile do atacante ou do alvo é null!");
                return false;
            }

            PhotonGameManager.Instance.SendTargetedAttackRPC(
                selectedCardDisplay.currentTile.row, selectedCardDisplay.currentTile.column,
                targetCard.currentTile.row, targetCard.currentTile.column);

            CancelSelection();
            return true;
        }

        ExecuteTargetedAttack(selectedCardDisplay.currentTile.row, selectedCardDisplay.currentTile.column,
                              targetCard.currentTile.row, targetCard.currentTile.column);

        // Cancela a seleção após o ataque
        CancelSelection();

        return true;
    }

    // Executa o ataque com alvo específico (chamado via RPC nos dois clientes, ou localmente em offline)
    public void ExecuteTargetedAttack(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();

        CardTile fromTile = boardManager.GetTile(fromRow, fromCol);
        CardTile toTile = boardManager.GetTile(toRow, toCol);

        if (fromTile == null || toTile == null || fromTile.occupiedCard == null || toTile.occupiedCard == null)
        {
            Debug.LogError($"[GameManager] Ataque com alvo inválido: ({fromRow},{fromCol}) -> ({toRow},{toCol})");
            return;
        }

        CardDisplay attacker = fromTile.occupiedCard.GetComponent<CardDisplay>();
        CardDisplay target = toTile.occupiedCard.GetComponent<CardDisplay>();

        // Rastreia quem atacou (para efeitos reativos como congelar o atacante)
        target.attackerCardDisplay = attacker;

        int damageDealt = attacker.currentAttack;
        target.TakeDamage(damageDealt);

        // Marca que atacou neste round
        if (TurnManager.Instance != null)
        {
            attacker.lastAttackedRound = TurnManager.Instance.currentRound;
        }

        Debug.Log($"[GameManager] {attacker.card.cardName} atacou {target.card.cardName} causando {damageDealt} de dano");
    }

    // Checa efeitos periódicos das cartas (chamado a cada round)
    public void CheckPeriodicCardEffects()
    {
        if (TurnManager.Instance == null) return;

        var allCards = boardManager.GetAllCards();
        foreach (var card in allCards)
        {
            if (card != null)
            {
                CardEffectSimple effect = card.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    effect.CheckPeriodicEffects(TurnManager.Instance.currentRound);
                }
            }
        }
    }

    // Tenta atacar a torre (nexus) do jogador inimigo
    public bool TryAttackTower()
    {
        // Verifica se há uma carta selecionada no tabuleiro
        if (selectedCard == null || selectedCardDisplay == null || !isMovingCard)
        {
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

        // Em multiplayer, envia RPC — o ataque à torre executa nos DOIS clientes
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            PhotonGameManager.Instance.SendTowerAttackRPC(currentTile.row, currentTile.column, targetPlayerNumber);
            CancelSelection();
            return true;
        }

        ExecuteTowerAttack(currentTile.row, currentTile.column, targetPlayerNumber);
        CancelSelection();
        return true;
    }

    // Executa o ataque à torre de fato (chamado via RPC nos dois clientes, ou localmente em offline)
    public void ExecuteTowerAttack(int row, int column, int targetPlayerNumber)
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();

        CardTile tile = boardManager.GetTile(row, column);
        if (tile == null || tile.occupiedCard == null)
        {
            Debug.LogError($"[GameManager] Nenhuma carta no tile ({row}, {column}) para atacar a torre!");
            return;
        }

        CardDisplay attackerDisplay = tile.occupiedCard.GetComponent<CardDisplay>();
        PlayerData targetPlayer = TurnManager.Instance.GetPlayer(targetPlayerNumber);
        int damage = attackerDisplay.currentAttack;

        Debug.Log($">>> {attackerDisplay.card.cardName} ataca a torre do {targetPlayer.playerName} causando {damage} de dano!");

        targetPlayer.TakeDamage(damage);

        // Marca que atacou neste round
        attackerDisplay.lastAttackedRound = TurnManager.Instance.currentRound;

        // Verifica se o jogador foi derrotado
        if (targetPlayer.IsDefeated())
        {
            Debug.Log($"========== JOGADOR {targetPlayerNumber} DERROTADO ==========");

            // Mostra a tela de vitória
            if (GameUIManager.Instance != null)
            {
                int winnerPlayerNumber = targetPlayerNumber == 1 ? 2 : 1;
                GameUIManager.Instance.ShowVictoryScreen(winnerPlayerNumber);
            }
        }
    }

    // Sistema para seleção de alvo para congelar (Mage 3)
    private CardDisplay mageFreezingCard = null;
    private bool isWaitingForFreezeTarget = false;

    public void StartFreezeSelection(CardDisplay mageCard)
    {
        // Em multiplayer, só o dono do Mago escolhe o alvo (a escolha chega por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            mageCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[FreezeSelection] Oponente está escolhendo o alvo do congelamento...");
            return;
        }

        // Sem inimigos em campo, não há o que congelar — não entra no modo de seleção
        int enemyPlayer = mageCard.ownerPlayerNumber == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        if (board == null || board.GetCardsByOwner(enemyPlayer).Count == 0)
        {
            Debug.Log("[FreezeSelection] Nenhum inimigo em campo, efeito não ativado");
            return;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowDecisionPopup(
                "Clique em um inimigo no tabuleiro para congelar (ESC cancela)",
                "Entendi",
                () => { /* aguardando clique no inimigo */ },
                "Cancelar",
                () => CancelFreezeSelection()
            );
        }

        mageFreezingCard = mageCard;
        isWaitingForFreezeTarget = true;
        Debug.Log("[FreezeSelection] Aguardando seleção de alvo para congelar...");
    }

    public void TryFreezeEnemy(CardDisplay targetCard)
    {
        if (!isWaitingForFreezeTarget || mageFreezingCard == null) return;

        // Verifica se é um inimigo
        if (targetCard.ownerPlayerNumber == mageFreezingCard.ownerPlayerNumber ||
            targetCard.ownerPlayerNumber == 0)
        {
            Debug.Log("[FreezeSelection] Alvo inválido! Deve ser um inimigo. (ESC cancela a seleção)");
            return;
        }

        // Em multiplayer, a escolha do alvo viaja por RPC (congela nos dois clientes)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            if (mageFreezingCard.currentTile != null && targetCard.currentTile != null)
            {
                PhotonGameManager.Instance.SendEffectTargetRPC(1,
                    mageFreezingCard.currentTile.row, mageFreezingCard.currentTile.column,
                    targetCard.currentTile.row, targetCard.currentTile.column);
            }
            isWaitingForFreezeTarget = false;
            mageFreezingCard = null;
            return;
        }

        // Congela o inimigo
        targetCard.Freeze();
        isWaitingForFreezeTarget = false;
        mageFreezingCard = null;

        Debug.Log($"[FreezeSelection] {targetCard.card.cardName} foi congelada!");
    }

    // Executa efeitos com alvo escolhido (chamado via RPC nos dois clientes)
    public void ExecuteEffectOnTarget(int effectType, int sourceRow, int sourceCol, int targetRow, int targetCol)
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();

        CardTile targetTile = boardManager.GetTile(targetRow, targetCol);
        if (targetTile == null || targetTile.occupiedCard == null)
        {
            Debug.LogError($"[GameManager] Alvo do efeito {effectType} não encontrado em ({targetRow},{targetCol})!");
            return;
        }
        CardDisplay target = targetTile.occupiedCard.GetComponent<CardDisplay>();

        CardTile sourceTile = boardManager.GetTile(sourceRow, sourceCol);
        CardDisplay source = (sourceTile != null && sourceTile.occupiedCard != null)
            ? sourceTile.occupiedCard.GetComponent<CardDisplay>() : null;

        switch (effectType)
        {
            case 1: // Congelar (Mage 1)
                target.Freeze();
                Debug.Log($"[GameManager] {target.card.cardName} foi congelada!");
                break;

            case 2: // Quebrar armadura (Mage 2)
                if (source != null)
                {
                    CardEffectSimple mageEffect = source.GetComponent<CardEffectSimple>();
                    if (mageEffect != null) mageEffect.BreakEnemyShield(target);
                }
                break;

            case 3: // +2 armadura do Healer 2 em um Tank
                if (source != null)
                {
                    CardEffectSimple healerEffect = source.GetComponent<CardEffectSimple>();
                    if (healerEffect != null) healerEffect.HealerTier2Effect2_BoostTankShield(target);
                }
                break;

            default:
                // Tipos 4+ são efeitos com alvo escolhido por clique
                DispatchEffectOnTarget(effectType, source, target);
                break;
        }
    }

    // ── Seleção genérica de alvo por clique ─────────────────────────────
    // Tipos: 4=copiar stats (Mage 5), 5=destruir tier inferior (Mage 4),
    //        6=remover bônus (Mage 4), 7=duplicar stats de aliado (Healer 5),
    //        8=invulnerabilidade (Healer 4), 9=+3 armadura em Mago (Tank 3)
    private CardDisplay effectTargetSource = null;
    private int effectTargetType = 0;
    private List<CardDisplay> effectTargetCandidates = null;
    private bool isWaitingForEffectTarget = false;

    public void StartEffectTargetSelection(CardDisplay sourceCard, int effectType,
        List<CardDisplay> candidates, string prompt)
    {
        // Em multiplayer, só o dono da carta escolhe (a escolha chega por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            sourceCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[EffectTarget] Oponente está escolhendo o alvo do efeito...");
            return;
        }

        if (candidates == null) return;
        candidates.RemoveAll(c => c == null || c.currentTile == null);
        if (candidates.Count == 0)
        {
            Debug.Log("[EffectTarget] Nenhum alvo válido em campo, efeito não ativado");
            return;
        }

        // Um único alvo possível: aplica direto, sem pedir clique
        if (candidates.Count == 1)
        {
            ApplyEffectTargetChoice(sourceCard, effectType, candidates[0]);
            return;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowDecisionPopup(
                prompt + "\nClique na carta desejada no tabuleiro (ESC cancela)",
                "Entendi", () => { /* aguardando clique */ },
                "Cancelar", () => CancelEffectTargetSelection());
        }

        effectTargetSource = sourceCard;
        effectTargetType = effectType;
        effectTargetCandidates = candidates;
        isWaitingForEffectTarget = true;
        Debug.Log($"[EffectTarget] Aguardando clique no alvo (efeito {effectType}, {candidates.Count} opções)...");
    }

    public bool IsWaitingForEffectTarget()
    {
        return isWaitingForEffectTarget;
    }

    public void TryApplyEffectTarget(CardDisplay targetCard)
    {
        if (!isWaitingForEffectTarget || effectTargetSource == null) return;

        if (effectTargetCandidates == null || !effectTargetCandidates.Contains(targetCard))
        {
            Debug.Log("[EffectTarget] Esta carta não é um alvo válido para o efeito! (ESC cancela)");
            return;
        }

        CardDisplay source = effectTargetSource;
        int effectType = effectTargetType;
        CancelEffectTargetSelection(); // Encerra o modo antes de aplicar
        ApplyEffectTargetChoice(source, effectType, targetCard);
    }

    public void CancelEffectTargetSelection()
    {
        isWaitingForEffectTarget = false;
        effectTargetSource = null;
        effectTargetType = 0;
        effectTargetCandidates = null;
        Debug.Log("[EffectTarget] Seleção de alvo encerrada");
    }

    void ApplyEffectTargetChoice(CardDisplay source, int effectType, CardDisplay target)
    {
        // Em multiplayer, a escolha viaja por RPC como coordenadas de tile
        // (executa nos dois clientes); offline aplica direto
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            source.currentTile != null && target.currentTile != null)
        {
            PhotonGameManager.Instance.SendEffectTargetRPC(effectType,
                source.currentTile.row, source.currentTile.column,
                target.currentTile.row, target.currentTile.column);
        }
        else
        {
            DispatchEffectOnTarget(effectType, source, target);
        }
    }

    // Aplica o efeito escolhido (offline direto; multiplayer via ExecuteEffectOnTarget/RPC)
    public void DispatchEffectOnTarget(int effectType, CardDisplay source, CardDisplay target)
    {
        CardEffectSimple effect = source != null ? source.GetComponent<CardEffectSimple>() : null;
        if (effect == null)
        {
            Debug.LogError($"[GameManager] Fonte do efeito {effectType} sem CardEffectSimple!");
            return;
        }

        switch (effectType)
        {
            case 4: effect.ActivateCopyStats(target); break;        // Mage 5: copiar stats do inimigo
            case 5: effect.ActivateDestroyLowerTier(target); break; // Mage 4: destruir inimigo de tier inferior
            case 6: effect.ActivateRemoveBonus(target); break;      // Mage 4: remover bônus do inimigo
            case 7: effect.ActivateDoubleStats(target); break;      // Healer 5: duplicar stats de aliado
            case 8: effect.ActivateInvulnerability(target); break;  // Healer 4: invulnerabilidade em aliado
            case 9: effect.ActivateBoostMagoShield(target); break;  // Tank 3: +3 armadura em Mago aliado
            default:
                Debug.LogError($"[GameManager] Tipo de efeito com alvo desconhecido: {effectType}");
                break;
        }
    }

    public void CancelFreezeSelection()
    {
        isWaitingForFreezeTarget = false;
        mageFreezingCard = null;
        Debug.Log("[FreezeSelection] Seleção cancelada");
    }

    public bool IsWaitingForFreezeTarget()
    {
        return isWaitingForFreezeTarget;
    }

    // Tenta aplicar +2 armadura do Healer 2 (ATK 1, HP 3) a um Tank
    private bool TryApplyHealerShield(CardDisplay targetCard)
    {
        if (targetCard == null || targetCard.card.cardClass != CardClass.Tank) return false;

        BoardManager board = BoardManager.Instance;
        if (board == null) return false;

        int currentPlayerNumber = TurnManager.Instance?.currentPlayerNumber ?? 0;
        var allies = board.GetCardsByOwner(currentPlayerNumber);

        // Procura por Healer 2 (ATK 1, HP 3) que pode dar armadura
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Healer &&
                ally.card.attack == 1 && ally.card.health == 3 &&
                ally.healerShieldUseCount < 2)
            {
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    // Em multiplayer, a escolha viaja por RPC (executa nos dois clientes)
                    if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
                        ally.currentTile != null && targetCard.currentTile != null)
                    {
                        PhotonGameManager.Instance.SendEffectTargetRPC(3,
                            ally.currentTile.row, ally.currentTile.column,
                            targetCard.currentTile.row, targetCard.currentTile.column);
                    }
                    else
                    {
                        effect.HealerTier2Effect2_BoostTankShield(targetCard);
                    }
                    return true;
                }
            }
        }

        return false;
    }

    // Sistema para seleção de 2 inimigos para quebra de armadura (Mage 2 ATK 4, HP 3)
    private CardDisplay shieldBreakMage = null;
    private int shieldBreakTargetsSelected = 0;
    private int shieldBreakTargetsRequired = 2;
    private CardDisplay shieldBreakFirstTarget = null;
    private bool isWaitingForShieldBreakTargets = false;

    public void StartShieldBreakSelection(CardDisplay mageCard)
    {
        // Em multiplayer, só o dono do Mago escolhe os alvos (as escolhas chegam por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            mageCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[ShieldBreakSelection] Oponente está escolhendo os alvos...");
            return;
        }

        // Sem inimigos em campo, não há alvo — não entra no modo de seleção
        int enemyPlayer = mageCard.ownerPlayerNumber == 1 ? 2 : 1;
        BoardManager board = BoardManager.Instance;
        int enemyCount = board != null ? board.GetCardsByOwner(enemyPlayer).Count : 0;
        if (enemyCount == 0)
        {
            Debug.Log("[ShieldBreakSelection] Nenhum inimigo em campo, efeito não ativado");
            return;
        }

        // Se só há 1 inimigo, pede apenas 1 alvo
        shieldBreakTargetsRequired = Mathf.Min(2, enemyCount);

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowDecisionPopup(
                $"Clique em {shieldBreakTargetsRequired} inimigo(s) no tabuleiro para quebrar a armadura (ESC cancela)",
                "Entendi",
                () => { /* aguardando clique */ },
                "Cancelar",
                () => CancelShieldBreakSelection()
            );
        }

        shieldBreakMage = mageCard;
        shieldBreakTargetsSelected = 0;
        shieldBreakFirstTarget = null;
        isWaitingForShieldBreakTargets = true;
        Debug.Log($"[ShieldBreakSelection] Aguardando seleção de {shieldBreakTargetsRequired} alvo(s)...");
    }

    public void TryBreakShield(CardDisplay targetCard)
    {
        if (!isWaitingForShieldBreakTargets || shieldBreakMage == null) return;

        // Verifica se é um inimigo
        if (targetCard.ownerPlayerNumber == shieldBreakMage.ownerPlayerNumber ||
            targetCard.ownerPlayerNumber == 0)
        {
            Debug.Log("[ShieldBreakSelection] Alvo inválido! Deve ser um inimigo. (ESC cancela a seleção)");
            return;
        }

        // Não deixa escolher o mesmo inimigo duas vezes
        if (targetCard == shieldBreakFirstTarget)
        {
            Debug.Log("[ShieldBreakSelection] Este inimigo já foi selecionado! Escolha outro.");
            return;
        }

        // Quebra armadura — em multiplayer, a escolha viaja por RPC (executa nos dois clientes)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            shieldBreakMage.currentTile != null && targetCard.currentTile != null)
        {
            PhotonGameManager.Instance.SendEffectTargetRPC(2,
                shieldBreakMage.currentTile.row, shieldBreakMage.currentTile.column,
                targetCard.currentTile.row, targetCard.currentTile.column);
        }
        else
        {
            CardEffectSimple effect = shieldBreakMage.GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                effect.BreakEnemyShield(targetCard);
            }
        }

        shieldBreakFirstTarget = targetCard;
        shieldBreakTargetsSelected++;

        if (shieldBreakTargetsSelected >= shieldBreakTargetsRequired)
        {
            isWaitingForShieldBreakTargets = false;
            shieldBreakMage = null;
            shieldBreakTargetsSelected = 0;
            shieldBreakFirstTarget = null;
            Debug.Log("[ShieldBreakSelection] Seleção completa!");
        }
        else
        {
            Debug.Log($"[ShieldBreakSelection] Alvo selecionado, falta(m) {shieldBreakTargetsRequired - shieldBreakTargetsSelected}");
        }
    }

    public void CancelShieldBreakSelection()
    {
        isWaitingForShieldBreakTargets = false;
        shieldBreakMage = null;
        shieldBreakTargetsSelected = 0;
        shieldBreakFirstTarget = null;
        Debug.Log("[ShieldBreakSelection] Seleção cancelada");
    }

    public bool IsWaitingForShieldBreakTargets()
    {
        return isWaitingForShieldBreakTargets;
    }
}
