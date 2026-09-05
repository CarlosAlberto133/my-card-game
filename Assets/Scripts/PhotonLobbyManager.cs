using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Fluxo do lobby:
// - Create Room  -> cria sala com nome único e abre popup de espera (anfitrião)
// - Join Game    -> abre popup com a lista de salas abertas; clicar em uma entra nela
// - Anfitrião inicia a partida quando o jogador 2 entrar (via room property "start",
//   sem RPC novo — não mexe no RpcList nem na sincronização do jogo)
// - Fechar Jogo  -> sai do aplicativo
public class PhotonLobbyManager : UnityEngine.MonoBehaviour
{
    // Versão do jogo — separa builds incompatíveis no matchmaking do Photon E
    // marca a telemetria de balanceamento (card_stats). Subir a cada mudança
    // de simulação. "4.1" = rebalance das 4 classes (tanks/arqueiros/healers/magos).
    // 4.2: travar cartas individuais na loja mudou o refresh (sorteios dependem
    // das travas) + RPC novo — builds 4.1 não podem parear com esta
    // 4.3: Ponta Perfurante (Sanguinária/Couraçada/Sabotadora quebram 1 de
    // armadura ao atacar) muda a simulação + telemetria de dano corrigida
    // (TakeDamage não registrava — números de dano antigos são subcontados)
    // + lendários das tríades (Arcanor 6/0/7 cataclisma/raio tipo 17;
    // Serafina 3/0/8 cura em área por round) no lugar dos payoffs antigos
    public const string GameVersion = "4.4";

    public Button createRoomButton;
    public Button joinRoomButton;
    public RoomListPanel roomListPanel; // legado (painel antigo da cena; não é mais usado)

    private LobbyUI ui;
    private bool loadingGame = false;
    private bool retriedCreate = false;

    // Salas que o servidor JA recusou nesta sessão de lobby.
    //
    // A lista de salas do Photon é um cache no cliente, e ele mente: a cada
    // atualização o PUN joga fora a entrada antiga e monta um RoomInfo novo com
    // o que veio no pacote (NetworkingPeer.cs, EventCode.GameListUpdate), e os
    // padrões de RoomInfo são IsOpen=true e MaxPlayers=0. Uma sala fechada pode
    // reaparecer "aberta" e com contagem velha. Quando o servidor responde
    // "cheia"/"fechada", ISSO é a verdade — anotamos e sumimos com a linha.
    private readonly System.Collections.Generic.HashSet<string> salasRecusadas =
        new System.Collections.Generic.HashSet<string>();

    // Qual sala o jogador clicou por último (o callback de falha do PUN não diz)
    private string ultimaSalaTentada;

    private enum State { Idle, Browsing, HostWaiting, GuestWaiting }
    private State state = State.Idle;
    private bool startingBotMode = false;
    private Button quitButtonRef;
    private Button botButtonRef;
    private Button howToButtonRef;
    private Button quickButtonRef;

    // Partida Rápida em andamento: o sorteio foi pedido e ainda não voltou
    private bool procurandoPartida = false;

    void Start()
    {
        // Voltando de um treino contra o bot: desliga o modo offline do Photon
        // ANTES de qualquer coisa, para reconectar ao multiplayer normal
        BotMode.Enabled = false;
        if (PhotonNetwork.offlineMode)
        {
            if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
            PhotonNetwork.offlineMode = false;
        }

        // Nick que o oponente vê na partida (custom > conta Google > "Jogador")
        PhotonNetwork.playerName = PlayerNick.Get();

        // Cenário de taverna/mesa de RPG (tema do jogo) montado por código
        LobbyDecor.Build();

        // Precisamos estar no lobby do Photon para receber a lista de salas
        PhotonNetwork.autoJoinLobby = true;

        if (!PhotonNetwork.connected)
        {
            // gameVersion separa builds incompatíveis no matchmaking. "4.1" =
            // rebalance dos Tanks (+1 vida todos, +1 escudo T4/T5, statlines
            // novas), aura de ataque duplo com cooldown de round, guarda só
            // protege quem está ao lado/atrás (e protege tanks), 50% de
            // redução não acumula, e delay de 2 turnos no Mage 3.
            // "4.0" = balanceamento de efeitos. Builds antigos simulariam
            // outro resultado → desync.
            PhotonNetwork.ConnectUsingSettings(GameVersion);
            Debug.Log("[Lobby] Conectando ao Photon...");
        }
        else if (PhotonNetwork.inRoom)
        {
            // Voltou para o lobby ainda dentro de uma sala: sai dela primeiro
            PhotonNetwork.LeaveRoom();
        }
        else if (!PhotonNetwork.insideLobby)
        {
            PhotonNetwork.JoinLobby();
        }

        // Botões extras ANTES dos popups: quem entra por último no Canvas
        // desenha por cima, e os popups precisam cobrir os botões
        CreateQuitButton();
        CreateBotTrainingButton();
        CreateHowToPlayButton();
        CreateQuickMatchButton();

        // Constrói os popups (ficam ocultos até serem usados)
        Canvas canvas = createRoomButton != null
            ? createRoomButton.GetComponentInParent<Canvas>()
            : FindObjectOfType<Canvas>();

        // RESPONSIVIDADE: a UI inteira do lobby é desenhada em referência
        // 1920x1080. O Canvas da cena estava em "Constant Pixel Size" (800x600)
        // — em telas menores (notebook 1366x768) nada encolhia e os painéis
        // transbordavam. Escala pela tela, como a cena da partida já faz.
        EnsureResponsiveScaler(canvas);

        ui = new LobbyUI(canvas);
        ui.OnListClosed = () => { state = State.Idle; };

        // [DECKMODE] Botão "Montar Deck (beta)" no canto — abre o construtor
        // de decks do modo deck. Para DESATIVAR o modo: comente esta linha
        DeckBuilderUI.AddEntryButton(canvas);

        // Título do jogo + painel de perfil (estatísticas da conta do launcher)
        LobbyProfileUI.Build(canvas);

        // Menu no tema da taverna: tabuleta de madeira + botões arredondados
        // com moldura dourada, empilhados numa coluna organizada
        LayoutMenuColumn();

        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(CreateRoom);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OpenRoomList);

        UpdateMainButtons();

        if (!PhotonNetwork.insideLobby)
            ui.Toast("Conectando ao servidor...");
    }

    // Escala a UI pela resolução da tela (referência 1920x1080). match 0.5
    // equilibra largura/altura: em 16:9 é idêntico, e em telas 16:10/4:3
    // encolhe um pouco dos dois lados sem sobrepor o título com o perfil
    // (match 1.0 preservaria a altura e faria os painéis laterais invadirem
    // o centro em telas mais quadradas)
    public static void EnsureResponsiveScaler(Canvas canvas)
    {
        if (canvas == null) return;

        UnityEngine.UI.CanvasScaler scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();

        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    // Habilita Create/Join somente quando estamos no lobby do Photon
    void UpdateMainButtons()
    {
        bool ready = PhotonNetwork.insideLobby;
        if (createRoomButton != null) createRoomButton.interactable = ready;
        if (joinRoomButton != null) joinRoomButton.interactable = ready;
        if (quickButtonRef != null) quickButtonRef.interactable = ready;
    }

    // Botão "Fechar Jogo" clonado do Create Room para manter o mesmo visual
    void CreateQuitButton()
    {
        if (createRoomButton == null) return;

        GameObject clone = Instantiate(createRoomButton.gameObject, createRoomButton.transform.parent);
        clone.name = "QuitButton";

        RectTransform sourceRt = createRoomButton.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -116f);

        // Fica logo após o Create Room na hierarquia — nunca por cima dos popups,
        // que são adicionados no fim do Canvas
        clone.transform.SetSiblingIndex(createRoomButton.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "Fechar Jogo";
        }
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Fechar Jogo";
        }

        Button quitButton = clone.GetComponent<Button>();
        quitButton.interactable = true; // funciona mesmo sem conexão
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(QuitGame);
        quitButtonRef = quitButton;
    }

    void QuitGame()
    {
        Debug.Log("[Lobby] Fechando o jogo...");
        Application.Quit();
    }

    // Botão "Como Jogar" — clone do Create Room, abre a tela de regras
    void CreateHowToPlayButton()
    {
        if (createRoomButton == null) return;

        GameObject clone = Instantiate(createRoomButton.gameObject, createRoomButton.transform.parent);
        clone.name = "HowToPlayButton";
        clone.transform.SetSiblingIndex(createRoomButton.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null) tmpLabel.text = "Como Jogar";
        else { Text legacy = clone.GetComponentInChildren<Text>(); if (legacy != null) legacy.text = "Como Jogar"; }

        Button b = clone.GetComponent<Button>();
        b.interactable = true; // funciona sem conexão
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(ShowHowToPlay);
        howToButtonRef = b;
    }

    void ShowHowToPlay()
    {
        Canvas canvas = createRoomButton != null
            ? createRoomButton.GetComponentInParent<Canvas>()
            : FindObjectOfType<Canvas>();
        HowToPlayUI.Show(canvas);
    }

    // Monta a coluna do menu como na arte de referência: painel escuro
    // ornamentado CENTRALIZADO na parte de baixo da tela (sobre o tabuleiro),
    // "Criar Sala" dourado e os demais botões escuros com moldura dourada.
    void LayoutMenuColumn()
    {
        if (createRoomButton == null) return;

        RectTransform baseRt = createRoomButton.GetComponent<RectTransform>();
        // Com 6 botões a tabuleta cresce para BAIXO (o topo dela não se mexe: o
        // centro desce meio "gap" e a altura sobe um "gap" inteiro). Com os 76/60
        // de antes o rodapé do painel encostava na borda de baixo da tela de
        // referência (1080) — daí o aperto.
        Vector2 btnSize = new Vector2(350f, 56f);
        float gap = 68f;

        // Posição fixa no centro-baixo do quadro (independente da cena)
        baseRt.anchorMin = new Vector2(0.5f, 0.5f);
        baseRt.anchorMax = new Vector2(0.5f, 0.5f);
        baseRt.pivot = new Vector2(0.5f, 0.5f);
        Vector2 basePos = new Vector2(0f, -96f);

        // Tabuleta de fundo (desenhada antes dos botões = atrás deles)
        GameObject board = new GameObject("MenuBoard", typeof(RectTransform), typeof(Image));
        board.transform.SetParent(createRoomButton.transform.parent, false);
        board.transform.SetSiblingIndex(createRoomButton.transform.GetSiblingIndex());
        RectTransform brt = board.GetComponent<RectTransform>();
        brt.anchorMin = baseRt.anchorMin;
        brt.anchorMax = baseRt.anchorMax;
        brt.pivot = baseRt.pivot;
        // 6 botões (Rápida / Criar / Procurar / Treinar / Como Jogar / Fechar)
        brt.anchoredPosition = basePos + new Vector2(0f, -gap * 2.5f);
        brt.sizeDelta = new Vector2(btnSize.x + 58f, gap * 5f + btnSize.y + 58f);
        Image boardImg = board.GetComponent<Image>();
        LobbySprites.MakeRounded(boardImg, new Color(0.055f, 0.040f, 0.028f, 0.94f));
        boardImg.raycastTarget = false;
        LobbySprites.AddRing(board.transform, new Color(0.96f, 0.77f, 0.32f, 0.75f));

        // Losangos decorativos no topo e na base do painel (como na arte)
        MakeMenuDiamond(board.transform, new Vector2(0f, brt.sizeDelta.y / 2f), 16f);
        MakeMenuDiamond(board.transform, new Vector2(0f, -brt.sizeDelta.y / 2f), 16f);

        // "Partida Rápida" é o botão dourado (o caminho recomendado): o sorteio
        // é decidido no servidor e não depende da lista, que pode estar defasada
        StyleMenuButton(quickButtonRef, "Partida Rápida", true, basePos, btnSize, baseRt);
        StyleMenuButton(createRoomButton, "Criar Sala", false, basePos + new Vector2(0f, -gap), btnSize, baseRt);
        StyleMenuButton(joinRoomButton, "Procurar Salas", false, basePos + new Vector2(0f, -gap * 2f), btnSize, baseRt);
        StyleMenuButton(botButtonRef, "Treinar vs Bot", false, basePos + new Vector2(0f, -gap * 3f), btnSize, baseRt);
        StyleMenuButton(howToButtonRef, "Como Jogar", false, basePos + new Vector2(0f, -gap * 4f), btnSize, baseRt);
        StyleMenuButton(quitButtonRef, "Fechar Jogo", false, basePos + new Vector2(0f, -gap * 5f), btnSize, baseRt);
    }

    // Losango dourado decorativo (quadrado arredondado girado 45°)
    static void MakeMenuDiamond(Transform parent, Vector2 pos, float size)
    {
        GameObject go = new GameObject("Diamond", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image img = go.GetComponent<Image>();
        LobbySprites.MakeRounded(img, new Color(0.96f, 0.77f, 0.32f));
        img.raycastTarget = false;
    }

    // Reconstrói o visual de um botão: sprite arredondado, moldura dourada,
    // rótulo em português e cores de hover/clique via ColorBlock
    void StyleMenuButton(Button b, string label, bool primary, Vector2 pos, Vector2 size, RectTransform anchorRef)
    {
        if (b == null) return;

        Color gold = new Color(0.96f, 0.77f, 0.32f);
        Color woodDark = new Color(0.105f, 0.078f, 0.052f, 0.98f);
        Color textDark = new Color(0.14f, 0.10f, 0.03f);
        Color textLight = new Color(0.95f, 0.91f, 0.82f);
        Color fill = primary ? gold : woodDark;

        // Mesmo referencial de âncora do Create Room (posições consistentes)
        RectTransform rt = b.GetComponent<RectTransform>();
        rt.anchorMin = anchorRef.anchorMin;
        rt.anchorMax = anchorRef.anchorMax;
        rt.pivot = anchorRef.pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = b.GetComponent<Image>();
        if (img != null)
        {
            LobbySprites.MakeRounded(img, fill);

            // Moldura dourada (só uma; o layout roda 1x por carga da cena)
            if (b.transform.Find("Ring") == null)
                LobbySprites.AddRing(b.transform, primary
                    ? new Color(1f, 0.92f, 0.66f, 0.9f)
                    : new Color(0.96f, 0.77f, 0.32f, 0.70f));

            // Hover/clique: clareia/escurece a cor do próprio botão
            b.targetGraphic = img;
            b.transition = Selectable.Transition.ColorTint;
            ColorBlock cb = b.colors;
            cb.normalColor = fill;
            cb.highlightedColor = Color.Lerp(fill, Color.white, 0.18f);
            cb.pressedColor = Color.Lerp(fill, Color.black, 0.22f);
            cb.selectedColor = fill;
            cb.disabledColor = new Color(fill.r, fill.g, fill.b, 0.35f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
        }

        TMP_Text tmp = b.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = label;
            tmp.color = primary ? textDark : textLight;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = 22f;
            tmp.characterSpacing = 2f;
        }
        else
        {
            Text legacy = b.GetComponentInChildren<Text>();
            if (legacy != null)
            {
                legacy.text = label;
                legacy.color = primary ? textDark : textLight;
                legacy.fontStyle = FontStyle.Bold;
            }
        }
    }

    // ================== TREINO CONTRA O BOT ==================

    // Botão "Treinar vs Bot" clonado do Create Room (mesmo visual), na mesma
    // coluna dele: 2 posições abaixo (-232), logo após o "Fechar Jogo" (-116).
    // NÃO usar a posição do Join como referência: dependendo do layout da cena,
    // "join - 116" cai em cima do Create Room e esconde o botão.
    void CreateQuickMatchButton()
    {
        Button source = createRoomButton != null ? createRoomButton : joinRoomButton;
        if (source == null) return;

        GameObject clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = "QuickMatchButton";

        RectTransform sourceRt = source.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -348f);
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "Partida Rápida";
        }
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Partida Rápida";
        }

        Button quickButton = clone.GetComponent<Button>();
        quickButton.interactable = false;   // liberado quando entrar no lobby
        quickButton.onClick.RemoveAllListeners();
        quickButton.onClick.AddListener(QuickMatch);
        quickButtonRef = quickButton;
    }

    void CreateBotTrainingButton()
    {
        Button source = createRoomButton != null ? createRoomButton : joinRoomButton;
        if (source == null) return;

        GameObject clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = "BotTrainingButton";

        RectTransform sourceRt = source.GetComponent<RectTransform>();
        RectTransform cloneRt = clone.GetComponent<RectTransform>();
        cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -232f);
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        TMP_Text tmpLabel = clone.GetComponentInChildren<TMP_Text>();
        if (tmpLabel != null)
        {
            tmpLabel.text = "Treinar vs Bot";
        }
        else
        {
            Text legacyLabel = clone.GetComponentInChildren<Text>();
            if (legacyLabel != null) legacyLabel.text = "Treinar vs Bot";
        }

        Button botButton = clone.GetComponent<Button>();
        botButton.interactable = true; // treino é offline: funciona sem conexão
        botButton.onClick.RemoveAllListeners();
        botButton.onClick.AddListener(StartTrainingVsBot);
        botButtonRef = botButton;
    }

    // Abre o popup do treino (dificuldade + mapa); a escolha da dificuldade
    // inicia a partida offline: liga o offlineMode do Photon (os mesmos RPCs
    // executam localmente — o jogo não percebe diferença) e carrega a cena
    // da partida direto, sem sala online
    int botMapTheme = 1; // mapa escolhido no popup (padrão Mesa de RPG)

    void StartTrainingVsBot()
    {
        if (loadingGame || startingBotMode) return;

        ui.ShowBotSetup((difficulty, mapTheme) =>
        {
            if (loadingGame || startingBotMode) return;
            startingBotMode = true;
            BotMode.Enabled = true;
            BotMode.Difficulty = difficulty;
            botMapTheme = mapTheme;

            ui.Toast("Preparando o treino contra o bot...");
            StartCoroutine(BotModeRoutine());
        });
    }

    System.Collections.IEnumerator BotModeRoutine()
    {
        // O offlineMode só pode ligar DESCONECTADO do Photon
        if (PhotonNetwork.connected && !PhotonNetwork.offlineMode)
        {
            PhotonNetwork.Disconnect();
            float t = 0f;
            while (PhotonNetwork.connected && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        PhotonNetwork.offlineMode = true;
        PhotonNetwork.playerName = PlayerNick.Get();

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        PhotonNetwork.CreateRoom("Treino vs Bot", options, null);

        // Mapa: o escolhido no popup do treino. A sala offline aceita room
        // properties normalmente — a cena do jogo lê "theme" como sempre
        Hashtable props = new Hashtable();
        props["theme"] = botMapTheme;
        // [DECKMODE] modo escolhido no popup do treino (0 = padrão, 1 = deck)
        props["mode"] = ui != null ? ui.SelectedBotGameMode : 0;
        if (PhotonNetwork.room != null) PhotonNetwork.room.SetCustomProperties(props);

        LoadGameScene();
    }

    // ================== PARTIDA RÁPIDA ==================
    //
    // Matchmaking do SERVIDOR. A diferença para "Procurar Salas" não é de
    // conforto, é de confiabilidade: a lista de salas é um cache no cliente e
    // pode mentir (ver OnPhotonJoinRoomFailed), enquanto o sorteio é resolvido
    // no servidor, de forma atômica — não existe sala fantasma para clicar.
    //
    // Os dois modos convivem: quem quer jogar com um amigo específico ainda cria
    // a sala e passa o nome. Este caminho não toca em nada do outro; depois do
    // OnJoinedRoom o fluxo é exatamente o mesmo (anfitrião espera, convidado
    // aguarda o início) e nenhum RPC novo entra em cena.
    void QuickMatch()
    {
        if (!PhotonNetwork.insideLobby)
        {
            ui.Toast("Ainda conectando ao servidor, aguarde...");
            return;
        }

        PhotonNetwork.playerName = PlayerNick.Get();
        procurandoPartida = true;
        ui.Toast("Procurando uma partida...");
        Debug.Log("[Lobby] Partida Rápida: pedindo sorteio ao servidor.");
        PhotonNetwork.JoinRandomRoom();
    }

    // Sorteio sem resultado (ninguém esperando): vira anfitrião e espera.
    //
    // Callback PRÓPRIO do JoinRandomRoom — não se mistura com
    // OnPhotonJoinRoomFailed, então nada aqui alimenta a lista de salas
    // recusadas. E o PUN mantém o cliente no lobby quando o sorteio falha
    // ("staying on master server"), por isso o CreateRoom() abaixo passa direto
    // pela própria guarda de insideLobby.
    void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        if (!procurandoPartida) return;   // não é nosso
        procurandoPartida = false;

        int codigo = 0;
        if (codeAndMsg != null && codeAndMsg.Length > 0 && codeAndMsg[0] != null)
            codigo = System.Convert.ToInt32(codeAndMsg[0]);

        Debug.Log("[Lobby] Partida Rápida: nenhuma sala aberta (código " + codigo +
                  "). Criando uma e esperando oponente.");
        ui.Toast("Ninguém esperando — criei uma sala para você.");
        CreateRoom();
    }

    // ================== CRIAR SALA ==================

    void CreateRoom()
    {
        if (!PhotonNetwork.insideLobby)
        {
            ui.Toast("Ainda conectando ao servidor, aguarde...");
            return;
        }

        string roomName = "Sala #" + Random.Range(100, 1000);
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;

        // Reaplica o nick (o jogador pode ter editado no perfil agora há pouco)
        PhotonNetwork.playerName = PlayerNick.Get();
        bool success = PhotonNetwork.CreateRoom(roomName, options, null);
        Debug.Log($"[Lobby] Criando sala: {roomName}, Success: {success}");
        if (success) ui.Toast("Criando sala...");
    }

    // Nome sorteado já existia (raro): tenta uma vez com outro número
    void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        Debug.LogWarning("[Lobby] Falha ao criar sala.");
        if (!retriedCreate)
        {
            retriedCreate = true;
            CreateRoom();
        }
        else
        {
            retriedCreate = false;
            ui.Toast("Não foi possível criar a sala. Tente novamente.");
        }
    }

    // ================== LISTA DE SALAS / ENTRAR ==================

    void OpenRoomList()
    {
        if (!PhotonNetwork.insideLobby)
        {
            ui.Toast("Ainda conectando ao servidor, aguarde...");
            return;
        }

        state = State.Browsing;
        ui.Toast("");
        ui.ShowRoomList();
        RefreshRoomList();
    }

    void RefreshRoomList()
    {
        // GetRoomList() devolve o ARRAY INTERNO do PUN (networkingPeer.mGameListCopy),
        // não uma cópia — ordenar no lugar mexeria na estrutura dele. Copiamos.
        RoomInfo[] bruto = PhotonNetwork.GetRoomList();
        System.Collections.Generic.List<RoomInfo> salas =
            new System.Collections.Generic.List<RoomInfo>();
        if (bruto != null)
        {
            foreach (RoomInfo r in bruto)
            {
                if (r == null || string.IsNullOrEmpty(r.Name)) continue;
                if (salasRecusadas.Contains(r.Name)) continue;   // fantasma conhecido
                salas.Add(r);
            }
        }

        // A ordem que o PUN entrega vem de um Dictionary (mGameList) — sem ordem
        // definida, e ela MUDA quando entra ou sai sala. As linhas pulavam de
        // lugar debaixo do cursor, e como duas salas são visualmente iguais
        // ("Sala #123   1/2 jogadores") dá para clicar na errada sem perceber.
        // Por nome, a posição é estável.
        salas.Sort(delegate(RoomInfo a, RoomInfo b)
        {
            return string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
        });

        Debug.Log("[Lobby] Salas na lista: " + salas.Count +
                  " (cache do Photon tinha " + (bruto != null ? bruto.Length : 0) +
                  ", " + salasRecusadas.Count + " recusada(s) escondida(s))");
        ui.PopulateRoomList(salas.ToArray(), JoinSpecificRoom);
    }

    void JoinSpecificRoom(string roomName)
    {
        Debug.Log($"[Lobby] Entrando na sala: {roomName}");
        ultimaSalaTentada = roomName;   // o callback de falha não diz qual era
        ui.Toast("Entrando na sala...");
        PhotonNetwork.playerName = PlayerNick.Get();
        PhotonNetwork.JoinRoom(roomName);
    }

    // O PUN entrega o motivo em codeAndMsg (código do servidor + texto). Antes
    // isso era jogado fora e todo mundo via a mesma frase genérica — por isso um
    // teste com 4 pessoas (04/set/2026) ficou sem diagnóstico: ninguém sabia que
    // o servidor estava respondendo "cheia" para uma sala que a lista mostrava
    // como livre.
    void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        int codigo = 0;
        string detalhe = "";
        if (codeAndMsg != null && codeAndMsg.Length > 0 && codeAndMsg[0] != null)
            codigo = System.Convert.ToInt32(codeAndMsg[0]);   // vem como short
        if (codeAndMsg != null && codeAndMsg.Length > 1 && codeAndMsg[1] != null)
            detalhe = codeAndMsg[1].ToString();

        Debug.LogWarning("[Lobby] Falha ao entrar em '" + ultimaSalaTentada +
                         "': código " + codigo + " — " + detalhe);

        string aviso;
        if (codigo == ErrorCode.GameFull || codigo == ErrorCode.GameClosed)
            aviso = "Essa partida já começou. Escolha outra sala.";
        else if (codigo == ErrorCode.GameDoesNotExist)
            aviso = "Essa sala não existe mais.";
        else
            aviso = "Não foi possível entrar na sala. Tente outra.";

        // O servidor acabou de dizer a verdade sobre esta sala: tira ela da tela
        // para o jogador não insistir no mesmo fantasma
        if (!string.IsNullOrEmpty(ultimaSalaTentada))
            salasRecusadas.Add(ultimaSalaTentada);

        ui.Toast(aviso);
        if (state == State.Browsing) RefreshRoomList();
    }

    // ================== CALLBACKS DO PHOTON ==================

    void OnJoinedLobby()
    {
        Debug.Log("[Lobby] Entrou no lobby do Photon.");
        // Conexão nova = lista nova. Também evita que um nome recusado fique
        // banido para sempre: são só 900 nomes possíveis ("Sala #100".."#999"),
        // então uma sala nova pode acabar reaproveitando o nome de uma morta.
        salasRecusadas.Clear();
        UpdateMainButtons();
        if (state == State.Idle) ui.Toast("");
        if (state == State.Browsing) RefreshRoomList();
    }

    // O Photon atualiza a lista de salas sozinho enquanto estamos no lobby
    void OnReceivedRoomListUpdate()
    {
        if (state == State.Browsing) RefreshRoomList();
    }

    void OnJoinedRoom()
    {
        // Sala offline do treino contra o bot: o fluxo próprio cuida de tudo
        // (nada de popup de anfitrião/espera)
        if (BotMode.Enabled) return;

        procurandoPartida = false;
        string roomName = PhotonNetwork.room != null ? PhotonNetwork.room.Name : "Sala";
        Debug.Log($"[Lobby] Entrou na sala: {roomName} (master: {PhotonNetwork.isMasterClient})");
        ui.Toast("");

        if (PhotonNetwork.isMasterClient)
        {
            // Criador da sala: espera o jogador 2 e inicia (vira o P1 da partida)
            state = State.HostWaiting;
            ui.ShowHostRoom(roomName, StartMatch, LeaveRoom);
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
        }
        else
        {
            // Convidado: espera o anfitrião iniciar (vira o P2 da partida)
            state = State.GuestWaiting;
            ui.ShowGuestRoom(roomName, LeaveRoom);
        }
    }

    void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log($"[Lobby] Jogador entrou na sala: {newPlayer.ID}");
        if (state == State.HostWaiting)
        {
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
            AtualizarVisibilidadeDaSala();
        }
    }

    void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log($"[Lobby] Jogador saiu da sala: {otherPlayer.ID}");
        if (state == State.HostWaiting)
        {
            ui.SetHostStatus(PhotonNetwork.room.PlayerCount >= 2);
            AtualizarVisibilidadeDaSala();   // vagou: volta para a lista
            ui.Toast("O jogador 2 saiu da sala.");
        }
    }

    // A sala some da lista assim que ENCHE — não na hora de "Iniciar Partida".
    //
    // POR QUE ISSO IMPORTA (bug de 04/set/2026, teste com 4 pessoas):
    // antes, IsOpen/IsVisible só viravam false dentro de StartMatch(), na MESMA
    // linha do tempo do SceneManager.LoadScene. Depois disso ninguém mais mexe
    // nessa sala — o lobby foi descarregado. Se o aviso não vingasse ali, a sala
    // ficava listada pelo resto da partida: cinco minutos depois ela ainda
    // aparecia para os outros, com a contagem de jogadores desatualizada, com
    // cara de sala livre. Quem clicava levava "sala cheia" e não entendia nada.
    //
    // Aqui o aviso sai com o cliente parado no lobby, muito antes de qualquer
    // troca de cena, e o servidor tem todo o tempo do mundo para propagá-lo.
    void AtualizarVisibilidadeDaSala()
    {
        // Só o anfitrião mexe, para os dois clientes não mandarem o mesmo aviso
        if (!PhotonNetwork.isMasterClient || PhotonNetwork.room == null) return;

        // MaxPlayers 0 significa "sem limite" no Photon. Se por qualquer motivo a
        // sala vier assim, "cheia" daria sempre true e a sala do anfitrião
        // sumiria da lista para sempre — inclusive depois do convidado sair.
        int limite = PhotonNetwork.room.MaxPlayers;
        if (limite <= 0) limite = 2;   // aqui toda sala é de 2 (ver CreateRoom)

        bool cheia = PhotonNetwork.room.PlayerCount >= limite;

        // Idempotente de graça: os setters do PUN (Room.cs) só falam com o
        // servidor quando o valor MUDA, então repetir a chamada não custa nada
        PhotonNetwork.room.IsOpen = !cheia;
        PhotonNetwork.room.IsVisible = !cheia;

        Debug.Log("[Lobby] Sala '" + PhotonNetwork.room.Name + "' " +
                  (cheia ? "cheia: escondida e fechada." : "com vaga: visível na lista.") +
                  " (" + PhotonNetwork.room.PlayerCount + "/" + limite + ")");
    }

    // Se o anfitrião sair, o convidado não deve herdar a sala: volta para o lobby
    void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        if (state == State.GuestWaiting)
        {
            Debug.Log("[Lobby] O anfitrião saiu; deixando a sala.");
            LeaveRoom();
            ui.Toast("O anfitrião fechou a sala.");
        }
    }

    void LeaveRoom()
    {
        state = State.Idle;
        procurandoPartida = false;
        ui.HideAll();
        if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
        UpdateMainButtons(); // reabilitados quando OnJoinedLobby disparar de novo
    }

    void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] Falha ao conectar: {cause}");
        ui.Toast("Falha na conexão. Verifique sua internet e reabra o jogo.");
    }

    void OnConnectionFail(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] Conexão perdida: {cause}");
        ui.Toast("Conexão perdida com o servidor.");
    }

    // ================== INICIAR PARTIDA ==================

    // Anfitrião clicou em "Iniciar Partida"
    void StartMatch()
    {
        if (PhotonNetwork.room == null || PhotonNetwork.room.PlayerCount < 2)
        {
            ui.Toast("Aguarde o jogador 2 entrar!");
            return;
        }

        Debug.Log("[Lobby] Iniciando a partida!");

        // Rede de segurança. O trabalho de verdade já foi feito em
        // AtualizarVisibilidadeDaSala() quando o jogador 2 entrou — e foi feito
        // lá de propósito, porque AQUI estamos a um passo do LoadScene e este
        // aviso pode não ter tempo de ir embora. Se por algum motivo a sala
        // ainda estiver aberta, os setters mandam agora; se já estiver fechada,
        // não custa nada (o PUN só fala com o servidor quando o valor muda).
        PhotonNetwork.room.IsOpen = false;
        PhotonNetwork.room.IsVisible = false;

        // Room property avisa os DOIS clientes para carregar o jogo
        // (chega ao anfitrião também, mas LoadGameScene é protegido contra repetição).
        // "theme" leva o mapa escolhido pelo anfitrião (0 = Espaço, 1 = Mesa de RPG)
        Hashtable props = new Hashtable();
        props["start"] = 1;
        props["theme"] = ui != null ? ui.SelectedMapTheme : 1;
        // [DECKMODE] modo de jogo escolhido pelo anfitrião (0 = padrão, 1 = deck)
        props["mode"] = ui != null ? ui.SelectedGameMode : 0;
        PhotonNetwork.room.SetCustomProperties(props);

        LoadGameScene();
    }

    // Dispara nos dois clientes quando o anfitrião marca "start" na sala
    void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey("start"))
        {
            Debug.Log("[Lobby] Partida iniciada pelo anfitrião!");
            LoadGameScene();
        }
    }

    void LoadGameScene()
    {
        if (loadingGame) return; // evita carregar duas vezes (clique + callback)
        loadingGame = true;
        Debug.Log("[Lobby] Carregando SampleScene...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }
}
