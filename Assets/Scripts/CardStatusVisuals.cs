using UnityEngine;
using TMPro;
using System.Collections;

// Efeitos visuais de status das cartas, criados por código (sem sprites):
// - Congelada: painel azul translúcido + faixa "CONGELADA"
// - Atordoada: painel amarelo translúcido + faixa "ATORDOADA"
// - Marcada (águia do Archer 3): painel roxo + faixa "MARCADA"
// - Buff de status: brilho verde que pisca e some
// - Dano/debuff: brilho vermelho que pisca e some
public class CardStatusVisuals : MonoBehaviour
{
    private GameObject frozenOverlay;
    private GameObject stunnedOverlay;
    private GameObject eagleOverlay;

    public void SetFrozen(bool active)
    {
        SetOverlay(ref frozenOverlay, active, 0.040f, -0.35f,
            new Color(0.35f, 0.75f, 1.00f, 0.35f), "CONGELADA", new Color(0.80f, 0.95f, 1.00f));
    }

    public void SetStunned(bool active)
    {
        SetOverlay(ref stunnedOverlay, active, 0.044f, 0.00f,
            new Color(1.00f, 0.85f, 0.20f, 0.35f), "ATORDOADA", new Color(1.00f, 0.95f, 0.60f));
    }

    public void SetEagleMark(bool active)
    {
        SetOverlay(ref eagleOverlay, active, 0.048f, 0.35f,
            new Color(0.75f, 0.40f, 1.00f, 0.30f), "MARCADA", new Color(0.93f, 0.80f, 1.00f));
    }

    public void FlashBuff()
    {
        StartCoroutine(FlashRoutine(new Color(0.30f, 1.00f, 0.45f, 0.55f)));
    }

    public void FlashDamage()
    {
        StartCoroutine(FlashRoutine(new Color(1.00f, 0.30f, 0.25f, 0.55f)));
    }

    // ── Internos ─────────────────────────────────────────────────────────

    void SetOverlay(ref GameObject overlay, bool active, float yLayer, float bannerZ,
                    Color panelColor, string label, Color labelColor)
    {
        if (active && overlay == null)
        {
            overlay = CreateOverlay(yLayer, bannerZ, panelColor, label, labelColor);
        }
        else if (!active && overlay != null)
        {
            Destroy(overlay);
            overlay = null;
        }
    }

    GameObject CreateOverlay(float yLayer, float bannerZ, Color panelColor, string label, Color labelColor)
    {
        GameObject root = new GameObject("StatusOverlay_" + label);
        root.transform.SetParent(transform, false);

        CreatePanel(root.transform, new Vector3(0f, yLayer, 0f), 1.98f, 2.66f, panelColor);

        GameObject txtObj = new GameObject("Banner");
        txtObj.transform.SetParent(root.transform, false);
        txtObj.transform.localPosition = new Vector3(0f, yLayer + 0.010f, bannerZ);
        txtObj.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);

        TextMeshPro tmp = txtObj.AddComponent<TextMeshPro>();
        tmp.text = label;
        tmp.fontSize = 2.4f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = labelColor;
        tmp.richText = false;
        tmp.rectTransform.sizeDelta = new Vector2(1.9f, 0.6f);

        return root;
    }

    GameObject CreatePanel(Transform parent, Vector3 localPos, float width, float height, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Panel";
        quad.transform.SetParent(parent, false);
        quad.transform.localPosition = localPos;
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(width, height, 1f);

        // Remove o collider para não roubar os cliques da carta
        Collider col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = quad.GetComponent<Renderer>();
        // Sprites/Default suporta transparência e está em Always Included Shaders
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null && r != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            r.material = mat;
        }
        return quad;
    }

    IEnumerator FlashRoutine(Color color)
    {
        GameObject panel = CreatePanel(transform, new Vector3(0f, 0.055f, 0f), 2.10f, 2.80f, color);
        Renderer r = panel != null ? panel.GetComponent<Renderer>() : null;

        const float duration = 0.7f;
        float t = 0f;
        while (t < duration && r != null)
        {
            t += Time.deltaTime;
            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, t / duration);
            r.material.color = c;
            yield return null;
        }

        if (panel != null) Destroy(panel);
    }
}
