using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 10f;
    public float edgeSize = 20f; // Pixels de distância da borda para ativar movimento

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

    void Update()
    {
        // Só processa input se o mouse estiver na área desta câmera
        if (IsMouseInViewport())
        {
            MoveCameraWithMouse();
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

    void MoveCameraWithMouse()
    {
        // Verifica se o mouse existe (necessário para novo Input System)
        if (Mouse.current == null || cam == null)
            return;

        Vector3 movement = Vector3.zero;
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Calcula os limites do viewport desta câmera
        Rect viewportRect = cam.rect;
        float viewportMinX = viewportRect.x * Screen.width;
        float viewportMaxX = (viewportRect.x + viewportRect.width) * Screen.width;
        float viewportMinY = viewportRect.y * Screen.height;
        float viewportMaxY = (viewportRect.y + viewportRect.height) * Screen.height;

        // Verifica borda esquerda (relativa ao viewport)
        if (mousePosition.x < viewportMinX + edgeSize)
        {
            movement.x = -1;
        }
        // Verifica borda direita (relativa ao viewport)
        else if (mousePosition.x > viewportMaxX - edgeSize)
        {
            movement.x = 1;
        }

        // Verifica borda inferior (relativa ao viewport)
        if (mousePosition.y < viewportMinY + edgeSize)
        {
            movement.z = -1;
        }
        // Verifica borda superior (relativa ao viewport)
        else if (mousePosition.y > viewportMaxY - edgeSize)
        {
            movement.z = 1;
        }

        // Aplica o movimento (no plano XZ para câmera top-down)
        if (movement != Vector3.zero)
        {
            Vector3 newPosition = transform.position + movement * moveSpeed * Time.deltaTime;

            // Aplica limites se habilitado
            if (useLimits)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.z = Mathf.Clamp(newPosition.z, minZ, maxZ);
            }

            transform.position = newPosition;
        }
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
