using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Calibração AO VIVO do TAMPO da mesa em tábuas (tema Mesa de RPG).
//
// Irmão do BoardFrameTuning — mesma ideia, mesmo fluxo:
// 1. Na cena SampleScene, crie um GameObject vazio (ex. "TampoTuning") e
//    adicione este componente nele.
// 2. Dê Play, entre numa partida (o treino contra o bot serve) e mexa nos
//    valores — o tampo se remonta NA HORA a cada mudança.
// 3. Achou o ponto? AINDA EM PLAY: três pontinhos do componente →
//    "Copy Component". Saia do Play e, no mesmo componente, três pontinhos →
//    "Paste Component Values". Depois Ctrl+S.
//
// ⚠️ Com o MesaStage assado na cena, o código NÃO monta o cenário — então
// estes sliders não têm o que remontar. Para calibrar: apague (ou desative) o
// MesaStage, acerte os números aqui, e depois use o menu
// "Cardsworn → Mesa de RPG: refazer só o TAMPO com as tábuas".
//
// Puramente visual — nenhum efeito no lockstep.
// ════════════════════════════════════════════════════════════════════════════
public class TableTopTuning : MonoBehaviour
{
    [Header("Tábuas")]
    [Tooltip("Desmarcado = volta à laje única com a madeira procedural de antes")]
    public bool usarTabuas = true;

    [Tooltip("Largura de cada tábua. A mesa tem 90 de fundo: 8 dá ~11 tábuas")]
    [Range(3f, 20f)] public float larguraTabua = 8f;

    [Tooltip("Espessura da tábua (o resto da altura da mesa é a base escura)")]
    [Range(0.4f, 4f)] public float espessuraTabua = 1.5f;

    [Tooltip("Sobra lateral: um tiquinho a mais para não abrir fresta entre elas")]
    [Range(0f, 1.5f)] public float sobraLateral = 0.15f;

    [Tooltip("Sorteia tábuas giradas 180° — o mesmo modelo repetido fica menos óbvio")]
    public bool alternarSentido = true;

    public static TableTopTuning Ativa { get; private set; }

    void OnEnable() { Ativa = this; }
    void OnDisable() { if (Ativa == this) Ativa = null; }

    // Assinatura barata dos valores: mudou qualquer campo → remonta o tampo
    float ultimaAssinatura = float.NaN;

    void Update()
    {
        float agora = larguraTabua * 1.31f + espessuraTabua * 2.17f
                    + sobraLateral * 3.03f
                    + (usarTabuas ? 5.13f : 0f) + (alternarSentido ? 7.77f : 0f);
        if (agora == ultimaAssinatura) return;
        bool primeira = float.IsNaN(ultimaAssinatura);
        ultimaAssinatura = agora;
        if (!primeira) TabletopEnvironment.RebuildTableTop();
    }
}
