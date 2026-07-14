using UnityEngine;

// Duas "bolinhas" na base da carta no tabuleiro, indicando o que ela ainda
// pode fazer NESTE turno:
//   - Verde   = pode se MOVER (andar)
//   - Vermelha = pode ATACAR
// Acesa (cor viva) quando a ação está disponível; apagada (cor escura) quando
// já foi usada neste round ou a carta está impedida (congelada/atordoada/águia).
// Ex.: ao atacar, a bolinha vermelha apaga; no próximo round ela reacende.
//
// Atualiza a cada frame lendo os "peeks" silenciosos do CardDisplay (sem logs
// nem efeitos colaterais). É só visual — não toca em estado de jogo, então não
// interfere no lockstep do Photon.
public class CardActionDots : MonoBehaviour
{
    private CardDisplay card;
    private Renderer moveDot;
    private Renderer attackDot;

    // Base local da carta: o topo fica em z≈-1.25, a base em z≈+1.25. As bolinhas
    // ficam na BASE (z além da borda, ~1.35) com o número do cooldown no centro,
    // entre elas. y um pouco acima da face para a esfera não afundar.
    static readonly Vector3 MoveDotPos   = new Vector3(-0.5f, 0.11f, 1.35f);
    static readonly Vector3 AttackDotPos = new Vector3(0.5f, 0.11f, 1.35f);
    const float DotScale = 0.26f;

    static readonly Color MoveOn    = new Color(0.30f, 1.00f, 0.42f);
    static readonly Color MoveOff   = new Color(0.09f, 0.20f, 0.11f);
    static readonly Color AttackOn  = new Color(1.00f, 0.32f, 0.28f);
    static readonly Color AttackOff = new Color(0.22f, 0.08f, 0.07f);

    public void Init(CardDisplay owner)
    {
        card = owner;
        if (moveDot == null) moveDot = CreateDot("MoveDot", MoveDotPos);
        if (attackDot == null) attackDot = CreateDot("AttackDot", AttackDotPos);
        Refresh();
    }

    Renderer CreateDot(string name, Vector3 localPos)
    {
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = name;
        dot.transform.SetParent(transform, false);
        dot.transform.localPosition = localPos;
        dot.transform.localScale = Vector3.one * DotScale;

        // Sem collider — não pode roubar os cliques da carta
        Collider col = dot.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer r = dot.GetComponent<Renderer>();
        // Mesma ordem de shaders do CardStatusVisuals (Sprites/Default está sempre
        // incluído no build e respeita material.color) — cor chapada, sem luz
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        if (shader != null && r != null) r.material = new Material(shader);
        return r;
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (card == null) return;
        if (moveDot != null)
            moveDot.material.color = card.CanMovePeek() ? MoveOn : MoveOff;
        if (attackDot != null)
            attackDot.material.color = card.CanAttackPeek() ? AttackOn : AttackOff;
    }
}
