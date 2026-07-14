using UnityEngine;

// Projétil visual de efeito: uma esfera brilhante que voa em arco da carta
// que lançou até o alvo e estoura em partículas na chegada. 100% visual e
// local — o efeito/dano em si já foi aplicado na hora (lockstep intocado).
public class EffectProjectileFX : MonoBehaviour
{
    private Vector3 start;
    private Vector3 end;
    private Color color;
    private float duration = 0.45f;
    private float age = 0f;
    private float baseSize = 0.55f;
    private Transform core;
    private Material coreMat; // destruído junto (Destroy(go) não libera assets)

    // Cores padrão por "escola" de efeito
    public static readonly Color Fire = new Color(1f, 0.45f, 0.15f);
    public static readonly Color Ice = new Color(0.55f, 0.85f, 1f);
    public static readonly Color HealGreen = new Color(0.40f, 1f, 0.50f);
    public static readonly Color Arrow = new Color(0.95f, 0.90f, 0.60f);
    public static readonly Color Arcane = new Color(0.80f, 0.55f, 1f);
    public static readonly Color ShieldBlue = new Color(0.40f, 0.70f, 1f);
    public static readonly Color GoldBuff = new Color(1f, 0.85f, 0.30f);

    public static void Launch(CardDisplay from, CardDisplay to, Color color, float size = 0.55f)
    {
        if (from == null || to == null || from == to) return;
        Launch(from.transform.position, to.transform.position, color, size);
    }

    public static void Launch(Vector3 from, Vector3 to, Color color, float size = 0.55f)
    {
        GameObject go = new GameObject("EffectProjectile");
        EffectProjectileFX fx = go.AddComponent<EffectProjectileFX>();

        // Voa um pouco acima das cartas
        fx.start = from + Vector3.up * 1.2f;
        fx.end = to + Vector3.up * 0.8f;
        fx.color = color;
        fx.baseSize = size;
        go.transform.position = fx.start;

        // Núcleo: esfera brilhante (Sprites/Default = sem sombra, sempre visível,
        // está em Always Included Shaders — seguro no build)
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Core";
        sphere.transform.SetParent(go.transform, false);
        sphere.transform.localScale = Vector3.one * size;
        fx.core = sphere.transform;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = sphere.GetComponent<Renderer>();
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null && r != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            r.material = mat;
            fx.coreMat = mat;
        }

        Destroy(go, 2f); // rede de segurança
    }

    void OnDestroy()
    {
        // Materiais criados em runtime vazam se não forem destruídos junto
        if (coreMat != null) Destroy(coreMat);
    }

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);

        // Trajetória em arco (parábola) com leve aceleração no fim
        float eased = k * k * (3f - 2f * k); // smoothstep
        Vector3 pos = Vector3.Lerp(start, end, eased);
        pos.y += Mathf.Sin(k * Mathf.PI) * 2.2f;
        transform.position = pos;

        // Pulso do núcleo (dá vida ao projétil)
        if (core != null)
        {
            float pulse = 1f + Mathf.Sin(age * 40f) * 0.15f;
            core.localScale = Vector3.one * (baseSize * pulse);
        }

        // Chegou: estoura em partículas da cor do efeito e some
        if (k >= 1f)
        {
            CardAnimator.SpawnPoof(end, color);
            Destroy(gameObject);
        }
    }
}
