using UnityEngine;

// Cenário "mesa de RPG" (temática medieval), 100% por código: uma grande mesa
// de madeira sob o tabuleiro (com veios procedurais), moldura em volta do campo
// e miniaturas de diorama espalhadas — árvores, dados, livros e uma caneca —
// como numa sessão de RPG de mesa. Puramente visual: nenhum collider (não rouba
// cliques) e nenhum uso de UnityEngine.Random (lockstep intocado — as posições
// usam System.Random com a seed da partida, idênticas nos dois clientes).
public static class TabletopEnvironment
{
    private static GameObject root;
    private static Texture2D woodTexture;

    public static void Clear()
    {
        if (root != null)
        {
            Object.Destroy(root);
            root = null;
        }
    }

    public static void Build(int seed)
    {
        Clear();
        root = new GameObject("TabletopEnvironment");

        System.Random rng = new System.Random(seed * 7 + 3);

        // Fundo: taverna escura e quente (contraste com o azul do espaço)
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.080f, 0.055f, 0.040f);
        }

        Vector3 center = BoardManager.Instance != null
            ? BoardManager.Instance.transform.position
            : Vector3.zero;

        // ── Tampo da mesa ─────────────────────────────────────────────────
        // Grande o bastante para loja (x -27.5) e mãos (z ±29.5) ficarem sobre
        // ela. Topo em y = -0.15 (logo abaixo dos tiles, que estão em y = 0).
        GameObject slab = MakeBox("TableTop", center + new Vector3(0f, -1.65f, 0f),
            new Vector3(112f, 3f, 90f), new Color(0.42f, 0.28f, 0.16f), GetWoodTexture());

        // ── Moldura do campo (borda de madeira escura em volta dos tiles) ──
        // Tabuleiro 7x7 = 45.6 de lado (meia-largura 22.8). Moldura de 2 de
        // espessura colada na borda, topo em y = 0.15.
        Color frameColor = new Color(0.28f, 0.17f, 0.09f);
        float half = 22.8f, th = 2f, fh = 0.6f, fy = -0.15f;
        MakeBox("FrameN", center + new Vector3(0f, fy, half + th / 2f),
            new Vector3(2f * (half + th), fh, th), frameColor, GetWoodTexture());
        MakeBox("FrameS", center + new Vector3(0f, fy, -(half + th / 2f)),
            new Vector3(2f * (half + th), fh, th), frameColor, GetWoodTexture());
        MakeBox("FrameE", center + new Vector3(half + th / 2f, fy, 0f),
            new Vector3(th, fh, 2f * half), frameColor, GetWoodTexture());
        MakeBox("FrameW", center + new Vector3(-(half + th / 2f), fy, 0f),
            new Vector3(th, fh, 2f * half), frameColor, GetWoodTexture());

        float top = -0.15f; // Altura do tampo (base das miniaturas)

        // ── Árvores em miniatura (KayKit Forest, CC0) ─────────────────────
        // Posições seguras: fora do campo, da coluna da loja (x≈-27.5, |z|<22)
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
            FlickerLight.Attach(root.transform, basePos + upW * 7f,
                new Color(1f, 0.62f, 0.25f), 2.3f, 24f);
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
        FlickerLight.Attach(root.transform, center + new Vector3(-29f, top + 3f, -35f),
            new Color(1f, 0.6f, 0.2f), 1.4f, 18f);

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

    // Material Unlit brilhante (vagalumes) — não depende de luz
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
        Texture2D tex, Quaternion? rot = null)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root.transform, false);
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
        if (col != null) Object.Destroy(col);

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

    // Textura de madeira procedural: veios horizontais (Perlin alongado no X),
    // clara o bastante para a cor do material dominar
    static Texture2D GetWoodTexture()
    {
        if (woodTexture != null) return woodTexture;

        const int size = 128;
        woodTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Veios: ruído esticado no X + ondulação fina
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
