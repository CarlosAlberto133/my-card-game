using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class CardDisplay : MonoBehaviour
{
    [Header("Referência da Carta")]
    public Card card;

    [Header("Estado da Carta")]
    public bool isInHand = false;
    public bool isOnBoard = false;
    public bool isInShop = true; // Começa na loja (fase de compra)
    public bool isLockedInShop = false; // "Travar cartas": sobrevive ao refresh da loja (aura dourada)
    public CardTile currentTile; // Tile onde a carta está atualmente
    public int ownerPlayerNumber = 0; // 0 = sem dono (loja), 1 = Player1, 2 = Player2
    public int lastMovedRound = -1; // Em qual round a carta se moveu pela última vez (-1 = nunca)
    public int lastAttackedRound = -1; // Em qual round a carta atacou pela última vez (-1 = nunca)
    public int extraAttackUsedRound = -1; // Arqueiros: 2º ataque do turno (aura do Tank 4 2/6/8) já usado neste round
    public int archerAuraUsedRound = -1; // Tank 4 (2/6/8): round em que a aura de ataque duplo foi consumida — no round seguinte ela descansa (round sim, round não)
    public int doubleVsTankLastUsedRound = -1; // Archer 4 (5/2): round em que dobrou o dano contra Tank — só pode de novo 2 rounds depois
    public bool treeDefenseUsed = false; // Archer efeito 4: já usou o dodge de árvore
    public bool treeDefenseActive = false; // Archer efeito 4: efeito está ativo NESTE turno
    public bool treeDefensePopupShown = false; // Rastreia se o popup já foi mostrado neste turno
    public bool healOnEnterUsed = false; // Healer 2: rastreia se já usou o efeito ao entrar em campo
    public bool isFrozen = false; // Mage 3: carta está congelada, não pode se mover/atacar/ativar efeito
    public bool isStunned = false; // Archer tier-2: carta está stunada, não pode se mover/atacar
    public bool archerShieldArrowUsed = false; // Archer 2 (ATK 3, HP 3): efeito de parar ataque já foi usado
    public bool archerStunOnHitUsed = false; // Archer 2 (ATK 2, HP 2): efeito de stun ao receber ataque já foi usado
    public bool archerComboActivated = false; // Archer tier-2 combo: +5 ATK já foi ativado
    public int maxHealthBonus = 0; // Healer 2 (ATK 1, HP 3): bônus de vida máxima
    public bool healerComboActivated = false; // Healer tier-2 combo: restauração de ouro/vida já foi ativada
    public bool eagleMarked = false; // Archer 3 (ATK 3, HP 3): marcado pela águia, não pode atacar
    public int moveCountThisRound = 0; // Archer 3 (ATK 3, HP 2): contador de movimentações neste turno (máx 2 se tem Mago)
    public int lastMoveCountRound = -1; // Archer 3 (ATK 3, HP 2): último round em que moveu (para resetar contador)
    public bool archerTier3Effect1Used = false; // Archer 3 (ATK 3, HP 3): águia já foi invocada nesta partida
    public bool archerTier3Effect2Used = false; // Archer 3 (ATK 4, HP 2): cópia já foi feita nesta partida
    public bool archerTier3Effect3Used = false; // Archer 3 (ATK 3, HP 2): dano à torre já foi feito nesta partida
    public bool archerTier3Effect4Used = false; // Archer 3 (ATK 4, HP 3): dano em cruz já foi feito nesta partida
    public bool healerTier3Effect1Used = false; // Healer 3 (ATK 1, HP 4): ouro com Mago já foi ganho nesta partida
    public bool healerTier3Effect3Used = false; // Healer 3 (ATK 1, HP 3): ouro por contagem já foi ganho nesta partida
    public bool healerTier3Effect4Used = false; // Healer 3 (ATK 2, HP 3): ouro por Mago já foi ganho nesta partida
    public bool mageTier3Effect1Used = false; // Mage 3 (ATK 1, HP 3): roubo de status já foi feito nesta partida
    public bool mageTier3Effect2Used = false; // Mage 3 (ATK 3, HP 5): +1 ATK para todos já foi feito nesta partida
    public bool mageTier3Effect4Used = false; // Mage 3 (ATK 2, HP 5): +1 ATK na mão já foi feito nesta partida
    public bool tankTier3Effect1Used = false; // Tank 3 (ATK 2, Shield 3, HP 6): +2 armadura Healers já foi feito nesta partida
    public bool tankTier3Effect3Used = false; // Tank 3 (ATK 2, Shield 4, HP 5): +2 armadura por Tank já foi feito nesta partida
    public bool tankTier3Effect4Used = false; // Tank 3 (ATK 2, Shield 4, HP 6): +3 armadura Mago já foi feito nesta partida
    public int archerTier4Effect2LastUsedRound = -3; // Archer 4 (ATK 4, HP 2): último round que usou stun (para reutilizar a cada 2 turnos)
    public int archerTier4Effect4LastAttackRound = -1; // Archer 4 (ATK 4, HP 3): último round que atacou alvo ao lado
    public int healerTier4Effect1LastCureRound = -3; // Healer 4 (ATK 2, HP 5): último round que curou (a cada 2 turnos)
    public bool healerTier4Effect3Used = false; // Healer 4 (ATK 1, HP 4): invunerabilidade já foi dada nesta partida
    public bool healerTier4Effect4Used = false; // Healer 4 (ATK 3, HP 4): +3 todos status já foi ativado nesta partida
    public bool mageTier4Effect1Used = false; // Mage 4 (ATK 4, HP 5): remover bônus já foi feito nesta partida
    public int mageTier4Effect1UsesLeft = 1; // Mage 4 (ATK 4, HP 5): pode usar 2 vezes se tem Healer + Arqueiro
    public bool mageTier4Effect3Used = false; // Mage 4 (ATK 3, HP 5): destruir inimigo já foi feito nesta partida
    public bool tankTier4Effect1Used = false; // Tank 4 (ATK 2, Shield 5, HP 6): +5 HP +2 Shield já foi feito nesta partida
    public int tankTier4Effect2LastUsedRound = -1; // Tank 4 (ATK 2, Shield 6, HP 6): último round que recebeu ataque (uma vez por turno)
    public bool tankTier4Effect3Used = false; // Tank 4 (ATK 2, Shield 5, HP 7): Arqueiros x2 ataque já foi ativado nesta partida
    public bool archerTier5Effect2Used = false; // Archer 5 (ATK 6, HP 4): remover armadura inimigos já foi feito nesta partida
    public bool healerTier5Effect3Used = false; // Healer 5 (ATK 3, HP 7): duplicar stats de aliado já foi feito nesta partida
    public int mageTier5Effect1LastUsedRound = -1; // Mage 5 (ATK 5, HP 5): último round que congelou inimigos (uma vez por round)
    public bool mageTier5Effect2Used = false; // Mage 5 (ATK 4, HP 6): copiar stats de inimigo já foi feito nesta partida
    public int tankTier5Effect2LastArmorRound = -3; // Tank 5 (ATK 3, Shield 6, HP 9): último round que concedeu armadura (a cada 2 turnos)

    // Stats atuais (mudam durante o jogo)
    public int currentHealth;
    public int currentShield;

    // Nasceu de EFEITO (cópia, eco, invocação) e não da mão do jogador.
    // A Devoção de Classe só conta cartas de verdade — cópias não contam
    public bool isEffectSpawn = false;

    // ── Tetos anti-tartaruga (v4.2): crescimento permanente TEM limite ──
    // Sem teto, defesa acumulava sem fim e as partidas travavam (feedback
    // do Carlos: "jogo parado e congestionado")
    public int garrisonShieldGrants = 0;   // Guarnição da torre: máx. 4 por unidade
    public int matriarcaMaxHpGrants = 0;   // Matriarca (+1 vida máx.): máx. 3 por unidade
    public int canalizadorAtkGrants = 0;   // Canalizador (+1 ATK por cura aliada): máx. 5 (v4.3)
    public int currentAttack;

    // Escalas padrão por zona (o tabuleiro tem tiles 6x6, cartas pequenas ficavam ilegíveis).
    // Loja/mão maiores a pedido do Carlos — crescem PARA CIMA (base fixa em GroundY)
    public const float HandScale = 2.4f;
    public const float BoardScale = 2f;

    // No TABULEIRO a carta fica DEITADA sobre o tile, centralizada
    public const float BoardYOffset = 0.75f; // Um pouco acima do topo do tile (evita z-fighting)
    public static readonly Quaternion BoardRotation = Quaternion.Euler(0f, 180f, 0f); // Deitada, face para cima

    // Na LOJA e na MÃO a carta fica "em pé" (de frente para a câmera): o centro
    // precisa subir metade do comprimento dela (1.25 × escala) + folga,
    // senão a base afunda no chão
    public static float GroundY(float scale)
    {
        return 1.25f * scale + 0.6f;
    }

    private HandManager handManager;
    private Vector3 preHoverScale = Vector3.zero;    // Escala antes do hover (restaurada ao sair)
    private Vector3 preHoverPosition = Vector3.zero; // Posição antes do hover (cartas da loja sobem)
    private bool hoverLifted = false;
    private int hoverZone = 0; // Onde o hover começou: 0 = nenhum, 1 = loja, 2 = tabuleiro
    private bool isMouseOver = false; // Flag para saber se o mouse está sobre a carta

    // Verso da carta: na mão do OPONENTE você não vê a frente, só o verso
    public bool isFaceDown = false;
    private GameObject backCover; // Encarte opaco criado sob demanda sobre a frente

    // ===== Durações de status e contadores de efeito (só mudam dentro de RPCs) =====
    // Congelamento/atordoamento contam fins de turno DO DONO da carta (a carta
    // perde exatamente 1 turno dela); águia conta fins de turno globais.
    public int freezeTurnsLeft = 0;
    public int stunTurnsLeft = 0;
    public int eagleTurnsLeft = 0;
    public int invulnerableRoundsLeft = 0; // Healer 4 (ATK 1, HP 4): invulnerável por 3 ROUNDS

    // Contador visível acima da carta (amarelo = turnos, rosa = rounds).
    // effectPeriod > 0: ao chegar em 0 o efeito dispara e o contador renova.
    // effectPeriod == 0: é um cooldown — ao chegar em 0 só esconde (pronto para usar).
    public int effectCounter = -1; // -1 = sem contador visível
    public int effectPeriod = 0;
    public bool effectCounterIsRound = false;

    [Header("UI Elements (Assign in Inspector)")]
    public TextMeshPro cardNameText;
    public TextMeshPro attackText;
    public TextMeshPro shieldText;
    public TextMeshPro healthText;
    public TextMeshPro tierText;
    public TextMeshPro effectText;
    public TextMeshPro classText;
    public Renderer artworkRenderer;
    public Renderer backgroundRenderer;
    public Renderer tierBarRenderer;

    [Header("Cores por Classe")]
    public Color tankColor = new Color(0.7f, 0.7f, 0.7f); // Cinza
    public Color magoColor = new Color(0.5f, 0.3f, 0.9f); // Roxo
    public Color healerColor = new Color(0.3f, 0.9f, 0.5f); // Verde
    public Color arqueiroColor = new Color(0.9f, 0.6f, 0.3f); // Laranja

    void Awake()
    {
        // (escala por zona é aplicada por CardManager/HandManager/GameManager)
    }

    void Start()
    {
        // Auto-atribui elementos se não foram setados manualmente
        AutoAssignElements();

        if (card != null)
        {
            UpdateCardDisplay();
        }

        // Encontra o HandManager. NÃO sobrescreve se já foi definido: cartas da
        // loja oculta do oponente são compradas ANTES do Start rodar (o objeto
        // nasce desativado), e ExecuteBuy já apontou para a mão correta.
        if (handManager == null)
        {
            handManager = FindObjectOfType<HandManager>();
        }
    }

    void Update()
    {
        if (!isMouseOver) return;

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        // Botão direito em qualquer carta legível: abre a inspeção ampliada
        // (efeito, foto e stats). O antigo atalho de ataque do botão direito
        // agora vive só na tecla A — o ataque normal (selecionar e clicar no
        // inimigo) continua igual.
        if (mouse != null && mouse.rightButton.wasPressedThisFrame &&
            !isFaceDown && card != null && GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowCardPreview(this);
        }

        // Tecla A: ataque rápido no inimigo adjacente (carta sua, no tabuleiro)
        if (isOnBoard && TurnManager.Instance != null &&
            keyboard != null && keyboard.aKey.wasPressedThisFrame)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;
            if (ownerPlayerNumber == currentPlayerNumber)
            {
                // Decisão de efeito pendente: nenhuma ação nova até resolver
                if (GameManager.IsDecisionPending())
                {
                    Debug.Log("[CardDisplay] Aguarde a decisão de efeito ser resolvida!");
                    return;
                }

                // Em multiplayer, só ataca no SEU turno e via RPC (executa nos dois clientes)
                if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
                {
                    if (currentPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
                    {
                        Debug.Log("[CardDisplay] Não é seu turno, não pode atacar!");
                    }
                    else if (currentTile != null)
                    {
                        PhotonGameManager.Instance.SendAttackRPC(currentTile.row, currentTile.column);
                    }
                }
                else
                {
                    AttackAdjacentEnemy();
                }
            }
        }
    }

    // Verifica se a carta pode se mover neste round
    public bool CanMoveThisRound()
    {
        // Se está congelada, não pode se mover
        if (isFrozen)
        {
            Debug.Log($"{card.cardName} está congelada, não pode se mover!");
            return false;
        }

        // Se está stunada, não pode se mover
        if (isStunned)
        {
            Debug.Log($"{card.cardName} está stunada, não pode se mover!");
            return false;
        }

        if (TurnManager.Instance == null) return true;

        // Verifica se o contador de movimentações foi resetado para este round
        if (lastMoveCountRound < TurnManager.Instance.currentRound)
        {
            moveCountThisRound = 0;
            lastMoveCountRound = TurnManager.Instance.currentRound;
        }

        // Se nunca se moveu ou se moveu em um round anterior, pode mover
        if (lastMovedRound < TurnManager.Instance.currentRound) return true;

        // Archer 3 (3/2, tier 3) com Mago aliado: pode se mover 2 vezes por
        // round. (O mecanismo nunca tinha sido ligado — moveCountThisRound
        // existia mas ninguém incrementava/consultava; a carta só movia 1x)
        if (card != null && card.cardClass == CardClass.Arqueiro &&
            card.tier == CardTier.Tier3 && card.attack == 3 && card.health == 2 &&
            moveCountThisRound < 2 &&
            BoardManager.Instance != null &&
            BoardManager.Instance.HasClassOnBoard(ownerPlayerNumber, CardClass.Mago))
            return true;

        return false;
    }

    // Marca a movimentação desta carta no round atual (conta o 2º movimento
    // do Archer 3 [3/2] com Mago)
    public void MarkMoveUsed()
    {
        if (TurnManager.Instance == null) return;
        int round = TurnManager.Instance.currentRound;
        if (lastMoveCountRound < round)
        {
            moveCountThisRound = 0;
            lastMoveCountRound = round;
        }
        moveCountThisRound++;
        lastMovedRound = round;
    }

    // Verifica se a carta pode atacar neste round
    public bool CanAttackThisRound()
    {
        // Se está com marca de águia, não pode atacar
        if (eagleMarked)
        {
            Debug.Log($"{card.cardName} está marcado pela águia, não pode atacar!");
            return false;
        }

        // Se está congelada, não pode atacar
        if (isFrozen)
        {
            Debug.Log($"{card.cardName} está congelada, não pode atacar!");
            return false;
        }

        // Se está stunada, não pode atacar
        if (isStunned)
        {
            Debug.Log($"{card.cardName} está stunada, não pode atacar!");
            return false;
        }

        if (TurnManager.Instance == null) return true;
        if (!isOnBoard) return false;
        if (lastAttackedRound < TurnManager.Instance.currentRound) return true;

        // Tank 4 (2/6/8): com as 4 classes em campo, Arqueiros do dono podem
        // atacar uma SEGUNDA vez no mesmo turno (aura contínua, avaliada aqui
        // a cada ataque — antes era um one-shot na entrada do Tank que só
        // resetava quem já tinha atacado, por isso nunca funcionava)
        if (card != null && card.cardClass == CardClass.Arqueiro &&
            extraAttackUsedRound < TurnManager.Instance.currentRound &&
            HasArcherDoubleAttackAura())
            return true;

        return false;
    }

    // ── Helpers de POSIÇÃO dos efeitos de Tank (balanceamento de efeitos) ──
    // Tanks que ficavam parados na retaguarda "trabalhando de longe" agora
    // precisam se posicionar: guarda exige estar COLADO no protegido e as
    // auras exigem o tank na LINHA DE FRENTE (fora das 2 fileiras de trás).

    // Colado = até 1 casa de distância (as 8 casas em volta contam)
    public static bool IsNextTo(CardDisplay a, CardDisplay b)
    {
        if (a == null || b == null || a.currentTile == null || b.currentTile == null) return false;
        return Mathf.Abs(a.currentTile.row - b.currentTile.row) <= 1 &&
               Mathf.Abs(a.currentTile.column - b.currentTile.column) <= 1;
    }

    // Guarda-costas protege quem está DO LADO ou ATRÁS dele (na direção de
    // marcha do dono: P1 avança das fileiras baixas para as altas, P2 o
    // contrário). Aliado que já passou NA FRENTE do tank fica sem escolta.
    public static bool IsBesideOrBehind(CardDisplay tank, CardDisplay ally)
    {
        if (tank == null || ally == null || tank.currentTile == null || ally.currentTile == null) return false;
        return tank.ownerPlayerNumber == 1
            ? ally.currentTile.row <= tank.currentTile.row
            : ally.currentTile.row >= tank.currentTile.row;
    }

    // Linha de frente = fora das 2 fileiras de casa do dono (P1 marcha das
    // fileiras baixas para as altas; P2 o contrário — ver TryAttackTower)
    public static bool IsOnFrontLines(CardDisplay c)
    {
        if (c == null || c.currentTile == null || BoardManager.Instance == null) return false;
        int rows = BoardManager.Instance.rows;
        return c.ownerPlayerNumber == 1
            ? c.currentTile.row >= 2
            : c.currentTile.row <= rows - 3;
    }

    // Aura do Tank 4 (ATK 2, Shield 6, HP 8): verdadeiro se o dono tem um
    // desses tanks NA LINHA DE FRENTE e as 4 classes em campo (o próprio tank
    // conta como Tank). O estandarte precisa marchar na frente do exército —
    // um Tank 4 parado na retaguarda não inspira ninguém.
    // COOLDOWN: depois de um round em que o ataque duplo foi usado, a aura
    // descansa no round seguinte e volta no outro (round sim, round não).
    bool HasArcherDoubleAttackAura()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || ownerPlayerNumber == 0) return false;

        int round = TurnManager.Instance != null ? TurnManager.Instance.currentRound : -1;
        bool hasTank4 = false, hasArcher = false, hasMage = false, hasHealer = false, hasTank = false;
        foreach (var c in board.GetCardsByOwner(ownerPlayerNumber))
        {
            if (c == null || c.card == null) continue;
            if (c.card.cardClass == CardClass.Arqueiro) hasArcher = true;
            else if (c.card.cardClass == CardClass.Mago) hasMage = true;
            else if (c.card.cardClass == CardClass.Healer) hasHealer = true;
            else if (c.card.cardClass == CardClass.Tank)
            {
                hasTank = true;
                if (c.card.attack == 2 && c.card.shield == 6 && c.card.health == 8 &&
                    c.card.tier == CardTier.Tier4 && IsOnFrontLines(c) &&
                    !(c.archerAuraUsedRound >= 0 && round == c.archerAuraUsedRound + 1))
                    hasTank4 = true;
            }
        }
        return hasTank4 && hasArcher && hasMage && hasHealer && hasTank;
    }

    // "Enjoo de invocação" (cópia do Archer 4 ao matar): a carta recém-criada
    // pode ANDAR, mas só ataca quando a vez voltar para o dono no próximo
    // round. Consome também o ataque extra da aura do Tank 4 (senão a aura
    // driblava o bloqueio). Corta o snowball de limpar o tabuleiro num turno.
    public void BlockAttackThisRound()
    {
        if (TurnManager.Instance == null) return;
        int round = TurnManager.Instance.currentRound;
        lastAttackedRound = round;
        extraAttackUsedRound = round;
    }

    // Marca o ataque desta carta no round atual. Se já tinha atacado (2º
    // ataque via aura do Tank 4), consome o ataque extra e põe a aura em
    // recarga: ela descansa no round seguinte (round sim, round não).
    public void MarkAttackUsed()
    {
        if (TurnManager.Instance == null) return;
        int round = TurnManager.Instance.currentRound;
        if (lastAttackedRound == round)
        {
            extraAttackUsedRound = round;
            MarkArcherAuraUsed(round);
        }
        else lastAttackedRound = round;
    }

    // O 2º ataque de um Arqueiro só existe pela aura do Tank 4 (2/6/8) — ao
    // ser consumido, TODOS os Tank 4 (2/6/8) do dono entram em recarga
    void MarkArcherAuraUsed(int round)
    {
        if (card == null || card.cardClass != CardClass.Arqueiro) return;
        BoardManager board = BoardManager.Instance;
        if (board == null) return;
        foreach (var c in board.GetCardsByOwner(ownerPlayerNumber))
        {
            if (c != null && c.card != null && c.card.cardClass == CardClass.Tank &&
                c.card.attack == 2 && c.card.shield == 6 && c.card.health == 8 &&
                c.card.tier == CardTier.Tier4)
                c.archerAuraUsedRound = round;
        }
    }

    // Versões "silenciosas" (sem Debug.Log nem efeitos colaterais) de
    // CanMoveThisRound/CanAttackThisRound — usadas pelas bolinhas de ação
    // (CardActionDots), que consultam o estado a cada frame.
    public bool CanMovePeek()
    {
        if (isFrozen || isStunned) return false;
        if (TurnManager.Instance == null) return true;
        int round = TurnManager.Instance.currentRound;

        if (lastMovedRound < round) return true;

        // Archer 3 (3/2) com Mago aliado: 2º movimento no mesmo round
        int moves = (lastMoveCountRound < round) ? 0 : moveCountThisRound;
        if (card != null && card.cardClass == CardClass.Arqueiro &&
            card.tier == CardTier.Tier3 && card.attack == 3 && card.health == 2 &&
            moves < 2 && BoardManager.Instance != null &&
            BoardManager.Instance.HasClassOnBoard(ownerPlayerNumber, CardClass.Mago))
            return true;

        return false;
    }

    public bool CanAttackPeek()
    {
        if (eagleMarked || isFrozen || isStunned) return false;
        if (TurnManager.Instance == null) return true;
        if (!isOnBoard) return false;
        int round = TurnManager.Instance.currentRound;

        if (lastAttackedRound < round) return true;

        // Aura do Tank 4 (2/6/8): 2º ataque dos Arqueiros com as 4 classes
        if (card != null && card.cardClass == CardClass.Arqueiro &&
            extraAttackUsedRound < round && HasArcherDoubleAttackAura())
            return true;

        return false;
    }

    void AutoAssignElements()
    {
        // Encontra automaticamente os elementos filhos por nome
        if (cardNameText == null)
        {
            Transform nameTransform = transform.Find("CardNameText");
            if (nameTransform != null) cardNameText = nameTransform.GetComponent<TextMeshPro>();
        }

        if (attackText == null)
        {
            Transform attackTransform = transform.Find("AttackText");
            if (attackTransform != null) attackText = attackTransform.GetComponent<TextMeshPro>();
        }

        if (shieldText == null)
        {
            Transform shieldTransform = transform.Find("ShieldText");
            if (shieldTransform != null) shieldText = shieldTransform.GetComponent<TextMeshPro>();
        }

        if (healthText == null)
        {
            Transform healthTransform = transform.Find("HealthText");
            if (healthTransform != null) healthText = healthTransform.GetComponent<TextMeshPro>();
        }

        if (tierText == null)
        {
            Transform tierTransform = transform.Find("TierText");
            if (tierTransform != null) tierText = tierTransform.GetComponent<TextMeshPro>();
        }

        if (artworkRenderer == null)
        {
            Transform artworkTransform = transform.Find("Artwork");
            if (artworkTransform != null) artworkRenderer = artworkTransform.GetComponent<Renderer>();
        }

        if (backgroundRenderer == null)
        {
            Transform bgTransform = transform.Find("Background");
            if (bgTransform != null) backgroundRenderer = bgTransform.GetComponent<Renderer>();
        }

        if (effectText == null)
        {
            Transform t = transform.Find("EffectText");
            if (t != null) effectText = t.GetComponent<TextMeshPro>();
        }

        if (classText == null)
        {
            Transform t = transform.Find("ClassText");
            if (t != null) classText = t.GetComponent<TextMeshPro>();
        }

        if (tierBarRenderer == null)
        {
            Transform t = transform.Find("TierBar");
            if (t != null) tierBarRenderer = t.GetComponent<Renderer>();
        }
    }

    public void SetCard(Card newCard)
    {
        card = newCard;

        // Inicializa stats atuais com os valores base da carta
        if (card != null)
        {
            currentHealth = card.health;
            currentShield = card.shield;
            currentAttack = card.attack;
        }

        // Garante que os campos estão atribuídos mesmo se Start() ainda não rodou
        AutoAssignElements();
        UpdateCardDisplay();
    }

    public void ApplyCardEffect(string effectType = "")
    {
        if (card == null) return;

        CardEffectSimple effect = GetComponent<CardEffectSimple>();
        if (effect == null) return;

        if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier5)
            effect.ArcherTier5Effect();
        else if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier4)
            effect.ArcherTier4Effect();
        else if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier3)
            effect.ArcherTier3Effect();
        else if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier2)
            effect.ArcherTier2Effect();
        else if (card.cardClass == CardClass.Arqueiro)
            effect.ArcherEffect();
        else if (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier5)
            effect.HealerTier5Effect();
        else if (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier4)
            effect.HealerTier4Effect();
        else if (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier3)
            effect.HealerTier3Effect();
        else if (card.cardClass == CardClass.Healer && card.tier == CardTier.Tier2)
            effect.HealerTier2Effect();
        else if (card.cardClass == CardClass.Healer)
            effect.HealerEffect();
        else if (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier5)
            effect.MageTier5Effect();
        else if (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier4)
            effect.MageTier4Effect();
        else if (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier3)
            effect.MageTier3Effect();
        else if (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier2)
            effect.MageTier2Effect();
        else if (card.cardClass == CardClass.Mago)
            effect.MageEffect();
        else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier5)
            effect.TankTier5Effect();
        else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier4)
            effect.TankTier4Effect();
        else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier3)
            effect.TankTier3Effect();
        else if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier2)
            effect.TankTier2Effect();
        else if (card.cardClass == CardClass.Tank)
            effect.TankEffect();

        // Liga o contador visível dos efeitos periódicos desta carta
        // (amarelo = turnos, rosa = rounds; ticado pelo TurnManager)
        if (isOnBoard)
        {
            effect.SetupPeriodicCounter();
        }

        // (Canalizador — Mago T4 4/6 — v4.3: o gancho "+1 ATK quando healer
        // entra" virou "+1 ATK quando um aliado é CURADO", dentro do Heal())
    }

    void ApplyMageEffect()
    {
        // Quando um Healer toma dano, os Magos 1 (2/3) aliados dão +1/+2 ATK a ele.
        // IMPORTANTE: só o Mago 1 tem esse efeito — sem o filtro de stats/tier,
        // TODO mago aliado disparava o bônus (3 magos + tank = +6 ATK por dano)
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var alliedMages = board.GetCardsByOwner(ownerPlayerNumber)
            .FindAll(c => c.card.cardClass == CardClass.Mago &&
                          c.card.tier == CardTier.Tier1 &&
                          c.card.attack == 2 && c.card.health == 3);

        foreach (var mage in alliedMages)
        {
            if (!DuplicateEffectGate.TryActivate(mage)) continue;
            CardEffectSimple effect = mage.GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                effect.ApplyMageEffectOnHealerDamage(this);
            }
        }

        Debug.Log($"[MageEffect Trigger] {card.cardName} tomou dano - {alliedMages.Count} mago(s) aliado(s) ativou(aram) efeito");
    }

    public void UpdateDisplay()
    {
        UpdateCardDisplay();
    }

    void UpdateCardDisplay()
    {
        if (card == null) return;

        // Redesenha o layout (cantos arredondados, chips de stats, medalhão de
        // tier, respiro entre as zonas) — roda uma única vez por instância
        EnsureModernLayout();

        // Atualiza textos usando stats atuais (não os base)
        if (cardNameText != null) cardNameText.text = card.cardName;
        // Sem zero à esquerda e sem quebra de linha: a caixa é estreita e o formato
        // "00" partia o número em duas linhas (0 em cima, dígito real em baixo),
        // dando a ilusão de duas filas de stats.
        if (attackText != null)
        {
            attackText.textWrappingMode = TextWrappingModes.NoWrap;
            attackText.text = currentAttack.ToString();
        }
        if (shieldText != null)
        {
            shieldText.textWrappingMode = TextWrappingModes.NoWrap;
            shieldText.text = currentShield.ToString();
        }
        if (healthText != null)
        {
            healthText.textWrappingMode = TextWrappingModes.NoWrap;
            healthText.text = currentHealth.ToString();
        }
        if (tierText != null) tierText.text = ((int)card.tier).ToString();

        // Atualiza artwork
        if (artworkRenderer != null && card.artwork != null)
        {
            // Fallback final: shader do material atual do quad (pode ser nulo no Build)
            Shader artShader = Shader.Find("Unlit/Texture")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Standard")
                            ?? (artworkRenderer.sharedMaterial != null ? artworkRenderer.sharedMaterial.shader : null);
            if (artShader != null)
            {
                Material artworkMat = new Material(artShader);
                artworkMat.mainTexture = card.artwork.texture;
                artworkMat.SetTexture("_BaseMap", card.artwork.texture); // URP
                // O quad do Artwork já é criado com rotação Euler(90,0,180) que orienta a
                // imagem corretamente. Não invertemos o V aqui para não duplicar a inversão
                // (isso deixava a foto de cabeça para baixo).
                artworkMat.mainTextureScale = new Vector2(1, 1);
                artworkMat.mainTextureOffset = new Vector2(0, 0);
                artworkMat.SetTextureScale("_BaseMap", new Vector2(1, 1));   // URP
                artworkMat.SetTextureOffset("_BaseMap", new Vector2(0, 0));  // URP
                artworkRenderer.material = artworkMat;
            }
        }

        // Fundo: gradiente escuro tingido pela cor da classe (azul-gelo se congelada)
        if (backgroundRenderer != null)
        {
            EnsureQuadMaterial(backgroundRenderer);
            Texture2D gradient = GetClassGradient(card.cardClass);
            backgroundRenderer.material.mainTexture = gradient;
            backgroundRenderer.material.SetTexture("_BaseMap", gradient);
            Color tint = isFrozen ? new Color(0.55f, 0.75f, 1.00f) : Color.white;
            backgroundRenderer.material.color = tint;
            backgroundRenderer.material.SetColor("_BaseColor", tint);
        }

        // Atualiza texto de efeito
        if (effectText != null)
        {
            effectText.text = string.IsNullOrEmpty(card.effectDescription) ? "Sem efeito" : card.effectDescription;
        }

        // Atualiza texto de classe
        if (classText != null)
        {
            classText.text = card.cardClass.ToString();
        }

        // Atualiza cor da barra de tier
        if (tierBarRenderer != null)
        {
            EnsureQuadMaterial(tierBarRenderer);
            Color tierColor = GetTierColor(card.tier);
            tierBarRenderer.material.color = tierColor;
            tierBarRenderer.material.SetColor("_BaseColor", tierColor);
        }

        // Painéis tingidos pela cor da classe (identidade visual da carta)
        Color classTint = GetClassColor(card.cardClass);
        Color deepBase = new Color(0.10f, 0.10f, 0.16f);
        SetQuadColor("NameHeader", Color.Lerp(deepBase, classTint, 0.32f));
        SetQuadColor("ArtworkFrame", Color.Lerp(deepBase, classTint, 0.55f));
        SetQuadColor("ClassChip", Color.Lerp(deepBase, classTint, 0.45f));

        // Aplica as cores dos quads estáticos (corrige shaders URP em runtime)
        ApplyCardTheme();

        // Overlays de status (congelada/atordoada/marcada) e flash de buff/dano
        UpdateStatusVisuals();

        // Figura 3D da classe em pé sobre a carta (só no tabuleiro)
        UpdateBoardFigure();
    }

    // Rastreia os últimos stats exibidos para detectar mudanças automaticamente:
    // qualquer aumento pisca verde, qualquer redução pisca vermelho — vale para
    // TODOS os efeitos sem precisar ligar um por um
    private int lastShownAttack;
    private int lastShownShield;
    private int lastShownHealth;
    private bool statsTracked = false;

    private CardActionDots actionDots;
    private CardAuraIndicator auraIndicator;

    void UpdateStatusVisuals()
    {
        CardStatusVisuals visuals = GetComponent<CardStatusVisuals>();
        if (visuals == null) visuals = gameObject.AddComponent<CardStatusVisuals>();

        // Bolinhas de ação (andar/atacar) na base — só para cartas no tabuleiro
        // (as da loja/mão não agem). Criadas 1x; se atualizam sozinhas por frame.
        if (isOnBoard && actionDots == null)
        {
            actionDots = gameObject.AddComponent<CardActionDots>();
            actionDots.Init(this);
        }

        // Orbe de AURA no topo — só para cartas com efeito condicional/proteção
        // (verde = ativa, amarelo = descansando, vermelho = condição não cumprida)
        if (isOnBoard && auraIndicator == null && CardAuraIndicator.KindOf(this) != CardAuraIndicator.Kind.None)
        {
            auraIndicator = gameObject.AddComponent<CardAuraIndicator>();
            auraIndicator.Init(this);
        }

        visuals.SetFrozen(isFrozen);
        visuals.SetStunned(isStunned);
        visuals.SetEagleMark(eagleMarked);
        visuals.SetInvulnerable(invulnerableRoundsLeft > 0);
        visuals.SetTreeDefense(treeDefenseActive);

        // Número acima da carta: duração de status tem prioridade sobre o
        // contador de efeito periódico. Amarelo = turnos, rosa = rounds.
        int counterShown = -1;
        bool counterIsRound = false;
        if (isFrozen && freezeTurnsLeft > 0) { counterShown = freezeTurnsLeft; }
        else if (isStunned && stunTurnsLeft > 0) { counterShown = stunTurnsLeft; }
        else if (eagleMarked && eagleTurnsLeft > 0) { counterShown = eagleTurnsLeft; }
        else if (invulnerableRoundsLeft > 0) { counterShown = invulnerableRoundsLeft; counterIsRound = true; }
        else if (isOnBoard && effectCounter > 0) { counterShown = effectCounter; counterIsRound = effectCounterIsRound; }

        if (counterShown > 0) visuals.SetEffectCounter(counterShown, counterIsRound);
        else visuals.HideEffectCounter();

        if (statsTracked)
        {
            bool increased = currentAttack > lastShownAttack ||
                             currentShield > lastShownShield ||
                             currentHealth > lastShownHealth;
            bool decreased = currentAttack < lastShownAttack ||
                             currentShield < lastShownShield ||
                             currentHealth < lastShownHealth;

            if (increased)
            {
                visuals.FlashBuff();
                CardAnimator.Get(gameObject).Hop();
                bool healed = currentHealth > lastShownHealth;
                SoundManager.Play(healed ? SoundManager.Sound.Heal : SoundManager.Sound.Buff);
            }
            if (decreased)
            {
                visuals.FlashDamage();
                CardAnimator.Get(gameObject).Shake();
                SoundManager.Play(SoundManager.Sound.Hit);

                // Figura com rig: reação de "levar dano" (se houver clip react)
                bool tookDamage = currentHealth < lastShownHealth || currentShield < lastShownShield;
                if (tookDamage && boardFigure != null)
                {
                    FigureRiggedAnimator ra = boardFigure.GetComponent<FigureRiggedAnimator>();
                    if (ra != null) ra.Hit();
                }
            }

            // Números flutuantes sobre a carta: -N dano, +N HP cura, ±N ATK/DEF.
            // Como pega a DIFERENÇA dos stats exibidos, cobre todos os efeitos
            // automaticamente, sem ligar um por um
            if (isOnBoard)
            {
                int dHp = currentHealth - lastShownHealth;
                int dAtk = currentAttack - lastShownAttack;
                int dDef = currentShield - lastShownShield;

                if (dHp < 0)
                    FloatingTextFX.ShowAboveCard(this, dHp.ToString(), FloatingTextFX.DamageColor, 6.5f);
                else if (dHp > 0)
                    FloatingTextFX.ShowAboveCard(this, $"+{dHp} HP", FloatingTextFX.HealColor);

                if (dAtk != 0)
                    FloatingTextFX.ShowAboveCard(this, $"{(dAtk > 0 ? "+" : "")}{dAtk} ATK", FloatingTextFX.AttackColor);

                if (dDef != 0)
                    FloatingTextFX.ShowAboveCard(this, $"{(dDef > 0 ? "+" : "")}{dDef} DEF", FloatingTextFX.ShieldColor);
            }
        }

        lastShownAttack = currentAttack;
        lastShownShield = currentShield;
        lastShownHealth = currentHealth;
        statsTracked = true;
    }

    // Define a cor dos quads estáticos que não mudam por carta
    void ApplyCardTheme()
    {
        // Borda colorida pelo dono: azul = Jogador 1, vermelho = Jogador 2, ardósia = loja
        // (o quase-preto antigo sumia contra o fundo espacial escuro).
        // Carta TRAVADA na loja: borda dourada (a "aura" de trancada)
        Color borderColor;
        if (isInShop && isLockedInShop) borderColor = new Color(0.96f, 0.77f, 0.32f);
        else if (ownerPlayerNumber == 1) borderColor = new Color(0.15f, 0.40f, 1.00f);
        else if (ownerPlayerNumber == 2) borderColor = new Color(0.95f, 0.25f, 0.20f);
        else borderColor = new Color(0.30f, 0.29f, 0.42f);
        SetQuadColor("Border", borderColor);
        // Caixa de efeito neutra escura; stats viraram chips coloridos
        // (NameHeader/ArtworkFrame/ClassChip são tingidos pela classe no UpdateCardDisplay)
        SetQuadColor("EffectBackground", new Color(0.13f, 0.13f, 0.20f));
        SetQuadColor("AtkChip", new Color(0.58f, 0.20f, 0.12f));
        SetQuadColor("ShieldChip", new Color(0.13f, 0.30f, 0.55f));
        SetQuadColor("HpChip", new Color(0.12f, 0.40f, 0.19f));
        // Artwork cinza enquanto não há imagem
        if (artworkRenderer == null || card.artwork == null)
            SetQuadColor("Artwork", new Color(0.28f, 0.28f, 0.28f));

        // Oculta TextMeshPro filhos com nomes não reconhecidos (labels de prefabs antigos)
        // Usa comparação por nome para não depender dos campos estarem preenchidos
        foreach (Transform child in transform)
        {
            if (child.GetComponent<TextMeshPro>() == null) continue;
            string n = child.name;
            if (n == "CardNameText" || n == "AttackText" || n == "ShieldText" ||
                n == "HealthText" || n == "TierText" || n == "EffectText" ||
                n == "ClassText" || n == "ShopLockLabel") continue;
            child.gameObject.SetActive(false);
        }
    }

    void HideChild(string childName)
    {
        Transform t = transform.Find(childName);
        if (t != null) t.gameObject.SetActive(false);
    }

    void SetQuadColor(string childName, Color color)
    {
        Transform t = transform.Find(childName);
        if (t == null) return;
        Renderer r = t.GetComponent<Renderer>();
        if (r == null) return;
        EnsureQuadMaterial(r);
        if (r.sharedMaterial == null) return;
        r.material.color = color;
        r.material.SetColor("_BaseColor", color);
        r.material.SetColor("_Color", color);
    }

    // O material dos quads não é salvo dentro do prefab (fica nulo no Build).
    // Garante que o renderer tenha um material válido antes de pintar.
    void EnsureQuadMaterial(Renderer r)
    {
        if (r.sharedMaterial != null && r.sharedMaterial.shader != null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("Standard");
        if (shader == null) return;
        r.material = new Material(shader);
    }

    // ============ REDESIGN PROCEDURAL DA CARTA (layout v2) ============
    // O prefab antigo é uma pilha de quads retos com as zonas coladas.
    // Aqui, em runtime, os quads são substituídos por meshes de cantos
    // arredondados OPACAS (nada de transparência = nenhum problema de
    // ordenação no build) e os textos são reposicionados com respiro.
    // Camadas Y: 0.000 borda → 0.003 fundo → 0.006 painéis → 0.009 arte/chips
    // → 0.012-0.016 textos. Tudo abaixo do verso (0.020+).

    private bool layoutStyled = false;

    void EnsureModernLayout()
    {
        if (layoutStyled) return;
        layoutStyled = true;

        AutoAssignElements();

        // ── Painéis (carta 1.8 × 2.5; topo = z -1.25) ─────────────────────
        StyleRoundedPanel("Border", 1.94f, 2.64f, 0.18f, 0f, 0f, 0.000f);
        StyleRoundedPanel("Background", 1.80f, 2.50f, 0.14f, 0f, 0f, 0.003f);
        StyleRoundedPanel("NameHeader", 1.64f, 0.30f, 0.09f, 0f, -1.02f, 0.006f);
        StyleRoundedPanel("ArtworkFrame", 1.64f, 0.94f, 0.08f, 0f, -0.35f, 0.006f);
        StyleRoundedPanel("EffectBackground", 1.64f, 0.58f, 0.08f, 0f, 0.51f, 0.006f);
        StyleRoundedPanel("ClassChip", 0.70f, 0.20f, 0.10f, 0f, 0.10f, 0.012f);
        // A barra de tier vira um medalhão redondo no canto do cabeçalho
        // (o CardDisplay já pinta "TierBar" com a cor do tier)
        StyleRoundedPanel("TierBar", 0.32f, 0.32f, 0.16f, -0.68f, -1.02f, 0.009f);
        // Stats: três chips coloridos (ATK / DEF / HP).
        // ATENÇÃO: a carta nasce com rotação Y=180 (CardManager), então o X
        // local aparece ESPELHADO na tela — x positivo = lado esquerdo pro
        // jogador. ATK em +0.56 para a ordem visual ser ATK / DEF / HP.
        StyleRoundedPanel("AtkChip", 0.50f, 0.32f, 0.10f, 0.56f, 1.01f, 0.009f);
        StyleRoundedPanel("ShieldChip", 0.50f, 0.32f, 0.10f, 0f, 1.01f, 0.009f);
        StyleRoundedPanel("HpChip", 0.50f, 0.32f, 0.10f, -0.56f, 1.01f, 0.009f);

        // Elementos do layout antigo que não existem mais
        HideChild("StatsBackground");
        HideChild("StatsDivider1");
        HideChild("StatsDivider2");

        // Artwork emoldurado (o quad da arte fica ACIMA da moldura)
        Transform art = transform.Find("Artwork");
        if (art != null)
        {
            art.localPosition = new Vector3(0f, 0.009f, -0.35f);
            art.localScale = new Vector3(1.56f, 0.86f, 1f);
        }

        // ── Textos ────────────────────────────────────────────────────────
        StyleText(cardNameText, new Vector3(0.10f, 0.012f, -1.02f),
            new Vector2(1.24f, 0.26f), 1.3f, 2.4f, Color.white, false);
        StyleText(tierText, new Vector3(-0.68f, 0.013f, -1.02f),
            new Vector2(0.32f, 0.30f), 1.6f, 2.5f, Color.white, false);
        StyleText(classText, new Vector3(0f, 0.016f, 0.10f),
            new Vector2(0.66f, 0.18f), 0.9f, 1.45f, new Color(0.93f, 0.93f, 0.96f), false);
        StyleText(attackText, new Vector3(0.56f, 0.013f, 1.01f),
            new Vector2(0.46f, 0.28f), 2.0f, 3.1f, Color.white, false);
        StyleText(shieldText, new Vector3(0f, 0.013f, 1.01f),
            new Vector2(0.46f, 0.28f), 2.0f, 3.1f, Color.white, false);
        StyleText(healthText, new Vector3(-0.56f, 0.013f, 1.01f),
            new Vector2(0.46f, 0.28f), 2.0f, 3.1f, Color.white, false);
        StyleText(effectText, new Vector3(0f, 0.012f, 0.51f),
            new Vector2(1.46f, 0.48f), 0.9f, 1.9f, new Color(0.90f, 0.90f, 0.86f), true);
        if (effectText != null) effectText.fontStyle = FontStyles.Normal;
    }

    // Substitui o quad do filho por uma mesh de cantos arredondados (ou cria
    // o filho se não existir no prefab). O material continua vindo do
    // EnsureQuadMaterial/SetQuadColor — nada serializado no prefab.
    void StyleRoundedPanel(string childName, float width, float height,
                           float radius, float x, float z, float yLayer)
    {
        Transform t = transform.Find(childName);
        GameObject obj;
        if (t == null)
        {
            obj = new GameObject(childName);
            obj.transform.SetParent(transform, false);
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
        }
        else
        {
            obj = t.gameObject;
            Collider c = obj.GetComponent<Collider>();
            if (c != null) Destroy(c); // filhos nunca roubam cliques do BoxCollider da raiz
        }

        MeshFilter mf = obj.GetComponent<MeshFilter>();
        if (mf == null) mf = obj.AddComponent<MeshFilter>();
        mf.sharedMesh = GetRoundedRectMesh(width, height, radius);

        // A mesh já é gerada no tamanho final, no plano XZ virada para +Y
        // (mesma face dos quads antigos rotacionados)
        obj.transform.localPosition = new Vector3(x, yLayer, z);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
    }

    void StyleText(TextMeshPro tmp, Vector3 localPos, Vector2 rectSize,
                   float minSize, float maxSize, Color color, bool wrap)
    {
        if (tmp == null) return;
        tmp.transform.localPosition = localPos;
        tmp.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);
        tmp.rectTransform.sizeDelta = rectSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }

    // Cache global: as cartas compartilham as mesmas meshes por dimensão
    static readonly System.Collections.Generic.Dictionary<string, Mesh> roundedMeshCache =
        new System.Collections.Generic.Dictionary<string, Mesh>();

    static Mesh GetRoundedRectMesh(float width, float height, float radius)
    {
        string key = width.ToString("F3") + "x" + height.ToString("F3") + "x" + radius.ToString("F3");
        Mesh cached;
        if (roundedMeshCache.TryGetValue(key, out cached) && cached != null) return cached;

        const int segmentsPerCorner = 6;
        radius = Mathf.Min(radius, width * 0.5f, height * 0.5f);
        float cx = width * 0.5f - radius;
        float cz = height * 0.5f - radius;

        var verts = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var normals = new System.Collections.Generic.List<Vector3>();

        verts.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));
        normals.Add(Vector3.up);

        // Perímetro: 4 arcos de canto percorridos em sequência (loop fechado)
        Vector2[] corners = {
            new Vector2( cx,  cz),   // ângulos   0°– 90°
            new Vector2(-cx,  cz),   //          90°–180°
            new Vector2(-cx, -cz),   //         180°–270°
            new Vector2( cx, -cz)    //         270°–360°
        };
        for (int c = 0; c < 4; c++)
        {
            for (int s = 0; s <= segmentsPerCorner; s++)
            {
                float ang = (c * 90f + s * (90f / segmentsPerCorner)) * Mathf.Deg2Rad;
                float px = corners[c].x + Mathf.Cos(ang) * radius;
                float pz = corners[c].y + Mathf.Sin(ang) * radius;
                verts.Add(new Vector3(px, 0f, pz));
                // v = 1 no topo da carta (z negativo) — casa com o gradiente
                uvs.Add(new Vector2(0.5f + px / width, 0.5f - pz / height));
                normals.Add(Vector3.up);
            }
        }

        // Leque a partir do centro; ordem invertida para a face frontal
        // apontar para +Y (winding horário visto de cima)
        int n = verts.Count - 1;
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < n; i++)
        {
            tris.Add(0);
            tris.Add(1 + ((i + 1) % n));
            tris.Add(1 + i);
        }

        Mesh mesh = new Mesh();
        mesh.name = "RoundedRect_" + key;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        roundedMeshCache[key] = mesh;
        return mesh;
    }

    // Gradiente vertical do fundo (topo tingido pela classe → base escura),
    // gerado por código e cacheado por classe
    static readonly System.Collections.Generic.Dictionary<CardClass, Texture2D> classGradientCache =
        new System.Collections.Generic.Dictionary<CardClass, Texture2D>();

    Texture2D GetClassGradient(CardClass cardClass)
    {
        Texture2D cached;
        if (classGradientCache.TryGetValue(cardClass, out cached) && cached != null) return cached;

        Color top = Color.Lerp(new Color(0.10f, 0.10f, 0.16f), GetClassColor(cardClass), 0.38f);
        Color bottom = new Color(0.06f, 0.06f, 0.10f);

        Texture2D tex = new Texture2D(2, 64, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            Color c = Color.Lerp(bottom, top, y / 63f); // y=63 → v=1 → topo da carta
            tex.SetPixel(0, y, c);
            tex.SetPixel(1, y, c);
        }
        tex.Apply();

        classGradientCache[cardClass] = tex;
        return tex;
    }

    // ============ FIGURA 3D SOBRE A CARTA (tabuleiro) ============
    // Modelos em Assets/Resources/Models/personagem_<classe>.obj (+ textura
    // personagem_<classe>_tex.png) aparecem em pé sobre a carta quando ela
    // entra em campo. As 4 classes têm modelo: tank (Crusader), mago
    // (Archmage), healer (Emerald Archer), arqueiro (Obsidian Sentinel).
    // Animação procedural via FigureAnimator (os OBJ não têm rig/esqueleto).

    private GameObject boardFigure;

    // Cache do Resources.Load — guarda inclusive o "não existe" (null) para
    // não procurar o arquivo de novo a cada UpdateDisplay
    static readonly System.Collections.Generic.Dictionary<CardClass, GameObject> figurePrefabCache =
        new System.Collections.Generic.Dictionary<CardClass, GameObject>();

    static string FigureResourceName(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank: return "Models/personagem_tank";
            case CardClass.Mago: return "Models/personagem_mago";
            case CardClass.Healer: return "Models/personagem_healer";
            case CardClass.Arqueiro: return "Models/personagem_arqueiro";
        }
        return null;
    }

    // ===== FIGURAS KAYKIT (modelos leves, com rig e ARMA NA MÃO) =====
    // Os personagens do KayKit Adventurers têm ossos-soquete "handslot.r" e
    // "handslot.l" feitos para encaixar equipamento: a arma vira filha do osso
    // e acompanha as animações sozinha. Os mesmos modelos servem o lobby
    // (DecorProps.PlaceChar) — nada é duplicado.
    //
    // ╔════════════════════════════════════════════════════════════════╗
    // ║  true  = personagens KayKit (com armas)                        ║
    // ║  false = modelos antigos em Models/personagem_<classe>.obj     ║
    // ╚════════════════════════════════════════════════════════════════╝
    public const bool UseKayKitFigures = true;

    const string KayKitCharPath = "decor/kaykit/chars/";
    const string KayKitWeaponPath = "decor/kaykit/weapons/";

    // Modelo e textura de cada classe
    static string KayKitCharName(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank: return "Knight";
            case CardClass.Mago: return "Mage";
            case CardClass.Healer: return "Rogue_Hooded";
            case CardClass.Arqueiro: return "Ranger";
        }
        return null;
    }

    static string KayKitTextureName(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank: return "knight_texture";
            case CardClass.Mago: return "mage_texture";
            case CardClass.Healer: return "rogue_texture";
            case CardClass.Arqueiro: return "ranger_texture";
        }
        return null;
    }

    // Item de mão: modelo, textura própria e em qual soquete encaixa.
    // Cada arma vem com a textura do "tema" dela (a espada usa knight_texture,
    // o cajado usa mage_texture...) — por isso a textura é declarada aqui.
    struct HandItem
    {
        public string model, texture, bone;
        public Vector3 euler;   // giro extra dentro da mão (0,0,0 = pose do soquete)
        public Vector3 offset;  // deslocamento extra dentro da mão

        public HandItem(string model, string texture, string bone)
        { this.model = model; this.texture = texture; this.bone = bone; euler = Vector3.zero; offset = Vector3.zero; }

        // Versão com ajuste fino (quando a pose padrão do soquete não serve
        // para aquele item — o arco, por exemplo, nasce virado de lado)
        public HandItem(string model, string texture, string bone, Vector3 euler)
        { this.model = model; this.texture = texture; this.bone = bone; this.euler = euler; offset = Vector3.zero; }

        public HandItem(string model, string texture, string bone, Vector3 euler, Vector3 offset)
        { this.model = model; this.texture = texture; this.bone = bone; this.euler = euler; this.offset = offset; }
    }

    const string HandRight = "handslot.r";
    const string HandLeft = "handslot.l";

    static readonly HandItem[] NoItems = new HandItem[0];

    // ── EQUIPAMENTO POR CLASSE (é só mexer aqui para trocar de arma) ─────
    // Disponíveis em decor/kaykit/weapons: sword_1handed, shield_round, staff,
    // bow_withString, wand, spellbook_closed, dagger, quiver
    static HandItem[] KayKitHandItems(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank:
                return new[] {
                    new HandItem("sword_1handed", "knight_texture", HandRight),
                    new HandItem("shield_round",  "knight_texture", HandLeft),
                };
            case CardClass.Arqueiro:
                // O arco nasce de lado no soquete: 90° no eixo do cabo deixa
                // a curvatura virada para frente. Se ainda sair torto, tentar
                // o mesmo 90 (ou -90) em outro eixo do Vector3 abaixo.
                return new[] {
                    new HandItem("bow_withString", "ranger_texture", HandLeft, new Vector3(0f, 90f, 0f)),
                };
            case CardClass.Mago:
                return new[] {
                    new HandItem("staff", "mage_texture", HandRight),
                };
            case CardClass.Healer:
                return new[] {
                    new HandItem("wand",             "mage_texture", HandRight),
                    new HandItem("spellbook_closed", "mage_texture", HandLeft),
                };
        }
        return NoItems;
    }

    static bool IsKayKitClass(CardClass cardClass)
    {
        return UseKayKitFigures && KayKitCharName(cardClass) != null;
    }

    // ── Ajuste de PEÇAS do modelo ────────────────────────────────────────
    // Os personagens KayKit vêm em partes separadas (Mage_Hat, Mage_Cape,
    // Knight_Helmet...). Vista de cima no tabuleiro, uma peça alta tapa o
    // personagem inteiro — o chapéu pontudo do mago era o caso.
    struct PartTweak
    {
        public string nameContains;
        public float scale;
        public PartTweak(string nameContains, float scale)
        { this.nameContains = nameContains; this.scale = scale; }
    }

    static readonly PartTweak[] NoTweaks = new PartTweak[0];

    // ── PEÇAS ENCOLHIDAS POR CLASSE (mexer aqui para calibrar) ───────────
    static PartTweak[] KayKitPartTweaks(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Mago:
                // Chapéu a 55%: dá para ver a cara e o cajado dele de cima
                return new[] { new PartTweak("Hat", 0.55f) };
        }
        return NoTweaks;
    }

    // Encolhe as peças configuradas. Rodar ANTES do FitFigureOnCard: assim a
    // normalização de altura já mede a silhueta corrigida e o corpo cresce.
    static void ApplyPartTweaks(GameObject figure, CardClass cardClass)
    {
        if (figure == null) return;

        PartTweak[] tweaks = KayKitPartTweaks(cardClass);
        if (tweaks.Length == 0) return;

        foreach (PartTweak tweak in tweaks)
        {
            foreach (Renderer r in figure.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.IndexOf(tweak.nameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                SkinnedMeshRenderer skinned = r as SkinnedMeshRenderer;
                if (skinned != null && skinned.sharedMesh != null)
                {
                    // Peça colada no esqueleto: escalar o transform não faz
                    // efeito (a skin manda), então encolhemos a MALHA
                    skinned.sharedMesh = GetShrunkMesh(skinned.sharedMesh, tweak.scale);
                }
                else
                {
                    // Peça solta pendurada num osso: basta escalar
                    r.transform.localScale = r.transform.localScale * tweak.scale;
                }
            }
        }
    }

    // Cópia da malha encolhida na direção da BASE da peça (o chapéu continua
    // apoiado na cabeça). Cacheada: todas as cartas da classe compartilham a
    // mesma malha, e a original do projeto nunca é modificada.
    static readonly System.Collections.Generic.Dictionary<string, Mesh> shrunkMeshCache =
        new System.Collections.Generic.Dictionary<string, Mesh>();

    static Mesh GetShrunkMesh(Mesh source, float factor)
    {
        string key = source.name + "|" + factor.ToString("0.###");
        Mesh cached;
        if (shrunkMeshCache.TryGetValue(key, out cached) && cached != null) return cached;

        Mesh copy = Instantiate(source); // NUNCA mexer na malha original
        copy.name = source.name + "_x" + factor.ToString("0.##");

        Bounds b = source.bounds;
        Vector3 pivot = new Vector3(b.center.x, b.min.y, b.center.z); // base da peça

        Vector3[] verts = copy.vertices;
        for (int i = 0; i < verts.Length; i++)
            verts[i] = pivot + (verts[i] - pivot) * factor;
        copy.vertices = verts;
        copy.RecalculateBounds();

        shrunkMeshCache[key] = copy;
        return copy;
    }

    // Encaixa o equipamento nos ossos-soquete. Chamar SEMPRE DEPOIS do
    // FitFigureOnCard: as armas entrariam na medição dos bounds e encolheriam
    // o personagem (um arco é mais alto que o arqueiro).
    static void AttachHandItems(GameObject figure, CardClass cardClass)
    {
        if (figure == null) return;

        HandItem[] items = KayKitHandItems(cardClass);
        if (items.Length == 0) return;

        // Mapeia os soquetes uma vez só (o rig tem centenas de ossos)
        Transform right = null, left = null;
        foreach (Transform t in figure.GetComponentsInChildren<Transform>(true))
        {
            if (right == null && t.name.Equals(HandRight, System.StringComparison.OrdinalIgnoreCase)) right = t;
            else if (left == null && t.name.Equals(HandLeft, System.StringComparison.OrdinalIgnoreCase)) left = t;
            if (right != null && left != null) break;
        }

        if (right == null && left == null)
        {
            Debug.LogWarning($"[Figure] {cardClass}: soquetes de mão não encontrados no rig — sem arma.");
            return;
        }

        foreach (HandItem item in items)
        {
            Transform socket = item.bone == HandLeft ? left : right;
            if (socket == null) continue;

            GameObject prefab = GetWeaponPrefab(item.model);
            if (prefab == null) continue;

            GameObject weapon = Instantiate(prefab, socket);
            weapon.name = "Hand_" + item.model;
            // O soquete já entrega a pose certa; euler/offset são o ajuste
            // fino de itens que fogem do padrão. A escala vem do personagem,
            // que já foi dimensionado pelo FitFigureOnCard.
            weapon.transform.localPosition = item.offset;
            weapon.transform.localRotation = Quaternion.Euler(item.euler);
            weapon.transform.localScale = Vector3.one;

            foreach (Collider c in weapon.GetComponentsInChildren<Collider>(true))
                Destroy(c);

            Material mat = GetWeaponMaterial(item.texture);
            if (mat != null)
            {
                foreach (Renderer r in weapon.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterial = mat;
            }
        }
    }

    static readonly System.Collections.Generic.Dictionary<string, GameObject> weaponPrefabCache =
        new System.Collections.Generic.Dictionary<string, GameObject>();

    static GameObject GetWeaponPrefab(string model)
    {
        GameObject cached;
        if (weaponPrefabCache.TryGetValue(model, out cached)) return cached;

        GameObject prefab = Resources.Load<GameObject>(KayKitWeaponPath + model);
        if (prefab == null) Debug.LogWarning($"[Figure] Arma não encontrada: {KayKitWeaponPath}{model}");
        weaponPrefabCache[model] = prefab;
        return prefab;
    }

    // Um material por textura de arma, compartilhado por todas as instâncias
    static readonly System.Collections.Generic.Dictionary<string, Material> weaponMatCache =
        new System.Collections.Generic.Dictionary<string, Material>();

    static Material GetWeaponMaterial(string textureName)
    {
        Material cached;
        if (weaponMatCache.TryGetValue(textureName, out cached)) return cached;

        Material mat = null;
        Shader s = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");
        if (s != null)
        {
            mat = new Material(s);
            Texture2D tex = Resources.Load<Texture2D>(KayKitCharPath + textureName);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BaseMap", tex);
            }
        }
        weaponMatCache[textureName] = mat;
        return mat;
    }

    static GameObject GetFigurePrefab(CardClass cardClass)
    {
        GameObject cached;
        if (figurePrefabCache.TryGetValue(cardClass, out cached)) return cached;

        GameObject prefab = null;

        // KayKit primeiro; se faltar o arquivo, cai no modelo antigo
        if (IsKayKitClass(cardClass))
            prefab = Resources.Load<GameObject>(KayKitCharPath + KayKitCharName(cardClass));

        if (prefab == null)
        {
            string resourceName = FigureResourceName(cardClass);
            prefab = resourceName != null ? Resources.Load<GameObject>(resourceName) : null;
        }

        figurePrefabCache[cardClass] = prefab;
        return prefab;
    }

    // ===== FIGURA COM RIG (por classe) =====
    // Modelo + animações carregados por convenção de nome:
    //   Models/personagem_<classe>_idle / _walk / _attack / _death / _react
    // GLB (Meshy, glTFast) e FBX (Mixamo) funcionam igual — o Resources.Load
    // ignora a extensão. Basta ter um modelo riggado + pelo menos idle OU walk.
    // Sem isso, a classe cai no OBJ estático + FigureAnimator procedural.
    class RiggedSet
    {
        public GameObject model;
        public AnimationClip idle, walk, attack, death, react;
    }

    // O modelo já traz textura própria? (boneco padrão do Mixamo com pele
    // embutida traz; Meshy sem textura embutida não — aí reaplicamos a da classe)
    static bool ModelHasOwnTexture(GameObject fig)
    {
        foreach (Renderer r in fig.GetComponentsInChildren<Renderer>(true))
        {
            Material m = r.sharedMaterial;
            if (m == null) continue;
            if (m.mainTexture != null) return true;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return true;
        }
        return false;
    }

    static readonly System.Collections.Generic.Dictionary<CardClass, RiggedSet> riggedSetCache =
        new System.Collections.Generic.Dictionary<CardClass, RiggedSet>();

    static AnimationClip FirstClip(string resourcePath)
    {
        foreach (AnimationClip c in Resources.LoadAll<AnimationClip>(resourcePath))
            return c;
        return null;
    }

    // Todos os clipes dos FBX de animação do KayKit (carregado uma vez só)
    static AnimationClip[] kaykitClips;

    static AnimationClip[] GetKayKitClips()
    {
        if (kaykitClips != null) return kaykitClips;

        var all = new System.Collections.Generic.List<AnimationClip>();
        all.AddRange(Resources.LoadAll<AnimationClip>("decor/kaykit/anim/Rig_Medium_General"));
        all.AddRange(Resources.LoadAll<AnimationClip>("decor/kaykit/anim/Rig_Medium_MovementBasic"));
        kaykitClips = all.ToArray();

        if (kaykitClips.Length == 0)
            Debug.LogWarning("[Figure] Nenhum clipe KayKit em decor/kaykit/anim — figuras usarão animação procedural.");
        return kaykitClips;
    }

    // Primeiro clipe cujo nome casa com uma das palavras (na ordem dada)
    static AnimationClip PickClip(AnimationClip[] pool, params string[] keys)
    {
        if (pool == null) return null;
        foreach (string key in keys)
        {
            foreach (AnimationClip c in pool)
            {
                if (c != null && c.name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return c;
            }
        }
        return null;
    }

    // Escolhe o FBX que traz a MALHA (SkinnedMeshRenderer): normalmente o idle
    // "With Skin", mas em alguns downloads a pele veio só na death/attack
    static GameObject FindRiggedModel(string baseName)
    {
        string[] suf = { "_idle", "_walk", "_attack", "_death", "_react" };
        foreach (string s in suf)
        {
            GameObject g = Resources.Load<GameObject>(baseName + s);
            if (g != null && g.GetComponentInChildren<SkinnedMeshRenderer>(true) != null)
                return g;
        }
        // Nenhum com malha detectada: usa o primeiro que carregar (fallback)
        foreach (string s in suf)
        {
            GameObject g = Resources.Load<GameObject>(baseName + s);
            if (g != null) return g;
        }
        return null;
    }

    static RiggedSet GetRiggedSet(CardClass cardClass)
    {
        RiggedSet cached;
        if (riggedSetCache.TryGetValue(cardClass, out cached)) return cached;

        RiggedSet set = null;

        // ── KayKit: um único conjunto de animações serve os 4 personagens ──
        // (todos usam o mesmo esqueleto "Rig_Medium"), então os clipes vêm dos
        // FBX compartilhados e são escolhidos pelo NOME.
        if (IsKayKitClass(cardClass))
        {
            GameObject model = Resources.Load<GameObject>(KayKitCharPath + KayKitCharName(cardClass));
            if (model != null)
            {
                AnimationClip[] pool = GetKayKitClips();
                set = new RiggedSet
                {
                    model = model,
                    idle = PickClip(pool, "Idle_A", "Idle"),
                    walk = PickClip(pool, "Walking_A", "Walking", "Running"),
                    // O pack grátis não traz ataque; se um dia vier um pack com
                    // ataque, estes nomes o encontram sozinhos
                    attack = PickClip(pool, "Attack", "Melee", "Slice", "Chop", "Shoot", "Use_Item"),
                    death = PickClip(pool, "Death_A", "Death"),
                    react = PickClip(pool, "Hit_A", "Hit"),
                };
                Debug.Log($"[Figure] KayKit {cardClass}: idle={set.idle != null}, walk={set.walk != null}, " +
                          $"attack={set.attack != null}, death={set.death != null}, react={set.react != null}");

                // Sem idle nem walk o rig não tem o que tocar (ficaria em pose
                // de bind): melhor cair na animação procedural
                if (set.idle == null && set.walk == null) set = null;
            }
            riggedSetCache[cardClass] = set;
            return set;
        }

        string baseName = FigureResourceName(cardClass); // "Models/personagem_arqueiro"
        if (baseName != null)
        {
            AnimationClip idle = FirstClip(baseName + "_idle");
            AnimationClip walk = FirstClip(baseName + "_walk");
            AnimationClip attack = FirstClip(baseName + "_attack");
            AnimationClip death = FirstClip(baseName + "_death");
            AnimationClip react = FirstClip(baseName + "_react");

            // O modelo riggado = o FBX que TEM malha (SkinnedMeshRenderer). Em
            // alguns downloads a malha veio só na death (With Skin), não no idle
            GameObject model = FindRiggedModel(baseName);

            if (model != null && (idle != null || walk != null))
            {
                set = new RiggedSet
                {
                    model = model, idle = idle, walk = walk, attack = attack, death = death, react = react,
                };
                Debug.Log($"[Figure] Rig de {cardClass}: idle={idle != null}, walk={walk != null}, attack={attack != null}, death={death != null}, react={react != null}");
            }
        }

        riggedSetCache[cardClass] = set;
        return set;
    }

    // Textura do modelo (os OBJ da Meshy vêm com PNG ao lado; carregamos
    // separado para aplicar via material URP em runtime — o material importado
    // do OBJ fica magenta no build URP). Cache inclui o "não existe" (null).
    static readonly System.Collections.Generic.Dictionary<CardClass, Texture2D> figureTexCache =
        new System.Collections.Generic.Dictionary<CardClass, Texture2D>();

    static Texture2D GetFigureTexture(CardClass cardClass)
    {
        Texture2D cached;
        if (figureTexCache.TryGetValue(cardClass, out cached)) return cached;

        Texture2D tex = null;

        if (IsKayKitClass(cardClass))
            tex = Resources.Load<Texture2D>(KayKitCharPath + KayKitTextureName(cardClass));

        if (tex == null)
        {
            string baseName = FigureResourceName(cardClass);
            tex = baseName != null ? Resources.Load<Texture2D>(baseName + "_tex") : null;
        }

        figureTexCache[cardClass] = tex;
        return tex;
    }

    void UpdateBoardFigure()
    {
        GameObject prefab = isOnBoard && card != null ? GetFigurePrefab(card.cardClass) : null;

        // Saiu do tabuleiro (ou classe sem modelo): remove a figura
        if (prefab == null)
        {
            if (boardFigure != null)
            {
                Destroy(boardFigure);
                boardFigure = null;
                figureGhosted = false;
                if (figureSolidMats != null) figureSolidMats.Clear();
                if (figureBaseMat != null) { Destroy(figureBaseMat); figureBaseMat = null; }
            }
            return;
        }

        if (boardFigure == null)
        {
            // Se a classe tem modelo COM RIG (animações reais), usa ele; senão
            // cai no OBJ estático + animação procedural
            RiggedSet rig = GetRiggedSet(card.cardClass);

            boardFigure = Instantiate(rig != null ? rig.model : prefab, transform);
            boardFigure.name = "BoardFigure";

            // A figura não pode roubar os cliques da carta
            foreach (Collider c in boardFigure.GetComponentsInChildren<Collider>())
                Destroy(c);

            // Peças desproporcionais (chapéu do mago) antes de medir a altura
            if (IsKayKitClass(card.cardClass)) ApplyPartTweaks(boardFigure, card.cardClass);

            FitFigureOnCard();

            bool kaykit = IsKayKitClass(card.cardClass);

            if (rig != null)
            {
                // KayKit: sempre reaplica a textura da classe (o material vindo
                // do FBX fica magenta no build URP). Nos outros modelos, só
                // quando o próprio modelo não trouxe textura embutida.
                if (kaykit || !ModelHasOwnTexture(boardFigure)) ApplyFigureMaterial();

                // Animação skeletal: anda só ao mover, pose de espera parado,
                // golpe ao atacar, react ao levar dano e morte ao morrer
                FigureRiggedAnimator ra = boardFigure.AddComponent<FigureRiggedAnimator>();
                // Vigia a CARTA (this.transform): o hover só escala a carta, então
                // não conta como "andar"; só o movimento real de casa dispara.
                ra.Initialize(rig.walk, rig.attack, rig.idle, rig.death, rig.react, transform);

                // Armas: modelos reais do KayKit encaixados nos ossos-soquete
                // das mãos (acompanham as animações). Sem KayKit, as antigas
                // armas desenhadas por código.
                if (!kaykit)
                {
                    if (card.cardClass == CardClass.Arqueiro)
                        ProceduralBow.Attach(boardFigure);
                    else if (card.cardClass == CardClass.Healer)
                        ProceduralMace.Attach(boardFigure);
                }
            }
            else
            {
                // Aplica a textura do modelo num material URP (o material importado
                // do OBJ fica magenta no build) — um material só para a figura toda
                ApplyFigureMaterial();

                // Vida na figura: entrada caindo, respiração no idle, pulinhos ao
                // mover e golpe ao atacar (ver FigureAnimator)
                FigureAnimator figAnim = boardFigure.AddComponent<FigureAnimator>();
                figAnim.Initialize();
                figAnim.PlayEntrance();
            }

            // Arma na mão por último: o FitFigureOnCard já mediu o personagem
            // sozinho (se a arma entrasse antes, um arco mais alto que o
            // arqueiro encolheria ele para caber)
            if (kaykit) AttachHandItems(boardFigure, card.cardClass);
        }

        // Tom do dono (azul = P1, vermelho = P2). Como agora há textura, é um
        // tint SUAVE que MULTIPLICA a textura (não a substitui) — dá para ler
        // de quem é o personagem sem apagar o visual dele
        Color ownerTint = ownerPlayerNumber == 1 ? new Color(0.78f, 0.86f, 1.00f)
                       : ownerPlayerNumber == 2 ? new Color(1.00f, 0.82f, 0.78f)
                       : Color.white;
        if (isFrozen) ownerTint = new Color(0.60f, 0.82f, 1.00f);
        figureTint = ownerTint;
        if (!figureGhosted && figureBaseMat != null)
        {
            figureBaseMat.color = ownerTint;
            figureBaseMat.SetColor("_BaseColor", ownerTint);
        }

        // Com o mouse em cima a figura fica FANTASMA (translúcida): o efeito
        // da carta continua legível e as animações seguem visíveis
        SetFigureGhost(isMouseOver);
    }

    // Material único da figura: shader URP com a textura do modelo. Unlit
    // primeiro (mostra a textura como veio, sem depender de luz da cena — os
    // modelos Meshy já têm sombreamento embutido na textura); Lit e
    // Sprites/Default como reserva. Todos estão a salvo do stripping do build.
    private Material figureBaseMat;

    void ApplyFigureMaterial()
    {
        if (boardFigure == null || card == null) return;

        Texture2D tex = GetFigureTexture(card.cardClass);

        Shader s = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");
        if (s == null) return; // sem shader seguro: mantém o material importado

        figureBaseMat = new Material(s);
        if (tex != null)
        {
            figureBaseMat.mainTexture = tex;
            figureBaseMat.SetTexture("_BaseMap", tex); // nome URP
        }

        foreach (Renderer r in boardFigure.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = new Material[r.sharedMaterials.Length == 0 ? 1 : r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = figureBaseMat;
            r.sharedMaterials = mats;
        }
    }

    // ── Modo fantasma da figura durante o hover ──────────────────────────
    // Troca todos os materiais por um translúcido (Sprites/Default, único
    // shader com transparência comprovado no build URP) e restaura os
    // originais ao sair. A figura continua ativa: FigureAnimator segue rodando.
    private bool figureGhosted = false;
    private Material figureGhostMat;
    private Color figureTint = Color.white;
    private System.Collections.Generic.Dictionary<Renderer, Material[]> figureSolidMats;

    void SetFigureGhost(bool ghost)
    {
        if (boardFigure == null || figureGhosted == ghost) return;
        figureGhosted = ghost;

        if (ghost)
        {
            if (figureGhostMat == null)
            {
                Shader s = Shader.Find("Sprites/Default")
                        ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (s == null)
                {
                    // Sem shader transparente disponível: volta ao comportamento
                    // antigo (esconder) em vez de deixar a figura opaca na frente
                    boardFigure.SetActive(false);
                    return;
                }
                figureGhostMat = new Material(s);
            }
            Color ghostColor = figureTint;
            ghostColor.a = 0.25f;
            figureGhostMat.color = ghostColor;
            figureGhostMat.SetColor("_BaseColor", ghostColor);

            if (figureSolidMats == null)
                figureSolidMats = new System.Collections.Generic.Dictionary<Renderer, Material[]>();
            foreach (Renderer r in boardFigure.GetComponentsInChildren<Renderer>(true))
            {
                if (!figureSolidMats.ContainsKey(r)) figureSolidMats[r] = r.sharedMaterials;
                Material[] ghosts = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < ghosts.Length; i++) ghosts[i] = figureGhostMat;
                r.sharedMaterials = ghosts;
            }
        }
        else
        {
            if (figureSolidMats != null)
            {
                foreach (var kv in figureSolidMats)
                {
                    if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
                }
                figureSolidMats.Clear();
            }
            if (!boardFigure.activeSelf) boardFigure.SetActive(true);
        }
    }

    // Normaliza escala/posição: o modelo pode vir do Blender em qualquer
    // tamanho — aqui ele é medido pelos bounds reais e ajustado para a
    // altura alvo no mundo, pés apoiados na superfície da carta
    void FitFigureOnCard()
    {
        Renderer[] renderers = boardFigure.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // De frente para o lado inimigo (P1 e P2 se encaram)
        boardFigure.transform.rotation =
            Quaternion.Euler(0f, ownerPlayerNumber == 2 ? 180f : 0f, 0f);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // Tiles têm 6 unidades — a figura precisa ter presença no tabuleiro
        const float TargetWorldHeight = 4.5f;
        if (b.size.y > 0.0001f)
        {
            float k = TargetWorldHeight / b.size.y;
            boardFigure.transform.localScale = boardFigure.transform.localScale * k;
        }

        // Re-mede depois da escala e posiciona a figura EM CIMA DA ARTE da
        // carta (zona local z -0.35, metade de cima) — assim a caixa de efeito
        // e os stats na metade de baixo continuam legíveis com ela em campo
        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        Vector3 anchor = transform.TransformPoint(new Vector3(0f, 0f, -0.35f));
        Vector3 shift = new Vector3(
            anchor.x - b.center.x,
            transform.position.y + 0.06f - b.min.y,
            anchor.z - b.center.z);
        boardFigure.transform.position += shift;
    }

    // ============ VERSO DA CARTA (mão do oponente) ============

    // Mostra/esconde o verso: um encarte opaco por cima da frente da carta.
    // O dono vê a carta normal no cliente dele; o oponente vê só o verso.
    public void SetFaceDown(bool faceDown)
    {
        isFaceDown = faceDown;

        if (faceDown && backCover == null)
        {
            BuildBackCover();
        }

        if (backCover != null)
        {
            backCover.SetActive(faceDown);
        }
    }

    // Monta o verso em runtime (o prefab não tem verso salvo — mesma regra dos
    // materiais: nada de depender de material serializado do prefab)
    void BuildBackCover()
    {
        backCover = new GameObject("BackCover");
        backCover.transform.SetParent(transform, false);

        // Camadas locais ACIMA de toda a frente da carta (borda 0.000 → textos 0.012)
        MakeCoverQuad("CoverBase", 1.94f, 2.64f, 0.020f, new Color(0.05f, 0.06f, 0.12f));
        MakeCoverQuad("CoverPanel", 1.70f, 2.40f, 0.023f, new Color(0.10f, 0.12f, 0.22f));

        // Logo dourado no centro (filho do BackCover, então o filtro de TMPs
        // desconhecidos do ApplyCardTheme não o atinge)
        GameObject logoObj = new GameObject("CoverLogo");
        logoObj.transform.SetParent(backCover.transform, false);
        logoObj.transform.localPosition = new Vector3(0f, 0.026f, 0f);
        logoObj.transform.localRotation = Quaternion.Euler(90f, 180f, 0f);

        TextMeshPro logo = logoObj.AddComponent<TextMeshPro>();
        logo.text = "CARD\nGAME";
        logo.fontSize = 3.2f;
        logo.fontStyle = FontStyles.Bold;
        logo.alignment = TextAlignmentOptions.Center;
        logo.color = new Color(0.96f, 0.77f, 0.32f);
        logo.richText = false;
        logo.rectTransform.sizeDelta = new Vector2(1.7f, 2.4f);
        // Acima dos quads do verso (3500), que por sua vez cobrem os textos da carta
        logo.fontMaterial.renderQueue = 3600;
    }

    void MakeCoverQuad(string quadName, float width, float height, float yLayer, Color color)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = quadName;
        quad.transform.SetParent(backCover.transform, false);
        quad.transform.localPosition = new Vector3(0f, yLayer, 0f);
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Mesma orientação da frente
        quad.transform.localScale = new Vector3(width, height, 1f);

        // Sem collider: o verso não pode roubar cliques
        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null) Destroy(quadCollider);

        Renderer r = quad.GetComponent<Renderer>();
        if (r != null)
        {
            // NÃO usar o material padrão do CreatePrimitive: ele usa o shader
            // Standard, que o build URP remove — no editor aparece, no build o
            // quad fica INVISÍVEL (e a carta continua legível por baixo).
            // Sprites/Default está em Always Included Shaders (mesmo truque do
            // CardStatusVisuals, comprovado no build).
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = color;
                mat.SetColor("_BaseColor", color);
                // Renderiza DEPOIS de todos os textos da carta: os TMPs ordenam
                // por distância e o nome (topo da carta, mais perto da câmera)
                // era desenhado por cima do verso, vazando o nome da carta
                mat.renderQueue = 3500;
                r.material = mat;
            }
        }
    }

    Color GetTierColor(CardTier tier)
    {
        switch (tier)
        {
            case CardTier.Tier1: return new Color(0.55f, 0.55f, 0.55f);  // Prata
            case CardTier.Tier2: return new Color(0.80f, 0.65f, 0.00f);  // Dourado
            case CardTier.Tier3: return new Color(1.00f, 0.55f, 0.00f);  // Laranja
            case CardTier.Tier4: return new Color(0.55f, 0.00f, 1.00f);  // Roxo
            case CardTier.Tier5: return new Color(0.86f, 0.08f, 0.24f);  // Carmesim
            default: return Color.gray;
        }
    }

    Color GetClassColor(CardClass cardClass)
    {
        switch (cardClass)
        {
            case CardClass.Tank:
                return tankColor;
            case CardClass.Mago:
                return magoColor;
            case CardClass.Healer:
                return healerColor;
            case CardClass.Arqueiro:
                return arqueiroColor;
            default:
                return Color.white;
        }
    }

    // Caixa envolvente (mundo) somando os renderers da carta — usada para ancorar
    // o zoom de hover na base/lado. Renderer.bounds já reflete a escala atual.
    Bounds GetCardWorldBounds()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        bool has = false;
        Bounds b = new Bounds(transform.position, Vector3.zero);
        foreach (var r in rends)
        {
            if (r == null) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // Para interação com o mouse — zoom RELATIVO à escala atual (o bug antigo usava
    // uma "escala original" capturada antes da escala da loja ser aplicada, então
    // o hover ENCOLHIA a carta em vez de aumentar)
    void OnMouseEnter()
    {
        // Mouse sobre a UI (um modal/popup por cima da carta): NÃO faz hover
        // nem tooltip — o clique e o realce pertencem à UI, não à carta atrás
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        isMouseOver = true;

        // Tooltip de inspeção: aparece para QUALQUER carta (inclusive na mão),
        // mostrando efeito por extenso + progresso de tríade. Só o verso não.
        if (!isFaceDown) CardTooltip.ShowFor(this);

        // Aura: destaca com moldura verde as cartas beneficiadas/protegidas
        if (isOnBoard) CardAuraIndicator.ShowLinksFor(this);

        if (isInHand) return;

        preHoverScale = transform.localScale;
        hoverLifted = false;
        hoverZone = isInShop ? 1 : 2;

        if (isInShop)
        {
            // Zoom para ler o efeito. ANCORA a base (min Y) e o lado voltado ao
            // tabuleiro (max X): assim a carta cresce só PARA CIMA e para o lado
            // da loja — antes escalava a partir do centro e a metade de baixo
            // afundava no tile do tabuleiro (e crescia por cima dele)
            preHoverPosition = transform.position;
            Bounds before = GetCardWorldBounds();
            transform.localScale = preHoverScale * 1.7f;
            Bounds after = GetCardWorldBounds();
            Vector3 shift = Vector3.zero;
            shift.y = before.min.y - after.min.y;   // base fixa → cresce para cima
            shift.x = before.max.x - after.max.x;   // borda do tabuleiro fixa → cresce para a loja
            transform.position += shift;

            // Traz a carta para a FRENTE das vizinhas. Como a câmera é ortográfica,
            // mover ao longo de -forward muda só a PROFUNDIDADE (desenha por cima)
            // sem alterar posição/tamanho na tela — então a âncora acima é mantida
            // e o hover não volta a invadir o tabuleiro.
            Camera cam = Camera.main;
            if (cam != null)
                transform.position += -cam.transform.forward * 10f;

            hoverLifted = true;
        }
        else
        {
            // Destaque leve no tabuleiro; a figura 3D fica translúcida para o
            // efeito da carta ficar legível enquanto o mouse estiver em cima
            transform.localScale = preHoverScale * 1.15f;
            SetFigureGhost(true);
        }
    }

    void OnMouseExit()
    {
        isMouseOver = false;

        CardTooltip.HideTip();
        CardAuraIndicator.HideLinks();

        // Mouse saiu: a figura 3D volta a ficar sólida
        SetFigureGhost(false);

        // Restaura APENAS se a carta continua na mesma "zona" de quando o hover
        // começou. Se ela mudou de lugar nesse meio-tempo (comprada, colocada no
        // tabuleiro, movida), quem manda é a nova posição/escala. Sem isso, um
        // estado velho da época da LOJA era despejado numa carta já em campo —
        // era o bug das cartas com efeito de seleção: o popup "Entendi" bloqueia
        // o OnMouseEnter (pointer sobre UI) de renovar o estado, e o OnMouseExit
        // (que roda SEM guarda de UI) teleportava a carta para o slot da loja
        // em tamanho de loja. O estado é SEMPRE limpo ao sair, em qualquer caso.
        bool sameZone = (hoverZone == 1 && isInShop) ||
                        (hoverZone == 2 && isOnBoard && !isInHand);
        if (sameZone)
        {
            if (preHoverScale != Vector3.zero)
            {
                transform.localScale = preHoverScale;
            }
            if (hoverLifted)
            {
                transform.position = preHoverPosition;
            }
        }
        preHoverScale = Vector3.zero;
        hoverLifted = false;
        hoverZone = 0;
    }

    void OnMouseDown()
    {
        // Clique sobre a UI (botões, popups): a UI consome o clique. Sem isso,
        // clicar em "Passar a Vez" com uma carta da loja atrás também COMPRAVA
        // a carta — o raycast 3D e o botão recebiam o mesmo clique
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        // Verifica se a carta foi inicializada
        if (card == null)
        {
            Debug.LogWarning("Carta não foi inicializada ainda!");
            return;
        }

        // Carta virada para baixo (mão do oponente): não interage
        if (isFaceDown) return;

        // Decisão de efeito pendente (popup seu ou o oponente decidindo):
        // nenhuma ação nova até resolver
        if (GameManager.IsDecisionPending())
        {
            Debug.Log("[CardDisplay] Aguarde a decisão de efeito ser resolvida!");
            return;
        }

        // Se o jogo aguarda seleção de alvo de efeito (congelar / quebrar armadura),
        // qualquer clique em carta do tabuleiro vai direto para a escolha de alvo —
        // sem isso, cliques em cartas inimigas nunca chegariam à seleção
        if (isOnBoard && GameManager.Instance != null &&
            (GameManager.Instance.IsWaitingForFreezeTarget() ||
             GameManager.Instance.IsWaitingForShieldBreakTargets() ||
             GameManager.Instance.IsWaitingForEffectTarget()))
        {
            CardTile selectionTile = currentTile != null ? currentTile : FindCurrentTile();
            if (selectionTile != null)
            {
                GameManager.Instance.SelectCardFromBoard(gameObject, this, selectionTile);
            }
            return;
        }

        // Modo "Vender carta" (fase inicial): clicar numa carta da SUA mão a
        // vende (devolve custo - 2 de ouro e libera 1 do limite de compras).
        // ANTES da checagem de dono vs. jogador do turno — na fase inicial as
        // compras são simultâneas e o P2 não é o "jogador atual"
        if (isInHand && TurnManager.Instance != null &&
            TurnManager.Instance.gameState == GameState.Lobby &&
            GameUIManager.Instance != null && GameUIManager.Instance.IsSellCardMode())
        {
            TrySellCard();
            return;
        }

        // Se a carta NÃO está na loja (está na mão ou tabuleiro), verifica se pertence ao jogador atual
        if (!isInShop && TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;

            if (ownerPlayerNumber != 0 && ownerPlayerNumber != currentPlayerNumber)
            {
                // Carta inimiga clicada - tenta atacar se houver uma carta selecionada
                if (GameManager.Instance != null && GameManager.Instance.HasSelectedCard())
                {
                    GameManager.Instance.TryAttackEnemyCard(this);
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        // Se a carta está no tabuleiro, seleciona para mover
        if (isOnBoard)
        {
            if (GameManager.Instance != null)
            {
                // Usa o tile armazenado ou tenta encontrar
                CardTile tile = currentTile != null ? currentTile : FindCurrentTile();
                if (tile != null)
                {
                    GameManager.Instance.SelectCardFromBoard(gameObject, this, tile);
                }
                else
                {
                    Debug.LogError("Não foi possível encontrar o tile onde a carta está!");
                }
            }
            return;
        }

        // Se a carta está na loja (fase de compra)
        if (isInShop)
        {
            // Modo "Travar cartas" ligado: o clique SELECIONA a carta para não
            // renovar no próximo refresh (aura dourada), em vez de comprar
            if (GameUIManager.Instance != null && GameUIManager.Instance.IsShopLockMode() &&
                TurnManager.Instance != null && TurnManager.Instance.gameState != GameState.Lobby)
            {
                TryToggleShopLock();
                return;
            }
            TryBuyCard();
            return;
        }

        // Se a carta ainda não está na mão, adiciona (fluxo antigo)
        if (!isInHand)
        {
            // Busca HandManager do jogador atual se estiver em modo multiplayer
            if (TurnManager.Instance != null)
            {
                PlayerData currentPlayer = TurnManager.Instance.GetCurrentPlayer();
                handManager = GetHandManagerForPlayer(currentPlayer.playerNumber);
                ownerPlayerNumber = currentPlayer.playerNumber; // Define o dono
                UpdateCardDisplay(); // Atualiza a borda com a cor do dono
            }
            else if (handManager == null)
            {
                // Fallback para modo single player
                handManager = FindObjectOfType<HandManager>();
            }

            if (handManager != null)
            {
                bool added = handManager.AddCardToHand(gameObject);
                if (added)
                {
                    isInHand = true;
                    isInShop = false;
                    transform.localScale = Vector3.one * HandScale; // Tamanho da mão
                }
            }
            else
            {
                Debug.LogError("HandManager não encontrado! Certifique-se de criar um GameObject com o componente HandManager.");
            }
        }
        else
        {
            // Carta está na mão - seleciona para colocar no tabuleiro
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SelectCardFromHand(gameObject, this);
            }
            else
            {
                Debug.LogError("GameManager não encontrado! Certifique-se de criar um GameObject com o componente GameManager.");
            }
        }
    }

    void TryBuyCard()
    {
        if (TurnManager.Instance == null)
        {
            Debug.LogError("TurnManager não encontrado!");
            return;
        }

        bool lobbyPhase = TurnManager.Instance.gameState == GameState.Lobby;

        // Comprador: na fase inicial as compras são SIMULTÂNEAS (sem turnos),
        // então o comprador é sempre o jogador LOCAL; durante a partida é o
        // jogador do turno atual
        PlayerData currentPlayer;
        if (lobbyPhase && PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            currentPlayer = TurnManager.Instance.GetPlayer(PhotonGameManager.Instance.myPlayerNumber);
        }
        else
        {
            currentPlayer = TurnManager.Instance.GetCurrentPlayer();
        }

        // Em multiplayer, DURANTE a partida só pode comprar no SEU turno
        // (na fase inicial não há turnos)
        if (!lobbyPhase && PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
            currentPlayer.playerNumber != PhotonGameManager.Instance.myPlayerNumber)
        {
            Debug.Log("[CardDisplay] Não é seu turno, não pode comprar!");
            return;
        }

        // Na fase de compra (Lobby): quem já clicou "Iniciar Partida" não compra mais
        // até a partida realmente começar
        if (lobbyPhase)
        {
            bool alreadyReady =
                (currentPlayer.playerNumber == 1 && TurnManager.Instance.player1Ready) ||
                (currentPlayer.playerNumber == 2 && TurnManager.Instance.player2Ready);
            if (alreadyReady)
            {
                Debug.Log("[CardDisplay] Você já clicou em Iniciar Partida — aguarde o oponente!");
                return;
            }
        }

        // Compra grátis pendente (Healer 5): ignora limite de compras e ouro
        bool freeBuy = currentPlayer.freePurchases > 0;

        // Limite de compras: fase inicial = 5 no total; partida = 2 por turno
        if (!freeBuy)
        {
            bool canBuy = lobbyPhase ? currentPlayer.CanBuyCardInLobby() : currentPlayer.CanBuyCard();
            if (!canBuy)
            {
                Debug.Log(lobbyPhase
                    ? $"[CardDisplay] Limite de {PlayerData.MaxCardsInLobby} compras da fase inicial atingido!"
                    : "[CardDisplay] Limite de compras deste turno atingido!");
                return;
            }
        }

        int cost = DiscountedCost(card, currentPlayer);

        // Verifica se o jogador tem ouro suficiente
        if (!freeBuy && !currentPlayer.HasEnoughGold(cost))
        {
            return;
        }

        // Mão cheia: bloqueia ANTES de enviar o RPC (não gasta ouro à toa)
        HandManager buyerHand = GetHandManagerForPlayer(currentPlayer.playerNumber);
        if (buyerHand != null && buyerHand.IsHandFull())
        {
            Debug.Log($"[CardDisplay] Mão cheia (máximo {buyerHand.maxCardsInHand} cartas)! Jogue cartas antes de comprar.");
            return;
        }

        // Em multiplayer, envia RPC — a compra executa nos DOIS clientes (inclusive este)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null && CardManager.Instance != null)
        {
            int shopIndex = CardManager.Instance.GetShopCardIndex(gameObject);
            if (shopIndex < 0)
            {
                Debug.LogError("[CardDisplay] Carta não encontrada na loja!");
                return;
            }
            PhotonGameManager.Instance.SendBuyCardRPC(shopIndex, currentPlayer.playerNumber);
            return;
        }

        // Modo offline: executa direto
        ExecuteBuy(currentPlayer.playerNumber);
    }

    // ========== VENDER CARTA (fase inicial) ==========

    // Taxa fixa da venda: o jogador recebe de volta (custo - 2), mínimo 0.
    // Ex.: carta de custo 3 devolve 1 de ouro. Só existe na fase inicial.
    public const int SellFee = 2;

    void TrySellCard()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.gameState != GameState.Lobby) return;

        // Vendedor = jogador LOCAL (fase inicial é simultânea, sem turnos)
        int seller = (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
            ? PhotonGameManager.Instance.myPlayerNumber
            : TurnManager.Instance.GetCurrentPlayer().playerNumber;

        if (ownerPlayerNumber != seller)
        {
            Debug.Log("[CardDisplay] Só é possível vender cartas da SUA mão!");
            return;
        }

        // Quem já clicou "Iniciar Partida" não mexe mais na mão
        bool alreadyReady =
            (seller == 1 && TurnManager.Instance.player1Ready) ||
            (seller == 2 && TurnManager.Instance.player2Ready);
        if (alreadyReady)
        {
            Debug.Log("[CardDisplay] Você já clicou em Iniciar Partida — não pode mais vender!");
            return;
        }

        // Em multiplayer vai por RPC: a carta é identificada pelo índice na mão
        // do vendedor (mãos são espelhadas nos 2 clientes)
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            HandManager sellerHand = GetHandManagerForPlayer(seller);
            int handIndex = sellerHand != null ? sellerHand.GetCardIndex(gameObject) : -1;
            if (handIndex < 0)
            {
                Debug.LogError("[CardDisplay] Carta não encontrada na mão para vender!");
                return;
            }
            PhotonGameManager.Instance.SendSellCardRPC(handIndex, seller);
            return;
        }

        // Modo offline: executa direto
        ExecuteSell(seller);
    }

    // Executa a venda de fato (chamado localmente em offline, ou via RPC nos
    // dois clientes): devolve (custo - 2) de ouro com o teto da fase inicial
    // (20) e libera 1 slot do limite de 5 compras
    public void ExecuteSell(int sellerPlayerNumber)
    {
        if (card == null || TurnManager.Instance == null) return;
        PlayerData sellerData = TurnManager.Instance.GetPlayer(sellerPlayerNumber);
        if (sellerData == null) return;

        int refund = Mathf.Max(0, card.GetGoldCost() - SellFee);
        if (refund > 0)
            sellerData.AddGold(refund, PlayerData.LobbyStartingGold);
        sellerData.cardsBoughtThisTurn = Mathf.Max(0, sellerData.cardsBoughtThisTurn - 1);

        HandManager sellerHand = GetHandManagerForPlayer(sellerPlayerNumber);
        if (sellerHand != null) sellerHand.RemoveCardFromHand(gameObject);

        SoundManager.Play(SoundManager.Sound.Buy);
        FloatingTextFX.Show(transform.position, $"VENDIDA! +{refund} ouro", FloatingTextFX.GoldColor, 5f);
        Debug.Log($"[CardDisplay] {sellerData.playerName} vendeu {card.cardName} (custo {card.GetGoldCost()}): +{refund} de ouro, 1 compra liberada. Ouro: {sellerData.gold}, compras: {sellerData.cardsBoughtThisTurn}/{PlayerData.MaxCardsInLobby}");

        Destroy(gameObject);
    }

    // ========== TRAVAR CARTA NA LOJA (não renova no refresh) ==========

    // Alterna a trava desta carta (clique com o modo "Travar cartas" ligado).
    // Em multiplayer vai por RPC: os 2 clientes marcam o MESMO slot, então o
    // refresh pula os mesmos sorteios dos dois lados (lockstep intacto)
    void TryToggleShopLock()
    {
        if (CardManager.Instance == null) return;
        int shopIndex = CardManager.Instance.GetShopCardIndex(gameObject);
        if (shopIndex < 0) return; // não está na SUA loja

        bool newState = !isLockedInShop;
        if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null)
        {
            // AllViaServer: volta para este cliente também, na ordem global
            PhotonGameManager.Instance.SendShopCardLockRPC(
                PhotonGameManager.Instance.myPlayerNumber, shopIndex, newState);
            return;
        }
        SetShopLock(newState); // offline: aplica direto
    }

    // Aplica a trava + visual (borda dourada e selo "TRAVADA")
    public void SetShopLock(bool locked)
    {
        if (isLockedInShop == locked) return;
        isLockedInShop = locked;
        UpdateCardDisplay(); // repinta a borda (dourada quando travada)
        UpdateShopLockLabel();
        if (card != null)
            Debug.Log($"[CardDisplay] {card.cardName}: {(locked ? "TRAVADA na loja (não renova)" : "destravada")}");
    }

    // Selo "« TRAVADA »" sobre a arte da carta enquanto ela estiver travada
    void UpdateShopLockLabel()
    {
        Transform t = transform.Find("ShopLockLabel");
        if (!isLockedInShop)
        {
            if (t != null) t.gameObject.SetActive(false);
            return;
        }

        if (t != null)
        {
            t.gameObject.SetActive(true);
            return;
        }

        GameObject go = new GameObject("ShopLockLabel");
        go.transform.SetParent(transform, false);
        // Sobre a arte (z -0.35), levemente acima da superfície da carta.
        // Rotação copiada de um texto existente: os TMP da carta têm a rotação
        // certa para "deitar" sobre a face dela
        go.transform.localPosition = new Vector3(0f, 0.02f, -0.35f);
        go.transform.localRotation = cardNameText != null
            ? cardNameText.transform.localRotation : Quaternion.identity;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "« TRAVADA »";
        tmp.fontSize = 2.2f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.77f, 0.32f);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 230);
        tmp.rectTransform.sizeDelta = new Vector2(3f, 0.5f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        // Por cima da arte da carta (mesmo truque dos textos flutuantes)
        if (tmp.fontMaterial != null) tmp.fontMaterial.renderQueue = 4000;
    }

    // Executa a compra de fato (chamado localmente em offline, ou via RPC nos dois clientes)
    public void ExecuteBuy(int buyerPlayerNumber)
    {
        PlayerData buyer = TurnManager.Instance.GetPlayer(buyerPlayerNumber);

        // Checa a mão ANTES de gastar ouro: se estiver cheia, a compra é abortada
        // nos DOIS clientes (mãos são espelhadas, então a decisão é a mesma)
        HandManager correctHandManager = GetHandManagerForPlayer(buyerPlayerNumber);
        if (correctHandManager == null)
        {
            Debug.LogError($"HandManager para {buyer.playerName} não encontrado!");
            return;
        }
        if (correctHandManager.IsHandFull())
        {
            Debug.Log($"[CardDisplay] Mão de {buyer.playerName} cheia — compra cancelada.");
            return;
        }

        // Neste cliente a carta pode estar na loja OCULTA do oponente: reativa
        // (na mão ela volta a existir para os dois, como verso para quem não é dono)
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Deduz o ouro e marca a compra do turno. Compra grátis (Healer 5): não
        // gasta ouro nem consome o limite de compras — decisão idêntica nos dois
        // clientes, pois freePurchases só muda dentro de RPCs
        int cost = DiscountedCost(card, buyer);
        if (buyer.freePurchases > 0)
        {
            buyer.freePurchases--;
            Debug.Log($"[CardDisplay] {buyer.playerName} usou a COMPRA GRÁTIS em {card.cardName}! (restam {buyer.freePurchases})");
        }
        else
        {
            buyer.BuyCard(cost);
            Debug.Log($"[CardDisplay] {buyer.playerName} comprou {card.cardName} por {cost} ouro. Ouro restante: {buyer.gold}");
        }
        MatchStatsTracker.RecordBought(card, buyerPlayerNumber); // telemetria de balanceamento
        SoundManager.Play(SoundManager.Sound.Buy);

        // Remove da loja e adiciona à mão DO JOGADOR CORRETO
        isInShop = false;
        isLockedInShop = false; // comprada: a trava (e o selo) somem
        UpdateShopLockLabel();
        ownerPlayerNumber = buyerPlayerNumber; // Define o dono da carta
        UpdateCardDisplay(); // Atualiza a borda com a cor do dono

        bool added = correctHandManager.AddCardToHand(gameObject);
        if (added)
        {
            isInHand = true;
            handManager = correctHandManager; // Atualiza a referência
            transform.localScale = Vector3.one * HandScale; // Tamanho da mão

            // Carta do OPONENTE: este cliente vê apenas o verso dela na mão
            if (PhotonNetwork.inRoom && PhotonGameManager.Instance != null &&
                buyerPlayerNumber != PhotonGameManager.Instance.myPlayerNumber)
            {
                SetFaceDown(true);
            }
        }
    }

    // Healer 3 (ATK 2, HP 3): enquanto ela estiver em campo, a PRIMEIRA compra
    // do turno do dono custa 2 a menos (mínimo 0). Determinístico nos 2
    // clientes: tabuleiro e cardsBoughtThisTurn são espelhados, e a compra
    // executa via RPC. Cópias não acumulam (o desconto é um só).
    public static int DiscountedCost(Card card, PlayerData buyer)
    {
        int cost = card != null ? card.GetGoldCost() : 0;
        if (buyer == null || BoardManager.Instance == null) return cost;
        if (buyer.cardsBoughtThisTurn > 0) return cost; // só a 1ª compra

        int discount = 0;

        // Healer 3 (2/3): -2 na primeira compra do turno
        foreach (var ally in BoardManager.Instance.GetCardsByOwner(buyer.playerNumber))
        {
            if (ally != null && ally.card != null && ally.card.cardClass == CardClass.Healer &&
                ally.card.tier == CardTier.Tier3 && ally.card.attack == 2 && ally.card.health == 3)
            {
                discount += 2;
                break;
            }
        }

        // Torre: Mercado Negro (-1 na primeira compra do turno) — soma com o Healer 3
        discount += TowerSystem.FirstBuyDiscount(buyer);

        if (discount > 0)
            Debug.Log($"[DiscountedCost] Desconto de {discount} na 1ª compra do turno ({cost} → {Mathf.Max(0, cost - discount)})");
        return Mathf.Max(0, cost - discount);
    }

    // Busca o HandManager correto para o jogador
    HandManager GetHandManagerForPlayer(int playerNum)
    {
        HandManager[] allHandManagers = FindObjectsOfType<HandManager>();
        foreach (HandManager hm in allHandManagers)
        {
            if (hm.playerNumber == playerNum)
            {
                return hm;
            }
        }
        return null;
    }

    // Encontra o tile atual onde a carta está posicionada
    CardTile FindCurrentTile()
    {
        // Primeiro tenta com raycast na direção para baixo
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        // Faz raycast para baixo com distância suficiente
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 10f))
        {
            CardTile tile = hit.collider.GetComponent<CardTile>();
            if (tile != null)
            {
                return tile;
            }
        }

        // Se não encontrar com raycast, busca o tile mais próximo
        CardTile[] allTiles = FindObjectsOfType<CardTile>();
        CardTile closestTile = null;
        float closestDistance = float.MaxValue;

        foreach (CardTile tile in allTiles)
        {
            // Calcula distância horizontal (ignora Y)
            Vector3 tilePos = tile.transform.position;
            Vector3 cardPos = transform.position;
            float distance = Vector2.Distance(
                new Vector2(tilePos.x, tilePos.z),
                new Vector2(cardPos.x, cardPos.z)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTile = tile;
            }
        }

        // Retorna o tile mais próximo se estiver a menos de 1.5 unidades
        if (closestTile != null && closestDistance < 1.5f)
        {
            return closestTile;
        }

        return null;
    }

    // ============ SISTEMA DE COMBATE ============

    // Retorna lista de cartas inimigas adjacentes (somente adjacentes ortogonais: cima, baixo, esquerda, direita)
    public System.Collections.Generic.List<CardDisplay> GetAdjacentEnemies()
    {
        System.Collections.Generic.List<CardDisplay> enemies = new System.Collections.Generic.List<CardDisplay>();

        if (!isOnBoard || currentTile == null)
        {
            Debug.Log($"{card.cardName}: Não está no tabuleiro ou currentTile é null");
            return enemies;
        }

        BoardManager boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
        {
            Debug.LogError("BoardManager não encontrado!");
            return enemies;
        }


        // Alcance: todas as cartas atacam a 1 casa nas 4 direções; MAGOS e
        // ARQUEIROS alcançam também 2 casas em linha reta (cruz estendida),
        // atirando por cima de qualquer carta que esteja na casa do meio.
        // A ordem é FIXA e as casas próximas vêm primeiro: determinístico nos
        // dois clientes e o ataque automático prefere o inimigo mais perto.
        bool longRange = card != null &&
            (card.cardClass == CardClass.Arqueiro || card.cardClass == CardClass.Mago);

        var directions = new System.Collections.Generic.List<int[]>
        {
            new int[] { -1, 0 },  // Cima
            new int[] { 1, 0 },   // Baixo
            new int[] { 0, -1 },  // Esquerda
            new int[] { 0, 1 }    // Direita
        };

        if (longRange)
        {
            directions.Add(new int[] { -2, 0 });  // Cima x2
            directions.Add(new int[] { 2, 0 });   // Baixo x2
            directions.Add(new int[] { 0, -2 });  // Esquerda x2
            directions.Add(new int[] { 0, 2 });   // Direita x2
        }

        foreach (int[] dir in directions)
        {
            int newRow = currentTile.row + dir[0];
            int newCol = currentTile.column + dir[1];

            CardTile adjacentTile = boardManager.GetTile(newRow, newCol);
            if (adjacentTile != null && adjacentTile.occupiedCard != null)
            {
                CardDisplay enemyCard = adjacentTile.occupiedCard.GetComponent<CardDisplay>();
                if (enemyCard != null)
                {

                    if (enemyCard.ownerPlayerNumber != ownerPlayerNumber && enemyCard.ownerPlayerNumber != 0)
                    {
                        enemies.Add(enemyCard);
                    }
                }
            }
        }

        return enemies;
    }

    // Ataca a primeira carta inimiga adjacente encontrada
    // Anima a investida do atacante em direção ao alvo + som de ataque.
    // Puramente visual; roda igual nos dois clientes (ambos executam o ataque).
    public void PlayAttackAnim(CardDisplay target)
    {
        if (target == null) return;
        CardAnimator.Get(gameObject).Lunge(target.transform.position);

        // A figura 3D dá o golpe junto com a investida da carta
        if (boardFigure != null)
        {
            FigureAnimator figAnim = boardFigure.GetComponent<FigureAnimator>();
            if (figAnim != null) figAnim.Strike();

            // Figura com rig (mago GLB): toca a animação de ataque de verdade
            FigureRiggedAnimator rigAnim = boardFigure.GetComponent<FigureRiggedAnimator>();
            if (rigAnim != null) rigAnim.Strike();
        }

        SoundManager.Play(SoundManager.Sound.Attack);
    }

    public bool AttackAdjacentEnemy()
    {

        // Verifica se pode atacar
        if (!CanAttackThisRound())
        {
            Debug.Log($"[Attack] {card.cardName} NÃO pode atacar (lastAttackedRound={lastAttackedRound}, currentRound={(TurnManager.Instance != null ? TurnManager.Instance.currentRound : -99)})");
            return false;
        }

        // Busca inimigos adjacentes
        System.Collections.Generic.List<CardDisplay> enemies = GetAdjacentEnemies();

        if (enemies.Count == 0)
        {
            Debug.Log($"[Attack] {card.cardName} não encontrou inimigos adjacentes (tile: {(currentTile != null ? $"({currentTile.row},{currentTile.column})" : "NULL")})");
            return false;
        }

        Debug.Log($"[Attack] {card.cardName} em ({currentTile.row},{currentTile.column}) vai atacar {enemies[0].card.cardName} (dono P{enemies[0].ownerPlayerNumber})");

        // Ataque com todos os efeitos de atacante (compartilhado com o clique)
        PerformAttackOn(enemies[0]);

        return true;
    }

    // Executa o ataque desta carta contra o alvo com TODOS os efeitos de
    // atacante (dano dobrado vs Tank, ignorar armadura, execução, ataque duplo
    // em Healer, mover de novo no ataque lateral). Compartilhado pelos DOIS
    // caminhos de ataque — tecla A (AttackAdjacentEnemy) e clique
    // (GameManager.ExecuteTargetedAttack). Antes os efeitos só existiam no
    // caminho da tecla A: no ataque por clique (o principal!) nenhum deles
    // funcionava.
    public void PerformAttackOn(CardDisplay target)
    {
        if (target == null || card == null) return;

        int damageDealt = currentAttack;

        // Rastreia quem atacou (para efeitos reativos e "ao matar")
        target.attackerCardDisplay = this;

        // Investida visual em direção ao alvo
        PlayAttackAnim(target);

        // Efeitos de Archer tier-5 antes do dano
        bool ignoreArmor = false;
        bool tryExecute = false;
        int modifiedDamage = damageDealt;

        if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                // (O dano dobrado contra Tank desceu para o tier 4 [5/2], com
                // cooldown — ver o bloco tier-4 abaixo. O 6/3 do tier 5 agora
                // é a carta de cópia ao matar, cujo gancho fica em DestroyCard)

                // Efeito 2: ignora armadura se tem Tank aliado (ATK 6, HP 4)
                if (card.attack == 6 && card.health == 4 && effect.ShouldIgnoreArmor_Tier5Effect2())
                {
                    ignoreArmor = true;
                    Debug.Log($"[ArcherTier5Effect2] {card.cardName}: Ignorando armadura do inimigo!");
                }

                // Efeito 3: ignora armadura + executa com vida <= 2 (ATK 5, HP 4)
                if (card.attack == 5 && card.health == 4)
                {
                    ignoreArmor = true;
                    tryExecute = true;
                    Debug.Log($"[ArcherTier5Effect3] {card.cardName}: Ignorando armadura do inimigo!");
                }
            }
        }

        // Efeitos de Archer tier-4
        bool doubleAttackOnHealer = false;
        if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier4)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                // Efeito 1: ataque duplo se o alvo é Healer (ATK 5, HP 3).
                // BUGFIX: contra alvos que NÃO eram Healer a carta pulava o
                // dano normal e o efeito não fazia nada = ataque sem dano
                if (card.attack == 5 && card.health == 3 && target.card.cardClass == CardClass.Healer)
                {
                    doubleAttackOnHealer = true;
                    effect.ActivateDoubleAttackHealer(target);
                }

                // Efeito 4: move de novo ao atacar alvo adjacente (ATK 4, HP 3)
                if (card.attack == 4 && card.health == 3)
                    effect.CheckSideAttackAndMove(target);

                // Efeito 5 (desceu do tier 5): dano em DOBRO contra Tank
                // (ATK 5, HP 2) — vale para 1 ataque e só 1 vez a cada 2
                // turnos: procou no round R, volta a valer no round R+2
                if (card.attack == 5 && card.health == 2 &&
                    target.card.cardClass == CardClass.Tank &&
                    TurnManager.Instance != null &&
                    (doubleVsTankLastUsedRound < 0 ||
                     TurnManager.Instance.currentRound >= doubleVsTankLastUsedRound + 2))
                {
                    modifiedDamage = damageDealt * 2;
                    doubleVsTankLastUsedRound = TurnManager.Instance.currentRound;
                    FloatingTextFX.ShowAboveCard(this, "DANO EM DOBRO!", FloatingTextFX.EffectColor, 4.2f);
                    Debug.Log($"[ArcherTier4Effect5] {card.cardName}: Dano em dobro contra Tank! (recarrega em 2 turnos)");
                }
            }
        }

        // Auras de ATAQUE dos tanks (Capitão de Ferro +1 na frente, Baluarte
        // +2 com combo) — v4.2, no lugar das antigas reduções de dano
        modifiedDamage += AuraAttackBonus();

        // DEVOÇÃO — Matilha dos Arqueiros (3+/5 arqueiros jogados da mão):
        // degrau 1 = +1 ATK contra alvo já ferido; degrau 2 = quebra 1 de
        // escudo do alvo antes do golpe. Determinístico (estado do tabuleiro)
        if (card.cardClass == CardClass.Arqueiro && ownerPlayerNumber != 0)
        {
            int matilha = ClassDevotion.TierOf(ownerPlayerNumber, CardClass.Arqueiro);
            if (matilha >= 1 && target.currentHealth < target.card.health + target.maxHealthBonus)
            {
                modifiedDamage += 1;
                Debug.Log($"[Devoção/Matilha] {card.cardName}: +1 ATK contra alvo ferido ({modifiedDamage})");
            }
            if (matilha >= 2 && target.currentShield > 0)
            {
                target.currentShield -= 1;
                Debug.Log($"[Devoção/Matilha] {card.cardName}: quebrou 1 de escudo de {target.card.cardName}");
            }
        }

        // PONTA PERFURANTE (v4.3): a linha anti-tank do early/mid game —
        // Sanguinária T1 (2/3), Couraçada T3 (4/3) e Sabotadora T3 (3/2)
        // quebram 1 de armadura extra do alvo antes do golpe. Contra alvo sem
        // escudo não faz nada; empilha com a Matilha (degrau 2 = 2 de escudo).
        // Determinístico (só estado do tabuleiro), roda no fluxo do RPC de ataque
        if (card.cardClass == CardClass.Arqueiro && target.currentShield > 0 &&
            ((card.tier == CardTier.Tier1 && card.attack == 2 && card.health == 3) ||
             (card.tier == CardTier.Tier3 && card.attack == 4 && card.health == 3) ||
             (card.tier == CardTier.Tier3 && card.attack == 3 && card.health == 2)))
        {
            target.currentShield -= 1;
            FloatingTextFX.ShowAboveCard(target, "PERFUROU! -1 ARMADURA", FloatingTextFX.EffectColor, 3.6f);
            Debug.Log($"[PontaPerfurante] {card.cardName}: quebrou 1 de armadura de {target.card.cardName}");
        }

        // Tile do alvo capturado ANTES do dano — se o alvo morrer, o tile é
        // liberado e o respingo do Mago 3 (3/5) perderia a referência
        CardTile targetTileForSplash = target.currentTile;

        // Dano normal (o ataque duplo em Healer já aplicou o dano dele).
        // "Ignorar armadura" agora passa pelo TakeDamage com a flag
        // ignoreArmorNextDamage: pula SÓ o escudo, mas respeita
        // invulnerabilidade, esquiva, intercepto e os gatilhos reativos
        // (antes ignorava o fluxo INTEIRO e matava até carta invulnerável)
        if (!doubleAttackOnHealer)
        {
            if (ignoreArmor) target.ignoreArmorNextDamage = true;
            target.TakeDamage(modifiedDamage);
        }

        // Mago 3 (3/5): RESPINGO — o ataque também causa 1 de dano aos
        // inimigos adjacentes ao alvo (identidade de área dos magos, v4.1)
        if (card.cardClass == CardClass.Mago && card.tier == CardTier.Tier3 &&
            card.attack == 3 && card.health == 5)
        {
            CardEffectSimple splashFx = GetComponent<CardEffectSimple>();
            if (splashFx != null)
                splashFx.SplashDamageAroundTile(targetTileForSplash, 1, "MageTier3Effect2");
        }

        // Execução do Archer 5 (5/4): alvo sobrevivente com vida <= 2 morre
        if (tryExecute && target != null && target.currentHealth > 0)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.CheckArcherTier5Effect3_Execute(target);
        }

        // (Os efeitos "ao matar" — cópia do Archer 4 e armadura do Tank 5 —
        // disparam dentro de DestroyCard, valendo para todos os caminhos)

        // Marca que atacou neste round (ou consome o 2º ataque da aura do Tank 4)
        MarkAttackUsed();
    }

    // Cura a carta
    public void Heal(int amount, CardDisplay source = null)
    {
        // Projétil verde de cura saindo de quem curou (visual)
        if (source != null && source != this && isOnBoard && source.isOnBoard)
            EffectProjectileFX.Launch(source, this, EffectProjectileFX.HealGreen);

        int hpBefore = currentHealth;

        // Cura sobe a vida até o máximo (base + bônus do Healer 2), mas NUNCA
        // reduz. BUGFIX: o clamp antigo (currentHealth += amount; clampa para o
        // máximo) CORTAVA a vida de aliados que estavam ACIMA do máximo base
        // por buffs (+5 HP do Tank 4, +3 do Healer 4...) — a "cura" do Healer 1
        // parecia DAR DANO nesses aliados. Max(hpBefore, ...) impede a redução.
        int maxHp = card.health + maxHealthBonus;
        currentHealth = Mathf.Max(hpBefore, Mathf.Min(hpBefore + amount, maxHp));

        int healedAmount = currentHealth - hpBefore;
        MatchStatsTracker.RecordHealing(source, healedAmount); // telemetria de cura
        UpdateCardDisplay();

        // Gatilhos "quando curado" só disparam se a cura curou de verdade
        // (alvo de vida cheia não gera mais ouro/bônus do nada)
        if (healedAmount <= 0 || ownerPlayerNumber == 0) return;

        // Torre: Vínculo Sagrado (torre recupera 1 quando um aliado é curado)
        TowerSystem.OnAllyHealed(ownerPlayerNumber);

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(ownerPlayerNumber);

        // Healer 1* (1/2): ganha 1 de ouro quando um aliado é curado
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Healer &&
                ally.card.attack == 1 && ally.card.health == 2)
            {
                if (!DuplicateEffectGate.TryActivate(ally)) continue;
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    effect.OnAllyHealed();
                }
            }
        }

        // Canalizador — Mago T4 (4/6), v4.3: +1 ATK sempre que um aliado é
        // curado (máx. +5). Era "+1 quando healer ENTRA em campo" — dependia
        // de compras futuras e quase nunca disparava (2 compras, 33% WR)
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Mago &&
                ally.card.attack == 4 && ally.card.health == 6 &&
                ally.card.tier == CardTier.Tier4)
            {
                if (!DuplicateEffectGate.TryActivate(ally)) continue;
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect != null) effect.ActivateBoostOnAllyHealed();
            }
        }

        // Tanks "quando curado": só quando ESTA carta (a que foi curada) é o
        // tank. BUGFIX: antes o bônus disparava quando QUALQUER aliado era
        // curado — com um healer periódico os tanks inflavam sem limite
        if (card.cardClass == CardClass.Tank)
        {
            CardEffectSimple selfFx = GetComponent<CardEffectSimple>();
            if (selfFx != null)
            {
                // Tank 2: +1 todos atributos quando curado
                if (card.attack == 1 && card.shield == 1 && card.health == 5)
                {
                    if (DuplicateEffectGate.TryActivate(this))
                        selfFx.TankEffect2_BoostOnHeal();
                }
                // Tank 3: +1 ataque quando ganhar vida
                else if (card.attack == 0 && card.shield == 2 && card.health == 4)
                {
                    if (DuplicateEffectGate.TryActivate(this))
                        selfFx.TankEffect3_AttackOnHeal();
                }
            }
        }

        // Healer 2 (1/3, tier 2): aumenta a vida máxima DO ALIADO CURADO em +1.
        // BUGFIX: antes aumentava a vida máxima da própria Healer 2 e a curava
        // por completo a cada cura de qualquer aliado; e sem o gate de tier, a
        // Healer 3 (2/1) pegava o efeito de carona
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Healer &&
                ally.card.attack == 1 && ally.card.health == 3 &&
                ally.card.tier == CardTier.Tier2)
            {
                if (!DuplicateEffectGate.TryActivate(ally)) continue;
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect != null)
                {
                    effect.HealerTier2Effect1_OnAllyHealed(this);
                }
            }
        }
    }

    // Recebe dano (primeiro absorve no escudo, depois na vida)
    public void TakeDamage(int damage)
    {
        // Consome o atacante DESTA pancada. O campo é setado pelos caminhos de
        // ataque logo antes de TakeDamage; consumir aqui garante que dano de
        // efeito (sem atacante) não dispare ganchos "contra o atacante" usando
        // um atacante antigo de outro ataque
        CardDisplay attacker = attackerCardDisplay;
        attackerCardDisplay = null;

        // Consome a flag "ignorar armadura" (Archer 5): pula só o escudo,
        // mas o resto do fluxo (invulnerabilidade, esquiva, intercepto,
        // gatilhos reativos) roda normalmente
        bool skipShield = ignoreArmorNextDamage;
        ignoreArmorNextDamage = false;

        // Invulnerabilidade (Healer 4, dura 3 ROUNDS): nega qualquer dano
        if (invulnerableRoundsLeft > 0)
        {
            Debug.Log($"[Invulnerável] {card.cardName} está invulnerável e negou o dano!");
            return;
        }

        // Se o efeito de árvore está ativo, nega o dano
        if (treeDefenseActive)
        {
            Debug.Log($"[TreeDefense] {card.cardName} nega o dano com a árvore!");
            return;
        }

        // Tank 4 tier-4 (ATK 2, Shield 7, HP 7) - Intercepta ATAQUE 1x por turno
        // (attacker != null: dano de efeito não é "um ataque").
        // Protege QUALQUER aliado, inclusive outros tanks — e roda ANTES das
        // reduções do alvo: quem intercepta aplica as PRÓPRIAS defesas.
        if (ownerPlayerNumber != 0 && attacker != null)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 2 && ally.card.shield == 7 && ally.card.health == 7 &&
                        ally.card.tier == CardTier.Tier4)
                    {
                        // Guarda-costas de verdade: só intercepta se estiver
                        // COLADO na carta atacada (até 1 casa) E se o protegido
                        // estiver do lado ou ATRÁS dele — quem passa na frente
                        // do escudo fica sem escolta
                        if (!IsNextTo(ally, this)) continue;
                        if (!IsBesideOrBehind(ally, this)) continue;
                        if (!DuplicateEffectGate.TryActivate(ally)) continue;
                        // Verifica se pode interceptar (1x por turno)
                        if (TurnManager.Instance != null &&
                            ally.tankTier4Effect2LastUsedRound < TurnManager.Instance.currentRound)
                        {
                            CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                            if (effect != null)
                            {
                                // v4.2: o desconto de 25% SAIU — o Quebra-Golpes
                                // intercepta tomando o dano CHEIO (a identidade
                                // de guarda-costas fica, o jogo destrava)
                                Debug.Log($"[TankTier4Effect2] {ally.card.cardName}: Interceptou ataque em lugar de {card.cardName}!");
                                // Marca o uso ANTES de aplicar o dano; TakeRedirectedDamage
                                // aplica as defesas do próprio tank sem disparar novos
                                // redirecionamentos em cadeia
                                ally.tankTier4Effect2LastUsedRound = TurnManager.Instance.currentRound;
                                FloatingTextFX.ShowAboveCard(ally, "INTERCEPTOU!", FloatingTextFX.EffectColor, 4.2f);
                                ally.TakeRedirectedDamage(damage, attacker, false);
                                return; // Tank recebeu o ataque
                            }
                        }
                    }
                }
            }
        }

        // v4.2: as reduções de dano dos tanks saíram (ver AuraAttackBonus) —
        // a chamada fica como ponto de extensão e o flag hoje é inerte
        bool halvedOnce = false;
        damage = ApplyTankDamageReductions(damage, ref halvedOnce);

        // Healer atacado: o Tank 1/3/5 (fora da tríade) ainda pode assumir o dano.
        // (Os efeitos de tríade — Mago 2/4 ganhar +1 ATK e Tank 1/2/4 interceptar —
        // foram removidos: os membros de tríade só têm o efeito da tríade.)
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            if (TryTankAssumeAnyDamage(damage, attacker, halvedOnce))
                return; // Decisão pendente: o dano é resolvido no callback
        }

        // (A cura do Healer 3 [2/4] quando um Tank leva dano agora dispara
        // DEPOIS do dano ser aplicado — ver TriggerHealerCureOnTankDamaged().
        // Antes curava um tank ainda com vida cheia = cura sempre desperdiçada)

        // Triggers para Mago tier-2 quando Tank é atacado
        if (card.cardClass == CardClass.Tank && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // (O Mago 2 ATK 2/HP 3 perdeu o solo da bola de fogo — a carta
                // agora é só tríade, então o gancho foi removido)

                // (O Tank 2 [1/3/3] NÃO protege outros Tanks — o gancho aqui
                // fazia esse 1/3/3 frágil morrer comendo pancada no lugar de
                // tanks parrudos; a especificação dele é defender Arqueiros)

                // Tank tier 2 (ATK 1, Shield 3, HP 5) pode assumir o dano (o dono escolhe)
                if (TryTankAssumeAnyDamage(damage, attacker, halvedOnce))
                    return; // Decisão pendente: o dano é resolvido no callback
            }
        }

        // Arqueiro atacado: o Tank 1/3/5 (fora da tríade) ainda pode assumir o dano.
        // (Mago 3/3 congelar o atacante e Tank 1/3/3 interceptar foram removidos —
        // os membros de tríade só têm o efeito da tríade.)
        if (card.cardClass == CardClass.Arqueiro && ownerPlayerNumber != 0)
        {
            if (TryTankAssumeAnyDamage(damage, attacker, halvedOnce))
                return; // Decisão pendente: o dano é resolvido no callback
        }

        // Mago atacado: o Tank 1/3/5 (fora da tríade) ainda pode assumir o dano.
        // (Tank 0/3/4 interceptar foi removido — os membros de tríade só têm o
        // efeito da tríade.)
        if (card.cardClass == CardClass.Mago && ownerPlayerNumber != 0)
        {
            if (TryTankAssumeAnyDamage(damage, attacker, halvedOnce))
                return; // Decisão pendente: o dano é resolvido no callback
        }

        // Verifica se é um Healer sendo atacado e há Archer 2 (ATK 3, HP 2) que pode parar o ataque.
        // Só em ATAQUES de verdade (attacker != null): dano de efeito não é "um ataque"
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0 && attacker != null)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Arqueiro &&
                        ally.card.attack == 3 && ally.card.health == 3 &&
                        ally.card.tier == CardTier.Tier2 &&
                        !ally.archerShieldArrowUsed)
                    {
                        if (!DuplicateEffectGate.TryActivate(ally)) continue;
                        // Decisão sincronizada: o dono da carta defendida escolhe.
                        // BUGFIX: a flecha stuna o ATACANTE (antes passava "this"
                        // e stunava o próprio healer aliado que levou o dano)
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        CardDisplay atk = attacker;
                        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
                            "Um Archer pode parar este ataque!",
                            "Parar ataque", "Não parar",
                            accepted =>
                            {
                                bool blocked = accepted && effect != null &&
                                    effect.ArcherTier2Effect2_ShieldArrow(atk);
                                if (!blocked) ApplyDamageNormally(damage, atk);
                            });
                        return;
                    }
                }
            }
        }

        // (O Archer 2/2 é membro de tríade: o solo de "stunar o atacante ao
        // receber ataque" foi removido — só tem o efeito da tríade.)

        // Verifica se há uma Healer 1* (0/4, tier 1, "anule um ATAQUE a cada 3
        // turnos") aliada que pode bloquear. BUGFIX: sem o filtro de tier, a
        // Healer 3 do ouro (também 1/2) bloqueava ataques SEM cooldown nenhum;
        // e sem o gate de attacker, bloqueava até dano de efeito
        if (ownerPlayerNumber != 0 && attacker != null)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer &&
                        ally.card.attack == 0 && ally.card.health == 4 &&
                        ally.card.tier == CardTier.Tier1)
                    {
                        if (!DuplicateEffectGate.TryActivate(ally)) continue;
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null && effect.CanBlockAttackThisTurn())
                        {
                            ShowBlockAttackPopup(damage, effect, attacker);
                            return; // Pausa o dano enquanto aguarda resposta
                        }
                    }
                }
            }
        }

        // Se é o Archer 1 da árvore (3/2, tier 1), o efeito não foi usado e
        // ainda não mostrou popup neste turno (tier gate: a tríade Archer 2 ←
        // também é 3/2 e ganhava o popup da árvore por engano)
        if (card.cardClass == CardClass.Arqueiro && card.attack == 3 && card.health == 2 &&
            card.tier == CardTier.Tier1 &&
            !treeDefenseUsed && !treeDefensePopupShown)
        {
            // Mostra popup perguntando se quer ativar o efeito
            treeDefensePopupShown = true;
            ShowTreeDefensePopup(damage, attacker);
            return; // Pausa o dano enquanto aguarda resposta
        }

        // Telemetria: dano EFETIVO = escudo consumido + vida perdida (sem contar
        // overkill). BUGFIX: este caminho (o principal, de TODO ataque direto)
        // nunca registrava — só o ApplyDamageNormally (danos adiados por popup)
        // registrava, então o "Mais dano" do dashboard via só uma fração do real
        int shieldPart = skipShield ? 0 : Mathf.Min(currentShield, damage);
        int effectiveDamage = shieldPart + Mathf.Max(0, Mathf.Min(currentHealth, damage - shieldPart));

        // Primeiro o escudo absorve o dano (a menos que a flecha ignore armadura)
        if (!skipShield && currentShield > 0)
        {
            int shieldAbsorbed = Mathf.Min(currentShield, damage);
            currentShield -= shieldAbsorbed;
            damage -= shieldAbsorbed;
        }

        // O dano que sobrou vai para a vida
        if (damage > 0)
        {
            currentHealth -= damage;
        }

        MatchStatsTracker.RecordDamage(attacker != null ? attacker : MatchStatsTracker.EffectSource,
                                       this, effectiveDamage);

        // Atualiza a UI
        UpdateCardDisplay();

        // Tank tier-5 efeito 2 (ATK 3, Shield 7, HP 10): +1 ATK ao receber dano
        if (card.cardClass == CardClass.Tank && card.attack == 3 && card.shield == 7 && card.health == 10 &&
            card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateAttackBoostOnDamage_Tier5Effect2();
        }

        // Tank tier-5 efeito 3 (ATK 3, Shield 7, HP 11): +1 ATK ao receber dano, +armadura se tem Healer/Mago
        if (card.cardClass == CardClass.Tank && card.attack == 3 && card.shield == 7 && card.health == 11 &&
            card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateAttackBoostOnDamage_Tier5Effect3();
        }

        // Senhor da Guerra — Tank 5 (3/8/9), v4.3: SOBREVIVEU a um ATAQUE →
        // +1 armadura a todos os aliados (+1 ATK a magos/arqueiros). Só
        // ataques (attacker != null) — dano de efeito não conta
        if (attacker != null && currentHealth > 0 &&
            card.cardClass == CardClass.Tank && card.attack == 3 && card.shield == 8 && card.health == 9 &&
            card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateWarlordOnSurvive();
        }

        // Se um Healer toma dano, aplica efeito do Mago
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            ApplyMageEffect();
        }

        // Healer 3 (2/4): cura o Tank em 3 DEPOIS do dano aplicado
        TriggerHealerCureOnTankDamaged();

        // Verifica se a carta morreu (o atacante volta ao campo para os
        // efeitos "ao matar" dentro de DestroyCard)
        if (currentHealth <= 0)
        {
            attackerCardDisplay = attacker;
            DestroyCard();
        }
    }

    // Healer 3 (ATK 2, HP 4): "sempre que um tanque receber dano, cure em 1".
    // Roda DEPOIS do dano ser aplicado de verdade (antes rodava antes do dano:
    // num tank de vida cheia a cura era 100% desperdiçada)
    public void TriggerHealerCureOnTankDamaged()
    {
        if (card == null || card.cardClass != CardClass.Tank || ownerPlayerNumber == 0) return;
        if (currentHealth <= 0) return; // morreu: não há o que curar

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(ownerPlayerNumber);
        foreach (var ally in allies)
        {
            if (ally != null && ally != this && ally.card.cardClass == CardClass.Healer &&
                ally.card.attack == 2 && ally.card.health == 4 && ally.card.tier == CardTier.Tier3)
            {
                if (!DuplicateEffectGate.TryActivate(ally)) continue;
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect != null)
                    effect.HealerTier3Effect2_CureTankWhenDamaged(this);
            }
        }
    }

    void ShowBlockAttackPopup(int damage, CardEffectSimple healerEffect, CardDisplay attacker)
    {
        // Decisão sincronizada: o dono da carta defendida escolhe
        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
            "Uma Healer pode bloquear este ataque!",
            "Bloquear ataque", "Não bloquear",
            accepted =>
            {
                if (accepted) ActivateBlockAttack(damage, healerEffect);
                else ApplyDamageNormally(damage, attacker);
            });
    }

    void ShowTreeDefensePopup(int damage, CardDisplay attacker)
    {
        // Decisão sincronizada: o dono do Archer escolhe
        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
            $"{card.cardName} pode usar a árvore para esquivar o dano!",
            "Ativar efeito", "Não usar",
            accepted =>
            {
                if (accepted) ActivateTreeDefense(damage);
                else ApplyDamageNormally(damage, attacker);
            });
    }

    void ActivateBlockAttack(int damage, CardEffectSimple healerEffect)
    {
        healerEffect.ActivateBlockAttack();
        FloatingTextFX.ShowAboveCard(this, "ATAQUE BLOQUEADO!", FloatingTextFX.EffectColor, 4.2f);
        Debug.Log($"[BlockAttack] Ataque bloqueado pela Healer!");
    }

    void ActivateTreeDefense(int damage)
    {
        treeDefenseActive = true;
        treeDefenseUsed = true;
        FloatingTextFX.ShowAboveCard(this, "ESQUIVOU NA ÁRVORE!", FloatingTextFX.EffectColor, 4.2f);
        // Refresh visual NA HORA: o dano é negado (nada mais redesenha a
        // carta), então sem esta chamada o selo "NA ÁRVORE" nunca aparecia
        UpdateDisplay();
        Debug.Log($"[TreeDefense] {card.cardName} ativou o efeito! Esquivando dano neste turno.");
    }

    // Tank tier 2 (ATK 1, Shield 3, HP 5): pode assumir o dano de um aliado
    // ADJACENTE atacado (até 1 casa — escolta tem que andar junto do time)
    // que esteja DO LADO ou ATRÁS dele (quem passa na frente fica sem escolta).
    // O dono escolhe via popup sincronizado. Retorna true se a decisão ficou
    // pendente. alreadyHalved: o dano já levou uma redução — não soma outra.
    bool TryTankAssumeAnyDamage(int damage, CardDisplay attacker, bool alreadyHalved = false)
    {
        // Só ATAQUES podem ser assumidos ("caso qualquer carta seja atacada")
        if (attacker == null) return false;

        BoardManager board = BoardManager.Instance;
        if (board == null) return false;

        var allies = board.GetCardsByOwner(ownerPlayerNumber);
        foreach (var ally in allies)
        {
            // ally != this: o Tank não pode "assumir" o próprio dano (causava loop infinito)
            if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                ally.card.attack == 1 && ally.card.shield == 3 && ally.card.health == 5)
            {
                // Precisa estar COLADO no aliado atacado para se jogar na frente,
                // e o protegido tem que estar do lado ou ATRÁS do tank
                if (!IsNextTo(ally, this)) continue;
                if (!IsBesideOrBehind(ally, this)) continue;
                if (!DuplicateEffectGate.TryActivate(ally)) continue;
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect == null) continue;

                CardDisplay tank = ally;
                PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
                    $"{tank.card.cardName} pode assumir o dano de {card.cardName}!",
                    "Assumir dano", "Não assumir",
                    accepted =>
                    {
                        if (accepted)
                        {
                            FloatingTextFX.ShowAboveCard(tank, "ASSUMIU O DANO!", FloatingTextFX.EffectColor, 4.2f);
                            effect.TankTier2Effect4_TakeAnyAttack(this, damage, attacker, alreadyHalved);
                        }
                        else ApplyDamageNormally(damage, attacker);
                    });
                return true;
            }
        }
        return false;
    }

    // Reduções de dano dos Tanks, compartilhadas por TakeDamage e pelo dano
    // v4.2: as REDUÇÕES DE DANO dos tanks SAÍRAM por completo (o jogo travava
    // — pedido do Carlos). O Capitão de Ferro e o Baluarte viraram cartas de
    // PRESSÃO (bônus de ataque — ver AuraAttackBonus). A assinatura e os
    // chamadores ficam (fluxos de redirecionamento intactos); alreadyHalved
    // hoje não faz nada, mantido só para não mexer em todas as assinaturas.
    int ApplyTankDamageReductions(int damage, ref bool alreadyHalved)
    {
        return damage;
    }

    // Bônus de ATAQUE por auras dos tanks (v4.2 — no lugar das reduções):
    // · Capitão de Ferro (2/3/6 T3): tanks aliados na LINHA DE FRENTE atacam
    //   com +1 (o Capitão também precisa estar na frente, liderando o avanço)
    // · Baluarte (3/7/8 T4): com Healer+Mago+Arqueiro em campo, ataca com +2
    // Vale para ataques em CARTAS e na TORRE (os dois caminhos somam isto)
    public int AuraAttackBonus()
    {
        if (card == null || card.cardClass != CardClass.Tank || ownerPlayerNumber == 0)
            return 0;

        int bonus = 0;

        BoardManager board = BoardManager.Instance;
        if (board != null && IsOnFrontLines(this))
        {
            foreach (var ally in board.GetCardsByOwner(ownerPlayerNumber))
            {
                if (ally == null || ally.card == null) continue;
                if (ally.card.cardClass != CardClass.Tank) continue;
                if (!(ally.card.attack == 2 && ally.card.shield == 3 && ally.card.health == 6 &&
                      ally.card.tier == CardTier.Tier3)) continue;
                if (!IsOnFrontLines(ally)) continue;    // a aura emana da frente
                if (!DuplicateEffectGate.TryActivate(ally)) continue;

                bonus += 1;
                Debug.Log($"[TankTier3Effect2] {card.cardName}: +1 de dano pela aura do Capitão de Ferro");
                break;
            }
        }

        if (card.attack == 3 && card.shield == 7 && card.health == 8 &&
            card.tier == CardTier.Tier4)
        {
            CardEffectSimple selfEffect = GetComponent<CardEffectSimple>();
            if (selfEffect != null && selfEffect.TankTier4Effect4_HasCombo())
            {
                bonus += 2;
                Debug.Log($"[TankTier4Effect4] {card.cardName}: +2 de dano (combo Healer+Mago+Arqueiro)");
            }
        }

        return bonus;
    }

    // Dano redirecionado/assumido/interceptado por um Tank: aplica as defesas
    // do PRÓPRIO tank (invulnerabilidade e reduções) antes de aplicar — os
    // redirecionamentos antigos pulavam essas defesas e o tank tomava cheio.
    // alreadyHalved: o dano já levou uma redução no caminho (intercepto com
    // healer ou aura na vítima) — as reduções daqui não somam outra
    public void TakeRedirectedDamage(int damage, CardDisplay attacker = null, bool alreadyHalved = false)
    {
        if (invulnerableRoundsLeft > 0)
        {
            Debug.Log($"[Invulnerável] {card.cardName} negou o dano redirecionado!");
            return;
        }

        bool halved = alreadyHalved;
        damage = ApplyTankDamageReductions(damage, ref halved);
        ApplyDamageNormally(damage, attacker);
    }

    public void ApplyDamageNormally(int damage, CardDisplay attacker = null)
    {
        // DEVOÇÃO — Falange dos Tanks (degrau 1): ATAQUES causam -1 (mín. 1).
        // Dano de efeito (attacker nulo) passa cheio
        if (attacker != null)
            damage = ClassDevotion.ReduceAttackDamageOnTank(this, damage);

        // Telemetria: dano EFETIVO = escudo consumido + vida perdida (sem contar
        // overkill). A fonte é o atacante ou, em dano de efeito, o EffectSource
        int effectiveDamage = Mathf.Min(currentShield, damage)
                            + Mathf.Max(0, Mathf.Min(currentHealth, damage - currentShield));

        // Primeiro o escudo absorve o dano
        if (currentShield > 0)
        {
            int shieldAbsorbed = Mathf.Min(currentShield, damage);
            currentShield -= shieldAbsorbed;
            damage -= shieldAbsorbed;
        }

        // O dano que sobrou vai para a vida
        if (damage > 0)
        {
            currentHealth -= damage;
        }

        MatchStatsTracker.RecordDamage(attacker != null ? attacker : MatchStatsTracker.EffectSource,
                                       this, effectiveDamage);

        // Atualiza a UI
        UpdateCardDisplay();

        // Tanks tier-5: +1 ATK ao receber dano — estes ganchos só existiam no
        // TakeDamage, então dano redirecionado/adiado por popup não disparava
        if (card.cardClass == CardClass.Tank && card.tier == CardTier.Tier5)
        {
            CardEffectSimple tankFx = GetComponent<CardEffectSimple>();
            if (tankFx != null)
            {
                if (card.attack == 3 && card.shield == 7 && card.health == 10)
                    tankFx.ActivateAttackBoostOnDamage_Tier5Effect2();
                else if (card.attack == 3 && card.shield == 7 && card.health == 11)
                    tankFx.ActivateAttackBoostOnDamage_Tier5Effect3();

                // Senhor da Guerra (3/8/9), v4.3: sobreviveu a um ATAQUE
                // (dano adiado/redirecionado também conta — attacker viaja junto)
                if (attacker != null && currentHealth > 0 &&
                    card.attack == 3 && card.shield == 8 && card.health == 9)
                    tankFx.ActivateWarlordOnSurvive();
            }
        }

        // Se um Healer toma dano, aplica efeito do Mago
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            ApplyMageEffect();
        }

        // Healer 3 (2/4): cura o Tank em 2 DEPOIS do dano aplicado (este caminho
        // cobre dano interceptado/redirecionado, que antes nunca disparava a cura)
        TriggerHealerCureOnTankDamaged();

        // DEVOÇÃO — Falange (degrau 2): atacante corpo a corpo leva 1 de volta
        // (entra com attacker nulo lá dentro — sem correntes de reflexo)
        ClassDevotion.TryReflect(this, attacker);

        // Verifica se a carta morreu (repassa o atacante para os efeitos "ao matar")
        if (currentHealth <= 0)
        {
            attackerCardDisplay = attacker;
            DestroyCard();
        }
    }

    // Destrói a carta
    public CardDisplay attackerCardDisplay = null; // Rastreia quem atacou essa carta
    [System.NonSerialized] public bool ignoreArmorNextDamage = false; // Archer 5: próximo TakeDamage pula o escudo

    public void DestroyCard()
    {
        // Telemetria: kill do atacante (se houver) + morte desta carta
        MatchStatsTracker.RecordKill(attackerCardDisplay, this);

        // Efeitos "ao matar" do atacante — centralizados AQUI para valerem em
        // TODOS os caminhos de ataque (clique, tecla A, dano adiado por popup).
        // Antes o Archer 4 (5/2) só copiava no caminho da tecla A.

        // (O Archer 3/2 é membro de tríade: o solo de "invocar um Archer ao matar"
        // foi removido — só tem o efeito da tríade.)

        // Archer 5 (ATK 6, HP 3, subiu do tier 4): cria cópia de si ao matar
        // (+ move de novo se tem Tank)
        if (attackerCardDisplay != null &&
            attackerCardDisplay.card.cardClass == CardClass.Arqueiro &&
            attackerCardDisplay.card.attack == 6 &&
            attackerCardDisplay.card.health == 3 &&
            attackerCardDisplay.card.tier == CardTier.Tier5)
        {
            CardEffectSimple killerFx = attackerCardDisplay.GetComponent<CardEffectSimple>();
            if (killerFx != null) killerFx.ActivateCopyOnKill();
        }

        // (Senhor da Guerra — Tank 5 3/8/9 — v4.3: o gancho "ao matar" virou
        // "sobreviver a ataque", nos caminhos de dano TakeDamage/ApplyDamageNormally)

        // Inquisidora — Archer 4 (ATK 5, HP 3): matou um HEALER → pode se
        // mover de novo. Fica AQUI (e não na checagem logo após as flechadas)
        // porque o dano pode ser ADIADO por popup (anular ataque, tank
        // assumindo/interceptando) — nesses casos o healer "ainda estava vivo"
        // na checagem antiga e o bônus se perdia em silêncio
        if (attackerCardDisplay != null &&
            card != null && card.cardClass == CardClass.Healer &&
            attackerCardDisplay.card.cardClass == CardClass.Arqueiro &&
            attackerCardDisplay.card.attack == 5 &&
            attackerCardDisplay.card.health == 3 &&
            attackerCardDisplay.card.tier == CardTier.Tier4)
        {
            attackerCardDisplay.lastMovedRound = -1;
            FloatingTextFX.ShowAboveCard(attackerCardDisplay, "MOVA DE NOVO!", FloatingTextFX.EffectColor, 3.6f);
            Debug.Log($"[ArcherTier4Effect1] {attackerCardDisplay.card.cardName}: Matou o Healer — pode se mover novamente!");
        }

        // Libera o tile se a carta estiver no tabuleiro
        if (isOnBoard && currentTile != null)
        {
            currentTile.FreeTile();
        }

        // Remove da mão se estiver lá
        if (isInHand && handManager != null)
        {
            handManager.RemoveCardFromHand(gameObject);
        }

        // Explosão de partículas + som na posição da carta (objeto independente,
        // sobrevive à destruição da carta)
        Color poofColor = ownerPlayerNumber == 1 ? new Color(0.35f, 0.55f, 1f)
                        : ownerPlayerNumber == 2 ? new Color(1f, 0.4f, 0.35f)
                        : new Color(0.8f, 0.8f, 0.85f);
        CardAnimator.SpawnPoof(transform.position, poofColor);
        SoundManager.Play(SoundManager.Sound.Death);

        // Figura com rig e clip de morte: solta a figura da carta, toca a
        // animação de morrer e ela some sozinha depois (a carta é destruída já)
        if (boardFigure != null)
        {
            FigureRiggedAnimator rigAnim = boardFigure.GetComponent<FigureRiggedAnimator>();
            if (rigAnim != null && rigAnim.HasDeathClip)
            {
                boardFigure.transform.SetParent(null, true);
                rigAnim.PlayDeath();
                Destroy(boardFigure, rigAnim.DeathLength + 1.2f);
                boardFigure = null; // não morre junto com a carta
            }
        }

        Debug.Log($"Carta '{card.cardName}' destruída!");
        Destroy(gameObject);
    }

    // Verdadeiro enquanto uma cópia está sendo spawnada: o onEnter da cópia
    // NÃO deve disparar efeitos de "criar cópia" de novo (a cópia do Archer 1
    // gerava outra cópia, que gerava outra... em cadeia pela linha inteira)
    public static bool spawningCopy = false;

    // Cria uma cópia da carta em um tile específico
    public CardDisplay SpawnCardCopy(CardTile targetTile)
    {
        if (card == null || targetTile == null) return null;

        CardManager cardManager = CardManager.Instance;
        if (cardManager == null) return null;

        // Spawna a cópia usando CardManager
        CardDisplay copiedCard;
        spawningCopy = true;
        try
        {
            copiedCard = cardManager.SpawnCardOnTile(card, targetTile, ownerPlayerNumber);
        }
        finally
        {
            spawningCopy = false;
        }

        if (copiedCard != null)
        {
            // Copia os stats atuais da carta original
            copiedCard.currentHealth = currentHealth;
            copiedCard.currentAttack = currentAttack;
            copiedCard.currentShield = currentShield;
            copiedCard.UpdateDisplay();
        }

        return copiedCard;
    }

    // "Eco" (cópia de ENTRADA dos Archers 1 [1/3] e 3 [4/2]): a cópia nasce
    // com METADE dos stats (arredondado para baixo, mínimo 1 de ataque/vida)
    // e com enjoo — no round em que entra só pode andar. Corta o
    // "compra 1 leva 2 de graça" que deixava os arqueiros opressores.
    // (A cópia AO MATAR do Archer 5 continua com stats cheios — o freio dela
    // é o enjoo + ser carta única de tier 5.)
    public void WeakenAsEcho()
    {
        currentAttack = Mathf.Max(1, currentAttack / 2);
        currentHealth = Mathf.Max(1, currentHealth / 2);
        currentShield = currentShield / 2;
        BlockAttackThisRound();
        UpdateDisplay();
    }

    // Congela a carta por 1 turno DELA (Mage 3). Se congelada durante o próprio
    // turno, ainda perde o turno seguinte inteiro (contador 2, tica no fim do
    // turno do dono); congelada no turno do adversário, perde o próximo (1).
    public void Freeze(bool forceSingleTurn = false, CardDisplay source = null)
    {
        if (TurnManager.Instance == null) return;

        isFrozen = true;
        // forceSingleTurn: congelamentos decididos por popup criado durante o
        // tique de fim de turno resolvem DEPOIS da troca de jogador — sem a
        // trava, a vítima perdia 2 turnos em vez de 1
        freezeTurnsLeft = forceSingleTurn ? 1 : StatusDurationForVictim();
        UpdateCardDisplay(); // Tinge a carta de azul-gelo
        FloatingTextFX.ShowAboveCard(this, "CONGELADA!", new Color(0.55f, 0.85f, 1f));
        Debug.Log($"[Freeze] {card.cardName} foi congelada por {freezeTurnsLeft} turno(s) dela!");
        MatchStatsTracker.RecordDebuff(source); // telemetria (source null = não atribui)
    }

    // Stuna a carta por 1 turno DELA (Archer tier-2) — mesma regra do congelamento
    public void Stun(CardDisplay source = null)
    {
        if (TurnManager.Instance == null) return;

        isStunned = true;
        stunTurnsLeft = StatusDurationForVictim();
        UpdateCardDisplay(); // Mostra o overlay "ATORDOADA"
        FloatingTextFX.ShowAboveCard(this, "STUNADA!", new Color(1f, 0.88f, 0.40f));
        Debug.Log($"[Stun] {card.cardName} foi stunada por {stunTurnsLeft} turno(s) dela!");
        MatchStatsTracker.RecordDebuff(source); // telemetria (source null = não atribui)
    }

    // Duração para a vítima perder EXATAMENTE 1 turno dela:
    // - Congelada durante o próprio turno: 2 (o fim do turno atual dela desconta 1)
    // - Congelada no turno do adversário: 1
    // - Congelada por efeito de CONTADOR na passagem de turno (fase 2 do tique,
    //   que roda depois da fase de descontos): 1 — nada desconta nesta passada
    int StatusDurationForVictim()
    {
        if (TurnManager.TickingCounterEffects) return 1;
        return TurnManager.Instance.currentPlayerNumber == ownerPlayerNumber ? 2 : 1;
    }

    // Marca a carta com a águia por 2 turnos (Archer 3, ATK 4, HP 2)
    public void MarkWithEagle()
    {
        if (TurnManager.Instance == null) return;

        eagleMarked = true;
        eagleTurnsLeft = 2; // 2 passagens de turno (de qualquer jogador)
        UpdateCardDisplay(); // Mostra o overlay "MARCADA"
        Debug.Log($"[Eagle] {card.cardName} foi marcada pela águia! Não pode atacar por 2 turnos");
    }

    // Inicia/renova o contador visível do efeito (amarelo = turnos, rosa = rounds).
    // renews=true: efeito periódico (dispara e renova em 0); false: cooldown.
    public void StartEffectCounter(int value, bool isRound, bool renews)
    {
        effectCounter = value;
        effectPeriod = renews ? value : 0;
        effectCounterIsRound = isRound;
        UpdateCardDisplay();
    }

    // FASE 1 do tique de fim de turno (TurnManager): durações de status.
    // Roda para TODAS as cartas ANTES dos contadores de efeito — assim um stun
    // novo disparado por contador nunca é decrementado na mesma passada
    // (a duração não pode depender da posição das cartas no tabuleiro).
    public void TickStatusDurations(int endedTurnPlayerNumber, bool roundCompleted)
    {
        bool changed = false;

        // Congelamento/atordoamento: só contam no fim do turno DO DONO
        if (isFrozen && ownerPlayerNumber == endedTurnPlayerNumber)
        {
            freezeTurnsLeft--;
            if (freezeTurnsLeft <= 0)
            {
                isFrozen = false;
                changed = true;
                Debug.Log($"[Unfreeze] {card.cardName} foi descongelada!");
            }
        }

        if (isStunned && ownerPlayerNumber == endedTurnPlayerNumber)
        {
            stunTurnsLeft--;
            if (stunTurnsLeft <= 0)
            {
                isStunned = false;
                changed = true;
                Debug.Log($"[Unstun] {card.cardName} foi desestunada!");
            }
        }

        // Marca de águia: conta toda passagem de turno
        if (eagleMarked)
        {
            eagleTurnsLeft--;
            changed = true;
            if (eagleTurnsLeft <= 0)
            {
                eagleMarked = false;
                Debug.Log($"[Eagle] A marca de águia foi removida de {card.cardName}!");
            }
        }

        // Invulnerabilidade (Healer 4): conta ROUNDS
        if (invulnerableRoundsLeft > 0 && roundCompleted)
        {
            invulnerableRoundsLeft--;
            changed = true;
            if (invulnerableRoundsLeft <= 0)
                Debug.Log($"[Invulnerável] {card.cardName} perdeu a invulnerabilidade!");
        }

        if (changed) UpdateCardDisplay();
    }

    // FASE 2 do tique de fim de turno: contador de efeito periódico / cooldown
    public void TickEffectCounter(bool roundCompleted)
    {
        if (effectCounter <= 0) return;
        if (effectCounterIsRound && !roundCompleted) return;

        effectCounter--;

        if (effectCounter <= 0)
        {
            if (effectPeriod > 0)
            {
                // Efeito periódico: dispara (se as condições permitirem) e renova
                CardEffectSimple fx = GetComponent<CardEffectSimple>();
                if (fx != null) fx.OnPeriodicCounterExpired();
                effectCounter = effectPeriod;
            }
            else
            {
                effectCounter = -1; // Cooldown pronto: esconde o número
            }
        }

        UpdateCardDisplay();
    }
}
