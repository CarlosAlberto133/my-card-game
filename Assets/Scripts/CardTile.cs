using UnityEngine;

public class CardTile : MonoBehaviour
{
    public int row;
    public int column;
    public GameObject occupiedCard; // Carta que está neste tile

    private Renderer tileRenderer;
    private Color normalColor = new Color(0.8f, 0.8f, 0.8f);
    private Color hoverColor = new Color(1f, 1f, 0.6f);
    private Color validPlacementColor = new Color(0.3f, 1f, 0.3f); // Verde para tiles válidos
    private Color invalidPlacementColor = new Color(1f, 0.3f, 0.3f); // Vermelho para tiles inválidos
    private Color attackHighlightColor = new Color(1f, 0.92f, 0.22f); // Amarelo: inimigo ao alcance de ataque
    private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f); // Cinza para tiles ocupados

    private enum HL { None, Valid, Invalid, Attack }
    private HL highlight = HL.None;

    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            tileRenderer.material.color = normalColor;
        }
    }

    // Permite ao BoardManager tematizar o tile (tema espacial: tons escuros alternados)
    public void SetBaseColor(Color baseColor)
    {
        normalColor = baseColor;
        occupiedColor = new Color(baseColor.r * 0.55f, baseColor.g * 0.55f, baseColor.b * 0.55f);
        if (tileRenderer == null) tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null && highlight == HL.None)
        {
            tileRenderer.material.color = normalColor;
        }
    }

    public void Initialize(int row, int column)
    {
        this.row = row;
        this.column = column;
        gameObject.name = $"Tile_{row}_{column}";
    }

    void OnMouseEnter()
    {
        if (tileRenderer != null)
        {
            // Se está destacado, aumenta um pouco o brilho
            if (highlight == HL.Valid)
                tileRenderer.material.color = new Color(0.4f, 1f, 0.4f);   // Verde mais brilhante
            else if (highlight == HL.Attack)
                tileRenderer.material.color = new Color(1f, 0.98f, 0.45f); // Amarelo mais brilhante
            else if (highlight == HL.Invalid)
                tileRenderer.material.color = new Color(1f, 0.4f, 0.4f);   // Vermelho mais brilhante
            else if (occupiedCard == null)
                tileRenderer.material.color = hoverColor;
        }
    }

    void OnMouseExit()
    {
        if (tileRenderer != null)
        {
            // Restaura a cor baseado no estado
            if (highlight == HL.Valid)
                tileRenderer.material.color = validPlacementColor;    // Verde
            else if (highlight == HL.Attack)
                tileRenderer.material.color = attackHighlightColor;   // Amarelo
            else if (highlight == HL.Invalid)
                tileRenderer.material.color = invalidPlacementColor;  // Vermelho
            else if (occupiedCard != null)
                tileRenderer.material.color = occupiedColor;
            else
                tileRenderer.material.color = normalColor;
        }
    }

    void OnMouseDown()
    {
        // Clique sobre a UI (botões, popups): a UI consome o clique — não
        // posiciona carta em tile que esteja atrás de um botão
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // Se há uma carta selecionada, tenta colocar neste tile
        if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
        {
            GameManager.Instance.TryPlaceCardOnTile(this);
        }
        else
        {
            Debug.Log($"Clicou no tile: Row {row}, Column {column}");
        }
    }

    // Marca o tile como ocupado por uma carta
    public void OccupyTile(GameObject card)
    {
        occupiedCard = card;
        if (tileRenderer != null)
        {
            tileRenderer.material.color = occupiedColor;
        }
    }

    // Libera o tile
    public void FreeTile()
    {
        occupiedCard = null;
        if (tileRenderer != null)
        {
            tileRenderer.material.color = normalColor;
        }
    }

    public bool IsOccupied()
    {
        return occupiedCard != null;
    }

    // Define o destaque do tile (verde = válido, vermelho = inválido)
    public void SetHighlight(bool isValid)
    {
        highlight = isValid ? HL.Valid : HL.Invalid;

        if (tileRenderer != null)
        {
            tileRenderer.material.color = isValid ? validPlacementColor : invalidPlacementColor;
        }
    }

    // Destaque AMARELO: um inimigo neste tile está ao alcance de ataque da carta
    // selecionada (tem prioridade sobre o vermelho de "não pode mover pra cá")
    public void SetAttackHighlight()
    {
        highlight = HL.Attack;
        if (tileRenderer != null)
        {
            tileRenderer.material.color = attackHighlightColor;
        }
    }

    // Remove o destaque do tile
    public void ClearHighlight()
    {
        highlight = HL.None;

        if (tileRenderer != null)
        {
            if (occupiedCard != null)
            {
                tileRenderer.material.color = occupiedColor;
            }
            else
            {
                tileRenderer.material.color = normalColor;
            }
        }
    }
}
