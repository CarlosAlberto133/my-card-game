using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 30f;
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 30f;
    
    private Vector3 lastMousePos;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }

    void Update()
    {
        // Arrasto do mouse com botão direito
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePos - lastMousePos;
            transform.Translate(-delta * moveSpeed * Time.deltaTime * 0.01f);
            lastMousePos = currentMousePos;
        }

        // Zoom (scroll do mouse) - muda o tamanho da câmera
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0 && mainCamera != null)
        {
            mainCamera.orthographicSize -= scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize, minZoom, maxZoom);
        }
    }
}
