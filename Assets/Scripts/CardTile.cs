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
            // Se há uma carta selecionada, mostra se o tile é válido
            if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
            {
                if (occupiedCard != null)
                {
                    tileRenderer.material.color = invalidPlacementColor; // Vermelho - ocupado
                }
                else if (row < 2)
                {
                    tileRenderer.material.color = validPlacementColor; // Verde - válido
                }
                else
                {
                    tileRenderer.material.color = invalidPlacementColor; // Vermelho - muito longe
                }
            }
            else
            {
                tileRenderer.material.color = hoverColor;
            }
        }
    }

    void OnMouseExit()
    {
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
}
