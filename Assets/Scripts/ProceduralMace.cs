using UnityEngine;

// Desenha uma maça (cabo + cabeça esférica com espinhos) e a gruda na MÃO do
// personagem riggado (osso do esqueleto do Mixamo), pra acompanhar as animações.
// 100% por código, sem asset. Sem colliders. Puramente visual, nada de lockstep.
public static class ProceduralMace
{
    // ── Ajustes finos (mexer se ficar torto no jogo) ─────────────────────
    const string HandBoneEndsWith = "RightHand";   // mão que segura a maça
    const float TargetWorldHeight = 2.3f;           // altura da maça em unidades de mundo
    static readonly Vector3 LocalOffset = Vector3.zero;
    static readonly Vector3 LocalEuler = new Vector3(0f, 0f, 0f);

    // Forma (unidades locais; a escala final é medida e ajustada)
    const float HandleBottom = -0.15f;
    const float HandleTop = 0.72f;
    const float HeadY = 0.9f;
    const float HeadRadius = 0.2f;
    const int Spikes = 8;

    static readonly Color HandleColor = new Color(0.24f, 0.19f, 0.14f); // cabo escuro
    static readonly Color HeadColor = new Color(0.86f, 0.68f, 0.28f);   // cabeça dourada
    static readonly Color SpikeColor = new Color(0.95f, 0.90f, 0.75f);  // espinhos claros

    public static void Attach(GameObject figureRoot)
    {
        if (figureRoot == null) return;

        Transform hand = FindBone(figureRoot.transform, HandBoneEndsWith);
        if (hand == null)
        {
            Debug.Log("[Mace] Osso da mão não encontrado — maça não adicionada.");
            return;
        }

        // A maça NÃO é filha do osso: fica na raiz da figura e um follower copia
        // posição/rotação da mão a cada frame — imune à ESCALA animada dos ossos
        // (clips do Mixamo, como o de morte, escalam ossos e deixavam a arma gigante)
        GameObject mace = new GameObject("Mace");
        Transform mt = mace.transform;
        mt.SetParent(figureRoot.transform, false);
        WeaponBoneFollower follow = mace.AddComponent<WeaponBoneFollower>();
        follow.bone = hand;
        follow.positionOffset = LocalOffset;
        follow.eulerOffset = LocalEuler;

        Material handleMat = MakeMat(HandleColor);
        Material headMat = MakeMat(HeadColor);
        Material spikeMat = MakeMat(SpikeColor);

        // Cabo
        MakeRod(mt, new Vector3(0f, HandleBottom, 0f), new Vector3(0f, HandleTop, 0f), 0.045f, handleMat);

        // Cabeça (esfera)
        Vector3 head = new Vector3(0f, HeadY, 0f);
        MakeSphere(mt, head, HeadRadius * 2f, headMat);

        // Espinhos radiais na cabeça
        for (int i = 0; i < Spikes; i++)
        {
            float a = (i / (float)Spikes) * Mathf.PI * 2f;
            // metade num anel na horizontal, o resto inclinado (cima/baixo)
            float tilt = (i % 2 == 0) ? 0.35f : -0.35f;
            Vector3 dir = new Vector3(Mathf.Cos(a), tilt, Mathf.Sin(a)).normalized;
            MakeRod(mt, head + dir * (HeadRadius * 0.8f), head + dir * (HeadRadius + 0.12f), 0.03f, spikeMat);
        }
        // um espinho no topo
        MakeRod(mt, head + Vector3.up * (HeadRadius * 0.8f), head + Vector3.up * (HeadRadius + 0.14f), 0.03f, spikeMat);

        FitWorldHeight(mt, TargetWorldHeight);
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
        rod.name = "MacePart";
        Collider col = rod.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        rod.transform.SetParent(parent, false);
        Vector3 dir = b - a;
        float len = dir.magnitude;
        rod.transform.localPosition = (a + b) * 0.5f;
        if (len > 1e-4f) rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        rod.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
        Renderer r = rod.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void MakeSphere(Transform parent, Vector3 pos, float diameter, Material mat)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "MaceHead";
        Collider col = s.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos;
        s.transform.localScale = Vector3.one * diameter;
        Renderer r = s.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static void FitWorldHeight(Transform mace, float targetWorld)
    {
        Renderer[] rends = mace.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y > 1e-4f) mace.localScale *= (targetWorld / b.size.y);
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
