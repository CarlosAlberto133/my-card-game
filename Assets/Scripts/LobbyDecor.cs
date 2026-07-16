using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Cenário do LOBBY na temática de mesa de RPG (taverna), 100% por código:
// um tampo de madeira inclinado preenchendo a parte de baixo do quadro, com
// miniaturas (árvores, dados, livros, caneca), cartas decorativas, uma vela
// acesa com luz quente e "vagalumes" dourados flutuando. Tudo montado no
// espaço da CÂMERA (mesma técnica do SpaceBackground.EnsureFacingCamera),
// então funciona com qualquer orientação de câmera do lobby.
// Puramente visual: nenhum collider (não rouba cliques da UI).
public static class LobbyDecor
{
    static GameObject root;
    static Texture2D woodTexture;

    public static void Clear()
    {
        if (root != null)
        {
            Object.Destroy(root);
            root = null;
        }
    }

    public static void Build()
    {
        if (root != null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();

        root = new GameObject("LobbyDecor");

        // Fundo: taverna escura e quente (mesma família de cor do mapa Mesa de RPG)
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.038f, 0.028f);
        }

        // ── Moldura visível a 30 de distância (mesma conta do SpaceBackground) ──
        float dist = 30f;
        float aspect = (cam != null && cam.aspect > 0.01f) ? cam.aspect : 16f / 9f;
        float frameH;
        if (cam != null && cam.orthographic)
        {
            frameH = cam.orthographicSize * 2f;
        }
        else
        {
            float fov = (cam != null) ? cam.fieldOfView : 60f;
            frameH = 2f * dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }
        float frameW = frameH * aspect;

        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 f = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;

        // ── Tampo da mesa ────────────────────────────────────────────────
        // Superfície inclinada ~40° em direção à câmera (bem visível, como uma
        // mesa vista de pé). "tUp" é a normal; "tFwd" é a direção que afunda
        // para o fundo do quadro; "tRight" acompanha a horizontal da tela.
        Vector3 tUp = (up * 0.75f - f * 0.66f).normalized;
        Vector3 tFwd = Vector3.ProjectOnPlane(f, tUp).normalized;
        Vector3 tRight = Vector3.Cross(tUp, tFwd).normalized;
        Quaternion tRot = Quaternion.LookRotation(tFwd, tUp);

        // Centro do tampo: parte de baixo do quadro, empurrado para o fundo
        Vector3 slabCenter = camPos + f * dist - up * (frameH * 0.34f) + tFwd * 6f;

        MakeBox("TableTop", slabCenter, new Vector3(frameW * 1.9f, 1.2f, 46f), tRot,
            new Color(0.42f, 0.28f, 0.16f), GetWoodTexture());

        // Ponto de apoio das miniaturas: (x = lateral, z = profundidade)
        System.Func<float, float, float, Vector3> onTable = (x, z, h) =>
            slabCenter + tRight * x + tFwd * z + tUp * (0.6f + h);

        System.Random rng = new System.Random(1337);

        // ── Cartas decorativas sobre a mesa (verso azul com borda dourada) ──
        MakeCard(onTable(-frameW * 0.30f, 2f, 0f), tRot * Quaternion.Euler(0f, -14f, 0f), tUp);
        MakeCard(onTable(-frameW * 0.24f, 5.5f, 0.06f), tRot * Quaternion.Euler(0f, 11f, 0f), tUp);
        MakeCard(onTable(frameW * 0.27f, 3f, 0f), tRot * Quaternion.Euler(0f, 24f, 0f), tUp);

        // ── Miniaturas de diorama ────────────────────────────────────────
        // Árvores do KayKit Forest nas laterais/fundo (fora dos botões)
        DecorProps.PlaceForest(root.transform, "Tree_1_A_Color1",
            onTable(-frameW * 0.42f, 10f, 0f), 11f, tUp, -tFwd);
        DecorProps.PlaceForest(root.transform, "Tree_2_A_Color1",
            onTable(-frameW * 0.33f, 14f, 0f), 8.5f, tUp, -tFwd);
        DecorProps.PlaceForest(root.transform, "Tree_3_A_Color1",
            onTable(frameW * 0.40f, 12f, 0f), 12f, tUp, -tFwd);
        DecorProps.PlaceForest(root.transform, "Tree_4_A_Color1",
            onTable(frameW * 0.31f, 15f, 0f), 8f, tUp, -tFwd);
        DecorProps.PlaceForest(root.transform, "Tree_1_B_Color1",
            onTable(frameW * 0.45f, 6f, 0f), 7f, tUp, -tFwd);

        // Arbusto e pedra completando o cantinho
        DecorProps.PlaceForest(root.transform, "Bush_1_A_Color1",
            onTable(-frameW * 0.38f, 5f, 0f), 2f, tUp, -tFwd);
        DecorProps.PlaceForest(root.transform, "Rock_2_C_Color1",
            onTable(frameW * 0.44f, 10f, 0f), 1.8f, tUp, -tFwd);

        // Dados coloridos
        MakeDie(onTable(-frameW * 0.36f, 4f, 0.8f), tRot, new Color(0.72f, 0.18f, 0.16f), rng);
        MakeDie(onTable(-frameW * 0.32f, 2.2f, 0.7f), tRot, new Color(0.93f, 0.90f, 0.80f), rng);
        MakeDie(onTable(frameW * 0.34f, 7.5f, 0.8f), tRot, new Color(0.20f, 0.35f, 0.70f), rng);

        // Livros do mestre (pilha à direita)
        MakeBox("Book1", onTable(frameW * 0.40f, 2f, 0.5f), new Vector3(5.2f, 1.0f, 3.8f),
            tRot * Quaternion.Euler(0f, 18f, 0f), new Color(0.48f, 0.14f, 0.12f), null);
        MakeBox("Book2", onTable(frameW * 0.40f, 2.2f, 1.4f), new Vector3(4.6f, 0.8f, 3.3f),
            tRot * Quaternion.Euler(0f, -9f, 0f), new Color(0.14f, 0.22f, 0.42f), null);

        // Caneca à esquerda
        GameObject mug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mug.name = "Mug";
        mug.transform.SetParent(root.transform, false);
        mug.transform.position = onTable(-frameW * 0.44f, 7f, 1.1f);
        mug.transform.rotation = tRot;
        mug.transform.localScale = new Vector3(2.0f, 1.1f, 2.0f);
        FinishDecor(mug, new Color(0.50f, 0.34f, 0.18f), GetWoodTexture());

        // ── Props do KayKit Dungeon Pack (CC0) sobre a mesa ──────────────
        // Vela tripla acesa (fonte da luz quente local)
        Vector3 candleBase = onTable(frameW * 0.22f, 9f, 0f);
        DecorProps.Place(root.transform, "candle_triple", candleBase, 3.2f, tUp, -tFwd);

        // Baú de ouro com moedas (tesouro da taverna, lado direito)
        DecorProps.Place(root.transform, "chest_gold",
            onTable(frameW * 0.33f, 5f, 0f), 3.6f, tUp, -tFwd);
        DecorProps.Place(root.transform, "coin_stack_medium",
            onTable(frameW * 0.28f, 3.5f, 0f), 1.4f, tUp, -tFwd);

        // Barril de cerveja à esquerda (perto da caneca)
        DecorProps.Place(root.transform, "keg_decorated",
            onTable(-frameW * 0.47f, 10f, 0f), 4.5f, tUp, -tFwd);

        // Estandartes azul e vermelho ao fundo (as cores dos dois jogadores)
        DecorProps.Place(root.transform, "banner_shield_blue",
            onTable(-frameW * 0.10f, 16f, 0f), 8f, tUp, -tFwd);
        DecorProps.Place(root.transform, "banner_shield_red",
            onTable(frameW * 0.10f, 16f, 0f), 8f, tUp, -tFwd);

        // Fregueses da taverna (aventureiros do KayKit) entre os heróis
        DecorProps.PlaceChar(root.transform, "Barbarian", "barbarian_texture",
            onTable(-frameW * 0.31f, 11f, 0f), 8f, tUp, -tFwd);
        DecorProps.PlaceChar(root.transform, "Rogue_Hooded", "rogue_texture",
            onTable(frameW * 0.31f, 11f, 0f), 8f, tUp, -tFwd);

        // ── Luzes ────────────────────────────────────────────────────────
        // Luz quente principal (como a lanterna da taverna sobre a mesa)
        GameObject keyGO = new GameObject("TavernLight");
        keyGO.transform.SetParent(root.transform, false);
        keyGO.transform.position = slabCenter + tUp * 16f - tFwd * 6f;
        Light key = keyGO.AddComponent<Light>();
        key.type = LightType.Point;
        key.color = new Color(1f, 0.78f, 0.52f);
        key.intensity = 2.4f;
        key.range = 110f;

        // Luzinha da vela tremulando (fogo vivo)
        FlickerLight.Attach(root.transform, candleBase + tUp * 4.5f,
            new Color(1f, 0.6f, 0.2f), 1.7f, 26f);

        // ── Os 4 heróis do jogo em pé na mesa (modelos reais das cartas) ──
        // Tank e Arqueiro na frente (laterais), Mago e Healer atrás — todos
        // de frente para a câmera, animados em idle quando o rig existe
        DecorProps.PlaceHero(root.transform, "Models/personagem_tank",
            onTable(-frameW * 0.40f, 7f, 0f), 9.5f, tUp, -tFwd, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_mago",
            onTable(-frameW * 0.22f, 12f, 0f), 9f, tUp, -tFwd, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_healer",
            onTable(frameW * 0.22f, 12f, 0f), 9f, tUp, -tFwd, true);
        DecorProps.PlaceHero(root.transform, "Models/personagem_arqueiro",
            onTable(frameW * 0.40f, 7f, 0f), 9f, tUp, -tFwd, true);

        // ── "Vagalumes" dourados flutuando acima da mesa ─────────────────
        for (int i = 0; i < 14; i++)
        {
            float x = ((float)rng.NextDouble() * 2f - 1f) * frameW * 0.45f;
            float z = (float)rng.NextDouble() * 16f;
            float h = 2.5f + (float)rng.NextDouble() * (frameH * 0.45f);
            float s = 0.10f + (float)rng.NextDouble() * 0.16f;

            GameObject fly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fly.name = "Firefly";
            fly.transform.SetParent(root.transform, false);
            fly.transform.position = onTable(x, z, h);
            fly.transform.localScale = Vector3.one * s;
            float warm = 0.55f + (float)rng.NextDouble() * 0.4f;
            FinishGlow(fly, new Color(1f, 0.8f, 0.35f, warm));
        }
    }

    // ── Peças ─────────────────────────────────────────────────────────────

    // Carta de baralho deitada: borda dourada + verso azul-escuro
    static void MakeCard(Vector3 pos, Quaternion rot, Vector3 tUp)
    {
        MakeBox("CardBorder", pos, new Vector3(4.6f, 0.08f, 6.4f), rot,
            new Color(0.96f, 0.77f, 0.32f), null);
        MakeBox("CardBack", pos + tUp * 0.06f, new Vector3(4.1f, 0.08f, 5.9f), rot,
            new Color(0.07f, 0.10f, 0.22f), null);
    }

    static void MakeDie(Vector3 pos, Quaternion baseRot, Color color, System.Random rng)
    {
        GameObject die = GameObject.CreatePrimitive(PrimitiveType.Cube);
        die.name = "Die";
        die.transform.SetParent(root.transform, false);
        die.transform.position = pos;
        die.transform.rotation = baseRot * Quaternion.Euler(0f, (float)(rng.NextDouble() * 90.0), 0f);
        die.transform.localScale = Vector3.one * 1.7f;
        FinishDecor(die, color, null);
    }

    static GameObject MakeBox(string name, Vector3 pos, Vector3 scale, Quaternion rot,
        Color color, Texture2D tex)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root.transform, false);
        box.transform.position = pos;
        box.transform.rotation = rot;
        box.transform.localScale = scale;
        FinishDecor(box, color, tex);
        return box;
    }

    // Material URP Lit + remove o collider (decoração não rouba cliques)
    static void FinishDecor(GameObject go, Color color, Texture2D tex)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        Renderer r = go.GetComponent<Renderer>();
        if (r == null) return;

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

    // Material Unlit "brilhante" (chama da vela, vagalumes) — não depende de luz
    static void FinishGlow(GameObject go, Color color)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

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

    // Mesma madeira procedural do TabletopEnvironment (veios de Perlin)
    static Texture2D GetWoodTexture()
    {
        if (woodTexture != null) return woodTexture;

        const int size = 128;
        woodTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.02f, y * 0.35f);
                float n2 = Mathf.PerlinNoise(x * 0.15f + 51f, y * 0.05f + 17f);
                float v = 0.70f + 0.22f * n1 + 0.08f * n2;
                woodTexture.SetPixel(x, y, new Color(v, v * 0.96f, v * 0.90f, 1f));
            }
        }

        woodTexture.Apply();
        return woodTexture;
    }
}

// Toca o clip de idle em loop num herói do lobby (Playables, sem
// AnimatorController — mesma técnica do FigureRiggedAnimator do jogo).
// O grafo precisa ser destruído junto com o objeto para não vazar.
public class LobbyHeroIdle : MonoBehaviour
{
    PlayableGraph graph;

    public void Play(Animator animator, AnimationClip clip)
    {
        graph = PlayableGraph.Create("LobbyHeroIdle");
        var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "idle", animator);
        var playable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip);
        output.SetSourcePlayable(playable);
        graph.Play();
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
