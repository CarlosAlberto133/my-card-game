using UnityEngine;
using System.Collections;

// Anima a figura 3D estática sobre a carta (modelo SEM esqueleto/bones —
// animações esqueléticas baixadas não funcionam em .obj estático; isto aqui
// dá vida por transformação pura):
// - Entrada: cai do céu e aterrissa com squash & stretch
// - Idle: respiração (sobe/desce) + balanço sutil
// - Movimento: quando a carta troca de tile, dá pulinhos
// - Ataque: inclinada rápida de golpe (a carta inteira já faz a investida)
// Puramente visual e local em cada cliente — não toca no estado do jogo,
// então não afeta o lockstep do multiplayer.
public class FigureAnimator : MonoBehaviour
{
    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRot;
    private float phase;              // desincroniza a respiração de figuras vizinhas
    private float bounceUntil = 0f;   // pulinhos de deslocamento até este instante
    private float busyUntil = 0f;     // entrada/golpe no controle: idle não interfere
    private float moveMuteUntil = 0f; // ignora a investida do ataque como "movimento"
    private Vector3 lastParentPos;
    private bool initialized = false;

    // Chamar DEPOIS do FitFigureOnCard: captura a pose final como base
    public void Initialize()
    {
        baseLocalPos = transform.localPosition;
        baseLocalScale = transform.localScale;
        baseLocalRot = transform.localRotation;
        phase = Mathf.Abs(transform.position.x * 7.13f + transform.position.z * 3.71f) % (Mathf.PI * 2f);
        if (transform.parent != null) lastParentPos = transform.parent.position;
        initialized = true;
    }

    public void PlayEntrance()
    {
        if (!initialized || !gameObject.activeInHierarchy) return;
        StartCoroutine(EntranceRoutine());
    }

    public void Strike()
    {
        if (!initialized || !gameObject.activeInHierarchy) return;
        // A investida da carta (CardAnimator.Lunge) move o pai — não é troca de tile
        moveMuteUntil = Time.time + 1.2f;
        StartCoroutine(StrikeRoutine());
    }

    IEnumerator EntranceRoutine()
    {
        const float dropTime = 0.32f;
        const float squashTime = 0.28f;
        busyUntil = Time.time + dropTime + squashTime;

        // Queda (unidades locais; a carta tem escala 2 no tabuleiro)
        Vector3 from = baseLocalPos + Vector3.up * 3.5f;
        float t = 0f;
        while (t < dropTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dropTime);
            transform.localPosition = Vector3.Lerp(from, baseLocalPos, k * k); // acelera caindo
            yield return null;
        }
        transform.localPosition = baseLocalPos;

        // Impacto: achata e recupera (seno vai 0→1→0 num ciclo só)
        t = 0f;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            float s = Mathf.Sin(Mathf.Clamp01(t / squashTime) * Mathf.PI);
            transform.localScale = new Vector3(
                baseLocalScale.x * (1f + 0.10f * s),
                baseLocalScale.y * (1f - 0.16f * s),
                baseLocalScale.z * (1f + 0.10f * s));
            yield return null;
        }
        transform.localScale = baseLocalScale;
    }

    IEnumerator StrikeRoutine()
    {
        const float inTime = 0.10f;
        const float outTime = 0.26f;
        busyUntil = Time.time + inTime + outTime;

        // Golpe: inclina rápido para frente...
        float t = 0f;
        while (t < inTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / inTime);
            transform.localRotation = baseLocalRot * Quaternion.Euler(26f * k, 0f, 0f);
            yield return null;
        }
        // ...e volta suave
        t = 0f;
        while (t < outTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / outTime);
            transform.localRotation = baseLocalRot * Quaternion.Euler(26f * k, 0f, 0f);
            yield return null;
        }
        transform.localRotation = baseLocalRot;
    }

    void Update()
    {
        if (!initialized) return;

        // Carta trocou de posição (movimento de tile) → pulinhos de deslocamento
        if (transform.parent != null)
        {
            float moved = Vector3.Distance(transform.parent.position, lastParentPos);
            lastParentPos = transform.parent.position;
            if (moved > 0.5f && Time.time >= moveMuteUntil)
                bounceUntil = Time.time + 0.45f;
        }

        // Entrada ou golpe tocando: as corrotinas mandam no transform
        if (Time.time < busyUntil) return;

        float bob;
        if (Time.time < bounceUntil)
        {
            // Pulinhos rápidos enquanto "chega" no novo tile
            bob = Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 0.35f;
        }
        else
        {
            // Respiração: sobe e desce devagar
            bob = (Mathf.Sin(Time.time * 2.1f + phase) * 0.5f + 0.5f) * 0.06f;
        }
        transform.localPosition = baseLocalPos + Vector3.up * bob;

        // Balanço sutil no eixo Y (vivo, mas sem sair da direção do inimigo)
        float sway = Mathf.Sin(Time.time * 1.3f + phase) * 2.2f;
        transform.localRotation = baseLocalRot * Quaternion.Euler(0f, sway, 0f);
    }

    // Reapareceu depois do hover (a carta esconde a figura com o mouse em
    // cima): volta limpo para a pose base — corrotinas morrem no disable
    void OnEnable()
    {
        if (!initialized) return;
        transform.localPosition = baseLocalPos;
        transform.localScale = baseLocalScale;
        transform.localRotation = baseLocalRot;
        busyUntil = 0f;
        bounceUntil = 0f;
    }
}
