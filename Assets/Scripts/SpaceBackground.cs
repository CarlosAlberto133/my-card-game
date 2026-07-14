using UnityEngine;

// Fundo espacial procedural (sem assets): céu escuro, estrelas derivando
// lentamente e meteoros ocasionais cruzando a tela. Tudo criado por código,
// então funciona no build sem depender de texturas/materiais importados.
public class SpaceBackground : MonoBehaviour
{
    private static SpaceBackground instance;
    private static Texture2D dotTexture;

    private bool cameraFacing; // Layout de tela (lobby) em vez de plano do tabuleiro

    // Cria o fundo uma única vez (chamado pelo GameManager na cena do jogo).
    // Layout deitado no plano XZ, visto pela câmera inclinada do tabuleiro.
    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("SpaceBackground");
        instance = go.AddComponent<SpaceBackground>();
    }

    // Versão para telas 2D (lobby/menu): estrelas e meteoros ficam num plano
    // de frente para a câmera, então funciona com qualquer orientação de câmera.
    public static void EnsureFacingCamera()
    {
        if (instance != null) return;
        GameObject go = new GameObject("SpaceBackground");
        SpaceBackground sb = go.AddComponent<SpaceBackground>();
        sb.cameraFacing = true;
    }

    // Remove o fundo espacial (troca de temática pelo BoardThemeManager)
    public static void Clear()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    void Start()
    {
        // Céu escuro (quase preto, levemente azulado).
        // No lobby a câmera é "Untagged", então Camera.main é null — cai no fallback.
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.015f, 0.045f);
        }

        if (cameraFacing)
        {
            CreateCameraFacing(cam);
            return;
        }

        Vector3 center = Vector3.zero;
        BoardManager board = BoardManager.Instance;
        if (board != null) center = board.transform.position;

        CreateStarfield(center);
        CreateMeteors(center);
    }

    // ---- Layout de frente para a câmera (lobby) ----
    // Monta um "painel" no espaço local da câmera: estrelas preenchendo o quadro
    // e meteoros riscando na diagonal. Usa espaço de simulação Local, então tudo
    // acompanha a câmera e enquadra certo em qualquer rotação.
    void CreateCameraFacing(Camera cam)
    {
        if (cam == null) cam = Camera.main;

        // Dimensões do quadro visível a uma certa distância à frente da câmera
        float dist = 30f;
        float aspect = (cam != null && cam.aspect > 0.01f) ? cam.aspect : 16f / 9f;
        float height, width;
        if (cam != null && cam.orthographic)
        {
            height = cam.orthographicSize * 2f;
        }
        else
        {
            float fov = (cam != null) ? cam.fieldOfView : 60f;
            height = 2f * dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        }
        width = height * aspect;
        // Margem extra para nunca aparecer borda vazia
        width *= 1.4f;
        height *= 1.4f;

        Vector3 forward = (cam != null) ? cam.transform.forward : Vector3.forward;
        Quaternion rot = (cam != null) ? cam.transform.rotation : Quaternion.identity;
        Vector3 camPos = (cam != null) ? cam.transform.position : Vector3.zero;
        Vector3 panelPos = camPos + forward * dist;

        CreateStarfieldFacing(panelPos, rot, width, height);
        CreateMeteorsFacing(panelPos, rot, width, height);
    }

    void CreateStarfieldFacing(Vector3 pos, Quaternion rot, float width, float height)
    {
        GameObject go = new GameObject("Starfield");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.rotation = rot;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(25f, 45f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.9f), new Color(0.65f, 0.80f, 1f, 0.7f));
        main.maxParticles = 1200;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        // Plano XY local (de frente para a câmera), fino em Z
        shape.scale = new Vector3(width, height, 0.1f);

        // Deriva lenta lateral no plano da tela
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve twinkle = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.15f, 1f),
            new Keyframe(0.5f, 0.6f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0.3f));
        size.size = new ParticleSystem.MinMaxCurve(1f, twinkle);

        SetupRenderer(ps, false);
        ps.Play();
    }

    void CreateMeteorsFacing(Vector3 pos, Quaternion rot, float width, float height)
    {
        GameObject go = new GameObject("Meteors");
        go.transform.SetParent(transform, false);
        // Nasce um pouco acima/à direita do quadro e cruza na diagonal
        go.transform.position = pos + rot * new Vector3(width * 0.35f, height * 0.6f, 0f);
        go.transform.rotation = rot;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 3.5f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.55f, 0.95f), new Color(1f, 0.55f, 0.25f, 0.95f));
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 0.3f; // Um meteoro a cada ~3s

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width * 0.5f, height * 0.4f, 0.1f);

        // Desce na diagonal pelo plano da tela (local)
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-22f, -14f);
        vel.y = new ParticleSystem.MinMaxCurve(-22f, -14f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        SetupRenderer(ps, true);
        ps.Play();
    }

    void CreateStarfield(Vector3 center)
    {
        GameObject go = new GameObject("Starfield");
        go.transform.SetParent(transform, false);
        go.transform.position = center + new Vector3(0f, -3f, 0f); // Abaixo dos tiles

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.prewarm = true; // Tela já começa cheia de estrelas
        main.startLifetime = new ParticleSystem.MinMaxCurve(25f, 45f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.9f), new Color(0.65f, 0.80f, 1f, 0.7f));
        main.maxParticles = 1200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(260f, 0.1f, 260f);

        // Deriva lenta — as estrelas "passam" devagar pelo fundo
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        // Os 3 eixos precisam usar o MESMO modo de curva (duas constantes)
        vel.x = new ParticleSystem.MinMaxCurve(-1.2f, -0.4f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        // Cintilar: tamanho oscila ao longo da vida
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve twinkle = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.15f, 1f),
            new Keyframe(0.5f, 0.6f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0.3f));
        size.size = new ParticleSystem.MinMaxCurve(1f, twinkle);

        SetupRenderer(ps, false);
        ps.Play();
    }

    void CreateMeteors(Vector3 center)
    {
        GameObject go = new GameObject("Meteors");
        go.transform.SetParent(transform, false);
        // Nasce num canto e cruza o tabuleiro na diagonal
        go.transform.position = center + new Vector3(90f, -2f, 90f);
        go.transform.rotation = Quaternion.LookRotation(new Vector3(-1f, 0f, -1f));

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 3.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(55f, 85f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.85f, 0.55f, 0.95f), new Color(1f, 0.55f, 0.25f, 0.95f));
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0.25f; // Um meteoro a cada ~4s

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(160f, 0.1f, 30f); // Faixa larga: cada meteoro num lugar

        SetupRenderer(ps, true);
        ps.Play();
    }

    void SetupRenderer(ParticleSystem ps, bool stretched)
    {
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (r == null) return;

        // Sprites/Default está em Always Included Shaders — seguro no build
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return;

        Material mat = new Material(shader);
        mat.mainTexture = GetDotTexture();
        r.material = mat;

        if (stretched)
        {
            // Meteoros viram riscos alongados na direção do movimento
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.12f;
        }
    }

    // Textura circular suave gerada por código (usada por estrelas e meteoros)
    static Texture2D GetDotTexture()
    {
        if (dotTexture != null) return dotTexture;

        const int size = 64;
        dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // Queda suave nas bordas
                dotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        dotTexture.Apply();
        return dotTexture;
    }
}
