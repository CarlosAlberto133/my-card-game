using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
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

    private Vector3 lastMousePos;
    private Camera mainCamera;
    private float targetZoom;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera != null)
        {
            targetZoom = mainCamera.orthographicSize;

            // Câmera ortográfica: afastar ao longo do eixo dela NÃO muda a imagem,
            // só afasta o near plane — evita cortar o topo das cartas (que ficam
            // "em pé") quando elas estão na parte de baixo da tela
            transform.position -= transform.forward * 30f;
            mainCamera.nearClipPlane = 0.1f;
        }
    }

    void Update()
    {
        if (Mouse.current == null || mainCamera == null) return;

        // ── Arrasto do mouse com botão direito ────────────────────────────
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePos - lastMousePos;

            // Converte pixels de tela em unidades de mundo no zoom atual:
            // arrastar X pixels move o mundo X pixels — o tabuleiro "gruda" no mouse
            float worldPerPixel = (mainCamera.orthographicSize * 2f) / mainCamera.pixelHeight;

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

        if (!Mathf.Approximately(mainCamera.orthographicSize, targetZoom))
        {
            mainCamera.orthographicSize = Mathf.Lerp(
                mainCamera.orthographicSize,
                targetZoom,
                1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime));
        }
    }
}
