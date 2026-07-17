using UnityEngine;

// Cenário "Floresta" (tema 2): o campo de batalha é uma CLAREIRA — chão de
// grama a perder de vista, paredão de árvores do KayKit Forest em volta,
// tochas nos cantos do campo, estandartes nas cores dos jogadores e vagalumes.
// Puramente visual: nenhum collider (não rouba cliques) e nenhum uso de
// UnityEngine.Random (lockstep intocado — as posições usam System.Random com
// a seed da partida, idênticas nos dois clientes).
public static class ForestEnvironment
{
    private static GameObject root;
    private static Texture2D grassTexture;

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
        root = new GameObject("ForestEnvironment");

        System.Random rng = new System.Random(seed * 17 + 5);

        // Fundo: verde-noite profundo (o paredão de árvores fecha o horizonte)
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.025f, 0.050f, 0.038f);
        }

        Vector3 center = BoardManager.Instance != null
            ? BoardManager.Instance.transform.position
            : Vector3.zero;

        // ── Chão de grama ─────────────────────────────────────────────────
        // Grande o bastante para loja (x -27.5) e mãos (z ±29.5). Topo em
        // y = -0.15 (logo abaixo das casas de terra, que estão em y = 0).
        // Textura repetida = nítida (esticada uma vez só viraria borrão).
        // (verde um tom mais escuro para o campo de terra SALTAR do fundo)
        GameObject ground = MakeBox("GrassGround", center + new Vector3(0f, -1.65f, 0f),
            new Vector3(150f, 3f, 130f), new Color(0.44f, 0.60f, 0.34f), GetGrassTexture());
        SetTextureTiling(ground, 7f, 6f);

        float top = -0.15f;   // Altura do chão (base de tudo)
        float half = 22.8f;   // Meia-largura do campo 7x7

        // ── Cordilheira em miniatura demarcando o CAMPO ───────────────────
        // Pedras lado a lado em volta do 7x7 (pedido do Carlos: o campo se
        // perdia na grama — a moldura de rocha mostra exatamente onde as
        // cartas podem ser posicionadas). Baixas nos lados (não tampam a
        // fileira do fundo), mais altas nos cantos.
        string[] borderRocks =
        {
            "Rock_1_A_Color1", "Rock_2_C_Color1", "Rock_3_E_Color1",
            "Rock_1_C_Color1", "Rock_3_B_Color1",
        };
        float ring = half + 1.6f;   // colada na borda do campo
        int rockSegs = 10;          // pedras por lado
        float rockStep = (2f * ring) / rockSegs;
        int rockIdx = 0;
        for (int i = 0; i < rockSegs; i++)
        {
            float t = -ring + rockStep * (i + 0.5f);
            Vector2[] sides =
            {
                new Vector2(t, ring), new Vector2(t, -ring),
                new Vector2(ring, t), new Vector2(-ring, t),
            };
            foreach (Vector2 p in sides)
            {
                Vector3 basePos = Jitter(center, p, top, 0.5f, rng);
                float h = 1.8f + (float)rng.NextDouble() * 1.2f;
                DecorProps.PlaceForest(root.transform, borderRocks[rockIdx++ % borderRocks.Length],
                    basePos, h, Vector3.up, center - basePos);
            }
        }
        // Cantos: pedras maiores amarram a moldura
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 basePos = center + new Vector3(sx * ring, top, sz * ring);
                DecorProps.PlaceForest(root.transform, borderRocks[rockIdx++ % borderRocks.Length],
                    basePos, 3.4f + (float)rng.NextDouble() * 0.8f, Vector3.up, center - basePos);
            }

        // ── Tochas nos 4 cantos do campo (fora da cordilheira) ────────────
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 basePos = center + new Vector3(sx * (half + 5f), top, sz * (half + 5f));
                DecorProps.Place(root.transform, "torch_lit", basePos, 7.5f, Vector3.up,
                    center - basePos);
                FlickerLight.Attach(root.transform, basePos + Vector3.up * 7f,
                    new Color(1f, 0.62f, 0.25f), 2.3f, 24f);
            }

        // ── Estandartes nas cores dos jogadores (AZUL P1 -z, VERMELHO P2 +z) ──
        DecorProps.Place(root.transform, "banner_shield_blue",
            center + new Vector3(-30f, top, -26f), 9f, Vector3.up, new Vector3(1f, 0f, 0.4f));
        DecorProps.Place(root.transform, "banner_patternA_blue",
            center + new Vector3(30f, top, -26f), 9f, Vector3.up, new Vector3(-1f, 0f, 0.4f));
        DecorProps.Place(root.transform, "banner_shield_red",
            center + new Vector3(-30f, top, 26f), 9f, Vector3.up, new Vector3(1f, 0f, -0.4f));
        DecorProps.Place(root.transform, "banner_patternA_red",
            center + new Vector3(30f, top, 26f), 9f, Vector3.up, new Vector3(-1f, 0f, -0.4f));

        // Tesouro perto da coluna da loja (o mesmo "cartaz" de compras da Mesa)
        DecorProps.Place(root.transform, "chest_gold",
            center + new Vector3(-34f, top, 8f), 4.5f, Vector3.up, Vector3.right);
        DecorProps.Place(root.transform, "coin_stack_medium",
            center + new Vector3(-33f, top, 12.5f), 1.5f, Vector3.up, Vector3.right);

        // ── Paredão de árvores ────────────────────────────────────────────
        // ZONAS PROIBIDAS (aprendido na prática — árvore na frente da câmera
        // cobria a mão e a loja): mãos em z≈±29.5 com |x|<20 → nada de árvore
        // com |x|<26 atrás delas; loja em x≈-27.5 com |z|<22 → nada de árvore
        // em x<-30 com |z|<24. Anel PRÓXIMO: só cantos e lateral direita.
        Vector2[] nearSpots =
        {
            new Vector2( 30f,  31f), new Vector2(-30f,  31f),
            new Vector2( 30f, -31f), new Vector2(-30f, -31f),
            new Vector2( 37f,  13f), new Vector2( 37f, -11f),
            new Vector2(-38f,  26f), new Vector2(-38f, -26f),
            new Vector2( 40f,   2f), new Vector2( 36f, -22f),
            new Vector2( 35f,  22f), new Vector2(-33f, -32f),
        };
        // Anel de FUNDO: SÓ nas laterais e cantos (nunca no corredor central
        // atrás das mãos) e mais baixo que antes
        Vector2[] farSpots =
        {
            new Vector2( 46f,  30f), new Vector2( 46f, -28f), new Vector2( 49f,   8f),
            new Vector2(-48f,  21f), new Vector2(-47f, -30f), new Vector2(-49f,  -1f),
            new Vector2( 45f, -14f), new Vector2(-46f,  32f),
            new Vector2( 28f,  44f), new Vector2(-28f,  44f),
            new Vector2( 28f, -44f), new Vector2(-28f, -44f),
        };
        string[] treeModels =
        {
            "Tree_1_A_Color1", "Tree_2_A_Color1", "Tree_3_A_Color1",
            "Tree_4_A_Color1", "Tree_1_B_Color1", "Tree_2_C_Color1",
            "Tree_1_C_Color1", "Tree_2_B_Color1", "Tree_3_B_Color1",
            "Tree_4_B_Color1",
        };
        int treeIdx = 0;
        foreach (Vector2 spot in nearSpots)
        {
            // Jitter pequeno (1.5) para não invadir as zonas proibidas acima
            Vector3 basePos = Jitter(center, spot, top, 1.5f, rng);
            float s = 0.85f + (float)rng.NextDouble() * 0.45f;
            DecorProps.PlaceForest(root.transform, treeModels[treeIdx++ % treeModels.Length],
                basePos, 8f * s, Vector3.up, center - basePos);
        }
        foreach (Vector2 spot in farSpots)
        {
            Vector3 basePos = Jitter(center, spot, top, 2f, rng);
            float s = 0.9f + (float)rng.NextDouble() * 0.5f;
            DecorProps.PlaceForest(root.transform, treeModels[treeIdx++ % treeModels.Length],
                basePos, 10f * s, Vector3.up, center - basePos);
        }

        // ── Sub-bosque: arbustos, pedras e tufos de grama entre as árvores ──
        string[] bushModels = { "Bush_1_A_Color1", "Bush_2_B_Color1", "Bush_3_A_Color1", "Bush_4_A_Color1" };
        string[] rockModels = { "Rock_1_A_Color1", "Rock_2_C_Color1", "Rock_3_E_Color1", "Rock_1_C_Color1", "Rock_3_B_Color1" };
        string[] grassModels = { "Grass_1_A_Color1", "Grass_2_B_Color1", "Grass_1_B_Color1", "Grass_2_C_Color1" };
        // (mesmas zonas proibidas das árvores: nada no corredor central atrás
        // das mãos nem colado atrás da loja)
        Vector2[] underSpots =
        {
            new Vector2( 27f,  35f), new Vector2(-27f,  35f), new Vector2( 33f,  27f),
            new Vector2( 27f, -35f), new Vector2(-27f, -35f), new Vector2(-33f, -28f),
            new Vector2( 41f,  18f), new Vector2( 41f,  -7f), new Vector2( 36f,   6f),
            new Vector2(-42f,  26f), new Vector2(-41f, -26f), new Vector2(-36f,  30f),
            new Vector2( 26f,  39f), new Vector2(-26f,  39f), new Vector2( 26f, -39f),
            new Vector2(-26f, -39f), new Vector2( 44f, -20f), new Vector2(-44f, -30f),
        };
        for (int i = 0; i < underSpots.Length; i++)
        {
            Vector3 basePos = Jitter(center, underSpots[i], top, 2f, rng);
            int kind = rng.Next(3);
            if (kind == 0)
                DecorProps.PlaceForest(root.transform, bushModels[rng.Next(bushModels.Length)],
                    basePos, 1.8f + (float)rng.NextDouble() * 1.4f, Vector3.up, center - basePos);
            else if (kind == 1)
                DecorProps.PlaceForest(root.transform, rockModels[rng.Next(rockModels.Length)],
                    basePos, 1.4f + (float)rng.NextDouble() * 1.6f, Vector3.up, center - basePos);
            else
                DecorProps.PlaceForest(root.transform, grassModels[rng.Next(grassModels.Length)],
                    basePos, 1.0f + (float)rng.NextDouble() * 0.6f, Vector3.up, center - basePos);
        }

        // Tufos de grama logo DEPOIS da cordilheira (amarram a moldura ao chão)
        for (int i = 0; i < 14; i++)
        {
            // Um ponto na moldura do campo (lado sorteado), fora das pedras
            int side = rng.Next(4);
            float along = ((float)rng.NextDouble() * 2f - 1f) * (half - 2f);
            float off = half + 3.4f + (float)rng.NextDouble() * 1.8f;
            Vector2 p = side == 0 ? new Vector2(along, off)
                      : side == 1 ? new Vector2(along, -off)
                      : side == 2 ? new Vector2(off, along)
                      : new Vector2(-off, along);
            // Não invade a coluna da loja (x≈-27.5)
            if (p.x < -24f && Mathf.Abs(p.y) < 22f) continue;
            DecorProps.PlaceForest(root.transform, grassModels[rng.Next(grassModels.Length)],
                center + new Vector3(p.x, top, p.y), 0.8f + (float)rng.NextDouble() * 0.6f,
                Vector3.up, Vector3.forward);
        }

        // ── Batedores observando a clareira (Ranger e o Ladino encapuzado) ──
        // (Ranger na lateral direita; o Ladino no canto superior esquerdo —
        // fora da coluna da loja, que ocupa x≈-27.5 com |z|<22)
        DecorProps.PlaceChar(root.transform, "Ranger", "ranger_texture",
            center + new Vector3(33f, top, -18f), 5.8f, Vector3.up, center - new Vector3(33f, 0f, -18f));
        DecorProps.PlaceChar(root.transform, "Rogue_Hooded", "rogue_texture",
            center + new Vector3(-35f, top, 31f), 5.6f, Vector3.up, center - new Vector3(-35f, 0f, 31f));

        // ── Vagalumes esverdeados (a clareira viva à noite) ───────────────
        System.Random flyRng = new System.Random(seed * 13 + 7);
        for (int i = 0; i < 16; i++)
        {
            float side = flyRng.Next(2) == 0 ? -1f : 1f;
            float flyX = side * (25f + (float)flyRng.NextDouble() * 18f);
            float flyZ = ((float)flyRng.NextDouble() * 2f - 1f) * 36f;
            float flyY = 1.5f + (float)flyRng.NextDouble() * 8f;
            float flyS = 0.10f + (float)flyRng.NextDouble() * 0.15f;

            GameObject fly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fly.name = "Firefly";
            fly.transform.SetParent(root.transform, false);
            fly.transform.position = center + new Vector3(flyX, top + flyY, flyZ);
            fly.transform.localScale = Vector3.one * flyS;
            FinishGlow(fly, new Color(0.75f, 1f, 0.45f, 0.85f));
        }
    }

    // Jitter determinístico em volta de um ponto (nunca no eixo Y)
    static Vector3 Jitter(Vector3 center, Vector2 spot, float y, float amount, System.Random rng)
    {
        float jx = ((float)rng.NextDouble() * 2f - 1f) * amount;
        float jz = ((float)rng.NextDouble() * 2f - 1f) * amount;
        return center + new Vector3(spot.x + jx, y, spot.y + jz);
    }

    static GameObject MakeBox(string name, Vector3 pos, Vector3 scale, Color color, Texture2D tex)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(root.transform, false);
        box.transform.position = pos;
        box.transform.localScale = scale;

        Collider col = box.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        Renderer r = box.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (r != null && shader != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BaseMap", tex);
            }
            r.material = mat;
        }
        return box;
    }

    static void SetTextureTiling(GameObject go, float tilesX, float tilesY)
    {
        Renderer r = go != null ? go.GetComponent<Renderer>() : null;
        if (r == null) return;
        r.material.mainTextureScale = new Vector2(tilesX, tilesY);
        if (r.material.HasProperty("_BaseMap"))
            r.material.SetTextureScale("_BaseMap", new Vector2(tilesX, tilesY));
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

    // Textura de grama procedural (512px, mipmaps, repetível): manchas de tom
    // (Perlin largo) + fiapos finos alongados no Z — clara o bastante para a
    // cor do material dominar (o verde vem do material)
    static Texture2D GetGrassTexture()
    {
        if (grassTexture != null) return grassTexture;

        const int size = 512;
        grassTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
        grassTexture.wrapMode = TextureWrapMode.Repeat;
        grassTexture.filterMode = FilterMode.Trilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float patch = Mathf.PerlinNoise(x * 0.017f, y * 0.017f);      // manchas
                float blade = Mathf.PerlinNoise(x * 0.35f + 73f, y * 0.09f);  // fiapos
                float v = 0.68f + 0.20f * patch + 0.12f * blade;
                // Levemente mais quente nas manchas claras (grama seca)
                grassTexture.SetPixel(x, y, new Color(v * 0.98f, v, v * 0.82f, 1f));
            }
        }

        grassTexture.Apply(true);
        return grassTexture;
    }
}
