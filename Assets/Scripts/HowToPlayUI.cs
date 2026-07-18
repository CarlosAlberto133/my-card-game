using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela "Como Jogar" do lobby: um overlay com as regras do jogo, construído por
// código (mesmo estilo dos painéis do LobbyUI). Puramente informativo — não
// toca em rede nem no estado do jogo. Chamado pelo botão "Como Jogar" do menu.
public static class HowToPlayUI
{
    static readonly Color PanelBg = new Color(0.07f, 0.05f, 0.035f, 0.98f);
    static readonly Color Border = new Color(0.96f, 0.77f, 0.32f, 0.45f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color Ink = new Color(0.92f, 0.90f, 0.84f);

    static GameObject overlay;

    // Constrói (uma vez) e mostra o overlay. Recria se o canvas mudou de cena.
    public static void Show(Canvas canvas)
    {
        if (canvas == null) return;
        if (overlay == null || overlay.transform.parent != canvas.transform)
            Build(canvas);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public static void Hide()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    static void Build(Canvas canvas)
    {
        // Fundo escuro em tela cheia (bloqueia cliques atrás)
        overlay = new GameObject("HowToPlayOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        // Clicar no fundo fecha
        Button bgBtn = overlay.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(Hide);

        // Painel central
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(760f, 700f);
        Image pImg = panel.GetComponent<Image>();
        LobbySprites.MakeRounded(pImg, PanelBg);
        LobbySprites.AddRing(panel.transform, Border);

        // Título
        MakeText(panel.transform, "Title", "COMO JOGAR", 34, Gold,
            TextAlignmentOptions.Center, FontStyles.Bold,
            new Vector2(0f, 308f), new Vector2(720f, 46f), 0f);

        MakeText(panel.transform, "Sub", "Um duelo tático de cartas — derrube a torre do oponente!",
            17, new Color(0.66f, 0.59f, 0.47f), TextAlignmentOptions.Center, FontStyles.Normal,
            new Vector2(0f, 274f), new Vector2(720f, 26f), 0f);

        // Regras em DUAS colunas (aproveita a largura e evita o texto colidir
        // com o botão). Cada coluna é ancorada pelo TOPO e flui para baixo.
        string colLeft =
            "<b><color=#F5C451>» Objetivo</color></b>\n" +
            "Cada jogador tem uma torre com 30 de vida. Vence quem zerar a torre inimiga.\n\n" +
            "<b><color=#F5C451>» O tabuleiro</color></b>\n" +
            "Um campo 7×7. Posicione suas cartas nas duas fileiras do seu lado e avance em direção à torre inimiga.\n\n" +
            "<b><color=#F5C451>» No seu turno</color></b>\n" +
            "1) Compre cartas na loja.  2) Posicione cartas da mão.  3) Mova e ataque.  4) Passe a vez.\n" +
            "Cada carta move 1 casa E ataca 1 vez por rodada (na ordem que quiser).\n\n" +
            "<b><color=#F5C451>» Loja e tiers</color></b>\n" +
            "Você ganha ouro a cada turno. As cartas vão do tier 1 (baratas) ao 5 (lendárias) — os tiers altos aparecem mais conforme a partida avança.";

        string colRight =
            "<b><color=#F5C451>» As 4 classes</color></b>\n" +
            "<b>Tanque</b> — muita armadura e vida; segura a linha de frente.\n" +
            "<b>Mago</b> — dano alto e efeitos; alcança 2 casas em linha reta.\n" +
            "<b>Arqueiro</b> — o maior dano, mas frágil; também alcança 2 casas.\n" +
            "<b>Healer</b> — o suporte: cura, ouro e buffs mantêm o time vivo.\n\n" +
            "<b><color=#F5C451>» Tríades</color></b>\n" +
            "Três cartas específicas da mesma classe e tier, juntas no campo, ativam um COMBO poderoso (ex.: +5 de ataque em todos). Vale a pena montar!\n\n" +
            "<b><color=#F5C451>» Efeitos das cartas</color></b>\n" +
            "Cada carta tem um efeito próprio — passe o mouse sobre ela a qualquer momento para ler o que faz.";

        MakeColumn(panel.transform, "ColLeft", colLeft, new Vector2(-182f, 242f), new Vector2(330f, 500f));
        MakeColumn(panel.transform, "ColRight", colRight, new Vector2(182f, 242f), new Vector2(330f, 500f));

        // Botão "Entendi!" — bem abaixo das colunas, com folga
        MakeButton(panel.transform, "Entendi!", new Vector2(0f, -312f), new Vector2(240f, 50f), Hide);
    }

    // Coluna de texto ancorada pelo TOPO (pivot 0.5,1): o texto começa em `topY`
    // e flui para baixo, então nunca "sobe" para cima da caixa nem colide com o
    // botão. lineSpacing menor deixa o bloco mais compacto.
    static void MakeColumn(Transform parent, string name, string text, Vector2 topPos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 1f);        // topo
        rt.anchoredPosition = topPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 15.5f;
        tmp.color = Ink;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.lineSpacing = 2f;
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }

    // ── helpers ──

    static TMP_Text MakeText(Transform parent, string name, string text, float size,
        Color color, TextAlignmentOptions align, FontStyles style,
        Vector2 pos, Vector2 sizeDelta, float unused)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        LobbySprites.MakeRounded(img, Gold);
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(onClick);

        TextMeshProUGUI tmp = new GameObject("L", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        RectTransform trt = tmp.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.12f, 0.09f, 0.02f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }
}
