using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Referências")]
    public GameObject cardPrefab;
    public CardPool cardPool;

    [Header("Configurações de Spawn")]
    public int numberOfCards = 5;
    public Vector3 centerPosition = new Vector3(0, 1.5f, 0);
    public Vector3 shopPosition = new Vector3(10, 1.5f, 0); // Posição à direita do tabuleiro
    public float cardSpacing = 4f;
    public float cardScale = 1.5f;

    private List<GameObject> spawnedCards = new List<GameObject>();
    private Vector3 currentSpawnPosition;
    private bool verticalLayout = false; // Se true, cartas ficam uma abaixo da outra
    private int shopSpawnCount = 0; // Quantas vezes a loja já foi gerada (para derivar seed única por spawn)

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

        // Força os valores por código (os da cena estavam desatualizados):
        // cartas 2x maiores e espaçamento que evita sobreposição na loja
        cardScale = 3f;
        cardSpacing = 8f;

        // Altura correta para a nova escala (a base da carta não pode afundar no chão)
        float shopY = CardDisplay.GroundY(cardScale);
        centerPosition = new Vector3(centerPosition.x, shopY, centerPosition.z);
        shopPosition = new Vector3(shopPosition.x, shopY, shopPosition.z);

        // Inicialmente no centro (lobby)
        currentSpawnPosition = centerPosition;
    }

    void Start()
    {
        // Encontra o CardPool se não foi atribuído
        if (cardPool == null)
        {
            cardPool = FindObjectOfType<CardPool>();
        }

        // Em multiplayer, quem spawna a loja é o PhotonGameManager (quando a seed chega).
        // Só spawna direto aqui em modo offline/single player.
        if (PhotonNetwork.inRoom)
        {
            Debug.Log("[CardManager] Multiplayer: aguardando seed sincronizada para spawnar a loja...");
        }
        else
        {
            SpawnRandomCards();
        }
    }

    void Update()
    {
        // Pressione R para spawnar novas cartas aleatórias (para teste)
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RefreshCards();
        }
    }

    public void SpawnRandomCards()
    {
        Debug.Log("[CardManager] SpawnRandomCards() chamado");

        // Sincroniza seed com PhotonGameManager para gerar mesmas cartas em ambos
        if (PhotonGameManager.Instance != null)
        {
            int baseSeed = PhotonGameManager.Instance.currentGameSeed;

            if (baseSeed > 0)
            {
                // Deriva uma seed diferente a cada spawn (senão o refresh repetiria a mesma loja),
                // mas determinística: ambos os clientes fazem o mesmo número de spawns
                int derivedSeed = baseSeed + shopSpawnCount * 7919;
                UnityEngine.Random.InitState(derivedSeed);
                shopSpawnCount++;
                Debug.Log($"[CardManager] Aplicado Random.InitState({derivedSeed}) (spawn #{shopSpawnCount})");
            }
            else
            {
                Debug.LogWarning("[CardManager] Seed ainda é 0! Usando Random padrão.");
            }
        }

        // Limpa cartas anteriores
        ClearSpawnedCards();

        if (cardPool == null)
        {
            cardPool = FindObjectOfType<CardPool>();
        }

        if (cardPool == null)
        {
            Debug.LogError("CardPool não encontrado!");
            return;
        }

        // Spawna cartas aleatórias
        for (int i = 0; i < numberOfCards; i++)
        {
            CardInstance randomCard = cardPool.DrawRandomCard();

            if (randomCard != null)
            {
                Vector3 position;

                if (verticalLayout)
                {
                    // Layout vertical: cartas uma abaixo da outra (eixo Z)
                    float totalDepth = (numberOfCards - 1) * cardSpacing;
                    Vector3 startPosition = currentSpawnPosition - new Vector3(0, 0, totalDepth / 2f);
                    position = startPosition + new Vector3(0, 0, i * cardSpacing);
                }
                else
                {
                    // Layout horizontal: cartas lado a lado (eixo X)
                    float totalWidth = (numberOfCards - 1) * cardSpacing;
                    Vector3 startPosition = currentSpawnPosition - new Vector3(totalWidth / 2f, 0, 0);
                    position = startPosition + new Vector3(i * cardSpacing, 0, 0);
                }

                GameObject cardObject = SpawnCard(randomCard.cardData, position);
                spawnedCards.Add(cardObject);

                Debug.Log($"Spawnou: {randomCard.cardData.cardName} (ID: {randomCard.instanceId})");
            }
        }
    }

    public void OnGameStart()
    {
        Debug.Log("========== CardManager: OnGameStart ==========");
        Debug.Log($"Posição anterior: {currentSpawnPosition}");
        Debug.Log($"Nova posição (shopPosition): {shopPosition}");

        currentSpawnPosition = shopPosition;
        verticalLayout = true; // Ativa layout vertical quando a partida começa

        Debug.Log($"Spawning novas cartas em: {currentSpawnPosition} (layout vertical)");
        SpawnRandomCards();
        Debug.Log("========== CardManager: OnGameStart Completo ==========");
    }

    // Retorna o índice de uma carta na loja (-1 se não está na loja)
    public int GetShopCardIndex(GameObject cardObject)
    {
        return spawnedCards.IndexOf(cardObject);
    }

    // Retorna a carta da loja em um índice específico
    public GameObject GetShopCard(int index)
    {
        if (index < 0 || index >= spawnedCards.Count) return null;
        return spawnedCards[index];
    }

    public void RefreshShop()
    {
        Debug.Log("CardManager: Refresh da loja com 5 novas cartas!");
        Debug.Log($"Cartas antes do refresh: {spawnedCards.Count}");
        SpawnRandomCards();
        Debug.Log($"Cartas após refresh: {spawnedCards.Count}");
    }

    void ClearSpawnedCards()
    {
        int destroyed = 0;
        int preserved = 0;

        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
            {
                // Verifica se a carta está na mão ou no tabuleiro
                CardDisplay cardDisplay = card.GetComponent<CardDisplay>();
                if (cardDisplay != null && (cardDisplay.isInHand || cardDisplay.isOnBoard))
                {
                    // NÃO destrói cartas que estão nas mãos ou no tabuleiro
                    Debug.Log($"Preservando carta '{cardDisplay.card.cardName}' (na mão ou tabuleiro)");
                    preserved++;
                    continue;
                }

                // Só destrói cartas que ainda estão na loja
                Destroy(card);
                destroyed++;
            }
        }

        Debug.Log($"Limpeza da loja: {destroyed} destruídas, {preserved} preservadas");

        // Esvazia a lista COMPLETA: as destruídas ainda não são "null" neste frame
        // (Destroy é adiado), e as preservadas já saíram da loja (mão/tabuleiro).
        // Sem isso, a lista acumulava entradas mortas e os índices da loja ficavam errados.
        spawnedCards.Clear();
    }

    public void DestroyAllCards()
    {
        Debug.Log($"Destruindo TODAS as cartas ({spawnedCards.Count} cartas)...");

        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }

        spawnedCards.Clear();
        Debug.Log("Todas as cartas foram destruídas!");
    }

    public GameObject SpawnCard(Card card, Vector3 position)
    {
        if (cardPrefab == null || card == null)
        {
            Debug.LogError("Card Prefab ou Card está nulo!");
            return null;
        }

        GameObject cardObject = Instantiate(cardPrefab, position, Quaternion.Euler(90, 180, 0));
        cardObject.transform.localScale = Vector3.one * cardScale;

        CardDisplay display = cardObject.GetComponent<CardDisplay>();

        if (display != null)
        {
            display.SetCard(card);
        }

        // Adiciona e configura efeitos simples
        SetupCardEffect(cardObject, card);

        return cardObject;
    }

    void SetupCardEffect(GameObject cardObject, Card card)
    {
        CardEffectSimple effect = cardObject.GetComponent<CardEffectSimple>();
        if (effect == null)
        {
            effect = cardObject.AddComponent<CardEffectSimple>();
        }

        // Aqui você pode adicionar lógica para chamar os efeitos quando apropriado
        Debug.Log($"[CardEffect] Setup para {card.cardName} ({card.cardClass})");
    }

    // Reseta e spawna novas cartas aleatórias
    public void RefreshCards()
    {
        SpawnRandomCards();
    }

    // Spawna uma carta específica em uma posição
    public GameObject SpawnCardInstance(CardInstance cardInstance, Vector3 position)
    {
        if (cardInstance != null)
        {
            return SpawnCard(cardInstance.cardData, position);
        }
        return null;
    }

    // Spawna uma carta em um tile específico
    public CardDisplay SpawnCardOnTile(Card card, CardTile tile, int ownerPlayerNumber)
    {
        if (card == null || tile == null) return null;

        GameObject cardObject = SpawnCard(card,
            tile.transform.position + Vector3.up * CardDisplay.BoardYOffset);
        if (cardObject == null) return null;
        cardObject.transform.rotation = CardDisplay.BoardRotation; // Deitada sobre o tile

        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            // Coloca a carta no tile
            tile.OccupyTile(cardObject);
            cardObject.transform.localScale = Vector3.one * CardDisplay.BoardScale;
            cardDisplay.currentTile = tile;
            cardDisplay.isOnBoard = true;
            cardDisplay.ownerPlayerNumber = ownerPlayerNumber;
            cardDisplay.UpdateDisplay(); // Atualiza a borda com a cor do dono

            // Aplica o efeito de entrada em campo
            cardDisplay.ApplyCardEffect();
        }

        return cardDisplay;
    }

    // Invoca um Archer aleatório (tier 1-4) quando um Archer 2 (ATK 3, HP 3) mata inimigo
    public void InvokeRandomArcher(int ownerPlayerNumber, CardTile nearTile)
    {
        CardPool cardPool = FindObjectOfType<CardPool>();
        if (cardPool == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Archers nos tiers 1-4
        var allCards = cardPool.GetAvailableCards();
        var archerCards = allCards.FindAll(c =>
            c.cardData.cardClass == CardClass.Arqueiro &&
            (c.cardData.tier == CardTier.Tier1 || c.cardData.tier == CardTier.Tier2 ||
             c.cardData.tier == CardTier.Tier3 || c.cardData.tier == CardTier.Tier4));

        if (archerCards.Count == 0)
        {
            Debug.Log("[InvokeArcher] Nenhum Archer disponível para invocar!");
            return;
        }

        // Escolhe um Archer aleatório
        CardInstance invokedCard = archerCards[Random.Range(0, archerCards.Count)];

        // Encontra um tile vazio próximo para spawnar
        CardTile spawnTile = nearTile;
        if (nearTile != null && nearTile.IsOccupied())
        {
            spawnTile = board.FindAdjacentEmptyTile(nearTile, ownerPlayerNumber);
        }

        if (spawnTile != null)
        {
            CardDisplay archerDisplay = SpawnCardOnTile(invokedCard.cardData, spawnTile, ownerPlayerNumber);
            if (archerDisplay != null)
            {
                Debug.Log($"[InvokeArcher] Invocado {archerDisplay.card.cardName}!");
            }
        }
        else
        {
            Debug.Log("[InvokeArcher] Nenhum tile vazio disponível para spawnar!");
        }
    }

    // Invoca um Mago Lendário aleatório quando os 3 Magos tier-2 estão em campo
    public void InvokeRandomLegendaryMage(int ownerPlayerNumber, CardTile nearTile)
    {
        CardPool cardPool = FindObjectOfType<CardPool>();
        if (cardPool == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Procura por Magos nos tiers 3-4 (lendários)
        var allCards = cardPool.GetAvailableCards();
        var legendaryMages = allCards.FindAll(c =>
            c.cardData.cardClass == CardClass.Mago &&
            (c.cardData.tier == CardTier.Tier3 || c.cardData.tier == CardTier.Tier4));

        if (legendaryMages.Count == 0)
        {
            Debug.Log("[InvokeLegendaryMage] Nenhum Mago Lendário disponível para invocar!");
            return;
        }

        // Escolhe um Mago aleatório
        CardInstance invokedCard = legendaryMages[Random.Range(0, legendaryMages.Count)];

        // Encontra um tile vazio próximo para spawnar
        CardTile spawnTile = nearTile;
        if (nearTile != null && nearTile.IsOccupied())
        {
            spawnTile = board.FindAdjacentEmptyTile(nearTile, ownerPlayerNumber);
        }

        if (spawnTile != null)
        {
            CardDisplay mageDisplay = SpawnCardOnTile(invokedCard.cardData, spawnTile, ownerPlayerNumber);
            if (mageDisplay != null)
            {
                Debug.Log($"[InvokeLegendaryMage] Invocado {mageDisplay.card.cardName}!");
            }
        }
        else
        {
            Debug.Log("[InvokeLegendaryMage] Nenhum tile vazio disponível para spawnar!");
        }
    }
}
