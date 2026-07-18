using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// TELA DE ESCOLHA DA TORRE (início da partida): overlay em tela cheia com as 4
// torres — o bonequinho 3D da classe girando (renderizado num "photobooth"
// escondido via RenderTexture) e as 3 cartas mágicas de classe embaixo.
//
// Sincronização: cada jogador grava a escolha em room property ("tower1"/
// "tower2"); os dois clientes fazem poll e só liberam o jogo quando AMBAS
// existem. Contra o bot, a escolha dele é sorteada na hora (seed da partida).
// Enquanto aberto, o input do jogo fica bloqueado (GameManager checa IsOpen).
public static class TowerSelectUI
{
    static readonly Color PanelBg = new Color(0.07f, 0.05f, 0.035f, 0.99f);
    static readonly Color ColBg = new Color(0.11f, 0.085f, 0.055f, 0.98f);
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color Ink = new Color(0.93f, 0.90f, 0.84f);
    static readonly Color Muted = new Color(0.62f, 0.56f, 0.46f);
    static readonly Color Slot = new Color(0.16f, 0.13f, 0.09f, 1f);

    public static bool IsOpen { get; private set; }

    const float PickTimeout = 30f;
    static readonly int[] classOrder = { (int)CardClass.Tank, (int)CardClass.Healer, (int)CardClass.Mago, (int)CardClass.Arqueiro };

    static GameObject canvasGo, boothRoot;
    static Camera boothCam;
    static readonly GameObject[] boothModels = new GameObject[4];
    static readonly RenderTexture[] boothRts = new RenderTexture[4];
    static readonly Image[] colRings = new Image[4];
    static Button confirmBtn;
    static TMP_Text statusText, timerText;
    static Runner runner;

    static int selectedIdx = -1;   // índice em classOrder
    static bool confirmed;
    static float openedAt;
    static int myPick = -1, otherPick = -1;

    // ── Abertura (TurnManager.StartGame) ─────────────────────────────────
    public static void Open()
    {
        Close(); // limpa resto de partida anterior
        IsOpen = true;
        selectedIdx = -1;
        confirmed = false;
        myPick = otherPick = -1;
        openedAt = Time.realtimeSinceStartup;

        // Contra o bot: o pick dele sai na hora, sorteado pela seed da partida
        if (BotMode.Enabled)
        {
            int seed = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.currentGameSeed : 999;
            otherPick = classOrder[new System.Random(seed * 7 + 13).Next(classOrder.Length)];
        }

        BuildBooth();
        BuildUI();

        GameObject runnerGo = new GameObject("TowerSelectRunner");
        runner = runnerGo.AddComponent<Runner>();
    }

    static int MyPlayerNumber()
    {
        if (BotMode.Enabled)
            return BotMode.BotPlayerNumber == 1 ? 2 : 1;
        return PhotonGameManager.Instance != null && PhotonGameManager.Instance.myPlayerNumber != 0
            ? PhotonGameManager.Instance.myPlayerNumber : 1;
    }

    // Chave da escolha nas room properties, ESCOPADA pela seed da partida —
    // o restart sincronizado gera seed nova, então a escolha da partida
    // anterior nunca "vaza" para a seleção seguinte
    static string TowerPropKey(int player)
    {
        int seed = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.currentGameSeed : 0;
        return "tw" + seed + "_p" + player;
    }

    // ── Photobooth (modelos girando fora da vista da câmera principal) ───
    static void BuildBooth()
    {
        boothRoot = new GameObject("TowerSelectBooth");
        boothRoot.transform.position = new Vector3(0f, -500f, 0f);

        GameObject camGo = new GameObject("BoothCam");
        camGo.transform.SetParent(boothRoot.transform, false);
        boothCam = camGo.AddComponent<Camera>();
        boothCam.enabled = false; // só renderiza sob demanda (Render())
        boothCam.clearFlags = CameraClearFlags.SolidColor;
        boothCam.backgroundColor = new Color(0.05f, 0.038f, 0.027f, 1f);
        boothCam.fieldOfView = 28f;
        boothCam.nearClipPlane = 0.1f;
        boothCam.farClipPlane = 30f;

        string[] paths = { "Models/personagem_tank", "Models/personagem_healer",
                           "Models/personagem_mago", "Models/personagem_arqueiro" };
        for (int i = 0; i < 4; i++)
        {
            boothRts[i] = new RenderTexture(256, 300, 16);

            GameObject prefab = Resources.Load<GameObject>(paths[i]);
            if (prefab == null) continue;
            GameObject m = Object.Instantiate(prefab, boothRoot.transform);
            m.transform.localPosition = new Vector3(i * 12f, 0f, 0f);

            // Normaliza a altura (modelos vêm em escalas diferentes)
            Bounds b = new Bounds(m.transform.position, Vector3.zero);
            bool has = false;
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            if (has && b.size.y > 0.001f)
                m.transform.localScale = m.transform.localScale * (2.2f / b.size.y);
            boothModels[i] = m;
        }
    }

    // Chamado pelo Runner a cada frame: gira e renderiza cada modelo no seu RT
    static void RenderBooth()
    {
        if (boothCam == null) return;
        for (int i = 0; i < 4; i++)
        {
            GameObject m = boothModels[i];
            if (m == null || boothRts[i] == null) continue;
            m.transform.Rotate(0f, 24f * Time.deltaTime, 0f, Space.World);

            // Centro real do modelo (bounds) para enquadrar
            Bounds b = new Bounds(m.transform.position, Vector3.zero);
            bool has = false;
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>())
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            if (!has) continue;

            boothCam.transform.position = b.center + new Vector3(0f, 0.15f, -4.6f);
            boothCam.transform.LookAt(b.center);
            boothCam.targetTexture = boothRts[i];
            boothCam.Render();
        }
        boothCam.targetTexture = null;
    }

    // ── UI ───────────────────────────────────────────────────────────────
    static void BuildUI()
    {
        canvasGo = new GameObject("TowerSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas cv = canvasGo.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 800;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Fundo escuro que bloqueia cliques no jogo
        GameObject bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        Stretch(bg);
        bg.GetComponent<Image>().color = new Color(0.02f, 0.015f, 0.01f, 0.94f);

        MakeText(bg.transform, "Title", "ESCOLHA SUA TORRE", 40, Gold, TextAlignmentOptions.Center,
            FontStyles.Bold, new Vector2(0f, 480f), new Vector2(1200f, 54f));
        MakeText(bg.transform, "Sub", "A torre define quais cartas mágicas de classe aparecem para você nos rounds 3, 6, 9...",
            17, Muted, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0f, 438f), new Vector2(1300f, 26f));

        // 4 colunas
        float colW = 330f, gap = 26f;
        float startX = -(colW * 3f + gap * 3f) / 2f;
        for (int i = 0; i < 4; i++)
            BuildColumn(bg.transform, i, new Vector2(startX + i * (colW + gap), 10f), colW);

        // Confirmar + status + timer
        GameObject btn = MakeButton(bg.transform, "CONFIRMAR TORRE", new Vector2(0f, -450f),
            new Vector2(320f, 58f), Gold, new Color(0.12f, 0.09f, 0.02f), 21, OnConfirm);
        confirmBtn = btn.GetComponent<Button>();
        confirmBtn.interactable = false;

        statusText = MakeText(bg.transform, "Status", "Clique numa torre para ver e selecionar",
            16, Muted, TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0f, -498f), new Vector2(900f, 24f));
        timerText = MakeText(bg.transform, "Timer", "", 15, Gold, TextAlignmentOptions.Center,
            FontStyles.Bold, new Vector2(0f, -524f), new Vector2(400f, 22f));
    }

    static void BuildColumn(Transform parent, int i, Vector2 pos, float width)
    {
        int classIdx = classOrder[i];

        GameObject col = new GameObject("Col_" + TowerCards.ClassLabel(classIdx),
            typeof(RectTransform), typeof(Image), typeof(Button));
        col.transform.SetParent(parent, false);
        RectTransform rt = col.GetComponent<RectTransform>();
        Center(rt, pos, new Vector2(width, 800f));
        LobbySprites.MakeRounded(col.GetComponent<Image>(), ColBg);
        int idx = i;
        col.GetComponent<Button>().onClick.AddListener(() => OnSelect(idx));

        // Anel de seleção (aparece dourado quando escolhida)
        GameObject ringGo = LobbySprites.AddRing(col.transform, new Color(0f, 0f, 0f, 0f));
        colRings[i] = ringGo.GetComponent<Image>();

        // Bonequinho 3D (RenderTexture do photobooth)
        GameObject raw = new GameObject("Figure", typeof(RectTransform), typeof(RawImage));
        raw.transform.SetParent(col.transform, false);
        Center(raw.GetComponent<RectTransform>(), new Vector2(0f, 250f), new Vector2(width - 40f, 268f));
        RawImage ri = raw.GetComponent<RawImage>();
        ri.texture = boothRts[i];
        ri.raycastTarget = false;

        MakeText(col.transform, "Name", TowerCards.TowerName(classIdx), 26, Gold,
            TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(0f, 100f), new Vector2(width - 20f, 34f));
        MakeText(col.transform, "Cls", "Torre " + TowerCards.ClassLabel(classIdx), 14, Muted,
            TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0f, 74f), new Vector2(width - 20f, 20f));

        // As 3 cartas mágicas de classe. Nome + descrição num TEXTO ÚNICO
        // (rich text): impossível um sobrepor o outro; o auto-ajuste encolhe
        // a fonte se a descrição for comprida.
        var cards = TowerCards.OfClass(classIdx);
        for (int c = 0; c < cards.Count && c < 3; c++)
        {
            GameObject cardGo = new GameObject("Magic_" + cards[c].cardName, typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(col.transform, false);
            Center(cardGo.GetComponent<RectTransform>(), new Vector2(0f, -18f - c * 132f), new Vector2(width - 28f, 124f));
            LobbySprites.MakeRounded(cardGo.GetComponent<Image>(), Slot);
            cardGo.GetComponent<Image>().raycastTarget = false;

            var t = MakeText(cardGo.transform, "Txt",
                "<b><color=#F5C451>» " + cards[c].cardName + "</color></b>\n<size=80%>" + cards[c].description + "</size>",
                14, Ink, TextAlignmentOptions.TopLeft, FontStyles.Normal,
                Vector2.zero, new Vector2(width - 56f, 104f));
            t.textWrappingMode = TextWrappingModes.Normal;
            t.enableAutoSizing = true;
            t.fontSizeMin = 9f;
            t.fontSizeMax = 14f;
        }
    }

    static void OnSelect(int idx)
    {
        if (confirmed) return;
        selectedIdx = idx;
        for (int i = 0; i < 4; i++)
        {
            if (colRings[i] != null)
                colRings[i].color = i == idx ? Gold : new Color(0f, 0f, 0f, 0f);
        }
        if (confirmBtn != null) confirmBtn.interactable = true;
        if (statusText != null)
            statusText.text = TowerCards.TowerName(classOrder[idx]) + " selecionada — confirme para travar";
    }

    static void OnConfirm()
    {
        if (confirmed || selectedIdx < 0) return;
        confirmed = true;
        myPick = classOrder[selectedIdx];
        if (confirmBtn != null) confirmBtn.interactable = false;
        if (statusText != null) statusText.text = "Aguardando o oponente escolher a torre dele…";

        // Online: publica a escolha via room property (os dois clientes fazem poll)
        if (!BotMode.Enabled && PhotonNetwork.inRoom)
        {
            Hashtable props = new Hashtable();
            props[TowerPropKey(MyPlayerNumber())] = myPick;
            PhotonNetwork.room.SetCustomProperties(props);
        }
    }

    // Poll do estado (Runner.Update): props da sala, timeout e finalização
    static void Tick()
    {
        if (!IsOpen) return;

        RenderBooth();

        // Timeout: escolhe uma torre aleatória por você
        float left = PickTimeout - (Time.realtimeSinceStartup - openedAt);
        if (!confirmed)
        {
            if (timerText != null)
                timerText.text = "Escolha automática em " + Mathf.CeilToInt(Mathf.Max(0f, left)) + "s";
            if (left <= 0f)
            {
                if (selectedIdx < 0) OnSelect(Random.Range(0, 4));
                OnConfirm();
            }
        }
        else if (timerText != null) timerText.text = "";

        // Online: lê a escolha do oponente nas props da sala (chave por seed)
        if (!BotMode.Enabled && PhotonNetwork.inRoom && PhotonNetwork.room.customProperties != null)
        {
            int me = MyPlayerNumber();
            int other = me == 1 ? 2 : 1;
            object v;
            if (otherPick < 0 && PhotonNetwork.room.customProperties.TryGetValue(TowerPropKey(other), out v))
                otherPick = (int)v;
            if (myPick < 0 && PhotonNetwork.room.customProperties.TryGetValue(TowerPropKey(me), out v))
                myPick = (int)v;
        }

        if (myPick >= 0 && otherPick >= 0) Finalize();
    }

    static void Finalize()
    {
        int me = MyPlayerNumber();
        int other = me == 1 ? 2 : 1;
        TowerSystem.SetTower(me, myPick);
        TowerSystem.SetTower(other, otherPick);
        Close();
        TowerMagicShopUI.RefreshChips();
        Debug.Log($"[TowerSelect] Torres definidas: P{me}={TowerCards.TowerName(myPick)}, P{other}={TowerCards.TowerName(otherPick)}");
    }

    static void Close()
    {
        IsOpen = false;
        if (canvasGo != null) Object.Destroy(canvasGo);
        if (boothRoot != null) Object.Destroy(boothRoot);
        if (runner != null) Object.Destroy(runner.gameObject);
        canvasGo = null; boothRoot = null; boothCam = null; runner = null;
        for (int i = 0; i < 4; i++)
        {
            boothModels[i] = null;
            if (boothRts[i] != null) { boothRts[i].Release(); Object.Destroy(boothRts[i]); boothRts[i] = null; }
            colRings[i] = null;
        }
    }

    class Runner : MonoBehaviour
    {
        void Update() { Tick(); }
    }

    // ── helpers de UI ────────────────────────────────────────────────────
    static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

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
