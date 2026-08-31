using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    // Camada das cartas da LOJA: desenhadas por uma câmera overlay (stack da
    // URP) que limpa o depth — a loja nunca fica escondida atrás das
    // estruturas do cenário com a câmera deitada. Layer 30 está livre no
    // projeto (nenhum outro script usa layers).
    public const int ShopTopLayer = 30;
    private Camera shopTopCamera;

    [Header("Movimento (arrasto com botão direito)")]
    [Tooltip("1 = o tabuleiro acompanha o mouse exatamente; aumente para arrastar mais rápido")]
    public float dragSensitivity = 1.2f;

    [Header("Zoom (scroll do mouse)")]
    [Tooltip("Quanto cada 'clique' do scroll altera o zoom")]
    public float zoomStep = 2.5f;
    [Tooltip("Velocidade da suavização do zoom (maior = responde mais rápido)")]
    public float zoomSmoothness = 12f;
    public float minZoom = 5f;
    public float maxZoom = 30f;
    [Tooltip("Zoom com que a partida COMEÇA (meia-altura visível no chão). Meio termo: nem colado nas cartas, nem vendo o tabuleiro inteiro de longe.")]
    public float startZoom = 16f;

    [Header("Limites do arrasto")]
    [Tooltip("Até onde o centro da tela pode ir para os LADOS (unidades do mundo; tabuleiro no centro)")]
    public float panLimitX = 30f;
    [Tooltip("Até onde o centro da tela pode ir para FRENTE/TRÁS (as mãos ficam em ±29.5)")]
    public float panLimitZ = 36f;

    [Header("Inclinação (visão do jogador)")]
    [Tooltip("Ângulo em graus a partir do chão: 90 = de cima (só o topo da cabeça das figuras); 45 = câmera mais deitada, vendo as figuras de frente. Pode ajustar em play mode.")]
    [Range(25f, 90f)]
    public float tiltAngle = 45f;

    [Header("Perspectiva (profundidade real)")]
    [Tooltip("Abertura da lente em graus. Menor = mais 'achatada' (tele); maior = mais dramática. Pode ajustar em play mode.")]
    [Range(20f, 60f)]
    public float fieldOfView = 35f;

    [Header("Abertura (fase de compras)")]
    [Tooltip("Inclinação na fase de compras. 60° é a visão de cima da versão antiga: dá para ver o tabuleiro inteiro e as duas mãos de uma vez.")]
    [Range(25f, 90f)]
    public float openingTilt = 60f;
    [Tooltip("Zoom (meia-altura visível) da fase de compras — grande o bastante para caber tabuleiro + mãos.")]
    public float openingZoom = 28f;
    [Tooltip("Quanto o enquadramento da abertura desce no eixo Z em relação ao da partida. Negativo puxa para a mão do jogador 1, centralizando as duas mãos na tela.")]
    public float openingOffsetZ = -7f;
    [Tooltip("Duração (segundos) da descida suave quando os 2 jogadores clicam em Iniciar Partida.")]
    public float descentDuration = 2.2f;

    private static CameraController instance;

    private Vector3 lastMousePos;
    private Camera mainCamera;
    private float targetZoom;
    private float currentZoom;             // meia-altura visível (em unidades) no ponto do chão olhado
    private float appliedTilt = float.NaN; // última inclinação aplicada (NaN = nenhuma)
    private float appliedFov = float.NaN;

    // Enquadramento da PARTIDA (o de hoje), capturado da cena no Start: o ponto
    // do chão que a câmera original olha. É para cá que a descida termina.
    private Vector3 playTarget;
    private float playTilt;
    private float playZoom;
    private Coroutine descida;             // != null enquanto a câmera se move sozinha
    private bool naAbertura;               // true enquanto a fase de compras não acabou
    private int appliedViewPlayer;         // lado da mesa já aplicado (0 = nenhum)

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera != null)
        {
            // O zoom continua medido como "meia-altura visível no chão" (a antiga
            // orthographicSize) — minZoom/maxZoom/zoomStep valem como antes.
            // A partida começa no startZoom (meio termo), não no zoom máximo
            // salvo na cena, que mostrava o tabuleiro inteiro de longe.
            targetZoom = Mathf.Clamp(startZoom, minZoom, maxZoom);
            currentZoom = targetZoom;

            // PERSPECTIVA de verdade (estilo LoL). A ortográfica inclinada parecia
            // uma imagem entortada: sem "longe fica menor", o tabuleiro não tinha
            // profundidade e dava a impressão de flutuar no ar (feedback do Carlos)
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = fieldOfView;
            mainCamera.nearClipPlane = 1f;
            mainCamera.farClipPlane = 1000f;
            appliedFov = fieldOfView;

            // Enquadramento da PARTIDA: o ponto do chão que a câmera da cena
            // olha (≈ centro do tabuleiro), com o tilt e o zoom de jogo.
            // Guardado antes de qualquer coisa — é o destino da descida.
            playTilt = tiltAngle;
            playZoom = targetZoom;
            if (!GroundTarget(out playTarget)) playTarget = Vector3.zero;

            // A cena guarda a câmera a 60° (quase de cima); aqui ela deita
            // para o ângulo configurado, mirando o mesmo ponto do tabuleiro
            ApplyTilt();

            // Fase de compras: começa NA VISÃO DE CIMA (a antiga), mostrando
            // tabuleiro e mãos inteiros. Só desce quando os dois clicarem em
            // Iniciar Partida. Entrar direto no meio de uma partida (recarga
            // de cena) mantém a câmera de jogo.
            if (TurnManager.Instance == null ||
                TurnManager.Instance.gameState == GameState.Lobby)
            {
                AplicarAbertura();
            }

            // De que lado da mesa este cliente senta (jogador 2 = 180°).
            // Pode ainda não se saber aqui — o Update reconfere.
            SincronizarLadoDaMesa();

            // Câmera overlay da loja: filha da principal (acompanha posição,
            // rotação e lente), desenha SÓ a camada da loja limpando o depth
            mainCamera.cullingMask &= ~(1 << ShopTopLayer);
            GameObject overlay = new GameObject("ShopTopCamera");
            overlay.transform.SetParent(transform, false);
            shopTopCamera = overlay.AddComponent<Camera>();
            shopTopCamera.cullingMask = 1 << ShopTopLayer;
            shopTopCamera.fieldOfView = mainCamera.fieldOfView;
            shopTopCamera.nearClipPlane = mainCamera.nearClipPlane;
            shopTopCamera.farClipPlane = mainCamera.farClipPlane;
            // Os eventos de mouse da loja vêm DESTA câmera: o SendMouseEvents
            // do Unity só entrega OnMouseDown/Enter por uma câmera cujo
            // cullingMask E eventMask incluem a camada do collider — a
            // principal não vê mais a camada 30, então sem isto as cartas da
            // loja ficavam inclicáveis (bug real: não dava para comprar)
            shopTopCamera.eventMask = 1 << ShopTopLayer;
            // Desempate de clique: se loja e tabuleiro disputarem o mesmo
            // pixel, ganha quem desenha por cima (a URP ignora depth para
            // overlay, mas o SendMouseEvents respeita)
            shopTopCamera.depth = mainCamera.depth + 1;

            // URP: overlay entra no stack da câmera base
            // (clearDepth é read-only nesta versão da URP; overlay já limpa
            // o depth por padrão — é exatamente o que queremos)
            var overlayData = shopTopCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            var mainData = mainCamera.GetUniversalAdditionalCameraData();
            if (!mainData.cameraStack.Contains(shopTopCamera))
                mainData.cameraStack.Add(shopTopCamera);
        }
    }

    // Distância câmera→alvo que enquadra 'currentZoom' de meia-altura no alvo
    float DistanceForZoom()
    {
        return currentZoom / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // Ponto do chão (y=0) no centro da tela — âncora do tilt e do zoom
    bool GroundTarget(out Vector3 alvo)
    {
        Vector3 fwd = transform.forward;
        if (fwd.y > -0.01f) { alvo = Vector3.zero; return false; }
        alvo = transform.position + fwd * (transform.position.y / -fwd.y);
        return true;
    }

    // Re-aponta a câmera no ângulo configurado SEM mudar o ponto do chão que
    // ela olha — o tabuleiro continua enquadrado igual, só a inclinação muda.
    void ApplyTilt()
    {
        Vector3 alvoNoChao;
        if (!GroundTarget(out alvoNoChao)) return; // câmera não olha para baixo
        Enquadrar(alvoNoChao);
    }

    // Coloca a câmera no tilt/zoom atuais olhando exatamente para 'alvoNoChao'
    void Enquadrar(Vector3 alvoNoChao)
    {
        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
        transform.position = alvoNoChao - transform.forward * DistanceForZoom();
        appliedTilt = tiltAngle;
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  ABERTURA → PARTIDA                                               ║
    // ║  A fase de compras usa a câmera antiga (de cima, tabuleiro e as   ║
    // ║  duas mãos na tela). Quando os DOIS clicam em Iniciar Partida a   ║
    // ║  câmera desce suavemente até o enquadramento de jogo.             ║
    // ║  Puramente visual: nada aqui mexe em estado de partida, então     ║
    // ║  não precisa de RPC (o StartGame já roda nos dois clientes).      ║
    // ╚══════════════════════════════════════════════════════════════════╝
    void AplicarAbertura()
    {
        naAbertura = true;
        tiltAngle = openingTilt;
        currentZoom = targetZoom = openingZoom;
        Enquadrar(AlvoDaAbertura());
    }

    // "Para baixo na tela" é +z para o jogador 1 e -z para o jogador 2 — o
    // deslocamento da abertura acompanha o lado de quem está vendo.
    Vector3 AlvoDaAbertura()
    {
        float lado = CardDisplay.ViewFlipped ? -1f : 1f;
        return playTarget + new Vector3(0f, 0f, openingOffsetZ * lado);
    }

    // ── LADO DA MESA ─────────────────────────────────────────────────────
    // O jogador 2 senta DE FRENTE: a câmera dele gira 180° em torno do Y, e
    // as cartas giram junto (CardDisplay.RefreshViewRotation) — a imagem sai
    // idêntica à do jogador 1, com a mão dele embaixo. Não dá para fazer isso
    // só no Start: o número do jogador local chega pelo Photon depois.
    void SincronizarLadoDaMesa()
    {
        int me = CardDisplay.LocalViewPlayer;
        if (me == appliedViewPlayer) return;
        appliedViewPlayer = me;

        // O ponto que a câmera enquadra espelha junto (veio da câmera da cena,
        // que estava um pouco à frente do centro — do lado do jogador 1)
        playTarget = new Vector3(playTarget.x, 0f,
            Mathf.Abs(playTarget.z) * (me == 2 ? -1f : 1f));

        transform.rotation = Quaternion.Euler(tiltAngle, me == 2 ? 180f : 0f, 0f);
        Enquadrar(naAbertura ? AlvoDaAbertura() : playTarget);

        CardDisplay.RefreshViewRotation();
        Debug.Log($"[Camera] Vendo a mesa do lado do jogador {me}.");
    }

    // Chamado pelo TurnManager quando a partida começa de verdade
    public static void DescerParaPartida()
    {
        if (instance == null || instance.mainCamera == null) return;
        if (instance.descida != null) instance.StopCoroutine(instance.descida);
        instance.descida = instance.StartCoroutine(instance.Descer());
    }

    // Chamado no reinício (revanche): a fase de compras volta a ser vista de cima
    public static void VoltarParaAbertura()
    {
        if (instance == null || instance.mainCamera == null) return;
        if (instance.descida != null) { instance.StopCoroutine(instance.descida); instance.descida = null; }
        instance.AplicarAbertura();
    }

    IEnumerator Descer()
    {
        // A escolha da torre abre um painel que cobre a tela inteira: descer
        // atrás dele seria animação jogada fora. Espera ele sair da frente.
        while (TowerSelectUI.IsOpen) yield return null;

        Vector3 alvo0;
        if (!GroundTarget(out alvo0)) alvo0 = AlvoDaAbertura();
        Vector3 alvo1 = new Vector3(alvo0.x, 0f, playTarget.z); // mantém o lado para onde o jogador arrastou
        float tilt0 = tiltAngle, zoom0 = currentZoom;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, descentDuration);
            // SmoothStep: sai devagar, ganha corpo no meio e encosta macio no fim
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            tiltAngle = Mathf.Lerp(tilt0, playTilt, k);
            currentZoom = targetZoom = Mathf.Lerp(zoom0, playZoom, k);
            Enquadrar(Vector3.Lerp(alvo0, alvo1, k));
            yield return null;
        }

        tiltAngle = playTilt;
        currentZoom = targetZoom = playZoom;
        Enquadrar(alvo1);
        naAbertura = false;
        descida = null;
    }

    void Update()
    {
        if (mainCamera == null) return;

        // O lado da mesa só é conhecido depois do SyncPlayers do Photon
        SincronizarLadoDaMesa();

        if (Mouse.current == null) return;

        // Durante a descida a câmera é do roteiro, não do mouse: arrasto e
        // zoom aqui só brigariam com o lerp (e o Inspector idem)
        if (descida != null) return;

        // Sliders de inclinação/lente mexidos no Inspector durante o jogo: reaplica
        if (!float.IsNaN(appliedFov) && !Mathf.Approximately(appliedFov, fieldOfView))
        {
            mainCamera.fieldOfView = fieldOfView;
            if (shopTopCamera != null) shopTopCamera.fieldOfView = fieldOfView;
            appliedFov = fieldOfView;
            ApplyTilt(); // recoloca na distância certa para a lente nova
        }
        if (!float.IsNaN(appliedTilt) && !Mathf.Approximately(appliedTilt, tiltAngle))
        {
            ApplyTilt();
        }

        // ── Arrasto do mouse com botão direito ────────────────────────────
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            lastMousePos = Mouse.current.position.ReadValue();
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentMousePos = Mouse.current.position.ReadValue();
            Vector3 delta = currentMousePos - lastMousePos;

            // Converte pixels de tela em unidades de mundo no zoom atual (medido
            // no plano do tabuleiro): o tabuleiro "gruda" no mouse ao arrastar
            float worldPerPixel = (currentZoom * 2f) / mainCamera.pixelHeight;

            // Pan no PLANO DO CHÃO (altura constante). Mover no espaço local da
            // câmera inclinada abaixava a câmera ao arrastar para baixo, fazendo
            // o near plane cortar o topo das cartas.
            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forwardOnGround = transform.up;
            forwardOnGround.y = 0f;
            float foreshorten = forwardOnGround.magnitude; // compressão visual do chão (câmera inclinada)
            if (foreshorten < 0.15f)
            {
                // Câmera quase de topo: usa o forward projetado no chão
                forwardOnGround = transform.forward;
                forwardOnGround.y = 0f;
                foreshorten = Mathf.Max(forwardOnGround.magnitude, 0.15f);
            }
            forwardOnGround.Normalize();

            Vector3 move = (-delta.x * right - (delta.y / foreshorten) * forwardOnGround)
                           * worldPerPixel * dragSensitivity;
            transform.position += move;

            // Limite do arrasto: o PONTO DO CHÃO no centro da tela não pode
            // sair da área do tabuleiro+mãos (antes dava para ir ao infinito)
            Vector3 alvoAtual;
            if (GroundTarget(out alvoAtual))
            {
                Vector3 preso = new Vector3(
                    Mathf.Clamp(alvoAtual.x, -panLimitX, panLimitX), 0f,
                    Mathf.Clamp(alvoAtual.z, -panLimitZ, panLimitZ));
                if (preso != alvoAtual) transform.position += preso - alvoAtual;
            }

            lastMousePos = currentMousePos;
        }

        // ── Zoom suave ─────────────────────────────────────────────────────
        float scroll = Mouse.current.scroll.ReadValue().y;

        // Mouse sobre a UI (ex: painel de Logs): o scroll é da UI, não do zoom
        if (scroll != 0 &&
            UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            scroll = 0;
        }

        if (scroll != 0)
        {
            // Usa só a direção: em algumas plataformas o scroll vem em passos de ±120
            targetZoom = Mathf.Clamp(targetZoom - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);
        }

        if (!Mathf.Approximately(currentZoom, targetZoom))
        {
            // Em perspectiva o zoom é a câmera se APROXIMANDO/AFASTANDO do ponto
            // do chão no centro da tela (mesma sensação do zoom do LoL)
            Vector3 alvoNoChao;
            if (GroundTarget(out alvoNoChao))
            {
                currentZoom = Mathf.Lerp(
                    currentZoom,
                    targetZoom,
                    1f - Mathf.Exp(-zoomSmoothness * Time.deltaTime));
                transform.position = alvoNoChao - transform.forward * DistanceForZoom();
            }
        }
    }
}
