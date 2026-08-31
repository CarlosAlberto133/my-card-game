using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Calibração AO VIVO da moldura do tabuleiro (tema Mesa de RPG).
//
// COMO USAR:
// 1. Na cena SampleScene, crie um GameObject vazio (nome livre, ex.
//    "MolduraTuning") e adicione este componente nele.
// 2. Dê Play, entre numa partida (o treino contra o bot serve) e mexa nos
//    sliders — a moldura se remonta NA HORA a cada mudança.
// 3. Achou o ponto? AINDA EM PLAY: três pontinhos do componente →
//    "Copy Component". Saia do Play e, no mesmo componente, três pontinhos →
//    "Paste Component Values". (Mudança feita em Play mode se perde ao sair
//    do Play; o copiar/colar traz os números de volta.)
//
// Com o componente na cena, os valores DELE valem para o jogo (ficam salvos
// na cena). Sem ele, valem os padrões do TabletopEnvironment. Puramente
// visual — nenhum efeito no lockstep.
// ════════════════════════════════════════════════════════════════════════════
public class BoardFrameTuning : MonoBehaviour
{
    [Header("Cantos (peça em L)")]
    [Tooltip("Tamanho do canto no chão (1 = mesmo comprimento de uma mureta)")]
    [Range(0.8f, 1.8f)] public float escalaCanto = 1.20f;

    [Tooltip("Empurra o canto para FORA na diagonal")]
    [Range(-1f, 3f)] public float empurraoCanto = 0.97f;

    [Tooltip("Teto de altura do canto (a mureta tem ~1.95)")]
    [Range(1f, 4f)] public float alturaCanto = 2.61f;

    [Tooltip("Ajuste fino vertical: positivo sobe, negativo desce")]
    [Range(-1.5f, 1.5f)] public float sobeCanto = 0f;

    [Header("Muretas")]
    [Tooltip("Quantas muretas por lado, entre os dois cantos")]
    [Range(4, 10)] public int muretasPorLado = 6;

    [Header("Sentido das muretas (marcado = girada 180°)")]
    [Tooltip("Lado de CIMA da tela do jogador 1 (longe de você)")]
    public bool girarCima = true;
    [Tooltip("Lado de BAIXO da tela do jogador 1 (perto de você)")]
    public bool girarBaixo = false;
    [Tooltip("Lado DIREITO da tela do jogador 1")]
    public bool girarDireita = true;
    [Tooltip("Lado ESQUERDO da tela do jogador 1")]
    public bool girarEsquerda = false;

    public static BoardFrameTuning Ativa { get; private set; }

    void OnEnable() { Ativa = this; }
    void OnDisable() { if (Ativa == this) Ativa = null; }

    // Assinatura barata dos valores: mudou qualquer slider → remonta a moldura
    float ultimaAssinatura = float.NaN;

    void Update()
    {
        float agora = escalaCanto * 1.31f + empurraoCanto * 2.17f
                    + alturaCanto * 3.03f + sobeCanto * 4.71f
                    + muretasPorLado * 5.13f
                    + (girarCima ? 7.77f : 0f) + (girarBaixo ? 11.3f : 0f)
                    + (girarDireita ? 13.9f : 0f) + (girarEsquerda ? 17.3f : 0f);
        if (agora == ultimaAssinatura) return;
        bool primeira = float.IsNaN(ultimaAssinatura);
        ultimaAssinatura = agora;
        if (!primeira) TabletopEnvironment.RebuildFrame();
    }
}
