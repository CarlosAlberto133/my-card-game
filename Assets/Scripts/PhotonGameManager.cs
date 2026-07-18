using ExitGames.Client.Photon;
using Photon;
using UnityEngine;

public class PhotonGameManager : UnityEngine.MonoBehaviour
{
    public static PhotonGameManager Instance { get; private set; }

    // Informações do jogo
    public int myPlayerNumber = 0; // 1 ou 2
    public int opponentPlayerNumber = 0;
    public bool isMyTurn = false;
    public int currentGameSeed = 0; // Seed sincronizado para geração aleatória

    // Referências
    private TurnManager turnManager;

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
    }

    void Start()
    {
        turnManager = TurnManager.Instance;

        // Modo Treino: cria o controlador do bot (joga como P2 pelos mesmos RPCs)
        if (BotMode.Enabled && BotController.Instance == null)
        {
            gameObject.AddComponent<BotController>();
            Debug.Log("[PhotonGame] Modo Treino: BotController criado (bot = Jogador 2)");
        }

        // Debug: verificar conexão Photon
        DebugPhotonStatus();

        // Sincroniza quem é P1 e P2
        SyncPlayers();

        // Sincroniza seed de geração aleatória
        SyncGameSeed();
    }

    void DebugPhotonStatus()
    {
        Debug.Log("========== PHOTON STATUS ==========");
        Debug.Log($"[Photon] Conectado: {PhotonNetwork.connected}");
        Debug.Log($"[Photon] Em sala: {PhotonNetwork.inRoom}");

        if (PhotonNetwork.inRoom)
        {
            Debug.Log($"[Photon] Nome da sala: {PhotonNetwork.room.name}");
            Debug.Log($"[Photon] Players na sala: {PhotonNetwork.room.playerCount}");

            foreach (var player in PhotonNetwork.playerList)
            {
                Debug.Log($"  - Player {player.ID}: {player.name} (Master: {player.isMasterClient})");
            }
        }
        else
        {
            Debug.LogWarning("[Photon] Não há sala ativa!");
        }
        Debug.Log("==================================");
    }

    void SyncPlayers()
    {
        // Jogador que criou a sala é P1
        // Jogador que entrou é P2
        if (PhotonNetwork.isMasterClient)
        {
            myPlayerNumber = 1;
            opponentPlayerNumber = 2;
            Debug.Log("[PhotonGame] Eu sou o PLAYER 1 (criador da sala)");
        }
        else
        {
            myPlayerNumber = 2;
            opponentPlayerNumber = 1;
            Debug.Log("[PhotonGame] Eu sou o PLAYER 2 (entrei na sala)");
        }

        // Notifica o TurnManager sobre os jogadores
        if (turnManager != null)
        {
            turnManager.SetPlayers(myPlayerNumber, opponentPlayerNumber);
        }

        Debug.Log($"[PhotonGame] Sincronizado: Eu={myPlayerNumber}, Oponente={opponentPlayerNumber}");
    }

    // Contador de ações sincronizadas (para reseed determinístico dos efeitos aleatórios)
    private int syncedActionCount = 0;

    // Reseeda o Random de forma determinística antes de cada ação sincronizada.
    // Como ambos os clientes executam as mesmas ações na mesma ordem, os efeitos
    // aleatórios (congelamento, etc.) dão o mesmo resultado nos dois lados.
    void ReseedForAction()
    {
        if (currentGameSeed > 0)
        {
            syncedActionCount++;
            UnityEngine.Random.InitState(currentGameSeed + syncedActionCount * 31);
        }
    }

    // ========== ATAQUE (identificado pelo tile do atacante) ==========

    public void SendAttackRPC(int row, int column)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_Attack", PhotonTargets.All, row, column);
            Debug.Log($"[PhotonGame] Enviado RPC: Ataque da carta em ({row}, {column})");
        }
    }

    [PunRPC]
    public void RPC_Attack(int row, int column)
    {
        Debug.Log($"[PhotonGame] RPC_Attack: carta em ({row}, {column})");

        ReseedForAction();

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        CardTile tile = board.GetTile(row, column);
        if (tile == null || tile.occupiedCard == null)
        {
            Debug.LogError($"[PhotonGame] Nenhuma carta no tile ({row}, {column})!");
            return;
        }

        CardDisplay attacker = tile.occupiedCard.GetComponent<CardDisplay>();
        if (attacker != null)
        {
            // Executa o ataque completo nos dois clientes (mesmo estado = mesmo resultado)
            bool result = attacker.AttackAdjacentEnemy();
            Debug.Log($"[PhotonGame] RPC_Attack executado: {attacker.card.cardName} atacou? {result}");
        }
        else
        {
            Debug.LogError($"[PhotonGame] Objeto no tile ({row},{column}) não tem CardDisplay!");
        }
    }

    // ========== MOVIMENTO (identificado por tiles origem/destino) ==========

    public void SendMoveCardRPC(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_MoveCard", PhotonTargets.All, fromRow, fromCol, toRow, toCol);
            Debug.Log($"[PhotonGame] Enviado RPC: Movimento ({fromRow},{fromCol}) -> ({toRow},{toCol})");
        }
    }

    [PunRPC]
    public void RPC_MoveCard(int fromRow, int fromCol, int toRow, int toCol)
    {
        Debug.Log($"[PhotonGame] RPC_MoveCard: ({fromRow},{fromCol}) -> ({toRow},{toCol})");

        ReseedForAction();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecuteMoveCard(fromRow, fromCol, toRow, toCol);
        }
    }

    // ========== COLOCAR CARTA EM CAMPO (índice da mão + tile destino) ==========

    public void SendPlaceCardRPC(int handIndex, int ownerPlayerNumber, int row, int column)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_PlaceCard", PhotonTargets.All, handIndex, ownerPlayerNumber, row, column);
            Debug.Log($"[PhotonGame] Enviado RPC: P{ownerPlayerNumber} colocou carta {handIndex} da mão em ({row}, {column})");
        }
    }

    [PunRPC]
    public void RPC_PlaceCard(int handIndex, int ownerPlayerNumber, int row, int column)
    {
        Debug.Log($"[PhotonGame] RPC_PlaceCard: mão[{handIndex}] de P{ownerPlayerNumber} -> ({row}, {column})");

        ReseedForAction();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecutePlaceCard(handIndex, ownerPlayerNumber, row, column);
        }
    }

    // Executa a compra nos DOIS clientes (identifica a carta pelo índice na loja)
    [PunRPC]
    public void RPC_BuyCard(int shopIndex, int buyerPlayerNumber)
    {
        Debug.Log($"[PhotonGame] RPC_BuyCard: slot {shopIndex} comprado por P{buyerPlayerNumber}");

        if (CardManager.Instance == null)
        {
            Debug.LogError("[PhotonGame] CardManager não encontrado!");
            return;
        }

        // Cada jogador tem a própria loja: busca no shop do COMPRADOR (neste
        // cliente pode ser a loja oculta do oponente)
        GameObject cardObject = CardManager.Instance.GetShopCard(shopIndex, buyerPlayerNumber);
        if (cardObject == null)
        {
            Debug.LogError($"[PhotonGame] Carta no slot {shopIndex} da loja do P{buyerPlayerNumber} não encontrada!");
            return;
        }

        CardDisplay cardDisplay = cardObject.GetComponent<CardDisplay>();
        if (cardDisplay != null)
        {
            cardDisplay.ExecuteBuy(buyerPlayerNumber);
        }
    }

    // Método para chamar RPC de compra (PhotonTargets.All executa nos dois clientes, incluindo este)
    public void SendBuyCardRPC(int shopIndex, int buyerPlayerNumber)
    {
        if (!PhotonNetwork.connected)
        {
            Debug.LogWarning("[PhotonGame] Não conectado ao Photon! RPC não enviado.");
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_BuyCard", PhotonTargets.All, shopIndex, buyerPlayerNumber);
            Debug.Log($"[PhotonGame] Enviado RPC: Compra slot {shopIndex} [P{buyerPlayerNumber}]");
        }
    }

    // ========== DECISÕES DE EFEITO (popups sincronizados) ==========

    // Decisões pendentes: registradas NOS DOIS clientes na mesma ordem (dentro de ações RPC),
    // então o ID incremental é idêntico nos dois lados. Cada cliente guarda seu próprio callback.
    private System.Collections.Generic.Dictionary<int, System.Action<bool>> pendingDecisions =
        new System.Collections.Generic.Dictionary<int, System.Action<bool>>();
    private int nextDecisionId = 0;

    // Há decisão de efeito ainda não resolvida (deste lado ou do oponente)?
    public bool HasPendingDecisions()
    {
        return pendingDecisions.Count > 0;
    }

    // Pergunta uma decisão de efeito. Em multiplayer: registra o callback nos dois clientes,
    // mostra o popup SÓ para o dono (deciderPlayerNumber), e a escolha viaja por RPC.
    // Em offline: mostra o popup local direto.
    public static void AskEffectDecision(int deciderPlayerNumber, string message,
        string yesLabel, string noLabel, System.Action<bool> onDecision)
    {
        // Modo offline / sem Photon: popup local
        if (!PhotonNetwork.inRoom || Instance == null)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowDecisionPopup(message, yesLabel,
                    () => onDecision(true), noLabel, () => onDecision(false));
            }
            else
            {
                onDecision(true); // Sem UI: comporta-se como o auto-yes do ShowDecisionPopup
            }
            return;
        }

        int id = Instance.nextDecisionId++;
        Instance.pendingDecisions[id] = onDecision;

        if (deciderPlayerNumber == Instance.myPlayerNumber)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowDecisionPopup(message, yesLabel,
                    () => Instance.SendEffectDecisionRPC(id, true),
                    noLabel,
                    () => Instance.SendEffectDecisionRPC(id, false));
            }
            else
            {
                Instance.SendEffectDecisionRPC(id, true);
            }
        }
        else if (BotMode.IsBot(deciderPlayerNumber))
        {
            // Modo Treino: o bot decide sozinho (sempre ativa o efeito), com uma
            // pausa curta para o jogador ver o que está acontecendo
            Instance.StartCoroutine(Instance.BotDecisionRoutine(id));
        }
        else
        {
            // Avisa quem está ESPERANDO: sem isso o jogador "saía batendo" sem
            // saber que o oponente ainda estava lendo o popup do primeiro ataque
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowWaitingBanner("O oponente está decidindo se ativa um efeito de carta...");
            }
            Debug.Log($"[PhotonGame] Aguardando decisão do oponente (id {id}): {message}");
        }
    }

    // Decisão automática do bot (modo treino): aceita o efeito após um instante
    System.Collections.IEnumerator BotDecisionRoutine(int decisionId)
    {
        yield return new WaitForSeconds(0.8f);
        SendEffectDecisionRPC(decisionId, true);
    }

    void SendEffectDecisionRPC(int decisionId, bool accepted)
    {
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_EffectDecision", PhotonTargets.All, decisionId, accepted);
            Debug.Log($"[PhotonGame] Enviado RPC: Decisão {decisionId} = {accepted}");
        }
    }

    [PunRPC]
    public void RPC_EffectDecision(int decisionId, bool accepted)
    {
        Debug.Log($"[PhotonGame] RPC_EffectDecision: id {decisionId}, aceito: {accepted}");

        ReseedForAction(); // Callbacks podem usar Random (ex: congelamento aleatório do Mage 3)

        System.Action<bool> callback;
        if (pendingDecisions.TryGetValue(decisionId, out callback))
        {
            pendingDecisions.Remove(decisionId);
            callback(accepted);
        }
        else
        {
            // Os IDs são contadores implícitos nos dois clientes; se divergirem
            // (ex.: exceção no meio de um efeito num lado só), a entrada órfã
            // ficava em pendingDecisions PARA SEMPRE → IsDecisionPending()
            // travava cliques e o fim de turno = jogo congelado sem volta.
            // Melhor limpar e seguir (com log alto) do que travar a partida
            Debug.LogError($"[PhotonGame] Decisão {decisionId} não encontrada neste cliente! " +
                $"Possível dessincronização — limpando {pendingDecisions.Count} decisão(ões) pendente(s) para o jogo não travar.");
            pendingDecisions.Clear();

            // Aviso VISÍVEL: a partida pode ter divergido entre os clientes.
            // Melhor os jogadores saberem (e exportarem os logs) do que seguir
            // jogando uma partida corrompida só com um log no console
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.ShowDecisionPopup(
                    "⚠ A partida pode ter DESSINCRONIZADO entre os jogadores!\n" +
                    "Recomendado: exportem os logs (botão Logs > Exportar) e reiniciem a sala.",
                    "Entendi", () => { }, "Fechar", () => { });
            }
        }

        // Última decisão resolvida: tira a faixa de espera da tela
        if (pendingDecisions.Count == 0 && GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideWaitingBanner();
        }
    }

    // ========== SELEÇÃO DE ALVO DE EFEITO (congelar, quebrar armadura, escudo) ==========
    // effectType: 1 = congelar (Mage 1), 2 = quebrar armadura (Mage 2), 3 = escudo do Healer 2 em Tank

    public void SendEffectTargetRPC(int effectType, int sourceRow, int sourceCol, int targetRow, int targetCol)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_EffectTarget", PhotonTargets.All, effectType, sourceRow, sourceCol, targetRow, targetCol);
            Debug.Log($"[PhotonGame] Enviado RPC: Efeito {effectType} de ({sourceRow},{sourceCol}) em ({targetRow},{targetCol})");
        }
    }

    [PunRPC]
    public void RPC_EffectTarget(int effectType, int sourceRow, int sourceCol, int targetRow, int targetCol)
    {
        Debug.Log($"[PhotonGame] RPC_EffectTarget: tipo {effectType}, ({sourceRow},{sourceCol}) -> ({targetRow},{targetCol})");

        ReseedForAction();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecuteEffectOnTarget(effectType, sourceRow, sourceCol, targetRow, targetCol);
        }
    }

    // ========== ATAQUE COM ALVO ESPECÍFICO (clique na carta inimiga) ==========

    public void SendTargetedAttackRPC(int fromRow, int fromCol, int toRow, int toCol)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_TargetedAttack", PhotonTargets.All, fromRow, fromCol, toRow, toCol);
            Debug.Log($"[PhotonGame] Enviado RPC: Ataque com alvo ({fromRow},{fromCol}) -> ({toRow},{toCol})");
        }
    }

    [PunRPC]
    public void RPC_TargetedAttack(int fromRow, int fromCol, int toRow, int toCol)
    {
        Debug.Log($"[PhotonGame] RPC_TargetedAttack: ({fromRow},{fromCol}) -> ({toRow},{toCol})");

        ReseedForAction();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecuteTargetedAttack(fromRow, fromCol, toRow, toCol);
        }
    }

    // ========== ATAQUE À TORRE ==========

    public void SendTowerAttackRPC(int row, int column, int targetPlayerNumber)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_TowerAttack", PhotonTargets.All, row, column, targetPlayerNumber);
            Debug.Log($"[PhotonGame] Enviado RPC: Ataque à torre do P{targetPlayerNumber} pela carta em ({row}, {column})");
        }
    }

    [PunRPC]
    public void RPC_TowerAttack(int row, int column, int targetPlayerNumber)
    {
        Debug.Log($"[PhotonGame] RPC_TowerAttack: carta em ({row}, {column}) ataca torre do P{targetPlayerNumber}");

        ReseedForAction();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExecuteTowerAttack(row, column, targetPlayerNumber);
        }
    }

    // ========== MAGIA DA TORRE (compra/equipar) ==========

    // Compra de carta mágica da torre: executa nos DOIS clientes (a validação
    // do TowerSystem é idêntica sobre o mesmo estado). replaceSlot: -1 = slot
    // livre; 0/1 = substitui a equipada naquele slot.
    public void SendBuyTowerCardRPC(int buyerPlayerNumber, int cardId, int replaceSlot)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_BuyTowerCard", PhotonTargets.All, buyerPlayerNumber, cardId, replaceSlot);
            Debug.Log($"[PhotonGame] Enviado RPC: P{buyerPlayerNumber} compra magia de torre {cardId} (slot {replaceSlot})");
        }
    }

    [PunRPC]
    public void RPC_BuyTowerCard(int buyerPlayerNumber, int cardId, int replaceSlot)
    {
        Debug.Log($"[PhotonGame] RPC_BuyTowerCard: P{buyerPlayerNumber} compra {cardId} (slot {replaceSlot})");

        ReseedForAction();
        TowerSystem.BuyCard(buyerPlayerNumber, cardId, replaceSlot);
    }

    // ========== SINCRONIZAÇÃO DE TURNO ==========

    // Envia "passar a vez" para os dois clientes
    public void SendEndTurnRPC()
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_EndTurn", PhotonTargets.All);
        }
    }

    [PunRPC]
    public void RPC_EndTurn()
    {
        Debug.Log("[PhotonGame] RPC_EndTurn recebido");

        // Reseed: efeitos de fim de turno usam Random (ex: congelamento aleatório do Mage 5)
        ReseedForAction();

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.EndTurn();
        }
    }

    // ========== SINCRONIZAÇÃO DE INÍCIO DE PARTIDA ==========

    // Este jogador clicou em "Iniciar Partida"
    public void SendPlayerReadyRPC()
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_PlayerReady", PhotonTargets.All, myPlayerNumber);
        }
    }

    [PunRPC]
    public void RPC_PlayerReady(int playerNumber)
    {
        Debug.Log($"[PhotonGame] RPC_PlayerReady: Jogador {playerNumber} está pronto");
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.SetPlayerReady(playerNumber);
        }
    }

    // ========== SINCRONIZAÇÃO DE RESET DA LOJA ==========

    // Carrega o número do jogador: na fase inicial (compras simultâneas) o
    // "jogador do turno" não existe, então o reset é de quem clicou
    public void SendResetStoreRPC(int playerNumber)
    {
        if (!PhotonNetwork.connected) return;

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            photonView.RPC("RPC_ResetStore", PhotonTargets.All, playerNumber);
        }
    }

    [PunRPC]
    public void RPC_ResetStore(int playerNumber)
    {
        Debug.Log($"[PhotonGame] RPC_ResetStore recebido (P{playerNumber})");
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.TryResetStore(playerNumber);
        }
    }

    // ========== TRAVAR LOJA (checkbox — não renovar na virada do round) ==========

    // AllViaServer: o PRÓPRIO remetente também recebe via servidor, garantindo a
    // MESMA ordem global do RPC_EndTurn nos dois clientes. Com PhotonTargets.All
    // o remetente aplicaria na hora e o oponente depois — se um EndTurn chegasse
    // no meio, um cliente renovava a loja e o outro não (desync do pool).
    public void SendShopLockRPC(int playerNumber, bool locked)
    {
        if (!PhotonNetwork.connected) return;
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("RPC_SetShopLock", PhotonTargets.AllViaServer, playerNumber, locked);
            Debug.Log($"[PhotonGame] Enviado RPC: Travar loja P{playerNumber} = {locked}");
        }
    }

    [PunRPC]
    public void RPC_SetShopLock(int playerNumber, bool locked)
    {
        Debug.Log($"[PhotonGame] RPC_SetShopLock: P{playerNumber} -> {locked}");
        if (CardManager.Instance != null)
            CardManager.Instance.SetShopLocked(playerNumber, locked);
    }

    // ========== REINÍCIO DE PARTIDA (Jogar Novamente) ==========

    // Botão "Jogar Novamente": qualquer jogador pode pedir. O Master gera a NOVA
    // seed e sincroniza o reinício nos dois clientes (loja idêntica, sem desync).
    public void RequestRestart()
    {
        if (!PhotonNetwork.inRoom)
        {
            // Offline: gera seed local e reinicia direto
            int seed = UnityEngine.Random.Range(1, 100000);
            DoRestart(seed);
            return;
        }

        PhotonView pv = GetComponent<PhotonView>();
        if (pv == null) return;

        if (PhotonNetwork.isMasterClient)
        {
            int seed = UnityEngine.Random.Range(1, 100000);
            pv.RPC("RPC_RestartGame", PhotonTargets.All, seed);
        }
        else
        {
            // Não-master pede ao Master, que gera a seed e faz o broadcast
            pv.RPC("RPC_RequestRestart", PhotonTargets.MasterClient);
        }
    }

    [PunRPC]
    public void RPC_RequestRestart()
    {
        if (!PhotonNetwork.isMasterClient) return;
        int seed = UnityEngine.Random.Range(1, 100000);
        PhotonView pv = GetComponent<PhotonView>();
        if (pv != null) pv.RPC("RPC_RestartGame", PhotonTargets.All, seed);
    }

    [PunRPC]
    public void RPC_RestartGame(int seed)
    {
        Debug.Log($"[PhotonGame] RPC_RestartGame com seed {seed}");
        DoRestart(seed);
    }

    // Aplica a nova seed (zerando o contador de ações do lockstep) e dispara o
    // reset completo no TurnManager, que respawna a loja da fase inicial.
    void DoRestart(int seed)
    {
        currentGameSeed = seed;
        syncedActionCount = 0;
        UnityEngine.Random.InitState(seed);

        // Reaplica o cenário com a seed nova (o MAPA continua o escolhido pelo
        // anfitrião — room property "theme" —, só a decoração é re-semeada)
        BoardThemeManager.SetSeed(seed);

        if (TurnManager.Instance != null)
            TurnManager.Instance.RestartGame();
    }

    // ── Desconexões no meio da partida → salva como "abandonada" ─────────
    // (callbacks mágicos do PUN; só reage se a partida estiver rolando)

    // O OPONENTE caiu/saiu: este cliente ainda está online → upload normal
    void OnPhotonPlayerDisconnected(PhotonPlayer other)
    {
        // Só se a partida está em andamento E ainda NÃO foi decidida. Sem o
        // MatchInProgress, o vencedor saindo DEPOIS de ganhar viraria uma
        // vitória falsa para quem já tinha perdido (não há estado GameOver —
        // o gameState continua "Playing" após o fim).
        if (TurnManager.Instance != null && TurnManager.Instance.gameState == GameState.Playing
            && MatchReporter.MatchInProgress)
        {
            // O oponente saiu no meio (desistiu, caiu ou fechou): quem ficou
            // VENCE. Mostra a tela de vitória e registra a vitória (em vez de
            // "abandonada" — antes o jogador que ficou levava um registro de
            // abandono mesmo sem ter feito nada de errado).
            int me = myPlayerNumber != 0 ? myPlayerNumber : 1;
            if (GameUIManager.Instance != null)
                GameUIManager.Instance.ShowVictoryScreen(me);
            else
                MatchReporter.ReportMatchEnd(me);
        }
    }

    // EU perdi a conexão: o upload provavelmente falha e vira log pendente
    void OnDisconnectedFromPhoton()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.gameState == GameState.Playing)
            MatchReporter.ReportMatchAbandoned();
    }

    // Sincroniza a seed aleatória para ambos os jogadores
    void SyncGameSeed()
    {
        Debug.Log($"[PhotonGame] SyncGameSeed() chamado. isMasterClient: {PhotonNetwork.isMasterClient}");

        if (PhotonNetwork.isMasterClient)
        {
            // P1 gera a seed e já spawna sua loja com ela
            currentGameSeed = UnityEngine.Random.Range(1, 100000);
            Debug.Log($"[PhotonGame] P1 gerou seed: {currentGameSeed}");
            ApplySeedAndSpawnShop(currentGameSeed);
        }
        else
        {
            // P2 pede a seed ao P1 (repete até receber, pois o P1 pode ainda estar carregando)
            Debug.Log("[PhotonGame] Sou P2, pedindo seed ao P1...");
            StartCoroutine(RequestSeedRoutine());
        }
    }

    System.Collections.IEnumerator RequestSeedRoutine()
    {
        while (currentGameSeed == 0)
        {
            if (PhotonNetwork.connected)
            {
                PhotonView photonView = GetComponent<PhotonView>();
                if (photonView != null)
                {
                    Debug.Log("[PhotonGame] Enviando RPC_RequestSeed ao Master...");
                    photonView.RPC("RPC_RequestSeed", PhotonTargets.MasterClient);
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // P2 pede a seed; o Master responde com RPC_SetGameSeed
    [PunRPC]
    public void RPC_RequestSeed()
    {
        Debug.Log("[PhotonGame] RPC_RequestSeed recebido");

        if (!PhotonNetwork.isMasterClient) return;
        if (currentGameSeed == 0)
        {
            Debug.LogWarning("[PhotonGame] Seed ainda não gerada, pedido ignorado (P2 vai pedir de novo)");
            return;
        }

        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView != null)
        {
            Debug.Log($"[PhotonGame] Respondendo com seed {currentGameSeed}");
            photonView.RPC("RPC_SetGameSeed", PhotonTargets.Others, currentGameSeed);
        }
    }

    // Aplica a seed e spawna a loja
    void ApplySeedAndSpawnShop(int seed)
    {
        // Entrega a seed ao cenário (decoração determinística; o mapa em si vem
        // da escolha do anfitrião via room property "theme")
        BoardThemeManager.SetSeed(seed);

        UnityEngine.Random.InitState(seed);
        Debug.Log($"[PhotonGame] Random.InitState({seed}) aplicado");

        if (CardManager.Instance != null)
        {
            CardManager.Instance.SpawnRandomCards();
        }
        else
        {
            Debug.LogWarning("[PhotonGame] CardManager ainda não existe, loja será spawnada quando ele iniciar");
        }
    }

    // RPC para sincronizar seed
    [PunRPC]
    public void RPC_SetGameSeed(int seed)
    {
        // Se já aplicou essa seed, ignora (evita spawns duplicados)
        if (currentGameSeed == seed)
        {
            Debug.Log($"[PhotonGame] Seed {seed} já aplicada, ignorando");
            return;
        }

        currentGameSeed = seed;
        Debug.Log($"[PhotonGame] RPC_SetGameSeed recebido com seed: {seed}");

        ApplySeedAndSpawnShop(seed);
    }
}
