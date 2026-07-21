using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Painel "DEVOÇÃO DE CLASSE" da partida: mostra, para o jogador LOCAL, quantas
// unidades de cada classe ele tem em campo e qual bônus está ativo — linha
// apagada enquanto não ativou (com o progresso x/3), colorida no degrau 1 e
// DOURADA no degrau 2. Quando um degrau liga/desliga, a linha pisca (escala)
// para o jogador bater o olho. Também resume as devoções ATIVAS do inimigo.
// Atualiza sozinho a cada 0.3s lendo o tabuleiro (ClassDevotion).
public class ClassDevotionUI : MonoBehaviour
{
    static ClassDevotionUI instance;

    static readonly CardClass[] Order =
        { CardClass.Tank, CardClass.Mago, CardClass.Arqueiro, CardClass.Healer };

    static readonly string[] Labels = { "Tanks", "Magos", "Arqueiros", "Healers" };

    static readonly Color[] ClassColors =
    {
        new Color(0.45f, 0.62f, 0.95f),   // Tank (azul)
        new Color(0.72f, 0.48f, 0.95f),   // Mago (roxo)
        new Color(0.95f, 0.62f, 0.30f),   // Arqueiro (laranja)
        new Color(0.42f, 0.88f, 0.55f),   // Healer (verde)
    };

    // Descrições curtas dos DOIS degraus — sempre visíveis na linha de baixo
    // de cada classe (o jogador vê o que já ativou E o que falta ativar)
    static readonly string[] Tier1Desc =
    {
        "−1 dano nos tanks",
        "efeitos de dano +1",
        "+1 ATK vs feridos",
        "+1 ouro por round",
    };
    static readonly string[] Tier2Desc =
    {
        "reflete 1 (perto)",
        "raio por round",
        "quebra 1 escudo",
        "+2 ouro por round",
    };

    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color TextMuted = new Color(0.60f, 0.54f, 0.44f);

    TMP_Text[] rows = new TMP_Text[4];
    TMP_Text[] effectRows = new TMP_Text[4];   // linha de baixo: os 2 degraus
    Image[] chips = new Image[4];
    TMP_Text enemyLine;
    int[] lastTier = { -1, -1, -1, -1 };
    float[] flashUntil = new float[4];
    float refreshTimer;

    public static void Ensure()
    {
        if (instance != null) return;

        GameObject go = new GameObject("ClassDevotionUI");
        instance = go.AddComponent<ClassDevotionUI>();
        instance.Build();
    }

    void Build()
    {
        // Canvas próprio (abaixo dos overlays de torre, 700/800)
        GameObject canvasGo = new GameObject("DevotionCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        Canvas cv = canvasGo.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 550;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Painel no canto esquerdo, um pouco acima do meio (a loja abre embaixo)
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(0f, 0.5f);
        prt.pivot = new Vector2(0f, 0.5f);
        prt.anchoredPosition = new Vector2(12f, 148f);
        prt.sizeDelta = new Vector2(276f, 248f);
        Image pImg = panel.GetComponent<Image>();
        LobbySprites.MakeRounded(pImg, new Color(0.055f, 0.042f, 0.030f, 0.88f));
        pImg.raycastTarget = false; // informativo: não come cliques do jogo
        LobbySprites.AddRing(panel.transform, new Color(0.96f, 0.77f, 0.32f, 0.45f));

        TMP_Text title = MakeText(panel.transform, "Title", "«  DEVOÇÃO DE CLASSE  »", 13,
            TextMuted, new Vector2(0f, 104f), new Vector2(248f, 20f), TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;

        // Cada classe = 2 linhas: cabeçalho (nome + contagem) e, EMBAIXO, os
        // dois degraus sempre visíveis — cada um acende quando ativa
        for (int i = 0; i < 4; i++)
        {
            float y = 76f - i * 44f;

            GameObject chip = new GameObject("Chip" + i, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(panel.transform, false);
            RectTransform crt = chip.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(13f, 13f);
            crt.anchoredPosition = new Vector2(-116f, y);
            chips[i] = chip.GetComponent<Image>();
            LobbySprites.MakeRounded(chips[i], ClassColors[i]);
            chips[i].raycastTarget = false;

            rows[i] = MakeText(panel.transform, "Row" + i, "", 13, TextMuted,
                new Vector2(14f, y), new Vector2(232f, 24f), TextAlignmentOptions.Left);

            effectRows[i] = MakeText(panel.transform, "Fx" + i, "", 11, TextMuted,
                new Vector2(14f, y - 18f), new Vector2(232f, 20f), TextAlignmentOptions.Left);
        }

        enemyLine = MakeText(panel.transform, "Enemy", "", 11, TextMuted,
            new Vector2(0f, -104f), new Vector2(248f, 20f), TextAlignmentOptions.Center);

        Refresh(true);
    }

    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.3f;
            Refresh(false);
        }

        // Pisca a linha cujo degrau acabou de mudar (chama o olho do jogador)
        for (int i = 0; i < 4; i++)
        {
            if (rows[i] == null) continue;
            float t = flashUntil[i] - Time.time;
            rows[i].transform.localScale = t > 0f
                ? Vector3.one * (1f + 0.10f * Mathf.Abs(Mathf.Sin(t * 10f)))
                : Vector3.one;
        }
    }

    int LocalPlayer()
    {
        if (PhotonGameManager.Instance != null && PhotonGameManager.Instance.myPlayerNumber > 0)
            return PhotonGameManager.Instance.myPlayerNumber;
        return 1; // treino/offline: o humano é sempre o P1
    }

    void Refresh(bool first)
    {
        int me = LocalPlayer();
        int enemy = me == 1 ? 2 : 1;

        for (int i = 0; i < 4; i++)
        {
            if (rows[i] == null) continue;

            int count = ClassDevotion.CountOnBoard(me, Order[i]);
            int tier = count >= ClassDevotion.Tier2Count ? 2
                     : count >= ClassDevotion.Tier1Count ? 1 : 0;
            string name = ClassDevotion.DevotionName(Order[i]);

            // Cabeçalho: nome + contagem real (cópias não contam) + progresso
            if (tier == 0)
            {
                rows[i].text = $"{Labels[i]} {count}/{ClassDevotion.Tier1Count} — {name}";
                rows[i].color = TextMuted;
                rows[i].fontStyle = FontStyles.Normal;
                chips[i].color = new Color(ClassColors[i].r, ClassColors[i].g, ClassColors[i].b, 0.35f);
            }
            else if (tier == 1)
            {
                rows[i].text = $"{Labels[i]} {count}/{ClassDevotion.Tier2Count} — {name} ATIVA";
                rows[i].color = ClassColors[i];
                rows[i].fontStyle = FontStyles.Bold;
                chips[i].color = ClassColors[i];
            }
            else
            {
                rows[i].text = $"{Labels[i]} {count} — {name} MÁXIMA";
                rows[i].color = Gold;
                rows[i].fontStyle = FontStyles.Bold;
                chips[i].color = Gold;
            }

            // Linha de baixo: os DOIS degraus sempre visíveis, cada um aceso
            // conforme ativa (3+ na cor da classe, 5 em dourado)
            if (effectRows[i] != null)
            {
                string mutedHex = ColorUtility.ToHtmlStringRGB(TextMuted);
                string t1Hex = tier >= 1 ? ColorUtility.ToHtmlStringRGB(ClassColors[i]) : mutedHex;
                string t2Hex = tier >= 2 ? ColorUtility.ToHtmlStringRGB(Gold) : mutedHex;
                string t1Style = tier >= 1 ? "<b>" : "";
                string t1StyleEnd = tier >= 1 ? "</b>" : "";
                string t2Style = tier >= 2 ? "<b>" : "";
                string t2StyleEnd = tier >= 2 ? "</b>" : "";

                effectRows[i].text =
                    $"<color=#{t1Hex}>{t1Style}3+: {Tier1Desc[i]}{t1StyleEnd}</color>" +
                    $"<color=#{mutedHex}>  ·  </color>" +
                    $"<color=#{t2Hex}>{t2Style}5: {Tier2Desc[i]}{t2StyleEnd}</color>";
            }

            // Degrau mudou: pisca a linha por 1.6s (sem piscar na criação)
            if (!first && lastTier[i] >= 0 && lastTier[i] != tier)
                flashUntil[i] = Time.time + 1.6f;
            lastTier[i] = tier;
        }

        // Devoções ativas do INIMIGO (informação tática, discreta)
        if (enemyLine != null)
        {
            string list = "";
            for (int i = 0; i < 4; i++)
            {
                if (ClassDevotion.TierOf(enemy, Order[i]) < 1) continue;
                if (list.Length > 0) list += " · ";
                list += ClassDevotion.DevotionName(Order[i]);
            }
            enemyLine.text = list.Length > 0 ? "Inimigo: " + list : "";
        }
    }

    static TMP_Text MakeText(Transform parent, string name, string text, int fontSize,
        Color color, Vector2 pos, Vector2 size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return tmp;
    }
}
