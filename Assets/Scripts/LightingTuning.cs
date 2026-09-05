using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Calibração AO VIVO da ILUMINAÇÃO da Mesa de RPG.
//
// Irmão do BoardFrameTuning e do TableTopTuning — mesmo fluxo:
// 1. Na SampleScene, crie um GameObject vazio (ex. "LuzTuning") e adicione
//    este componente nele.
// 2. Dê Play, entre numa partida (o treino contra o bot serve) e mexa nos
//    valores — a iluminação se refaz NA HORA a cada mudança.
// 3. Achou o ponto? AINDA EM PLAY: três pontinhos do componente →
//    "Copy Component". Saia do Play e, no mesmo componente, três pontinhos →
//    "Paste Component Values". Depois Ctrl+S.
//
// ✔️ Ao contrário dos outros dois, este funciona COM o MesaStage assado: a
// iluminação é ajustada por código em cima do que existir na cena, não
// remontada. É o mesmo caminho do fundo da câmera.
//
// O que NÃO está aqui, porque mora em asset e não em código:
//   • Bloom / Vignette / Tonemapping → Assets/Settings/SampleSceneProfile
//   • Distância de sombra, MSAA, luzes por objeto → Assets/Settings/PC_RPAsset
//
// Puramente visual — nenhum efeito no lockstep.
// ════════════════════════════════════════════════════════════════════════════
public class LightingTuning : MonoBehaviour
{
    [Header("Ambiente (o maior ganho)")]
    [Tooltip("Desmarcado = volta ao ambiente do skybox, o azul de dia claro " +
             "que lavava a mesa e comia o contraste das tochas")]
    public bool ambienteEscuro = true;

    [Tooltip("Luz que vem de cima")]
    public Color corAlto = new Color(0.340f, 0.292f, 0.250f);

    [Tooltip("Luz que vem dos lados")]
    public Color corMeio = new Color(0.260f, 0.220f, 0.188f);

    [Tooltip("Luz que sobe do chão (a mais escura)")]
    public Color corChao = new Color(0.160f, 0.132f, 0.110f);

    [Header("Luz principal (a direcional da cena)")]
    [Tooltip("Estava em 2 e branca: um sol de meio-dia numa taverna. " +
             "Abaixo de 1 ela vira luz de sala e deixa as tochas falarem")]
    [Range(0f, 3f)] public float luzPrincipal = 1.60f;

    public Color corLuzPrincipal = new Color(1.00f, 0.93f, 0.80f);

    [Header("Névoa")]
    [Tooltip("Na cor do fundo da câmera: as bordas da mesa derretem no escuro " +
             "em vez de terminarem num corte seco")]
    public bool nevoa = true;

    [Tooltip("Exponencial ao quadrado. Passando de ~0.01 a mesa some")]
    [Range(0f, 0.02f)] public float densidadeNevoa = 0.0032f;

    public Color corNevoa = new Color(0.080f, 0.055f, 0.040f);

    [Header("Tochas")]
    [Tooltip("Multiplica a intensidade de fábrica de cada tocha (2.3 na cena). " +
             "1 = como estava")]
    [Range(0.2f, 4f)] public float tochas = 1.00f;

    [Tooltip("Multiplica o alcance de fábrica (24 na cena)")]
    [Range(0.5f, 3f)] public float alcanceTochas = 1.00f;

    public static LightingTuning Ativa { get; private set; }

    void OnEnable() { Ativa = this; }
    void OnDisable() { if (Ativa == this) Ativa = null; }

    // Assinatura barata dos valores: mudou qualquer campo → refaz a luz
    float ultimaAssinatura = float.NaN;

    void Update()
    {
        float agora = luzPrincipal * 1.31f + densidadeNevoa * 977f
                    + tochas * 2.17f + alcanceTochas * 3.03f
                    + (ambienteEscuro ? 5.13f : 0f) + (nevoa ? 7.77f : 0f)
                    + Soma(corAlto) * 11f + Soma(corMeio) * 13f
                    + Soma(corChao) * 17f + Soma(corLuzPrincipal) * 19f
                    + Soma(corNevoa) * 23f;

        if (agora == ultimaAssinatura) return;
        bool primeira = float.IsNaN(ultimaAssinatura);
        ultimaAssinatura = agora;
        if (!primeira) TabletopEnvironment.RebuildLighting();
    }

    static float Soma(Color c) { return c.r + c.g * 1.7f + c.b * 2.9f; }
}
