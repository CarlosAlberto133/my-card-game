using UnityEngine;

// Cenário "mesa de RPG" (temática medieval), 100% por código: uma grande mesa
// de madeira sob o tabuleiro (com veios procedurais), moldura em volta do campo
// e miniaturas de diorama espalhadas — árvores, dados, livros e uma caneca —
// como numa sessão de RPG de mesa. Puramente visual: nenhum collider (não rouba
// cliques) e nenhum uso de UnityEngine.Random (lockstep intocado — as posições
// usam System.Random com a seed da partida, idênticas nos dois clientes).
public static class TabletopEnvironment
{
    // ── Moldura do tabuleiro ──────────────────────────────────────────────
    // Peças próprias do Meshy (Resources/decor/cenario): a mureta de blocos
    // de pedra + o canto em "L". Troque FrameWallPiece por null para voltar
    // à mureta do KayKit sem mais nenhuma mudança.
    public const string FrameWallPiece = "borda-tabuleiro";
    public const string FrameCornerPiece = "canto-tabuleiro";
    public const int FrameSegments = 6; // muretas por lado, entre os 2 cantos

    // O diamante do vértice do canto estica a caixa da peça na diagonal, então
    // o encaixe pela caixa deixa os braços curtos e recuados. Compensação
    // conferida em jogo: aumenta o canto e empurra para FORA na diagonal.
    public const float FrameCornerScale = 1.20f; // multiplicador do tamanho
    public const float FrameCornerOut = 0.97f;   // empurrão para fora (mundo)
    public const float FrameCornerHeight = 2.61f; // teto de altura do canto
                                                  // (a mureta tem ~1.95)

    // Sentido de cada lado da moldura (true = mureta girada 180°). A face
    // trabalhada tem que olhar para o lado certo; acertado em jogo pelo
    // Carlos com as caixinhas do BoardFrameTuning. "Cima" = lado de cima da
    // tela do jogador 1 (z+).
    public const bool FrameFlipCima = true;
    public const bool FrameFlipBaixo = false;
    public const bool FrameFlipDireita = true;
    public const bool FrameFlipEsquerda = false;

    // ── Tampo da mesa ──────────────────────────────────────────────────────
    // Com uma peça em TableTopPiece, o tampo é feito de TÁBUAS de verdade
    // (peça própria do Meshy repetida lado a lado) sobre uma base escura que
    // fecha as frestas e dá a espessura vista de lado. Troque por null para
    // voltar à laje única com a textura de madeira procedural.
    public const string TableTopPiece = "tabua";
    public const float TableTopWidth = 112f;      // eixo X: loja (-29.5) e
    public const float TableTopDepth = 90f;       // mãos (z ±29.5) em cima
    public const float TableTopSurface = -0.15f;  // logo abaixo dos tiles (y 0)
    public const float PlankWidth = 8f;           // largura de cada tábua
    public const float PlankThickness = 1.5f;     // espessura da tábua
    public const float PlankOverlap = 0.15f;      // sobra lateral: sem fresta

    // ── Iluminação da taverna ──────────────────────────────────────────────
    // O ambiente vinha do skybox procedural padrão: um azul de dia claro
    // lavando a mesa inteira e comendo o contraste das tochas. Aqui vira um
    // gradiente escuro e quente, o "sol" cai para luz de sala, e a névoa NA
    // COR DO FUNDO faz as bordas derreterem no escuro em vez de acabarem num
    // corte seco — é por isso que ela usa a mesma TavernDark da câmera.
    //
    // Por código, como o fundo da câmera: vale também com o MesaStage assado
    // e o build não fica dependendo do Lighting salvo na cena.
    public static readonly Color TavernDark = new Color(0.080f, 0.055f, 0.040f);
    public static readonly Color AmbientSky = new Color(0.340f, 0.292f, 0.250f);
    public static readonly Color AmbientEquator = new Color(0.260f, 0.220f, 0.188f);
    public static readonly Color AmbientGround = new Color(0.160f, 0.132f, 0.110f);
    public static readonly Color SunColor = new Color(1.00f, 0.93f, 0.80f);
    public const float SunIntensity = 1.60f;   // era 2 (branca): sol de meio-dia
    public const float FogDensity = 0.0032f;
    // ⚠️ ESCALA DAS LUZES. Uma casa do tabuleiro tem 6 unidades e a tocha tem
    // 7.5 de altura — ou seja, este mundo é ~5× maior que metros. A atenuação
    // de um point light é 1/d² em unidades de mundo, então intensidade que
    // funciona numa cena "1 unidade = 1 metro" precisa ser multiplicada por
    // ~25 aqui. Foi por isso que as tochas de 2.3 não iluminavam NADA: a 6
    // unidades (uma casa) elas entregavam 2.3/36 = 0.06, menos que o ambiente.
    // Se for criar luz nova, pense em quanto quer A UMA CASA DE DISTÂNCIA e
    // multiplique por 36.
    //
    // Estes dois são só tempero: os valores de fábrica já estão certos.
    public const float TorchBoost = 1.00f;
    public const float TorchReach = 1.00f;

    // ── Cenário "ASSADO" na cena (MesaStage) ─────────────────────────────
    // O menu do editor "Cardsworn → Mesa de RPG: assar cenário" transforma
    // este cenário gerado por código em objetos DE VERDADE na cena, dentro de
    // um GameObject chamado MesaStage — para o Carlos editar tudo à mão no
    // editor (mover, girar, apagar, acrescentar). Com um MesaStage na cena,
    // ELE manda: o código só o liga/desliga e pinta o fundo da câmera. Sem
    // ele (apagado da cena), volta o gerador por código abaixo.
    public const string BakedStageName = "MesaStage";

    static GameObject FindBakedStage()
    {
        // Inclusive INATIVOS — o Clear desativa o palco nos outros mapas
        // (mesma técnica do TesteStage no BoardThemeManager)
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name == BakedStageName) return t.gameObject;
        return null;
    }

    // Entrega o root recém-montado para o assador do editor (que o renomeia
    // para MesaStage). Soltamos a referência para um Clear futuro não apagar
    // o que agora pertence à cena.
    public static GameObject TakeRootForBake()
    {
        GameObject r = root;
        root = null;
        return r;
    }

    // O assador salva a textura gerada como asset e re-assa do zero — o cache
    // não pode continuar apontando para o asset antigo
    public static void ResetCachesForBake() { woodTexture = null; }

    private static GameObject root;
    private static Texture2D woodTexture;

    public static void Clear()
    {
        RestoreLighting();
        GameObject baked = FindBakedStage();
        if (baked != null) baked.SetActive(false);
        if (root != null)
        {
            DecorProps.Kill(root);
            root = null;
        }
    }

    // Centro usado na última montagem — a remontagem da moldura precisa dele
    private static Vector3 lastCenter;

    // Remonta SÓ a moldura (chamado pelo BoardFrameTuning quando os sliders
    // mudam em Play mode — o resto do cenário fica quieto)
    public static void RebuildFrame()
    {
        if (root == null) return; // (com MesaStage assado, root fica null —
                                  // edite a Moldura direto na cena)
        Transform velha = root.transform.Find("Moldura");
        if (velha != null) DecorProps.Kill(velha.gameObject);
        BuildFrame();
    }

    // Remonta SÓ o tampo (chamado pelo TableTopTuning quando os sliders mudam)
    public static void RebuildTableTop()
    {
        // Com MesaStage assado o root fica null — aí remontamos DENTRO do
        // palco da cena mesmo, para os sliders valerem também nesse caso (em
        // Play mode; sair do Play desfaz, e aí é o menu do editor que grava)
        GameObject alvo = root != null ? root : FindBakedStage();
        if (alvo == null) return;

        foreach (string nome in new[] { "Tampo", "TableTop", "TableBody" })
        {
            Transform velho = alvo.transform.Find(nome);
            if (velho != null) DecorProps.Kill(velho.gameObject);
        }

        BoardManager bm = BoardManager.Instance;
        if (bm == null) bm = Object.FindObjectOfType<BoardManager>();
        if (bm != null) lastCenter = bm.transform.position;

        GameObject anterior = root;
        root = alvo;
        BuildTableTop();
        root = anterior;
    }

    // Monta o tampo SOZINHO e devolve solto (sem pai), para o menu do editor
    // trocar só ele dentro de um MesaStage já assado — re-assar o cenário
    // inteiro apagaria tudo o que foi editado à mão.
    public static GameObject BuildTableTopForBake(Vector3 center)
    {
        GameObject anterior = root;
        Vector3 centroAnterior = lastCenter;

        root = new GameObject("TampoTemp");
        lastCenter = center;
        BuildTableTop();

        Transform t = root.transform.Find("Tampo");
        GameObject tampo = t != null ? t.gameObject : null;
        if (tampo != null) tampo.transform.SetParent(null, true);

        DecorProps.Kill(root);
        root = anterior;
        lastCenter = centroAnterior;
        return tampo;
    }

    // Tábuas de verdade lado a lado + base escura por baixo. Cai na laje
    // única de antes se a peça não carregar (o mapa nunca fica sem mesa).
    static void BuildTableTop()
    {
        if (root == null) return;
        Vector3 center = lastCenter;

        GameObject tampo = new GameObject("Tampo");
        tampo.transform.SetParent(root.transform, false);

        TableTopTuning tune = TableTopTuning.Ativa;
        bool usarTabuas = tune == null || tune.usarTabuas;
        float largura = tune != null ? tune.larguraTabua : PlankWidth;
        float espessura = tune != null ? tune.espessuraTabua : PlankThickness;
        float sobra = tune != null ? tune.sobraLateral : PlankOverlap;
        bool alternar = tune == null || tune.alternarSentido;

        float topo = TableTopSurface;
        int postas = 0;

        if (usarTabuas && !string.IsNullOrEmpty(TableTopPiece))
        {
            // Tábuas ao longo do X (o lado comprido), empilhadas no Z.
            // System.Random com seed FIXA: o sorteio de quais vêm giradas é
            // decoração e tem de sair igual nos dois clientes (lockstep).
            int n = Mathf.Max(1, Mathf.RoundToInt(TableTopDepth / Mathf.Max(largura, 0.5f)));
            float passo = TableTopDepth / n;
            System.Random rng = new System.Random(20260902);

            for (int i = 0; i < n; i++)
            {
                float z = -TableTopDepth * 0.5f + passo * (i + 0.5f);
                bool gira = alternar && rng.Next(2) == 0;
                GameObject t = DecorProps.PlaceSceneryPlank(tampo.transform,
                    TableTopPiece, center + new Vector3(0f, topo, z),
                    Vector3.right, TableTopWidth, passo + sobra, espessura, gira);
                if (t != null) postas++;
            }
        }

        if (postas > 0)
        {
            // Base escura logo abaixo das tábuas: tapa qualquer fresta entre
            // elas e faz a espessura da mesa (a câmera deitada vê a borda).
            // Um dedo mais estreita para as tábuas serem a beirada visível.
            GameObject corpo = MakeBox("TableBody",
                center + new Vector3(0f, topo - espessura * 0.5f - 1.5f, 0f),
                new Vector3(TableTopWidth - 0.8f, 3f, TableTopDepth - 0.8f),
                new Color(0.34f, 0.22f, 0.13f), GetWoodTexture(), null, tampo.transform);
            SetTextureTiling(corpo, 5f, 4f);
        }
        else
        {
            // Sem a peça: a laje única de antes, intacta. A textura REPETE —
            // esticada uma vez só sobre 112 unidades ela virava um borrão.
            GameObject slab = MakeBox("TableTop",
                center + new Vector3(0f, topo - 1.5f, 0f),
                new Vector3(TableTopWidth, 3f, TableTopDepth),
                new Color(0.52f, 0.36f, 0.21f), GetWoodTexture(), null, tampo.transform);
            SetTextureTiling(slab, 5f, 4f);
        }
    }

    // Moldura do campo em volta dos tiles. Tabuleiro 7x7 = 45.6 de lado
    // (meia-largura 22.8). Os valores vêm do componente BoardFrameTuning se
    // houver um na cena (calibração ao vivo); senão, dos consts da classe.
    static void BuildFrame()
    {
        if (root == null) return;
        GameObject moldura = new GameObject("Moldura");
        moldura.transform.SetParent(root.transform, false);

        Vector3 center = lastCenter;
        float half = 22.8f;
        float top = -0.15f;

        BoardFrameTuning tune = BoardFrameTuning.Ativa;

        if (FrameWallPiece != null)
        {
            // Peças PRÓPRIAS (Meshy, decor/cenario): mureta de blocos de pedra
            // + canto em "L" com o diamante no vértice. Tudo alinhado pela
            // FACE EXTERNA, num quadrado de lado 2*outerHalf; a conta do seg
            // usa a proporção grossura/comprimento da mureta (0.54/1.9 no
            // arquivo) para a face INTERNA cair rente ao limite das casas.
            int segs = tune != null ? tune.muretasPorLado : FrameSegments;
            float escalaCanto = tune != null ? tune.escalaCanto : FrameCornerScale;
            float empurrao = tune != null ? tune.empurraoCanto : FrameCornerOut;
            float alturaCanto = tune != null ? tune.alturaCanto : FrameCornerHeight;
            float sobeCanto = tune != null ? tune.sobeCanto : 0f;
            bool gCima = tune != null ? tune.girarCima : FrameFlipCima;
            bool gBaixo = tune != null ? tune.girarBaixo : FrameFlipBaixo;
            bool gDir = tune != null ? tune.girarDireita : FrameFlipDireita;
            bool gEsq = tune != null ? tune.girarEsquerda : FrameFlipEsquerda;

            const float proporcao = 0.284f; // grossura ÷ comprimento da mureta
            float seg = 2f * half / (segs + 2f - 2f * proporcao);
            float outerHalf = half + seg * proporcao;

            for (int i = 0; i < segs; i++)
            {
                float t = -outerHalf + seg * (i + 1.5f); // pula o canto (1 braço)

                // Inverter o "along" gira a mureta 180° — a face trabalhada
                // tem que olhar para o lado certo, e cada lado tem o seu
                // sentido (os flips, calibrados em jogo, vivem nos consts
                // FrameFlip* / nas caixinhas do BoardFrameTuning)
                DecorProps.PlaceSceneryWall(moldura.transform, FrameWallPiece,
                    center + new Vector3(t, top, outerHalf),
                    gCima ? Vector3.left : Vector3.right, Vector3.forward, seg);
                DecorProps.PlaceSceneryWall(moldura.transform, FrameWallPiece,
                    center + new Vector3(t, top, -outerHalf),
                    gBaixo ? Vector3.left : Vector3.right, Vector3.back, seg);
                DecorProps.PlaceSceneryWall(moldura.transform, FrameWallPiece,
                    center + new Vector3(outerHalf, top, t),
                    gDir ? Vector3.back : Vector3.forward, Vector3.right, seg);
                DecorProps.PlaceSceneryWall(moldura.transform, FrameWallPiece,
                    center + new Vector3(-outerHalf, top, t),
                    gEsq ? Vector3.back : Vector3.forward, Vector3.left, seg);
            }
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    GameObject canto = DecorProps.PlaceSceneryCorner(moldura.transform,
                        FrameCornerPiece,
                        center + new Vector3(sx * outerHalf, top, sz * outerHalf),
                        sx, sz, seg * escalaCanto, alturaCanto);
                    if (canto != null)
                        canto.transform.position +=
                            new Vector3(sx * empurrao, sobeCanto, sz * empurrao);
                }
        }
        else
        {
            // Mureta de pedra do KayKit (a moldura antiga): segmentos ao longo
            // de cada lado + coluna baixa em cada canto
            float th = 2f;
            float frameHalf = half + th / 2f;          // linha central da mureta
            int segs = 8;                              // segmentos por lado
            float segLen = (2f * frameHalf) / segs;    // ~6.2 → mureta baixa (~1.5)
            for (int i = 0; i < segs; i++)
            {
                float t = -frameHalf + segLen * (i + 0.5f);
                DecorProps.PlaceSpan(moldura.transform, "barrier",
                    center + new Vector3(t, top, frameHalf), Vector3.up, Vector3.right, segLen);
                DecorProps.PlaceSpan(moldura.transform, "barrier",
                    center + new Vector3(t, top, -frameHalf), Vector3.up, Vector3.right, segLen);
                DecorProps.PlaceSpan(moldura.transform, "barrier",
                    center + new Vector3(frameHalf, top, t), Vector3.up, Vector3.forward, segLen);
                DecorProps.PlaceSpan(moldura.transform, "barrier",
                    center + new Vector3(-frameHalf, top, t), Vector3.up, Vector3.forward, segLen);
            }
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    DecorProps.Place(moldura.transform, "barrier_column",
                        center + new Vector3(sx * frameHalf, top, sz * frameHalf),
                        2.4f, Vector3.up, new Vector3(-sx, 0f, -sz));
        }
    }

    public static void Build(int seed)
    {
        Clear();

        // Cenário assado na cena? Ele é o dono do visual — liga e pronto
        GameObject baked = FindBakedStage();
        if (baked != null)
        {
            baked.SetActive(true);

            // Chamas sem material e luzes sem FlickerLight (componente que
            // morreu ao recarregar a cena) são refeitas AQUI, antes da luz, porque
            // este é o único ponto garantido de rodar — ver o aviso em FlameLook.cs
            int chamas = FlameLook.HealAll();
            int luzes = FlickerLight.HealAll();

            PaintBackground();
            ApplyLighting();
            Debug.Log("[Tabletop] Cenário 'MesaStage' (assado na cena, editável) ativado" +
                (chamas + luzes > 0
                    ? " — curadas " + chamas + " chama(s) e " + luzes + " luz(es)."
                    : "."));
            return;
        }

        root = new GameObject("TabletopEnvironment");

        System.Random rng = new System.Random(seed * 7 + 3);

        PaintBackground();
        ApplyLighting();

        // Instance é null fora do Play mode — o assador do editor também
        // chama este Build, então procuramos o objeto da cena de qualquer forma
        BoardManager bm = BoardManager.Instance;
        if (bm == null) bm = Object.FindObjectOfType<BoardManager>();
        Vector3 center = bm != null ? bm.transform.position : Vector3.zero;

        // ── Tampo da mesa ─────────────────────────────────────────────────
        // Em BuildTableTop() para os sliders do TableTopTuning poderem
        // remontá-lo AO VIVO (e o menu do editor trocar só ele num MesaStage
        // já assado, sem perder as edições feitas à mão no resto)
        lastCenter = center;
        BuildTableTop();

        // ── Pernas da mesa + chão da taverna ──────────────────────────────
        // Com a câmera de cima ninguém via, mas com a câmera deitada (estilo
        // LoL) o tampo era uma placa FLUTUANDO no vazio — "tabuleiro criado no
        // ar" (feedback do Carlos). Pernas robustas e um chão de taverna lá
        // embaixo ancoram a mesa no mundo.
        Color legWood = new Color(0.45f, 0.30f, 0.17f);
        foreach (Vector2 corner in new[] {
            new Vector2(-46f, -35f), new Vector2(46f, -35f),
            new Vector2(-46f, 35f), new Vector2(46f, 35f) })
        {
            MakeBox("TableLeg", center + new Vector3(corner.x, -24.15f, corner.y),
                new Vector3(9f, 42f, 9f), legWood, GetWoodTexture());
        }
        // Travessas ligando as pernas (mesa de taverna pesada, não 4 palitos)
        MakeBox("TableBrace", center + new Vector3(0f, -38f, -35f),
            new Vector3(92f, 5f, 4f), legWood, GetWoodTexture());
        MakeBox("TableBrace", center + new Vector3(0f, -38f, 35f),
            new Vector3(92f, 5f, 4f), legWood, GetWoodTexture());

        // Chão: grama e terra a perder de vista. É UM plano de 700 com a textura
        // repetida — não lajota por lajota: a peca do Meshy tem 5 mil vértices e
        // ladrilhar 700×700 sairiam milhares de malhas para um chão que quase
        // sempre está na penumbra.
        GameObject floor = MakeBox("TavernFloor", center + new Vector3(0f, -46.6f, 0f),
            new Vector3(700f, 3f, 700f), GroundTint, GetGroundTexture());
        SetGroundNormal(floor);
        SetTextureTiling(floor, GroundTiles, GroundTiles);

        // ── Moldura do campo ──────────────────────────────────────────────
        // Extraída para BuildFrame() para poder ser remontada AO VIVO pelos
        // sliders do componente BoardFrameTuning (calibração no Play mode).
        float half = 22.8f;
        float top = -0.15f; // Altura do tampo (base das miniaturas e da borda)
        lastCenter = center;
        BuildFrame();

        // ── Árvores em miniatura (KayKit Forest, CC0) ─────────────────────
        // Posições seguras: fora do campo, da coluna da loja (x≈-29.5, |z|<27)
        // e das fileiras das mãos (z≈±29.5, |x|<20). Jitter determinístico.
        Vector2[] treeSpots =
        {
            new Vector2( 30f,  30f), new Vector2(-30f,  30f),
            new Vector2( 30f, -30f), new Vector2(-30f, -30f),
            new Vector2( 38f,  12f), new Vector2( 38f, -10f),
            new Vector2(-38f,  27f), new Vector2(-38f, -27f),
            new Vector2( 26f,  36f), new Vector2(-26f, -36f),
        };
        string[] treeModels =
        {
            "Tree_1_A_Color1", "Tree_2_A_Color1", "Tree_3_A_Color1",
            "Tree_4_A_Color1", "Tree_1_B_Color1", "Tree_2_C_Color1",
        };
        int treeIdx = 0;
        foreach (Vector2 spot in treeSpots)
        {
            float jx = (float)(rng.NextDouble() * 3.0 - 1.5);
            float jz = (float)(rng.NextDouble() * 3.0 - 1.5);
            float s = 0.8f + (float)rng.NextDouble() * 0.5f;
            Vector3 basePos = center + new Vector3(spot.x + jx, top, spot.y + jz);
            DecorProps.PlaceForest(root.transform, treeModels[treeIdx % treeModels.Length],
                basePos, 8.5f * s, Vector3.up, center - basePos);
            treeIdx++;
        }

        // Arbustos e pedras completando o diorama (mesmo atlas da floresta)
        DecorProps.PlaceForest(root.transform, "Bush_1_A_Color1",
            center + new Vector3(27f, top, 32f), 2.2f, Vector3.up, -Vector3.forward);
        DecorProps.PlaceForest(root.transform, "Bush_2_B_Color1",
            center + new Vector3(-27f, top, -33f), 2f, Vector3.up, Vector3.forward);
        DecorProps.PlaceForest(root.transform, "Rock_1_A_Color1",
            center + new Vector3(35f, top, 33f), 2.4f, Vector3.up, -Vector3.forward);
        DecorProps.PlaceForest(root.transform, "Rock_3_E_Color1",
            center + new Vector3(-35f, top, 33f), 1.8f, Vector3.up, -Vector3.forward);
        DecorProps.PlaceForest(root.transform, "Grass_1_A_Color1",
            center + new Vector3(31f, top, -34f), 1.2f, Vector3.up, Vector3.forward);

        // ── Dados (d6) ────────────────────────────────────────────────────
        MakeDie(center + new Vector3(32f, top, 6f), new Color(0.93f, 0.90f, 0.80f), rng);
        MakeDie(center + new Vector3(34.5f, top, -16f), new Color(0.72f, 0.18f, 0.16f), rng);
        MakeDie(center + new Vector3(29f, top, -20f), new Color(0.20f, 0.35f, 0.70f), rng);

        // ── Livros do mestre (pilha) ──────────────────────────────────────
        Vector3 bookPos = center + new Vector3(-33f, top, -32f);
        MakeBox("Book1", bookPos + new Vector3(0f, 0.4f, 0f),
            new Vector3(4.8f, 0.8f, 3.5f), new Color(0.48f, 0.14f, 0.12f), null,
            Quaternion.Euler(0f, 18f, 0f));
        MakeBox("Book2", bookPos + new Vector3(0.3f, 1.1f, 0.2f),
            new Vector3(4.3f, 0.6f, 3.1f), new Color(0.14f, 0.22f, 0.42f), null,
            Quaternion.Euler(0f, -9f, 0f));

        // ── Caneca ────────────────────────────────────────────────────────
        GameObject mug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mug.name = "Mug";
        mug.transform.SetParent(root.transform, false);
        mug.transform.position = center + new Vector3(33f, top + 0.9f, 20f);
        mug.transform.localScale = new Vector3(1.7f, 0.9f, 1.7f);
        FinishDecor(mug, new Color(0.50f, 0.34f, 0.18f), GetWoodTexture());

        // ═══ Props do KayKit Dungeon Pack (CC0) — posições FIXAS, visual puro ═══
        Vector3 upW = Vector3.up;

        // Tochas acesas nos 4 cantos da moldura, com luz de fogo tremulando
        Vector2[] torchSpots =
        {
            new Vector2(-(half + 4f),  half + 4f), new Vector2(half + 4f,  half + 4f),
            new Vector2(-(half + 4f), -(half + 4f)), new Vector2(half + 4f, -(half + 4f)),
        };
        foreach (Vector2 t in torchSpots)
        {
            Vector3 basePos = center + new Vector3(t.x, top, t.y);
            DecorProps.Place(root.transform, "torch_lit", basePos, 7.5f, upW,
                center - basePos);
            // Sem luz aqui, de propósito: as fontes de luz da Mesa são as que o
            // Carlos adiciona e posiciona pelo menu (Cenário: adicionar luz).
            // Luz criada por código vira objeto solto que sobrevive à peça ser
            // apagada — foram as "luzinhas espalhadas" de 04/set/2026.
        }

        // Estandartes nas cores dos jogadores: AZUL no lado do P1 (-z),
        // VERMELHO no lado do P2 (+z) — fora das mãos (|x| > 20)
        DecorProps.Place(root.transform, "banner_shield_blue",
            center + new Vector3(-33f, top, -26f), 9f, upW, new Vector3(1f, 0f, 0.4f));
        DecorProps.Place(root.transform, "banner_patternA_blue",
            center + new Vector3(33f, top, -26f), 9f, upW, new Vector3(-1f, 0f, 0.4f));
        DecorProps.Place(root.transform, "banner_shield_red",
            center + new Vector3(-33f, top, 26f), 9f, upW, new Vector3(1f, 0f, -0.4f));
        DecorProps.Place(root.transform, "banner_patternA_red",
            center + new Vector3(33f, top, 26f), 9f, upW, new Vector3(-1f, 0f, -0.4f));

        // Tesouro perto da coluna da loja (tema "compras"): baú de ouro + moedas
        DecorProps.Place(root.transform, "chest_gold",
            center + new Vector3(-35f, top, 8f), 4.5f, upW, Vector3.right);
        DecorProps.Place(root.transform, "coin_stack_large",
            center + new Vector3(-34.5f, top, 3f), 2f, upW, Vector3.right);
        DecorProps.Place(root.transform, "coin_stack_medium",
            center + new Vector3(-33.5f, top, 12.5f), 1.5f, upW, Vector3.right);

        // Cantinho de taverna (lado direito, perto da caneca e dos livros)
        DecorProps.Place(root.transform, "keg_decorated",
            center + new Vector3(37f, top, 26f), 5f, upW, -Vector3.right);
        DecorProps.Place(root.transform, "barrel_large_decorated",
            center + new Vector3(41f, top, 21f), 5f, upW, -Vector3.right);
        DecorProps.Place(root.transform, "bottle_A_labeled_green",
            center + new Vector3(33f, top, 24f), 2.2f, upW, -Vector3.right);
        DecorProps.Place(root.transform, "plate_stack",
            center + new Vector3(30f, top, 27f), 1.4f, upW, -Vector3.right);

        // Troféu de armas dourado + vela tripla perto dos livros do mestre
        DecorProps.Place(root.transform, "sword_shield_gold",
            center + new Vector3(-37f, top, -14f), 5f, upW, Vector3.right);
        DecorProps.Place(root.transform, "candle_triple",
            center + new Vector3(-29f, top, -35f), 2.6f, upW, Vector3.right);

        // ── Aventureiros do KayKit assistindo da beirada ESQUERDA ─────────
        // (perto do tesouro; miniaturas viradas para o tabuleiro)
        DecorProps.PlaceChar(root.transform, "Knight", "knight_texture",
            center + new Vector3(-38.5f, top, -8f), 6f, upW, Vector3.right);
        DecorProps.PlaceChar(root.transform, "Rogue", "rogue_texture",
            center + new Vector3(-39.5f, top, 0f), 5.6f, upW, Vector3.right);
        DecorProps.PlaceChar(root.transform, "Barbarian", "barbarian_texture",
            center + new Vector3(-38.5f, top, 17f), 5.8f, upW, Vector3.right);

        // ── Os 4 heróis assistindo a partida da beirada direita da mesa ──
        // (miniaturas pequenas fora do campo, viradas para o tabuleiro)
        DecorProps.PlaceHero(root.transform, "Models/personagem_tank",
            center + new Vector3(38f, top, -13f), 6f, upW, -Vector3.right, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_mago",
            center + new Vector3(40f, top, -5f), 5.6f, upW, -Vector3.right, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_healer",
            center + new Vector3(40f, top, 3f), 5.6f, upW, -Vector3.right, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_arqueiro",
            center + new Vector3(38f, top, 11f), 5.6f, upW, -Vector3.right, true);

        // ── "Vagalumes" dourados nas bordas (efeito de vida na mesa) ──────
        System.Random flyRng = new System.Random(seed * 13 + 7);
        for (int i = 0; i < 12; i++)
        {
            // Só nas laterais/cantos (nunca sobre o campo de jogo)
            float side = flyRng.Next(2) == 0 ? -1f : 1f;
            float flyX = side * (26f + (float)flyRng.NextDouble() * 16f);
            float flyZ = ((float)flyRng.NextDouble() * 2f - 1f) * 34f;
            float flyY = 2f + (float)flyRng.NextDouble() * 7f;
            float flyS = 0.10f + (float)flyRng.NextDouble() * 0.15f;

            GameObject fly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fly.name = "Firefly";
            fly.transform.SetParent(root.transform, false);
            fly.transform.position = center + new Vector3(flyX, top + flyY, flyZ);
            fly.transform.localScale = Vector3.one * flyS;
            FinishGlow(fly, new Color(1f, 0.8f, 0.35f, 0.8f));
        }
    }

    // ── Iluminação ───────────────────────────────────────────────────────
    // O que a cena tinha ANTES de a Mesa mexer, para o Clear() devolver tudo.
    // Sem isto, trocar de temática num reinício (Mesa → Floresta) levaria o
    // ambiente escuro da taverna junto e a floresta amanheceria de noite.
    static bool lightingSaved;
    static UnityEngine.Rendering.AmbientMode savedAmbientMode;
    static Color savedSky, savedEquator, savedGround;
    static bool savedFog;
    static Color savedFogColor;
    static FogMode savedFogMode;
    static float savedFogDensity;
    static Light sun;
    static float savedSunIntensity;
    static Color savedSunColor;

    // Remonta SÓ a iluminação (chamado pelo LightingTuning quando os sliders
    // mudam em Play mode)
    public static void RebuildLighting() { ApplyLighting(); }

    static void ApplyLighting()
    {
        LightingTuning tune = LightingTuning.Ativa;

        if (!lightingSaved)
        {
            savedAmbientMode = RenderSettings.ambientMode;
            savedSky = RenderSettings.ambientSkyColor;
            savedEquator = RenderSettings.ambientEquatorColor;
            savedGround = RenderSettings.ambientGroundColor;
            savedFog = RenderSettings.fog;
            savedFogColor = RenderSettings.fogColor;
            savedFogMode = RenderSettings.fogMode;
            savedFogDensity = RenderSettings.fogDensity;
            lightingSaved = true;
        }

        // Ambiente: gradiente (Trilight) escuro e quente no lugar do skybox
        if (tune == null || tune.ambienteEscuro)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = tune != null ? tune.corAlto : AmbientSky;
            RenderSettings.ambientEquatorColor = tune != null ? tune.corMeio : AmbientEquator;
            RenderSettings.ambientGroundColor = tune != null ? tune.corChao : AmbientGround;

            // OBRIGATÓRIO. Mexer nas cores de ambiente em runtime NÃO tem efeito
            // nenhum sem isto: a Unity só recalcula a sonda de ambiente quando
            // mandam. Sem esta linha o ambiente ficava o azul de dia claro de
            // origem enquanto a direcional (que aplica na hora) caía — dava a
            // impressão de "nada mudou, só apagou".
            DynamicGI.UpdateEnvironment();
        }

        // Névoa na cor do fundo: a mesa deixa de terminar num corte seco
        bool comNevoa = tune == null || tune.nevoa;
        RenderSettings.fog = comNevoa;
        if (comNevoa)
        {
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = tune != null ? tune.corNevoa : TavernDark;
            RenderSettings.fogDensity = tune != null ? tune.densidadeNevoa : FogDensity;
        }

        // A direcional deixa de ser sol e vira a luz da sala
        if (sun == null) sun = AchaSol();
        if (sun != null)
        {
            if (savedSunIntensity <= 0f)
            {
                savedSunIntensity = sun.intensity;
                savedSunColor = sun.color;
            }
            sun.intensity = tune != null ? tune.luzPrincipal : SunIntensity;
            sun.color = tune != null ? tune.corLuzPrincipal : SunColor;
        }

        FlickerLight.RebaseAll(tune != null ? tune.tochas : TorchBoost,
                               tune != null ? tune.alcanceTochas : TorchReach);

        Debug.Log("[Tabletop] Iluminação aplicada — sol " +
            (sun != null ? sun.intensity.ToString("0.00") : "(não achei a direcional!)") +
            ", névoa " + (RenderSettings.fog ? RenderSettings.fogDensity.ToString("0.0000") : "off") +
            ", tochas ×" + (tune != null ? tune.tochas : TorchBoost).ToString("0.00") +
            (tune != null ? " (LightingTuning na cena)" : " (padrões do código)"));
    }

    static void RestoreLighting()
    {
        if (!lightingSaved) return;

        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientSkyColor = savedSky;
        RenderSettings.ambientEquatorColor = savedEquator;
        RenderSettings.ambientGroundColor = savedGround;
        DynamicGI.UpdateEnvironment();   // mesma regra na volta
        RenderSettings.fog = savedFog;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogMode = savedFogMode;
        RenderSettings.fogDensity = savedFogDensity;

        if (sun != null && savedSunIntensity > 0f)
        {
            sun.intensity = savedSunIntensity;
            sun.color = savedSunColor;
        }
        savedSunIntensity = 0f;

        FlickerLight.RebaseAll(1f, 1f);
        lightingSaved = false;
    }

    static Light AchaSol()
    {
        foreach (Light l in Object.FindObjectsByType<Light>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != null && l.type == LightType.Directional) return l;
        return null;
    }

    // Fundo: taverna escura e quente (contraste com o azul do espaço). É a
    // única coisa que continua por código mesmo com o MesaStage assado —
    // fundo de câmera não dá para guardar num GameObject da cena.
    static void PaintBackground()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = TavernDark;
        }
    }

    // Material Unlit brilhante (vagalumes) — não depende de luz
    static void FinishGlow(GameObject go, Color color)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) DecorProps.Kill(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return;

        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        r.material = mat;
    }

    // ── Miniaturas ────────────────────────────────────────────────────────

    static void MakeDie(Vector3 basePos, Color color, System.Random rng)
    {
        GameObject die = GameObject.CreatePrimitive(PrimitiveType.Cube);
        die.name = "Die";
        die.transform.SetParent(root.transform, false);
        die.transform.position = basePos + new Vector3(0f, 0.8f, 0f);
        die.transform.localScale = Vector3.one * 1.6f;
        die.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 90.0), 0f);
        FinishDecor(die, color, null);
    }

    static GameObject MakeBox(string name, Vector3 pos, Vector3 scale, Color color,
        Texture2D tex, Quaternion? rot = null, Transform parent = null)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent != null ? parent : root.transform, false);
        box.transform.position = pos;
        box.transform.localScale = scale;
        if (rot.HasValue) box.transform.rotation = rot.Value;
        FinishDecor(box, color, tex);
        return box;
    }

    // Remove o collider (decoração não pode roubar cliques do tabuleiro/cartas)
    // e aplica material URP com cor/textura
    static void FinishDecor(GameObject go, Color color, Texture2D tex)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) DecorProps.Kill(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

        // Mesma escolha de shader dos tiles (BoardManager): Lit com cor base
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return;

        Material mat = new Material(shader);
        mat.color = color;
        if (tex != null)
        {
            mat.mainTexture = tex;
            mat.SetTexture("_BaseMap", tex);
        }
        r.material = mat;
    }

    // ── Chão da taverna ──────────────────────────────────────

    // Calçada de pedra irregular, do Medieval Village MegaKit (a mesma família
    // de material das construções). Diferente das peças do Meshy, esta já NASCE
    // sem emenda — é uma textura de superfície feita para repetir, não um atlas
    // de UV de uma malha — então não precisa do tratamento "-liso" que a grama
    // precisou. (A grama continua instalada como chao-grama-liso, caso volte.)
    public const string GroundSlug = "chao-pedra";

    // 700 ÷ 50 = 14 por repetição — com ~8 pedras na largura da textura, cada
    // pedra fica com ~1.7 (uma casa tem 6). Mais repetições = pedra miúda;
    // menos = pedra grande demais e o desenho começa a se repetir à vista.
    public const float GroundTiles = 50f;

    // Perto do neutro de propósito: a cor vem da textura, e um pouco abaixo de
    // 1 para o chão não ficar mais claro que o tampo da mesa na penumbra. Quem
    // pinta a pedra é a luz alaranjada das tochas.
    public static readonly Color GroundTint = new Color(0.80f, 0.77f, 0.72f);

    static Texture2D groundTex;
    static Texture2D groundNrm;

    static Texture2D GetGroundTexture()
    {
        if (groundTex == null)
        {
            groundTex = Resources.Load<Texture2D>("decor/cenario/" + GroundSlug + "_tex");
            Repetivel(groundTex);
        }
        // Sem a peca instalada, o chão volta às tábuas em vez de ficar branco
        return groundTex != null ? groundTex : GetWoodTexture();
    }

    // Trava contra o bug de 04/set/2026: as texturas do chão foram instaladas com
    // .meta clonado do piso-tabuleiro, que é uma LAJOTA — e lajota vem em Clamp
    // (usa o próprio atlas em 0..1, onde Clamp até ajuda). Numa textura repetida
    // 50× num plano de 700, Clamp NÃO repete: estica o pixel da borda pela área
    // inteira e o chão vira UMA COR CHAPADA, sem desenho nenhum. Levou duas
    // trocas de textura até alguém perceber que o problema nunca foi a arte.
    static void Repetivel(Texture2D tex)
    {
        if (tex != null && tex.wrapMode != TextureWrapMode.Repeat)
        {
            Debug.LogWarning("[Tabletop] '" + tex.name + "' estava em " + tex.wrapMode +
                             "; chão precisa de Repeat. Corrigido em runtime — " +
                             "arrume o Wrap Mode no .meta para valer no build.");
            tex.wrapMode = TextureWrapMode.Repeat;
        }
    }

    // Relevo do chão: é o normal map que faz a grama pegar a luz das tochas em
    // vez de virar um adesivo chapado. Vai separado porque MakeBox/FinishDecor
    // só sabem de cor + textura.
    static void SetGroundNormal(GameObject go)
    {
        if (groundNrm == null)
        {
            groundNrm = Resources.Load<Texture2D>("decor/cenario/" + GroundSlug + "_normal");
            Repetivel(groundNrm);
        }
        if (groundNrm == null) return;

        Renderer r = go != null ? go.GetComponent<Renderer>() : null;
        if (r == null || r.sharedMaterial == null) return;
        if (!r.sharedMaterial.HasProperty("_BumpMap")) return;

        r.sharedMaterial.SetTexture("_BumpMap", groundNrm);
        r.sharedMaterial.SetTextureScale("_BumpMap", new Vector2(GroundTiles, GroundTiles));
        r.sharedMaterial.EnableKeyword("_NORMALMAP");
        // Terra é fosca: o 0.5 padrão do URP Lit deixa o chão encerado
        if (r.sharedMaterial.HasProperty("_Smoothness"))
            r.sharedMaterial.SetFloat("_Smoothness", 0.10f);
    }

    // Textura de madeira procedural em TÁBUAS (512px, com mipmaps): cada faixa
    // horizontal é uma tábua com tom próprio, veios alongados e emendas escuras
    // (horizontais entre tábuas + verticais desencontradas). Clara o bastante
    // para a cor do material dominar. Feita para repetir (wrap = Repeat).
    static Texture2D GetWoodTexture()
    {
        if (woodTexture != null) return woodTexture;

        const int size = 512;
        const int plankH = 64;               // 8 tábuas por repetição
        woodTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        woodTexture.wrapMode = TextureWrapMode.Repeat;
        woodTexture.filterMode = FilterMode.Trilinear;

        // Tom e deslocamento de cada tábua — fixos (visual puro, sempre igual)
        System.Random rng = new System.Random(4217);
        int planks = size / plankH;
        float[] tone = new float[planks];
        int[] seamX = new int[planks];
        float[] grainOff = new float[planks];
        for (int p = 0; p < planks; p++)
        {
            tone[p] = 0.88f + 0.12f * (float)rng.NextDouble();
            seamX[p] = rng.Next(size);       // emenda vertical desencontrada
            grainOff[p] = (float)rng.NextDouble() * 100f;
        }

        for (int y = 0; y < size; y++)
        {
            int p = y / plankH;
            int yIn = y % plankH;
            for (int x = 0; x < size; x++)
            {
                // Veios: ruído bem esticado no X + granulado fino, por tábua
                float n1 = Mathf.PerlinNoise(x * 0.013f + grainOff[p], y * 0.16f);
                float n2 = Mathf.PerlinNoise(x * 0.11f + 51f + grainOff[p], y * 0.07f + 17f);
                float v = tone[p] * (0.74f + 0.19f * n1 + 0.07f * n2);

                // Emenda horizontal entre tábuas (2px escuros + 1px de brilho)
                if (yIn <= 1) v *= 0.55f;
                else if (yIn == 2) v = Mathf.Min(1f, v * 1.12f);
                else if (yIn == plankH - 1) v *= 0.72f;

                // Emenda vertical da tábua (topo de tábua desencontrado)
                int dx = Mathf.Abs(x - seamX[p]);
                dx = Mathf.Min(dx, size - dx); // distância com wrap
                if (dx <= 1) v *= 0.60f;

                woodTexture.SetPixel(x, y, new Color(v, v * 0.95f, v * 0.88f, 1f));
            }
        }

        woodTexture.Apply(true);
        return woodTexture;
    }

    // Repetição da textura no material (URP Lit usa _BaseMap)
    static void SetTextureTiling(GameObject go, float tilesX, float tilesY)
    {
        // sharedMaterial, não .material: cada peça já tem instância própria
        // (FinishDecor cria uma por objeto), e LER .material fora do Play mode
        // (o assar do MesaStage) duplicaria o material com aviso de vazamento
        Renderer r = go != null ? go.GetComponent<Renderer>() : null;
        if (r == null || r.sharedMaterial == null) return;
        r.sharedMaterial.mainTextureScale = new Vector2(tilesX, tilesY);
        if (r.sharedMaterial.HasProperty("_BaseMap"))
            r.sharedMaterial.SetTextureScale("_BaseMap", new Vector2(tilesX, tilesY));
    }
}
