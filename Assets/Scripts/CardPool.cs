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

// Porcentagens de aparição por tier na loja. Quando um slot da loja sorteia uma
// carta candidata, rola-se 0-99 contra a chance do tier dela: passou, entra;
// falhou, sorteia outra candidata (com limite de tentativas). Assim tiers altos
// vão ficando mais comuns conforme os rounds avançam.
// IMPORTANTE (lockstep): só é chamado dentro de fluxos já semeados (loja usa
// Random.InitState com seed derivada; filas do lobby usam System.Random com
// seed fixa), então o resultado é idêntico nos dois clientes.
public static class TierOdds
{
    // Chance (%) por round de partida (índice 0 = round 1; round 10+ usa o último)
    static readonly int[][] roundOdds =
    {
        //          R1   R2   R3   R4   R5   R6   R7   R8   R9  R10+
        new int[] { 100, 100, 100,  95,  85,  75,  65,  55,  50,  45 }, // Tier 1
        new int[] {  90,  92,  95,  95,  95,  90,  85,  80,  75,  70 }, // Tier 2
        new int[] {  50,  60,  70,  80,  90,  95,  95,  95,  95,  90 }, // Tier 3
        new int[] {   0,  10,  20,  35,  50,  62,  75,  82,  88,  95 }, // Tier 4
        new int[] {   0,   5,  10,  17,  25,  31,  37,  41,  44,  47 }, // Tier 5 (metade do T4 — mais "lendário")
    };

    // Chance (%) na fase inicial de compras (loja do lobby)
    static readonly int[] lobbyOdds = { 100, 90, 50, 20, 10 };

    public static int GetChance(CardTier tier, bool lobbyPhase, int round)
    {
        int t = Mathf.Clamp((int)tier, 1, 5) - 1;
        if (lobbyPhase) return lobbyOdds[t];

        int r = Mathf.Clamp(round, 1, 10) - 1;
        return roundOdds[t][r];
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

    // Filtro do MODO TESTE. Identifica por CLASSE/TIER/STATS (não por nome —
    // as cartas ganharam nomes temáticos e o nome pode mudar de novo).
    // Mantém o conjunto que o filtro antigo por nome pegava: o Tank 0/2/5,
    // o Mago 2/3 e TODOS os Healers/Arqueiros de tier 1.
    bool IsCardWithEffect(Card card)
    {
        if (card == null) return false;

        return (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier1 &&
                card.attack == 0 && card.shield == 2 && card.health == 5) ||
               (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier1 &&
                card.attack == 2 && card.health == 3) ||
               (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier1) ||
               (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier1);
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

    // Pega uma carta aleatória disponível (uniforme, sem porcentagens de tier)
    public CardInstance DrawRandomCard()
    {
        List<CardInstance> available = GetAvailableCards();
        if (available.Count == 0) return null;

        int randomIndex = Random.Range(0, available.Count);
        CardInstance drawnCard = available[randomIndex];
        drawnCard.isInDeck = false;

        return drawnCard;
    }

    // Pega uma carta aleatória respeitando as porcentagens de tier (TierOdds).
    // Determinístico: as chamadas de Random acontecem na mesma ordem nos dois
    // clientes (fluxo da loja já re-semeado com Random.InitState)
    public CardInstance DrawRandomCard(bool lobbyPhase, int round)
    {
        List<CardInstance> available = GetAvailableCards();
        CardInstance drawn = PickWithOdds(available, lobbyPhase, round, max => Random.Range(0, max));
        if (drawn != null) drawn.isInDeck = false;
        return drawn;
    }

    // Sorteio com teste de aceitação: candidata aleatória + dado 0-99 contra a
    // chance do tier. Após esgotar as tentativas, cai no MENOR tier disponível
    // (não deixa a loja com slot vazio). nextInt abstrai a fonte de aleatório
    // (UnityEngine.Random na loja, System.Random nas filas do lobby).
    static CardInstance PickWithOdds(List<CardInstance> pool, bool lobbyPhase, int round,
                                     System.Func<int, int> nextInt)
    {
        if (pool == null || pool.Count == 0) return null;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            CardInstance candidate = pool[nextInt(pool.Count)];
            int chance = TierOdds.GetChance(candidate.cardData.tier, lobbyPhase, round);
            if (nextInt(100) < chance)
                return candidate;
        }

        // Fallback determinístico: primeira carta do menor tier ainda disponível
        CardInstance best = pool[0];
        foreach (CardInstance c in pool)
            if (c.cardData.tier < best.cardData.tier) best = c;
        return best;
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

    // ===== FILAS DA FASE INICIAL DE COMPRAS =====
    // As compras/resets do início são SIMULTÂNEOS: se os dois jogadores
    // sorteassem do pool compartilhado, a ordem de chegada dos RPCs mudaria o
    // resultado em cada cliente (desync). Solução: no começo da fase, o pool é
    // embaralhado com a seed e dividido em DUAS filas disjuntas — cada jogador
    // saca apenas da sua, imune à ordem das ações do outro.
    private List<CardInstance> lobbyQueueP1;
    private List<CardInstance> lobbyQueueP2;

    public void EnsureLobbyQueues(int seed)
    {
        if (lobbyQueueP1 != null) return; // Já montadas nesta fase

        List<CardInstance> available = GetAvailableCards();

        // Monta a ordem das filas com as porcentagens de tier do lobby (TierOdds):
        // sorteia carta a carta com teste de aceitação, então o COMEÇO das filas
        // (o que os jogadores realmente veem) segue as chances — tiers altos
        // afundam para o fim. System.Random próprio (não mexe no UnityEngine.Random,
        // que o lockstep re-semeia para os efeitos).
        // As cartas são distribuídas ALTERNADAMENTE entre P1 e P2, para as duas
        // filas terem o mesmo perfil de raridade (partir ao meio concentraria os
        // tiers altos na fila de um jogador só).
        System.Random rng = new System.Random(seed);
        lobbyQueueP1 = new List<CardInstance>();
        lobbyQueueP2 = new List<CardInstance>();

        bool toP1 = true;
        while (available.Count > 0)
        {
            CardInstance picked = PickWithOdds(available, true, 1, max => rng.Next(max));
            available.Remove(picked);
            (toP1 ? lobbyQueueP1 : lobbyQueueP2).Add(picked);
            toP1 = !toP1;
        }

        Debug.Log($"[CardPool] Filas da fase inicial: P1={lobbyQueueP1.Count}, P2={lobbyQueueP2.Count} cartas");
    }

    public CardInstance DrawFromLobbyQueue(int playerNumber)
    {
        List<CardInstance> queue = playerNumber == 2 ? lobbyQueueP2 : lobbyQueueP1;
        if (queue == null || queue.Count == 0) return null;

        CardInstance drawn = queue[0];
        queue.RemoveAt(0);
        drawn.isInDeck = false;
        return drawn;
    }

    // Descarta as filas quando a partida começa (volta a sortear do pool normal)
    public void ClearLobbyQueues()
    {
        lobbyQueueP1 = null;
        lobbyQueueP2 = null;
    }

    // Reinício de partida: devolve TODAS as instâncias ao deck e descarta as
    // filas da fase inicial. Sem isso, as cartas que foram para mão/tabuleiro
    // continuam marcadas como usadas e o pool nasce esvaziado na nova partida.
    public void ResetPool()
    {
        foreach (CardInstance c in cardPool)
        {
            c.isOnBoard = false;
            c.isInDeck = true;
        }
        ClearLobbyQueues();
        Debug.Log($"[CardPool] Pool resetado para reinício: {cardPool.Count} cartas de volta ao deck");
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
