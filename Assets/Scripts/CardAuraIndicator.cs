using System.Collections.Generic;
using UnityEngine;

// ORBE DE AURA: um pontinho no TOPO das cartas com efeito condicional/de
// proteção, mostrando se o efeito está valendo AGORA:
//   - Verde (pulsando) = aura ATIVA
//   - Amarelo          = descansando/já usada neste round (volta depois)
//   - Vermelho escuro  = INATIVA (condição não cumprida — ex.: fora da linha
//                        de frente, faltam classes em campo, efeito gasto)
//
// Ao passar o MOUSE na carta, as cartas beneficiadas/protegidas pela aura
// ganham uma moldura verde (some ao tirar o mouse). O tooltip também explica
// o motivo quando a aura está inativa.
//
// 100% visual (lê estado com "peeks", sem efeitos colaterais) — não toca no
// lockstep. Mesmo padrão do CardActionDots.
public class CardAuraIndicator : MonoBehaviour
{
    public enum Kind { None, PortaBandeira, CapitaoFerro, Baluarte, QuebraGolpes, GuardaCostas, FlechaFiel }
    public enum Status { Active, Resting, Inactive }

    // Topo da carta (base local: topo z≈-1.25; as bolinhas de ação ficam na base +1.35)
    static readonly Vector3 OrbPos = new Vector3(0f, 0.11f, -1.35f);
    const float OrbScale = 0.26f;

    static readonly Color ActiveA = new Color(0.30f, 1.00f, 0.42f);
    static readonly Color ActiveB = new Color(0.70f, 1.00f, 0.75f);
    static readonly Color RestingC = new Color(1.00f, 0.85f, 0.30f);
    static readonly Color InactiveC = new Color(0.45f, 0.10f, 0.08f);

    private CardDisplay card;
    private Renderer orb;
    private float pulse;

    public void Init(CardDisplay owner)
    {
        card = owner;
        EnsureFieldManager(); // liga os halos permanentes nas cartas beneficiadas
        if (orb == null)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "AuraOrb";
            dot.transform.SetParent(transform, false);
            dot.transform.localPosition = OrbPos;
            dot.transform.localScale = Vector3.one * OrbScale;
            Collider col = dot.GetComponent<Collider>();
            if (col != null) Destroy(col);

            orb = dot.GetComponent<Renderer>();
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            if (shader != null && orb != null) orb.material = new Material(shader);
        }
    }

    void Update()
    {
        if (card == null || orb == null) return;
        if (!card.isOnBoard || KindOf(card) == Kind.None)
        {
            orb.enabled = false;
            return;
        }

        orb.enabled = true;
        switch (GetStatus(card))
        {
            case Status.Active:
                pulse += Time.deltaTime * 3.5f;
                orb.material.color = Color.Lerp(ActiveA, ActiveB, (Mathf.Sin(pulse) + 1f) * 0.5f);
                break;
            case Status.Resting:
                orb.material.color = RestingC;
                break;
            default:
                orb.material.color = InactiveC;
                break;
        }
    }

    // ── Identificação e avaliação (por stats, como todo o resto do jogo) ──

    public static Kind KindOf(CardDisplay cd)
    {
        if (cd == null || cd.card == null) return Kind.None;
        Card c = cd.card;

        if (c.cardClass == CardClass.Tank)
        {
            if (c.tier == CardTier.Tier4 && c.attack == 2 && c.shield == 6 && c.health == 8)
                return Kind.PortaBandeira;   // aura: arqueiros atacam 2x
            if (c.tier == CardTier.Tier3 && c.attack == 2 && c.shield == 3 && c.health == 6)
                return Kind.CapitaoFerro;    // aura: tanks tomam 50% a menos
            if (c.tier == CardTier.Tier4 && c.attack == 3 && c.shield == 7 && c.health == 8)
                return Kind.Baluarte;        // 50% c/ combo + armadura adjacente
            if (c.tier == CardTier.Tier4 && c.attack == 2 && c.shield == 7 && c.health == 7)
                return Kind.QuebraGolpes;    // intercepta 1x/turno (lado/atrás)
            if (c.tier == CardTier.Tier2 && c.attack == 1 && c.shield == 3 && c.health == 5)
                return Kind.GuardaCostas;    // assume dano (lado/atrás)
        }
        else if (c.cardClass == CardClass.Arqueiro)
        {
            if (c.tier == CardTier.Tier2 && c.attack == 3 && c.health == 3)
                return Kind.FlechaFiel;      // 1x por partida protege healer
        }
        return Kind.None;
    }

    public static Status GetStatus(CardDisplay cd)
    {
        BoardManager board = BoardManager.Instance;
        TurnManager tm = TurnManager.Instance;
        if (board == null || cd == null || !cd.isOnBoard) return Status.Inactive;
        int owner = cd.ownerPlayerNumber;

        switch (KindOf(cd))
        {
            case Kind.PortaBandeira:
            {
                bool classes = board.HasClassOnBoard(owner, CardClass.Tank) &&
                               board.HasClassOnBoard(owner, CardClass.Healer) &&
                               board.HasClassOnBoard(owner, CardClass.Mago) &&
                               board.HasClassOnBoard(owner, CardClass.Arqueiro);
                if (!classes || !CardDisplay.IsOnFrontLines(cd)) return Status.Inactive;
                if (tm != null && cd.archerAuraUsedRound >= 0 &&
                    tm.currentRound == cd.archerAuraUsedRound + 1) return Status.Resting;
                return Status.Active;
            }
            case Kind.CapitaoFerro:
                return CardDisplay.IsOnFrontLines(cd) ? Status.Active : Status.Inactive;

            case Kind.Baluarte:
            {
                bool combo = board.HasClassOnBoard(owner, CardClass.Healer) &&
                             board.HasClassOnBoard(owner, CardClass.Mago) &&
                             board.HasClassOnBoard(owner, CardClass.Arqueiro);
                return combo ? Status.Active : Status.Inactive;
            }
            case Kind.QuebraGolpes:
                return (tm == null || cd.tankTier4Effect2LastUsedRound < tm.currentRound)
                    ? Status.Active : Status.Resting;

            case Kind.GuardaCostas:
                return Status.Active;

            case Kind.FlechaFiel:
                return cd.archerShieldArrowUsed ? Status.Inactive : Status.Active;
        }
        return Status.Inactive;
    }

    // Linha extra do TOOLTIP explicando o estado da aura (null = carta sem aura)
    public static string StatusLine(CardDisplay cd)
    {
        Kind k = KindOf(cd);
        if (k == Kind.None || cd == null || !cd.isOnBoard) return null;

        switch (GetStatus(cd))
        {
            case Status.Active:
                return "<color=#4DFF6B>» Efeito ATIVO agora</color>";
            case Status.Resting:
                return k == Kind.PortaBandeira
                    ? "<color=#FFD84D>» Aura descansando — volta no próximo round</color>"
                    : "<color=#FFD84D>» Já usado neste round — volta no próximo</color>";
            default:
                switch (k)
                {
                    case Kind.PortaBandeira:
                        return CardDisplay.IsOnFrontLines(cd)
                            ? "<color=#FF7B6B>» INATIVO — faltam classes em campo</color>"
                            : "<color=#FF7B6B>» INATIVO — precisa estar na linha de frente</color>";
                    case Kind.CapitaoFerro:
                        return "<color=#FF7B6B>» INATIVO — precisa estar na linha de frente</color>";
                    case Kind.Baluarte:
                        return "<color=#FF7B6B>» INATIVO — precisa de healer, mago e arqueiro</color>";
                    case Kind.FlechaFiel:
                        return "<color=#FF7B6B>» Flecha já gasta nesta partida</color>";
                    default:
                        return "<color=#FF7B6B>» Efeito inativo</color>";
                }
        }
    }

    // ── Destaque das cartas beneficiadas (hover na carta da aura) ─────────

    static readonly List<GameObject> linkHighlights = new List<GameObject>();

    public static void ShowLinksFor(CardDisplay source)
    {
        HideLinks();
        if (source == null || !source.isOnBoard) return;
        if (GetStatus(source) != Status.Active) return; // inativa não protege ninguém

        foreach (CardDisplay target in AffectedBy(source))
        {
            if (target == null || target.card == null) continue;
            linkHighlights.Add(BuildHighlight(target));
        }
    }

    public static void HideLinks()
    {
        foreach (GameObject go in linkHighlights)
            if (go != null) Object.Destroy(go);
        linkHighlights.Clear();
    }

    // Quem a aura alcança NESTE momento (mesmas regras dos efeitos)
    static List<CardDisplay> AffectedBy(CardDisplay src)
    {
        var result = new List<CardDisplay>();
        BoardManager board = BoardManager.Instance;
        if (board == null) return result;
        int owner = src.ownerPlayerNumber;

        switch (KindOf(src))
        {
            case Kind.PortaBandeira:
                foreach (var c in board.GetCardsByOwner(owner))
                    if (c != null && c.card != null && c.card.cardClass == CardClass.Arqueiro)
                        result.Add(c);
                break;

            case Kind.CapitaoFerro:
                foreach (var c in board.GetCardsByOwner(owner))
                    if (c != null && c.card != null && c.card.cardClass == CardClass.Tank)
                        result.Add(c);
                break;

            case Kind.Baluarte:
                foreach (var c in board.GetCardsByOwner(owner))
                    if (c != null && c != src && CardDisplay.IsNextTo(c, src))
                        result.Add(c);
                break;

            case Kind.QuebraGolpes:
            case Kind.GuardaCostas:
                foreach (var c in board.GetCardsByOwner(owner))
                    if (c != null && c != src &&
                        CardDisplay.IsNextTo(src, c) && CardDisplay.IsBesideOrBehind(src, c))
                        result.Add(c);
                break;

            case Kind.FlechaFiel:
                foreach (var c in board.GetCardsByOwner(owner))
                    if (c != null && c.card != null && c.card.cardClass == CardClass.Healer)
                        result.Add(c);
                break;
        }
        return result;
    }

    // ── HALO PERMANENTE nas cartas beneficiadas (sem precisar de hover) ──
    // Uma placa colorida SOB a carta, um pouco maior que ela — dá pra bater o
    // olho e ver quem está protegido/bufado agora:
    //   VERDE = protegida por um guarda (Guarda-Costas / Quebra-Golpes)
    //   AZUL  = recebendo aura de bônus (Porta-Bandeira / Capitão / Baluarte /
    //           Flecha Fiel)
    // Um manager único reavalia o campo 4x por segundo (barato, só leitura).

    static readonly Color ProtectHalo = new Color(0.24f, 0.85f, 0.36f);
    static readonly Color BuffHalo = new Color(0.30f, 0.62f, 1.00f);

    static FieldManager fieldManager;

    static void EnsureFieldManager()
    {
        if (fieldManager != null) return;
        GameObject go = new GameObject("AuraFieldManager");
        fieldManager = go.AddComponent<FieldManager>();
    }

    class FieldManager : MonoBehaviour
    {
        // Carta beneficiada -> halo criado (filho da carta; morre com ela)
        readonly Dictionary<CardDisplay, Renderer> halos = new Dictionary<CardDisplay, Renderer>();
        readonly Dictionary<CardDisplay, bool> wanted = new Dictionary<CardDisplay, bool>(); // true = proteção
        readonly List<CardDisplay> toRemove = new List<CardDisplay>();
        float nextTick;

        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.25f;

            BoardManager board = BoardManager.Instance;
            if (board == null) return;

            // 1) Quem deve estar aceso agora (proteção tem prioridade sobre bônus)
            wanted.Clear();
            foreach (var src in board.GetAllCards())
            {
                if (src == null || !src.isOnBoard) continue;
                Kind k = KindOf(src);
                if (k == Kind.None || GetStatus(src) != Status.Active) continue;
                bool isProtection = k == Kind.QuebraGolpes || k == Kind.GuardaCostas;

                foreach (var target in AffectedBy(src))
                {
                    if (target == null || !target.isOnBoard) continue;
                    bool cur;
                    if (!wanted.TryGetValue(target, out cur) || (!cur && isProtection))
                        wanted[target] = isProtection;
                }
            }

            // 2) Cria/recolore os halos necessários
            foreach (var kv in wanted)
            {
                Renderer halo;
                if (!halos.TryGetValue(kv.Key, out halo) || halo == null)
                {
                    halo = BuildHalo(kv.Key);
                    halos[kv.Key] = halo;
                }
                if (halo != null)
                {
                    halo.enabled = true;
                    halo.material.color = kv.Value ? ProtectHalo : BuffHalo;
                }
            }

            // 3) Apaga os que não valem mais (carta saiu do alcance/aura caiu)
            toRemove.Clear();
            foreach (var kv in halos)
            {
                if (kv.Key == null || kv.Value == null) { toRemove.Add(kv.Key); continue; }
                if (!wanted.ContainsKey(kv.Key)) kv.Value.enabled = false;
            }
            foreach (var dead in toRemove) halos.Remove(dead);
        }

        // Placa fina e OPACA sob a carta, maior que o corpo (1.8 x 2.5) — as
        // bordas aparecem em volta, como uma base iluminada
        static Renderer BuildHalo(CardDisplay target)
        {
            GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "AuraHalo";
            plate.transform.SetParent(target.transform, false);
            plate.transform.localPosition = new Vector3(0f, -0.045f, 0f);
            plate.transform.localScale = new Vector3(2.1f, 0.03f, 2.82f);
            Collider col = plate.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = plate.GetComponent<Renderer>();
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            if (shader != null && r != null) r.material = new Material(shader);
            return r;
        }
    }

    // Moldura verde OPACA (4 barras — quads transparentes dão bug de sorting
    // no build) em volta da carta beneficiada; filha da carta, segue ela
    static GameObject BuildHighlight(CardDisplay target)
    {
        GameObject root = new GameObject("AuraLinkHighlight");
        root.transform.SetParent(target.transform, false);

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");

        // (x, z, escalaX, escalaZ) — contorno do corpo da carta (1.8 x 2.5)
        float[][] bars =
        {
            new[] { 0f, -1.32f, 2.02f, 0.10f },  // topo
            new[] { 0f,  1.32f, 2.02f, 0.10f },  // base
            new[] { -0.98f, 0f, 0.10f, 2.74f },  // esquerda
            new[] { 0.98f, 0f, 0.10f, 2.74f },   // direita
        };
        foreach (float[] b in bars)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Bar";
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(b[0], 0.09f, b[1]);
            bar.transform.localScale = new Vector3(b[2], 0.04f, b[3]);
            Collider col = bar.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            Renderer r = bar.GetComponent<Renderer>();
            if (shader != null && r != null)
            {
                r.material = new Material(shader);
                r.material.color = new Color(0.30f, 1.00f, 0.42f);
            }
        }
        return root;
    }
}
