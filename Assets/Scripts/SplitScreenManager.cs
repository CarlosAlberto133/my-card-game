using UnityEngine;

public class SplitScreenManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera player1Camera;
    public Camera player2Camera;

    [Header("Configurações")]
    public bool enableSplitScreen = true;

    void Start()
    {
        // Garante que ambas as câmeras tenham CameraController
        EnsureCameraControllers();
        SetupSplitScreen();
    }

    void EnsureCameraControllers()
    {
        // Adiciona CameraController na Player2Camera se não tiver
        if (player2Camera != null && player2Camera.GetComponent<CameraController>() == null)
        {
            // Copia as configurações da câmera 1 se ela tiver CameraController
            if (player1Camera != null)
            {
                CameraController cam1Controller = player1Camera.GetComponent<CameraController>();
                if (cam1Controller != null)
                {
                    CameraController cam2Controller = player2Camera.gameObject.AddComponent<CameraController>();
                    // Copia as configurações
                    cam2Controller.moveSpeed = cam1Controller.moveSpeed;
                    cam2Controller.edgeSize = cam1Controller.edgeSize;
                    cam2Controller.zoomSpeed = cam1Controller.zoomSpeed;
                    cam2Controller.minZoom = cam1Controller.minZoom;
                    cam2Controller.maxZoom = cam1Controller.maxZoom;
                    cam2Controller.useLimits = cam1Controller.useLimits;
                }
            }
        }
    }

    void SetupSplitScreen()
    {
        if (!enableSplitScreen)
        {
            // Modo single screen - desativa câmera do player 2
            if (player2Camera != null)
            {
                player2Camera.enabled = false;
            }
            if (player1Camera != null)
            {
                player1Camera.rect = new Rect(0, 0, 1, 1); // Tela cheia
                // Habilita controle de câmera no modo single screen
                EnableCameraControl(player1Camera, true);
            }
            return;
        }

        // Modo split screen vertical (esquerda/direita)
        if (player1Camera != null)
        {
            // Player 1: metade esquerda da tela
            player1Camera.rect = new Rect(0, 0, 0.5f, 1);
            player1Camera.enabled = true;
            // Mantém controle de câmera ativo no split screen
            EnableCameraControl(player1Camera, true);
        }

        if (player2Camera != null)
        {
            // Player 2: metade direita da tela
            player2Camera.rect = new Rect(0.5f, 0, 0.5f, 1);
            player2Camera.enabled = true;
            // Mantém controle de câmera ativo no split screen
            EnableCameraControl(player2Camera, true);
        }

        Debug.Log("Split screen configurado: Player 1 (esquerda) | Player 2 (direita)");
    }

    void EnableCameraControl(Camera cam, bool enable)
    {
        if (cam == null) return;

        CameraController controller = cam.GetComponent<CameraController>();
        if (controller != null)
        {
            controller.enabled = enable;
        }
    }

    public void ToggleSplitScreen()
    {
        enableSplitScreen = !enableSplitScreen;
        SetupSplitScreen();
    }
}
