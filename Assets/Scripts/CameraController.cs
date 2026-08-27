using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    // Camada das cartas da LOJA: desenhadas por uma câmera overlay (stack da
    // URP) que limpa o depth — a loja nunca fica escondida atrás das
    // estruturas do cenário com a câmera deitada. Layer 30 está livre no
    // projeto (nenhum outro script usa layers).
    public const int ShopTopLayer = 30;
    private Camera shopTopCamera;

    [Header("Movimento (arrasto com botão direito)")]
    [Tooltip("1 = o tabuleiro acompanha o mouse exatamente; aumente para arrastar mais rápido")]
    public float dragSensitivity = 1.2f;

    [Header("Zoom (scroll do mouse)")]
    [Tooltip("Quanto cada 'clique' do scroll altera o zoom")]
    public float zoomStep = 2.5f;
    [Tooltip("Velocidade da suavização do zoom (maior = responde mais rápido)")]
    public float zoomSmoothness = 12f;
    public float minZoom = 5f;
    public float maxZoom = 30f;
    [Tooltip("Zoom com que a partida COMEÇA (meia-altura visível no chão). Meio termo: nem colado nas cartas, nem vendo o tabuleiro inteiro de longe.")]
    public float startZoom = 16f;

    [Header("Limites do arrasto")]
    [Tooltip("Até onde o centro da tela pode ir para os LADOS (unidades do mundo; tabuleiro no centro)")]
    public float panLimitX = 30f;
    [Tooltip("Até onde o centro da tela pode ir para FRENTE/TRÁS (as mãos ficam em ±29.5)")]
    public float panLimitZ = 36f;

    [Header("Inclinação (visão do jogador)")]
    [Tooltip("Ângulo em graus a partir do chão: 90 = de cima (só o topo da cabeça das figuras); 45 = câmera mais deitada, vendo as figuras de frente. Pode ajustar em play mode.")]
    [Range(25f, 90f)]
    public float tiltAngle = 45f;

    [Header("Perspectiva (profundidade real)")]
    [Tooltip("Abertura da lente em graus. Menor = mais 'achatada' (tele); maior = mais dramática. Pode ajustar em play mode.")]
    [Range(20f, 60f)]
    public float fieldOfView = 35f;

    private Vector3 lastMousePos;
    private Camera mainCamera;
    private float targetZoom;
    private float currentZoom;             // meia-altura visível (em unidades) no ponto do chão olhado
    private float appliedTilt = float.NaN; // última inclinação aplicada (NaN = nenhuma)
    private float appliedFov = float.NaN;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera != null)
        {
            // O zoom continua medido como "meia-altura visível no chão" (a antiga
            // orthographicSize) — minZoom/maxZoom/zoomStep valem como antes.
            // A partida começa no startZoom (meio termo), não no zoom máximo
            // salvo na cena, que mostrava o tabuleiro inteiro de longe.
            targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
            currentZoom = targetZoom;

            // PERSPECTIVA de verdade (estilo LoL). A ortográfica inclinada parecia
            // uma imagem entortada: sem "longe fica menor", o tabuleiro não tinha
            // profundidade e dava a impressão de flutuar no ar (feedback do Carlos)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;
            mainCamera.nearClipPlane = 1f;
            mainCamera.farClipPlane = 1000f;
            appliedFov = fieldOfView;

            // A cena guarda a câmera a 60° (quase de cima); aqui ela deita
            // para o ângulo configurado, mirando o mesmo ponto do tabuleiro
            ApplyTilt();

            // Câmera overlay da loja: filha da principal (acompanha posição,
            // rotação e lente), desenha SÓ a camada da loja limpando o depth
            mainCamera.cullingMask &= ~(1 << ShopTopLayer);
            GameObject overlay = new GameObject("ShopTopCamera");
            overlay.transform.SetParent(transform, false);
            shopTopCamera = overlay.AddComponent<Camera>();
            shopTopCamera.cullingMask = 1 << ShopTopLayer;
            shopTopCamera.fieldOfView = mainCamera.fieldOfView;
            shopTopCamera.nearClipPlane = mainCamera.nearClipPlane;
            shopTopCamera.farClipPlane = mainCamera.farClipPlane;
            shopTopCamera.eventMask = 0; // cliques continuam pela principal

            // URP: overlay entra no stack da câmera base
            // (clearDepth é read-only nesta versão da URP; overlay já limpa
            // o depth por padrão — é exatamente o que queremos)
            var overlayData = shopTopCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            var mainData = mainCamera.GetUniversalAdditionalCameraData();
            if (!mainData.cameraStack.Contains(shopTopCamera))
                mainData.cameraStack.Add(shopTopCamera);
        }
    }

    // Distância câmera→alvo que enquadra 'currentZoom' de meia-altura no alvo
    float DistanceForZoom()
    {
        return currentZoom / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // Ponto do chão (y=0) no centro da tela — âncora do tilt e do zoom
    bool GroundTarget(out Vector3 alvo)
    {
        Vector3 fwd = transform.forward;
        if (fwd.y > -0.01f) { alvo = Vector3.zero; return false; }
        alvo = transform.position + fwd * (transform.position.y / -fwd.y);
        return true;
    }

    // Re-aponta a câmera no ângulo configurado SEM mudar o ponto do chão que
    // ela olha — o tabuleiro continua enquadrado igual, só a inclinação muda.
    void ApplyTilt()
    {
        Vector3 alvoNoChao;
        if (!GroundTarget(out alvoNoChao)) return; // câmera não olha para baixo

        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
        transform.position = alvoNoChao - transform.forward * DistanceForZoom();
        appliedTilt = tiltAngle;
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null) return;

        // Sliders de inclinação/lente mexidos no Inspector durante o jogo: reaplica
        if (!float.IsNaN(appliedFov) && !Mathf.Approximately(appliedFov, fieldOfView))
        {
            mainCamera.fieldOfView = fieldOfView;
            if (shopTopCamera != null) shopTopCamera.fieldOfView = fieldOfView;
            appliedFov = fieldOfView;
            ApplyTilt(); // recoloca na distância certa para a lente nova
        }
        if (!float.IsNaN(appliedTilt) && !Mathf.Approximately(appliedTilt, tiltAngle))
        {
            ApplyTilt();
        }

        // ── Arrasto do mouse com botão direito ────────────────────────────
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePos - lastMousePos;

            // Converte pixels de tela em unidades de mundo no zoom atual (medido
            // no plano do tabuleiro): o tabuleiro "gruda" no mouse ao arrastar
            float worldPerPixel = (currentZoom * 2f) / mainCamera.pixelHeight;

            // Pan no PLANO DO CHÃO (altura constante). Mover no espaço local da
            // câmera inclinada abaixava a câmera ao arrastar para baixo, fazendo
            // o near plane cortar o topo das cartas.
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forwardOnGround = transform.up;
            forwardOnGround.y = 0f;
            float foreshorten = forwardOnGround.magnitude; // compressão visual do chão (câmera inclinada)
            if (foreshorten < 0.15f)
            {
                // Câmera quase de topo: usa o forward projetado no chão
                forwardOnGround = transform.forward;
                forwardOnGround.y = 0f;
                foreshorten = Mathf.Max(forwardOnGround.magnitude, 0.15f);
            }
            forwardOnGround.Normalize();

            Vector3 move = (-delta.x * right - (delta.y / foreshorten) * forwardOnGround)
                           * worldPerPixel * dragSensitivity;
            transform.position += move;

            // Limite do arrasto: o PONTO DO CHÃO no centro da tela não pode
            // sair da área do tabuleiro+mãos (antes dava para ir ao infinito)
            Vector3 alvoAtual;
            if (GroundTarget(out alvoAtual))
            {
                Vector3 preso = new Vector3(
                    Mathf.Clamp(alvoAtual.x, -panLimitX, panLimitX), 0f,
                    Mathf.Clamp(alvoAtual.z, -panLimitZ, panLimitZ));
                if (preso != alvoAtual) transform.position += preso - alvoAtual;
            }

            lastMousePos = currentMousePos;
        }

        // ── Zoom suave ─────────────────────────────────────────────────────
        float scroll = Mouse.current.scroll.ReadValue().y;

        // Mouse sobre a UI (ex: painel de Logs): o scroll é da UI, não do zoom
        if (scroll != 0 &&
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            scroll = 0;
        }

        if (scroll != 0)
        {
            // Usa só a direção: em algumas plataformas o scroll vem em passos de ±120
            targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);
        }

        if (!Mathf.Approximately(currentZoom, targetZoom))
        {
            // Em perspectiva o zoom é a câmera se APROXIMANDO/AFASTANDO do ponto
            // do chão no centro da tela (mesma sensação do zoom do LoL)
            Vector3 alvoNoChao;
            if (GroundTarget(out alvoNoChao))
            {
                currentZoom = Mathf.Lerp(
                    currentZoom,
                    targetZoom,
                    1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime));
                transform.position = alvoNoChao - transform.forward * DistanceForZoom();
            }
        }
    }
}
