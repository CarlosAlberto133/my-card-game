using TMPro;
using UnityEngine;
using UnityEngine.UI;

// LOJA MÁGICA DA TORRE: abre nos rounds 3, 6, 9... com a oferta do jogador
// local (1 carta da classe da torre + 2 universais, sorteio sincronizado do
// TowerSystem). Compra custa 3 de ouro e vai por RPC (executa nos 2 clientes).
// A oferta SOME quando o round vira. Também mantém os "chips" persistentes
// mostrando a torre + cartas equipadas dos dois jogadores.
public static class TowerMagicShopUI
{
    static readonly Color PanelBg = new Color(0.07f, 0.05f, 0.035f, 0.99f);
    static readonly Color Border = new Color(0.96f, 0.77f, 0.32f, 0.5f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color Ink = new Color(0.93f, 0.90f, 0.84f);
    static readonly Color Muted = new Color(0.62f, 0.56f, 0.46f);
    static readonly Color Slot = new Color(0.16f, 0.13f, 0.09f, 1f);

    static GameObject canvasGo, panel, reopenBtn, chipsGo;
    static TMP_Text chipsText, statusText;
    static int pendingBuyCardId = -1; // aguardando escolher qual equipada substituir
    static int currentOfferRound = -1;
    static readonly System.Collections.Generic.List<Button> buyButtons =
        new System.Collections.Generic.List<Button>();

    static int LocalPlayer()
    {
        if (BotMode.Enabled)
            return BotMode.BotPlayerNumber == 1 ? 2 : 1;
        return PhotonGameManager.Instance != null && PhotonGameManager.Instance.myPlayerNumber != 0
            ? PhotonGameManager.Instance.myPlayerNumber : 1;
    }

    static Canvas EnsureCanvas()
    {
        if (canvasGo == null)
        {
            canvasGo = new GameObject("TowerMagicCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas cv = canvasGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 700;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }
        return canvasGo.GetComponent<Canvas>();
    }

    // ── Janela de oferta (chamada pelo TowerSystem na virada do round) ───
    public static void OnOfferWindowOpened(int round)
    {
        currentOfferRound = round;
        pendingBuyCardId = -1;
        RefreshChips();

        int me = LocalPlayer();
        int[] offer = TowerSystem.GetOffer(me, round);
        if (offer.Length == 0) return;

        BuildPanel(round, offer);
        ShowReopenButton(true);
    }

    public static void OnOfferWindowClosed()
    {
        currentOfferRound = -1;
        pendingBuyCardId = -1;
        if (panel != null) Object.Destroy(panel);
        panel = null;
        ShowReopenButton(false);
        RefreshChips();
    }

    // Após uma compra (dos dois lados): re-renderiza chips e o painel local
    public static void RefreshAfterPurchase(int buyerPlayer)
    {
        RefreshChips();
        if (buyerPlayer == LocalPlayer() && panel != null)
        {
            Object.Destroy(panel);
            panel = null;
            ShowReopenButton(false);
        }
    }

    static void ShowReopenButton(bool show)
    {
        if (show)
        {
            if (reopenBtn == null)
            {
                Canvas cv = EnsureCanvas();
                reopenBtn = MakeButton(cv.transform, "» Magia da Torre", new Vector2(-190f, 240f),
                    new Vector2(220f, 46f), Gold, new Color(0.12f, 0.09f, 0.02f), 16, () =>
                    {
                        int me = LocalPlayer();
                        int[] offer = TowerSystem.GetOffer(me, currentOfferRound);
                        if (offer.Length > 0 && panel == null) BuildPanel(currentOfferRound, offer);
                    });
                RectTransform rt = reopenBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-24f, 240f);
            }
            reopenBtn.SetActive(true);
        }
        else if (reopenBtn != null) reopenBtn.SetActive(false);
    }

    static void BuildPanel(int round, int[] offer)
    {
        if (panel != null) Object.Destroy(panel);
        Canvas cv = EnsureCanvas();
        int me = LocalPlayer();

        panel = new GameObject("TowerMagicPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(cv.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(1f, 0.5f);
        prt.anchorMax = new Vector2(1f, 0.5f);
        prt.pivot = new Vector2(1f, 0.5f);
        prt.anchoredPosition = new Vector2(-24f, -40f);
        prt.sizeDelta = new Vector2(400f, 660f);
        LobbySprites.MakeRounded(panel.GetComponent<Image>(), PanelBg);
        LobbySprites.AddRing(panel.transform, Border);

        MakeText(panel.transform, "T", "» MAGIA DA TORRE", 22, Gold, TextAlignmentOptions.Center,
            FontStyles.Bold, new Vector2(0f, 300f), new Vector2(360f, 30f));
        string towerName = TowerCards.TowerName(TowerSystem.TowerClassOf(me));
        MakeText(panel.transform, "S", towerName + " — round " + round + " · escolha 1 (custa " +
            TowerCard.GoldCost + " de ouro)", 13.5f, Muted, TextAlignmentOptions.Center,
            FontStyles.Normal, new Vector2(0f, 272f), new Vector2(370f, 22f));

        MakeButton(panel.transform, "X", new Vector2(178f, 304f), new Vector2(32f, 32f),
            new Color(0.55f, 0.18f, 0.16f), Color.white, 16, () =>
            { if (panel != null) { Object.Destroy(panel); panel = null; } });

        statusText = MakeText(panel.transform, "St", "", 13.5f, Gold, TextAlignmentOptions.Center,
            FontStyles.Normal, new Vector2(0f, 246f), new Vector2(370f, 22f));

        buyButtons.Clear();
        for (int i = 0; i < offer.Length && i < 3; i++)
        {
            TowerCard card = TowerCards.Get(offer[i]);
            if (card == null) continue;
            BuildOfferEntry(panel.transform, card, new Vector2(0f, 140f - i * 182f), me);
        }

        // Reavaliação AO VIVO dos botões: o ouro muda durante o round (renda,
        // efeitos, compras) e o botão precisa acompanhar — antes ele era
        // calculado só na abertura e ficava travado com o saldo velho
        panel.AddComponent<LiveRefresher>();

        LiveRefresh();
        panel.transform.SetAsLastSibling();
    }

    static void BuildOfferEntry(Transform parent, TowerCard card, Vector2 pos, int me)
    {
        GameObject go = new GameObject("Offer_" + card.cardName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Center(go.GetComponent<RectTransform>(), pos, new Vector2(360f, 170f));
        LobbySprites.MakeRounded(go.GetComponent<Image>(), Slot);

        // Nome + tag + descrição num TEXTO ÚNICO (não tem como sobrepor);
        // o botão fica num degrau fixo abaixo, com folga
        bool isClass = card.classIdx >= 0;
        string tag = isClass ? TowerCards.ClassLabel(card.classIdx) : "Universal";
        var t = MakeText(go.transform, "Txt",
            "<b><color=#F5C451>» " + card.cardName + "</color></b> <size=70%><i>" + tag + "</i></size>\n" +
            "<size=82%>" + card.description + "</size>",
            15, Ink, TextAlignmentOptions.TopLeft, FontStyles.Normal,
            new Vector2(0f, 22f), new Vector2(324f, 106f));
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMin = 10f;
        t.fontSizeMax = 15f;

        int cardId = card.id;
        GameObject buy = MakeButton(go.transform, "Comprar (" + TowerCard.GoldCost + " ouro)",
            new Vector2(0f, -62f), new Vector2(210f, 30f), Gold, new Color(0.12f, 0.09f, 0.02f), 13,
            () => OnBuyClicked(cardId));

        // O estado (ouro/já comprou) é reavaliado AO VIVO pelo LiveRefresher
        buyButtons.Add(buy.GetComponent<Button>());
    }

    // Reavalia botões + status a cada 0,25s enquanto o painel existe
    class LiveRefresher : MonoBehaviour
    {
        float nextTick;
        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.25f;
            LiveRefresh();
        }
    }

    static void LiveRefresh()
    {
        if (panel == null || pendingBuyCardId >= 0) return; // diálogo de troca aberto

        int me = LocalPlayer();
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(me) : null;
        bool canBuy = p != null && p.gold >= TowerCard.GoldCost &&
                      !TowerSystem.HasBoughtThisWindow(me, currentOfferRound);

        foreach (Button b in buyButtons)
            if (b != null) b.interactable = canBuy;

        UpdatePanelStatus(me);
    }

    static void UpdatePanelStatus(int me)
    {
        if (statusText == null) return;
        TurnManager tm = TurnManager.Instance;
        PlayerData p = tm != null ? tm.GetPlayer(me) : null;
        if (TowerSystem.HasBoughtThisWindow(me, currentOfferRound))
            statusText.text = "Você já comprou nesta janela.";
        else if (p != null && p.gold < TowerCard.GoldCost)
            statusText.text = "Ouro insuficiente (" + p.gold + "/" + TowerCard.GoldCost + ").";
        else if (TowerSystem.EquippedOf(me).Count >= TowerSystem.MaxEquipped)
            statusText.text = "Slots cheios: comprar vai pedir qual substituir.";
        else
            statusText.text = "A oferta some quando o round virar!";
    }

    static void OnBuyClicked(int cardId)
    {
        int me = LocalPlayer();
        var equipped = TowerSystem.EquippedOf(me);

        if (equipped.Count < TowerSystem.MaxEquipped)
        {
            SendBuy(me, cardId, -1);
            return;
        }

        // Slots cheios: pergunta qual substituir
        pendingBuyCardId = cardId;
        BuildReplaceDialog(me);
    }

    static void BuildReplaceDialog(int me)
    {
        if (panel == null) return;
        // Substitui o conteúdo do painel por uma pergunta simples
        foreach (Transform child in panel.transform)
            if (child.name.StartsWith("Offer_") || child.name == "St") child.gameObject.SetActive(false);

        var equipped = TowerSystem.EquippedOf(me);
        TowerCard nova = TowerCards.Get(pendingBuyCardId);
        MakeText(panel.transform, "RepT", "Substituir qual carta por\n<b>" + (nova != null ? nova.cardName : "?") + "</b>?",
            17, Ink, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0f, 90f), new Vector2(340f, 60f));

        // 3 slots (v4.2): botões mais compactos para caber tudo + o Cancelar
        for (int s = 0; s < equipped.Count && s < TowerSystem.MaxEquipped; s++)
        {
            TowerCard old = TowerCards.Get(equipped[s]);
            int slot = s;
            MakeButton(panel.transform, "Substituir: " + (old != null ? old.cardName : "?"),
                new Vector2(0f, 26f - s * 52f), new Vector2(300f, 44f), Slot, Ink, 14,
                () => { SendBuy(me, pendingBuyCardId, slot); });
        }
        MakeButton(panel.transform, "Cancelar", new Vector2(0f, -140f), new Vector2(300f, 44f),
            new Color(0.55f, 0.18f, 0.16f), Color.white, 14, () =>
            {
                pendingBuyCardId = -1;
                int[] offer = TowerSystem.GetOffer(me, currentOfferRound);
                if (offer.Length > 0) BuildPanel(currentOfferRound, offer);
            });
    }

    static void SendBuy(int player, int cardId, int replaceSlot)
    {
        if (PhotonGameManager.Instance != null)
            PhotonGameManager.Instance.SendBuyTowerCardRPC(player, cardId, replaceSlot);
    }

    // ── Chips persistentes (torre + equipadas dos 2 jogadores) ───────────
    // Canto inferior DIREITO. As SUAS magias aparecem com nome + descrição;
    // as do oponente só com o nome (compacto). Painel cresce com o conteúdo.
    public static void RefreshChips()
    {
        int me = LocalPlayer();
        int other = me == 1 ? 2 : 1;
        if (TowerSystem.TowerClassOf(me) < 0 && TowerSystem.TowerClassOf(other) < 0)
        {
            if (chipsGo != null) chipsGo.SetActive(false);
            return;
        }

        const float PanelW = 470f, TextW = 434f;
        if (chipsGo == null)
        {
            Canvas cv = EnsureCanvas();
            chipsGo = new GameObject("TowerChips", typeof(RectTransform), typeof(Image));
            chipsGo.transform.SetParent(cv.transform, false);
            RectTransform rt = chipsGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-16f, 14f);
            rt.sizeDelta = new Vector2(PanelW, 80f);
            LobbySprites.MakeRounded(chipsGo.GetComponent<Image>(), new Color(0.06f, 0.045f, 0.03f, 0.78f));
            chipsGo.GetComponent<Image>().raycastTarget = false;

            chipsText = MakeText(chipsGo.transform, "T", "", 13, Ink, TextAlignmentOptions.TopLeft,
                FontStyles.Normal, Vector2.zero, new Vector2(TextW, 64f));
            chipsText.textWrappingMode = TextWrappingModes.Normal;
        }

        chipsGo.SetActive(true);
        chipsText.text = OwnChipBlock(me) + "\n" + EnemyChipLine(other);

        // Altura do painel acompanha o texto (nome+descrição variam)
        chipsText.rectTransform.sizeDelta = new Vector2(TextW, 1000f);
        chipsText.ForceMeshUpdate();
        float h = chipsText.preferredHeight;
        chipsText.rectTransform.sizeDelta = new Vector2(TextW, h);
        chipsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelW, h + 24f);
    }

    // Bloco do jogador local: torre + cada magia com nome E descrição
    static string OwnChipBlock(int player)
    {
        int cls = TowerSystem.TowerClassOf(player);
        if (cls < 0) return "<b>Sua torre</b>: <i>—</i>";

        string s = "<b>Sua torre</b> <color=#F5C451>" + TowerCards.TowerName(cls) + "</color>:";
        var eq = TowerSystem.EquippedOf(player);
        if (eq.Count == 0) return s + " <i>sem magias equipadas</i>";

        for (int i = 0; i < eq.Count; i++)
        {
            TowerCard c = TowerCards.Get(eq[i]);
            if (c == null) continue;
            s += "\n<color=#F5C451>» " + c.cardName + "</color> — <color=#B6A07C><size=12>" +
                 c.description + "</size></color>";
        }
        return s;
    }

    // Linha do oponente: só torre + nomes (compacto; dá pra ver o que ele tem)
    static string EnemyChipLine(int player)
    {
        int cls = TowerSystem.TowerClassOf(player);
        if (cls < 0) return "<b>Torre inimiga</b>: <i>—</i>";
        var eq = TowerSystem.EquippedOf(player);
        string cards = "";
        for (int i = 0; i < eq.Count; i++)
        {
            TowerCard c = TowerCards.Get(eq[i]);
            if (c == null) continue;
            cards += (i > 0 ? " · " : "") + c.cardName;
        }
        if (cards == "") cards = "<i>sem magias</i>";
        return "<b>Torre inimiga</b> <color=#F5C451>" + TowerCards.TowerName(cls) + "</color>: " + cards;
    }

    // ── helpers de UI ────────────────────────────────────────────────────
    static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static TMP_Text MakeText(Transform parent, string name, string text, float size, Color color,
        TextAlignmentOptions align, FontStyles style, Vector2 pos, Vector2 size2)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Center(go.GetComponent<RectTransform>(), pos, size2);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.alignment = align; tmp.fontStyle = style; tmp.richText = true; tmp.raycastTarget = false;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, Color fg, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Center(go.GetComponent<RectTransform>(), pos, size);
        LobbySprites.MakeRounded(go.GetComponent<Image>(), bg);
        go.GetComponent<Button>().onClick.AddListener(onClick);
        MakeText(go.transform, "L", label, fontSize, fg, TextAlignmentOptions.Center,
            FontStyles.Bold, Vector2.zero, size);
        return go;
    }
}
