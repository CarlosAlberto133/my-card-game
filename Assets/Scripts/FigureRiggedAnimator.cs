using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Anima uma figura COM RIG (FBX do Mixamo) via API de Playables — toca os
// AnimationClips direto no Animator, sem AnimatorController.
//
// Estados:
// - PARADO: clip de idle em loop (ou Walking congelado numa pose neutra).
// - VIAGEM: quando a CARTA muda de casa (teleporte), o personagem FICA na casa
//   antiga, espera um instante e então CAMINHA até a carta (vira para a direção
//   do movimento, anda em velocidade constante, chega, vira de volta). É o que
//   deixa o movimento "real": a carta desliza, o boneco vai andando atrás.
// - GOLPE (Strike) / LEVAR DANO (Hit): one-shot e volta ao estado natural.
// - MORTE (PlayDeath): toca o clip e congela no último frame.
// 100% visual/local — nenhuma relação com o lockstep.
public class FigureRiggedAnimator : MonoBehaviour
{
    // ── Constantes de ajuste ─────────────────────────────────────────────
    const float IdlePoseNormalizedTime = 0.25f; // pose neutra do ciclo de caminhada
    const float WalkHysteresis = 0.25f;  // movimento contínuo: segue andando por este tempo
    const float MoveEpsilon = 0.01f;     // deslocamento mínimo por frame p/ contar movimento
    const float JumpThreshold = 2.5f;    // acima disso num frame = a carta TELEPORTOU de casa
    const float TravelDelay = 0.6f;      // espera na casa antiga antes de sair andando
    const float TravelWalkSpeed = 5.2f;  // unidades/s (1 casa = 6.6 → ~1.3s de caminhada)
    const float ArriveEpsilon = 0.06f;   // distância para considerar "chegou"
    const float FaceTurnSpeed = 10f;     // velocidade do giro para a direção do movimento
    const float TurnBackDuration = 0.25f;// giro de volta ao chegar

    enum State { Idle, Walk, OneShot, Death }

    private PlayableGraph graph;
    private AnimationPlayableOutput output;
    private AnimationClipPlayable walkP, attackP, idleP, deathP, reactP;
    private AnimationClip walkClip, attackClip, idleClip, deathClip, reactClip;

    private State state = State.Idle;
    private float oneShotUntil = -1f;
    private float walkingUntil = -1f;
    private Vector3 lastPos;
    private Transform moveTracker; // o que vigiar (a CARTA — hover só escala, não move)

    // Viagem (carta trocou de casa e o boneco vai andando até ela)
    private Vector3 homeLocalPos;       // posição local correta sobre a carta
    private Quaternion homeLocalRot;    // rotação local correta (de frente pro inimigo)
    private bool traveling = false;
    private float travelGoTime = 0f;    // quando começa a andar (após o delay)
    private bool turningBack = false;
    private float turnBackT = 0f;
    private Quaternion turnBackFrom;

    // Cancelador do avanço do clip de caminhada: clips do Mixamo baixados SEM
    // "In Place" movem o QUADRIL pra frente dentro do clip e ele volta num
    // estalo quando o clip reinicia ("anda de novo rapidinho"). Aqui o quadril
    // é PRESO no lugar: cada frame o esqueleto é recuado pelo tanto que o clip
    // avançou. Com clips "In Place" o desvio é ~0 e isso vira um no-op.
    private Transform hipsBone;             // mixamorig:Hips
    private Transform rigChild;             // filho direto da raiz que contém o esqueleto
    private Vector3 rigChildBaseLocalPos;
    private bool driftRefCaptured = false;
    private Vector3 driftRefLocal;

    public bool HasDeathClip { get { return deathClip != null; } }
    public float DeathLength { get { return deathClip != null ? deathClip.length : 0f; } }

    public void Initialize(AnimationClip walk, AnimationClip attack,
                           AnimationClip idle = null, AnimationClip death = null,
                           AnimationClip react = null, Transform tracker = null)
    {
        walkClip = walk; attackClip = attack; idleClip = idle; deathClip = death; reactClip = react;
        moveTracker = tracker;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();

        graph = PlayableGraph.Create("FigureRig");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        output = AnimationPlayableOutput.Create(graph, "FigureRigOut", animator);

        if (walkClip != null)   walkP   = AnimationClipPlayable.Create(graph, walkClip);
        if (attackClip != null) attackP = AnimationClipPlayable.Create(graph, attackClip);
        if (idleClip != null)   idleP   = AnimationClipPlayable.Create(graph, idleClip);
        if (deathClip != null)  deathP  = AnimationClipPlayable.Create(graph, deathClip);
        if (reactClip != null)  reactP  = AnimationClipPlayable.Create(graph, reactClip);

        // "Casa" da figura sobre a carta (FitFigureOnCard já rodou)
        homeLocalPos = transform.localPosition;
        homeLocalRot = transform.localRotation;

        // Localiza o quadril e o nó do esqueleto (para o cancelador de avanço)
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("Hips")) { hipsBone = t; break; }
        if (hipsBone != null)
        {
            Transform walker = hipsBone;
            while (walker.parent != null && walker.parent != transform) walker = walker.parent;
            rigChild = (walker.parent == transform) ? walker : null;
            if (rigChild != null) rigChildBaseLocalPos = rigChild.localPosition;
        }

        lastPos = TrackedPos();
        EnterIdle();
        graph.Play();
    }

    // ── Estados ──────────────────────────────────────────────────────────

    void EnterIdle()
    {
        state = State.Idle;
        if (idleP.IsValid())
        {
            idleP.SetTime(0); idleP.SetSpeed(1);
            output.SetSourcePlayable(idleP);
        }
        else if (walkP.IsValid() && walkClip != null)
        {
            walkP.SetTime(walkClip.length * IdlePoseNormalizedTime);
            walkP.SetSpeed(0);
            output.SetSourcePlayable(walkP);
        }
    }

    void EnterWalk()
    {
        state = State.Walk;
        if (!walkP.IsValid()) { EnterIdle(); return; }
        walkP.SetSpeed(1);
        output.SetSourcePlayable(walkP);
    }

    void PlayOneShot(AnimationClipPlayable p, AnimationClip clip)
    {
        if (state == State.Death) return;
        if (!p.IsValid() || clip == null) return;
        state = State.OneShot;
        p.SetTime(0); p.SetSpeed(1);
        output.SetSourcePlayable(p);
        oneShotUntil = Time.time + clip.length;
    }

    public void Strike() { PlayOneShot(attackP, attackClip); }
    public void Hit()    { PlayOneShot(reactP, reactClip); }

    public bool PlayDeath()
    {
        if (!graph.IsValid() || !deathP.IsValid() || deathClip == null) return false;
        state = State.Death;
        traveling = false;
        deathP.SetTime(0); deathP.SetSpeed(1);
        output.SetSourcePlayable(deathP);
        return true;
    }

    // ── Loop ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!graph.IsValid()) return;

        if (state == State.Death)
        {
            if (deathP.IsValid() && deathP.GetTime() >= deathClip.length) deathP.SetSpeed(0);
            return;
        }

        // Vigia a posição da CARTA
        Vector3 pos = TrackedPos();
        Vector3 delta = pos - lastPos;
        lastPos = pos;

        // A carta TELEPORTOU de casa: o boneco fica onde estava (compensa o
        // arrasto de ser filho da carta) e agenda a caminhada até ela
        if (delta.magnitude > JumpThreshold && transform.parent != null)
        {
            transform.position -= delta;      // desfaz o "puxão" deste frame
            traveling = true;
            turningBack = false;
            travelGoTime = Time.time + TravelDelay;
        }

        if (traveling) { UpdateTravel(); MaintainLoops(); return; }

        // Giro de volta suave depois de chegar da viagem
        if (turningBack)
        {
            turnBackT += Time.deltaTime / TurnBackDuration;
            transform.localRotation = Quaternion.Slerp(turnBackFrom, homeLocalRot, turnBackT);
            if (turnBackT >= 1f) turningBack = false;
        }

        // Fim do one-shot (golpe/dano) → volta ao natural
        if (state == State.OneShot)
        {
            if (oneShotUntil > 0f && Time.time >= oneShotUntil)
            {
                oneShotUntil = -1f;
                if (Time.time < walkingUntil) EnterWalk(); else EnterIdle();
            }
            MaintainLoops();
            return;
        }

        // Movimento contínuo (deslizadinhas curtas): anda enquanto durar
        if (delta.magnitude > MoveEpsilon) walkingUntil = Time.time + WalkHysteresis;
        bool moving = Time.time < walkingUntil;
        if (moving && state != State.Walk) EnterWalk();
        else if (!moving && state == State.Walk) EnterIdle();

        MaintainLoops();
    }

    // A caminhada da casa antiga até a carta
    void UpdateTravel()
    {
        Transform parent = transform.parent;
        if (parent == null) { traveling = false; return; } // carta morreu no meio

        // One-shot no meio da viagem (ex.: levou dano andando): deixa terminar
        if (state == State.OneShot)
        {
            if (oneShotUntil > 0f && Time.time >= oneShotUntil) { oneShotUntil = -1f; EnterWalk(); }
            else return;
        }

        // Esperinha na casa antiga antes de sair andando
        if (Time.time < travelGoTime)
        {
            if (state != State.Idle) EnterIdle();
            return;
        }

        Vector3 target = parent.TransformPoint(homeLocalPos);
        Vector3 to = target - transform.position;
        float dist = to.magnitude;

        if (dist <= ArriveEpsilon)
        {
            // Chegou: encaixa na posição exata e gira de volta suavemente
            transform.localPosition = homeLocalPos;
            traveling = false;
            turningBack = true;
            turnBackT = 0f;
            turnBackFrom = transform.localRotation;
            EnterIdle();
            return;
        }

        // Anda em direção à carta, virado para onde anda
        transform.position = Vector3.MoveTowards(transform.position, target, TravelWalkSpeed * Time.deltaTime);
        Vector3 flat = new Vector3(to.x, 0f, to.z);
        if (flat.sqrMagnitude > 1e-4f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), FaceTurnSpeed * Time.deltaTime);

        if (state != State.Walk) EnterWalk();
    }

    // Loops manuais dos clips (sem .meta, o loopTime do importador vem desligado)
    void MaintainLoops()
    {
        if (state == State.Walk && walkP.IsValid() && walkClip != null &&
            walkP.GetTime() >= walkClip.length)
            walkP.SetTime(walkP.GetTime() % walkClip.length);

        if (state == State.Idle && idleP.IsValid() && idleClip != null &&
            idleP.GetTime() >= idleClip.length)
            idleP.SetTime(idleP.GetTime() % idleClip.length);
    }

    Vector3 TrackedPos()
    {
        if (moveTracker != null) return moveTracker.position;
        return transform.position;
    }

    // Roda DEPOIS do Animator escrever os ossos: prende o quadril no lugar
    // enquanto o clip de caminhada toca (cancela o avanço "não In Place")
    void LateUpdate()
    {
        if (!graph.IsValid() || state == State.Death) return; // morte usa o deslocamento do clip

        if (state != State.Walk)
        {
            ResetDrift();
            return;
        }
        if (hipsBone == null || rigChild == null) return;

        // Posição do quadril no espaço da RAIZ da figura
        Vector3 local = transform.InverseTransformPoint(hipsBone.position);
        if (!driftRefCaptured)
        {
            driftRefCaptured = true;
            driftRefLocal = local;
            return;
        }

        // Recua o esqueleto pelo tanto que o clip avançou (só no plano XZ —
        // o balanço vertical do passo é mantido)
        Vector3 d = local - driftRefLocal;
        d.y = 0f;
        if (d.sqrMagnitude > 1e-10f) rigChild.localPosition -= d;
    }

    void ResetDrift()
    {
        if (driftRefCaptured && rigChild != null)
            rigChild.localPosition = rigChildBaseLocalPos;
        driftRefCaptured = false;
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
