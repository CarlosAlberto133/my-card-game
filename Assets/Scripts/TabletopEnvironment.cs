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

        // ── Árvores em miniatura (diorama) ────────────────────────────────
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
        foreach (Vector2 spot in treeSpots)
        {
            float jx = (float)(rng.NextDouble() * 3.0 - 1.5);
            float jz = (float)(rng.NextDouble() * 3.0 - 1.5);
            float s = 0.8f + (float)rng.NextDouble() * 0.5f;
            MakeTree(center + new Vector3(spot.x + jx, top, spot.y + jz), s, rng);
        }

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
    }

    // ── Miniaturas ────────────────────────────────────────────────────────

    static void MakeTree(Vector3 basePos, float scale, System.Random rng)
    {
        // Tronco
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "TreeTrunk";
        trunk.transform.SetParent(root.transform, false);
        trunk.transform.position = basePos + new Vector3(0f, 1.1f * scale, 0f);
        trunk.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f) * scale;
        FinishDecor(trunk, new Color(0.36f, 0.24f, 0.13f), null);

        // Copa: duas esferas empilhadas (tom de verde levemente variado)
        float g = 0.32f + (float)rng.NextDouble() * 0.10f;
        Color leaf = new Color(0.13f, g, 0.15f);

        GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lower.name = "TreeLeaves1";
        lower.transform.SetParent(root.transform, false);
        lower.transform.position = basePos + new Vector3(0f, 2.9f * scale, 0f);
        lower.transform.localScale = Vector3.one * 2.6f * scale;
        FinishDecor(lower, leaf, null);

        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        upper.name = "TreeLeaves2";
        upper.transform.SetParent(root.transform, false);
        upper.transform.position = basePos + new Vector3(0f, 4.3f * scale, 0f);
        upper.transform.localScale = Vector3.one * 1.8f * scale;
        FinishDecor(upper, new Color(leaf.r, leaf.g + 0.05f, leaf.b), null);
    }

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
