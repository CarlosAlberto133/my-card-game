using UnityEngine;
using UnityEngine.UI;

// Script para fazer o Canvas aparecer em ambas as câmeras do split screen
[RequireComponent(typeof(Canvas))]
public class DualCameraCanvas : MonoBehaviour
{
    public Camera camera1;
    public Camera camera2;

    private Canvas canvas;

    void Start()
    {
        canvas = GetComponent<Canvas>();

        // Se não atribuiu as câmeras, tenta encontrar automaticamente
        if (camera1 == null || camera2 == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            if (cameras.Length >= 2)
            {
                camera1 = cameras[0];
                camera2 = cameras[1];
                Debug.Log($"Câmeras encontradas: {camera1.name} e {camera2.name}");
            }
        }

        // Garante que está em Screen Space - Camera
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }

        // Define a câmera principal
        canvas.worldCamera = camera1;
    }

    void LateUpdate()
    {
        // Alternativamente, você pode adicionar um segundo Canvas para a segunda câmera
        // Mas para UI compartilhada, usar Screen Space - Overlay é melhor
    }
}
