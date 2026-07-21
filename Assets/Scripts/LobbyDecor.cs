using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Cenário do LOBBY: a ARTE da taverna (Resources/lobby_bg.png) preenche a tela
// inteira como fundo. Os heróis, o tabuleiro e toda a cena já estão pintados na
// própria arte — nada de 3D por cima. Só a interface (título, menu e painel de
// perfil, criados por LobbyProfileUI/PhotonLobbyManager) fica sobreposta.
public static class LobbyDecor
{
    static GameObject root;
    static Camera cam;

    const float BackgroundDepth = 40f;   // distância da arte até a câmera
    const float CamFov = 50f;

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  INTERRUPTOR: qual cenário o lobby usa                            ║
    // ║                                                                   ║
    // ║   true  = ARTE de fundo (Resources/lobby_bg.png)                  ║
    // ║   false = cenário MONTADO À MÃO no editor (objeto "LobbyStage")   ║
    // ║                                                                   ║
    // ║  Trocar só esta linha e salvar. Nada é apagado nos dois casos:    ║
    // ║  no modo arte o "LobbyStage" é apenas ESCONDIDO durante o jogo    ║
    // ║  (o que você montou no editor continua intacto na cena).          ║
    // ╚══════════════════════════════════════════════════════════════════╝
    public const bool UseArtBackground = true;

    // Nome do objeto que marca o cenário montado à mão no editor
    public const string HandBuiltStageName = "LobbyStage";

    public static bool HasHandBuiltStage()
    {
        GameObject stage = GameObject.Find(HandBuiltStageName);
        return stage != null && stage.activeInHierarchy;
    }

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

        GameObject stage = GameObject.Find(HandBuiltStageName);

        if (!UseArtBackground)
        {
            // Modo "montado à mão": o que está na cena manda, código sai fora
            if (stage != null && stage.activeInHierarchy)
            {
                Debug.Log("[LobbyDecor] Usando o cenário montado à mão ('LobbyStage').");
                return;
            }
            Debug.LogWarning("[LobbyDecor] UseArtBackground=false mas não achei 'LobbyStage' ativo — caindo para a arte de fundo.");
        }
        else if (stage != null)
        {
            // Modo arte: esconde o cenário do editor SÓ durante a execução
            // (nada é destruído — ao parar o Play ele volta como estava)
            stage.SetActive(false);
            Debug.Log("[LobbyDecor] 'LobbyStage' escondido — usando a arte de fundo. " +
                      "Para voltar ao seu cenário: UseArtBackground = false.");
        }

        cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            Debug.LogWarning("[LobbyDecor] Nenhuma câmera no lobby.");
            return;
        }

        root = new GameObject("LobbyDecor");

        // Câmera olhando reto para +Z (a arte fica perpendicular a ela)
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = false;
        cam.fieldOfView = CamFov;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, BackgroundDepth + 30f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.045f, 0.032f, 0.024f);

        BuildBackground();
    }

    // ── Fundo: a arte da taverna cobrindo a tela inteira ─────────────────

    static void BuildBackground()
    {
        Texture2D art = Resources.Load<Texture2D>("lobby_bg");
        if (art == null)
        {
            Debug.LogWarning("[LobbyDecor] Resources/lobby_bg.png não encontrada — fundo liso.");
            return;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "LobbyBackground";
        quad.transform.SetParent(root.transform, false);
        quad.transform.position = new Vector3(0f, 0f, cam.transform.position.z + BackgroundDepth);
        quad.transform.rotation = Quaternion.identity;

        Collider col = quad.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        // Escala "cobrir": preenche a tela inteira sem distorcer (o excesso
        // sai pelas bordas quando a tela não é 16:9)
        float frameH = 2f * BackgroundDepth * Mathf.Tan(CamFov * 0.5f * Mathf.Deg2Rad);
        float screenAspect = (cam.aspect > 0.01f) ? cam.aspect : 16f / 9f;
        float frameW = frameH * screenAspect;
        float artAspect = (float)art.width / art.height;

        float w, h;
        if (screenAspect > artAspect) { w = frameW; h = w / artAspect; }
        else { h = frameH; w = h * artAspect; }
        quad.transform.localScale = new Vector3(w, h, 1f);

        // Unlit: a arte já vem "iluminada", nenhuma luz da cena deve alterá-la
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Sprites/Default");
        if (shader == null) return;

        Material mat = new Material(shader);
        mat.color = Color.white;
        mat.mainTexture = art;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", art);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        quad.GetComponent<Renderer>().material = mat;
    }
}

// Toca o clip de idle em loop num herói do lobby (Playables, sem
// AnimatorController — mesma técnica do FigureRiggedAnimator do jogo).
// Mantido para uso futuro caso voltem figuras 3D ao lobby.
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
