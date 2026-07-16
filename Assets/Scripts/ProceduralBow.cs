using UnityEngine;

// Desenha um arco simples (limbo curvo de madeira + corda) e o gruda na MÃO do
// personagem riggado (osso do esqueleto do Mixamo), pra acompanhar as animações.
// 100% por código, sem asset — igual aos cenários. Sem colliders (não rouba
// cliques). Puramente visual, nada de lockstep.
public static class ProceduralBow
{
    // ── Ajustes finos (mexer aqui se ficar torto/errado no jogo) ─────────
    const string HandBoneEndsWith = "LeftHand";   // mão que segura o arco (Mixamo mira com a esquerda)
    const float TargetWorldHeight = 3.4f;          // altura do arco em unidades de mundo
    static readonly Vector3 LocalOffset = Vector3.zero;              // deslocamento na palma
    static readonly Vector3 LocalEuler = new Vector3(0f, 0f, 0f);    // rotação relativa à mão

    // Forma do arco (em unidades locais; a escala final é medida e ajustada)
    const float H = 2.0f;      // ponta a ponta
    const float Bulge = 0.38f; // o quanto o limbo curva para longe da corda
    const int Segments = 12;

    static readonly Color WoodColor = new Color(0.36f, 0.23f, 0.11f);
    static readonly Color StringColor = new Color(0.85f, 0.84f, 0.76f);

    public static void Attach(GameObject figureRoot)
    {
        if (figureRoot == null) return;

        Transform hand = FindBone(figureRoot.transform, HandBoneEndsWith);
        if (hand == null)
        {
            Debug.Log("[Bow] Osso da mão não encontrado — arco não adicionado (rig sem mixamorig?).");
            return;
        }

        // O arco NÃO é filho do osso: fica na raiz da figura e um follower copia
        // posição/rotação da mão a cada frame — imune à ESCALA animada dos ossos
        // (clips do Mixamo, como o de morte, escalam ossos e deixavam a arma gigante)
        GameObject bow = new GameObject("Bow");
        Transform bt = bow.transform;
        bt.SetParent(figureRoot.transform, false);
        WeaponBoneFollower follow = bow.AddComponent<WeaponBoneFollower>();
        follow.bone = hand;
        follow.positionOffset = LocalOffset;
        follow.eulerOffset = LocalEuler;

        Material limbMat = MakeMat(WoodColor);
        Material stringMat = MakeMat(StringColor);

        // Limbo: arco que "estufa" para +X (lado do alvo), pontas em ±Y
        Vector3[] pts = new Vector3[Segments + 1];
        for (int i = 0; i <= Segments; i++)
        {
            float t = (float)i / Segments;
            float y = -H * 0.5f + t * H;
            float x = Bulge * Mathf.Sin(Mathf.PI * t); // 0 nas pontas, máximo no meio
            pts[i] = new Vector3(x, y, 0f);
        }
        for (int i = 0; i < Segments; i++)
            MakeRod(bt, pts[i], pts[i + 1], 0.035f, limbMat);

        // Corda: reta de ponta a ponta
        MakeRod(bt, pts[0], pts[Segments], 0.012f, stringMat);

        // Empunhadura: um trecho mais grosso no meio
        MakeRod(bt, new Vector3(0f, -0.2f, 0f), new Vector3(0f, 0.2f, 0f), 0.055f, limbMat);

        // Ajusta a ESCALA para o arco ter ~TargetWorldHeight no mundo,
        // independente da escala do osso da mão (que herda o rig + FitFigureOnCard)
        FitWorldHeight(bt, TargetWorldHeight);
    }

    // ── Internos ─────────────────────────────────────────────────────────

    static Transform FindBone(Transform root, string endsWith)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(endsWith)) return t;
        return null;
    }

    static void MakeRod(Transform parent, Vector3 a, Vector3 b, float radius, Material mat)
    {
        GameObject rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rod.name = "BowRod";
        Collider col = rod.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        rod.transform.SetParent(parent, false);

        Vector3 dir = b - a;
        float len = dir.magnitude;
        rod.transform.localPosition = (a + b) * 0.5f;
        if (len > 1e-4f)
            rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        rod.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f); // cilindro tem 2 de altura

        Renderer r = rod.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void FitWorldHeight(Transform bow, float targetWorld)
    {
        Renderer[] rends = bow.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y > 1e-4f)
            bow.localScale *= (targetWorld / b.size.y);
    }

    static Material MakeMat(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");
        Material m = new Material(s);
        m.color = c;
        m.SetColor("_BaseColor", c);
        return m;
    }
}
