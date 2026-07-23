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
    public Vector3 shopPosition = new Vector3(-25.5f, 1.5f, 0); // Posição ao lado do tabuleiro (7x7)
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

    // Travar cartas: cada carta da loja pode ser travada individualmente
    // (CardDisplay.isLockedInShop, via checkbox "Travar cartas" + clique).
    // No refresh, as travadas ficam no MESMO slot e só os outros renovam.
    // SINCRONIZADO via RPC_SetShopCardLock: os 2 clientes pulam os MESMOS
    // slots, então o consumo do pool e a seed continuam idênticos (lockstep).
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
        // cartas maiores (pedido do Carlos) e espaçamento que evita sobreposição
        cardScale = 3.4f;
        cardSpacing = 9f;

        // Altura correta para a nova escala (a base da carta não pode afundar no
        // chão) — cresce só para cima porque a base fica fixa em GroundY
        float shopY = CardDisplay.GroundY(cardScale);
        centerPosition = new Vector3(centerPosition.x, shopY, centerPosition.z);
        // X forçado por código (a cena tem -42 serializado, do tabuleiro 12x12):
        // tabuleiro 7x7 tem borda em -22.8. Cartas maiores → loja um pouco mais
        // afastada para a carta (mais larga) não invadir o tile do tabuleiro
        shopPosition = new Vector3(-27.5f, shopY, 0f);

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
            // na mesma sequência, então as duas lojas saem idênticas nos dois lados.
            // Cartas travadas de cada loja são preservadas dentro do spawn.
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

        // Cartas TRAVADAS pelo dono (checkbox "Travar cartas"): sobrevivem ao
        // refresh no MESMO slot; só os outros slots renovam. Coletadas ANTES da
        // limpeza (a limpeza pula elas, mas esvazia a lista)
        Dictionary<int, GameObject> lockedBySlot = null;
        for (int i = 0; i < shop.Count; i++)
        {
            GameObject go = shop[i];
            if (go == null) continue;
            CardDisplay lockedCd = go.GetComponent<CardDisplay>();
            if (lockedCd != null && lockedCd.isInShop && lockedCd.isLockedInShop)
            {
                if (lockedBySlot == null) lockedBySlot = new Dictionary<int, GameObject>();
                lockedBySlot[i] = go;
            }
        }

        // Limpa cartas anteriores desta loja (preserva as travadas)
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

        // Cartas que o dono desta loja JÁ TEM (mão ou campo): não são oferecidas
        // de novo até serem destruídas. Determinístico: os dois clientes veem as
        // mesmas mãos/tabuleiro no momento do refresh (compra/colocação/morte
        // chegam por RPC aos dois lados)
        System.Collections.Generic.HashSet<string> ownedNames =
            (UsePerPlayerShops() && shopNumber != 0) ? CollectOwnedCardNames(shopNumber) : null;

        // Cartas travadas também bloqueiam cópias nos novos sorteios (não faz
        // sentido a loja oferecer a mesma carta ao lado da travada)
        if (lockedBySlot != null)
        {
            foreach (var kv in lockedBySlot)
            {
                CardDisplay lockedCd = kv.Value != null ? kv.Value.GetComponent<CardDisplay>() : null;
                if (lockedCd == null || lockedCd.card == null) continue;
                if (ownedNames == null) ownedNames = new System.Collections.Generic.HashSet<string>();
                ownedNames.Add(lockedCd.card.cardName);
            }
        }

        // Spawna cartas aleatórias (slots travados mantêm a carta que já estava)
        for (int i = 0; i < cardsToSpawn; i++)
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

            // Slot travado: mantém a carta que já estava, sem consumir sorteio.
            // Os 2 clientes têm as MESMAS flags (RPC), então tiram o MESMO número
            // de cartas do pool — a sequência do Random continua idêntica
            GameObject kept;
            if (lockedBySlot != null && lockedBySlot.TryGetValue(i, out kept) && kept != null)
            {
                kept.transform.position = position;
                shop.Add(kept);
                CardDisplay keptCd = kept.GetComponent<CardDisplay>();
                Debug.Log($"Slot {i} (loja {shopNumber}): TRAVADO, manteve {(keptCd != null && keptCd.card != null ? keptCd.card.cardName : "?")}");
                continue;
            }

            // Sorteio com porcentagens de tier (TierOdds): no lobby a fila já foi
            // montada com as chances; na partida a chance evolui com o round
            int currentRound = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 1;
            CardInstance randomCard = useLobbyQueues
                ? cardPool.DrawFromLobbyQueue(shopNumber, ownedNames)
                : cardPool.DrawRandomCard(lobbyPhase, currentRound, ownedNames);

            if (randomCard != null)
            {
                GameObject cardObject = SpawnCard(randomCard.cardData, position);
                if (cardObject == null) continue;
                if (hiddenShop) cardObject.SetActive(false);
                shop.Add(cardObject);

                Debug.Log($"Spawnou (loja {shopNumber}): {randomCard.cardData.cardName} (ID: {randomCard.instanceId})");
            }
        }

        // Segurança: travada num slot que não existe mais (ex.: transição da loja
        // de 10 do lobby para a de 5 da partida) volta ao pool e é destruída
        if (lockedBySlot != null)
        {
            foreach (var kv in lockedBySlot)
            {
                if (kv.Key < cardsToSpawn || kv.Value == null) continue;
                CardDisplay lostCd = kv.Value.GetComponent<CardDisplay>();
                if (cardPool != null && lostCd != null && lostCd.card != null)
                    cardPool.ReturnCardCopyToDeck(lostCd.card);
                Destroy(kv.Value);
            }
        }
    }

    // Nomes das cartas que o jogador tem na MÃO ou em CAMPO (para a loja dele
    // não oferecer cópia repetida). Nulo quando não há nenhuma — o filtro nem
    // roda. Nome basta como identidade: desde os nomes temáticos, cada carta
    // tem nome único (cópias/ecos em campo também bloqueiam, de propósito).
    System.Collections.Generic.HashSet<string> CollectOwnedCardNames(int playerNumber)
    {
        System.Collections.Generic.HashSet<string> owned = null;

        foreach (CardDisplay cd in FindObjectsOfType<CardDisplay>())
        {
            if (cd == null || cd.card == null) continue;
            if (cd.ownerPlayerNumber != playerNumber) continue;
            if (!cd.isInHand && !cd.isOnBoard) continue;

            if (owned == null) owned = new System.Collections.Generic.HashSet<string>();
            owned.Add(cd.card.cardName);
        }
        return owned;
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

                // Carta TRAVADA continua na loja: não destrói nem devolve ao pool
                // (o SpawnShopForPlayer recoloca ela no mesmo slot)
                if (cardDisplay != null && cardDisplay.isInShop && cardDisplay.isLockedInShop)
                {
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

    // Reinício de partida: destrói as cartas da loja, zera os contadores de spawn
    // (para as seeds derivadas baterem com um jogo novo) e volta a loja para o
    // layout/posição da fase inicial (centro, horizontal).
    public void ResetForRestart()
    {
        DestroyAllCards();
        shopSpawnCounts = new int[3];
        currentSpawnPosition = centerPosition;
        verticalLayout = false;
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
            // CRÍTICO: a carta nasce do prefab com isInShop=true (padrão). Sem
            // zerar isso, o OnMouseDown a trata como carta de loja e o clique
            // NUNCA entra na lógica de "atacar carta inimiga" — cópias e cartas
            // invocadas ficavam impossíveis de atacar
            cardDisplay.isInShop = false;
            cardDisplay.isInHand = false;
            cardDisplay.ownerPlayerNumber = ownerPlayerNumber;
            // SpawnCardOnTile SÓ é usado por efeitos (cópias, ecos, invocações)
            // — cartas jogadas da mão seguem outro caminho. A marca exclui
            // estas unidades da contagem da Devoção de Classe
            cardDisplay.isEffectSpawn = true;
            cardDisplay.UpdateDisplay(); // Atualiza a borda com a cor do dono

            // Aplica o efeito de entrada em campo
            cardDisplay.ApplyCardEffect();
        }

        return cardDisplay;
    }

    // Invoca um Archer aleatório (tier 1-4) quando um Archer 2 (ATK 3, HP 2) mata inimigo
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

    // ═══════════ LENDÁRIOS DAS TRÍADES (v4.3) ═══════════
    // Cartas EXCLUSIVAS: não existem no pool nem na loja — só nascem quando a
    // tríade tier-2 da classe fecha. Criadas em runtime (ScriptableObject) de
    // propósito: nenhum sorteio/refresh as conhece. Arte emprestada de um T5
    // da classe (allBaseCards é o catálogo — nunca esvazia como o pool).
    // Stats únicos dentro de classe+tier (regra do dispatch por stats):
    //   Arcanor  Mago   T5 6/0/7 (T5 existentes: 5/5, 4/6, 5/6)
    //   Serafina Healer T5 3/0/8 (T5 existentes: 3/6, 2/7, 3/7)
    static Card arcanorCard;
    static Card serafinaCard;

    Card GetTriadLegendaryCard(CardClass cls)
    {
        if (cls == CardClass.Mago && arcanorCard != null) return arcanorCard;
        if (cls == CardClass.Healer && serafinaCard != null) return serafinaCard;

        Card c = ScriptableObject.CreateInstance<Card>();
        if (cls == CardClass.Mago)
        {
            c.cardName = "Arcanor, o Primordial";
            c.cardClass = CardClass.Mago;
            c.tier = CardTier.Tier5;
            c.attack = 6; c.shield = 0; c.health = 7;
            c.effectDescription = "Lendário da tríade. Ao entrar: Cataclisma — 1 de dano em TODOS os inimigos. Todo round, dispara um raio de 1 de dano num inimigo à sua escolha";
            arcanorCard = c;
        }
        else
        {
            c.cardName = "Serafina, a Eterna";
            c.cardClass = CardClass.Healer;
            c.tier = CardTier.Tier5;
            c.attack = 3; c.shield = 0; c.health = 8;
            c.effectDescription = "Lendária da tríade. Ao entrar: cura 1 em todos os aliados e concede 5 de ouro. Todo round, cura 1 em todos os aliados";
            serafinaCard = c;
        }
        c.artwork = BorrowTier5Artwork(cls);
        return c;
    }

    Sprite BorrowTier5Artwork(CardClass cls)
    {
        CardPool cardPool = FindObjectOfType<CardPool>();
        if (cardPool == null) return null;
        foreach (Card b in cardPool.allBaseCards)
            if (b != null && b.cardClass == cls && b.tier == CardTier.Tier5 && b.artwork != null)
                return b.artwork;
        return null;
    }

    // Invoca o lendário exclusivo da tríade num tile vazio perto da carta que
    // fechou o combo. Determinístico: carta fixa (sem sorteio) +
    // FindAdjacentEmptyTile (varredura em ordem fixa) — roda no fluxo do RPC
    // de colocar a 3ª carta, idêntico nos dois clientes.
    public void InvokeTriadLegendary(CardClass cls, int ownerPlayerNumber, CardTile nearTile)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        CardTile spawnTile = nearTile;
        if (nearTile != null && nearTile.IsOccupied())
        {
            spawnTile = board.FindAdjacentEmptyTile(nearTile, ownerPlayerNumber);
        }

        if (spawnTile == null)
        {
            Debug.Log("[TriadLegendary] Nenhum tile vazio disponível para spawnar!");
            return;
        }

        Card legend = GetTriadLegendaryCard(cls);
        CardDisplay display = SpawnCardOnTile(legend, spawnTile, ownerPlayerNumber);
        if (display != null)
        {
            FloatingTextFX.ShowAboveCard(display,
                cls == CardClass.Mago ? "ARCANOR DESPERTA!" : "SERAFINA DESPERTA!",
                FloatingTextFX.EffectColor, 4.6f);
            Debug.Log($"[TriadLegendary] Invocado {legend.cardName}!");
        }
    }
}
