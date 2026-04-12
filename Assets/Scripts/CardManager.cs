using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    [Header("Referências")]
    public GameObject cardPrefab;
    public CardPool cardPool;

    [Header("Configurações de Spawn")]
    public int numberOfCards = 5;
    public Vector3 centerPosition = new Vector3(0, 1.5f, 0);
    public float cardSpacing = 4f;
    public float cardScale = 1.5f;

    private List<GameObject> spawnedCards = new List<GameObject>();

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
        Vector3 startPosition = centerPosition - new Vector3(totalWidth / 2f, 0, 0);

        // Spawna cartas aleatórias
        for (int i = 0; i < numberOfCards; i++)
        {
            CardInstance randomCard = cardPool.DrawRandomCard();

            if (randomCard != null)
            {
                Vector3 position = startPosition + new Vector3(i * cardSpacing, 0, 0); // Mantém a altura do startPosition
                GameObject cardObject = SpawnCard(randomCard.cardData, position);
                spawnedCards.Add(cardObject);

                Debug.Log($"Spawnou: {randomCard.cardData.cardName} (ID: {randomCard.instanceId})");
            }
        }
    }

    void ClearSpawnedCards()
    {
        foreach (GameObject card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        spawnedCards.Clear();
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
