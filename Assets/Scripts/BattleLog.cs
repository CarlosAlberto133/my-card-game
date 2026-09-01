using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ════════════════════════════════════════════════════════════════════════════
//  DIÁRIO DE BATALHA — painel no canto inferior direito
//
//  Mostra tudo o que acontece na partida, em ordem: quem jogou qual carta em
//  qual casa, quem andou, quem comprou, quem atacou quem e qual efeito atingiu
//  qual alvo. O X do cabeçalho minimiza; minimizado vira só uma plaquinha no
//  canto, que reabre o painel ao ser clicada (igual à Magia da Torre).
//
//  REGRA DE OURO (lockstep): as chamadas de log ficam nos pontos que OS DOIS
//  clientes executam — dentro dos RPCs / dos métodos comuns —, nunca no clique
//  de quem jogou. Assim os dois jogadores leem o MESMO diário. Esta classe é
//  puramente visual: não guarda estado de jogo, não sorteia nada, não fala com
//  a rede.
//
//  ⚠️ Aprendido na marra (1ª versão não aparecia na tela):
//   - canvas PRÓPRIO, como a TowerMagicShopUI faz. FindObjectOfType<Canvas>()
//     pega qualquer canvas da cena, inclusive um world-space de carta.
//   - viewport com RectMask2D, NUNCA Mask. O Mask recorta pelos pixels da
//     imagem dele: com uma imagem quase transparente (alpha 0.001) ele some
//     com todo o conteúdo, e o painel fica vazio sem dar erro nenhum.
//   - nada de SetAsFirstSibling: isso jogava o painel para trás de tudo.
// ════════════════════════════════════════════════════════════════════════════
public class BattleLog : MonoBehaviour
{
    static BattleLog instance;

    // Quantas linhas ficam guardadas. Passou disso, a mais velha é apagada —
    // uma partida longa não pode virar milhares de objetos de UI.
    const int MaxLinhas = 80;

    // ── Paleta (mesma família do HUD medieval) ──────────────────────────
    static readonly Color Fundo = new Color(0.06f, 0.045f, 0.03f, 0.93f);
    static readonly Color Borda = new Color(0.96f, 0.77f, 0.32f, 0.45f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color Ink = new Color(0.90f, 0.86f, 0.78f);

    // Cores dos jogadores: as MESMAS da base de miniatura no tabuleiro, para o
    // jogador ligar a linha do diário à unidade que ele vê em campo
    const string HexP1 = "#6194FF";
    const string HexP2 = "#FF6657";
    const string HexNeutro = "#F5C451";

    // Canto inferior direito. O y sobe 104 para passar POR CIMA da faixa de
    // chips da torre (TowerChips: 80 de altura em y 14, mesmo canto).
    static readonly Vector2 Canto = new Vector2(-16f, 104f);
    static readonly Vector2 TamPainel = new Vector2(340f, 420f);
    static readonly Vector2 TamChip = new Vector2(215f, 40f);

    GameObject canvasGo, panel, chipMinimizado;
    RectTransform conteudoRt;
    ScrollRect scroll;
    readonly Queue<GameObject> linhas = new Queue<GameObject>();

    // Escolha do jogador: sobrevive a linhas novas (uma jogada não pode
    // reabrir o painel que ele fechou). Começa FECHADO (pedido do Carlos — a
    // tela nasce limpa e quem quiser acompanhar abre na plaquinha).
    static bool minimizado = true;

    static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("BattleLog");
        instance = go.AddComponent<BattleLog>();
    }

    // ════════════════════════════════════════════════════════════════════
    //  API — o que o resto do jogo chama
    // ════════════════════════════════════════════════════════════════════

    // Linha crua, já formatada. `playerNumber` 0 = evento neutro (sistema).
    public static void Add(int playerNumber, string mensagem)
    {
        if (string.IsNullOrEmpty(mensagem)) return;
        Ensure();
        if (instance != null) instance.Escrever(playerNumber, mensagem);
    }

    // Marco neutro (troca de round, começo de turno...): linha dourada
    public static void Marco(string mensagem)
    {
        Add(0, mensagem);
    }

    // Limpa o diário (partida nova / revanche)
    public static void Clear()
    {
        if (instance == null) return;
        while (instance.linhas.Count > 0)
        {
            GameObject velha = instance.linhas.Dequeue();
            if (velha != null) Destroy(velha);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Eventos do jogo — um método por ação, para os pontos de chamada
    //  ficarem numa linha só e o texto viver todo aqui
    // ════════════════════════════════════════════════════════════════════

    public static void Jogou(int player, string carta, int row, int col)
    {
        Add(player, Nome(player) + " jogou " + Carta(carta) + " em " + Casa(row, col));
    }

    public static void Moveu(int player, string carta,
                             int deRow, int deCol, int paraRow, int paraCol)
    {
        Add(player, Nome(player) + " moveu " + Carta(carta) + " de " +
            Casa(deRow, deCol) + " para " + Casa(paraRow, paraCol));
    }

    public static void Comprou(int player, string carta, int custo)
    {
        Add(player, Nome(player) + " comprou " + Carta(carta) +
            (custo > 0 ? " por " + custo + " de ouro" : " (grátis)"));
    }

    public static void Atacou(int player, string atacante, string alvo)
    {
        Add(player, Nome(player) + " atacou " + Carta(alvo) + " com " + Carta(atacante));
    }

    // `alvo` nulo/vazio = efeito sem alvo escolhido
    public static void Efeito(int player, string carta, string alvo)
    {
        string txt = Nome(player) + " usou o efeito de " + Carta(carta);
        if (!string.IsNullOrEmpty(alvo)) txt += " em " + Carta(alvo);
        Add(player, txt);
    }

    // ── Formatadores auxiliares ─────────────────────────────────────────

    // Nick do jogador (o custom definido no lobby chega aqui pelo PlayerData).
    // Defensivo de propósito: o diário nunca pode derrubar uma partida.
    public static string Nome(int playerNumber)
    {
        if (TurnManager.Instance != null)
        {
            PlayerData p = TurnManager.Instance.GetPlayer(playerNumber);
            if (p != null && !string.IsNullOrEmpty(p.playerName)) return p.playerName;
        }
        return "Jogador " + playerNumber;
    }

    // Casa em notação de tabuleiro: coluna vira letra, fileira vira número
    // (A1..G7). Números crus de row/col não dizem nada para quem joga.
    public static string Casa(int row, int col)
    {
        char letra = (char)('A' + Mathf.Clamp(col, 0, 25));
        return letra.ToString() + (row + 1);
    }

    // Nome de carta destacado (as cartas são o assunto de quase toda linha)
    public static string Carta(string nome)
    {
        return "<color=#F0E2C0><b>" + nome + "</b></color>";
    }

    // ════════════════════════════════════════════════════════════════════
    //  Desenho
    // ════════════════════════════════════════════════════════════════════

    void Escrever(int playerNumber, string mensagem)
    {
        if (panel == null) Build();
        if (panel == null || conteudoRt == null) return;

        string hex = playerNumber == 1 ? HexP1
                   : playerNumber == 2 ? HexP2 : HexNeutro;

        GameObject go = new GameObject("Linha", typeof(RectTransform));
        go.transform.SetParent(conteudoRt, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 14f;
        tmp.color = Ink;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        UIFonts.Set(tmp, UIFonts.Body);

        // Trilho colorido na frente da linha: o olho separa as jogadas dos
        // dois lados sem ler o nome. É um "|" de propósito — caractere ASCII,
        // presente em qualquer fonte; símbolo bonitinho fora do Latin-1 vira
        // quadradinho vazio se a fonte medieval não tiver o glifo.
        tmp.text = "<color=" + hex + ">|</color> " + mensagem;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 16f;

        linhas.Enqueue(go);
        while (linhas.Count > MaxLinhas)
        {
            GameObject velha = linhas.Dequeue();
            if (velha != null) Destroy(velha);
        }

        RolarParaOFim();
    }

    void RolarParaOFim()
    {
        // Só faz sentido (e só funciona) com o painel aberto: layout de objeto
        // inativo não é recalculado
        if (panel == null || !panel.activeSelf || scroll == null) return;
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 0f;
    }

    // ── Minimizar / reabrir ─────────────────────────────────────────────

    void AplicarEstado()
    {
        if (panel != null) panel.SetActive(!minimizado);
        if (chipMinimizado != null) chipMinimizado.SetActive(minimizado);
        if (!minimizado) RolarParaOFim();
    }

    void Minimizar()
    {
        minimizado = true;
        AplicarEstado();
    }

    void Reabrir()
    {
        minimizado = false;
        AplicarEstado();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Construção
    // ════════════════════════════════════════════════════════════════════

    Canvas EnsureCanvas()
    {
        if (canvasGo == null)
        {
            // Canvas PRÓPRIO (receita da TowerMagicShopUI): não depende de
            // achar o canvas certo na cena nem de ordem de criação.
            // sortingOrder abaixo do 700 da torre — quando a magia da torre
            // abrir, ela fica por cima do diário, como deve ser.
            canvasGo = new GameObject("BattleLogCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas cv = canvasGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 690;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
        return canvasGo.GetComponent<Canvas>();
    }

    void Build()
    {
        Canvas cv = EnsureCanvas();

        // ── Painel ───────────────────────────────────────────────────────
        panel = new GameObject("BattleLogPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(cv.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1f, 0f);
        prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(1f, 0f);
        prt.anchoredPosition = Canto;
        prt.sizeDelta = TamPainel;
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), Fundo);
        LobbySprites.AddRing(panel.transform, Borda);

        // ── Cabeçalho ────────────────────────────────────────────────────
        GameObject titulo = new GameObject("Titulo", typeof(RectTransform));
        titulo.transform.SetParent(panel.transform, false);
        RectTransform trt = titulo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -10f);
        trt.sizeDelta = new Vector2(-52f, 24f); // sobra para o X à direita

        TextMeshProUGUI tt = titulo.AddComponent<TextMeshProUGUI>();
        tt.text = "» DIÁRIO DE BATALHA";
        tt.fontSize = 15f;
        tt.fontStyle = FontStyles.Bold;
        tt.color = Gold;
        tt.alignment = TextAlignmentOptions.Left;
        tt.raycastTarget = false;
        UIFonts.Set(tt, UIFonts.Title);

        // X de minimizar, no canto do cabeçalho
        GameObject fechar = MakeBotao(panel.transform, "X", new Vector2(26f, 26f), 15, Minimizar);
        RectTransform frt = fechar.GetComponent<RectTransform>();
        frt.anchorMin = new Vector2(1f, 1f);
        frt.anchorMax = new Vector2(1f, 1f);
        frt.pivot = new Vector2(1f, 1f);
        frt.anchoredPosition = new Vector2(-10f, -8f);

        // ── Área rolável ─────────────────────────────────────────────────
        // RectMask2D (e NÃO Mask): recorta pelo retângulo, sem depender de
        // uma imagem — foi o Mask que fez a 1ª versão nascer vazia
        GameObject view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        view.transform.SetParent(panel.transform, false);
        RectTransform vrt = view.GetComponent<RectTransform>();
        vrt.anchorMin = new Vector2(0f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = new Vector2(12f, 12f);
        vrt.offsetMax = new Vector2(-12f, -38f); // abaixo do cabeçalho

        GameObject conteudo = new GameObject("Conteudo",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        conteudo.transform.SetParent(view.transform, false);
        conteudoRt = conteudo.GetComponent<RectTransform>();
        conteudoRt.anchorMin = new Vector2(0f, 1f);
        conteudoRt.anchorMax = new Vector2(1f, 1f);
        conteudoRt.pivot = new Vector2(0.5f, 1f);
        conteudoRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = conteudo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = conteudo.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = vrt;
        scroll.content = conteudoRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 25f;

        // ── Plaquinha de reabrir (estado minimizado) ─────────────────────
        chipMinimizado = MakeBotao(cv.transform, "» DIÁRIO DE BATALHA", TamChip, 14, Reabrir);
        RectTransform crt = chipMinimizado.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(1f, 0f);
        crt.anchorMax = new Vector2(1f, 0f);
        crt.pivot = new Vector2(1f, 0f);
        crt.anchoredPosition = Canto;

        AplicarEstado();
        Debug.Log("[BattleLog] Diário de batalha criado (canto inferior direito).");
    }

    // Botão simples no estilo do HUD: fundo escuro arredondado, aro e texto
    // dourado. `onClick` é chamado sem argumento.
    GameObject MakeBotao(Transform parent, string label, Vector2 tamanho,
                         float fonte, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Botao_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = tamanho;
        LobbySprites.MakeRounded(go.GetComponent<Image>(), new Color(0.12f, 0.09f, 0.02f, 0.95f));
        LobbySprites.AddRing(go.transform, Borda);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        GameObject txt = new GameObject("Label", typeof(RectTransform));
        txt.transform.SetParent(go.transform, false);
        RectTransform lrt = txt.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fonte;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Gold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        UIFonts.Set(tmp, UIFonts.Title);

        return go;
    }
}
