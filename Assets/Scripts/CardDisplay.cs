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
    public CardTile currentTile; // Tile onde a carta está atualmente
    public int ownerPlayerNumber = 0; // 0 = sem dono (loja), 1 = Player1, 2 = Player2
    public int lastMovedRound = -1; // Em qual round a carta se moveu pela última vez (-1 = nunca)
    public int lastAttackedRound = -1; // Em qual round a carta atacou pela última vez (-1 = nunca)
    public bool treeDefenseUsed = false; // Archer efeito 4: já usou o dodge de árvore
    public bool treeDefenseActive = false; // Archer efeito 4: efeito está ativo NESTE turno
    public bool treeDefensePopupShown = false; // Rastreia se o popup já foi mostrado neste turno
    public bool healOnEnterUsed = false; // Healer 2: rastreia se já usou o efeito ao entrar em campo
    public bool isFrozen = false; // Mage 3: carta está congelada, não pode se mover/atacar/ativar efeito
    public bool isStunned = false; // Archer tier-2: carta está stunada, não pode se mover/atacar
    public bool archerShieldArrowUsed = false; // Archer 2 (ATK 3, HP 2): efeito de parar ataque já foi usado
    public bool archerStunOnHitUsed = false; // Archer 2 (ATK 3, HP 1): efeito de stun ao receber ataque já foi usado
    public bool archerComboActivated = false; // Archer tier-2 combo: +5 ATK já foi ativado
    public int maxHealthBonus = 0; // Healer 2 (ATK 2, HP 1): bônus de vida máxima
    public int healerShieldUseCount = 0; // Healer 2 (ATK 1, HP 3): vezes usado +armadura neste turno
    public bool healerComboActivated = false; // Healer tier-2 combo: restauração de ouro/vida já foi ativada
    public bool eagleMarked = false; // Archer 3 (ATK 4, HP 2): marcado pela águia, não pode atacar
    public int moveCountThisRound = 0; // Archer 3 (ATK 3, HP 2): contador de movimentações neste turno (máx 2 se tem Mago)
    public int lastMoveCountRound = -1; // Archer 3 (ATK 3, HP 2): último round em que moveu (para resetar contador)
    public bool archerTier3Effect1Used = false; // Archer 3 (ATK 4, HP 2): águia já foi invocada nesta partida
    public bool archerTier3Effect2Used = false; // Archer 3 (ATK 5, HP 3): cópia já foi feita nesta partida
    public bool archerTier3Effect3Used = false; // Archer 3 (ATK 3, HP 2): dano à torre já foi feito nesta partida
    public bool archerTier3Effect4Used = false; // Archer 3 (ATK 4, HP 1): dano em cruz já foi feito nesta partida
    public bool healerTier3Effect1Used = false; // Healer 3 (ATK 3, HP 3): ouro com Mago já foi ganho nesta partida
    public bool healerTier3Effect3Used = false; // Healer 3 (ATK 2, HP 1): ouro por contagem já foi ganho nesta partida
    public bool healerTier3Effect4Used = false; // Healer 3 (ATK 1, HP 2): ouro por Mago já foi ganho nesta partida
    public bool mageTier3Effect1Used = false; // Mage 3 (ATK 0, HP 1): roubo de status já foi feito nesta partida
    public bool mageTier3Effect2Used = false; // Mage 3 (ATK 4, HP 4): +1 ATK para todos já foi feito nesta partida
    public bool mageTier3Effect4Used = false; // Mage 3 (ATK 3, HP 3): +1 ATK na mão já foi feito nesta partida
    public bool tankTier3Effect1Used = false; // Tank 3 (ATK 2, Shield 3, HP 4): +2 armadura Healers já foi feito nesta partida
    public bool tankTier3Effect3Used = false; // Tank 3 (ATK 2, Shield 2, HP 5): +2 armadura por Tank já foi feito nesta partida
    public bool tankTier3Effect4Used = false; // Tank 3 (ATK 2, Shield 2, HP 6): +3 armadura Mago já foi feito nesta partida
    public int archerTier4Effect2LastUsedRound = -3; // Archer 4 (ATK 6, HP 3): último round que usou stun (para reutilizar a cada 2 turnos)
    public int archerTier4Effect4LastAttackRound = -1; // Archer 4 (ATK 6, HP 2): último round que atacou alvo ao lado
    public int healerTier4Effect1LastCureRound = -3; // Healer 4 (ATK 3, HP 3): último round que curou (a cada 2 turnos)
    public bool healerTier4Effect3Used = false; // Healer 4 (ATK 5, HP 3): invunerabilidade já foi dada nesta partida
    public bool healerTier4Effect4Used = false; // Healer 4 (ATK 4, HP 4): +3 todos status já foi ativado nesta partida
    public bool mageTier4Effect1Used = false; // Mage 4 (ATK 7, HP 4): remover bônus já foi feito nesta partida
    public int mageTier4Effect1UsesLeft = 1; // Mage 4 (ATK 7, HP 4): pode usar 2 vezes se tem Healer + Arqueiro
    public bool mageTier4Effect3Used = false; // Mage 4 (ATK 5, HP 4): destruir inimigo já foi feito nesta partida
    public int mageTier4Effect4LastUsedRound = -1; // Mage 4 (ATK 6, HP 3): último round que ganhou ouro (uma vez por round)
    public bool tankTier4Effect1Used = false; // Tank 4 (ATK 1, Shield 6, HP 3): +5 HP +2 Shield já foi feito nesta partida
    public int tankTier4Effect2LastUsedRound = -1; // Tank 4 (ATK 2, Shield 6, HP 5): último round que recebeu ataque (uma vez por turno)
    public bool tankTier4Effect3Used = false; // Tank 4 (ATK 2, Shield 3, HP 5): Arqueiros x2 ataque já foi ativado nesta partida
    public bool archerTier5Effect2Used = false; // Archer 5 (ATK 10, HP 5): remover armadura inimigos já foi feito nesta partida
    public bool healerTier5Effect3Used = false; // Healer 5 (ATK 4, HP 5): duplicar stats de aliado já foi feito nesta partida
    public int mageTier5Effect1LastUsedRound = -1; // Mage 5 (ATK 8, HP 4): último round que congelou inimigos (uma vez por round)
    public bool mageTier5Effect2Used = false; // Mage 5 (ATK 6, HP 5): copiar stats de inimigo já foi feito nesta partida
    public int tankTier5Effect2LastArmorRound = -3; // Tank 5 (ATK 2, Shield 6, HP 8): último round que concedeu armadura (a cada 2 turnos)

    // Stats atuais (mudam durante o jogo)
    public int currentHealth;
    public int currentShield;
    public int currentAttack;

    // Escalas padrão por zona (o tabuleiro tem tiles 6x6, cartas pequenas ficavam ilegíveis)
    public const float HandScale = 2f;
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
    public int invulnerableRoundsLeft = 0; // Healer 4 (ATK 5, HP 3): invulnerável por 3 ROUNDS

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
        // Se a carta está no tabuleiro e pertence ao jogador atual, permite atacar com botão direito ou tecla A
        if (isOnBoard && isMouseOver && TurnManager.Instance != null)
        {
            int currentPlayerNumber = TurnManager.Instance.currentPlayerNumber;
            if (ownerPlayerNumber == currentPlayerNumber)
            {
                // Verifica inputs com o novo Input System
                Mouse mouse = Mouse.current;
                Keyboard keyboard = Keyboard.current;

                if (mouse != null && keyboard != null)
                {
                    // Botão direito do mouse ou tecla A para atacar
                    if (mouse.rightButton.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
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

        // Se nunca se moveu ou se moveu em um round anterior, pode mover (até o máximo de movimentações permitidas)
        // Por padrão, máximo é 1, mas Archer 3 (ATK 3, HP 2) com Mago pode ter 2
        return lastMovedRound < TurnManager.Instance.currentRound;
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
        return lastAttackedRound < TurnManager.Instance.currentRound;
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

        // Hook: Mage 4 (ATK 6, HP 6) ganha +1 ATK quando Healer entra em campo
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var alliedMages = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var mage in alliedMages)
                {
                    if (mage != null && mage.card.cardClass == CardClass.Mago &&
                        mage.card.attack == 6 && mage.card.health == 6 && mage.card.tier == CardTier.Tier4)
                    {
                        CardEffectSimple mageEffect = mage.GetComponent<CardEffectSimple>();
                        if (mageEffect != null)
                            mageEffect.ActivateBoostOnHealerEnter();
                    }
                }
            }
        }
    }

    void ApplyMageEffect()
    {
        // Quando um Healer toma dano, todos os Magos aliados aplicam efeito
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var alliedMages = board.GetCardsByOwner(ownerPlayerNumber)
            .FindAll(c => c.card.cardClass == CardClass.Mago);

        foreach (var mage in alliedMages)
        {
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

        // Atualiza cor de fundo baseado na classe (azul-gelo se congelada)
        if (backgroundRenderer != null)
        {
            EnsureQuadMaterial(backgroundRenderer);
            Color classColor = isFrozen
                ? new Color(0.45f, 0.75f, 1.00f)
                : GetClassColor(card.cardClass);
            backgroundRenderer.material.color = classColor;
            backgroundRenderer.material.SetColor("_BaseColor", classColor);
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

        // Aplica as cores dos quads estáticos (corrige shaders URP em runtime)
        ApplyCardTheme();

        // Overlays de status (congelada/atordoada/marcada) e flash de buff/dano
        UpdateStatusVisuals();
    }

    // Rastreia os últimos stats exibidos para detectar mudanças automaticamente:
    // qualquer aumento pisca verde, qualquer redução pisca vermelho — vale para
    // TODOS os efeitos sem precisar ligar um por um
    private int lastShownAttack;
    private int lastShownShield;
    private int lastShownHealth;
    private bool statsTracked = false;

    void UpdateStatusVisuals()
    {
        CardStatusVisuals visuals = GetComponent<CardStatusVisuals>();
        if (visuals == null) visuals = gameObject.AddComponent<CardStatusVisuals>();

        visuals.SetFrozen(isFrozen);
        visuals.SetStunned(isStunned);
        visuals.SetEagleMark(eagleMarked);

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
        // Borda colorida pelo dono: azul = Jogador 1, vermelho = Jogador 2, escuro = loja
        Color borderColor;
        if (ownerPlayerNumber == 1) borderColor = new Color(0.15f, 0.40f, 1.00f);
        else if (ownerPlayerNumber == 2) borderColor = new Color(0.95f, 0.25f, 0.20f);
        else borderColor = new Color(0.06f, 0.06f, 0.10f);
        SetQuadColor("Border", borderColor);
        SetQuadColor("NameHeader", new Color(0.18f, 0.18f, 0.28f));
        SetQuadColor("EffectBackground", new Color(0.22f, 0.22f, 0.32f));
        SetQuadColor("StatsBackground", new Color(0.16f, 0.16f, 0.24f));
        SetQuadColor("StatsDivider1", new Color(0.35f, 0.35f, 0.55f));
        SetQuadColor("StatsDivider2", new Color(0.35f, 0.35f, 0.55f));
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
                n == "ClassText") continue;
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

    // Para interação com o mouse — zoom RELATIVO à escala atual (o bug antigo usava
    // uma "escala original" capturada antes da escala da loja ser aplicada, então
    // o hover ENCOLHIA a carta em vez de aumentar)
    void OnMouseEnter()
    {
        isMouseOver = true;
        if (isInHand) return;

        preHoverScale = transform.localScale;
        hoverLifted = false;

        if (isInShop)
        {
            // Zoom forte na loja para conseguir ler o efeito;
            // sobe a carta para ficar por cima das vizinhas
            transform.localScale = preHoverScale * 2f;
            preHoverPosition = transform.position;
            transform.position = preHoverPosition + Vector3.up * 2f;
            hoverLifted = true;
        }
        else
        {
            // Destaque leve no tabuleiro
            transform.localScale = preHoverScale * 1.15f;
        }
    }

    void OnMouseExit()
    {
        isMouseOver = false;
        // Se a carta foi comprada durante o hover, a mão já definiu escala/posição
        if (isInHand) return;

        if (preHoverScale != Vector3.zero)
        {
            transform.localScale = preHoverScale;
        }
        if (hoverLifted)
        {
            transform.position = preHoverPosition;
            hoverLifted = false;
        }
        preHoverScale = Vector3.zero;
    }

    void OnMouseDown()
    {
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

        int cost = card.GetGoldCost();

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
        int cost = card.GetGoldCost();
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
        SoundManager.Play(SoundManager.Sound.Buy);

        // Remove da loja e adiciona à mão DO JOGADOR CORRETO
        isInShop = false;
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

        // Ataca o primeiro inimigo encontrado
        CardDisplay target = enemies[0];
        int damageDealt = currentAttack;

        // Rastreia quem atacou (para efeitos como Archer tier-2)
        target.attackerCardDisplay = this;

        // Investida visual em direção ao alvo
        PlayAttackAnim(target);

        // Ativa efeitos de Archer tier-5 antes do dano
        bool ignoreArmor = false;
        int modifiedDamage = damageDealt;

        if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                // Efeito 1: Double Damage Against Tank (ATK 8, HP 3)
                if (card.attack == 8 && card.health == 3)
                {
                    if (effect.IsTargetTank(target))
                    {
                        modifiedDamage = damageDealt * 2;
                        Debug.Log($"[ArcherTier5Effect1] {card.cardName}: Duplicou dano contra Tank!");
                    }
                }

                // Efeito 2: Remove Enemy Armor + Ignore Armor if Has Tank (ATK 10, HP 5)
                if (card.attack == 10 && card.health == 5)
                {
                    if (effect.ShouldIgnoreArmor_Tier5Effect2())
                    {
                        ignoreArmor = true;
                        Debug.Log($"[ArcherTier5Effect2] {card.cardName}: Ignorando armadura do inimigo!");
                    }
                }

                // Efeito 3: Ignore Armor + Execute (ATK 15, HP 4)
                if (card.attack == 15 && card.health == 4)
                {
                    ignoreArmor = true;
                    Debug.Log($"[ArcherTier5Effect3] {card.cardName}: Ignorando armadura do inimigo!");
                }
            }
        }

        // Ativa efeitos de Archer tier-4 antes do dano
        if (card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier4)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
            {
                // Efeito 1: Double Attack Healer (ATK 5, HP 3)
                if (card.attack == 5 && card.health == 3)
                    effect.ActivateDoubleAttackHealer(target);

                // (Efeito 2 do ATK 6/HP 3 — stun — agora dispara ao ENTRAR em campo
                // e a cada 2 turnos via contador, não mais no ataque)

                // Efeito 4: Extra Move on Side Attack (ATK 6, HP 2)
                if (card.attack == 6 && card.health == 2)
                    effect.CheckSideAttackAndMove(target);
            }
        }

        // Se não é double attack do Tier-4 Efeito 1, faz o ataque normal
        if (!(card.cardClass == CardClass.Arqueiro && card.tier == CardTier.Tier4 && card.attack == 5 && card.health == 3))
        {
            // Aplica armadura antes do dano se não ignorar
            if (ignoreArmor)
            {
                // Ignora armadura: aplica dano direto
                target.currentHealth -= modifiedDamage;
                if (target.currentHealth < 0)
                    target.currentHealth = 0;
                target.UpdateCardDisplay();

                // Efeito 3: Executar se HP <= 2
                if (card.attack == 15 && card.health == 4 && card.tier == CardTier.Tier5)
                {
                    CardEffectSimple effect = GetComponent<CardEffectSimple>();
                    if (effect != null)
                        effect.CheckArcherTier5Effect3_Execute(target);
                }
                // Para outros efeitos com ignoreArmor, verifica se a carta morreu
                else if (target.currentHealth <= 0)
                {
                    target.DestroyCard();
                }
            }
            else
            {
                target.TakeDamage(modifiedDamage);
            }
        }

        // Efeito 3: Copy on Kill (ATK 7, HP 3) - ativa após o dano
        bool targetDied = target.currentHealth <= 0;
        if (card.cardClass == CardClass.Arqueiro && card.attack == 7 && card.health == 3 && targetDied)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateCopyOnKill();
        }

        // Tank tier-5 efeito 1 (ATK 5, Shield 9, HP 6): Concede armadura a aliados ao matar
        if (card.cardClass == CardClass.Tank && card.attack == 5 && card.shield == 9 && card.health == 6 &&
            card.tier == CardTier.Tier5 && targetDied)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateShieldOnKill();
        }

        // Marca que atacou neste round
        if (TurnManager.Instance != null)
        {
            lastAttackedRound = TurnManager.Instance.currentRound;
        }

        return true;
    }

    // Cura a carta
    public void Heal(int amount, CardDisplay source = null)
    {
        currentHealth += amount;
        if (currentHealth > card.health)
            currentHealth = card.health;
        UpdateCardDisplay();

        // Trigger Healer 4 effect: ganhar ouro quando um aliado é curado
        if (ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer &&
                        ally.card.attack == 2 && ally.card.health == 2)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            effect.OnAllyHealed();
                        }
                    }
                }

                // Trigger Tank effects on heal
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Tank)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            // Tank 2: +1 todos atributos quando curado
                            if (ally.card.attack == 1 && ally.card.shield == 1 && ally.card.health == 1)
                                effect.TankEffect2_BoostOnHeal();
                            // Tank 3: +1 ataque quando ganhar vida
                            else if (ally.card.attack == 0 && ally.card.shield == 2 && ally.card.health == 1)
                                effect.TankEffect3_AttackOnHeal();
                        }
                    }
                }

                // Trigger Healer 2 (ATK 2, HP 1) effect on heal
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer &&
                        ally.card.attack == 2 && ally.card.health == 1)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            effect.HealerTier2Effect1_OnAllyHealed();
                        }
                    }
                }
            }
        }
    }

    // Recebe dano (primeiro absorve no escudo, depois na vida)
    public void TakeDamage(int damage)
    {
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

        // Reduz dano se for um Tank e houver outro Tank 3 (ATK 3, Shield 2, HP 4) em campo
        if (card.cardClass == CardClass.Tank && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 3 && ally.card.shield == 2 && ally.card.health == 4 &&
                        ally.card.tier == CardTier.Tier3)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                        {
                            int reducedDamage = effect.ReduceTankDamage(damage);
                            damage = reducedDamage;
                            Debug.Log($"[TankTier3Effect2] {card.cardName}: Dano reduzido em 50% ({reducedDamage} de dano)");
                        }
                        break;
                    }
                }
            }
        }

        // Tank 4 tier-4 (ATK 2, Shield 6, HP 5) - Intercepta ataque 1x por turno
        if (card.cardClass != CardClass.Tank && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 2 && ally.card.shield == 6 && ally.card.health == 5 &&
                        ally.card.tier == CardTier.Tier4)
                    {
                        // Verifica se pode interceptar (1x por turno)
                        if (TurnManager.Instance != null &&
                            ally.tankTier4Effect2LastUsedRound < TurnManager.Instance.currentRound)
                        {
                            CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                            if (effect != null)
                            {
                                int reductionPercent = effect.GetTankTier4Effect2Reduction();
                                if (reductionPercent > 0)
                                {
                                    damage = damage / 2; // 50% menos dano
                                    Debug.Log($"[TankTier4Effect2] {ally.card.cardName}: Interceptou ataque e recebeu 50% menos dano! ({damage} de dano)");
                                }
                                else
                                {
                                    Debug.Log($"[TankTier4Effect2] {ally.card.cardName}: Interceptou ataque em lugar de {card.cardName}!");
                                }
                                // Marca o uso ANTES de aplicar o dano e usa ApplyDamageNormally
                                // para o dano interceptado não disparar novos redirecionamentos
                                ally.tankTier4Effect2LastUsedRound = TurnManager.Instance.currentRound;
                                ally.ApplyDamageNormally(damage);
                                return; // Tank recebeu o ataque
                            }
                        }
                    }
                }
            }
        }

        // Triggers para Mago tier-2 quando Healer é atacado
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // Mago 2 (ATK 2, HP 3) ganha +1 ATK
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Mago &&
                        ally.card.attack == 2 && ally.card.health == 3)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.MageTier2Effect1_BoostAttack();
                    }
                }

                // Tank 2 (ATK 2, Shield 1, HP 3) recebe o ataque
                foreach (var ally in allies)
                {
                    if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 2 && ally.card.shield == 1 && ally.card.health == 3)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.TankTier2Effect1_TakeHealerAttack(this, damage);
                        return; // Tank recebeu o ataque, não aplicar dano ao Healer
                    }
                }

                // Tank tier 2 (ATK 1, Shield 3, HP 2) pode assumir o dano (o dono escolhe)
                if (TryTankAssumeAnyDamage(damage))
                    return; // Decisão pendente: o dano é resolvido no callback
            }
        }

        // Triggers para Healer tier-3 quando Tank é atacado (Healer 3 ATK 3 HP 1 cura)
        if (card.cardClass == CardClass.Tank && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // Healer 3 (ATK 3, HP 1) cura o Tank que está levando dano
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer &&
                        ally.card.attack == 3 && ally.card.health == 1 && ally.card.tier == CardTier.Tier3)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.HealerTier3Effect2_CureTankWhenDamaged(this);
                    }
                }
            }
        }

        // Triggers para Mago tier-2 quando Tank é atacado
        if (card.cardClass == CardClass.Tank && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // (O Mago 2 ATK 3/HP 2 perdeu o solo da bola de fogo — a carta
                // agora é só tríade, então o gancho foi removido)

                // Tank tier 2 (ATK 2, Shield 2, HP 2) recebe o ataque
                foreach (var ally in allies)
                {
                    if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 2 && ally.card.shield == 2 && ally.card.health == 2)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.TankTier2Effect2_TakeArcherAttack(this, damage);
                        return; // Tank recebeu o ataque, não aplicar dano ao Arqueiro
                    }
                }

                // Tank tier 2 (ATK 1, Shield 3, HP 2) pode assumir o dano (o dono escolhe)
                if (TryTankAssumeAnyDamage(damage))
                    return; // Decisão pendente: o dano é resolvido no callback
            }
        }

        // Triggers para Mago tier-2 quando Arqueiro é atacado
        if (card.cardClass == CardClass.Arqueiro && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // Mago 2 (ATK 3, HP 1) congela o atacante
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Mago &&
                        ally.card.attack == 3 && ally.card.health == 1)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.MageTier2Effect4_FreezeAttacker(this);
                    }
                }

                // Tank tier 2 (ATK 2, Shield 2, HP 2) recebe o ataque
                foreach (var ally in allies)
                {
                    if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 2 && ally.card.shield == 2 && ally.card.health == 2)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.TankTier2Effect2_TakeArcherAttack(this, damage);
                        return; // Tank recebeu o ataque, não aplicar dano
                    }
                }

                // Tank tier 2 (ATK 1, Shield 3, HP 2) pode assumir o dano (o dono escolhe)
                if (TryTankAssumeAnyDamage(damage))
                    return; // Decisão pendente: o dano é resolvido no callback
            }
        }

        // Triggers para Mago tier-2 quando Mago é atacado
        if (card.cardClass == CardClass.Mago && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);

                // Tank tier 2 (ATK 0, Shield 4, HP 1) recebe o ataque
                foreach (var ally in allies)
                {
                    if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                        ally.card.attack == 0 && ally.card.shield == 4 && ally.card.health == 1)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null)
                            effect.TankTier2Effect3_TakeMagoAttack(this, damage);
                        return; // Tank recebeu o ataque, não aplicar dano ao Mago
                    }
                }

                // Tank tier 2 (ATK 1, Shield 3, HP 2) pode assumir o dano (o dono escolhe)
                if (TryTankAssumeAnyDamage(damage))
                    return; // Decisão pendente: o dano é resolvido no callback
            }
        }

        // Verifica se é um Healer sendo atacado e há Archer 2 (ATK 3, HP 2) que pode parar o ataque
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Arqueiro &&
                        ally.card.attack == 3 && ally.card.health == 2 &&
                        !ally.archerShieldArrowUsed)
                    {
                        // Decisão sincronizada: o dono da carta defendida escolhe
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
                            "Um Archer pode parar este ataque!",
                            "Parar ataque", "Não parar",
                            accepted =>
                            {
                                if (accepted) { if (effect != null) effect.ArcherTier2Effect2_ShieldArrow(this); }
                                else ApplyDamageNormally(damage);
                            });
                        return;
                    }
                }
            }
        }

        // Verifica se é um Archer 2 (ATK 3, HP 1) recebendo ataque
        if (card.cardClass == CardClass.Arqueiro && card.attack == 3 && card.health == 1 &&
            !archerStunOnHitUsed && ownerPlayerNumber != 0)
        {
            // Decisão sincronizada: o dono do Archer escolhe se stuna o atacante
            CardEffectSimple stunEffect = GetComponent<CardEffectSimple>();
            PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
                $"{card.cardName} pode stunar o atacante!",
                "Ativar stun", "Não usar",
                accepted =>
                {
                    if (accepted && stunEffect != null) stunEffect.ArcherTier2Effect3_ActivateStun(this);
                    ApplyDamageNormally(damage);
                });
            return;
        }

        // Verifica se há uma Healer 3 aliada que pode bloquear o ataque
        if (ownerPlayerNumber != 0)
        {
            BoardManager board = BoardManager.Instance;
            if (board != null)
            {
                var allies = board.GetCardsByOwner(ownerPlayerNumber);
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer &&
                        ally.card.attack == 1 && ally.card.health == 2)
                    {
                        CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                        if (effect != null && effect.CanBlockAttackThisTurn())
                        {
                            ShowBlockAttackPopup(damage, effect);
                            return; // Pausa o dano enquanto aguarda resposta
                        }
                    }
                }
            }
        }

        // Se é Archer 4 e o efeito não foi usado e ainda não mostrou popup neste turno
        if (card.cardClass == CardClass.Arqueiro && card.attack == 3 && card.health == 2 &&
            !treeDefenseUsed && !treeDefensePopupShown)
        {
            // Mostra popup perguntando se quer ativar o efeito
            treeDefensePopupShown = true;
            ShowTreeDefensePopup(damage);
            return; // Pausa o dano enquanto aguarda resposta
        }

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

        // Atualiza a UI
        UpdateCardDisplay();

        // Tank tier-5 efeito 2 (ATK 2, Shield 6, HP 8): +1 ATK ao receber dano
        if (card.cardClass == CardClass.Tank && card.attack == 2 && card.shield == 6 && card.health == 8 &&
            card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateAttackBoostOnDamage_Tier5Effect2();
        }

        // Tank tier-5 efeito 3 (ATK 4, Shield 5, HP 10): +1 ATK ao receber dano, +armadura se tem Healer/Mago
        if (card.cardClass == CardClass.Tank && card.attack == 4 && card.shield == 5 && card.health == 10 &&
            card.tier == CardTier.Tier5)
        {
            CardEffectSimple effect = GetComponent<CardEffectSimple>();
            if (effect != null)
                effect.ActivateAttackBoostOnDamage_Tier5Effect3();
        }

        // Se um Healer toma dano, aplica efeito do Mago
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            ApplyMageEffect();
        }

        // Verifica se a carta morreu
        if (currentHealth <= 0)
        {
            DestroyCard();
        }
    }

    void ShowBlockAttackPopup(int damage, CardEffectSimple healerEffect)
    {
        // Decisão sincronizada: o dono da carta defendida escolhe
        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
            "Uma Healer pode bloquear este ataque!",
            "Bloquear ataque", "Não bloquear",
            accepted =>
            {
                if (accepted) ActivateBlockAttack(damage, healerEffect);
                else ApplyDamageNormally(damage);
            });
    }

    void ShowTreeDefensePopup(int damage)
    {
        // Decisão sincronizada: o dono do Archer escolhe
        PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
            $"{card.cardName} pode usar a árvore para esquivar o dano!",
            "Ativar efeito", "Não usar",
            accepted =>
            {
                if (accepted) ActivateTreeDefense(damage);
                else ApplyDamageNormally(damage);
            });
    }

    void ActivateBlockAttack(int damage, CardEffectSimple healerEffect)
    {
        healerEffect.ActivateBlockAttack();
        Debug.Log($"[BlockAttack] Ataque bloqueado pela Healer!");
    }

    void ActivateTreeDefense(int damage)
    {
        treeDefenseActive = true;
        treeDefenseUsed = true;
        Debug.Log($"[TreeDefense] {card.cardName} ativou o efeito! Esquivando dano neste turno.");
    }

    // Tank tier 2 (ATK 1, Shield 3, HP 2): pode assumir o dano de qualquer aliado atacado.
    // O dono escolhe via popup sincronizado. Retorna true se a decisão ficou pendente.
    bool TryTankAssumeAnyDamage(int damage)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return false;

        var allies = board.GetCardsByOwner(ownerPlayerNumber);
        foreach (var ally in allies)
        {
            // ally != this: o Tank não pode "assumir" o próprio dano (causava loop infinito)
            if (ally != null && ally != this && ally.card.cardClass == CardClass.Tank &&
                ally.card.attack == 1 && ally.card.shield == 3 && ally.card.health == 2)
            {
                CardEffectSimple effect = ally.GetComponent<CardEffectSimple>();
                if (effect == null) continue;

                CardDisplay tank = ally;
                PhotonGameManager.AskEffectDecision(ownerPlayerNumber,
                    $"{tank.card.cardName} pode assumir o dano de {card.cardName}!",
                    "Assumir dano", "Não assumir",
                    accepted =>
                    {
                        if (accepted) effect.TankTier2Effect4_TakeAnyAttack(this, damage);
                        else ApplyDamageNormally(damage);
                    });
                return true;
            }
        }
        return false;
    }

    public void ApplyDamageNormally(int damage)
    {
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

        // Atualiza a UI
        UpdateCardDisplay();

        // Se um Healer toma dano, aplica efeito do Mago
        if (card.cardClass == CardClass.Healer && ownerPlayerNumber != 0)
        {
            ApplyMageEffect();
        }

        // Verifica se a carta morreu
        if (currentHealth <= 0)
        {
            DestroyCard();
        }
    }

    // Destrói a carta
    public CardDisplay attackerCardDisplay = null; // Rastreia quem atacou essa carta

    public void DestroyCard()
    {
        // Verifica se foi destruída por um Archer 2 (ATK 3, HP 3)
        if (attackerCardDisplay != null &&
            attackerCardDisplay.card.cardClass == CardClass.Arqueiro &&
            attackerCardDisplay.card.attack == 3 &&
            attackerCardDisplay.card.health == 3)
        {
            // Invoca um Archer aleatório
            CardManager cardManager = CardManager.Instance;
            if (cardManager != null)
            {
                cardManager.InvokeRandomArcher(attackerCardDisplay.ownerPlayerNumber, attackerCardDisplay.currentTile);
            }
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

        Debug.Log($"Carta '{card.cardName}' destruída!");
        Destroy(gameObject);
    }

    // Cria uma cópia da carta em um tile específico
    public CardDisplay SpawnCardCopy(CardTile targetTile)
    {
        if (card == null || targetTile == null) return null;

        CardManager cardManager = CardManager.Instance;
        if (cardManager == null) return null;

        // Spawna a cópia usando CardManager
        CardDisplay copiedCard = cardManager.SpawnCardOnTile(card, targetTile, ownerPlayerNumber);

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

    // Congela a carta por 1 turno DELA (Mage 3). Se congelada durante o próprio
    // turno, ainda perde o turno seguinte inteiro (contador 2, tica no fim do
    // turno do dono); congelada no turno do adversário, perde o próximo (1).
    public void Freeze()
    {
        if (TurnManager.Instance == null) return;

        isFrozen = true;
        freezeTurnsLeft = StatusDurationForVictim();
        UpdateCardDisplay(); // Tinge a carta de azul-gelo
        Debug.Log($"[Freeze] {card.cardName} foi congelada por {freezeTurnsLeft} turno(s) dela!");
    }

    // Stuna a carta por 1 turno DELA (Archer tier-2) — mesma regra do congelamento
    public void Stun()
    {
        if (TurnManager.Instance == null) return;

        isStunned = true;
        stunTurnsLeft = StatusDurationForVictim();
        UpdateCardDisplay(); // Mostra o overlay "ATORDOADA"
        Debug.Log($"[Stun] {card.cardName} foi stunada por {stunTurnsLeft} turno(s) dela!");
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
