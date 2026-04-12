using UnityEngine;
using TMPro;

public class CardPrefabCreator : MonoBehaviour
{
    [Header("Configurações da Carta")]
    public float cardWidth = 1.8f;
    public float cardHeight = 2.5f;

    [ContextMenu("Create Card Prefab")]
    public void CreateCardPrefab()
    {
        // Cria o objeto raiz da carta
        GameObject cardRoot = new GameObject("CardPrefab");
        cardRoot.AddComponent<CardDisplay>();

        // Adiciona BoxCollider para interação
        BoxCollider collider = cardRoot.AddComponent<BoxCollider>();
        collider.size = new Vector3(cardWidth, 0.1f, cardHeight);
        collider.center = new Vector3(0, 0.05f, 0);

        // Cria a borda da carta primeiro (mais escura e maior)
        GameObject border = CreateQuad("Border", cardRoot.transform, cardWidth + 0.25f, cardHeight + 0.25f, -0.005f);
        Renderer borderRenderer = border.GetComponent<Renderer>();
        if (borderRenderer != null)
        {
            Material borderMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            borderMat.color = new Color(0.05f, 0.05f, 0.05f); // Preto bem escuro
            borderRenderer.material = borderMat;
        }

        // Cria o fundo da carta
        GameObject background = CreateQuad("Background", cardRoot.transform, cardWidth, cardHeight, 0);
        Renderer bgRenderer = background.GetComponent<Renderer>();
        if (bgRenderer != null)
        {
            Material bgMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bgMat.color = Color.white;
            bgRenderer.material = bgMat;
        }

        // Cria a área de artwork (imagem da carta) - ajustado para a rotação
        GameObject artwork = CreateQuad("Artwork", cardRoot.transform, cardWidth * 0.9f, cardHeight * 0.5f, 0.01f);
        artwork.transform.localPosition = new Vector3(0, 0.01f, -cardHeight * 0.15f); // Invertido para compensar a rotação do texto

        // Adiciona material ao artwork
        Renderer artworkRenderer = artwork.GetComponent<Renderer>();
        if (artworkRenderer != null)
        {
            Material artMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            artMat.color = new Color(0.9f, 0.9f, 0.9f);
            artworkRenderer.material = artMat;
        }

        // Cria textos usando TextMeshPro
        CreateCardTexts(cardRoot.transform);

        Debug.Log("Card Prefab criado! Salve como prefab arrastando para a pasta Assets.");
    }

    GameObject CreateQuad(string name, Transform parent, float width, float height, float yOffset)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent);
        quad.transform.localPosition = new Vector3(0, yOffset, 0);
        quad.transform.localRotation = Quaternion.Euler(90, 0, 0);
        quad.transform.localScale = new Vector3(width, height, 1);

        // Remove o collider do quad
        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null) DestroyImmediate(quadCollider);

        return quad;
    }

    void CreateCardTexts(Transform parent)
    {
        // Nome da carta (topo) - invertido no Z por causa da rotação
        CreateText("CardNameText", parent,
            new Vector3(0, 0.02f, -cardHeight * 0.45f),
            3f, TextAlignmentOptions.Center, Color.black);

        // Ataque (direita inferior - invertido) 
        CreateText("AttackText", parent,
            new Vector3(cardWidth * 0.3f, 0.02f, cardHeight * 0.35f),
            4f, TextAlignmentOptions.Center, new Color(0.8f, 0.1f, 0.1f)); // Vermelho escuro

        // Escudo (centro inferior - invertido)
        CreateText("ShieldText", parent,
            new Vector3(0, 0.02f, cardHeight * 0.35f),
            4f, TextAlignmentOptions.Center, new Color(0.1f, 0.5f, 0.9f)); // Azul

        // Vida (esquerda inferior - invertido)
        CreateText("HealthText", parent,
            new Vector3(-cardWidth * 0.3f, 0.02f, cardHeight * 0.35f),
            4f, TextAlignmentOptions.Center, new Color(0.1f, 0.8f, 0.1f)); // Verde

        // Tier (canto superior esquerdo - invertido)
        CreateText("TierText", parent,
            new Vector3(-cardWidth * 0.38f, 0.02f, -cardHeight * 0.44f),
            2.5f, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0f)); // Dourado
    }

    GameObject CreateText(string name, Transform parent, Vector3 position, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        textObj.transform.localPosition = position;
        textObj.transform.localRotation = Quaternion.Euler(90, 180, 0); // Rotação ajustada para não ficar de cabeça para baixo

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = name.Contains("Name") ? "Card Name" : "00";
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.rectTransform.sizeDelta = new Vector2(cardWidth * 0.9f, size * 0.5f);
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return textObj;
    }
}
