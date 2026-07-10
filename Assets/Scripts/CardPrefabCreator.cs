using UnityEngine;
using TMPro;

public class CardPrefabCreator : MonoBehaviour
{
    [Header("Dimensões da Carta")]
    public float cardWidth = 1.8f;
    public float cardHeight = 2.5f;

    // ── Paleta de cores do design ──────────────────────────────────────────
    static readonly Color BorderColor = new Color(0.06f, 0.06f, 0.10f);
    static readonly Color BgColor = new Color(0.13f, 0.13f, 0.20f);
    static readonly Color NameBgColor = new Color(0.18f, 0.18f, 0.28f);
    static readonly Color EffectBgColor = new Color(0.22f, 0.22f, 0.32f);
    static readonly Color StatsBgColor = new Color(0.16f, 0.16f, 0.24f);
    static readonly Color TierBarColor = new Color(0.80f, 0.65f, 0.00f);
    static readonly Color DividerColor = new Color(0.35f, 0.35f, 0.55f);
    static readonly Color AtkColor = new Color(1.00f, 0.40f, 0.20f);
    static readonly Color DefColor = new Color(0.30f, 0.60f, 1.00f);
    static readonly Color HpColor = new Color(0.20f, 0.90f, 0.40f);

    [ContextMenu("Criar Card Prefab")]
    public void CreateCardPrefab()
    {
        GameObject root = new GameObject("CardPrefab");
        root.AddComponent<CardDisplay>();

        BoxCollider col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(cardWidth, 0.1f, cardHeight);
        col.center = new Vector3(0f, 0.05f, 0f);

        // ── Camadas Y (evitar z-fighting) ─────────────────────────────────
        const float L0 = 0.000f;  // borda
        const float L1 = 0.003f;  // fundo principal
        const float L2 = 0.006f;  // seções
        const float L3 = 0.009f;  // divisores / detalhes
        const float L4 = 0.012f;  // textos

        // ── Alturas das zonas ──────────────────────────────────────────────
        float nameH = cardHeight * 0.12f;  // 0.300
        float artworkH = cardHeight * 0.40f;  // 1.000
        float effectH = cardHeight * 0.24f;  // 0.600
        float statsH = cardHeight * 0.16f;  // 0.400
        float tierH = cardHeight * 0.08f;  // 0.200

        // ── Centros Z (negativo = topo da carta, positivo = base) ──────────
        float top = -(cardHeight * 0.5f);
        float nameZ = top + nameH * 0.5f;
        float artZ = nameZ + nameH * 0.5f + artworkH * 0.5f;
        float effZ = artZ + artworkH * 0.5f + effectH * 0.5f;
        float statZ = effZ + effectH * 0.5f + statsH * 0.5f;
        float tierZ = statZ + statsH * 0.5f + tierH * 0.5f;

        // ── QUADS ─────────────────────────────────────────────────────────

        // Borda
        MakeQuad("Border", root, cardWidth + 0.14f, cardHeight + 0.14f, L0, 0f, 0f, BorderColor);
        // Fundo (cor de classe aplicada via CardDisplay)
        MakeQuad("Background", root, cardWidth, cardHeight, L1, 0f, 0f, BgColor);
        // Cabeçalho do nome
        MakeQuad("NameHeader", root, cardWidth, nameH, L2, 0f, nameZ, NameBgColor);
        // Artwork — substituído em tempo real pela textura da carta
        MakeArtworkQuad("Artwork", root, cardWidth * 0.94f, artworkH, L2, 0f, artZ, new Color(0.28f, 0.28f, 0.28f));
        // Caixa de efeito
        MakeQuad("EffectBackground", root, cardWidth * 0.92f, effectH, L2, 0f, effZ, EffectBgColor);
        // Fundo dos stats
        MakeQuad("StatsBackground", root, cardWidth, statsH, L2, 0f, statZ, StatsBgColor);
        // Barra de tier — cor atualizada via CardDisplay
        MakeQuad("TierBar", root, cardWidth, tierH, L2, 0f, tierZ, TierBarColor);

        // Divisores verticais dos stats (3 colunas)
        float colW = cardWidth / 3f;
        MakeQuad("StatsDivider1", root, 0.025f, statsH * 0.75f, L3, -colW, statZ, DividerColor);
        MakeQuad("StatsDivider2", root, 0.025f, statsH * 0.75f, L3, +colW, statZ, DividerColor);

        // ── TEXTOS ────────────────────────────────────────────────────────
        float tw = cardWidth * 0.85f;

        // Nome da carta
        MakeText("CardNameText", root,
            new Vector3(0f, L4, nameZ),
            3.2f, TextAlignmentOptions.Center, Color.white, "Nome da Carta",
            new Vector2(tw, nameH * 0.80f));

        // Valores dos stats — uma fileira centralizada na zona
        float col1 = -colW;
        float col2 = 0f;
        float col3 = +colW;
        float sTW = colW * 0.88f;

        MakeText("AttackText", root, new Vector3(col1, L4, statZ),
            4.5f, TextAlignmentOptions.Center, new Color(1.00f, 0.40f, 0.20f), "00",
            new Vector2(sTW, statsH * 0.90f));
        MakeText("ShieldText", root, new Vector3(col2, L4, statZ),
            4.5f, TextAlignmentOptions.Center, new Color(0.30f, 0.60f, 1.00f), "00",
            new Vector2(sTW, statsH * 0.90f));
        MakeText("HealthText", root, new Vector3(col3, L4, statZ),
            4.5f, TextAlignmentOptions.Center, new Color(0.20f, 0.90f, 0.40f), "00",
            new Vector2(sTW, statsH * 0.90f));

        // Texto de efeito (auto-size para caber no box)
        MakeEffectText(root, L4, effZ, tw, effectH * 0.88f);

        // Tier (número, canto esquerdo da barra)
        MakeText("TierText", root,
            new Vector3(-cardWidth * 0.35f, L4, tierZ),
            2.8f, TextAlignmentOptions.Center, Color.white, "1",
            new Vector2(colW * 0.70f, tierH * 0.80f));

        // Classe (canto direito da barra)
        MakeText("ClassText", root,
            new Vector3(+cardWidth * 0.30f, L4, tierZ),
            1.9f, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f), "Tank",
            new Vector2(colW, tierH * 0.80f));

        Debug.Log("Card Prefab criado com sucesso! Arraste para Assets/Prefabs/ para salvar.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    void MakeEffectText(GameObject parent, float yLayer, float zCenter, float width, float height)
    {
        GameObject obj = new GameObject("EffectText");
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = new Vector3(0f, yLayer, zCenter);
        obj.transform.localRotation = Quaternion.Euler(90, 180, 0);

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = "Sem efeito";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.88f, 0.88f, 0.88f);
        tmp.fontStyle = FontStyles.Normal;
        tmp.richText = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1.0f;
        tmp.fontSizeMax = 2.2f;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.rectTransform.sizeDelta = new Vector2(width, height);
    }

    void MakeArtworkQuad(string name, GameObject parent, float width, float depth,
                         float yLayer, float xCenter, float zCenter, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent.transform);
        quad.transform.localPosition = new Vector3(xCenter, yLayer, zCenter);
        // Euler(90,0,180) mantém o quad plano mas inverte V — imagem fica na orientação correta
        quad.transform.localRotation = Quaternion.Euler(90, 0, 180);
        quad.transform.localScale = new Vector3(width, depth, 1f);

        Collider c = quad.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        Renderer r = quad.GetComponent<Renderer>();
        if (r != null)
        {
            // Fallback final: shader do material padrão do próprio quad (sempre existe no Build)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard")
                         ?? r.sharedMaterial.shader;
            Material mat = new Material(shader);
            mat.color = color;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            r.material = mat;
        }
    }

    void MakeQuad(string name, GameObject parent, float width, float depth,
                  float yLayer, float xCenter, float zCenter, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent.transform);
        quad.transform.localPosition = new Vector3(xCenter, yLayer, zCenter);
        quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        quad.transform.localScale = new Vector3(width, depth, 1f);

        Collider c = quad.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        Renderer r = quad.GetComponent<Renderer>();
        if (r != null)
        {
            // Tenta URP Lit, usa Unlit/Color como fallback.
            // Fallback final: shader do material padrão do próprio quad (sempre existe no Build)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard")
                         ?? r.sharedMaterial.shader;
            Material mat = new Material(shader);
            mat.color = color;                      // shaders legados
            mat.SetColor("_BaseColor", color);      // URP Lit
            mat.SetColor("_Color", color);          // Standard
            r.material = mat;
        }
    }

    void MakeText(string name, GameObject parent, Vector3 localPos, float fontSize,
                  TextAlignmentOptions alignment, Color color, string defaultText, Vector2 rectSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.Euler(90, 180, 0);

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.richText = false;
        tmp.rectTransform.sizeDelta = rectSize;
    }
}
