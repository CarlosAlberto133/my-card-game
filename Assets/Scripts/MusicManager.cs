using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Música de fundo do jogo. Persiste entre cenas (DontDestroyOnLoad) e troca a
// playlist conforme a cena: LOBBY toca lobby-1/lobby-2, a PARTIDA toca
// in-game-1..4. Em cada cena começa com uma faixa ALEATÓRIA e, quando ela
// acaba, segue para a próxima em círculo — sempre tocando.
//
// Os arquivos ficam em Assets/Resources/Sounds/. Se algum não existir, é
// pulado silenciosamente (o jogo não quebra).
//
// Volume e mute são SÓ da música (os efeitos sonoros do SoundManager não são
// afetados) e ficam salvos em PlayerPrefs. O botão + modal de ajuste são
// criados por código em qualquer cena que tenha um Canvas.
public class MusicManager : MonoBehaviour
{
    private static readonly string[] LobbyTracks = { "lobby-1", "lobby-2" };
    private static readonly string[] GameTracks = { "in-game-1", "in-game-2", "in-game-3", "in-game-4" };

    private const string PrefVolume = "music_volume";
    private const string PrefMuted = "music_muted";

    private static MusicManager instance;
    private AudioSource source;

    private string[] currentPlaylist;
    private int currentIndex;
    private bool playbackStarted;
    private string loadedCategory; // "lobby" | "game" | null

    private float volume = 0.6f;
    private bool muted = false;

    // Cria o manager assim que o jogo abre, antes de qualquer cena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        GameObject go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;      // a próxima faixa é escolhida por código
        source.spatialBlend = 0f; // 2D

        volume = PlayerPrefs.GetFloat(PrefVolume, 0.6f);
        muted = PlayerPrefs.GetInt(PrefMuted, 0) == 1;
        ApplyVolumeToSource();

        SceneManager.sceneLoaded += OnSceneLoaded;

        // A cena inicial já pode estar carregada quando o Awake roda
        HandleScene(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene);
    }

    void HandleScene(Scene scene)
    {
        string cat = null;
        if (scene.name == "Lobby") cat = "lobby";
        else if (scene.name == "SampleScene") cat = "game";

        if (cat == null)
        {
            // Cena desconhecida: para a música
            loadedCategory = null;
            if (source != null) source.Stop();
            playbackStarted = false;
            return;
        }

        // Só reinicia a playlist se MUDOU de categoria (lobby <-> partida).
        // Assim a música do lobby não recomeça se a cena recarregar por outro motivo.
        if (cat != loadedCategory)
        {
            loadedCategory = cat;
            currentPlaylist = cat == "lobby" ? LobbyTracks : GameTracks;
            currentIndex = currentPlaylist.Length > 0 ? Random.Range(0, currentPlaylist.Length) : 0;
            PlayCurrent();
        }

        // Recria o botão + modal no Canvas desta cena (a UI da cena anterior
        // foi destruída junto com ela)
        BuildUI();
    }

    void PlayCurrent()
    {
        if (source == null || currentPlaylist == null || currentPlaylist.Length == 0) return;

        AudioClip clip = Resources.Load<AudioClip>("Sounds/" + currentPlaylist[currentIndex]);
        if (clip == null)
        {
            // Faixa ausente: tenta a próxima (evita loop infinito com um limite)
            Debug.LogWarning($"[MusicManager] Faixa não encontrada: Sounds/{currentPlaylist[currentIndex]}");
            AdvanceTrack(guardAgainstAllMissing: true);
            return;
        }

        source.clip = clip;
        ApplyVolumeToSource();
        source.Play();
        playbackStarted = true;
    }

    private int missingStreak = 0;
    void AdvanceTrack(bool guardAgainstAllMissing = false)
    {
        if (currentPlaylist == null || currentPlaylist.Length == 0) return;

        if (guardAgainstAllMissing)
        {
            missingStreak++;
            if (missingStreak >= currentPlaylist.Length)
            {
                // Todas as faixas faltando: desiste para não travar
                missingStreak = 0;
                playbackStarted = false;
                return;
            }
        }
        else missingStreak = 0;

        currentIndex = (currentIndex + 1) % currentPlaylist.Length;
        PlayCurrent();
    }

    void Update()
    {
        // Faixa terminou de tocar → vai para a próxima (mute NÃO para o
        // playback, só zera o volume, então a rotação continua normalmente)
        if (playbackStarted && source != null && source.clip != null && !source.isPlaying)
        {
            AdvanceTrack();
        }
    }

    void ApplyVolumeToSource()
    {
        if (source == null) return;
        source.volume = volume;
        source.mute = muted;
    }

    // ==================== CONFIGURAÇÕES (volume/mute) ====================

    void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        ApplyVolumeToSource();
        PlayerPrefs.SetFloat(PrefVolume, volume);
        PlayerPrefs.Save();
        RefreshSettingsUI();
    }

    void SetMuted(bool m)
    {
        muted = m;
        ApplyVolumeToSource();
        PlayerPrefs.SetInt(PrefMuted, muted ? 1 : 0);
        PlayerPrefs.Save();
        RefreshSettingsUI();
    }

    // ==================== UI (botão + modal) ====================

    private GameObject settingsModal;
    private Slider volumeSlider;
    private Toggle muteToggle;
    private TextMeshProUGUI volumeLabel;

    void BuildUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Remove instâncias antigas (Awake e sceneLoaded podem ambos disparar
        // para a cena inicial — sem isto, dois botões seriam criados)
        foreach (Transform child in canvas.transform)
        {
            if (child.name == "MusicButton" || child.name == "MusicSettingsModal")
                Destroy(child.gameObject);
        }

        settingsModal = null;
        volumeSlider = null;
        muteToggle = null;
        volumeLabel = null;

        CreateMusicButton(canvas);
        CreateSettingsModal(canvas);
    }

    // Botão redondo com nota musical, no canto superior direito
    void CreateMusicButton(Canvas canvas)
    {
        GameObject btnObj = new GameObject("MusicButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(canvas.transform, false);
        // Canto superior ESQUERDO — o superior direito tem "Sair do Jogo" na partida
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(14f, -14f);
        rt.sizeDelta = new Vector2(52f, 52f);
        btnObj.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.30f, 0.95f);

        // Ícone de engrenagem (configurações). Desenhado por código como sprite —
        // o glifo ⚙ da fonte TMP padrão não existe (renderizaria como quadrado)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(34f, 34f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.sprite = GetGearSprite();
        iconImg.color = new Color(0.96f, 0.85f, 0.45f);
        iconImg.raycastTarget = false;

        btnObj.GetComponent<Button>().onClick.AddListener(ToggleSettingsModal);
    }

    // Sprite de engrenagem gerado por código (compartilhado). Anel com dentes +
    // furo central, com anti-aliasing simples na borda.
    private static Sprite gearSprite;

    static Sprite GetGearSprite()
    {
        if (gearSprite != null) return gearSprite;

        const int size = 64;
        const int teeth = 8;
        float cx = size / 2f - 0.5f;
        float cy = size / 2f - 0.5f;
        float baseR = size * 0.30f;   // corpo do anel
        float toothR = size * 0.44f;  // ponta dos dentes
        float holeR = size * 0.13f;   // furo central

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx); // -π..π

                // Onda quadrada dos dentes: metade do período é "dente" (raio maior)
                float phase = Mathf.Repeat(ang / (2f * Mathf.PI) * teeth, 1f);
                float outerR = phase < 0.5f ? toothR : baseR;

                // Alpha: dentro do anel (entre furo e borda externa), com borda suave
                float aOuter = Mathf.Clamp01(outerR - r);      // 1 dentro, 0 fora
                float aInner = Mathf.Clamp01(r - holeR);       // 1 fora do furo
                float alpha = Mathf.Min(aOuter, aInner);

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        gearSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), 100f);
        return gearSprite;
    }

    void ToggleSettingsModal()
    {
        if (settingsModal == null) return;
        bool show = !settingsModal.activeSelf;
        settingsModal.SetActive(show);
        if (show)
        {
            settingsModal.transform.SetAsLastSibling();
            RefreshSettingsUI();
        }
    }

    void CreateSettingsModal(Canvas canvas)
    {
        // Só na partida (SampleScene) faz sentido "Sair da partida"; no lobby o
        // modal mostra só "Fechar o jogo"
        bool inGame = loadedCategory == "game";

        settingsModal = new GameObject("MusicSettingsModal",
            typeof(RectTransform), typeof(Image));
        settingsModal.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = settingsModal.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        float panelH = inGame ? 400f : 330f;
        panelRt.sizeDelta = new Vector2(440f, panelH);
        settingsModal.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.13f, 0.98f);

        float half = panelH / 2f;

        // Título (agora o modal tem mais que música)
        MakeText(settingsModal.transform, "Title", "CONFIGURAÇÕES",
            new Vector2(0.5f, 0.5f), new Vector2(0f, half - 32f), new Vector2(400f, 40f),
            24f, FontStyles.Bold, new Color(0.96f, 0.77f, 0.32f));

        // Botão fechar (X)
        GameObject closeObj = new GameObject("Close",
            typeof(RectTransform), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(settingsModal.transform, false);
        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-8f, -8f);
        closeRt.sizeDelta = new Vector2(32f, 32f);
        closeObj.GetComponent<Image>().color = new Color(0.75f, 0.22f, 0.20f, 0.95f);
        closeObj.GetComponent<Button>().onClick.AddListener(() => settingsModal.SetActive(false));
        MakeText(closeObj.transform, "X", "X",
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f),
            18f, FontStyles.Bold, Color.white);

        // Rótulo do volume (mostra a %)
        volumeLabel = MakeText(settingsModal.transform, "VolLabel", "Volume da música: 60%",
            new Vector2(0.5f, 0.5f), new Vector2(0f, half - 92f), new Vector2(400f, 30f),
            19f, FontStyles.Normal, Color.white);

        // Linha do slider: [ - ] [==== slider ====] [ + ]
        float sliderY = half - 140f;
        MakeStepButton(settingsModal.transform, "-", new Vector2(-176f, sliderY), () => SetVolume(volume - 0.1f));
        volumeSlider = MakeSlider(settingsModal.transform, new Vector2(0f, sliderY), new Vector2(260f, 24f));
        volumeSlider.value = volume;
        volumeSlider.onValueChanged.AddListener(SetVolume);
        MakeStepButton(settingsModal.transform, "+", new Vector2(176f, sliderY), () => SetVolume(volume + 0.1f));

        // Checkbox de mute
        CreateMuteToggle(settingsModal.transform, new Vector2(0f, half - 200f));

        // Botões de saída (abaixo do mute). "Sair da partida" só na partida.
        if (inGame)
        {
            MakeWideButton(settingsModal.transform, "Sair da partida",
                new Color(0.55f, 0.36f, 0.10f, 1f), new Vector2(0f, -half + 130f), OnLeaveMatch);
            MakeWideButton(settingsModal.transform, "Fechar o jogo",
                new Color(0.55f, 0.12f, 0.12f, 1f), new Vector2(0f, -half + 68f), OnQuitGame);
        }
        else
        {
            MakeWideButton(settingsModal.transform, "Fechar o jogo",
                new Color(0.55f, 0.12f, 0.12f, 1f), new Vector2(0f, -half + 68f), OnQuitGame);
        }

        settingsModal.SetActive(false);
    }

    // Botão largo (usado pelas opções de saída dentro do modal)
    void MakeWideButton(Transform parent, string label, Color color, Vector2 pos,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Btn_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300f, 48f);
        btnObj.GetComponent<Image>().color = color;
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
        MakeText(btnObj.transform, "L", label,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 48f),
            22f, FontStyles.Bold, Color.white);
    }

    // Sai da sala do Photon e volta para a cena do lobby (não fecha o jogo)
    void OnLeaveMatch()
    {
        if (settingsModal != null) settingsModal.SetActive(false);
        Debug.Log("[MusicManager] Saindo da partida, voltando ao lobby...");
        if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Lobby");
    }

    // Fecha o jogo (desconecta do Photon antes, avisando o oponente)
    void OnQuitGame()
    {
        Debug.Log("[MusicManager] Fechando o jogo...");
        if (PhotonNetwork.connected) PhotonNetwork.Disconnect();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void CreateMuteToggle(Transform parent, Vector2 anchoredPos)
    {
        GameObject toggleObj = new GameObject("MuteToggle",
            typeof(RectTransform), typeof(Toggle));
        toggleObj.transform.SetParent(parent, false);
        RectTransform rt = toggleObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(280f, 34f);

        // Caixinha
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(toggleObj.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0f, 0.5f);
        boxRt.anchorMax = new Vector2(0f, 0.5f);
        boxRt.pivot = new Vector2(0f, 0.5f);
        boxRt.anchoredPosition = new Vector2(0f, 0f);
        boxRt.sizeDelta = new Vector2(28f, 28f);
        box.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.34f, 1f);

        // Marca de check
        GameObject check = new GameObject("Check", typeof(RectTransform), typeof(Image));
        check.transform.SetParent(box.transform, false);
        RectTransform checkRt = check.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.15f, 0.15f);
        checkRt.anchorMax = new Vector2(0.85f, 0.85f);
        checkRt.offsetMin = Vector2.zero; checkRt.offsetMax = Vector2.zero;
        check.GetComponent<Image>().color = new Color(0.96f, 0.77f, 0.32f);

        // Rótulo
        MakeText(toggleObj.transform, "Label", "Mutar apenas a música",
            new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(240f, 30f),
            18f, FontStyles.Normal, Color.white, TextAlignmentOptions.Left);

        muteToggle = toggleObj.GetComponent<Toggle>();
        muteToggle.targetGraphic = box.GetComponent<Image>();
        muteToggle.graphic = check.GetComponent<Image>();
        muteToggle.isOn = muted;
        muteToggle.onValueChanged.AddListener(SetMuted);
    }

    void RefreshSettingsUI()
    {
        if (volumeLabel != null)
            volumeLabel.text = $"Volume da música: {Mathf.RoundToInt(volume * 100f)}%";
        if (volumeSlider != null && !Mathf.Approximately(volumeSlider.value, volume))
        {
            volumeSlider.SetValueWithoutNotify(volume);
        }
        if (muteToggle != null && muteToggle.isOn != muted)
        {
            muteToggle.SetIsOnWithoutNotify(muted);
        }
    }

    // ---- helpers de UI ----

    TextMeshProUGUI MakeText(Transform parent, string name, string text,
        Vector2 anchor, Vector2 pos, Vector2 size, float fontSize,
        FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        return tmp;
    }

    void MakeStepButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject("Step" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(38f, 34f);
        btnObj.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.34f, 1f);
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
        MakeText(btnObj.transform, "L", label,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 34f),
            24f, FontStyles.Bold, Color.white);
    }

    // Slider de volume seguindo a estrutura padrão do Unity UI
    Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Trilho de fundo
        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.18f, 0.20f, 0.30f, 1f);

        // Área de preenchimento
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-15f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.sizeDelta = new Vector2(10f, 0f);
        fill.GetComponent<Image>().color = new Color(0.96f, 0.77f, 0.32f);

        // Área do handle
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = new Vector2(0f, 0f);
        haRt.anchorMax = new Vector2(1f, 1f);
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 0f);
        hRt.anchorMax = new Vector2(0f, 1f);
        hRt.sizeDelta = new Vector2(20f, 0f);
        handle.GetComponent<Image>().color = new Color(1f, 0.93f, 0.70f);

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }
}
