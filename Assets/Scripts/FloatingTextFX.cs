using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Texto flutuante 3D sobre as cartas: números de dano/cura, mudanças de stat
// e anúncios de efeito. Sobe, encara a câmera e some. 100% visual e local —
// nenhum impacto na sincronização (os dois clientes geram os seus).
public class FloatingTextFX : MonoBehaviour
{
    private float life = 1.4f;
    private float age = 0f;
    private float riseSpeed = 2.2f;
    private TextMeshPro tmp;

    // Empilha popups criados no mesmo instante sobre a mesma carta para não
    // se sobreporem (frame -> quantos já nasceram)
    private static int lastSpawnFrame = -1;
    private static int spawnsThisFrame = 0;

    // Cores padrão dos popups
    public static readonly Color DamageColor = new Color(1f, 0.30f, 0.25f);
    public static readonly Color HealColor = new Color(0.35f, 1f, 0.45f);
    public static readonly Color AttackColor = new Color(1f, 0.65f, 0.25f);
    public static readonly Color ShieldColor = new Color(0.40f, 0.70f, 1f);
    public static readonly Color GoldColor = new Color(1f, 0.85f, 0.30f);
    public static readonly Color EffectColor = new Color(0.85f, 0.70f, 1f);

    // Mostra um texto flutuante sobre uma carta
    public static void ShowAboveCard(CardDisplay card, string text, Color color, float size = 5f)
    {
        if (card == null) return;
        Show(card.transform.position, text, color, size);
    }

    // Multiplicador GLOBAL do tamanho dos popups (dano/cura/efeitos/debuffs).
    // Pedido do Carlos (jul/2026): estavam pequenos e difíceis de perceber.
    // Mexer só aqui escala TODOS mantendo as proporções entre eles.
    // 22/jul: 1.5 ainda estava pequeno → 2.4 ("bem visível")
    const float SizeBoost = 2.4f;

    public static void Show(Vector3 worldPos, string text, Color color, float size = 5f)
    {
        size *= SizeBoost;
        // Empilhamento: popups do mesmo frame nascem um acima do outro
        if (Time.frameCount != lastSpawnFrame)
        {
            lastSpawnFrame = Time.frameCount;
            spawnsThisFrame = 0;
        }
        float stackOffset = spawnsThisFrame * 1.8f; // espaçamento acompanha o texto maior
        spawnsThisFrame++;

        GameObject go = new GameObject("FloatingText");
        go.transform.position = worldPos + Vector3.up * (3.2f + stackOffset);

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.outlineWidth = 0.28f; // contorno um pouco mais grosso: legível sobre qualquer fundo
        tmp.outlineColor = new Color32(0, 0, 0, 220);
        tmp.rectTransform.sizeDelta = new Vector2(20f, 5f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        // Renderiza por cima das cartas. fontMaterial (e NÃO sharedMaterial!)
        // instancia um material só deste texto — mexer no compartilhado
        // mudaria a fonte de TODOS os textos do jogo
        if (tmp.fontMaterial != null) tmp.fontMaterial.renderQueue = 4000;

        FloatingTextFX fx = go.AddComponent<FloatingTextFX>();
        fx.tmp = tmp;

        Destroy(go, 2.5f); // rede de segurança
    }

    void LateUpdate()
    {
        age += Time.deltaTime;

        // Sobe desacelerando
        float k = Mathf.Clamp01(age / life);
        transform.position += Vector3.up * riseSpeed * (1f - k * 0.6f) * Time.deltaTime;

        // Encara a câmera
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        // Some no fim
        if (tmp != null && k > 0.55f)
        {
            Color c = tmp.color;
            c.a = 1f - (k - 0.55f) / 0.45f;
            tmp.color = c;
        }

        if (age >= life) Destroy(gameObject);
    }
}

// (Os selos de status fixos — CONGELADA, ATORDOADA, MARCADA, INVULNERÁVEL,
// NA ÁRVORE — são as faixas do CardStatusVisuals, desenhadas na própria carta)
