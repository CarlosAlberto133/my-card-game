using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Configurações do Tabuleiro")]
    public int rows = 12;
    public int columns = 12;
    public float tileSize = 6.0f;
    public float tileSpacing = 0.6f;

    [Header("Prefab")]
    public GameObject tilePrefab;

    private CardTile[,] board;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CreateBoard();
    }

    void CreateBoard()
    {
        board = new CardTile[rows, columns];

        // Calcula a posição inicial para centralizar o tabuleiro
        float totalWidth = (columns * tileSize) + ((columns - 1) * tileSpacing);
        float totalDepth = (rows * tileSize) + ((rows - 1) * tileSpacing);

        // Usa plano XZ (chão) ao invés de XY
        Vector3 startPosition = transform.position - new Vector3(totalWidth / 2f, 0, totalDepth / 2f);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // Calcula a posição do tile no plano XZ (chão)
                Vector3 position = startPosition + new Vector3(
                    col * (tileSize + tileSpacing) + tileSize / 2f,
                    0,
                    row * (tileSize + tileSpacing) + tileSize / 2f
                );

                // Cria o tile
                GameObject tileObject = CreateTile(position);
                CardTile tile = tileObject.GetComponent<CardTile>();

                if (tile != null)
                {
                    tile.Initialize(row, col);
                    board[row, col] = tile;
                }
            }
        }

        Debug.Log($"Tabuleiro {rows}x{columns} criado com sucesso!");
    }

    GameObject CreateTile(Vector3 position)
    {
        GameObject tile;

        if (tilePrefab != null)
        {
            // Usa o prefab se fornecido
            tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
        }
        else
        {
            // Cria um tile básico com sprite
            tile = new GameObject("Tile");
            tile.transform.position = position;
            tile.transform.parent = transform;

            // Adiciona MeshRenderer e MeshFilter para 3D
            MeshRenderer mr = tile.AddComponent<MeshRenderer>();
            MeshFilter mf = tile.AddComponent<MeshFilter>();

            // Cria um quad (plano)
            mf.mesh = CreateQuadMesh();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mr.material.color = Color.white;

            // Ajusta o tamanho e rotação para ficar horizontal
            tile.transform.localScale = new Vector3(tileSize, 1, tileSize);
            tile.transform.rotation = Quaternion.Euler(0, 0, 0);

            // Adiciona BoxCollider para detecção de cliques
            BoxCollider collider = tile.AddComponent<BoxCollider>();
        }

        // Adiciona o componente CardTile se ainda não tiver
        if (tile.GetComponent<CardTile>() == null)
        {
            tile.AddComponent<CardTile>();
        }

        return tile;
    }

    // Cria um mesh quad simples
    Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };

        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }

    // Método para obter um tile específico
    public CardTile GetTile(int row, int column)
    {
        if (row >= 0 && row < rows && column >= 0 && column < columns)
        {
            return board[row, column];
        }
        return null;
    }

    // Limpa todas as cartas dos tiles
    public void ClearAllTiles()
    {
        if (board == null) return;

        int clearedCount = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                CardTile tile = board[row, col];
                if (tile != null && tile.occupiedCard != null)
                {
                    tile.occupiedCard = null;
                    clearedCount++;
                }
            }
        }

        Debug.Log($"BoardManager: {clearedCount} tiles limpos!");
    }
}
