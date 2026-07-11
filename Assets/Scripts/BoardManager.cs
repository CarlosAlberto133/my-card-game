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

                    // Tema espacial: pedra escura em xadrez sutil
                    tile.SetBaseColor((row + col) % 2 == 0
                        ? new Color(0.30f, 0.33f, 0.43f)
                        : new Color(0.22f, 0.25f, 0.34f));
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
            // Textura rochosa procedural (a cor do CardTile tinge por cima)
            Texture2D rock = GetRockTexture();
            mr.material.mainTexture = rock;
            mr.material.SetTexture("_BaseMap", rock);

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

    // Textura de pedra gerada por código (ruído Perlin em camadas), compartilhada
    // por todos os tiles — valores claros (0.7-1.0) para a cor do tile dominar
    private static Texture2D rockTexture;

    static Texture2D GetRockTexture()
    {
        if (rockTexture != null) return rockTexture;

        const int size = 128;
        rockTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Duas oitavas de Perlin: forma geral + granulado fino
                float n1 = Mathf.PerlinNoise(x * 0.045f, y * 0.045f);
                float n2 = Mathf.PerlinNoise(x * 0.18f + 37f, y * 0.18f + 91f);
                float v = 0.72f + 0.20f * n1 + 0.08f * n2;
                rockTexture.SetPixel(x, y, new Color(v, v, v, 1f));
            }
        }

        rockTexture.Apply();
        return rockTexture;
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

    // Retorna todas as cartas no tabuleiro
    public System.Collections.Generic.List<CardDisplay> GetAllCards()
    {
        var result = new System.Collections.Generic.List<CardDisplay>();
        if (board == null) return result;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                CardTile tile = board[row, col];
                if (tile != null && tile.occupiedCard != null)
                {
                    CardDisplay card = tile.occupiedCard.GetComponent<CardDisplay>();
                    if (card != null) result.Add(card);
                }
            }
        }
        return result;
    }

    // Retorna cartas de um jogador específico
    public System.Collections.Generic.List<CardDisplay> GetCardsByOwner(int ownerPlayerNumber)
    {
        var result = new System.Collections.Generic.List<CardDisplay>();
        foreach (var card in GetAllCards())
            if (card.ownerPlayerNumber == ownerPlayerNumber) result.Add(card);
        return result;
    }

    // Busca uma carta pelo instance ID
    public CardDisplay FindCardByInstanceID(int instanceID)
    {
        foreach (var card in GetAllCards())
            if (card.gameObject.GetInstanceID() == instanceID) return card;
        return null;
    }

    // Conta cartas de uma classe específica de um jogador
    public int CountCardsByClass(int ownerPlayerNumber, CardClass cardClass)
    {
        int count = 0;
        foreach (var card in GetCardsByOwner(ownerPlayerNumber))
            if (card.card.cardClass == cardClass) count++;
        return count;
    }

    // Verifica se uma classe tem cartas em campo
    public bool HasClassOnBoard(int ownerPlayerNumber, CardClass cardClass)
    {
        return CountCardsByClass(ownerPlayerNumber, cardClass) > 0;
    }

    // Retorna um tile adjacente em uma direção específica
    public CardTile GetAdjacentTile(CardTile fromTile, string direction, int ownerPlayerNumber)
    {
        if (fromTile == null || board == null) return null;

        int row = fromTile.row;
        int col = fromTile.column;

        // Determina a direção baseado no dono (P1 = bottom to top, P2 = top to bottom)
        int rowOffset = 0;
        if (direction == "forward")
        {
            rowOffset = (ownerPlayerNumber == 1) ? -1 : 1;
        }

        int newRow = row + rowOffset;

        // Verifica limites
        if (newRow < 0 || newRow >= rows || col < 0 || col >= columns)
            return null;

        return board[newRow, col];
    }

    // Encontra um tile vazio adjacente para spawnar cópia
    public CardTile FindAdjacentEmptyTile(CardTile fromTile, int ownerPlayerNumber)
    {
        if (fromTile == null || board == null) return null;

        int row = fromTile.row;
        int col = fromTile.column;

        // Tenta encontrar um tile vazio nas adjacências (esquerda, direita, frente)
        int[] colOffsets = { -1, 1, 0 };

        foreach (int colOffset in colOffsets)
        {
            int newCol = col + colOffset;
            if (newCol < 0 || newCol >= columns) continue;

            CardTile tile = board[row, newCol];
            if (tile != null && !tile.IsOccupied())
            {
                return tile;
            }
        }

        return null;
    }
}
