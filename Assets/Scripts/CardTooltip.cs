using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tooltip de inspeção de carta: aparece ao passar o mouse sobre QUALQUER carta
// (loja, mão ou tabuleiro) mostrando nome, classe/tier, stats, o efeito por
// extenso e o progresso da tríade (ex.: "Tríade: 2/3 em campo"). Puramente
// informativo — não toca em rede nem no estado do jogo.
public class CardTooltip : MonoBehaviour
{
    static CardTooltip instance;

    GameObject panel;
    RectTransform panelRt;
    RectTransform canvasRt;
    Canvas canvas;
    TMP_Text nameText, subText, statsText, effectText, triadText;

    static readonly Color Ink = new Color(0.93f, 0.90f, 0.84f);
    static readonly Color Muted = new Color(0.68f, 0.62f, 0.50f);

    static Color ClassColor(CardClass c)
    {
        switch (c)
        {
            case CardClass.Tank: return new Color(0.55f, 0.72f, 1f);
            case CardClass.Mago: return new Color(0.78f, 0.60f, 1f);
            case CardClass.Arqueiro: return new Color(0.55f, 0.9f, 0.6f);
            case CardClass.Healer: return new Color(0.98f, 0.85f, 0.45f);
            default: return Ink;
        }
    }

    static string ClassName(CardClass c)
    {
        switch (c)
        {
            case CardClass.Tank: return "Tanque";
            case CardClass.Mago: return "Mago";
            case CardClass.Arqueiro: return "Arqueiro";
            case CardClass.Healer: return "Healer";
            default: return "";
        }
    }

    static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("CardTooltip");
        instance = go.AddComponent<CardTooltip>();
    }

    // ── API ──
    public static void ShowFor(CardDisplay cd)
    {
        if (cd == null || cd.card == null) return;
        Ensure();
        instance.Display(cd);
    }

    public static void HideTip()
    {
        if (instance != null && instance.panel != null) instance.panel.SetActive(false);
    }

    void Display(CardDisplay cd)
    {
        if (panel == null || canvas == null) Build();
        if (panel == null) return;

        Card c = cd.card;
        nameText.text = c.cardName;
        nameText.color = ClassColor(c.cardClass);
        subText.text = $"Tier {(int)c.tier}  ·  {ClassName(c.cardClass)}";

        // Stats atuais se está no tabuleiro (com buffs); senão os base da carta
        int atk = cd.isOnBoard ? cd.currentAttack : c.attack;
        int shd = cd.isOnBoard ? cd.currentShield : c.shield;
        int hp = cd.isOnBoard ? cd.currentHealth : c.health;
        statsText.text = $"<color=#FF8C6B>ATQ {atk}</color>   <color=#7FC7FF>ARM {shd}</color>   <color=#7BE08B>VIDA {hp}</color>";

        effectText.text = string.IsNullOrEmpty(c.effectDescription)
            ? "<i>Sem efeito especial.</i>" : c.effectDescription;

        // Progresso de tríade (só para cartas de tríade e quando há dono)
        int owner = cd.ownerPlayerNumber;
        int owned = (owner != 0) ? Triads.OwnedDistinct(owner, c) : (Triads.IsTriadCard(c) ? 0 : -1);
        if (owned >= 0)
        {
            triadText.gameObject.SetActive(true);
            triadText.text = owned >= 3
                ? "<color=#F5C451>» Tríade completa!</color>"
                : $"<color=#F5C451>» Tríade: {owned}/3 em campo</color>";
        }
        else triadText.gameObject.SetActive(false);

        // Reposiciona conforme o texto (o efeito tem tamanho variável)
        panel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);
        PositionNear(cd.transform.position);
        panel.transform.SetAsLastSibling();
    }

    void PositionNear(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
        if (cam == null || canvasRt == null) return;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out local))
            return;

        // Desloca para a direita da carta; se estourar, joga para a esquerda
        Vector2 size = panelRt.sizeDelta;
        Vector2 half = canvasRt.sizeDelta * 0.5f;
        float offX = 90f;
        Vector2 pos = local + new Vector2(offX + size.x * 0.5f, 0f);
        if (pos.x + size.x * 0.5f > half.x) pos = local - new Vector2(offX + size.x * 0.5f, 0f);

        // Clampa verticalmente para não sair da tela
        pos.y = Mathf.Clamp(pos.y, -half.y + size.y * 0.5f + 8f, half.y - size.y * 0.5f - 8f);
        pos.x = Mathf.Clamp(pos.x, -half.x + size.x * 0.5f + 8f, half.x - size.x * 0.5f - 8f);
        panelRt.anchoredPosition = pos;
    }

    void Build()
    {
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        canvasRt = canvas.GetComponent<RectTransform>();

        panel = new GameObject("CardTooltipPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(canvas.transform, false);
        panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(340f, 200f);

        Image bg = panel.GetComponent<Image>();
        if (LobbySprites.Fill != null) { bg.sprite = LobbySprites.Fill; bg.type = Image.Type.Sliced; }
        bg.color = new Color(0.06f, 0.045f, 0.03f, 0.98f);
        bg.raycastTarget = false;
        LobbySprites.AddRing(panel.transform, new Color(0.96f, 0.77f, 0.32f, 0.5f));

        VerticalLayoutGroup vlg = panel.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 12, 14);
        vlg.spacing = 5f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fit = panel.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        nameText = MakeRow("Name", 21, FontStyles.Bold, Ink);
        subText = MakeRow("Sub", 14, FontStyles.Normal, Muted);
        statsText = MakeRow("Stats", 17, FontStyles.Bold, Ink);
        effectText = MakeRow("Effect", 15, FontStyles.Normal, Ink);
        triadText = MakeRow("Triad", 15, FontStyles.Bold, new Color(0.96f, 0.77f, 0.32f));
    }

    TMP_Text MakeRow(string name, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
