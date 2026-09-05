using UnityEngine;

// Carrega e posiciona os modelos do KayKit Dungeon Pack (CC0, kaylousberg.com)
// que vivem em Resources/decor/kaykit. Todos os modelos usam UMA textura-atlas
// (dungeon_texture) — aplicamos um material URP Lit com ela em runtime, porque
// o material importado do FBX pode vir magenta no build URP.
// Decoração pura: colliders removidos (não roubam cliques) e nenhum uso de
// UnityEngine.Random (posições fixas — lockstep intocado).
public static class DecorProps
{
    // Apaga um Object tanto em jogo quanto no EDITOR — o "assar cenário" do
    // menu Card Game roda estes montadores fora do Play mode, onde Destroy()
    // é proibido (só DestroyImmediate funciona)
    public static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }

    // Zera os materiais cacheados. O assador do MesaStage salva os materiais
    // gerados como assets e apaga a pasta ao re-assar — sem isto os caches
    // ficariam apontando para assets mortos
    public static void ResetCachesForBake()
    {
        sharedMat = null;
        forestMat = null;
        toolsMat = null;
        tintedMats.Clear();
        sceneryMats.Clear();
    }

    static Material sharedMat;

    static Material GetMaterial()
    {
        if (sharedMat != null) return sharedMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;

        sharedMat = new Material(shader);
        sharedMat.color = Color.white;
        Texture2D tex = Resources.Load<Texture2D>("decor/kaykit/dungeon_texture");
        if (tex != null)
        {
            sharedMat.mainTexture = tex;
            sharedMat.SetTexture("_BaseMap", tex);
        }
        return sharedMat;
    }

    // Atlas do Forest Nature Pack (árvores/arbustos/pedras/grama)
    static Material forestMat;

    static Material GetForestMaterial()
    {
        if (forestMat != null) return forestMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;

        forestMat = new Material(shader);
        forestMat.color = Color.white;
        Texture2D tex = Resources.Load<Texture2D>("decor/kaykit/forest/forest_texture");
        if (tex != null)
        {
            forestMat.mainTexture = tex;
            forestMat.SetTexture("_BaseMap", tex);
        }
        return forestMat;
    }

    // Atlas do RPG Tools Bits (lanterna e afins)
    static Material toolsMat;

    static Material GetToolsMaterial()
    {
        if (toolsMat != null) return toolsMat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;

        toolsMat = new Material(shader);
        toolsMat.color = Color.white;
        Texture2D tex = Resources.Load<Texture2D>("decor/kaykit/tools_bits_texture");
        if (tex != null)
        {
            toolsMat.mainTexture = tex;
            toolsMat.SetTexture("_BaseMap", tex);
        }
        return toolsMat;
    }

    // Variações TINGIDAS do atlas do dungeon (ex.: estandarte branco → roxo do
    // Mago). Uma por cor, cacheadas — a cor multiplica a textura inteira.
    static readonly System.Collections.Generic.Dictionary<Color, Material> tintedMats =
        new System.Collections.Generic.Dictionary<Color, Material>();

    static Material GetTintedMaterial(Color tint)
    {
        Material cached;
        if (tintedMats.TryGetValue(tint, out cached)) return cached;

        Material baseMat = GetMaterial();
        Material mat = baseMat != null ? new Material(baseMat) : null;
        if (mat != null)
        {
            mat.color = tint;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        }
        tintedMats[tint] = mat;
        return mat;
    }

    // ── Peças de cenário próprias (geradas no Meshy) ─────────────────────
    // Vivem em Resources/decor/cenario/<slug>.fbx com textura PRÓPRIA ao lado
    // (<slug>_tex.png de cor + <slug>_normal.png opcional) — ao contrário do
    // KayKit, que compartilha um atlas entre todos os modelos. Uma peça = um
    // material, cacheado: as 49 casas do tabuleiro dividem o mesmo.
    static readonly System.Collections.Generic.Dictionary<string, Material> sceneryMats =
        new System.Collections.Generic.Dictionary<string, Material>();

    static Material GetSceneryMaterial(string slug)
    {
        Material cached;
        if (sceneryMats.TryGetValue(slug, out cached)) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        Material mat = null;
        if (shader != null)
        {
            mat = new Material(shader);
            mat.color = Color.white;

            Texture2D tex = Resources.Load<Texture2D>("decor/cenario/" + slug + "_tex");
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BaseMap", tex);
            }

            // Relevo: é o normal map que faz a pedra ler como pedra na câmera
            // inclinada — sem ele a peça vira um desenho chapado
            Texture2D nrm = Resources.Load<Texture2D>("decor/cenario/" + slug + "_normal");
            if (nrm != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", nrm);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Pedra fosca: o padrão do URP Lit (0.5) deixa o chão plastificado
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        }

        sceneryMats[slug] = mat;
        return mat;
    }

    // Instancia um prop do KayKit em pé sobre uma superfície:
    //   basePos = ponto de apoio; up = normal da superfície;
    //   lookDir = para onde o prop "olha"; targetHeight = altura final.
    public static GameObject Place(Transform parent, string model, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir)
    {
        return PlaceFrom(parent, "decor/kaykit/" + model, GetMaterial(),
            basePos, targetHeight, up, lookDir);
    }

    // Prop do dungeon com o atlas tingido (estandarte roxo do Mago etc.)
    public static GameObject PlaceTinted(Transform parent, string model, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir, Color tint)
    {
        return PlaceFrom(parent, "decor/kaykit/" + model, GetTintedMaterial(tint),
            basePos, targetHeight, up, lookDir);
    }

    // Prop do RPG Tools Bits (lanterna)
    public static GameObject PlaceTool(Transform parent, string model, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir)
    {
        return PlaceFrom(parent, "decor/kaykit/" + model, GetToolsMaterial(),
            basePos, targetHeight, up, lookDir);
    }

    // Prop do Forest Nature Pack (árvores, arbustos, pedras, grama)
    public static GameObject PlaceForest(Transform parent, string model, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir)
    {
        return PlaceFrom(parent, "decor/kaykit/forest/" + model, GetForestMaterial(),
            basePos, targetHeight, up, lookDir);
    }

    // Peça de CHÃO do KayKit dimensionada pela largura (footprint) e com o
    // TOPO alinhado em topCenter — usada pelas casas do tabuleiro no tema Mesa
    // de RPG. Devolve também o Renderer principal para o CardTile tingir
    // (o acesso via .material do tile cria a instância própria — o highlight
    // de uma casa não vaza para as outras).
    public static GameObject PlaceFloor(Transform parent, string model, Vector3 topCenter,
        float size, float yRotation, out Renderer mainRenderer)
    {
        mainRenderer = null;

        GameObject prefab = Resources.Load<GameObject>("decor/kaykit/" + model);
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorProps] Peça de chão não encontrada: {model}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = "Floor_" + model;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Kill(c);

        Material mat = GetMaterial();
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (mat != null) r.sharedMaterial = mat;
            if (mainRenderer == null) mainRenderer = r;
        }

        go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        Bounds b = BoundsOf(go);
        float foot = Mathf.Max(b.size.x, b.size.z);
        if (foot > 0.0001f)
            go.transform.localScale = go.transform.localScale * (size / foot);

        b = BoundsOf(go);
        go.transform.position += topCenter - new Vector3(b.center.x, b.max.y, b.center.z);
        return go;
    }

    // Igual ao PlaceFloor, mas para as peças PRÓPRIAS de Resources/decor/cenario
    // (textura por peça em vez do atlas do KayKit).
    //
    // ⚠️ Os modelos do Meshy costumam vir Z-up: a laje chega EM PÉ, como uma
    // parede. Em vez de confiar na importação (que converte umas sim, outras
    // não — ver os bonecos das cartas), medimos e deitamos sozinhos: o eixo
    // mais FINO de uma peça de chão é, por definição, a espessura dela, então
    // se o fino não for o Y, giramos -90° no X.
    public static GameObject PlaceSceneryFloor(Transform parent, string slug, Vector3 topCenter,
        float size, float yRotation, out Renderer mainRenderer)
    {
        mainRenderer = null;

        GameObject go = InstantiateScenery(parent, slug, "Floor_" + slug);
        if (go == null) return null;

        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (mainRenderer == null) { mainRenderer = r; break; }
        }

        // Deita a peça se ela veio em pé, e só então aplica o giro do tabuleiro
        go.transform.rotation = Quaternion.identity;
        Vector3 raw = BoundsOf(go).size;
        bool emPe = raw.y > Mathf.Min(raw.x, raw.z);
        Quaternion deitar = emPe ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
        go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f) * deitar;

        // Dimensiona pela pegada (largura da casa) e encosta o TOPO no tile
        Bounds b = BoundsOf(go);
        float foot = Mathf.Max(b.size.x, b.size.z);
        if (foot > 0.0001f)
            go.transform.localScale = go.transform.localScale * (size / foot);

        b = BoundsOf(go);
        go.transform.position += topCenter - new Vector3(b.center.x, b.max.y, b.center.z);
        return go;
    }

    // Uma TÁBUA (peça própria de decor/cenario) esticada como ripa de tampo:
    // o eixo mais COMPRIDO da malha vira o comprimento (na direção "along"),
    // o mais FINO vira a espessura (deitada, no Y) e o que sobra vira a
    // largura. Mesma desconfiança do resto do arquivo: nada de acreditar na
    // orientação do import — mede-se a malha e remonta-se.
    //
    // O TOPO da tábua encosta em topCenter.y: é a superfície onde as cartas
    // se apoiam, então é ela que tem de bater certo, não o centro.
    public static GameObject PlaceSceneryPlank(Transform parent, string slug,
        Vector3 topCenter, Vector3 along, float comprimento, float largura,
        float espessura, bool girar180)
    {
        GameObject go = InstantiateScenery(parent, slug, "Tabua_" + slug);
        if (go == null) return null;

        // Mede sem rotação e SEM mexer na escala: os fatores saem relativos
        // ao tamanho que a peça tem de fábrica
        go.transform.rotation = Quaternion.identity;
        Vector3 raw = BoundsOf(go).size;

        int eixoLongo = 0, eixoFino = 0;
        for (int i = 1; i < 3; i++)
        {
            if (raw[i] > raw[eixoLongo]) eixoLongo = i;
            if (raw[i] < raw[eixoFino]) eixoFino = i;
        }
        if (eixoFino == eixoLongo) eixoFino = (eixoLongo + 1) % 3;
        int eixoLargura = 3 - eixoLongo - eixoFino;

        Vector3 fator = Vector3.one;
        fator[eixoLongo] = comprimento / Mathf.Max(raw[eixoLongo], 0.0001f);
        fator[eixoLargura] = largura / Mathf.Max(raw[eixoLargura], 0.0001f);
        fator[eixoFino] = espessura / Mathf.Max(raw[eixoFino], 0.0001f);
        go.transform.localScale = Vector3.Scale(go.transform.localScale, fator);

        // Girar 180° troca a ponta da tábua: o MESMO modelo repetido doze
        // vezes lado a lado fica bem menos óbvio
        Vector3 dir = along.normalized;
        if (girar180) dir = -dir;
        go.transform.rotation = MapAxes(eixoLongo, dir, eixoFino, Vector3.up);

        Bounds b = BoundsOf(go);
        go.transform.position += topCenter - new Vector3(b.center.x, b.max.y, b.center.z);
        return go;
    }

    // Uma peça de cenário SOLTA, em pé, para ser largada na cena e posicionada
    // à MÃO no editor: escala pela ALTURA pedida e apoia a BASE em basePos
    // (é o pé que tem de encostar na mesa, não o centro).
    //
    // Aqui a orientação é palpite, não certeza como nas peças que o código
    // posiciona sozinho: os FBX do Meshy costumam chegar em pé, então o padrão
    // é não mexer. Só quando a peça vem claramente TOMBADA (altura abaixo de
    // 40% do maior lado) é que entra o -90 no X — a mesma correção que o piso
    // do tabuleiro já fazia. Se ainda assim vier torta, é um giro no Inspector:
    // esta peça existe para ser ajustada à mão.
    public static GameObject PlaceSceneryProp(Transform parent, string slug,
        Vector3 basePos, float altura, float yRotation)
    {
        GameObject go = InstantiateScenery(parent, slug, "Peca_" + slug);
        if (go == null) return null;

        go.transform.rotation = Quaternion.identity;
        Vector3 raw = BoundsOf(go).size;
        float maior = Mathf.Max(raw.x, Mathf.Max(raw.y, raw.z));
        Quaternion levantar = (maior > 0.0001f && raw.y < maior * 0.4f)
            ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
        go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f) * levantar;

        Bounds b = BoundsOf(go);
        if (b.size.y > 0.0001f)
            go.transform.localScale = go.transform.localScale * (altura / b.size.y);

        b = BoundsOf(go);
        go.transform.position += basePos - new Vector3(b.center.x, b.min.y, b.center.z);
        return go;
    }

    // ── Fogo e brilho ────────────────────────────────────────────────────

    // Pontinho macio (branco no meio, transparente na borda). Sem ele a
    // partícula é um quadrado. Gerado, como todo o resto do cenário.
    static Texture2D softDot;

    // UM material para TODAS as chamas. Antes cada chama criava o seu, o que
    // enchia a pasta do bake de cópias idênticas e multiplicava a chance de
    // dois assets brigarem pelo mesmo nome. Compartilhado, depois que o
    // primeiro vira arquivo os próximos já o encontram salvo e nem tentam.
    static Material flameMat;

    // Exposto para o menu de reparo do editor: uma chama que perdeu o material
    // (o bug de nomes repetidos do assador, corrigido em 04/set/2026) volta a
    // arder recebendo este mesmo material de novo.
    public static Material FlameMaterial()
    {
        if (flameMat == null)
        {
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return null;
            flameMat = new Material(shader);
            flameMat.name = "Chama";
            flameMat.mainTexture = SoftDot();
        }
        return flameMat;
    }

    public static Texture2D SoftDot()
    {
        if (softDot != null) return softDot;

        const int n = 64;
        softDot = new Texture2D(n, n, TextureFormat.RGBA32, false);
        softDot.name = "SoftDot";
        softDot.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f;
                float dy = (y + 0.5f) / n - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float a = Mathf.Clamp01(1f - d);
                softDot.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        softDot.Apply();
        return softDot;
    }

    // CHAMA viva para tocha, vela e candelabro. O fogo não vem no FBX: o
    // modelo é madeira parada, quem faz "arder" é este sistema de partículas
    // mais a luz que tremula (FlickerLight). Mesmo molde do "CardPoof" em
    // CardAnimator — montado na mão, sem pack, porque assim vira texto no
    // git em vez de mais binário no build.
    //
    // 100% visual e desenhado por cliente: aqui UnityEngine.Random seria até
    // permitido (a proibição do lockstep vale só para o que vira estado de
    // jogo), mas nem precisamos dele — a partícula já sorteia sozinha.
    public static ParticleSystem Flame(Transform parent, Vector3 pos,
        float tamanho, Color quente, Color fria)
    {
        GameObject go = new GameObject("Chama");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        // O cone do shape aponta para o +Z local; deitando -90 no X ele sobe
        go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        float esc = Mathf.Max(tamanho, 0.05f);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;      // sem isto a chama assada na cena nasce parada
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(esc * 1.1f, esc * 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(esc * 0.75f, esc * 1.30f);
        main.startColor = new ParticleSystem.MinMaxGradient(quente);
        main.maxParticles = 40;
        main.gravityModifier = -0.04f;   // puxa para CIMA
        // Mundo: a chama fica onde está em vez de escorregar junto se a peça
        // for movida no editor
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 26f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = esc * 0.22f;

        // Quente embaixo, escura e transparente em cima: é esse caminho de cor
        // que o olho lê como fogo, mais do que o formato
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(quente, 0f), new GradientColorKey(fria, 1f) },
            new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.55f, 0.45f),
                    new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Afina na ponta
        var siz = ps.sizeOverLifetime;
        siz.enabled = true;
        siz.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.12f));

        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            // Sprites/Default está em Always Included Shaders — seguro no build.
            // sharedMaterial, não material: ".material" clonaria uma cópia por
            // chama e desfaria justamente o compartilhamento.
            Material mat = FlameMaterial();
            if (mat != null) r.sharedMaterial = mat;
        }

        // A rede de segurança de verdade: o material da chama volta sozinho
        // toda vez que a cena carrega, sem depender de arquivo nenhum
        if (go.GetComponent<FlameLook>() == null) go.AddComponent<FlameLook>();

        ps.Play();
        return ps;
    }

    // CRISTAL ACESO. O diamante da peça de canto é pedra do MESMO material que
    // o resto do bloco (uma malha, uma textura) — pôr emissão no material
    // acenderia a mureta inteira. Então o brilho vem de fora: uma bolinha Unlit
    // com a cor multiplicada ALÉM DE 1, que é o que ultrapassa o threshold do
    // bloom e vira brilho de verdade, mais uma luz para a pedra em volta pegar
    // a cor. Sem bloom ligado, isto é só uma bola clara.
    public static GameObject GlowGem(Transform parent, Vector3 pos, float raio,
        Color cor, float forca)
    {
        GameObject go = new GameObject("Cristal");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.AddComponent<LightPiece>();   // clicar na bolinha seleciona a peça toda

        GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bola.name = "Brilho";
        bola.transform.SetParent(go.transform, false);
        bola.transform.localScale = Vector3.one * raio * 2f;
        Kill(bola.GetComponent<Collider>());   // nunca rouba clique

        Renderer r = bola.GetComponent<Renderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Sprites/Default");
        if (r != null && sh != null)
        {
            Material mat = new Material(sh);
            Color hdr = cor * forca;
            mat.color = hdr;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hdr);
            r.sharedMaterial = mat;
        }

        GameObject luz = new GameObject("LuzCristal");
        luz.transform.SetParent(go.transform, false);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = cor;
        // Mesma régua das tochas (ver ESCALA DAS LUZES em TabletopEnvironment):
        // 1/d² em unidades de mundo, e uma casa tem 6
        l.intensity = 22f;
        l.range = raio * 90f;

        // Respiração lenta, não tremulação de fogo — e fora do rebase das tochas
        FlickerLight f = luz.AddComponent<FlickerLight>();
        f.amplitude = 0.14f;
        f.velocidade = 0.8f;
        f.eTocha = false;
        return go;
    }

    // Carrega e instancia uma peça PRÓPRIA de Resources/decor/cenario, já sem
    // colliders e com o material da peça (textura + normal map + pedra fosca)
    static GameObject InstantiateScenery(Transform parent, string slug, string name)
    {
        GameObject prefab = Resources.Load<GameObject>("decor/cenario/" + slug);
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorProps] Peça de cenário não encontrada: decor/cenario/{slug}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = name;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Kill(c);

        Material mat = GetSceneryMaterial(slug);
        if (mat != null)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }
        return go;
    }

    // Rotação que leva o eixo LOCAL "a" (0=X, 1=Y, 2=Z) na direção dirA e o
    // eixo local "b" na direção dirB (ortogonais entre si). O terceiro eixo
    // sai do produto vetorial — sempre rotação de verdade, nunca espelho.
    static Quaternion MapAxes(int a, Vector3 dirA, int b, Vector3 dirB)
    {
        Vector3[] img = new Vector3[3];
        img[a] = dirA.normalized;
        img[b] = dirB.normalized;
        int c = 3 - a - b;
        img[c] = c == 0 ? Vector3.Cross(img[1], img[2])
               : c == 1 ? Vector3.Cross(img[2], img[0])
               : Vector3.Cross(img[0], img[1]);
        return Quaternion.LookRotation(img[2], img[1]);
    }

    // ── Moldura de pedra PRÓPRIA (Meshy) em volta do tabuleiro ──────────
    // A mureta reta e o canto em "L" vêm do mesmo lote do piso-tabuleiro,
    // então valem as mesmas suspeitas: o importador pode entregar a peça
    // girada. Em vez de confiar, MEDIMOS a malha e remontamos a orientação.

    // Mureta: estica ao longo de "along" (X ou Z), com a FACE EXTERNA
    // encostada na linha que passa por outerPos na direção "outward" e a base
    // na altura outerPos.y. O centro do vão é a componente de outerPos em
    // "along".
    public static GameObject PlaceSceneryWall(Transform parent, string slug,
        Vector3 outerPos, Vector3 along, Vector3 outward, float spanLength)
    {
        GameObject go = InstantiateScenery(parent, slug, "Wall_" + slug);
        if (go == null) return null;

        // Remonta a orientação pelas medidas: o eixo mais COMPRIDO é o vão;
        // dos dois que sobram, o maior vira altura (a mureta é mais alta que
        // grossa — 0.60 contra 0.54 no arquivo; se empatar, conferir no
        // render_fbx.py antes de culpar este código)
        go.transform.rotation = Quaternion.identity;
        Vector3 s = BoundsOf(go).size;
        int eixoLongo = s.x >= s.y && s.x >= s.z ? 0 : s.y >= s.z ? 1 : 2;
        int eixoAlto = -1; float alto = -1f;
        for (int i = 0; i < 3; i++)
            if (i != eixoLongo && s[i] > alto) { alto = s[i]; eixoAlto = i; }
        go.transform.rotation = MapAxes(eixoLongo, along, eixoAlto, Vector3.up);

        Bounds b = BoundsOf(go);
        float len = SizeAlong(b, along);
        if (len > 0.0001f)
            go.transform.localScale = go.transform.localScale * (spanLength / len);

        // Face externa na linha, base na altura, centrada no vão
        b = BoundsOf(go);
        Vector3 delta = along * Vector3.Dot(outerPos - b.center, along);
        float faceExterna = Vector3.Dot(b.center, outward) + SizeAlong(b, outward) * 0.5f;
        delta += outward * (Vector3.Dot(outerPos, outward) - faceExterna);
        delta += Vector3.up * (outerPos.y - b.min.y);
        go.transform.position += delta;
        return go;
    }

    // Canto em "L" da moldura, deitado no chão, com o VÉRTICE externo do L
    // no canto (sx, sz) do quadrado da moldura e os braços correndo pelos
    // dois lados. sx/sz = ±1 dizem qual dos 4 cantos é.
    // maxHeight > 0 trava a ALTURA em unidades de mundo, independente do
    // tamanho no chão — senão, engordar o canto o deixaria mais alto que a
    // mureta ao lado (aconteceu; o Carlos viu na hora).
    public static GameObject PlaceSceneryCorner(Transform parent, string slug,
        Vector3 outerCorner, int sx, int sz, float armLength, float maxHeight = 0f)
    {
        GameObject go = InstantiateScenery(parent, slug, "Corner_" + slug);
        if (go == null) return null;

        // 1) Deita: o eixo mais FINO do L é a altura dele
        go.transform.rotation = Quaternion.identity;
        Vector3 s = BoundsOf(go).size;
        int eixoFino = s.x <= s.y && s.x <= s.z ? 0 : s.y <= s.z ? 1 : 2;
        if (eixoFino != 1)
            go.transform.rotation = MapAxes(eixoFino, Vector3.up,
                eixoFino == 0 ? 1 : 0, eixoFino == 0 ? Vector3.forward : Vector3.right);

        // 2) Acha o miolo VAZIO do L contando vértices por quadrante — a
        // bounding box não diz (o L é quase quadrado), os vértices dizem
        Bounds b = BoundsOf(go);
        int[] conta = new int[4]; // (x-,z-) (x-,z+) (x+,z-) (x+,z+)
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                Vector3 w = mf.transform.TransformPoint(v);
                conta[(w.x > b.center.x ? 2 : 0) + (w.z > b.center.z ? 1 : 0)]++;
            }
        }
        int vazio = 0;
        for (int i = 1; i < 4; i++) if (conta[i] < conta[vazio]) vazio = i;
        int cheioX = (vazio & 2) != 0 ? -1 : 1; // vértice do L = oposto ao vazio
        int cheioZ = (vazio & 1) != 0 ? -1 : 1;

        // 3) Gira em passos de 90° até o vértice apontar para (sx, sz).
        // Os -90 são calibração CONFERIDA EM JOGO (Carlos, 31/ago, em duas
        // rodadas de ajuste): a contagem de vértices erra o quadrante do
        // miolo por um passo — o diamante do vértice engorda um dos braços
        // e desloca a leitura. Não consertar sem olhar o jogo.
        float atual = Mathf.Atan2(cheioX, cheioZ) * Mathf.Rad2Deg;
        float alvo = Mathf.Atan2(sx, sz) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, alvo - atual - 90f, 0f) * go.transform.rotation;

        // 4) Escala pelo braço e encosta o vértice externo no canto
        b = BoundsOf(go);
        float braco = Mathf.Max(b.size.x, b.size.z);
        if (braco > 0.0001f)
            go.transform.localScale = go.transform.localScale * (armLength / braco);

        // Achata só a altura se passou do teto. A peça está girada, então o
        // eixo LOCAL que aponta para cima precisa ser descoberto — com giros
        // em passos de 90°, é o que tiver |Y| ≈ 1 depois da rotação
        b = BoundsOf(go);
        if (maxHeight > 0f && b.size.y > maxHeight)
        {
            int eixoCima = 0; float maiorY = -1f;
            for (int i = 0; i < 3; i++)
            {
                Vector3 e = Vector3.zero; e[i] = 1f;
                float y = Mathf.Abs((go.transform.rotation * e).y);
                if (y > maiorY) { maiorY = y; eixoCima = i; }
            }
            Vector3 esc = go.transform.localScale;
            esc[eixoCima] *= maxHeight / b.size.y;
            go.transform.localScale = esc;
        }

        // Base de APOIO: a ponta de baixo do diamante desce ALÉM da base dos
        // braços — apoiar pelo mínimo global deixava os braços flutuando
        // (aconteceu; o Carlos viu). A base de verdade é o fundo das PONTAS
        // dos braços, longe do vértice; o diamante afunda na mesa e pronto.
        b = BoundsOf(go);
        float quinaX = sx > 0 ? b.max.x : b.min.x;
        float quinaZ = sz > 0 ? b.max.z : b.min.z;
        float alcance = Mathf.Max(b.size.x, b.size.z);
        float baseY = float.MaxValue;
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            foreach (Vector3 v in mf.sharedMesh.vertices)
            {
                Vector3 w = mf.transform.TransformPoint(v);
                float longe = Mathf.Max(Mathf.Abs(w.x - quinaX), Mathf.Abs(w.z - quinaZ));
                if (longe > alcance * 0.6f && w.y < baseY) baseY = w.y;
            }
        }
        if (baseY == float.MaxValue) baseY = b.min.y;

        Vector3 quina = new Vector3(quinaX, baseY, quinaZ);
        go.transform.position += outerCorner - quina;
        return go;
    }

    // Peça esticada ao longo de um eixo (muretas/troncos na borda do tabuleiro):
    // escala UNIFORME até spanLength no eixo "along", base apoiada em basePos.
    // O eixo longo do modelo é desconhecido — testa as duas orientações e fica
    // com a que deixa a peça mais comprida ao longo de "along".
    public static GameObject PlaceSpan(Transform parent, string model, Vector3 basePos,
        Vector3 up, Vector3 along, float spanLength)
    {
        GameObject prefab = Resources.Load<GameObject>("decor/kaykit/" + model);
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorProps] Modelo não encontrado: {model}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = "Span_" + model;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Kill(c);

        Material mat = GetMaterial();
        if (mat != null)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        Vector3 a = along.normalized;
        Quaternion rotA = Quaternion.LookRotation(a, up);
        Quaternion rotB = Quaternion.LookRotation(Vector3.Cross(up, a), up);
        go.transform.rotation = rotA;
        float lenA = SizeAlong(BoundsOf(go), a);
        go.transform.rotation = rotB;
        float lenB = SizeAlong(BoundsOf(go), a);
        if (lenA > lenB) go.transform.rotation = rotA;

        Bounds b = BoundsOf(go);
        float len = SizeAlong(b, a);
        if (len > 0.0001f)
            go.transform.localScale = go.transform.localScale * (spanLength / len);

        b = BoundsOf(go);
        float h = SizeAlong(b, up);
        go.transform.position += basePos - (b.center - up * (h * 0.5f));
        return go;
    }

    static GameObject PlaceFrom(Transform parent, string resourcePath, Material mat,
        Vector3 basePos, float targetHeight, Vector3 up, Vector3 lookDir)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorProps] Modelo não encontrado: {resourcePath}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = "Decor_" + prefab.name;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Kill(c);

        if (mat != null)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        Orient(go, basePos, targetHeight, up, lookDir);
        return go;
    }

    // Materiais dos aventureiros do KayKit (uma textura por personagem)
    static readonly System.Collections.Generic.Dictionary<string, Material> charMats =
        new System.Collections.Generic.Dictionary<string, Material>();

    static Material GetCharMaterial(string textureName)
    {
        Material cached;
        if (charMats.TryGetValue(textureName, out cached)) return cached;

        Material mat = null;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            mat = new Material(shader);
            mat.color = Color.white;
            Texture2D tex = Resources.Load<Texture2D>("decor/kaykit/chars/" + textureName);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BaseMap", tex);
            }
        }
        charMats[textureName] = mat;
        return mat;
    }

    // Aventureiro do KayKit Adventurers (CC0) como miniatura decorativa estática
    // (model = "Knight", "Rogue"...; textureName = "knight_texture", ...)
    public static GameObject PlaceChar(Transform parent, string model, string textureName,
        Vector3 basePos, float targetHeight, Vector3 up, Vector3 lookDir)
    {
        GameObject prefab = Resources.Load<GameObject>("decor/kaykit/chars/" + model);
        if (prefab == null)
        {
            Debug.LogWarning($"[DecorProps] Aventureiro não encontrado: {model}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);
        go.name = "Decor_" + model;

        foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
            Kill(c);

        Material mat = GetCharMaterial(textureName);
        if (mat != null)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        Orient(go, basePos, targetHeight, up, lookDir);
        return go;
    }

    // Os 4 heróis do jogo (Models/personagem_*) como miniaturas decorativas:
    // prefere o FBX riggado (pode animar idle), senão o OBJ estático.
    public static GameObject PlaceHero(Transform parent, string baseName, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir, bool animateIdle)
    {
        GameObject model = FindHeroModel(baseName);
        if (model == null) return null;

        GameObject hero = Object.Instantiate(model, parent);
        hero.name = "Hero_" + baseName.Replace("Models/", "");

        foreach (Collider c in hero.GetComponentsInChildren<Collider>(true))
            Kill(c);

        // Textura da classe se o modelo não tem própria (mesma regra do jogo)
        if (!HasOwnTexture(hero))
        {
            Texture2D tex = Resources.Load<Texture2D>(baseName + "_tex");
            if (tex != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = Color.white;
                    mat.mainTexture = tex;
                    mat.SetTexture("_BaseMap", tex);
                    foreach (Renderer r in hero.GetComponentsInChildren<Renderer>(true))
                        r.material = mat;
                }
            }
        }

        Orient(hero, basePos, targetHeight, up, lookDir);

        if (animateIdle && hero.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
        {
            AnimationClip idle = null;
            foreach (AnimationClip c in Resources.LoadAll<AnimationClip>(baseName + "_idle"))
            {
                idle = c;
                break;
            }
            if (idle != null)
            {
                Animator anim = hero.GetComponentInChildren<Animator>();
                if (anim == null) anim = hero.AddComponent<Animator>();
                hero.AddComponent<LobbyHeroIdle>().Play(anim, idle);
            }
        }
        return hero;
    }

    static GameObject FindHeroModel(string baseName)
    {
        string[] suf = { "_idle", "_walk", "_attack", "_death", "_react" };
        foreach (string s in suf)
        {
            GameObject g = Resources.Load<GameObject>(baseName + s);
            if (g != null && g.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                return g;
        }
        return Resources.Load<GameObject>(baseName);
    }

    static bool HasOwnTexture(GameObject fig)
    {
        foreach (Renderer r in fig.GetComponentsInChildren<Renderer>(true))
        {
            Material m = r.sharedMaterial;
            if (m == null) continue;
            if (m.mainTexture != null) return true;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return true;
        }
        return false;
    }

    // Rotaciona (em pé sobre "up", olhando "lookDir"), escala pela altura alvo
    // e apoia a BASE do modelo exatamente em basePos
    static void Orient(GameObject go, Vector3 basePos, float targetHeight, Vector3 up, Vector3 lookDir)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(lookDir, up);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.right;
        go.transform.rotation = Quaternion.LookRotation(fwd.normalized, up);

        Bounds b = BoundsOf(go);
        float h = SizeAlong(b, up);
        if (h > 0.0001f)
            go.transform.localScale = go.transform.localScale * (targetHeight / h);

        b = BoundsOf(go);
        h = SizeAlong(b, up);
        go.transform.position += basePos - (b.center - up * (h * 0.5f));
    }

    static Bounds BoundsOf(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // Extensão de um AABB projetada num eixo (para escalar em superfícies inclinadas)
    static float SizeAlong(Bounds b, Vector3 axis)
    {
        Vector3 e = b.extents;
        return 2f * (Mathf.Abs(e.x * axis.x) + Mathf.Abs(e.y * axis.y) + Mathf.Abs(e.z * axis.z));
    }
}
