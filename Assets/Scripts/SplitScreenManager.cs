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
        SetupSplitScreen();
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
            }
            return;
        }

        // Modo split screen vertical (esquerda/direita)
        if (player1Camera != null)
        {
            // Player 1: metade esquerda da tela
            player1Camera.rect = new Rect(0, 0, 0.5f, 1);
            player1Camera.enabled = true;
        }

        if (player2Camera != null)
        {
            // Player 2: metade direita da tela
            player2Camera.rect = new Rect(0.5f, 0, 0.5f, 1);
            player2Camera.enabled = true;
        }

        Debug.Log("Split screen configurado: Player 1 (esquerda) | Player 2 (direita)");
    }

    public void ToggleSplitScreen()
    {
        enableSplitScreen = !enableSplitScreen;
        SetupSplitScreen();
    }
}
