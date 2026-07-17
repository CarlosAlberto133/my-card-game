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

// Título do jogo + painel "Perfil do Jogador" no lobby, construídos por código
// no mesmo Canvas dos botões. As estatísticas vêm do Supabase via
// MatchReporter.FetchStats (conta logada pelo launcher — session.json).
// Sem login: mostra "Visitante" e o convite para entrar pelo launcher.
public static class LobbyProfileUI
{
    // Paleta pergaminho/taverna (casa com o LobbyDecor e o site)
    static readonly Color PanelBg = new Color(0.10f, 0.072f, 0.048f, 0.94f);
    static readonly Color PanelBorder = new Color(0.96f, 0.77f, 0.32f, 0.45f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color TextLight = new Color(0.95f, 0.91f, 0.82f);
    static readonly Color TextMuted = new Color(0.66f, 0.59f, 0.47f);
    static readonly Color Green = new Color(0.45f, 0.85f, 0.55f);
    static readonly Color Red = new Color(0.90f, 0.48f, 0.42f);

    public static void Build(Canvas canvas)
    {
        if (canvas == null) return;
        Transform root = canvas.transform;

        BuildTitle(root);
        BuildProfilePanel(root);
    }

    // ── Título do jogo (topo central) ────────────────────────────────────

    static void BuildTitle(Transform root)
    {
        // Tabuleta de madeira atrás do título (como a placa de uma taverna)
        GameObject sign = MakeImage(root, "TitleSign", new Vector2(660f, 118f), new Color(0.12f, 0.085f, 0.055f, 0.93f));
        LobbySprites.MakeRounded(sign.GetComponent<Image>(), new Color(0.12f, 0.085f, 0.055f, 0.93f));
        LobbySprites.AddRing(sign.transform, PanelBorder);
        RectTransform sgrt = sign.GetComponent<RectTransform>();
        sgrt.anchorMin = new Vector2(0.5f, 1f);
        sgrt.anchorMax = new Vector2(0.5f, 1f);
        sgrt.pivot = new Vector2(0.5f, 1f);
        sgrt.anchoredPosition = new Vector2(0f, -18f);

        TMP_Text title = MakeText(sign.transform, "GameTitle", "CARD GAME", 52, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 12f), new Vector2(640f, 62f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 16f;

        MakeText(sign.transform, "GameSubtitle", "«  duelos de cartas na mesa de RPG  »",
            18, TextMuted, TextAlignmentOptions.Center, new Vector2(0f, -32f), new Vector2(640f, 28f));
    }

    // ── Painel de perfil (canto superior direito) ────────────────────────

    static void BuildProfilePanel(Transform root)
    {
        // Painel arredondado com moldura dourada (mesma tabuleta dos botões)
        GameObject panel = MakeImage(root, "ProfilePanel", new Vector2(330f, 380f), PanelBg);
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(panel.transform, PanelBorder);
        RectTransform brt = panel.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-22f, -22f);

        MakeText(panel.transform, "Header", "«  PERFIL DO JOGADOR  »", 14, TextMuted,
            TextAlignmentOptions.Center, new Vector2(0f, 162f), new Vector2(300f, 24f));

        TMP_Text nameText = MakeText(panel.transform, "Name", "Carregando...", 23, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 128f), new Vector2(300f, 34f));
        nameText.fontStyle = FontStyles.Bold;
        nameText.enableWordWrapping = false;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        MakeImage(panel.transform, "Divider", new Vector2(280f, 2f), PanelBorder)
            .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 104f);

        // Linhas de estatística (rótulo à esquerda, valor à direita)
        TMP_Text vMatches = MakeStatRow(panel.transform, "Partidas", 70f, TextLight);
        TMP_Text vWins = MakeStatRow(panel.transform, "Vitórias", 36f, Green);
        TMP_Text vLosses = MakeStatRow(panel.transform, "Derrotas", 2f, Red);
        TMP_Text vAbandoned = MakeStatRow(panel.transform, "Abandonadas", -32f, TextMuted);
        TMP_Text vTime = MakeStatRow(panel.transform, "Tempo de jogo", -66f, TextLight);
        TMP_Text vRate = MakeStatRow(panel.transform, "Taxa de vitória", -100f, Gold);

        TMP_Text footer = MakeText(panel.transform, "Footer", "Buscando suas partidas...", 14,
            TextMuted, TextAlignmentOptions.Center, new Vector2(0f, -152f), new Vector2(300f, 52f));

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

            if (footer != null)
                footer.text = stats.total == 0
                    ? "Jogue sua primeira partida online\npara começar seu histórico!"
                    : "Estatísticas das partidas online salvas na sua conta.";
        });
    }

    static void SetAll(string value, params TMP_Text[] fields)
    {
        foreach (TMP_Text t in fields)
            if (t != null) t.text = value;
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

    // Linha "rótulo ..... valor"; retorna o TMP do VALOR para preencher depois
    static TMP_Text MakeStatRow(Transform parent, string label, float y, Color valueColor)
    {
        MakeText(parent, "Lbl_" + label, label, 17, TextMuted, TextAlignmentOptions.Left,
            new Vector2(-60f, y), new Vector2(180f, 28f));

        TMP_Text value = MakeText(parent, "Val_" + label, "…", 18, valueColor,
            TextAlignmentOptions.Right, new Vector2(95f, y), new Vector2(110f, 28f));
        value.fontStyle = FontStyles.Bold;
        return value;
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
