using UnityEngine;

// Janela de sobreposição (picture-in-picture) que mostra a loja por cima do
// tabuleiro. Como as cartas da loja são objetos 3D, uma segunda câmera sobre
// elas mantém o clique de compra funcionando normalmente dentro da janela.
public class ShopOverlayView : MonoBehaviour
{
    public static ShopOverlayView Instance { get; private set; }

    // Região da tela ocupada pela janela da loja (x, y, largura, altura em % da tela)
    public Rect overlayRect = new Rect(0.32f, 0.15f, 0.36f, 0.70f);
    public float cameraDistance = 30f; // Folga grande: não corta o topo das cartas com zoom de hover

    private Camera shopCamera;
    private bool isOpen = false;

    public bool IsOpen { get { return isOpen; } }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        CardManager cardManager = CardManager.Instance;
        if (cardManager == null)
        {
            Debug.LogWarning("[ShopOverlay] CardManager não encontrado");
            return;
        }

        // Coleta as posições das cartas atualmente na loja
        // (ignora cartas já compradas: elas ficam na lista até o próximo refresh)
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < cardManager.numberOfCards; i++)
        {
            GameObject shopCard = cardManager.GetShopCard(i);
            if (shopCard == null) continue;
            CardDisplay display = shopCard.GetComponent<CardDisplay>();
            if (display == null || !display.isInShop) continue;
            sum += shopCard.transform.position;
            count++;
        }

        if (count == 0)
        {
            Debug.Log("[ShopOverlay] Nenhuma carta na loja no momento");
            return;
        }

        Vector3 center = sum / count;

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[ShopOverlay] Câmera principal não encontrada");
            return;
        }

        if (shopCamera == null)
        {
            GameObject camObj = new GameObject("ShopOverlayCamera");
            camObj.transform.SetParent(transform, false);
            shopCamera = camObj.AddComponent<Camera>();
            shopCamera.orthographic = true;
            shopCamera.clearFlags = CameraClearFlags.SolidColor;
            shopCamera.backgroundColor = new Color(0.08f, 0.08f, 0.14f);
            shopCamera.rect = overlayRect;
        }

        // Mesma orientação da câmera principal, posicionada sobre o centro da loja
        shopCamera.transform.rotation = mainCam.transform.rotation;
        shopCamera.transform.position = center - shopCamera.transform.forward * cameraDistance;
        shopCamera.depth = mainCam.depth + 10; // Desenha por cima da câmera principal

        // Ajusta o zoom para caber todas as cartas da loja (com margem)
        float halfW = 0f;
        float halfH = 0f;
        for (int i = 0; i < cardManager.numberOfCards; i++)
        {
            GameObject shopCard = cardManager.GetShopCard(i);
            if (shopCard == null) continue;
            CardDisplay display = shopCard.GetComponent<CardDisplay>();
            if (display == null || !display.isInShop) continue;
            Vector3 local = shopCamera.transform.InverseTransformPoint(shopCard.transform.position);
            halfW = Mathf.Max(halfW, Mathf.Abs(local.x));
            halfH = Mathf.Max(halfH, Mathf.Abs(local.y));
        }
        halfW += 2.4f; // margem: metade da largura de uma carta + folga
        halfH += 2.8f; // margem: metade da altura de uma carta + folga
        float aspect = shopCamera.aspect > 0.01f ? shopCamera.aspect : 1f;
        shopCamera.orthographicSize = Mathf.Max(halfH, halfW / aspect);

        shopCamera.gameObject.SetActive(true);
        isOpen = true;
        Debug.Log("[ShopOverlay] Loja aberta");
    }

    public void Close()
    {
        if (shopCamera != null)
        {
            shopCamera.gameObject.SetActive(false);
        }
        isOpen = false;
        Debug.Log("[ShopOverlay] Loja fechada");
    }
}
