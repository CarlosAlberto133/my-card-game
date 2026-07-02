using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movimento (legado - não usado)")]
    // A câmera agora move-se arrastando com o botão esquerdo (ver HandleDragPan).
    // Estes campos ficam apenas para compatibilidade com o SplitScreenManager.
    public float moveSpeed = 10f;
    public float edgeSize = 20f;

    [Header("Configurações de Zoom")]
    public float zoomSpeed = 1f;
    public float minZoom = 3f;
    public float maxZoom = 15f;

    [Header("Limites da Câmera (opcional)")]
    public bool useLimits = true;
    public float minX = -10f;
    public float maxX = 10f;
    public float minZ = -10f;
    public float maxZ = 10f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Calcula limites automaticamente baseado no tabuleiro se existir
        BoardManager board = FindObjectOfType<BoardManager>();
        if (board != null && useLimits)
        {
            CalculateCameraLimits(board);
        }
    }

    // Estado do arrasto da câmera (botão esquerdo)
    private bool isDragging = false;
    private Vector3 dragOrigin; // ponto do tabuleiro "agarrado" no início do arrasto

    void Update()
    {
        // Arrastar segurando o botão esquerdo move a câmera.
        // Um clique parado não move nada, por isso selecionar/atacar cartas continua a funcionar.
        HandleDragPan();

        // Zoom com o scroll, apenas quando o rato está sobre esta câmera
        if (IsMouseInViewport())
        {
            HandleZoom();
        }
    }

    bool IsMouseInViewport()
    {
        if (Mouse.current == null || cam == null) return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Converte posição do mouse para viewport space (0-1)
        // Considera o rect da câmera para split screen
        Rect viewportRect = cam.rect;

        float viewportMinX = viewportRect.x * Screen.width;
        float viewportMaxX = (viewportRect.x + viewportRect.width) * Screen.width;
        float viewportMinY = viewportRect.y * Screen.height;
        float viewportMaxY = (viewportRect.y + viewportRect.height) * Screen.height;

        return mousePosition.x >= viewportMinX && mousePosition.x <= viewportMaxX &&
               mousePosition.y >= viewportMinY && mousePosition.y <= viewportMaxY;
    }

    // Move a câmera arrastando com o botão esquerdo: o ponto do tabuleiro
    // agarrado no início fica "colado" ao cursor enquanto se arrasta.
    void HandleDragPan()
    {
        if (Mouse.current == null || cam == null)
            return;

        // Inicia o arrasto ao premir o botão esquerdo dentro desta viewport
        if (Mouse.current.leftButton.wasPressedThisFrame && IsMouseInViewport())
        {
            if (TryGetGroundPoint(out Vector3 startPoint))
            {
                dragOrigin = startPoint;
                isDragging = true;
            }
        }

        // Termina o arrasto quando o botão é solto
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // Enquanto arrasta, move a câmera para manter o ponto agarrado sob o cursor
        if (isDragging && Mouse.current.leftButton.isPressed)
        {
            if (TryGetGroundPoint(out Vector3 currentPoint))
            {
                Vector3 diff = dragOrigin - currentPoint;
                diff.y = 0f;

                Vector3 newPosition = transform.position + diff;

                // Aplica limites se habilitado
                if (useLimits)
                {
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
                }

                transform.position = newPosition;
            }
        }
    }

    // Devolve o ponto onde o cursor toca o plano do chão (Y = 0)
    bool TryGetGroundPoint(out Vector3 point)
    {
        point = Vector3.zero;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }
        return false;
    }

    void HandleZoom()
    {
        // Verifica se o mouse existe (necessário para novo Input System)
        if (Mouse.current == null || cam == null)
            return;

        // Lê o valor do scroll (eixo Y do scroll)
        Vector2 scrollDelta = Mouse.current.scroll.ReadValue();

        if (scrollDelta.y != 0)
        {
            // Calcula o novo zoom (size da câmera ortográfica)
            float zoomChange = -scrollDelta.y * zoomSpeed * 0.1f; // Ajustado para zoom mais rápido
            float newSize = cam.orthographicSize + zoomChange;

            // Aplica os limites de zoom
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }

    void CalculateCameraLimits(BoardManager board)
    {
        // Calcula o tamanho total do tabuleiro no plano XZ
        float totalWidth = (board.columns * board.tileSize) + ((board.columns - 1) * board.tileSpacing);
        float totalDepth = (board.rows * board.tileSize) + ((board.rows - 1) * board.tileSpacing);

        // Adiciona margem extra
        float margin = 3f;

        // Limites em X (horizontal)
        minX = -(totalWidth / 2f) - margin;
        maxX = (totalWidth / 2f) + margin;

        // Limites em Z (vertical) - ajustado para câmera isométrica em Z=-10
        // O centro do tabuleiro está em Z=0, então ajustamos em relação à distância da câmera
        minZ = -(totalDepth / 2f) - margin - 10f; // -10 é a posição inicial da câmera
        maxZ = (totalDepth / 2f) + margin - 10f;

        Debug.Log($"Limites da câmera configurados: X({minX} a {maxX}), Z({minZ} a {maxZ})");
    }
}
