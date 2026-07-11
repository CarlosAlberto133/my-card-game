using System.Collections;
using UnityEngine;

// Animações procedurais de carta (sem assets/prefab), adicionadas sob demanda
// como o CardStatusVisuals. Tudo por código, então funciona no build e roda
// idêntico nos dois clientes (é puramente visual — não muda estado do jogo).
public class CardAnimator : MonoBehaviour
{
    private Vector3 home;      // Posição de descanso da carta (mundo)
    private bool active;       // Há uma animação de posição em andamento?

    // Garante que a carta tem um animador
    public static CardAnimator Get(GameObject go)
    {
        CardAnimator a = go.GetComponent<CardAnimator>();
        if (a == null) a = go.AddComponent<CardAnimator>();
        return a;
    }

    // Captura a posição de descanso e para qualquer animação anterior.
    // Se já estava animando, volta ao descanso antes de começar a próxima
    // (assim animações encadeadas não acumulam deslocamento).
    private void Begin()
    {
        if (active) transform.position = home;
        else home = transform.position;
        StopAllCoroutines();
        active = true;
    }

    // Investida: avança na direção do alvo e recua (ataque)
    public void Lunge(Vector3 targetWorldPos)
    {
        if (!gameObject.activeInHierarchy) return;
        Begin();
        StartCoroutine(LungeRoutine(targetWorldPos));
    }

    private IEnumerator LungeRoutine(Vector3 targetWorldPos)
    {
        Vector3 dir = targetWorldPos - home;
        dir.y = 0f; // Mantém no plano do tabuleiro
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        // Avança ~35% do caminho até o alvo (limitado para não voar longe)
        Vector3 peak = home + Vector3.ClampMagnitude(dir * 0.35f, 3.5f);

        yield return Move(home, peak, 0.10f, EaseOut);   // Estocada rápida
        yield return Move(peak, home, 0.16f, EaseIn);    // Recuo mais lento
        transform.position = home;
        active = false;
    }

    // Tremor: sacode a carta ao levar dano
    public void Shake()
    {
        if (!gameObject.activeInHierarchy) return;
        Begin();
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        const float duration = 0.28f;
        const float amplitude = 0.35f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float decay = 1f - (t / duration);              // Some com o tempo
            float offset = Mathf.Sin(t * 60f) * amplitude * decay;
            transform.position = home + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.position = home;
        active = false;
    }

    // Pulinho: pequeno salto ao ganhar status/ser curada
    public void Hop()
    {
        if (!gameObject.activeInHierarchy) return;
        Begin();
        StartCoroutine(HopRoutine());
    }

    private IEnumerator HopRoutine()
    {
        const float height = 0.6f;
        Vector3 top = home + new Vector3(0f, height, 0f);
        yield return Move(home, top, 0.12f, EaseOut);
        yield return Move(top, home, 0.16f, EaseIn);
        transform.position = home;
        active = false;
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float duration, System.Func<float, float> ease)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = ease(Mathf.Clamp01(t / duration));
            transform.position = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
    }

    private static float EaseOut(float x) => 1f - (1f - x) * (1f - x);
    private static float EaseIn(float x) => x * x;

    // ----- Explosão de partículas ao morrer (objeto independente da carta) -----
    private static Texture2D dotTex;

    public static void SpawnPoof(Vector3 position, Color color)
    {
        GameObject go = new GameObject("CardPoof");
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(color);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.4f;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        // Some ao longo da vida
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (r != null)
        {
            // Sprites/Default está em Always Included Shaders — seguro no build
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.mainTexture = GetDot();
                r.material = mat;
            }
        }

        ps.Play();
        Destroy(go, 1.5f);
    }

    static Texture2D GetDot()
    {
        if (dotTex != null) return dotTex;
        const int size = 32;
        dotTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                dotTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        dotTex.Apply();
        return dotTex;
    }
}
