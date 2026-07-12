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
    public const int LobbyShopSize = 10; // Fase inicial de compras: 10 cartas
    public Vector3 centerPosition = new Vector3(0, 1.5f, 0);
    public Vector3 shopPosition = new Vector3(-35.4f, 1.5f, 0); // Posição ao lado do tabuleiro
    public float cardSpacing = 4f;
    public float cardScale = 1.5f;

    // Lojas: em multiplayer cada jogador tem a SUA (índices 1 e 2); no modo offline
    // todos usam a loja compartilhada (índice 0). Os DOIS clientes geram as DUAS
    // lojas (determinístico via seed), mas cada um só mostra a própria — a do
    // oponente existe desativada, para a compra dele executar aqui também.
    private List<GameObject>[] shops = new List<GameObject>[]
    {
        new List<GameObject>(), new List<GameObject>(), new List<GameObject>()
    };
    private int[] shopSpawnCounts = new int[3]; // Spawns por loja (deriva seed única por spawn)
    private Vector3 currentSpawnPosition;
    private bool verticalLayout = false; // Se true, cartas ficam uma abaixo da outra

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
        // X forçado por código (a cena tem -42 serializado, do tabuleiro 12x12):
        // tabuleiro 10x10 tem borda em -32.7, loja fica com a mesma folga de antes
        shopPosition = new Vector3(-35.4f, shopY, 0f);

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
            // Em multiplayer NÃO: um refresh local sem RPC dessincronizaria as lojas
            if (PhotonNetwork.inRoom) return;
            RefreshCards();
        }
    }

    // Em multiplayer cada jogador tem loja própria; offline todos usam a loja 0
    bool UsePerPlayerShops()
    {
        return PhotonNetwork.inRoom;
    }

    // Número da loja que ESTE cliente vê (0 = compartilhada no offline)
    int LocalShopNumber()
    {
        if (UsePerPlayerShops() && PhotonGameManager.Instance != null)
            return PhotonGameManager.Instance.myPlayerNumber;
        return 0;
    }

    List<GameObject> GetShopList(int shopNumber)
    {
        if (shopNumber < 0 || shopNumber > 2) return shops[0];
        return shops[shopNumber];
    }

    public void SpawnRandomCards()
    {
        Debug.Log("[CardManager] SpawnRandomCards() chamado");

        if (UsePerPlayerShops())
        {
            // Ordem FIXA (P1 depois P2): os dois clientes consomem o pool de cartas
            // na mesma sequência, então as duas lojas saem idênticas nos dois lados
            SpawnShopForPlayer(1);
            SpawnShopForPlayer(2);
        }
        else
        {
            SpawnShopForPlayer(0);
        }
    }

    // Gera a loja de um jogador (0 = loja compartilhada do modo offline)
    public void SpawnShopForPlayer(int shopNumber)
    {
        // Sincroniza seed com PhotonGameManager para gerar mesmas cartas em ambos
        if (PhotonGameManager.Instance != null)
        {
            int baseSeed = PhotonGameManager.Instance.currentGameSeed;

            if (baseSeed > 0)
            {
                // Seed única por spawn E por loja. O 104729 (primo, não múltiplo de
                // 7919) separa a sequência do P2 da do P1 sem risco de colisão.
                int derivedSeed = baseSeed + shopSpawnCounts[shopNumber] * 7919
                                + (shopNumber == 2 ? 104729 : 0);
                UnityEngine.Random.InitState(derivedSeed);
                shopSpawnCounts[shopNumber]++;
                Debug.Log($"[CardManager] Random.InitState({derivedSeed}) (loja {shopNumber}, spawn #{shopSpawnCounts[shopNumber]})");
            }
            else
            {
                Debug.LogWarning("[CardManager] Seed ainda é 0! Usando Random padrão.");
            }
        }

        List<GameObject> shop = GetShopList(shopNumber);

        // Limpa cartas anteriores desta loja
        ClearShopCards(shop);

        if (cardPool == null)
        {
            cardPool = FindObjectOfType<CardPool>();
        }

        if (cardPool == null)
        {
            Debug.LogError("CardPool não encontrado!");
            return;
        }

        // Este cliente só MOSTRA a própria loja; a do oponente fica desativada
        // (invisível e inclicável), mas pronta para a compra dele executar aqui
        bool hiddenShop = UsePerPlayerShops() && shopNumber != LocalShopNumber();

        // Fase inicial de compras: 10 cartas, sorteadas de uma FILA por jogador.
        // As compras/resets do início são simultâneos — sortear do pool
        // compartilhado dessincronizaria (a ordem dos RPCs muda o resultado)
        bool lobbyPhase = TurnManager.Instance != null &&
                          TurnManager.Instance.gameState == GameState.Lobby;
        bool useLobbyQueues = lobbyPhase && UsePerPlayerShops();
        int cardsToSpawn = lobbyPhase ? LobbyShopSize : numberOfCards;

        if (useLobbyQueues && PhotonGameManager.Instance != null &&
            PhotonGameManager.Instance.currentGameSeed > 0)
        {
            cardPool.EnsureLobbyQueues(PhotonGameManager.Instance.currentGameSeed * 31 + 17);
        }

        // Spawna cartas aleatórias
        for (int i = 0; i < cardsToSpawn; i++)
        {
            CardInstance randomCard = useLobbyQueues
                ? cardPool.DrawFromLobbyQueue(shopNumber)
                : cardPool.DrawRandomCard();

            if (randomCard != null)
            {
                Vector3 position;

                if (verticalLayout)
                {
                    // Layout vertical: cartas uma abaixo da outra (eixo Z)
                    float totalDepth = (cardsToSpawn - 1) * cardSpacing;
                    Vector3 startPosition = currentSpawnPosition - new Vector3(0, 0, totalDepth / 2f);
                    position = startPosition + new Vector3(0, 0, i * cardSpacing);
                }
                else if (lobbyPhase)
                {
                    // Fase inicial: 2 fileiras de 5 (10 numa fileira só ficava gigante).
                    // Primeiras 5 na fileira da frente, últimas 5 na de trás
                    int perRow = Mathf.CeilToInt(cardsToSpawn / 2f);
                    int rowIndex = i / perRow;
                    int colIndex = i % perRow;
                    float rowGap = 10f; // Separação em Z entre as fileiras

                    float totalWidth = (perRow - 1) * cardSpacing;
                    Vector3 startPosition = currentSpawnPosition - new Vector3(totalWidth / 2f, 0, 0);
                    float rowZ = rowIndex == 0 ? -rowGap / 2f : rowGap / 2f;
                    position = startPosition + new Vector3(colIndex * cardSpacing, 0, rowZ);
                }
                else
                {
                    // Layout horizontal: cartas lado a lado (eixo X)
                    float totalWidth = (cardsToSpawn - 1) * cardSpacing;
                    Vector3 startPosition = currentSpawnPosition - new Vector3(totalWidth / 2f, 0, 0);
                    position = startPosition + new Vector3(i * cardSpacing, 0, 0);
                }

                GameObject cardObject = SpawnCard(randomCard.cardData, position);
                if (cardObject == null) continue;
                if (hiddenShop) cardObject.SetActive(false);
                shop.Add(cardObject);

                Debug.Log($"Spawnou (loja {shopNumber}): {randomCard.cardData.cardName} (ID: {randomCard.instanceId})");
            }
        }
    }

    // Quantidade de cartas na loja LOCAL (a fase inicial tem 10; a partida, 5)
    public int GetLocalShopCount()
    {
        return GetShopList(LocalShopNumber()).Count;
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

    // Retorna o índice de uma carta na loja LOCAL (-1 se não está na loja).
    // Só cartas da própria loja são clicáveis, então buscar na local basta.
    public int GetShopCardIndex(GameObject cardObject)
    {
        return GetShopList(LocalShopNumber()).IndexOf(cardObject);
    }

    // Retorna a carta da loja LOCAL em um índice específico (overlay, cliques)
    public GameObject GetShopCard(int index)
    {
        return GetShopCard(index, LocalShopNumber());
    }

    // Retorna a carta da loja de um jogador específico (o RPC de compra usa a
    // loja do COMPRADOR, que neste cliente pode ser a loja oculta do oponente)
    public GameObject GetShopCard(int index, int shopNumber)
    {
        if (!UsePerPlayerShops()) shopNumber = 0;
        List<GameObject> shop = GetShopList(shopNumber);
        if (index < 0 || index >= shop.Count) return null;
        return shop[index];
    }

    // Refresh de TODAS as lojas (virada de round)
    public void RefreshShop()
    {
        Debug.Log("CardManager: Refresh das lojas com novas cartas!");
        SpawnRandomCards();
    }

    // Reset pago: rerola SÓ a loja de quem pagou (chega aos dois clientes via RPC)
    public void RefreshShopForPlayer(int playerNumber)
    {
        SpawnShopForPlayer(UsePerPlayerShops() ? playerNumber : 0);
    }

    void ClearShopCards(List<GameObject> shop)
    {
        int destroyed = 0;
        int preserved = 0;

        foreach (GameObject card in shop)
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

                // Devolve a cópia não comprada ao pool — com duas lojas, o consumo
                // dobrou e sem isso o pool esvaziava no fim da partida
                if (cardPool != null && cardDisplay != null && cardDisplay.card != null)
                {
                    cardPool.ReturnCardCopyToDeck(cardDisplay.card);
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
        shop.Clear();
    }

    public void DestroyAllCards()
    {
        foreach (List<GameObject> shop in shops)
        {
            foreach (GameObject card in shop)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            shop.Clear();
        }

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
