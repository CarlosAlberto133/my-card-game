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

    [Header("TESTE")]
    [Tooltip("Se ativado, mostra APENAS as 4 cartas tier-1 (Archer, Healer, Mage, Tank) para testes")]
    public bool testModeOnly = false;

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
        int skipped = 0;

        foreach (Card card in allBaseCards)
        {
            // Ignora slots vazios e cartas ainda por preencher (template sem tier válido ou sem nome).
            // Assim, cartas por acabar não aparecem em branco na loja.
            if (card == null || (int)card.tier < 1 || (int)card.tier > 5 || string.IsNullOrWhiteSpace(card.cardName))
            {
                skipped++;
                continue;
            }

            // Se testModeOnly está ativo, mostra apenas as 4 cartas com efeito implementado
            if (testModeOnly)
            {
                bool hasEffect = IsCardWithEffect(card);
                if (!hasEffect)
                {
                    skipped++;
                    continue;
                }
            }

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

        string modeText = testModeOnly ? " [MODO TESTE - APENAS CARTAS COM EFEITO]" : "";
        Debug.Log($"Card Pool inicializado com {cardPool.Count} cartas totais{modeText}");
        Debug.Log($"- Cartas base atribuídas: {allBaseCards.Count} (ignoradas por preencher: {skipped})");
        Debug.Log($"- Tier 1-4: {copiesPerCard} cópias cada");
        Debug.Log($"- Tier 5: {tier5Copies} cópia cada");
    }

    bool IsCardWithEffect(Card card)
    {
        if (card == null) return false;

        return card.cardName == "Tank 1*" ||
               card.cardName == "Mage 1*" ||
               card.cardName == "Healer 1*" ||
               card.cardName == "Archer 1*";
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

    // Devolve UMA cópia desta carta ao deck (loja rerolada com carta não comprada).
    // As cópias são intercambiáveis: reativa a primeira instância fora do deck.
    // Determinístico: a ordem do pool é a mesma nos dois clientes.
    public void ReturnCardCopyToDeck(Card data)
    {
        CardInstance inst = cardPool.Find(c => c.cardData == data && !c.isInDeck && !c.isOnBoard);
        if (inst != null) inst.isInDeck = true;
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
