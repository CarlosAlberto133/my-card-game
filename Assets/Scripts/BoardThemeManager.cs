using UnityEngine;

public enum BoardTheme { Space = 0, Tabletop = 1 }

// Decide e aplica a temática visual da cena de jogo: ESPAÇO (a original) ou
// MESA DE RPG (medieval — tabuleiro sobre uma mesa de madeira com miniaturas).
// O ANFITRIÃO escolhe o mapa nos checkboxes da sala (padrão: Mesa de RPG); a
// escolha viaja aos dois clientes pela room property "theme" (0=Espaço, 1=Mesa),
// então os dois veem o MESMO cenário. Offline usa o padrão (Mesa de RPG).
//
// IMPORTANTE (lockstep): nada aqui usa UnityEngine.Random. A decoração usa
// System.Random próprio (com a seed da partida — mesas idênticas nos 2 lados);
// consumir sorteios do stream global entre o InitState da loja e o spawn
// dessincronizaria as cartas.
public class BoardThemeManager : MonoBehaviour
{
    private static BoardThemeManager instance;
    private static int themeSeed = 0;   // 0 = ainda sem seed definida
    private static bool applied = false;

    public static BoardTheme? Current { get; private set; }

    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("BoardThemeManager");
        instance = go.AddComponent<BoardThemeManager>();
    }

    void Awake()
    {
        // Cena nova (revanche/voltar ao lobby e jogar de novo): estado limpo
        applied = false;
        themeSeed = 0;
        Current = null;
    }

    // Chamado quando a seed sincronizada é conhecida (master ao gerar, P2 via
    // RPC_SetGameSeed, e no reinício de partida via DoRestart)
    public static void SetSeed(int seed)
    {
        Ensure(); // antes de gravar (o Awake de uma instância nova zera o estado)
        themeSeed = seed;
        applied = false; // reaplica — a temática pode ter mudado com a seed nova
    }

    void Update()
    {
        if (applied) return;

        // Espera o tabuleiro existir (precisamos re-tingir os tiles)
        BoardManager board = BoardManager.Instance;
        if (board == null || board.GetTile(0, 0) == null) return;

        BoardTheme theme;
        if (PhotonNetwork.inRoom)
        {
            // Espera a seed sincronizada (o P2 recebe ~1s depois de entrar) —
            // ela alimenta o System.Random da decoração, idêntica nos 2 lados
            if (themeSeed == 0) return;

            // Mapa escolhido pelo anfitrião na sala (0 = Espaço, 1 = Mesa de RPG).
            // Sem a property (build antigo/caso raro): padrão Mesa de RPG.
            object t = PhotonNetwork.room != null && PhotonNetwork.room.CustomProperties != null
                ? PhotonNetwork.room.CustomProperties["theme"] : null;
            theme = (t is int ti && ti == 0) ? BoardTheme.Space : BoardTheme.Tabletop;
        }
        else
        {
            // Offline: padrão Mesa de RPG (o mesmo default do checkbox da sala)
            if (themeSeed == 0) themeSeed = new System.Random().Next(1, 100000);
            theme = BoardTheme.Tabletop;
        }

        Apply(theme);
        applied = true;
    }

    void Apply(BoardTheme theme)
    {
        Current = theme;
        Debug.Log($"[BoardTheme] Temática da partida: {theme} (seed {themeSeed})");

        // Derruba a decoração anterior (troca de temática num reinício)
        SpaceBackground.Clear();
        TabletopEnvironment.Clear();

        if (theme == BoardTheme.Space)
        {
            // Pedra escura espacial (as cores originais do BoardManager).
            // Remove as peças de pedra de um eventual tema Mesa anterior
            RemoveStoneFloor();
            RetintTiles(new Color(0.30f, 0.33f, 0.43f), new Color(0.22f, 0.25f, 0.34f));
            SpaceBackground.Ensure();
        }
        else
        {
            // Casas de PEDRA de verdade (peças do KayKit Dungeon) sobre a mesa,
            // em tons quentes alternados (xadrez sutil mantém a grade legível)
            RetintTiles(new Color(1.00f, 0.97f, 0.92f), new Color(0.78f, 0.76f, 0.72f));
            BuildStoneFloor(themeSeed);
            TabletopEnvironment.Build(themeSeed);
        }
    }

    static void RetintTiles(Color a, Color b)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                CardTile tile = board.GetTile(row, col);
                if (tile != null)
                    tile.SetBaseColor((row + col) % 2 == 0 ? a : b);
            }
        }
    }

    // Troca o visual de cada casa pelo ladrilho de pedra do KayKit. Variedade
    // determinística pela seed (rachados/mato — idênticos nos 2 clientes);
    // clique e destaques continuam no CardTile, que agora tinge a peça.
    static void BuildStoneFloor(int seed)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        System.Random rng = new System.Random(seed * 31 + 11);

        for (int row = 0; row < board.rows; row++)
        {
            for (int col = 0; col < board.columns; col++)
            {
                CardTile tile = board.GetTile(row, col);
                if (tile == null) continue;

                // Maioria lisa; algumas rachadas/com mato para o chão ter vida
                double roll = rng.NextDouble();
                string model;
                if (roll < 0.07) model = "floor_tile_small_broken_A";
                else if (roll < 0.13) model = "floor_tile_small_broken_B";
                else if (roll < 0.20) model = "floor_tile_small_weeds_A";
                else if (roll < 0.26) model = "floor_tile_small_weeds_B";
                else if (roll < 0.31) model = "floor_tile_small_decorated";
                else model = "floor_tile_small";

                float yRot = 90f * rng.Next(4); // giro aleatório (quebra a repetição)

                Renderer pieceRenderer;
                GameObject piece = DecorProps.PlaceFloor(tile.transform, model,
                    tile.transform.position, board.tileSize, yRot, out pieceRenderer);

                if (piece != null && pieceRenderer != null)
                    tile.AdoptVisual(piece, pieceRenderer);
            }
        }
        Debug.Log("[BoardTheme] Casas de pedra do KayKit aplicadas ao tabuleiro");
    }

    static void RemoveStoneFloor()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        for (int row = 0; row < board.rows; row++)
            for (int col = 0; col < board.columns; col++)
            {
                CardTile tile = board.GetTile(row, col);
                if (tile != null) tile.ClearAdoptedVisual();
            }
    }
}
