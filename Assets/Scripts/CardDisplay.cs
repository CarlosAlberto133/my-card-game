using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CardDisplay : MonoBehaviour
{
    [Header("Referência da Carta")]
    public Card card;

    [Header("Estado da Carta")]
    public bool isInHand = false;
    public bool isOnBoard = false;
    public bool isInShop = true; // Começa na loja (fase de compra)
    public CardTile currentTile; // Tile onde a carta está atualmente
    public int ownerPlayerNumber = 0; // 0 = sem dono (loja), 1 = Player1, 2 = Player2
    public int lastMovedRound = -1; // Em qual round a carta se moveu pela última vez (-1 = nunca)
    public int lastAttackedRound = -1; // Em qual round a carta atacou pela última vez (-1 = nunca)

    // Stats atuais (mudam durante o jogo)
    public int currentHealth;
    public int currentShield;
    public int currentAttack;

    private HandManager handManager;
    private Vector3 originalScale;
    private bool isMouseOver = false; // Flag para saber se o mouse está sobre a carta

    [Header("UI Elements (Assign in Inspector)")]
    public TextMeshPro cardNameText;
    public TextMeshPro attackText;
    public TextMeshPro shieldText;
    public TextMeshPro healthText;
    public TextMeshPro tierText;
    public TextMeshPro effectText;
    public TextMeshPro classText;
    public Renderer artworkRenderer;
    public Renderer backgroundRenderer;
    public Renderer tierBarRenderer;

    [Header("Cores por Classe")]
    public Color tankColor = new Color(0.7f, 0.7f, 0.7f); // Cinza
    public Color magoColor = new Color(0.5f, 0.3f, 0.9f); // Roxo
    public Color healerColor = new Color(0.3f, 0.9f, 0.5f); // Verde
    public Color arqueiroColor = new Color(0.9f, 0.6f, 0.3f); // Laranja

    void Awake()
    {
        // Inicializa a escala original antes de qualquer coisa
        originalScale = transform.localScale;
    }

    void Start()
    {
        // Auto-atribui elementos se não foram setados manualmente
        AutoAssignElements();

        if (card != null)
        {
            UpdateCardDisplay();
        }

        // Encontra o HandManager
        handManager = FindObjectOfType<HandManager>();
    }

    void Update()
    {
        // Se a carta está no tabuleiro e pertence ao jogador atual, permite atacar com botão direito ou tecla A
        if (isOnBoard && isMouseOver && TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;
            if (ownerPlayerNumber == currentPlayerNumber)
            {
                // Verifica inputs com o novo Input System
                Mouse mouse = Mouse.current;
                Keyboard keyboard = Keyboard.current;

                if (mouse != null && keyboard != null)
                {
                    // Botão direito do mouse ou tecla A para atacar
                    if (mouse.rightButton.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                    {
                        Debug.Log($"Tentando atacar com {card.cardName}!");
                        AttackAdjacentEnemy();
                    }
                }
            }
        }
    }

    // Verifica se a carta pode se mover neste round
    public bool CanMoveThisRound()
    {
        if (TurnManager.Instance == null) return true;

        // Se nunca se moveu ou se moveu em um round anterior, pode mover
        return lastMovedRound < TurnManager.Instance.currentRound;
    }

    // Verifica se a carta pode atacar neste round
    public bool CanAttackThisRound()
    {
        if (TurnManager.Instance == null)
        {
            Debug.Log($"{card.cardName}: TurnManager é null, permitindo ataque");
            return true;
        }

        if (!isOnBoard)
        {
            Debug.Log($"{card.cardName}: Não está no tabuleiro, não pode atacar");
            return false;
        }

        bool canAttack = lastAttackedRound < TurnManager.Instance.currentRound;
        Debug.Log($"{card.cardName}: lastAttackedRound={lastAttackedRound}, currentRound={TurnManager.Instance.currentRound}, canAttack={canAttack}");

        return canAttack;
    }

    void AutoAssignElements()
    {
        // Encontra automaticamente os elementos filhos por nome
        if (cardNameText == null)
        {
            Transform nameTransform = transform.Find("CardNameText");
            if (nameTransform != null) cardNameText = nameTransform.GetComponent<TextMeshPro>();
        }

        if (attackText == null)
        {
            Transform attackTransform = transform.Find("AttackText");
            if (attackTransform != null) attackText = attackTransform.GetComponent<TextMeshPro>();
        }

        if (shieldText == null)
        {
            Transform shieldTransform = transform.Find("ShieldText");
            if (shieldTransform != null) shieldText = shieldTransform.GetComponent<TextMeshPro>();
        }

        if (healthText == null)
        {
            Transform healthTransform = transform.Find("HealthText");
            if (healthTransform != null) healthText = healthTransform.GetComponent<TextMeshPro>();
        }

        if (tierText == null)
        {
            Transform tierTransform = transform.Find("TierText");
            if (tierTransform != null) tierText = tierTransform.GetComponent<TextMeshPro>();
        }

        if (artworkRenderer == null)
        {
            Transform artworkTransform = transform.Find("Artwork");
            if (artworkTransform != null) artworkRenderer = artworkTransform.GetComponent<Renderer>();
        }

        if (backgroundRenderer == null)
        {
            Transform bgTransform = transform.Find("Background");
            if (bgTransform != null) backgroundRenderer = bgTransform.GetComponent<Renderer>();
        }

        if (effectText == null)
        {
            Transform t = transform.Find("EffectText");
            if (t != null) effectText = t.GetComponent<TextMeshPro>();
        }

        if (classText == null)
        {
            Transform t = transform.Find("ClassText");
            if (t != null) classText = t.GetComponent<TextMeshPro>();
        }

        if (tierBarRenderer == null)
        {
            Transform t = transform.Find("TierBar");
            if (t != null) tierBarRenderer = t.GetComponent<Renderer>();
        }
    }

    public void SetCard(Card newCard)
    {
        card = newCard;

        // Inicializa stats atuais com os valores base da carta
        if (card != null)
        {
            currentHealth = card.health;
            currentShield = card.shield;
            currentAttack = card.attack;
        }

        // Garante que os campos estão atribuídos mesmo se Start() ainda não rodou
        AutoAssignElements();
        UpdateCardDisplay();
    }

    void UpdateCardDisplay()
    {
        if (card == null) return;

        // Atualiza textos usando stats atuais (não os base)
        if (cardNameText != null) cardNameText.text = card.cardName;
        // Sem zero à esquerda e sem quebra de linha: a caixa é estreita e o formato
        // "00" partia o número em duas linhas (0 em cima, dígito real em baixo),
        // dando a ilusão de duas filas de stats.
        if (attackText != null)
        {
            attackText.textWrappingMode = TextWrappingModes.NoWrap;
            attackText.text = currentAttack.ToString();
        }
        if (shieldText != null)
        {
            shieldText.textWrappingMode = TextWrappingModes.NoWrap;
            shieldText.text = currentShield.ToString();
        }
        if (healthText != null)
        {
            healthText.textWrappingMode = TextWrappingModes.NoWrap;
            healthText.text = currentHealth.ToString();
        }
        if (tierText != null) tierText.text = ((int)card.tier).ToString();

        // Atualiza artwork
        if (artworkRenderer != null && card.artwork != null)
        {
            Shader artShader = Shader.Find("Unlit/Texture")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Standard");
            Material artworkMat = new Material(artShader);
            artworkMat.mainTexture = card.artwork.texture;
            artworkMat.SetTexture("_BaseMap", card.artwork.texture); // URP
            // O quad do Artwork já é criado com rotação Euler(90,0,180) que orienta a
            // imagem corretamente. Não invertemos o V aqui para não duplicar a inversão
            // (isso deixava a foto de cabeça para baixo).
            artworkMat.mainTextureScale = new Vector2(1, 1);
            artworkMat.mainTextureOffset = new Vector2(0, 0);
            artworkMat.SetTextureScale("_BaseMap", new Vector2(1, 1));   // URP
            artworkMat.SetTextureOffset("_BaseMap", new Vector2(0, 0));  // URP
            artworkRenderer.material = artworkMat;
        }

        // Atualiza cor de fundo baseado na classe
        if (backgroundRenderer != null)
        {
            Color classColor = GetClassColor(card.cardClass);
            backgroundRenderer.material.color = classColor;
            backgroundRenderer.material.SetColor("_BaseColor", classColor);
        }

        // Atualiza texto de efeito
        if (effectText != null)
        {
            effectText.text = string.IsNullOrEmpty(card.effectDescription) ? "Sem efeito" : card.effectDescription;
        }

        // Atualiza texto de classe
        if (classText != null)
        {
            classText.text = card.cardClass.ToString();
        }

        // Atualiza cor da barra de tier
        if (tierBarRenderer != null)
        {
            Color tierColor = GetTierColor(card.tier);
            tierBarRenderer.material.color = tierColor;
            tierBarRenderer.material.SetColor("_BaseColor", tierColor);
        }

        // Aplica as cores dos quads estáticos (corrige shaders URP em runtime)
        ApplyCardTheme();
    }

    // Define a cor dos quads estáticos que não mudam por carta
    void ApplyCardTheme()
    {
        SetQuadColor("Border", new Color(0.06f, 0.06f, 0.10f));
        SetQuadColor("NameHeader", new Color(0.18f, 0.18f, 0.28f));
        SetQuadColor("EffectBackground", new Color(0.22f, 0.22f, 0.32f));
        SetQuadColor("StatsBackground", new Color(0.16f, 0.16f, 0.24f));
        SetQuadColor("StatsDivider1", new Color(0.35f, 0.35f, 0.55f));
        SetQuadColor("StatsDivider2", new Color(0.35f, 0.35f, 0.55f));
        // Artwork cinza enquanto não há imagem
        if (artworkRenderer == null || card.artwork == null)
            SetQuadColor("Artwork", new Color(0.28f, 0.28f, 0.28f));

        // Oculta TextMeshPro filhos com nomes não reconhecidos (labels de prefabs antigos)
        // Usa comparação por nome para não depender dos campos estarem preenchidos
        foreach (Transform child in transform)
        {
            if (child.GetComponent<TextMeshPro>() == null) continue;
            string n = child.name;
            if (n == "CardNameText" || n == "AttackText" || n == "ShieldText" ||
                n == "HealthText" || n == "TierText" || n == "EffectText" ||
                n == "ClassText") continue;
            child.gameObject.SetActive(false);
        }
    }

    void HideChild(string childName)
    {
        Transform t = transform.Find(childName);
        if (t != null) t.gameObject.SetActive(false);
    }

    void SetQuadColor(string childName, Color color)
    {
        Transform t = transform.Find(childName);
        if (t == null) return;
        Renderer r = t.GetComponent<Renderer>();
        if (r == null) return;
        r.material.color = color;
        r.material.SetColor("_BaseColor", color);
        r.material.SetColor("_Color", color);
    }

    Color GetTierColor(CardTier tier)
    {
        switch (tier)
        {
            case CardTier.Tier1: return new Color(0.55f, 0.55f, 0.55f);  // Prata
            case CardTier.Tier2: return new Color(0.80f, 0.65f, 0.00f);  // Dourado
            case CardTier.Tier3: return new Color(1.00f, 0.55f, 0.00f);  // Laranja
            case CardTier.Tier4: return new Color(0.55f, 0.00f, 1.00f);  // Roxo
            case CardTier.Tier5: return new Color(0.86f, 0.08f, 0.24f);  // Carmesim
            default: return Color.gray;
        }
    }

    Color GetClassColor(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank:
                return tankColor;
            case CardClass.Mago:
                return magoColor;
            case CardClass.Healer:
                return healerColor;
            case CardClass.Arqueiro:
                return arqueiroColor;
            default:
                return Color.white;
        }
    }

    // Para interação com o mouse
    void OnMouseEnter()
    {
        isMouseOver = true;

        // Destaque visual quando passar o mouse (apenas se não estiver na mão)
        if (!isInHand && originalScale != Vector3.zero)
        {
            transform.localScale = originalScale * 1.1f;
        }
    }

    void OnMouseExit()
    {
        isMouseOver = false;

        // Volta ao tamanho normal (apenas se não estiver na mão)
        if (!isInHand && originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
    }

    void OnMouseDown()
    {
        // Verifica se a carta foi inicializada
        if (card == null)
        {
            Debug.LogWarning("Carta não foi inicializada ainda!");
            return;
        }

        // Se a carta NÃO está na loja (está na mão ou tabuleiro), verifica se pertence ao jogador atual
        if (!isInShop && TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;

            if (ownerPlayerNumber != 0 && ownerPlayerNumber != currentPlayerNumber)
            {
                // Carta inimiga clicada - tenta atacar se houver uma carta selecionada
                if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
                {
                    Debug.Log($"Tentando atacar carta inimiga {card.cardName}!");
                    GameManager.Instance.TryAttackEnemyCard(this);
                    return;
                }
                else
                {
                    Debug.Log($"Esta carta pertence ao Jogador {ownerPlayerNumber}! Você é o Jogador {currentPlayerNumber}.");
                    return; // Bloqueia a interação
                }
            }
        }

        // Se a carta está no tabuleiro, seleciona para mover
        if (isOnBoard)
        {
            if (GameManager.Instance != null)
            {
                // Usa o tile armazenado ou tenta encontrar
                CardTile tile = currentTile != null ? currentTile : FindCurrentTile();
                if (tile != null)
                {
                    GameManager.Instance.SelectCardFromBoard(gameObject, this, tile);
                }
                else
                {
                    Debug.LogError("Não foi possível encontrar o tile onde a carta está!");
                }
            }
            return;
        }

        // Se a carta está na loja (fase de compra)
        if (isInShop)
        {
            TryBuyCard();
            return;
        }

        // Se a carta ainda não está na mão, adiciona (fluxo antigo)
        if (!isInHand)
        {
            // Busca HandManager do jogador atual se estiver em modo multiplayer
            if (TurnManager.Instance != null)
            {
                PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();
                handManager = GetHandManagerForPlayer(currentPlayer.playerNumber);
                ownerPlayerNumber = currentPlayer.playerNumber; // Define o dono
            }
            else if (handManager == null)
            {
                // Fallback para modo single player
                handManager = FindObjectOfType<HandManager>();
            }

            if (handManager != null)
            {
                bool added = handManager.AddCardToHand(gameObject);
                if (added)
                {
                    isInHand = true;
                    isInShop = false;
                    if (originalScale != Vector3.zero)
                    {
                        transform.localScale = originalScale; // Reseta o tamanho
                    }
                    Debug.Log($"Carta '{card.cardName}' adicionada à mão!");
                }
            }
            else
            {
                Debug.LogError("HandManager não encontrado! Certifique-se de criar um GameObject com o componente HandManager.");
            }
        }
        else
        {
            // Carta está na mão - seleciona para colocar no tabuleiro
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectCardFromHand(gameObject, this);
            }
            else
            {
                Debug.LogError("GameManager não encontrado! Certifique-se de criar um GameObject com o componente GameManager.");
            }
        }
    }

    void TryBuyCard()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager não encontrado!");
            return;
        }

        PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();

        // Verifica se o jogador já comprou sua carta neste turno
        if (!currentPlayer.CanBuyCard())
        {
            Debug.Log($"{currentPlayer.playerName} já comprou 1 carta neste turno!");
            return;
        }

        int cost = card.GetGoldCost();

        // Verifica se o jogador tem ouro suficiente
        if (!currentPlayer.HasEnoughGold(cost))
        {
            Debug.Log($"{currentPlayer.playerName} não tem ouro suficiente! Precisa de {cost}, tem {currentPlayer.gold}");
            return;
        }

        // Compra a carta
        currentPlayer.BuyCard(cost);

        // Remove da loja e adiciona à mão DO JOGADOR CORRETO
        isInShop = false;
        ownerPlayerNumber = currentPlayer.playerNumber; // Define o dono da carta

        // Busca o HandManager do jogador correto
        HandManager correctHandManager = GetHandManagerForPlayer(currentPlayer.playerNumber);

        if (correctHandManager != null)
        {
            bool added = correctHandManager.AddCardToHand(gameObject);
            if (added)
            {
                isInHand = true;
                handManager = correctHandManager; // Atualiza a referência
                if (originalScale != Vector3.zero)
                {
                    transform.localScale = originalScale;
                }
                Debug.Log($"{currentPlayer.playerName} comprou '{card.cardName}' por {cost} ouro! Ouro restante: {currentPlayer.gold}");
            }
        }
        else
        {
            Debug.LogError($"HandManager para {currentPlayer.playerName} não encontrado!");
        }
    }

    // Busca o HandManager correto para o jogador
    HandManager GetHandManagerForPlayer(int playerNum)
    {
        HandManager[] allHandManagers = FindObjectsOfType<HandManager>();
        foreach (HandManager hm in allHandManagers)
        {
            if (hm.playerNumber == playerNum)
            {
                return hm;
            }
        }
        return null;
    }

    // Encontra o tile atual onde a carta está posicionada
    CardTile FindCurrentTile()
    {
        // Primeiro tenta com raycast na direção para baixo
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        // Faz raycast para baixo com distância suficiente
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 10f))
        {
            CardTile tile = hit.collider.GetComponent<CardTile>();
            if (tile != null)
            {
                return tile;
            }
        }

        // Se não encontrar com raycast, busca o tile mais próximo
        CardTile[] allTiles = FindObjectsOfType<CardTile>();
        CardTile closestTile = null;
        float closestDistance = float.MaxValue;

        foreach (CardTile tile in allTiles)
        {
            // Calcula distância horizontal (ignora Y)
            Vector3 tilePos = tile.transform.position;
            Vector3 cardPos = transform.position;
            float distance = Vector2.Distance(
                new Vector2(tilePos.x, tilePos.z),
                new Vector2(cardPos.x, cardPos.z)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
        }

        // Retorna o tile mais próximo se estiver a menos de 1.5 unidades
        if (closestTile != null && closestDistance < 1.5f)
        {
            return closestTile;
        }

        return null;
    }

    // ============ SISTEMA DE COMBATE ============

    // Retorna lista de cartas inimigas adjacentes (somente adjacentes ortogonais: cima, baixo, esquerda, direita)
    public System.Collections.Generic.List<CardDisplay> GetAdjacentEnemies()
    {
        System.Collections.Generic.List<CardDisplay> enemies = new System.Collections.Generic.List<CardDisplay>();

        if (!isOnBoard || currentTile == null)
        {
            Debug.Log($"{card.cardName}: Não está no tabuleiro ou currentTile é null");
            return enemies;
        }

        BoardManager boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("BoardManager não encontrado!");
            return enemies;
        }

        Debug.Log($"{card.cardName}: Verificando inimigos adjacentes. Posição: [{currentTile.row},{currentTile.column}], Owner: {ownerPlayerNumber}");

        // Verifica os 4 tiles adjacentes (cima, baixo, esquerda, direita)
        int[][] directions = new int[][]
        {
            new int[] { -1, 0 },  // Cima
            new int[] { 1, 0 },   // Baixo
            new int[] { 0, -1 },  // Esquerda
            new int[] { 0, 1 }    // Direita
        };

        foreach (int[] dir in directions)
        {
            int newRow = currentTile.row + dir[0];
            int newCol = currentTile.column + dir[1];

            CardTile adjacentTile = boardManager.GetTile(newRow, newCol);
            if (adjacentTile != null && adjacentTile.occupiedCard != null)
            {
                CardDisplay enemyCard = adjacentTile.occupiedCard.GetComponent<CardDisplay>();
                if (enemyCard != null)
                {
                    Debug.Log($"  Carta adjacente em [{newRow},{newCol}]: {enemyCard.card.cardName}, Owner: {enemyCard.ownerPlayerNumber}");

                    if (enemyCard.ownerPlayerNumber != ownerPlayerNumber && enemyCard.ownerPlayerNumber != 0)
                    {
                        enemies.Add(enemyCard);
                        Debug.Log($"    -> Adicionada como inimiga!");
                    }
                }
            }
        }

        Debug.Log($"{card.cardName}: Total de inimigos encontrados: {enemies.Count}");
        return enemies;
    }

    // Ataca a primeira carta inimiga adjacente encontrada
    public bool AttackAdjacentEnemy()
    {
        Debug.Log($"=== AttackAdjacentEnemy chamado para {card.cardName} ===");

        // Verifica se pode atacar
        if (!CanAttackThisRound())
        {
            Debug.Log($"{card.cardName} já atacou neste round!");
            return false;
        }

        // Busca inimigos adjacentes
        System.Collections.Generic.List<CardDisplay> enemies = GetAdjacentEnemies();

        if (enemies.Count == 0)
        {
            Debug.Log($"{card.cardName} não tem inimigos adjacentes para atacar!");
            return false;
        }

        // Ataca o primeiro inimigo encontrado
        CardDisplay target = enemies[0];
        int damageDealt = currentAttack;

        Debug.Log($">>> {card.cardName} (ATK:{currentAttack}) ataca {target.card.cardName} (HP:{target.currentHealth}, Shield:{target.currentShield}) causando {damageDealt} de dano!");

        target.TakeDamage(damageDealt);

        // Marca que atacou neste round
        if (TurnManager.Instance != null)
        {
            lastAttackedRound = TurnManager.Instance.currentRound;
            Debug.Log($"{card.cardName} atacou no round {TurnManager.Instance.currentRound}");
        }

        return true;
    }

    // Recebe dano (primeiro absorve no escudo, depois na vida)
    public void TakeDamage(int damage)
    {
        Debug.Log($"{card.cardName} recebeu {damage} de dano! Shield: {currentShield}, Vida: {currentHealth}");

        // Primeiro o escudo absorve o dano
        if (currentShield > 0)
        {
            int shieldAbsorbed = Mathf.Min(currentShield, damage);
            currentShield -= shieldAbsorbed;
            damage -= shieldAbsorbed;
            Debug.Log($"Escudo absorveu {shieldAbsorbed} de dano! Escudo restante: {currentShield}");
        }

        // O dano que sobrou vai para a vida
        if (damage > 0)
        {
            currentHealth -= damage;
            Debug.Log($"{card.cardName} perdeu {damage} de vida! Vida restante: {currentHealth}");
        }

        // Atualiza a UI
        UpdateCardDisplay();

        // Verifica se a carta morreu
        if (currentHealth <= 0)
        {
            Debug.Log($"{card.cardName} foi destruída!");
            DestroyCard();
        }
    }

    // Destrói a carta
    void DestroyCard()
    {
        // Libera o tile se a carta estiver no tabuleiro
        if (isOnBoard && currentTile != null)
        {
            currentTile.FreeTile();
        }

        // Remove da mão se estiver lá
        if (isInHand && handManager != null)
        {
            handManager.RemoveCardFromHand(gameObject);
        }

        Debug.Log($"Carta '{card.cardName}' destruída!");
        Destroy(gameObject);
    }
}
