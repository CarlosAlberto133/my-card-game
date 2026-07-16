using UnityEngine;

// Carrega e posiciona os modelos do KayKit Dungeon Pack (CC0, kaylousberg.com)
// que vivem em Resources/decor/kaykit. Todos os modelos usam UMA textura-atlas
// (dungeon_texture) — aplicamos um material URP Lit com ela em runtime, porque
// o material importado do FBX pode vir magenta no build URP.
// Decoração pura: colliders removidos (não roubam cliques) e nenhum uso de
// UnityEngine.Random (posições fixas — lockstep intocado).
public static class DecorProps
{
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

    // Instancia um prop do KayKit em pé sobre uma superfície:
    //   basePos = ponto de apoio; up = normal da superfície;
    //   lookDir = para onde o prop "olha"; targetHeight = altura final.
    public static GameObject Place(Transform parent, string model, Vector3 basePos,
        float targetHeight, Vector3 up, Vector3 lookDir)
    {
        return PlaceFrom(parent, "decor/kaykit/" + model, GetMaterial(),
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
            Object.Destroy(c);

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
            Object.Destroy(c);

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
            Object.Destroy(c);

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
            Object.Destroy(c);

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

// Luz pontual com tremulação de fogo (tochas/velas). Usa PerlinNoise com
// Time.time — puramente visual, nada de UnityEngine.Random (lockstep intocado).
public class FlickerLight : MonoBehaviour
{
    Light lt;
    float baseIntensity;
    float seedOffset;

    public static void Attach(Transform parent, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject("FlickerLight");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;

        FlickerLight f = go.AddComponent<FlickerLight>();
        f.lt = l;
        f.baseIntensity = intensity;
        f.seedOffset = pos.x * 3.7f + pos.z * 1.3f;
    }

    void Update()
    {
        if (lt != null)
            lt.intensity = baseIntensity * (0.82f + 0.36f * Mathf.PerlinNoise(Time.time * 5.5f, seedOffset));
    }
}
