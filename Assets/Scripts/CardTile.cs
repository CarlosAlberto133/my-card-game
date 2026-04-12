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
    private Color occupiedColor = new Color(0.5f, 0.5f, 0.5f); // Cinza para tiles ocupados
    private bool isHighlighted = false;
    private bool isValidHighlight = false;

    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
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
            if (isHighlighted)
            {
                if (isValidHighlight)
                {
                    tileRenderer.material.color = new Color(0.4f, 1f, 0.4f); // Verde mais brilhante
                }
                else
                {
                    tileRenderer.material.color = new Color(1f, 0.4f, 0.4f); // Vermelho mais brilhante
                }
            }
            else if (occupiedCard == null)
            {
                tileRenderer.material.color = hoverColor;
            }
        }
    }

    void OnMouseExit()
    {
        if (tileRenderer != null)
        {
            // Restaura a cor baseado no estado
            if (isHighlighted)
            {
                if (isValidHighlight)
                {
                    tileRenderer.material.color = validPlacementColor; // Verde
                }
                else
                {
                    tileRenderer.material.color = invalidPlacementColor; // Vermelho
                }
            }
            else if (occupiedCard != null)
            {
                tileRenderer.material.color = occupiedColor;
            }
            else
            {
                tileRenderer.material.color = normalColor;
            }
        }
    }

    void OnMouseDown()
    {
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

    // Define o destaque do tile (verde ou vermelho)
    public void SetHighlight(bool isValid)
    {
        isHighlighted = true;
        isValidHighlight = isValid;

        if (tileRenderer != null)
        {
            if (isValid)
            {
                tileRenderer.material.color = validPlacementColor; // Verde
            }
            else
            {
                tileRenderer.material.color = invalidPlacementColor; // Vermelho
            }
        }
    }

    // Remove o destaque do tile
    public void ClearHighlight()
    {
        isHighlighted = false;
        isValidHighlight = false;

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
