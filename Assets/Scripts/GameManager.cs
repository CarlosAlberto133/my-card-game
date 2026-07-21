using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Regras do Jogo")]
    public int maxPlacementRows = 2; // Apenas 2 primeiras fileiras
    // Movimentação v4.2: TODAS as classes andam em cruz até 2 casas (caminho
    // livre); Tank/Healer também andam 1 casa na diagonal.
    // As regras vivem em IsValidMovement.

    private GameObject selectedCard;
    private CardDisplay selectedCardDisplay;
    private CardTile currentTile; // Tile onde a carta selecionada está
    private HandManager handManager;
    private BoardManager boardManager;
    private bool isMovingCard = false; // Se está movendo uma carta do tabuleiro

    // Pilhagem (v4.2): último round em que cada jogador ganhou o +1 de ouro
    // por dano na torre (índices 1 e 2; campo de instância — zera por partida)
    private int[] lastPlunderRound = { -1, -1, -1 };

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

        // Temática da partida (mapa escolhido pelo anfitrião na sala: Espaço ou
        // Mesa de RPG) — aplica o cenário quando o tabuleiro + seed existirem
        BoardThemeManager.Ensure();
        SoundManager.Ensure();
    }

    void Update()
    {
        // Escolha de torre aberta: nenhuma ação de jogo (teclas/cliques)
        if (TowerSelectUI.IsOpen) return;

        // ESC cancela seleção de alvo de efeito (congelar / quebrar armadura / alvo genérico)
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsWaitingForFreezeTarget()) CancelFreezeSelection();
            if (IsWaitingForShieldBreakTargets()) CancelShieldBreakSelection();
            if (IsWaitingForEffectTarget()) CancelEffectTargetSelection();
        }

        // Com decisão de efeito pendente, nenhuma ação nova (a resolução vem antes)
        if (IsDecisionPending()) return;

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

        // Só age se a carta estiver ao alcance da torre adversária
        // (última fileira; Magos e Arqueiros também da penúltima)
        int towerReach = TowerReach(selectedCardDisplay);
        bool isAtLastRow = (playerNum == 1 && currentTile.row >= totalRows - towerReach) ||
                           (playerNum == 2 && currentTile.row <= towerReach - 1);
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

    // Alcance da carta até a torre: 1 fileira para todos; 2 para Magos e
    // Arqueiros (mesma regra do alcance de ataque em cruz estendida)
    int TowerReach(CardDisplay cardDisplay)
    {
        if (cardDisplay != null && cardDisplay.card != null &&
            (cardDisplay.card.cardClass == CardClass.Arqueiro ||
             cardDisplay.card.cardClass == CardClass.Mago))
        {
            return 2;
        }
        return 1;
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

        // Clicar de novo na carta JÁ selecionada desseleciona (apaga as cores)
        if (selectedCard == card)
        {
            CancelSelection();
            return;
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

        // (O antigo "+2 armadura do Healer 2 em Tank" foi REMOVIDO: a carta
        // virou tríade pura, e o efeito solo sequestraava qualquer clique em
        // Tank — dava armadura até para o tank inimigo e comia a seleção)

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

        // Clicar de novo na carta JÁ selecionada desseleciona (apaga as cores)
        if (selectedCard == card)
        {
            CancelSelection();
            return;
        }

        // NÃO bloqueia a seleção se a carta já se moveu: ela ainda pode ATACAR.
        // Mover e atacar são independentes (1 de cada por round, em qualquer
        // ordem); a validação do movimento acontece em TryMoveCard.

        CancelSelection(); // Cancela qualquer seleção anterior

        selectedCard = card;
        selectedCardDisplay = cardDisplay;
        currentTile = tile;
        isMovingCard = true;

        // Sempre destaca: os tiles de MOVIMENTO só aparecem se a carta ainda pode
        // andar, mas o AMARELO de ataque deve aparecer mesmo depois de ela ter
        // andado (mover e atacar são independentes). A lógica fica em HighlightValidTiles.
        HighlightValidTiles();
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
        int totalRows = boardManager != null ? boardManager.rows : 10;
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
            // Jogador 2: últimas 2 fileiras (8 e 9 em um tabuleiro 10x10)
            int minRow = totalRows - maxPlacementRows; // 10 - 2 = 8
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
        SoundManager.Play(SoundManager.Sound.Place);

        // Atualiza o estado da carta
        cardDisplay.isInHand = false;
        cardDisplay.isOnBoard = true;
        cardDisplay.currentTile = tile;
        MatchStatsTracker.RecordPlayed(cardDisplay); // telemetria: carta jogada
        TowerSystem.OnCardPlaced(cardDisplay); // torres: Estandarte/auras/Emboscada

        // Em campo a carta é pública: remove o verso (mão do oponente)
        cardDisplay.SetFaceDown(false);

        // Atualiza o visual (borda do dono + figura 3D da classe sobre a carta)
        cardDisplay.UpdateDisplay();

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

        // A carta anda 1 vez por round (a seleção não bloqueia mais isso, para
        // permitir mover -> atacar; a checagem real do movimento é aqui)
        if (selectedCardDisplay != null && !selectedCardDisplay.CanMoveThisRound())
        {
            Debug.Log("Esta carta já se moveu neste round!");
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

        // Verifica se o movimento é válido (regras por classe)
        if (!IsValidMovement(selectedCardDisplay, currentTile, targetTile))
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
        // Reafirma a escala de tabuleiro: cura cartas que ficaram com escala
        // errada (ex.: o antigo bug do hover que devolvia o tamanho da loja)
        cardObject.transform.localScale = Vector3.one * CardDisplay.BoardScale;

        // Ocupa o novo tile
        toTile.OccupyTile(cardObject);
        cardDisplay.currentTile = toTile;

        // Marca que a carta se moveu neste round (conta o 2º movimento do
        // Archer 3 [3/2] com Mago)
        cardDisplay.MarkMoveUsed();

        Debug.Log($"[GameManager] {cardDisplay.card.cardName} moveu de ({fromRow},{fromCol}) para ({toRow},{toCol})");
    }

    // Verifica se o movimento é válido — regras v4.2:
    //   TODAS as classes: cruz (reta) de até 2 casas; na de 2, a casa do MEIO
    //                     precisa estar livre (ninguém atravessa carta)
    //   Tank/Healer:      ADEMAIS, diagonal de 1 casa
    bool IsValidMovement(CardDisplay mover, CardTile from, CardTile to)
    {
        int rowDiff = Mathf.Abs(to.row - from.row);
        int colDiff = Mathf.Abs(to.column - from.column);

        // Cruz de 1 casa
        if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
            return true;

        // Cruz de 2 casas com o caminho livre
        if ((rowDiff == 2 && colDiff == 0) || (rowDiff == 0 && colDiff == 2))
        {
            CardTile mid = boardManager != null
                ? boardManager.GetTile((from.row + to.row) / 2, (from.column + to.column) / 2)
                : null;
            return mid != null && mid.occupiedCard == null;
        }

        // Diagonal de 1 casa: só Tank e Healer
        CardClass cls = (mover != null && mover.card != null)
            ? mover.card.cardClass : CardClass.Tank;
        if (cls == CardClass.Tank || cls == CardClass.Healer)
            return rowDiff == 1 && colDiff == 1;

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
        // Reafirma a escala de tabuleiro (cura escala errada de bugs antigos)
        selectedCard.transform.localScale = Vector3.one * CardDisplay.BoardScale;

        // Ocupa o novo tile
        targetTile.OccupyTile(selectedCard);

        // Atualiza a referência do tile na carta
        selectedCardDisplay.currentTile = targetTile;

        // Marca que a carta se moveu neste round (conta o 2º movimento do
        // Archer 3 [3/2] com Mago)
        selectedCardDisplay.MarkMoveUsed();

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
        MatchStatsTracker.RecordPlayed(selectedCardDisplay); // telemetria: carta jogada
        TowerSystem.OnCardPlaced(selectedCardDisplay); // torres: Estandarte/auras/Emboscada

        // Atualiza o visual (borda do dono + figura 3D da classe sobre a carta)
        selectedCardDisplay.UpdateDisplay();

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

    // Enquanto uma decisão de efeito está pendente (popup aberto AQUI ou o
    // oponente decidindo do lado dele), nenhuma ação nova pode começar —
    // "sair batendo" com decisões abertas embaralhava a ordem dos danos
    public static bool IsDecisionPending()
    {
        if (GameUIManager.Instance != null && GameUIManager.Instance.HasOpenDecision) return true;
        if (PhotonGameManager.Instance != null && PhotonGameManager.Instance.HasPendingDecisions()) return true;
        return false;
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
            return IsValidMovement(selectedCardDisplay, currentTile, tile); // Regras por classe
        }
        else
        {
            // Se está colocando da mão
            if (tile.occupiedCard != null) return false; // Não pode colocar em tile ocupado

            // Verifica fileiras válidas por jogador
            int playerNumber = selectedCardDisplay.ownerPlayerNumber;
            int totalRows = boardManager != null ? boardManager.rows : 10;

            if (playerNumber == 1)
            {
                // Jogador 1: primeiras 2 fileiras (0 e 1)
                return tile.row < maxPlacementRows;
            }
            else if (playerNumber == 2)
            {
                // Jogador 2: últimas 2 fileiras (8 e 9)
                int minRow = totalRows - maxPlacementRows;
                return tile.row >= minRow;
            }

            return false;
        }
    }

    // Destaca todos os tiles do tabuleiro: verde = válido, vermelho = inválido,
    // AMARELO = inimigo ao alcance de ataque da carta selecionada (no tabuleiro).
    void HighlightValidTiles()
    {
        if (boardManager == null) return;

        bool boardCard = isMovingCard && selectedCardDisplay != null && selectedCardDisplay.isOnBoard;

        // Inimigos que a carta selecionada consegue atacar agora (só se estiver no
        // tabuleiro e puder atacar neste round). CanAttackPeek é silencioso.
        var attackable = new System.Collections.Generic.HashSet<CardTile>();
        if (boardCard && selectedCardDisplay.CanAttackPeek())
        {
            foreach (CardDisplay enemy in selectedCardDisplay.GetAdjacentEnemies())
            {
                if (enemy != null && enemy.currentTile != null)
                    attackable.Add(enemy.currentTile);
            }
        }

        // Tiles de movimento (verde/vermelho) só quando a ação de mover ainda está
        // disponível: colocando da mão (!isMovingCard) ou carta que ainda pode andar.
        // Se já andou, não mostra verde/vermelho — mas o amarelo de ataque continua.
        bool showMoveTiles = !isMovingCard ||
            (selectedCardDisplay != null && selectedCardDisplay.CanMovePeek());

        CardTile[] allTiles = FindObjectsOfType<CardTile>();
        foreach (CardTile tile in allTiles)
        {
            if (attackable.Contains(tile))
                tile.SetAttackHighlight();                          // Amarelo: dá pra atacar
            else if (showMoveTiles)
                tile.SetHighlight(IsValidTileForCurrentSelection(tile)); // Verde/vermelho de movimento
            else
                tile.ClearHighlight();                              // Já andou: sem verde/vermelho
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

        // Ataque com TODOS os efeitos de atacante (dano dobrado, ignorar
        // armadura, execução, ataque duplo...). Antes este caminho fazia só um
        // TakeDamage seco e nenhum efeito de Arqueiro funcionava no clique
        attacker.PerformAttackOn(target);

        Debug.Log($"[GameManager] {attacker.card.cardName} atacou {target.card.cardName}");
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

        // Verifica se a carta está na posição correta para atacar a torre.
        // Magos e Arqueiros têm alcance 2: atacam a torre também da penúltima fileira
        int totalRows = boardManager != null ? boardManager.rows : 10;
        int playerNumber = selectedCardDisplay.ownerPlayerNumber;
        bool canAttackTower = false;
        int targetPlayerNumber = 0;
        int towerReach = TowerReach(selectedCardDisplay);

        if (playerNumber == 1 && currentTile.row >= totalRows - towerReach)
        {
            // Jogador 1 nas últimas linhas pode atacar torre do Jogador 2
            canAttackTower = true;
            targetPlayerNumber = 2;
        }
        else if (playerNumber == 2 && currentTile.row <= towerReach - 1)
        {
            // Jogador 2 nas primeiras linhas pode atacar torre do Jogador 1
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
        // Auras de ataque dos tanks (Capitão de Ferro/Baluarte) valem também
        // contra a torre — é o caminho da vitória, o bônus não pode sumir aqui
        int damage = attackerDisplay.currentAttack + attackerDisplay.AuraAttackBonus();

        Debug.Log($">>> {attackerDisplay.card.cardName} ataca a torre do {targetPlayer.playerName} causando {damage} de dano!");

        // Investida visual para frente (lado do inimigo) + som
        int forward = attackerDisplay.ownerPlayerNumber == 1 ? -1 : 1;
        Vector3 aheadTile = attackerDisplay.transform.position + new Vector3(0f, 0f, forward * 6f);
        CardAnimator.Get(attackerDisplay.gameObject).Lunge(aheadTile);
        SoundManager.Play(SoundManager.Sound.Attack);

        targetPlayer.TakeDamage(damage);

        // PILHAGEM (v4.2): causar dano na torre inimiga rende +1 de ouro, uma
        // vez por round por jogador — atacar paga melhor que tartaruguear.
        // Determinístico: roda no RPC do ataque, igual nos dois clientes
        int raider = attackerDisplay.ownerPlayerNumber;
        if (raider >= 1 && raider <= 2 && TurnManager.Instance != null &&
            lastPlunderRound[raider] != TurnManager.Instance.currentRound)
        {
            lastPlunderRound[raider] = TurnManager.Instance.currentRound;
            PlayerData raiderData = TurnManager.Instance.GetPlayer(raider);
            if (raiderData != null)
            {
                raiderData.AddGold(1);
                FloatingTextFX.ShowAboveCard(attackerDisplay, "PILHAGEM! +1 OURO",
                    FloatingTextFX.EffectColor, 4.2f);
                Debug.Log($"[Pilhagem] P{raider}: +1 de ouro por dano na torre (round {TurnManager.Instance.currentRound})");
            }
        }

        // Torres: gatilhos de "torre tomou dano" (Represália / Ressurgimento)
        TowerSystem.OnTowerDamaged(targetPlayerNumber, attackerDisplay);

        // Marca que atacou neste round (ou consome o 2º ataque da aura do Tank 4)
        attackerDisplay.MarkAttackUsed();

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
        // Carta do BOT (modo treino): o bot escolhe o alvo sozinho e envia pelo
        // mesmo RPC_EffectTarget — sem isso, o efeito nunca resolveria (não há
        // outro cliente para escolher)
        if (BotMode.IsBot(mageCard.ownerPlayerNumber))
        {
            BotController.AutoChooseFreezeTarget(mageCard);
            return;
        }

        // Em multiplayer, só o dono do Mago escolhe o alvo (a escolha chega por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            mageCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[FreezeSelection] Oponente está escolhendo o alvo do congelamento...");
            return;
        }

        // Outra seleção em andamento: entra na fila (não sobrescreve o estado)
        if (AnySelectionActive())
        {
            queuedSelections.Enqueue(() => StartFreezeSelection(mageCard));
            Debug.Log("[FreezeSelection] Seleção enfileirada (outra em andamento)");
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

        // Moldura dourada pulsando nos inimigos que podem ser congelados
        CardAuraIndicator.ShowSelectableTargets(board.GetCardsByOwner(enemyPlayer));
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
            CardAuraIndicator.HideSelectableTargets();
            StartNextQueuedSelection();
            return;
        }

        // Congela o inimigo
        EffectProjectileFX.Launch(mageFreezingCard, targetCard, EffectProjectileFX.Ice);
        targetCard.Freeze(false, mageFreezingCard);
        isWaitingForFreezeTarget = false;
        mageFreezingCard = null;
        CardAuraIndicator.HideSelectableTargets();

        Debug.Log($"[FreezeSelection] {targetCard.card.cardName} foi congelada!");
        StartNextQueuedSelection();
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
                if (source != null)
                    EffectProjectileFX.Launch(source, target, EffectProjectileFX.Ice);
                target.Freeze(false, source);
                Debug.Log($"[GameManager] {target.card.cardName} foi congelada!");
                break;

            case 2: // Quebrar armadura (Mage 2)
                if (source != null)
                {
                    CardEffectSimple mageEffect = source.GetComponent<CardEffectSimple>();
                    if (mageEffect != null) mageEffect.BreakEnemyShield(target);
                }
                break;

            // (case 3 — +2 armadura do Healer 2 em Tank — removido: a carta
            // Healer 2 (1/3) é tríade pura e não tem mais efeito solo)

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
        // Carta do BOT (modo treino): escolhe o alvo sozinho via RPC_EffectTarget
        if (BotMode.IsBot(sourceCard.ownerPlayerNumber))
        {
            BotController.AutoChooseEffectTarget(sourceCard, effectType, candidates);
            return;
        }

        // Em multiplayer, só o dono da carta escolhe (a escolha chega por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            sourceCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[EffectTarget] Oponente está escolhendo o alvo do efeito...");
            return;
        }

        // Já existe uma seleção em andamento? Entra na FILA em vez de
        // sobrescrever o estado (dois efeitos ao mesmo tempo: o segundo
        // apagava o primeiro e os cliques resolviam contra o efeito errado)
        if (AnySelectionActive())
        {
            queuedSelections.Enqueue(() => StartEffectTargetSelection(sourceCard, effectType, candidates, prompt));
            Debug.Log($"[EffectTarget] Seleção do efeito {effectType} enfileirada (outra em andamento)");
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

        // Moldura dourada pulsando nas cartas que PODEM ser clicadas
        CardAuraIndicator.ShowSelectableTargets(candidates);
        Debug.Log($"[EffectTarget] Aguardando clique no alvo (efeito {effectType}, {candidates.Count} opções)...");
    }

    public bool IsWaitingForEffectTarget()
    {
        return isWaitingForEffectTarget;
    }

    // ── Seleção de alvo para efeitos de TORRE (Tempestade/Nevasca, v4.2) ──
    // A torre não tem carta no tabuleiro como origem, então a escolha viaja
    // por um RPC próprio (RPC_TowerEffectTarget) carregando jogador + carta
    // da torre. Reusa TODO o resto da máquina de seleção (fila, molduras,
    // popup, ESC).
    private int towerTargetOwner = 0;    // 0 = nenhuma seleção de torre ativa
    private int towerTargetCardId = -1;

    public void StartTowerEffectTargetSelection(int ownerPlayer, int towerCardId,
        List<CardDisplay> candidates, string prompt)
    {
        // Torre do BOT (modo treino): escolhe sozinho via RPC
        if (BotMode.IsBot(ownerPlayer))
        {
            BotController.AutoChooseTowerEffectTarget(ownerPlayer, towerCardId, candidates);
            return;
        }

        // Em multiplayer, só o dono da torre escolhe (a escolha chega por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            ownerPlayer != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[TowerTarget] Oponente está escolhendo o alvo da torre...");
            return;
        }

        // Outra seleção em andamento: entra na fila
        if (AnySelectionActive())
        {
            queuedSelections.Enqueue(() => StartTowerEffectTargetSelection(ownerPlayer, towerCardId, candidates, prompt));
            Debug.Log($"[TowerTarget] Seleção da torre (carta {towerCardId}) enfileirada");
            return;
        }

        if (candidates == null) return;
        candidates.RemoveAll(c => c == null || c.currentTile == null);
        if (candidates.Count == 0)
        {
            Debug.Log("[TowerTarget] Nenhum alvo válido em campo, efeito não ativado");
            return;
        }

        // Um único alvo possível: aplica direto, sem pedir clique
        if (candidates.Count == 1)
        {
            ApplyTowerTargetChoice(ownerPlayer, towerCardId, candidates[0]);
            return;
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowDecisionPopup(
                prompt + "\nClique na carta desejada no tabuleiro (ESC cancela)",
                "Entendi", () => { /* aguardando clique */ },
                "Cancelar", () => CancelEffectTargetSelection());
        }

        towerTargetOwner = ownerPlayer;
        towerTargetCardId = towerCardId;
        effectTargetSource = null;
        effectTargetType = 0;
        effectTargetCandidates = candidates;
        isWaitingForEffectTarget = true;

        CardAuraIndicator.ShowSelectableTargets(candidates);
        Debug.Log($"[TowerTarget] Aguardando clique no alvo (torre carta {towerCardId}, {candidates.Count} opções)...");
    }

    void ApplyTowerTargetChoice(int ownerPlayer, int towerCardId, CardDisplay target)
    {
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            target != null && target.currentTile != null)
        {
            PhotonGameManager.Instance.SendTowerEffectTargetRPC(ownerPlayer, towerCardId,
                target.currentTile.row, target.currentTile.column);
        }
        else
        {
            TowerSystem.ApplyTowerEffectOnTarget(ownerPlayer, towerCardId, target);
        }
    }

    // Executa a escolha da torre (chamado via RPC nos dois clientes)
    public void ExecuteTowerEffectOnTarget(int ownerPlayer, int towerCardId, int targetRow, int targetCol)
    {
        if (boardManager == null) boardManager = FindObjectOfType<BoardManager>();
        CardTile tile = boardManager != null ? boardManager.GetTile(targetRow, targetCol) : null;
        CardDisplay target = (tile != null && tile.occupiedCard != null)
            ? tile.occupiedCard.GetComponent<CardDisplay>() : null;
        if (target == null)
        {
            Debug.LogWarning($"[TowerTarget] Alvo em ({targetRow},{targetCol}) não existe mais — efeito perdido");
            return;
        }
        TowerSystem.ApplyTowerEffectOnTarget(ownerPlayer, towerCardId, target);
    }

    public void TryApplyEffectTarget(CardDisplay targetCard)
    {
        if (!isWaitingForEffectTarget) return;
        bool towerMode = towerTargetOwner != 0;
        if (!towerMode && effectTargetSource == null) return;

        if (effectTargetCandidates == null || !effectTargetCandidates.Contains(targetCard))
        {
            Debug.Log("[EffectTarget] Esta carta não é um alvo válido para o efeito! (ESC cancela)");
            return;
        }

        CardDisplay source = effectTargetSource;
        int effectType = effectTargetType;
        int towerOwner = towerTargetOwner;
        int towerCardId = towerTargetCardId;

        // Encerra o modo SEM puxar a fila ainda: a escolha clicada precisa ser
        // aplicada ANTES da próxima seleção enfileirada (senão um efeito da
        // fila podia matar o alvo clicado antes do RPC dele)
        isWaitingForEffectTarget = false;
        effectTargetSource = null;
        effectTargetType = 0;
        effectTargetCandidates = null;
        towerTargetOwner = 0;
        towerTargetCardId = -1;
        CardAuraIndicator.HideSelectableTargets();

        if (towerMode) ApplyTowerTargetChoice(towerOwner, towerCardId, targetCard);
        else ApplyEffectTargetChoice(source, effectType, targetCard);
        StartNextQueuedSelection();
    }

    public void CancelEffectTargetSelection()
    {
        isWaitingForEffectTarget = false;
        effectTargetSource = null;
        effectTargetType = 0;
        effectTargetCandidates = null;
        towerTargetOwner = 0;
        towerTargetCardId = -1;
        CardAuraIndicator.HideSelectableTargets();
        Debug.Log("[EffectTarget] Seleção de alvo encerrada");
        StartNextQueuedSelection();
    }

    // ── Fila de seleções de alvo ────────────────────────────────────────
    // Só UMA seleção por vez segura o estado; as demais esperam aqui. Sem a
    // fila, dois efeitos simultâneos (ex.: carta colocada enquanto um contador
    // de fim de turno pedia alvo) se atropelavam e um deles era perdido.
    private readonly Queue<System.Action> queuedSelections = new Queue<System.Action>();

    bool AnySelectionActive()
    {
        return isWaitingForEffectTarget || isWaitingForFreezeTarget || isWaitingForShieldBreakTargets;
    }

    void StartNextQueuedSelection()
    {
        // Re-invoca a próxima seleção enfileirada; se ela não entrar em modo
        // (alvos morreram nesse meio-tempo), segue para a seguinte
        while (queuedSelections.Count > 0 && !AnySelectionActive())
        {
            var next = queuedSelections.Dequeue();
            next();
        }
    }

    // Limpa TODOS os modos de seleção de alvo (chamado na passagem de turno).
    // Um modo preso sequestrava todos os cliques do tabuleiro e o jogador não
    // conseguia mais mover nenhuma carta — este é o cinto de segurança.
    public void CancelAllTargetSelections()
    {
        queuedSelections.Clear(); // Seleções enfileiradas morrem com o turno
        if (IsWaitingForFreezeTarget()) CancelFreezeSelection();
        if (IsWaitingForShieldBreakTargets()) CancelShieldBreakSelection();
        if (IsWaitingForEffectTarget()) CancelEffectTargetSelection();
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

        // Projétil visual do feitiço voando da carta lançadora até o alvo
        // (roxo = arcano ofensivo, dourado = bênção em aliado, azul = armadura,
        // gelo = congelamentos, fogo = danos diretos/explosões)
        Color projColor;
        switch (effectType)
        {
            case 5: projColor = EffectProjectileFX.Fire; break;       // destruir
            case 7: case 8: projColor = EffectProjectileFX.GoldBuff; break; // bênçãos
            case 9: projColor = EffectProjectileFX.ShieldBlue; break; // armadura
            case 10: case 13: case 14: projColor = EffectProjectileFX.Fire; break; // danos escolhidos
            case 11: case 12: case 15: case 16: projColor = EffectProjectileFX.Ice; break; // congelamentos
            default: projColor = EffectProjectileFX.Arcane; break;    // 4, 6...
        }
        // Explosão em área maiorzinha; bola de fogo do Mago 5 é a maior
        float projSize = effectType == 14 ? 1.6f : (effectType == 13 ? 1.35f : 1.15f);
        EffectProjectileFX.Launch(source, target, projColor, projSize);

        switch (effectType)
        {
            case 4: effect.ActivateCopyStats(target); break;        // Mage 5: copiar stats do inimigo
            case 5: effect.ActivateDestroyLowerTier(target); break; // Mage 4: destruir inimigo de tier inferior
            case 6: effect.ActivateRemoveBonus(target); break;      // Mage 4: remover bônus do inimigo
            case 7: effect.ActivateDoubleStats(target); break;      // Healer 5: duplicar stats de aliado
            case 8: effect.ActivateInvulnerability(target); break;  // Healer 4: invulnerabilidade em aliado
            case 9: effect.ActivateBoostMagoShield(target); break;  // Tank 3: +3 armadura em Mago aliado
            case 10: effect.ActivateDamageChosen(target); break;          // Mago: 1 de dano no alvo escolhido
            case 11: effect.ActivateFreezeChosen(target); break;          // Mago 3 (3/4): congelar escolhido
            case 12: effect.ActivateFreezeAndDamageChosen(target); break; // Mago 3 (3/4): congelar + dano
            case 13: effect.ActivateAreaBlastChosen(target); break;       // Explosão 2+1 (Mago 3 [2/5] / Mago 4 [4/4])
            case 14: effect.ActivateFireballChosen(target); break;        // Mago 5 (5/6): bola de fogo 5+2
            case 15: effect.ActivateFreezePerRoundChosen(target, true); break;  // Mago 5 (5/5): 1º congelamento
            case 16: effect.ActivateFreezePerRoundChosen(target, false); break; // Mago 5 (5/5): 2º congelamento (Tank)
            default:
                Debug.LogError($"[GameManager] Tipo de efeito com alvo desconhecido: {effectType}");
                break;
        }
    }

    public void CancelFreezeSelection()
    {
        isWaitingForFreezeTarget = false;
        mageFreezingCard = null;
        CardAuraIndicator.HideSelectableTargets();
        Debug.Log("[FreezeSelection] Seleção cancelada");
        StartNextQueuedSelection();
    }

    public bool IsWaitingForFreezeTarget()
    {
        return isWaitingForFreezeTarget;
    }

    // Sistema para seleção de 2 inimigos para quebra de armadura (Mage 2 ATK 4, HP 3)
    private CardDisplay shieldBreakMage = null;
    private int shieldBreakTargetsSelected = 0;
    private int shieldBreakTargetsRequired = 2;
    private CardDisplay shieldBreakFirstTarget = null;
    private bool isWaitingForShieldBreakTargets = false;

    public void StartShieldBreakSelection(CardDisplay mageCard)
    {
        // Carta do BOT (modo treino): escolhe os alvos sozinho via RPC_EffectTarget
        if (BotMode.IsBot(mageCard.ownerPlayerNumber))
        {
            BotController.AutoChooseShieldBreakTargets(mageCard);
            return;
        }

        // Em multiplayer, só o dono do Mago escolhe os alvos (as escolhas chegam por RPC)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            mageCard.ownerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[ShieldBreakSelection] Oponente está escolhendo os alvos...");
            return;
        }

        // Outra seleção em andamento: entra na fila (não sobrescreve o estado)
        if (AnySelectionActive())
        {
            queuedSelections.Enqueue(() => StartShieldBreakSelection(mageCard));
            Debug.Log("[ShieldBreakSelection] Seleção enfileirada (outra em andamento)");
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

        // Moldura dourada pulsando nos inimigos selecionáveis
        if (board != null)
            CardAuraIndicator.ShowSelectableTargets(board.GetCardsByOwner(enemyPlayer));
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
            CardAuraIndicator.HideSelectableTargets();
            Debug.Log("[ShieldBreakSelection] Seleção completa!");
            StartNextQueuedSelection();
        }
        else
        {
            // Falta o 2º alvo: reacende as molduras SEM o inimigo já escolhido
            BoardManager board = BoardManager.Instance;
            if (board != null && shieldBreakMage != null)
            {
                int enemyPlayer = shieldBreakMage.ownerPlayerNumber == 1 ? 2 : 1;
                var remaining = board.GetCardsByOwner(enemyPlayer)
                    .FindAll(e => e != null && e != shieldBreakFirstTarget);
                CardAuraIndicator.ShowSelectableTargets(remaining);
            }
            Debug.Log($"[ShieldBreakSelection] Alvo selecionado, falta(m) {shieldBreakTargetsRequired - shieldBreakTargetsSelected}");
        }
    }

    public void CancelShieldBreakSelection()
    {
        isWaitingForShieldBreakTargets = false;
        shieldBreakMage = null;
        shieldBreakTargetsSelected = 0;
        shieldBreakFirstTarget = null;
        CardAuraIndicator.HideSelectableTargets();
        Debug.Log("[ShieldBreakSelection] Seleção cancelada");
        StartNextQueuedSelection();
    }

    public bool IsWaitingForShieldBreakTargets()
    {
        return isWaitingForShieldBreakTargets;
    }
}
