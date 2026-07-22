using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ═══════════ BANNER DE TROCA DE TURNO (v4.3, pedido do Carlos) ═══════════
// "SUA VEZ!" grandão: entra deslizando da DIREITA, para no CENTRO por 1s e
// sai deslizando pela ESQUERDA, com som. O indicador antigo (texto no topo)
// passava despercebido. 100% visual e local — cada cliente desenha o seu,
// nenhum impacto no lockstep.
//
// Não precisa de nada na cena: o TurnBannerWatcher se auto-instala no load
// e vigia o TurnManager; o banner monta o próprio Canvas em runtime.
public class TurnBannerFX : MonoBehaviour
{
    const float SlideInTime = 0.35f;
    const float HoldTime = 1.0f;
    const float SlideOutTime = 0.35f;

    public static void Show(string text)
    {
        // Canvas overlay próprio (sempre por cima de tudo)
        GameObject root = new GameObject("TurnBanner");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Faixa escura atrás do texto (legível sobre qualquer mapa)
        GameObject strip = new GameObject("Strip");
        strip.transform.SetParent(root.transform, false);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = new Color(0f, 0f, 0f, 0.55f);
        stripImg.raycastTarget = false; // banner nunca engole cliques do tabuleiro
        RectTransform stripRt = strip.GetComponent<RectTransform>();
        stripRt.anchorMin = new Vector2(0f, 0.5f);
        stripRt.anchorMax = new Vector2(1f, 0.5f);
        stripRt.sizeDelta = new Vector2(0f, 150f);
        stripRt.anchoredPosition = Vector2.zero;

        // Texto central (viaja horizontalmente dentro da faixa)
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(strip.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 96f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.30f); // dourado da UI
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 230);
        tmp.raycastTarget = false;
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(1400f, 150f);

        TurnBannerFX fx = root.AddComponent<TurnBannerFX>();
        fx.StartCoroutine(fx.SlideRoutine(txtRt, stripImg, tmp));

        SoundManager.Play(SoundManager.Sound.Buff); // placeholder — Carlos troca depois
        Destroy(root, SlideInTime + HoldTime + SlideOutTime + 1f); // rede de segurança
    }

    IEnumerator SlideRoutine(RectTransform txt, Image strip, TextMeshProUGUI tmp)
    {
        float halfW = 1920f; // fora da tela (referência do CanvasScaler)

        // A faixa surge em fade junto da entrada
        float t = 0f;
        while (t < SlideInTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / SlideInTime);
            float ease = 1f - (1f - k) * (1f - k); // ease-out
            txt.anchoredPosition = new Vector2(Mathf.Lerp(halfW, 0f, ease), 0f);
            strip.color = new Color(0f, 0f, 0f, 0.55f * k);
            yield return null;
        }
        txt.anchoredPosition = Vector2.zero;

        yield return new WaitForSeconds(HoldTime);

        t = 0f;
        while (t < SlideOutTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / SlideOutTime);
            float ease = k * k; // ease-in
            txt.anchoredPosition = new Vector2(Mathf.Lerp(0f, -halfW, ease), 0f);
            strip.color = new Color(0f, 0f, 0f, 0.55f * (1f - k));
            Color c = tmp.color; c.a = 1f - k; tmp.color = c;
            yield return null;
        }

        Destroy(gameObject);
    }
}

// Vigia a troca de turno e dispara o banner quando vira a vez do jogador
// LOCAL. Auto-instalado — nenhuma referência de cena necessária.
public class TurnBannerWatcher : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        GameObject go = new GameObject("TurnBannerWatcher");
        DontDestroyOnLoad(go);
        go.AddComponent<TurnBannerWatcher>();
    }

    int lastPlayer = -1;
    bool wasPlaying = false;

    void Update()
    {
        TurnManager tm = TurnManager.Instance;
        if (tm == null) { lastPlayer = -1; wasPlaying = false; return; }

        bool playing = tm.gameState == GameState.Playing;
        int cur = tm.currentPlayerNumber;

        // Dispara na TRANSIÇÃO: turno trocou (ou a partida acabou de começar)
        if (playing && (cur != lastPlayer || !wasPlaying))
        {
            if (cur == LocalPlayerNumber(cur))
                TurnBannerFX.Show("SUA VEZ!");
        }

        lastPlayer = cur;
        wasPlaying = playing;
    }

    // Multiplayer: o nº do Photon. Treino contra bot: o humano é o P1.
    // Hotseat local: quem joga agora é sempre "você" (banner a cada troca).
    static int LocalPlayerNumber(int fallback)
    {
        if (PhotonNetwork.inRoom && !PhotonNetwork.offlineMode &&
            PhotonGameManager.Instance != null)
            return PhotonGameManager.Instance.myPlayerNumber;
        if (BotMode.Enabled) return 1;
        return fallback;
    }
}
