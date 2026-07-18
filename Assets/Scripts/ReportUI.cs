using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Modal de REPORT do jogador: escolhe o tipo (Bug — padrão, Hacker ou
// Feedback), descreve, e envia para o Supabase via ReportSender (que anexa
// contexto da partida + logs). Construído por código, no estilo dos demais
// painéis. Não toca em rede do jogo (o envio é HTTP à parte).
public static class ReportUI
{
    static readonly Color PanelBg = new Color(0.07f, 0.05f, 0.035f, 0.99f);
    static readonly Color Border = new Color(0.96f, 0.77f, 0.32f, 0.5f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color Ink = new Color(0.93f, 0.90f, 0.84f);
    static readonly Color Muted = new Color(0.55f, 0.5f, 0.42f);
    static readonly Color Slot = new Color(0.16f, 0.13f, 0.09f, 1f);

    static GameObject overlay;
    static string selectedType = "bug";
    static Image[] typeBtns;
    static string[] typeKeys = { "bug", "hacker", "feedback" };
    static TMP_InputField input;
    static TMP_Text statusText;
    static Button submitBtn;

    public static void Show(Canvas canvas)
    {
        if (canvas == null) return;
        if (overlay == null || overlay.transform.parent != canvas.transform)
            Build(canvas);
        selectedType = "bug";
        HighlightType();
        if (input != null) input.text = "";
        if (statusText != null) statusText.text = "";
        if (submitBtn != null) submitBtn.interactable = true;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public static void Hide()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    static void Build(Canvas canvas)
    {
        overlay = new GameObject("ReportOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        RectTransform ort = overlay.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(600f, 600f);   // mais largo: o conteúdo cabe
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(panel.transform, Border);

        MakeText(panel.transform, "Title", "REPORTAR", 30, Gold, TextAlignmentOptions.Center,
            FontStyles.Bold, new Vector2(0f, 258f), new Vector2(540f, 42f));
        MakeText(panel.transform, "Sub", "Vai direto para o desenvolvedor, com o contexto da partida.",
            15, Muted, TextAlignmentOptions.Center, FontStyles.Normal,
            new Vector2(0f, 226f), new Vector2(540f, 24f));

        // Botão fechar (X) — canto superior direito do painel
        MakeButton(panel.transform, "X", new Vector2(262f, 262f),
            new Vector2(34f, 34f), new Color(0.55f, 0.18f, 0.16f), Color.white, 18, Hide);

        // Tipo (3 opções — Bug padrão). Rótulo à esquerda, dentro da margem.
        MakeText(panel.transform, "TypeLbl", "Tipo do report", 15, Muted, TextAlignmentOptions.Left,
            FontStyles.Normal, new Vector2(-135f, 184f), new Vector2(240f, 22f));

        string[] labels = { "Bug", "Hacker", "Feedback" };
        typeBtns = new Image[3];
        float[] bx = { -168f, 0f, 168f };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            Image bimg = MakeTypeButton(panel.transform, labels[i], new Vector2(bx[i], 148f),
                new Vector2(156f, 44f), () => { selectedType = typeKeys[idx]; HighlightType(); });
            typeBtns[i] = bimg;
        }

        // Descrição
        MakeText(panel.transform, "DescLbl", "Descrição", 15, Muted, TextAlignmentOptions.Left,
            FontStyles.Normal, new Vector2(-135f, 100f), new Vector2(240f, 22f));
        input = MakeInput(panel.transform, new Vector2(0f, -6f), new Vector2(500f, 150f),
            "Descreva o que aconteceu (quanto mais detalhe, melhor)...");

        // Status
        statusText = MakeText(panel.transform, "Status", "", 15, Gold, TextAlignmentOptions.Center,
            FontStyles.Normal, new Vector2(0f, -108f), new Vector2(540f, 24f));

        // Cancelar / Enviar
        MakeButton(panel.transform, "Cancelar", new Vector2(-130f, -158f), new Vector2(200f, 50f),
            Slot, Ink, 20, Hide);
        submitBtn = MakeButton(panel.transform, "Enviar", new Vector2(130f, -158f), new Vector2(200f, 50f),
            Gold, new Color(0.12f, 0.09f, 0.02f), 20, OnSubmit).GetComponent<Button>();

        HighlightType();
        overlay.SetActive(false);
    }

    static void HighlightType()
    {
        if (typeBtns == null) return;
        for (int i = 0; i < typeBtns.Length; i++)
        {
            if (typeBtns[i] == null) continue;
            bool sel = typeKeys[i] == selectedType;
            typeBtns[i].color = sel ? Gold : Slot;
            TMP_Text t = typeBtns[i].GetComponentInChildren<TMP_Text>();
            if (t != null) t.color = sel ? new Color(0.12f, 0.09f, 0.02f) : Ink;
        }
    }

    static void OnSubmit()
    {
        string desc = input != null ? input.text.Trim() : "";
        if (desc.Length < 3)
        {
            SetStatus("Escreva uma descrição antes de enviar.", Red());
            return;
        }

        if (submitBtn != null) submitBtn.interactable = false;
        SetStatus("Enviando...", Gold);

        ReportSender.Submit(selectedType, desc, ok =>
        {
            if (statusText == null) return; // trocou de cena
            if (ok)
            {
                SetStatus("Enviado! Muito obrigado 🙏", Green());
                if (input != null) input.text = "";
            }
            else
            {
                SetStatus("Não deu para enviar agora. Verifique a conexão.", Red());
                if (submitBtn != null) submitBtn.interactable = true;
            }
        });
    }

    static void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = color;
    }

    static Color Green() => new Color(0.45f, 0.85f, 0.55f);
    static Color Red() => new Color(0.92f, 0.5f, 0.44f);

    // ── helpers ──

    static TMP_Text MakeText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, FontStyles style, Vector2 pos, Vector2 size2)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size2;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = align; tmp.fontStyle = style; tmp.richText = true; tmp.raycastTarget = false;
        return tmp;
    }

    static Image MakeTypeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Type_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        LobbySprites.MakeRounded(img, Slot);
        go.GetComponent<Button>().onClick.AddListener(onClick);

        TMP_Text t = MakeText(go.transform, "L", label, 16, Ink, TextAlignmentOptions.Center,
            FontStyles.Bold, Vector2.zero, size);
        return img;
    }

    static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color fg, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        LobbySprites.MakeRounded(go.GetComponent<Image>(), bg);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        MakeText(go.transform, "L", label, fontSize, fg, TextAlignmentOptions.Center,
            FontStyles.Bold, Vector2.zero, size);
        return go;
    }

    // Campo de texto multi-linha (TMP_InputField montado por código)
    static TMP_InputField MakeInput(Transform parent, Vector2 pos, Vector2 size, string placeholder)
    {
        GameObject go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image bg = go.GetComponent<Image>();
        LobbySprites.MakeRounded(bg, new Color(0.03f, 0.025f, 0.02f, 1f));

        // Área de texto (com recorte) + textos
        GameObject area = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        area.transform.SetParent(go.transform, false);
        RectTransform art = area.GetComponent<RectTransform>();
        art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
        art.offsetMin = new Vector2(14f, 10f); art.offsetMax = new Vector2(-14f, -10f);

        TMP_Text placeholderText = MakeChildText(area.transform, "Placeholder", placeholder, Muted);
        placeholderText.fontStyle = FontStyles.Italic;
        TMP_Text textComp = MakeChildText(area.transform, "Text", "", Ink);

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.textViewport = art;
        field.textComponent = textComp;
        field.placeholder = placeholderText;
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.lineLimit = 0;
        field.characterLimit = 2000;
        field.targetGraphic = bg;
        return field;
    }

    static TMP_Text MakeChildText(Transform parent, string name, string text, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 16; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
