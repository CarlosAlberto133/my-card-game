using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ═══════════════ [DECKMODE] construtor de decks (lobby) ═══════════════
// Overlay do LOBBY para montar o baralho de 30 do modo deck: catálogo à
// esquerda (clique adiciona), deck à direita (clique remove), filtros por
// classe, salvar em PlayerPrefs. 100% por código, sem tocar na cena.
// Para REMOVER o modo deck: apagar este arquivo + a linha
// DeckBuilderUI.AddEntryButton no PhotonLobbyManager.
public static class DeckBuilderUI
{
    // Paleta (mesma família do LobbyUI)
    static readonly Color PanelBg = new Color(0.070f, 0.055f, 0.040f, 0.97f);
    static readonly Color Border = new Color(0.85f, 0.66f, 0.30f);
    static readonly Color Gold = new Color(0.96f, 0.82f, 0.45f);
    static readonly Color TextLight = new Color(0.93f, 0.89f, 0.80f);
    static readonly Color TextMuted = new Color(0.70f, 0.66f, 0.58f);
    static readonly Color Slate = new Color(0.22f, 0.20f, 0.17f);

    // Cores por classe (aproximadas das do jogo) + feitiço
    static readonly Color[] ClassColors =
    {
        new Color(0.45f, 0.62f, 0.95f), // Tank
        new Color(0.80f, 0.50f, 0.95f), // Mago
        new Color(0.50f, 0.90f, 0.60f), // Healer
        new Color(0.95f, 0.75f, 0.40f), // Arqueiro
    };
    static readonly Color SpellColor = new Color(0.78f, 0.60f, 1f);

    static GameObject overlay;
    static Transform catalogContent;
    static Transform deckContent;
    static TMP_Text counterText;
    static TMP_Text toastText;
    static int classFilter = -1; // -1 = todos, 0-3 = classe, 4 = feitiços
    static readonly List<string> working = new List<string>(); // 1 entrada por cópia

    // Botão de entrada no lobby (canto inferior direito)
    public static void AddEntryButton(Canvas canvas)
    {
        if (canvas == null) return;
        GameObject btn = MakeButton(canvas.transform, "Montar Deck (beta)",
            new Vector2(-14f, 14f), new Vector2(210f, 46f), Slate, Gold, 17,
            () => Open(canvas));
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-14f, 14f);
    }

    public static void Open(Canvas canvas)
    {
        if (overlay == null) Build(canvas);
        working.Clear();
        working.AddRange(DeckMode.LoadSavedDeck());
        classFilter = -1;
        overlay.SetActive(true);
        RefreshAll();
    }

    static void Close()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    // ── Regras de edição ─────────────────────────────────────────────────

    static int CountOf(string entry)
    {
        int n = 0;
        foreach (string e in working) if (e == entry) n++;
        return n;
    }

    static int CapOf(int tier) { return tier >= 5 ? DeckMode.MaxCopiesTier5 : DeckMode.MaxCopies; }

    static void TryAdd(string entry, int tier, string display)
    {
        if (working.Count >= DeckMode.DeckSize) { Toast("Deck cheio (30)!"); return; }
        if (CountOf(entry) >= CapOf(tier))
        {
            Toast(tier >= 5 ? $"{display}: lendária, só 1 cópia!" : $"{display}: máximo 3 cópias!");
            return;
        }
        working.Add(entry);
        RefreshAll();
    }

    static void RemoveOne(string entry)
    {
        working.Remove(entry);
        RefreshAll();
    }

    // ── Construção da UI ─────────────────────────────────────────────────

    static void Build(Canvas canvas)
    {
        overlay = new GameObject("DeckBuilderOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.sizeDelta = new Vector2(1480f, 860f);
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(panel.transform, Border);

        MakeText(panel.transform, "Title", "MONTAR DECK — modo deck (teste)", 28, Gold,
            TextAlignmentOptions.Center, new Vector2(0f, 396f), new Vector2(900f, 40f), FontStyles.Bold);
        counterText = MakeText(panel.transform, "Counter", "0/30", 24, TextLight,
            TextAlignmentOptions.Center, new Vector2(560f, 396f), new Vector2(220f, 40f), FontStyles.Bold);

        MakeText(panel.transform, "Hint",
            "Clique numa carta do catálogo para ADICIONAR · clique no deck para REMOVER · máx. 3 cópias (lendária: 1)",
            15, TextMuted, TextAlignmentOptions.Center, new Vector2(0f, 362f), new Vector2(1300f, 24f));

        // Filtros de classe
        string[] filters = { "Todos", "Tank", "Mago", "Healer", "Arqueiro", "Feitiços" };
        for (int i = 0; i < filters.Length; i++)
        {
            int idx = i - 1; // -1 = todos ... 4 = feitiços
            MakeButton(panel.transform, filters[i],
                new Vector2(-560f + i * 155f, 322f), new Vector2(145f, 38f),
                Slate, idx >= 0 && idx < 4 ? ClassColors[idx] : (idx == 4 ? SpellColor : TextLight),
                16, () => { classFilter = idx; RefreshCatalog(); });
        }

        // Catálogo (esquerda) e deck (direita), com scroll
        catalogContent = MakeScrollArea(panel.transform, "Catalog",
            new Vector2(-290f, -50f), new Vector2(860f, 640f), 2);
        deckContent = MakeScrollArea(panel.transform, "Deck",
            new Vector2(430f, -50f), new Vector2(520f, 640f), 1);

        // Rodapé: ações
        MakeButton(panel.transform, "Salvar Deck", new Vector2(-430f, -396f),
            new Vector2(220f, 52f), Gold, new Color(0.12f, 0.09f, 0.02f), 18, Save);
        MakeButton(panel.transform, "Deck Inicial", new Vector2(-180f, -396f),
            new Vector2(220f, 52f), Slate, TextLight, 18, () =>
            { working.Clear(); working.AddRange(DeckMode.StarterDeck()); RefreshAll(); });
        MakeButton(panel.transform, "Limpar", new Vector2(50f, -396f),
            new Vector2(180f, 52f), Slate, TextLight, 18, () => { working.Clear(); RefreshAll(); });
        MakeButton(panel.transform, "Fechar", new Vector2(430f, -396f),
            new Vector2(220f, 52f), new Color(0.45f, 0.16f, 0.13f), TextLight, 18, Close);

        toastText = MakeText(panel.transform, "Toast", "", 16, Gold,
            TextAlignmentOptions.Center, new Vector2(-40f, -352f), new Vector2(1100f, 26f));
    }

    static void Save()
    {
        if (working.Count != DeckMode.DeckSize)
        {
            Toast($"O deck precisa de EXATAMENTE {DeckMode.DeckSize} cartas (tem {working.Count})!");
            return;
        }
        DeckMode.SaveDeck(working);
        Toast("Deck salvo! Ele será usado nas partidas do modo deck.");
    }

    static void Toast(string msg)
    {
        if (toastText != null) toastText.text = msg;
    }

    // ── Listagens ────────────────────────────────────────────────────────

    static void RefreshAll()
    {
        if (counterText != null)
        {
            counterText.text = $"{working.Count}/{DeckMode.DeckSize}";
            counterText.color = working.Count == DeckMode.DeckSize
                ? new Color(0.55f, 0.95f, 0.60f) : TextLight;
        }
        RefreshCatalog();
        RefreshDeck();
    }

    static void RefreshCatalog()
    {
        Clear(catalogContent);

        // Unidades (sem as que dependem de ouro — não existem neste modo)
        if (classFilter < 4)
        {
            foreach (var u in DeckCatalog.Units)
            {
                if (u.usesGold) continue;
                if (classFilter >= 0 && u.classIdx != classFilter) continue;
                string entry = u.cardName;
                int have = CountOf(entry);
                string label = $"{u.cardName}  ·  T{u.tier}  ·  {u.attack}/{u.shield}/{u.health}" +
                               (have > 0 ? $"   [x{have}]" : "");
                int tier = u.tier;
                string nm = u.cardName;
                MakeRow(catalogContent, label, ClassColors[u.classIdx],
                    () => TryAdd(entry, tier, nm));
            }
        }

        // Feitiços
        if (classFilter == -1 || classFilter == 4)
        {
            foreach (var s in SpellCards.All)
            {
                string entry = "s" + s.id;
                int have = CountOf(entry);
                string label = $"{s.cardName}  ·  Feitiço  ·  custo {s.cost} de mana" +
                               (have > 0 ? $"   [x{have}]" : "");
                int tier = (int)s.tier;
                string nm = s.cardName;
                MakeRow(catalogContent, label, SpellColor,
                    () => TryAdd(entry, tier, nm));
            }
        }
    }

    static void RefreshDeck()
    {
        Clear(deckContent);

        // Agrupa por entrada, ordena por (custo de mana, nome)
        var counts = new Dictionary<string, int>();
        foreach (string e in working)
        {
            int c; counts.TryGetValue(e, out c); counts[e] = c + 1;
        }

        var rows = new List<KeyValuePair<string, int>>(counts);
        rows.Sort((a, b) =>
        {
            int ca = EntryCost(a.Key), cb = EntryCost(b.Key);
            if (ca != cb) return ca.CompareTo(cb);
            return string.Compare(EntryLabel(a.Key), EntryLabel(b.Key), System.StringComparison.Ordinal);
        });

        foreach (var kv in rows)
        {
            string entry = kv.Key;
            MakeRow(deckContent, $"{kv.Value}x  {EntryLabel(entry)}  ({EntryCost(entry)} mana)",
                EntryColor(entry), () => RemoveOne(entry));
        }
    }

    static string EntryLabel(string entry)
    {
        if (entry.StartsWith("s"))
        {
            int id; SpellCard s = int.TryParse(entry.Substring(1), out id) ? SpellCards.Get(id) : null;
            return s != null ? s.cardName : entry;
        }
        return entry;
    }

    static int EntryCost(string entry)
    {
        if (entry.StartsWith("s"))
        {
            int id; SpellCard s = int.TryParse(entry.Substring(1), out id) ? SpellCards.Get(id) : null;
            return s != null ? s.cost : 0;
        }
        foreach (var u in DeckCatalog.Units) if (u.cardName == entry) return u.tier;
        return 0;
    }

    static Color EntryColor(string entry)
    {
        if (entry.StartsWith("s")) return SpellColor;
        foreach (var u in DeckCatalog.Units)
            if (u.cardName == entry) return ClassColors[u.classIdx];
        return TextLight;
    }

    // ── Helpers de UI (versões enxutas dos do LobbyUI) ───────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Clear(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    // Área com scroll vertical; columns controla o grid interno
    static Transform MakeScrollArea(Transform parent, string name, Vector2 pos, Vector2 size, int columns)
    {
        GameObject frame = new GameObject(name, typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(parent, false);
        RectTransform frt = frame.GetComponent<RectTransform>();
        frt.anchoredPosition = pos;
        frt.sizeDelta = size;
        frame.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(frame.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(8f, 0f);
        crt.offsetMax = new Vector2(-8f, 0f);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2((size.x - 24f - (columns - 1) * 8f) / columns, 34f);
        grid.spacing = new Vector2(8f, 6f);
        grid.padding = new RectOffset(0, 0, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = frame.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        return content.transform;
    }

    static void MakeRow(Transform parent, string label, Color color, System.Action onClick)
    {
        if (parent == null) return;
        GameObject row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button));
        row.transform.SetParent(parent, false);
        LobbySprites.MakeRounded(row.GetComponent<Image>(), new Color(0.14f, 0.12f, 0.09f, 0.95f));
        row.GetComponent<Button>().onClick.AddListener(() => onClick());

        TMP_Text t = MakeText(row.transform, "L", label, 14.5f, color,
            TextAlignmentOptions.MidlineLeft, Vector2.zero, Vector2.zero);
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(12f, 0f); trt.offsetMax = new Vector2(-6f, 0f);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
    }

    static TMP_Text MakeText(Transform parent, string name, string content, float size, Color color,
        TextAlignmentOptions align, Vector2 pos, Vector2 box, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        if (box != Vector2.zero) rt.sizeDelta = box;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color textColor, float fontSize, System.Action onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        LobbySprites.MakeRounded(go.GetComponent<Image>(), bg);
        if (onClick != null) go.GetComponent<Button>().onClick.AddListener(() => onClick());

        TMP_Text t = MakeText(go.transform, "L", label, fontSize, textColor,
            TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, FontStyles.Bold);
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        return go;
    }
}
