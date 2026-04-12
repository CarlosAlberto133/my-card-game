using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CardInstance
{
    public Card cardData;
    public int instanceId;
    public bool isInDeck;
    public bool isOnBoard;

    public CardInstance(Card data, int id)
    {
        cardData = data;
        instanceId = id;
        isInDeck = true;
        isOnBoard = false;
    }
}

public class CardPool : MonoBehaviour
{
    [Header("Todas as Cartas Base")]
    public List<Card> allBaseCards = new List<Card>();

    [Header("Sistema de Cópias")]
    [Tooltip("Número de cópias para cartas tier 1-4")]
    public int copiesPerCard = 3;

    [Tooltip("Número de cópias para cartas tier 5 (únicas)")]
    public int tier5Copies = 1;

    // Pool de todas as instâncias de cartas no jogo
    private List<CardInstance> cardPool = new List<CardInstance>();

    void Awake()
    {
        InitializeCardPool();
    }

    void InitializeCardPool()
    {
        cardPool.Clear();
        int instanceIdCounter = 0;

        foreach (Card card in allBaseCards)
        {
            // Define quantas cópias criar baseado no tier
            int copies = (card.tier == CardTier.Tier5) ? tier5Copies : copiesPerCard;

            // Cria as cópias
            for (int i = 0; i < copies; i++)
            {
                CardInstance instance = new CardInstance(card, instanceIdCounter);
                cardPool.Add(instance);
                instanceIdCounter++;
            }
        }

        Debug.Log($"Card Pool inicializado com {cardPool.Count} cartas totais");
        Debug.Log($"- Cartas base: {allBaseCards.Count}");
        Debug.Log($"- Tier 1-4: {copiesPerCard} cópias cada");
        Debug.Log($"- Tier 5: {tier5Copies} cópia cada");
    }

    // Retorna todas as cartas disponíveis (não estão no board ou em uso)
    public List<CardInstance> GetAvailableCards()
    {
        return cardPool.Where(c => !c.isOnBoard && c.isInDeck).ToList();
    }

    // Retorna cartas disponíveis de uma classe específica
    public List<CardInstance> GetAvailableCardsByClass(CardClass cardClass)
    {
        return cardPool.Where(c => !c.isOnBoard && c.isInDeck && c.cardData.cardClass == cardClass).ToList();
    }

    // Retorna cartas disponíveis de um tier específico
    public List<CardInstance> GetAvailableCardsByTier(CardTier tier)
    {
        return cardPool.Where(c => !c.isOnBoard && c.isInDeck && c.cardData.tier == tier).ToList();
    }

    // Retorna uma carta específica por ID
    public CardInstance GetCardByInstanceId(int instanceId)
    {
        return cardPool.Find(c => c.instanceId == instanceId);
    }

    // Pega uma carta aleatória disponível
    public CardInstance DrawRandomCard()
    {
        List<CardInstance> available = GetAvailableCards();
        if (available.Count == 0) return null;

        int randomIndex = Random.Range(0, available.Count);
        CardInstance drawnCard = available[randomIndex];
        drawnCard.isInDeck = false;

        return drawnCard;
    }

    // Retorna uma carta ao deck
    public void ReturnCardToDeck(CardInstance card)
    {
        card.isInDeck = true;
        card.isOnBoard = false;
    }

    // Coloca uma carta no tabuleiro
    public void PlaceCardOnBoard(CardInstance card)
    {
        card.isOnBoard = true;
        card.isInDeck = false;
    }

    // Remove uma carta do tabuleiro
    public void RemoveCardFromBoard(CardInstance card)
    {
        card.isOnBoard = false;
    }

    // Estatísticas do pool
    public void PrintPoolStats()
    {
        Debug.Log("=== CARD POOL STATS ===");
        Debug.Log($"Total de cartas: {cardPool.Count}");
        Debug.Log($"Cartas disponíveis: {GetAvailableCards().Count}");
        Debug.Log($"Cartas no tabuleiro: {cardPool.Count(c => c.isOnBoard)}");

        foreach (CardClass cardClass in System.Enum.GetValues(typeof(CardClass)))
        {
            int classCount = cardPool.Count(c => c.cardData.cardClass == cardClass);
            Debug.Log($"- {cardClass}: {classCount} cartas");
        }
    }
}
