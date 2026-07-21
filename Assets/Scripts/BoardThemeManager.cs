using UnityEngine;

public enum BoardTheme { Space = 0, Tabletop = 1, Forest = 2, Teste = 3 }

// Decide e aplica a temática visual da cena de jogo: ESPAÇO (a original),
// MESA DE RPG (medieval — tabuleiro sobre uma mesa de madeira com miniaturas)
// ou FLORESTA (clareira com chão de terra cercada de árvores).
// O ANFITRIÃO escolhe o mapa nos checkboxes da sala (padrão: Mesa de RPG); a
// escolha viaja aos dois clientes pela room property "theme" (0=Espaço,
// 1=Mesa, 2=Floresta), então os dois veem o MESMO cenário. Offline usa o
// padrão (Mesa de RPG).
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

            // Mapa escolhido pelo anfitrião na sala (0 = Espaço, 1 = Mesa de
            // RPG, 2 = Floresta). Sem a property (build antigo): Mesa de RPG.
            object t = PhotonNetwork.room != null && PhotonNetwork.room.CustomProperties != null
                ? PhotonNetwork.room.CustomProperties["theme"] : null;
            int ti2 = t is int ? (int)t : 1;
            theme = ti2 == 0 ? BoardTheme.Space
                  : ti2 == 2 ? BoardTheme.Forest
                  : ti2 == 3 ? BoardTheme.Teste
                  : BoardTheme.Tabletop;
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
        ForestEnvironment.Clear();
        RemoveAdoptedFloor();

        // O cenário montado à mão só aparece no mapa Teste
        ShowTesteStage(theme == BoardTheme.Teste);

        if (theme == BoardTheme.Teste)
        {
            // Mapa MONTADO À MÃO no editor: o código não cria cenário nenhum,
            // só deixa as casas legíveis. Tudo o mais vem do "TesteStage".
            RetintTiles(new Color(1.00f, 0.97f, 0.92f), new Color(0.78f, 0.76f, 0.72f));
            if (TesteUseStoneTiles) BuildBoardFloor(themeSeed, false);

            // Fundo neutro: o que aparecer atrás é o que você montar na cena
            Camera cam = Camera.main;
            if (cam == null) cam = FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f);
            }
        }
        else if (theme == BoardTheme.Space)
        {
            // Pedra escura espacial (as cores originais do BoardManager)
            RetintTiles(new Color(0.30f, 0.33f, 0.43f), new Color(0.22f, 0.25f, 0.34f));
            SpaceBackground.Ensure();
        }
        else if (theme == BoardTheme.Forest)
        {
            // Casas de PEDRA lisas iguais às da Mesa de RPG (pedido do Carlos
            // — as de terra liam mal), com xadrez de MAIS contraste que o da
            // Mesa (na grama o campo se perdia; junto com a cordilheira de
            // pedras, a grade fica óbvia)
            RetintTiles(new Color(1.00f, 0.97f, 0.86f), new Color(0.70f, 0.68f, 0.52f));
            BuildBoardFloor(themeSeed, false);
            ForestEnvironment.Build(themeSeed);
        }
        else
        {
            // Casas de PEDRA de verdade (peças do KayKit Dungeon) sobre a mesa,
            // em tons quentes alternados (xadrez sutil mantém a grade legível)
            RetintTiles(new Color(1.00f, 0.97f, 0.92f), new Color(0.78f, 0.76f, 0.72f));
            BuildBoardFloor(themeSeed, false);
            TabletopEnvironment.Build(themeSeed);
        }
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  MAPA "TESTE" — montado à mão no editor                           ║
    // ║                                                                   ║
    // ║  Crie na cena SampleScene um objeto vazio chamado "TesteStage" e  ║
    // ║  monte o cenário DENTRO dele. Deixe-o DESATIVADO na cena: ele é   ║
    // ║  ligado sozinho quando a partida usa o mapa Teste, e continua     ║
    // ║  escondido nos outros mapas.                                      ║
    // ╚══════════════════════════════════════════════════════════════════╝
    public const string TesteStageName = "TesteStage";

    // true = as casas recebem as peças de pedra do KayKit (como na Mesa de RPG)
    // false = casas simples, para você colocar o visual que quiser por baixo
    public const bool TesteUseStoneTiles = true;

    // Liga/desliga o cenário montado à mão. Procura INCLUSIVE objetos
    // desativados (é assim que ele fica guardado na cena).
    static void ShowTesteStage(bool show)
    {
        GameObject stage = null;
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name == TesteStageName) { stage = t.gameObject; break; }
        }

        if (stage == null)
        {
            if (show)
                Debug.LogWarning($"[BoardTheme] Mapa Teste escolhido, mas não achei o objeto " +
                                 $"'{TesteStageName}' na cena — crie-o e monte o cenário dentro dele.");
            return;
        }

        stage.SetActive(show);
        if (show) Debug.Log($"[BoardTheme] Cenário '{TesteStageName}' ativado.");
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

    // Troca o visual de cada casa por uma peça real do KayKit — PEDRA (Mesa de
    // RPG) ou TERRA (Floresta). Variedade determinística pela seed (rachados/
    // mato — idênticos nos 2 clientes); clique e destaques continuam no
    // CardTile, que agora tinge a peça.
    static void BuildBoardFloor(int seed, bool dirt)
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

                double roll = rng.NextDouble();
                string model;
                if (dirt)
                {
                    // Terra batida: variantes lisas + algumas com matinho
                    if (roll < 0.15) model = "floor_dirt_small_weeds";
                    else if (roll < 0.36) model = "floor_dirt_small_B";
                    else if (roll < 0.57) model = "floor_dirt_small_C";
                    else if (roll < 0.78) model = "floor_dirt_small_D";
                    else model = "floor_dirt_small_A";
                }
                else
                {
                    // SÓ a peça LISA (pedido do Carlos): as variantes broken/
                    // weeds/decorated liam como "tile estranho/sumido" no
                    // tabuleiro. A variedade fica por conta do giro aleatório
                    model = "floor_tile_small";
                }

                float yRot = 90f * rng.Next(4); // giro aleatório (quebra a repetição)

                Renderer pieceRenderer;
                GameObject piece = DecorProps.PlaceFloor(tile.transform, model,
                    tile.transform.position, board.tileSize, yRot, out pieceRenderer);

                if (piece != null && pieceRenderer != null)
                    tile.AdoptVisual(piece, pieceRenderer);
            }
        }
        Debug.Log($"[BoardTheme] Casas de {(dirt ? "terra" : "pedra")} do KayKit aplicadas");
    }

    static void RemoveAdoptedFloor()
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
