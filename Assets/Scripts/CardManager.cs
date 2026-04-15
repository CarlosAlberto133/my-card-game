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

        SpawnRandomCards();
    }

    void Update()
    {
        // Pressione R para spawnar novas cartas aleatórias (para teste)
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RefreshCards();
        }
    }

    void SpawnRandomCards()
    {
        // Limpa cartas anteriores
        ClearSpawnedCards();

        if (cardPool == null)
        {
            Debug.LogError("CardPool não encontrado!");
            return;
        }

        // Calcula posição inicial para centralizar as cartas
        float totalWidth = (numberOfCards - 1) * cardSpacing;
        Vector3 startPosition = currentSpawnPosition - new Vector3(totalWidth / 2f, 0, 0);

        // Spawna cartas aleatórias
        for (int i = 0; i < numberOfCards; i++)
        {
            CardInstance randomCard = cardPool.DrawRandomCard();

            if (randomCard != null)
            {
                Vector3 position = startPosition + new Vector3(i * cardSpacing, 0, 0);
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

        Debug.Log($"Spawning novas cartas em: {currentSpawnPosition}");
        SpawnRandomCards();
        Debug.Log("========== CardManager: OnGameStart Completo ==========");
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
        spawnedCards.RemoveAll(card =>
        {
            if (card == null) return true;
            CardDisplay display = card.GetComponent<CardDisplay>();
            return display != null && (display.isInHand || display.isOnBoard);
        });
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

        return cardObject;
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
}
