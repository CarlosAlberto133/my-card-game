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
            targetZoom = mainCamera.orthographicSize;
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
            transform.Translate(-delta * worldPerPixel * dragSensitivity, Space.Self);

            lastMousePos = currentMousePos;
        }

        // ── Zoom suave ─────────────────────────────────────────────────────
        float scroll = Mouse.current.scroll.ReadValue().y;
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
