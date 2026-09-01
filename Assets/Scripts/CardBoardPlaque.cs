using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ╔══════════════════════════════════════════════════════════════════════╗
// ║  A CARTA VIRA UNIDADE                                                 ║
// ║                                                                       ║
// ║  Ao entrar no tabuleiro o "papel" da carta se desfaz (fade + onda de  ║
// ║  luz dourada) e sobra só o personagem, com:                           ║
// ║    · uma PLACA DE STATUS embaixo dele (os mesmos escudos heráldicos   ║
// ║      de ATQ/ARM/VIDA que a carta já tinha, agora sobre uma placa)     ║
// ║    · os NOMES dos status ativos logo abaixo dos pés (CONGELADA 2,     ║
// ║      PRESA 1, LÂMINA +2...)                                           ║
// ║                                                                       ║
// ║  O resto da carta (nome, arte, efeito) continua a um passar de mouse  ║
// ║  sobre o personagem (tooltip) ou a um clique direito nele (inspeção   ║
// ║  ampliada no canto da tela).                                          ║
// ║                                                                       ║
// ║  100% VISUAL: nada aqui muda estado de partida, não sorteia nada e    ║
// ║  não manda RPC — o lockstep do Photon não enxerga esta classe.        ║
// ╚══════════════════════════════════════════════════════════════════════╝
public class CardBoardPlaque : MonoBehaviour
{
    // O que NUNCA some: os escudos de stats (são justamente o que fica), o
    // personagem, o que esta classe cria e o que os outros sistemas penduram
    // na carta. Todo o RESTO dos filhos diretos é "papel" e se dissolve.
    //
    // Por exclusão de propósito: uma lista de nomes a apagar deixava de fora
    // qualquer coisa que aparecesse depois (foi assim que o halo de aura ficou
    // como um retângulo solto na casa) — aqui, o que eu não conheço, some.
    static readonly HashSet<string> KeepParts = new HashSet<string> {
        "AtkChip", "AtkRing", "AttackText",
        "ShieldChip", "ShieldRing", "ShieldText",
        "HpChip", "HpRing", "HealthText",
        "BoardFigure", "Plaque", "EntranceWave",
        "MoveDot", "AttackDot", "EffectCounter",          // CardActionDots / CardStatusVisuals
        "AuraOrb", "AuraHalo", "AuraHaloDisc", "AuraLinkHighlight", // CardAuraIndicator
        "FloatingText", "ShopLockLabel",
    };

    const float FadeTime = 0.55f;   // dissolução do papel
    const float RiseTime = 0.30f;   // placa de stats entrando

    // ── Paleta ───────────────────────────────────────────────────────────
    static readonly Color Gold = new Color(0.96f, 0.77f, 0.32f);
    static readonly Color PlateInk = new Color(0.045f, 0.040f, 0.032f, 0.88f);
    static readonly Color StatusInk = new Color(0.03f, 0.028f, 0.024f, 0.80f);

    CardDisplay card;
    Transform plaque;              // raiz de tudo o que criamos (fora do alcance
                                   // do "esconde TMP desconhecido" do ApplyCardTheme,
                                   // que só varre os filhos DIRETOS da carta)
    Renderer statPlate, statPlateRing, statusPlate;
    Renderer footBase, footRing;   // base de miniatura no chão na cor do dono —
                                   // sem a carta, a borda azul/carmesim sumia
                                   // junto e não dava mais para bater o olho e
                                   // ver de quem é a unidade

    // Lado da base, em unidades locais da carta (x2 de escala no tabuleiro =
    // 3.9 no mundo). A casa tem 6 de lado e os centros ficam a 6.6 de
    // distância: sobra folga de sobra para as bases não se encostarem.
    const float FootSize = 1.95f;
    Color ownerColor = Gold;
    TextMeshPro statusText;
    BoxCollider bodyCollider;      // corpo do personagem: é o que deixa passar o
                                   // mouse EM CIMA DELE (o collider da carta é
                                   // uma laje rasa, fica só sob os pés)

    readonly List<Transform> paper = new List<Transform>();
    readonly List<Material> tempMats = new List<Material>();
    Coroutine entering;

    string lastStatus = "\0";      // \0 = ainda não desenhado nenhuma vez
    float plateWidth = -1f;

    public bool Ready { get; private set; }   // false enquanto a dissolução roda

    // ═══════════════════════════════════════════════════════════════════
    //  ENTRADA / SAÍDA
    // ═══════════════════════════════════════════════════════════════════

    public void Enter(CardDisplay owner, bool animate)
    {
        card = owner;
        CollectPaper();
        BuildPlaque();
        AttachBodyCollider();
        RefreshStatus();

        if (!animate)
        {
            HidePaper();
            SetPlaqueAlpha(1f);
            Ready = true;
            return;
        }

        enterStartedAt = Time.time;
        if (entering != null) StopCoroutine(entering);
        entering = StartCoroutine(DissolveRoutine());
    }

    // Carta saiu do tabuleiro (voltou para a mão, virou outra coisa): o papel
    // volta na hora, sem animação — quem sai de campo é para ser lido de novo.
    public void Exit()
    {
        if (entering != null) { StopCoroutine(entering); entering = null; }
        RestoreMaterials();
        foreach (Transform t in paper)
            if (t != null) t.gameObject.SetActive(true);
        paper.Clear();

        if (plaque != null) Destroy(plaque.gameObject);
        plaque = null;
        statusText = null;
        statPlate = statPlateRing = statusPlate = null;
        lastStatus = "\0";
        plateWidth = -1f;

        if (bodyCollider != null) { Destroy(bodyCollider); bodyCollider = null; }
        Ready = false;
        enterStartedAt = -1f;   // desarma o vigia da entrada
        statusRowZ = StatusZ;
    }

    // Rede de segurança. A dissolução é uma corrotina; se ela morrer no meio
    // (objeto desativado, exceção em outro sistema no mesmo frame), a carta
    // ficava congelada meio transparente em cima da casa — o "fundo esquisito".
    // Aqui, passado o tempo dela com folga, a entrada se conclui sozinha.
    float enterStartedAt = -1f;

    void Update()
    {
        if (Ready || enterStartedAt < 0f) return;
        if (Time.time - enterStartedAt < FadeTime + RiseTime + 1.0f) return;

        Debug.LogWarning("[CardBoardPlaque] Entrada travou no meio — concluindo na marra.");
        ForceFinish();
    }

    void ForceFinish()
    {
        if (entering != null) { StopCoroutine(entering); entering = null; }

        Transform wave = transform.Find("EntranceWave");
        if (wave != null) Destroy(wave.gameObject);

        RestoreMaterials();
        HidePaper();
        if (plaque != null) plaque.localScale = Vector3.one;
        SetPlaqueAlpha(1f);
        FitBodyCollider();
        Ready = true;
    }

    void OnDestroy()
    {
        RestoreMaterials();
        foreach (Material m in tempMats) if (m != null) Destroy(m);
        tempMats.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  A DISSOLUÇÃO
    // ═══════════════════════════════════════════════════════════════════

    // Materiais originais de cada renderer de papel, para poder devolver
    readonly Dictionary<Renderer, Material[]> solidMats = new Dictionary<Renderer, Material[]>();

    void CollectPaper()
    {
        paper.Clear();

        // Estilo escolhido pelo jogador nas Configurações (BoardCardView):
        // no modo CARTA nada se desfaz — a miniatura fica de pé sobre a carta
        // dela, como numa mesa de verdade. SÍMBOLO e NENHUM dissolvem igual: a
        // diferença entre eles é só a base no chão (ver SetPlaqueAlpha —
        // símbolo = círculo preenchido, nenhum = só o aro).
        if (BoardCardView.Style == BoardCardStyle.Carta) return;

        foreach (Transform t in transform)
        {
            if (t == null || !t.gameObject.activeSelf) continue;
            if (KeepParts.Contains(t.name)) continue;
            // Os painelões de status são desligados à parte (quem informa
            // agora é a linha de nomes) — não entram na dissolução
            if (t.name.StartsWith("StatusOverlay_")) continue;
            if (t.GetComponent<Renderer>() == null && t.GetComponent<TMP_Text>() == null) continue;
            paper.Add(t);
        }
    }

    // O jogador trocou o estilo nas Configurações no meio da partida: reaplica
    // sem animação nenhuma (a dissolução é um efeito de ENTRADA; repetir aqui
    // ficaria estranho numa unidade que já está em campo há rounds).
    public void RefreshStyle()
    {
        if (card == null || !card.isOnBoard) return;
        if (!Ready) return; // ainda entrando — a rotina já vai coletar pelo novo

        // Devolve tudo ao normal antes de recolher pela regra nova
        RestoreMaterials();
        foreach (Transform t in paper)
            if (t != null) t.gameObject.SetActive(true);

        CollectPaper();
        HidePaper();

        // A base no chão também depende do estilo (ver SetPlaqueAlpha)
        SetPlaqueAlpha(plaqueAlpha);
    }

    IEnumerator DissolveRoutine()
    {
        Ready = false;
        SetPlaqueAlpha(0f);

        // Clona cada material do papel num Sprites/Default (o único shader com
        // transparência confiável no build URP — mesma escolha do modo fantasma
        // da figura e dos overlays de status). Um clone POR renderer: assim
        // cada painel some com a cor e a textura que tinha.
        var fadeMats = new List<Material>();
        var baseColors = new List<Color>();
        var texts = new List<TMP_Text>();

        Shader transp = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");

        foreach (Transform t in paper)
        {
            TMP_Text tmp = t.GetComponent<TMP_Text>();
            if (tmp != null) { texts.Add(tmp); continue; }

            Renderer r = t.GetComponent<Renderer>();
            if (r == null || transp == null) continue;

            if (!solidMats.ContainsKey(r)) solidMats[r] = r.sharedMaterials;

            Material src = r.sharedMaterial;
            Material clone = new Material(transp);
            if (src != null)
            {
                clone.mainTexture = src.mainTexture;
                clone.color = src.color;
            }
            tempMats.Add(clone);

            Material[] slots = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < slots.Length; i++) slots[i] = clone;
            r.sharedMaterials = slots;

            fadeMats.Add(clone);
            baseColors.Add(clone.color);
        }

        // Onda dourada saindo do tile: é ela que dá o "a carta virou tropa"
        Transform wave = BuildShockwave();

        float t0 = 0f;
        while (t0 < 1f)
        {
            t0 += Time.deltaTime / FadeTime;
            float k = Mathf.Clamp01(t0);

            // O papel some cedo; a onda acompanha até o fim
            float paperA = 1f - Mathf.Clamp01(k * 1.35f);
            for (int i = 0; i < fadeMats.Count; i++)
            {
                Color c = baseColors[i];
                c.a = paperA;
                fadeMats[i].color = c;
            }
            foreach (TMP_Text tmp in texts) if (tmp != null) tmp.alpha = paperA;

            if (wave != null)
            {
                float s = Mathf.Lerp(0.5f, 2.7f, Mathf.Sqrt(k)); // rápido no início
                wave.localScale = new Vector3(s, 1f, s);
                Renderer wr = wave.GetComponent<Renderer>();
                if (wr != null)
                {
                    Color wc = Gold;
                    wc.a = (1f - k) * 0.85f;
                    wr.material.color = wc;
                }
            }
            yield return null;
        }

        if (wave != null) Destroy(wave.gameObject);
        foreach (TMP_Text tmp in texts) if (tmp != null) tmp.alpha = 1f; // some por SetActive
        HidePaper();
        RestoreMaterials();

        // A placa de stats sobe no lugar do papel
        float t1 = 0f;
        while (t1 < 1f)
        {
            t1 += Time.deltaTime / RiseTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t1));
            SetPlaqueAlpha(k);
            if (plaque != null)
            {
                float s = Mathf.Lerp(0.72f, 1f, k);
                plaque.localScale = new Vector3(s, 1f, s);
            }
            yield return null;
        }

        if (plaque != null) plaque.localScale = Vector3.one;
        SetPlaqueAlpha(1f);
        FitBodyCollider();   // personagem já parado: agora a caixa fecha nele
        Ready = true;
        entering = null;
    }

    void HidePaper()
    {
        foreach (Transform t in paper)
            if (t != null) t.gameObject.SetActive(false);
    }

    void RestoreMaterials()
    {
        foreach (var kv in solidMats)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        solidMats.Clear();
    }

    Transform BuildShockwave()
    {
        Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) return null;

        GameObject go = new GameObject("EntranceWave");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.030f, 0f);
        go.AddComponent<MeshFilter>().sharedMesh =
            CardDisplay.GetRoundedRingMesh(1.9f, 1.9f, 0.95f, 0.07f);
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = new Material(s);
        tempMats.Add(mr.material);
        return go.transform;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  A PLACA
    // ═══════════════════════════════════════════════════════════════════
    //
    // Espaço LOCAL da carta: o plano dela é XZ (Y é a altura), o topo fica em
    // z ≈ -1.25 e a base em z ≈ +1.25. O personagem pisa em z = -0.35, então
    // "embaixo do personagem" é z crescente. X aparece ESPELHADO na tela (a
    // carta nasce com giro Y=180), por isso ATQ fica em x positivo.
    //
    //   z ≈ +0.55   nomes dos status  (CONGELADA 2 · PRESA 1)
    //   z ≈ +1.00   placa de stats    (ATQ / ARM / VIDA — escudos já existentes)
    //   z ≈ +1.35   bolinhas de ação  (CardActionDots, intocadas)

    const float StatusZ = 0.62f;   // padrão; desce se a base do boneco for larga
    const float StatusZMax = 0.86f; // além disto encostaria na placa de stats
    const float StatsZ = 1.00f;

    float statusRowZ = StatusZ;

    void BuildPlaque()
    {
        if (plaque != null) return;

        GameObject root = new GameObject("Plaque");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        plaque = root.transform;

        // BASE DE MINIATURA sob os pés, na cor do dono. Antes era só um aro de
        // 0.055 de espessura (0.11 no mundo): com a câmera a 45° ele aparecia
        // achatado e o corpo do boneco cobria metade dele — num tabuleiro
        // cheio não dava para bater o olho e saber de quem era cada unidade.
        // Agora é um disco CHEIO (semitransparente, a pedra do tabuleiro ainda
        // aparece por baixo) com um aro opaco por cima fechando a borda.
        footBase = MakePlate("FootBase",
            CardDisplay.GetRoundedRectMesh(FootSize, FootSize, FootSize * 0.5f),
            new Vector3(0f, 0.005f, -0.35f), Gold);
        footRing = MakePlate("FootRing",
            CardDisplay.GetRoundedRingMesh(FootSize, FootSize, FootSize * 0.5f, 0.075f),
            new Vector3(0f, 0.008f, -0.35f), Gold);

        // Placa sob os três escudos heráldicos (que vivem em y 0.009/0.012 e
        // continuam desenhando por cima dela)
        statPlate = MakePlate("StatPlate",
            CardDisplay.GetRoundedRectMesh(1.90f, 0.52f, 0.17f),
            new Vector3(0f, 0.004f, StatsZ), PlateInk);
        statPlateRing = MakePlate("StatPlateRing",
            CardDisplay.GetRoundedRingMesh(1.90f, 0.52f, 0.17f, 0.020f),
            new Vector3(0f, 0.006f, StatsZ), Gold);

        // Nomes dos status: uma linha de texto rico (cada status na cor dele)
        // sobre uma plaquinha que se redimensiona conforme o texto
        statusPlate = MakePlate("StatusPlate",
            CardDisplay.GetRoundedRectMesh(1.2f, 0.34f, 0.17f),
            new Vector3(0f, 0.004f, statusRowZ), StatusInk);

        GameObject txt = new GameObject("StatusNames");
        txt.transform.SetParent(plaque, false);
        txt.transform.localPosition = new Vector3(0f, 0.022f, statusRowZ);
        txt.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        statusText = txt.AddComponent<TextMeshPro>();
        statusText.fontSize = 1.05f;
        statusText.fontStyle = FontStyles.Bold;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.richText = true;
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.overflowMode = TextOverflowModes.Overflow;
        statusText.rectTransform.sizeDelta = new Vector2(2.9f, 0.9f);
        statusText.characterSpacing = 4f;
        statusText.outlineWidth = 0.22f;   // legível sobre qualquer piso
        statusText.outlineColor = new Color32(8, 6, 4, 255);
        UIFonts.Set(statusText, UIFonts.Body);

        SetStatus("");
    }

    Renderer MakePlate(string name, Mesh mesh, Vector3 localPos, Color color)
    {
        Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
        GameObject go = new GameObject(name);
        go.transform.SetParent(plaque, false);
        go.transform.localPosition = localPos;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        if (s != null)
        {
            mr.material = new Material(s) { color = color };
            tempMats.Add(mr.material);
        }
        return mr;
    }

    // Alpha de tudo o que a placa desenha (usado na entrada e para sumir com
    // a linha de status quando não há status nenhum)
    float plaqueAlpha = 1f;

    void SetPlaqueAlpha(float a)
    {
        plaqueAlpha = a;
        SetRendererAlpha(statPlate, PlateInk, a);
        SetRendererAlpha(statPlateRing, ownerColor, a);
        // A base do chão muda conforme o estilo escolhido nas Configurações:
        //   CARTA   — some inteira: a borda colorida da PRÓPRIA carta já diz
        //             de quem é a unidade, e o disco viraria poluição
        //   SÍMBOLO — disco cheio (translúcido, deixando a pedra aparecer) +
        //             aro, que é o que dá o contorno nítido
        //   NENHUM  — só o aro, o contorno limpo
        BoardCardStyle estilo = BoardCardView.Style;
        float aDisco = estilo == BoardCardStyle.Simbolo ? a * 0.55f : 0f;
        float aAro = estilo == BoardCardStyle.Carta ? 0f : a * 0.95f;
        SetRendererAlpha(footBase, ownerColor, aDisco);
        SetRendererAlpha(footRing, ownerColor, aAro);
        bool hasStatus = statusText != null && !string.IsNullOrEmpty(statusText.text);
        SetRendererAlpha(statusPlate, StatusInk, hasStatus ? a : 0f);
        if (statusText != null) statusText.alpha = hasStatus ? a : 0f;
    }

    void SetRendererAlpha(Renderer r, Color baseColor, float a)
    {
        if (r == null || r.material == null) return;
        Color c = baseColor;
        c.a *= a;
        r.material.color = c;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  NOMES DOS STATUS
    // ═══════════════════════════════════════════════════════════════════

    // Chamado a cada UpdateDisplay pelo CardDisplay. Só redesenha quando o
    // texto muda de verdade (a placa de fundo é remedida junto).
    public void RefreshStatus()
    {
        if (card == null || statusText == null) return;
        RefreshOwnerColor();
        SetStatus(BuildStatusLine(card));
    }

    // Azul = jogador 1, carmesim = jogador 2 (as mesmas cores da borda que a
    // carta tinha, clareadas para lerem sobre a pedra do tabuleiro). Congelada
    // pinta de gelo — o mesmo aviso que a figura já recebe.
    void RefreshOwnerColor()
    {
        Color c = card.isFrozen ? new Color(0.55f, 0.82f, 1.00f)
                : card.ownerPlayerNumber == 1 ? new Color(0.38f, 0.58f, 1.00f)
                : card.ownerPlayerNumber == 2 ? new Color(1.00f, 0.40f, 0.34f)
                : Gold;
        if (c == ownerColor) return;
        ownerColor = c;
        SetPlaqueAlpha(plaqueAlpha);
    }

    void SetStatus(string line)
    {
        if (line == lastStatus) return;
        lastStatus = line;

        statusText.text = line;
        bool has = !string.IsNullOrEmpty(line);
        statusText.alpha = has ? plaqueAlpha : 0f;

        if (!has)
        {
            SetRendererAlpha(statusPlate, StatusInk, 0f);
            return;
        }

        // Placa do tamanho do texto (largura arredondada em passos de 0.1 para
        // o cache de meshes do CardDisplay não encher de variantes)
        Vector2 pref = statusText.GetPreferredValues(line, 2.9f, 0f);
        float w = Mathf.Clamp(Mathf.Ceil((pref.x + 0.34f) * 10f) / 10f, 0.7f, 3.1f);
        float h = pref.y > 0.55f ? 0.66f : 0.36f;   // 1 ou 2 linhas
        if (!Mathf.Approximately(w, plateWidth))
        {
            plateWidth = w;
            MeshFilter mf = statusPlate != null ? statusPlate.GetComponent<MeshFilter>() : null;
            if (mf != null) mf.sharedMesh = CardDisplay.GetRoundedRectMesh(w, h, h * 0.5f);
        }
        SetRendererAlpha(statusPlate, StatusInk, plaqueAlpha);
    }

    // Um "chip" por status ativo, com a contagem quando ela existe.
    // Debuffs primeiro (é o que decide a jogada), depois o que é bônus.
    static string BuildStatusLine(CardDisplay c)
    {
        List<string> chips = new List<string>();

        Add(chips, c.isFrozen, "CONGELADA", c.freezeTurnsLeft, "#7FD4FF");
        Add(chips, c.isStunned, "ATORDOADA", c.stunTurnsLeft, "#FFD84D");
        Add(chips, c.sleepTurnsLeft > 0, "DORMINDO", c.sleepTurnsLeft, "#B79BF0");
        Add(chips, c.rootTurnsLeft > 0, "PRESA", c.rootTurnsLeft, "#D6CEA8");
        Add(chips, c.eagleMarked, "MARCADA", c.eagleTurnsLeft, "#D9A0FF");
        Add(chips, c.IsTaunted(), "PROVOCADA", c.tauntTurnsLeft, "#FF9B7A");
        Add(chips, c.wallRoundsLeft > 0, "MURO", c.wallRoundsLeft, "#C9B79A");

        Add(chips, c.invulnerableRoundsLeft > 0, "INVULNERÁVEL", c.invulnerableRoundsLeft, "#FFE38C");
        Add(chips, c.stoneSkinRoundsLeft > 0, "PELE DE PEDRA", c.stoneSkinRoundsLeft, "#B9C4CC");
        Add(chips, c.treeDefenseActive, "NA ÁRVORE", 0, "#A6F0B6");
        Add(chips, c.spellBonusAttack > 0, "LÂMINA +" + c.spellBonusAttack, 0, "#FF9E6B");
        Add(chips, c.spellBonusShield > 0, "ARMADURA +" + c.spellBonusShield, 0, "#8FC6FF");
        Add(chips, c.spellBonusMoves > 0, "BOTAS", 0, "#9BE8C4");
        Add(chips, c.hymnAttackBonus > 0, "HINO +" + c.hymnAttackBonus, 0, "#FFC96B");
        Add(chips, c.spellExtraAttacks > 0, "CONCENTRAÇÃO", c.spellExtraAttacks, "#FFB0D8");

        if (chips.Count == 0) return "";
        return string.Join("  ", chips.ToArray());
    }

    static void Add(List<string> into, bool active, string label, int count, string hex)
    {
        if (!active) return;
        string txt = count > 0 ? label + " " + count : label;
        into.Add("<color=" + hex + ">" + txt + "</color>");
    }

    // ═══════════════════════════════════════════════════════════════════
    //  COLLIDER DO CORPO
    // ═══════════════════════════════════════════════════════════════════
    //
    // O BoxCollider da carta é uma laje rasa (1.8 × 0.1 × 2.5): funcionava
    // enquanto a carta era a coisa visível. Agora o alvo é o PERSONAGEM, e ele
    // fica ACIMA dessa laje — sem isto o mouse atravessava o boneco e não havia
    // como passar o mouse nem clicar com o direito nele. Uma coluna estreita
    // em volta do corpo, de propósito: mais larga que isso e ela começaria a
    // roubar os cliques da casa de trás na câmera inclinada.
    // Largura máxima da coluna, em unidades locais da carta (× escala 2 = 2.3
    // no mundo, contra 6 de uma casa): estreita de propósito
    const float BodyWide = 1.15f;

    void AttachBodyCollider()
    {
        if (bodyCollider != null) return;

        // Caixa provisória pelas constantes do FitFigureOnCard (figura de 4.5
        // de altura no mundo, ancorada em z -0.35). A medida de verdade vem no
        // FitBodyCollider, quando a animação de entrada já assentou — medir
        // agora pegaria o personagem no meio da queda.
        bodyCollider = gameObject.AddComponent<BoxCollider>();
        bodyCollider.center = new Vector3(0f, 1.20f, -0.35f);
        bodyCollider.size = new Vector3(BodyWide, 2.40f, BodyWide);
    }

    void FitBodyCollider()
    {
        if (bodyCollider == null) return;

        Transform fig = transform.Find("BoardFigure");
        if (fig == null) return;

        Renderer[] rs = fig.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        if (b.size.y < 0.01f) return;

        float k = Mathf.Abs(transform.lossyScale.x);
        if (k < 0.0001f) k = 1f;

        Vector3 center = transform.InverseTransformPoint(b.center);
        Vector3 size = b.size / k;

        bodyCollider.center = center;
        bodyCollider.size = new Vector3(
            Mathf.Min(size.x, BodyWide),
            Mathf.Max(size.y, 0.4f),
            Mathf.Min(size.z, BodyWide));

        // Alguns personagens vêm com base/pedestal bem mais larga que os
        // outros e cobriam a linha de status. A carta só gira em Y, então o
        // alcance em z do modelo vale direto como z local: empurra a linha
        // para depois da ponta dos pés dele (até o limite antes dos stats).
        float frenteDoBoneco = center.z + size.z * 0.5f;
        SetStatusRowZ(Mathf.Clamp(frenteDoBoneco + 0.26f, StatusZ, StatusZMax));
    }

    void SetStatusRowZ(float z)
    {
        if (Mathf.Approximately(z, statusRowZ)) return;
        statusRowZ = z;
        if (statusPlate != null)
            statusPlate.transform.localPosition = new Vector3(0f, 0.004f, z);
        if (statusText != null)
            statusText.transform.localPosition = new Vector3(0f, 0.022f, z);
    }
}
