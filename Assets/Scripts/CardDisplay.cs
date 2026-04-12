using UnityEngine;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Referência da Carta")]
    public Card card;

    [Header("Estado da Carta")]
    public bool isInHand = false;
    public bool isOnBoard = false;
    public bool isInShop = true; // Começa na loja (fase de compra)
    public CardTile currentTile; // Tile onde a carta está atualmente
    private HandManager handManager;
    private Vector3 originalScale;

    [Header("UI Elements (Assign in Inspector)")]
    public TextMeshPro cardNameText;
    public TextMeshPro attackText;
    public TextMeshPro shieldText;
    public TextMeshPro healthText;
    public TextMeshPro tierText;
    public Renderer artworkRenderer;
    public Renderer backgroundRenderer;

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
    }

    public void SetCard(Card newCard)
    {
        card = newCard;
        UpdateCardDisplay();
    }

    void UpdateCardDisplay()
    {
        if (card == null) return;

        // Atualiza textos
        if (cardNameText != null) cardNameText.text = card.cardName;
        if (attackText != null) attackText.text = card.attack.ToString("00");
        if (shieldText != null) shieldText.text = card.shield.ToString("00");
        if (healthText != null) healthText.text = card.health.ToString("00");
        if (tierText != null) tierText.text = ((int)card.tier).ToString();

        // Atualiza artwork
        if (artworkRenderer != null && card.artwork != null)
        {
            // Cria um material com a textura do sprite
            Material artworkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            artworkMat.mainTexture = card.artwork.texture;
            artworkRenderer.material = artworkMat;
        }

        // Atualiza cor de fundo baseado na classe
        if (backgroundRenderer != null)
        {
            backgroundRenderer.material.color = GetClassColor(card.cardClass);
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
        // Destaque visual quando passar o mouse (apenas se não estiver na mão)
        if (!isInHand && originalScale != Vector3.zero)
        {
            transform.localScale = originalScale * 1.1f;
        }
    }

    void OnMouseExit()
    {
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
            if (handManager == null)
            {
                handManager = FindObjectOfType<HandManager>();
            }

            if (handManager != null)
            {
                bool added = handManager.AddCardToHand(gameObject);
                if (added)
                {
                    isInHand = true;
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

        // Remove da loja e adiciona à mão
        isInShop = false;

        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }

        if (handManager != null)
        {
            bool added = handManager.AddCardToHand(gameObject);
            if (added)
            {
                isInHand = true;
                if (originalScale != Vector3.zero)
                {
                    transform.localScale = originalScale;
                }
                Debug.Log($"{currentPlayer.playerName} comprou '{card.cardName}' por {cost} ouro! Ouro restante: {currentPlayer.gold}");
            }
        }
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
}
