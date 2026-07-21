using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sprites arredondados gerados por código para a UI do lobby (sem assets):
// Fill = retângulo de cantos arredondados preenchido (tingido pela Image);
// Ring = só o contorno (borda), para desenhar molduras douradas por cima.
// Ambos são 9-slice: esticam para qualquer tamanho sem deformar os cantos.
public static class LobbySprites
{
    static Sprite fill, ring;

    public static Sprite Fill { get { if (fill == null) fill = Make(false); return fill; } }
    public static Sprite Ring { get { if (ring == null) ring = Make(true); return ring; } }

    static Sprite Make(bool ringOnly)
    {
        const int S = 48;       // tamanho da textura
        const float R = 13f;    // raio dos cantos
        const float T = 3f;     // espessura do contorno (Ring)

        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float px = x + 0.5f - S / 2f;
                float py = y + 0.5f - S / 2f;
                float d = RoundRectSDF(px, py, S / 2f - 1.5f, S / 2f - 1.5f, R);

                float aFill = Mathf.Clamp01(0.5f - d);       // borda com 1px de antialias
                float a = ringOnly ? aFill - Mathf.Clamp01(0.5f - (d + T)) : aFill;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
    }

    // Distância com sinal até um retângulo arredondado centrado na origem
    static float RoundRectSDF(float x, float y, float halfW, float halfH, float r)
    {
        float dx = Mathf.Abs(x) - (halfW - r);
        float dy = Mathf.Abs(y) - (halfH - r);
        float ax = Mathf.Max(dx, 0f);
        float ay = Mathf.Max(dy, 0f);
        return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
    }

    // Aplica o visual "tabuleta": fundo arredondado + moldura dourada por cima
    public static void MakeRounded(Image img, Color fillColor)
    {
        img.sprite = Fill;
        img.type = Image.Type.Sliced;
        img.color = fillColor;
    }

    public static GameObject AddRing(Transform parent, Color color)
    {
        GameObject go = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.sprite = Ring;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return go;
    }
}

// Progressão do jogador (nível + XP) derivada das PARTIDAS REAIS salvas no
// Supabase — nada é inventado nem guardado à parte: o mesmo histórico que
// alimenta "Partidas/Vitórias/Derrotas" alimenta a barra. Por ser calculado do
// histórico, o nível é sempre consistente (em qualquer PC, mesmo após reinstalar)
// e nunca "desanda" por falha de gravação.
public static class PlayerProgress
{
    // Quanto cada resultado vale
    public const int XpWin = 150;
    public const int XpLoss = 60;
    public const int XpAbandoned = 20;

    // Nível 1→2 custa 300 XP; cada nível seguinte custa 100 a mais
    public const int XpFirstLevel = 300;
    public const int XpLevelStep = 100;

    public static int TotalXp(int wins, int losses, int abandoned)
    {
        return wins * XpWin + losses * XpLoss + abandoned * XpAbandoned;
    }

    public static int XpNeededFor(int level)
    {
        return XpFirstLevel + Mathf.Max(0, level - 1) * XpLevelStep;
    }

    // Quebra o XP total em nível atual, quanto já andou nele e quanto falta
    public static void Resolve(int totalXp, out int level, out int intoLevel, out int needed)
    {
        level = 1;
        intoLevel = Mathf.Max(0, totalXp);
        needed = XpNeededFor(level);

        // Trava de segurança: histórico gigante não pode virar laço infinito
        while (intoLevel >= needed && level < 999)
        {
            intoLevel -= needed;
            level++;
            needed = XpNeededFor(level);
        }
    }
}

// Título do jogo + painel "Perfil do Jogador" + caixa "Tabuleiro Tático",
// construídos por código no mesmo Canvas dos botões, seguindo a arte de
// referência do lobby (placa ornamentada, nível + barra de XP, estatísticas
// e últimas partidas). Estatísticas do Supabase via MatchReporter.FetchStats.
public static class LobbyProfileUI
{
    // Paleta pergaminho/taverna (casa com o LobbyDecor e o site)
    static readonly Color PanelBg = new Color(0.085f, 0.062f, 0.040f, 0.95f);
    static readonly Color PanelBorder = new Color(0.96f, 0.77f, 0.32f, 0.55f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color GoldSoft = new Color(0.85f, 0.70f, 0.38f);
    static readonly Color TextLight = new Color(0.95f, 0.91f, 0.82f);
    static readonly Color TextMuted = new Color(0.66f, 0.59f, 0.47f);
    static readonly Color Green = new Color(0.45f, 0.85f, 0.55f);
    static readonly Color Red = new Color(0.90f, 0.48f, 0.42f);
    static readonly Color BarBg = new Color(0.16f, 0.12f, 0.07f, 0.95f);

    public static void Build(Canvas canvas)
    {
        if (canvas == null) return;
        Transform root = canvas.transform;

        BuildTitle(root);
        BuildProfilePanel(root);
        BuildBoardInfoBox(root);
    }

    // ── Título do jogo (placa ornamentada no topo central) ───────────────

    static void BuildTitle(Transform root)
    {
        GameObject sign = MakeImage(root, "TitleSign", new Vector2(680f, 128f),
            new Color(0.075f, 0.055f, 0.035f, 0.96f));
        LobbySprites.MakeRounded(sign.GetComponent<Image>(), new Color(0.075f, 0.055f, 0.035f, 0.96f));
        LobbySprites.AddRing(sign.transform, new Color(0.96f, 0.77f, 0.32f, 0.85f));

        // Segunda moldura interna (efeito de placa entalhada da arte)
        GameObject inner = LobbySprites.AddRing(sign.transform, new Color(0.96f, 0.77f, 0.32f, 0.30f));
        RectTransform irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(7f, 7f);
        irt.offsetMax = new Vector2(-7f, -7f);

        RectTransform sgrt = sign.GetComponent<RectTransform>();
        sgrt.anchorMin = new Vector2(0.5f, 1f);
        sgrt.anchorMax = new Vector2(0.5f, 1f);
        sgrt.pivot = new Vector2(0.5f, 1f);
        sgrt.anchoredPosition = new Vector2(0f, -14f);

        // Gema no topo da placa (losango dourado com miolo verde)
        MakeDiamond(sign.transform, new Vector2(0f, 64f), 26f, Gold);
        MakeDiamond(sign.transform, new Vector2(0f, 64f), 14f, new Color(0.35f, 0.80f, 0.45f));

        TMP_Text title = MakeText(sign.transform, "GameTitle", "CARD GAME", 54, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 14f), new Vector2(660f, 64f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 14f;

        MakeText(sign.transform, "GameSubtitle", "«  duelos de cartas na mesa de RPG  »",
            18, TextMuted, TextAlignmentOptions.Center, new Vector2(0f, -34f), new Vector2(660f, 28f));

        // Losangos decorativos na base da placa
        MakeDiamond(sign.transform, new Vector2(-320f, -58f), 14f, GoldSoft);
        MakeDiamond(sign.transform, new Vector2(320f, -58f), 14f, GoldSoft);
    }

    // ── Painel de perfil (lado direito, como na arte) ────────────────────

    static void BuildProfilePanel(Transform root)
    {
        GameObject panel = MakeImage(root, "ProfilePanel", new Vector2(344f, 664f), PanelBg);
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(panel.transform, PanelBorder);
        RectTransform brt = panel.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-20f, -18f);

        MakeDiamond(panel.transform, new Vector2(-104f, 303f), 8f, GoldSoft);
        MakeText(panel.transform, "Header", "PERFIL DO JOGADOR", 14, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 302f), new Vector2(310f, 24f));
        MakeDiamond(panel.transform, new Vector2(104f, 303f), 8f, GoldSoft);

        TMP_Text nameText = MakeText(panel.transform, "Name", "Carregando...", 22, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 268f), new Vector2(310f, 32f));
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        // ── Nível + barra de XP (das partidas reais — ver PlayerProgress) ─
        GameObject badge = MakeImage(panel.transform, "LevelBadge", new Vector2(64f, 64f), Gold);
        LobbySprites.MakeRounded(badge.GetComponent<Image>(), Gold);
        LobbySprites.AddRing(badge.transform, new Color(0.55f, 0.40f, 0.12f, 0.9f));
        badge.GetComponent<RectTransform>().anchoredPosition = new Vector2(-118f, 212f);
        TMP_Text levelText = MakeText(badge.transform, "Lvl", "—", 28,
            new Color(0.22f, 0.15f, 0.03f), TextAlignmentOptions.Center,
            new Vector2(0f, 5f), new Vector2(64f, 44f));
        levelText.fontStyle = FontStyles.Bold;
        MakeText(badge.transform, "LvlCaption", "NÍVEL", 10,
            new Color(0.30f, 0.21f, 0.05f), TextAlignmentOptions.Center,
            new Vector2(0f, -19f), new Vector2(64f, 14f));

        // Barra: fundo + preenchimento dourado + "x / y XP" por cima
        GameObject barBg = MakeImage(panel.transform, "XpBarBg", new Vector2(214f, 26f), BarBg);
        LobbySprites.MakeRounded(barBg.GetComponent<Image>(), BarBg);
        LobbySprites.AddRing(barBg.transform, new Color(0.96f, 0.77f, 0.32f, 0.45f));
        barBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(26f, 219f);

        GameObject barFill = MakeImage(barBg.transform, "XpBarFill", new Vector2(0f, 18f), Gold);
        LobbySprites.MakeRounded(barFill.GetComponent<Image>(), Gold);
        RectTransform fillRt = barFill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0.5f);
        fillRt.anchorMax = new Vector2(0f, 0.5f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.anchoredPosition = new Vector2(4f, 0f);
        const float BarInnerWidth = 206f;

        TMP_Text xpText = MakeText(barBg.transform, "XpLabel", "—", 13, TextLight,
            TextAlignmentOptions.Center, Vector2.zero, new Vector2(BarInnerWidth, 22f));
        xpText.fontStyle = FontStyles.Bold;

        // Quanto falta para o próximo nível (linha discreta sob a barra)
        TMP_Text nextText = MakeText(panel.transform, "XpNext", "", 12, TextMuted,
            TextAlignmentOptions.Center, new Vector2(26f, 196f), new Vector2(214f, 18f));

        MakeImage(panel.transform, "Divider", new Vector2(296f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 168f);

        // Linhas de estatística (chip colorido + rótulo + valor à direita)
        TMP_Text vMatches = MakeStatRow(panel.transform, "Partidas", 138f, TextLight, GoldSoft);
        TMP_Text vWins = MakeStatRow(panel.transform, "Vitórias", 106f, Green, Green);
        TMP_Text vLosses = MakeStatRow(panel.transform, "Derrotas", 74f, Red, Red);
        TMP_Text vAbandoned = MakeStatRow(panel.transform, "Abandonadas", 42f, TextMuted, TextMuted);
        TMP_Text vTime = MakeStatRow(panel.transform, "Tempo de jogo", 10f, TextLight, new Color(0.55f, 0.62f, 0.85f));
        TMP_Text vRate = MakeStatRow(panel.transform, "Taxa de vitória", -22f, Gold, Gold);

        // Seção de histórico
        MakeImage(panel.transform, "Divider2", new Vector2(296f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -52f);
        MakeDiamond(panel.transform, new Vector2(-102f, -76f), 8f, GoldSoft);
        MakeText(panel.transform, "HistHeader", "ÚLTIMAS PARTIDAS", 13, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, -77f), new Vector2(310f, 22f));
        MakeDiamond(panel.transform, new Vector2(102f, -76f), 8f, GoldSoft);

        // Cada linha = DOIS textos separados (mapa/duração à esquerda, resultado
        // à direita). Antes era um texto só com <pos=72%>: quando o nome do mapa
        // era longo o resultado transbordava, quebrava linha e caía em cima da
        // linha de baixo. Com dois campos e sem quebra de linha isso não ocorre.
        TMP_Text[] histLeft = new TMP_Text[5];
        TMP_Text[] histRight = new TMP_Text[5];
        GameObject[] histChips = new GameObject[5];
        for (int i = 0; i < histLeft.Length; i++)
        {
            float y = -110f - i * 30f;

            GameObject chip = MakeImage(panel.transform, "HistChip" + i, new Vector2(9f, 9f),
                new Color(0.35f, 0.50f, 0.85f));
            LobbySprites.MakeRounded(chip.GetComponent<Image>(), new Color(0.35f, 0.50f, 0.85f));
            chip.GetComponent<RectTransform>().anchoredPosition = new Vector2(-142f, y);
            chip.SetActive(false); // só aparece quando a linha tiver partida
            histChips[i] = chip;

            histLeft[i] = MakeText(panel.transform, "HistL" + i, "", 13, TextMuted,
                TextAlignmentOptions.Left, new Vector2(-28f, y), new Vector2(196f, 22f));
            histLeft[i].enableWordWrapping = false;
            histLeft[i].overflowMode = TextOverflowModes.Ellipsis;

            histRight[i] = MakeText(panel.transform, "HistR" + i, "", 13, TextMuted,
                TextAlignmentOptions.Right, new Vector2(110f, y), new Vector2(88f, 22f));
            histRight[i].enableWordWrapping = false;
        }

        TMP_Text footer = MakeText(panel.transform, "Footer", "Buscando suas partidas...", 13,
            TextMuted, TextAlignmentOptions.Center, new Vector2(0f, -285f), new Vector2(310f, 48f));

        FillHistory(histLeft, histRight, histChips);

        // Busca as estatísticas (o callback pode chegar depois de trocar de cena:
        // os textos destruídos viram null e o preenchimento é simplesmente pulado)
        MatchReporter.FetchStats(stats =>
        {
            if (nameText == null) return;

            if (!stats.loggedIn)
            {
                nameText.text = "Visitante";
                nameText.color = TextLight;
                SetAll("—", vMatches, vWins, vLosses, vAbandoned, vTime, vRate);
                if (footer != null)
                    footer.text = "Entre com Google pelo launcher para\nsalvar partidas e ver suas estatísticas.";
                return;
            }

            nameText.text = string.IsNullOrEmpty(stats.playerName) ? "Jogador" : stats.playerName;

            if (stats.sessionExpired)
            {
                SetAll("—", vMatches, vWins, vLosses, vAbandoned, vTime, vRate);
                if (footer != null)
                    footer.text = "Sua sessão expirou.\nEntre de novo com Google pelo launcher.";
                return;
            }

            if (!stats.statsLoaded)
            {
                SetAll("—", vMatches, vWins, vLosses, vAbandoned, vTime, vRate);
                if (footer != null)
                    footer.text = "Não deu para buscar as estatísticas agora.\nVerifique a conexão e reabra o lobby.";
                return;
            }

            if (vMatches != null) vMatches.text = stats.total.ToString();
            if (vWins != null) vWins.text = stats.wins.ToString();
            if (vLosses != null) vLosses.text = stats.losses.ToString();
            if (vAbandoned != null) vAbandoned.text = stats.abandoned.ToString();
            if (vTime != null) vTime.text = FormatTime(stats.totalSeconds);

            int decided = stats.wins + stats.losses;
            if (vRate != null)
                vRate.text = decided > 0 ? Mathf.RoundToInt(stats.wins * 100f / decided) + "%" : "—";

            // Nível/XP calculados das partidas reais (ver PlayerProgress)
            int level, into, need;
            PlayerProgress.Resolve(
                PlayerProgress.TotalXp(stats.wins, stats.losses, stats.abandoned),
                out level, out into, out need);

            if (levelText != null) levelText.text = level.ToString();
            if (xpText != null) xpText.text = into + " / " + need + " XP";
            if (fillRt != null)
                fillRt.sizeDelta = new Vector2(
                    Mathf.Round(BarInnerWidth * Mathf.Clamp01(into / (float)need)), 18f);
            if (nextText != null)
                nextText.text = "Faltam " + (need - into) + " XP para o nível " + (level + 1);

            if (footer != null)
                footer.text = stats.total == 0
                    ? "Jogue sua primeira partida online\npara começar seu histórico!"
                    : "«  Estatísticas das partidas online  »";
        });
    }

    // ── Caixa "Tabuleiro Tático" (canto inferior esquerdo, como na arte) ─

    static void BuildBoardInfoBox(Transform root)
    {
        GameObject box = MakeImage(root, "BoardInfoBox", new Vector2(392f, 104f), PanelBg);
        LobbySprites.MakeRounded(box.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(box.transform, new Color(0.96f, 0.77f, 0.32f, 0.45f));
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 18f);

        // Ícone de grade (mini-tabuleiro desenhado com imagens)
        GameObject grid = MakeImage(box.transform, "GridIcon", new Vector2(54f, 54f), BarBg);
        LobbySprites.MakeRounded(grid.GetComponent<Image>(), BarBg);
        LobbySprites.AddRing(grid.transform, new Color(0.96f, 0.77f, 0.32f, 0.5f));
        grid.GetComponent<RectTransform>().anchoredPosition = new Vector2(-152f, 0f);

        // Grade 7x7 = 6 linhas internas em cada eixo (igual ao BoardManager)
        const int BoardCells = 7;
        const float GridSpan = 46f;
        for (int i = 1; i < BoardCells; i++)
        {
            float o = -GridSpan / 2f + GridSpan * i / BoardCells;
            MakeImage(grid.transform, "Gv" + i, new Vector2(1.2f, GridSpan), new Color(0.96f, 0.77f, 0.32f, 0.5f))
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(o, 0f);
            MakeImage(grid.transform, "Gh" + i, new Vector2(GridSpan, 1.2f), new Color(0.96f, 0.77f, 0.32f, 0.5f))
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, o);
        }

        TMP_Text t = MakeText(box.transform, "Title", "TABULEIRO TÁTICO 7x7", 18, Gold,
            TextAlignmentOptions.Left, new Vector2(34f, 26f), new Vector2(300f, 26f));
        t.fontStyle = FontStyles.Bold;

        MakeText(box.transform, "Desc", "Planeje suas jogadas, posicione\nsuas unidades e vença o duelo!",
            14, TextMuted, TextAlignmentOptions.Left, new Vector2(34f, -14f), new Vector2(300f, 44f));
    }

    static void SetAll(string value, params TMP_Text[] fields)
    {
        foreach (TMP_Text t in fields)
            if (t != null) t.text = value;
    }

    // Preenche as linhas do histórico com as últimas partidas (uma por linha:
    // resultado + mapa + duração). Callback pode chegar após trocar de cena →
    // guarda contra rows destruídas (viram null).
    static void FillHistory(TMP_Text[] left, TMP_Text[] right, GameObject[] chips)
    {
        MatchReporter.FetchRecentMatches(left.Length, matches =>
        {
            if (left == null || left.Length == 0 || left[0] == null) return;

            // Mensagem que ocupa a linha inteira (usa o campo da esquerda e
            // limpa o da direita)
            System.Action<int, string, Color> message = (i, text, color) =>
            {
                if (i < left.Length && left[i] != null) { left[i].text = text; left[i].color = color; }
                if (right != null && i < right.Length && right[i] != null) right[i].text = "";
            };

            // null = a busca FALHOU (rede/consulta); lista vazia = não há partida.
            // Confundir os dois escondia erro de banco atrás de "nenhuma partida"
            if (matches == null)
            {
                message(0, "<i>Não deu para buscar o histórico.</i>", Red);
                if (left.Length > 1) message(1, "<i>Veja o motivo no log do jogo.</i>", TextMuted);
                for (int i = 2; i < left.Length; i++) message(i, "", TextMuted);
                return;
            }

            if (matches.Count == 0)
            {
                message(0, "<i>Nenhuma partida ainda.</i>", TextMuted);
                for (int i = 1; i < left.Length; i++) message(i, "", TextMuted);
                return;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] == null) continue;
                if (i >= matches.Count) { message(i, "", TextMuted); continue; }
                if (chips != null && i < chips.Length && chips[i] != null) chips[i].SetActive(true);

                var m = matches[i];
                string res; Color resColor;
                if (m.iWon) { res = "Vitória"; resColor = Green; }
                else if (m.status == "finalizada") { res = "Derrota"; resColor = Red; }
                else { res = "Abandonada"; resColor = TextMuted; }

                left[i].color = TextLight;
                left[i].text = $"{MapName(m.map)}  ·  {ShortTime(m.durationSeconds)}";

                if (right != null && i < right.Length && right[i] != null)
                {
                    right[i].text = res;
                    right[i].color = resColor;
                    right[i].fontStyle = FontStyles.Bold;
                }
            }
        });
    }

    static string MapName(string map)
    {
        if (map == "mesa") return "Mesa de RPG";
        if (map == "espaco") return "Espaço";
        if (map == "floresta") return "Floresta";
        if (map == "teste") return "Teste";
        return "—";
    }

    static string ShortTime(int seconds)
    {
        if (seconds <= 0) return "0s";
        return seconds >= 60 ? $"{seconds / 60}min" : $"{seconds}s";
    }

    static string FormatTime(int seconds)
    {
        if (seconds <= 0) return "0min";
        int h = seconds / 3600;
        int m = (seconds % 3600) / 60;
        if (h > 0) return h + "h " + m + "min";
        if (m > 0) return m + "min";
        return "menos de 1min";
    }

    // Linha "chip + rótulo ..... valor"; retorna o TMP do VALOR
    static TMP_Text MakeStatRow(Transform parent, string label, float y, Color valueColor, Color chipColor)
    {
        GameObject chip = MakeImage(parent, "Chip_" + label, new Vector2(14f, 14f), chipColor);
        LobbySprites.MakeRounded(chip.GetComponent<Image>(), chipColor);
        chip.GetComponent<RectTransform>().anchoredPosition = new Vector2(-140f, y);

        MakeText(parent, "Lbl_" + label, label, 17, TextMuted, TextAlignmentOptions.Left,
            new Vector2(-28f, y), new Vector2(188f, 28f));

        TMP_Text value = MakeText(parent, "Val_" + label, "…", 18, valueColor,
            TextAlignmentOptions.Right, new Vector2(95f, y), new Vector2(110f, 28f));
        value.fontStyle = FontStyles.Bold;
        return value;
    }

    // Losango decorativo (quadrado arredondado girado 45°)
    static void MakeDiamond(Transform parent, Vector2 pos, float size, Color color)
    {
        GameObject go = MakeImage(parent, "Diamond", new Vector2(size, size), color);
        LobbySprites.MakeRounded(go.GetComponent<Image>(), color);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    // ── Helpers básicos (mesmo padrão do LobbyUI) ────────────────────────

    static GameObject MakeImage(Transform parent, string name, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = size;
        go.GetComponent<Image>().color = color;
        go.GetComponent<Image>().raycastTarget = false; // decorativo: não come cliques
        return go;
    }

    static TMP_Text MakeText(Transform parent, string name, string text, int fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    static TMP_Text MakeText(Transform parent, string name, string text, int fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        TMP_Text tmp = MakeText(parent, name, text, fontSize, color, alignment);
        RectTransform rt = tmp.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return tmp;
    }
}
