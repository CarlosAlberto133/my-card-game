using UnityEngine;
using System.Collections.Generic;

public class CardEffectSimple : MonoBehaviour
{
    CardDisplay cardDisplay;
    int lastHealerEffectRound = -2; // Rastreia o último round que o efeito foi ativado
    int lastBlockAttackRound = -3; // Rastreia o último round que bloqueou um ataque (Healer 3)

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
    }

    // ===== ARCHER TIER-5 =====
    public void ArcherTier5Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 8 && baseHp == 3)
            ArcherTier5Effect1_DoubleDamageAgainstTank();
        else if (baseAtk == 10 && baseHp == 5)
            ArcherTier5Effect2_RemoveEnemyArmor();
        else if (baseAtk == 15 && baseHp == 4)
            ArcherTier5Effect3_IgnoreArmorAndExecute();
    }

    // Efeito 1: Archer 5 (ATK 8, HP 3) - Causa dano duplicado se atacar um Tank
    void ArcherTier5Effect1_DoubleDamageAgainstTank()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em AttackAdjacentEnemy
        Debug.Log($"[ArcherTier5Effect1] {cardDisplay.card.cardName}: Pronta para duplicar dano contra Tanks");
    }

    public bool IsTargetTank(CardDisplay target)
    {
        return target != null && target.card.cardClass == CardClass.Tank;
    }

    // Efeito 2: Archer 5 (ATK 10, HP 5) - Remove 2 armadura de todos inimigos ao entrar, ignora armadura se tem Tank
    void ArcherTier5Effect2_RemoveEnemyArmor()
    {
        if (cardDisplay == null || cardDisplay.archerTier5Effect2Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.currentShield -= 2;
                if (enemy.currentShield < 0)
                    enemy.currentShield = 0;
                enemy.UpdateDisplay();
            }
        }

        cardDisplay.archerTier5Effect2Used = true;
        Debug.Log($"[ArcherTier5Effect2] {cardDisplay.card.cardName}: Removeu 2 de armadura de todos os inimigos!");
    }

    public bool ShouldIgnoreArmor_Tier5Effect2()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return false;

        return board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);
    }

    // Efeito 3: Archer 5 (ATK 15, HP 4) - Ignora armadura, executa se inimigo tem 2 HP ou menos
    void ArcherTier5Effect3_IgnoreArmorAndExecute()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em AttackAdjacentEnemy
        Debug.Log($"[ArcherTier5Effect3] {cardDisplay.card.cardName}: Pronta para ignorar armadura e executar inimigos com 2 HP");
    }

    public void CheckArcherTier5Effect3_Execute(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        // Se o inimigo tem 2 HP ou menos, executa imediatamente
        if (targetEnemy.currentHealth <= 2)
        {
            targetEnemy.currentHealth = 0;
            targetEnemy.DestroyCard();
            Debug.Log($"[ArcherTier5Effect3] {cardDisplay.card.cardName}: Executou {targetEnemy.card.cardName}!");
        }
    }

    // ===== HEALER TIER-5 =====
    public void HealerTier5Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 5 && baseHp == 4)
            HealerTier5Effect1_FreeCardPurchase();
        else if (baseAtk == 6 && baseHp == 3)
            HealerTier5Effect2_PeriodicAllyHeal();
        else if (baseAtk == 4 && baseHp == 5)
            HealerTier5Effect3_DoubleAllyStats();
    }

    // Efeito 1: Healer 5 (ATK 5, HP 4) - Ao entrar concede 1 compra grátis
    void HealerTier5Effect1_FreeCardPurchase()
    {
        if (cardDisplay == null) return;

        PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.cardsBoughtThisTurn--;
            Debug.Log($"[HealerTier5Effect1] {cardDisplay.card.cardName}: Concedeu 1 compra grátis!");
        }
    }

    // Efeito 2: Healer 5 (ATK 6, HP 3) - Cura todos aliados em 2 HP e 2 shield a cada turno
    void HealerTier5Effect2_PeriodicAllyHeal()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em TurnManager.EndTurn()
        Debug.Log($"[HealerTier5Effect2] {cardDisplay.card.cardName}: Pronta para curar aliados a cada turno");
    }

    public void ActivatePeriodicAllyHeal()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Cura todos os aliados em 2 de HP e 2 de shield
        foreach (var ally in allies)
        {
            if (ally != null)
            {
                ally.currentHealth += 2;
                if (ally.currentHealth > ally.card.health)
                    ally.currentHealth = ally.card.health;

                ally.currentShield += 2;
                ally.UpdateDisplay();
            }
        }

        Debug.Log($"[HealerTier5Effect2] {cardDisplay.card.cardName}: Curou todos aliados em 2 HP e 2 shield!");
    }

    // Efeito 3: Healer 5 (ATK 4, HP 5) - Duplica todos os status de um aliado à escolha
    void HealerTier5Effect3_DoubleAllyStats()
    {
        if (cardDisplay == null || cardDisplay.healerTier5Effect3Used) return;

        ShowDoubleStatsPopup();
    }

    void ShowDoubleStatsPopup()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Cria lista de opções para escolher qual aliado duplicar
        List<string> allyNames = new List<string>();
        foreach (var ally in allies)
        {
            if (ally != null)
                allyNames.Add(ally.card.cardName);
        }

        // O dono clica no aliado desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 7,
                new List<CardDisplay>(allies),
                "Escolha qual aliado terá seus status duplicados");
        }
    }

    public void ActivateDoubleStats(CardDisplay targetAlly)
    {
        if (cardDisplay == null || targetAlly == null) return;

        // Duplica todos os status
        targetAlly.currentAttack *= 2;
        targetAlly.currentShield *= 2;
        targetAlly.currentHealth *= 2;
        if (targetAlly.currentHealth > targetAlly.card.health * 2)
            targetAlly.currentHealth = targetAlly.card.health * 2;

        targetAlly.UpdateDisplay();
        cardDisplay.healerTier5Effect3Used = true;
        Debug.Log($"[HealerTier5Effect3] {cardDisplay.card.cardName}: Duplicou status de {targetAlly.card.cardName}!");
    }

    // ===== ARCHER TIER-4 =====
    public void ArcherTier4Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 5 && baseHp == 3)
            ArcherTier4Effect1_DoubleAttackHealer();
        else if (baseAtk == 6 && baseHp == 3)
            ArcherTier4Effect2_StunEvery2Turns();
        else if (baseAtk == 7 && baseHp == 3)
            ArcherTier4Effect3_CopyOnKill();
        else if (baseAtk == 6 && baseHp == 2)
            ArcherTier4Effect4_ExtraMoveOnSideAttack();
    }

    // Efeito 1: Archer 4 (ATK 5, HP 3) - Ataque 2 vezes se alvo é Healer, move novamente se mata
    void ArcherTier4Effect1_DoubleAttackHealer()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado durante o ataque via hook em AttackAdjacentEnemy
        Debug.Log($"[ArcherTier4Effect1] {cardDisplay.card.cardName}: Pronta para atacar Healers 2 vezes");
    }

    public void ActivateDoubleAttackHealer(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        // Ataca 2 vezes se for Healer
        if (targetEnemy.card.cardClass == CardClass.Healer)
        {
            targetEnemy.TakeDamage(cardDisplay.currentAttack);
            targetEnemy.TakeDamage(cardDisplay.currentAttack);
            Debug.Log($"[ArcherTier4Effect1] {cardDisplay.card.cardName}: Atacou {targetEnemy.card.cardName} 2 vezes!");

            // Se mata o Healer, pode se movimentar novamente
            if (targetEnemy.currentHealth <= 0)
            {
                cardDisplay.lastMovedRound = -1;
                Debug.Log($"[ArcherTier4Effect1] {cardDisplay.card.cardName}: Matou o Healer - pode se mover novamente!");
            }
        }
    }

    // Efeito 2: Archer 4 (ATK 6, HP 3) - Stune um alvo, pode reutilizar a cada 2 turnos
    void ArcherTier4Effect2_StunEvery2Turns()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via método separado quando atacar
        Debug.Log($"[ArcherTier4Effect2] {cardDisplay.card.cardName}: Pronta para stunar (pode reutilizar a cada 2 turnos)");
    }

    public void ActivateStunEvery2Turns(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        if (TurnManager.Instance == null) return;

        // Verifica se pode reutilizar a cada 2 turnos
        if (TurnManager.Instance.currentRound - cardDisplay.archerTier4Effect2LastUsedRound >= 2)
        {
            targetEnemy.Stun();
            cardDisplay.archerTier4Effect2LastUsedRound = TurnManager.Instance.currentRound;
            Debug.Log($"[ArcherTier4Effect2] {cardDisplay.card.cardName}: Stuneu {targetEnemy.card.cardName}!");
        }
        else
        {
            Debug.Log($"[ArcherTier4Effect2] {cardDisplay.card.cardName}: Stun ainda em cooldown");
        }
    }

    // Efeito 3: Archer 4 (ATK 7, HP 3) - Cria cópia ao matar, move novamente se tem Tank
    void ArcherTier4Effect3_CopyOnKill()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado quando mata um alvo (via hook em TakeDamage)
        Debug.Log($"[ArcherTier4Effect3] {cardDisplay.card.cardName}: Pronta para copiar ao matar");
    }

    public void ActivateCopyOnKill()
    {
        if (cardDisplay == null || cardDisplay.currentTile == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Cria uma cópia
        var emptyTile = board.FindAdjacentEmptyTile(cardDisplay.currentTile, cardDisplay.ownerPlayerNumber);
        if (emptyTile != null)
        {
            CardDisplay copy = cardDisplay.SpawnCardCopy(emptyTile);
            Debug.Log($"[ArcherTier4Effect3] {cardDisplay.card.cardName}: Criou uma cópia!");
        }

        // Se tem Tank aliado, pode se mover novamente
        if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank))
        {
            cardDisplay.lastMovedRound = -1;
            Debug.Log($"[ArcherTier4Effect3] {cardDisplay.card.cardName}: Tem Tank aliado - pode se mover novamente!");
        }
    }

    // Efeito 4: Archer 4 (ATK 6, HP 2) - Move novamente se atacar alvo ao lado
    void ArcherTier4Effect4_ExtraMoveOnSideAttack()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado quando ataca um alvo adjacente (via hook em AttackAdjacentEnemy)
        Debug.Log($"[ArcherTier4Effect4] {cardDisplay.card.cardName}: Pronta para se mover novamente ao atacar ao lado");
    }

    public void CheckSideAttackAndMove(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        if (cardDisplay.currentTile == null || targetEnemy.currentTile == null) return;

        // Verifica se o alvo é adjacente (ao lado)
        int rowDiff = Mathf.Abs(cardDisplay.currentTile.row - targetEnemy.currentTile.row);
        int colDiff = Mathf.Abs(cardDisplay.currentTile.column - targetEnemy.currentTile.column);

        // Adjacente = diferença de 1 em apenas um eixo
        if ((rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1))
        {
            cardDisplay.lastMovedRound = -1;
            Debug.Log($"[ArcherTier4Effect4] {cardDisplay.card.cardName}: Atacou ao lado - pode se mover novamente!");
        }
    }

    // ===== ARCHER TIER-3 =====
    public void ArcherTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 4 && baseHp == 2)
            ArcherTier3Effect1_InvokeEagle();
        else if (baseAtk == 5 && baseHp == 3)
            ArcherTier3Effect2_CopyIfMageAlly();
        else if (baseAtk == 3 && baseHp == 2)
            ArcherTier3Effect3_DamageToTowerAndExtraMove();
        else if (baseAtk == 4 && baseHp == 1)
            ArcherTier3Effect4_CrussDamageAndShield();
    }

    // Efeito 1: Archer 3 (ATK 4, HP 2) - Invoca uma águia para perseguir um inimigo aleatório
    void ArcherTier3Effect1_InvokeEagle()
    {
        if (cardDisplay == null || cardDisplay.archerTier3Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Encontra todos os inimigos
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Escolhe um inimigo aleatório
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];

        if (targetEnemy != null)
        {
            targetEnemy.MarkWithEagle();
            cardDisplay.archerTier3Effect1Used = true;
            Debug.Log($"[ArcherTier3Effect1] {cardDisplay.card.cardName}: Invocou uma águia para perseguir {targetEnemy.card.cardName}!");
        }
    }

    // Efeito 2: Archer 3 (ATK 5, HP 3) - Faz uma cópia se houver Mago aliado
    void ArcherTier3Effect2_CopyIfMageAlly()
    {
        if (cardDisplay == null || cardDisplay.archerTier3Effect2Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null) return;

        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);

        if (hasMageAlly)
        {
            var emptyTile = board.FindAdjacentEmptyTile(cardDisplay.currentTile, cardDisplay.ownerPlayerNumber);

            if (emptyTile != null)
            {
                CardDisplay copy = cardDisplay.SpawnCardCopy(emptyTile);
                if (copy != null)
                {
                    cardDisplay.archerTier3Effect2Used = true;
                    Debug.Log($"[ArcherTier3Effect2] {cardDisplay.card.cardName}: Criou uma cópia em {emptyTile.row},{emptyTile.column}!");
                }
            }
        }
        else
        {
            Debug.Log($"[ArcherTier3Effect2] {cardDisplay.card.cardName}: Nenhum mago aliado - efeito não ativado");
        }
    }

    // Efeito 3: Archer 3 (ATK 3, HP 2) - Causa 2 de dano à torre inimiga + 2 movimentações se houver Mago
    void ArcherTier3Effect3_DamageToTowerAndExtraMove()
    {
        if (cardDisplay == null || cardDisplay.archerTier3Effect3Used) return;

        // Causa dano à torre inimiga
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        PlayerData enemyPlayer = TurnManager.Instance.GetPlayer(enemyPlayerNumber);

        if (enemyPlayer != null)
        {
            enemyPlayer.TakeDamage(2);
            cardDisplay.archerTier3Effect3Used = true;
            Debug.Log($"[ArcherTier3Effect3] {cardDisplay.card.cardName}: Causou 2 de dano à torre inimiga!");
        }

        // Se houver Mago aliado, permite 2 movimentações neste turno
        BoardManager board = BoardManager.Instance;
        if (board != null && board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago))
        {
            // Permite 2 movimentações resetting lastMovedRound
            cardDisplay.lastMovedRound = -1;
            Debug.Log($"[ArcherTier3Effect3] {cardDisplay.card.cardName}: Tem um mago aliado - pode se mover 2 vezes neste turno!");
        }
    }

    // Efeito 4: Archer 3 (ATK 4, HP 1) - Causa 3 de dano em padrão + (cruz) + ganha 4 de armadura se houver Tank
    void ArcherTier3Effect4_CrussDamageAndShield()
    {
        if (cardDisplay == null || cardDisplay.archerTier3Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null) return;

        int row = cardDisplay.currentTile.row;
        int col = cardDisplay.currentTile.column;
        int playerNum = cardDisplay.ownerPlayerNumber;

        // Dano em padrão + (cruz): mesma linha, mesma coluna (adjacentes)
        int[] targetRows = { row - 1, row + 1, row, row };
        int[] targetCols = { col, col, col - 1, col + 1 };

        for (int i = 0; i < 4; i++)
        {
            int targetRow = targetRows[i];
            int targetCol = targetCols[i];

            if (targetRow >= 0 && targetRow < board.rows && targetCol >= 0 && targetCol < board.columns)
            {
                CardTile targetTile = board.GetTile(targetRow, targetCol);
                if (targetTile != null && targetTile.occupiedCard != null)
                {
                    CardDisplay targetCard = targetTile.occupiedCard.GetComponent<CardDisplay>();
                    if (targetCard != null && targetCard.ownerPlayerNumber != playerNum)
                    {
                        targetCard.TakeDamage(3);
                        Debug.Log($"[ArcherTier3Effect4] {cardDisplay.card.cardName}: Causou 3 de dano a {targetCard.card.cardName} (padrão +)");
                    }
                }
            }
        }

        cardDisplay.archerTier3Effect4Used = true;

        // Se houver Tank aliado, ganha 4 de armadura
        if (board.HasClassOnBoard(playerNum, CardClass.Tank))
        {
            cardDisplay.currentShield += 4;
            cardDisplay.UpdateDisplay();
            Debug.Log($"[ArcherTier3Effect4] {cardDisplay.card.cardName}: Tem um tank aliado - ganhou 4 de armadura! Total: {cardDisplay.currentShield}");
        }
    }

    // ===== ARCHER TIER-2 =====
    public void ArcherTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 3 && baseHp == 3)
            ArcherTier2Effect1_InvokeOnKill();
        else if (baseAtk == 3 && baseHp == 1)
            ArcherTier2Effect3_StunOnHit();
        // Archer 2 (ATK 3, HP 2) tem efeito reativo, não é ativado ao entrar em campo

        // Verifica combo das 3 cartas
        CheckArcherTier2Combo();
    }

    // Efeito 1: Archer 2 (ATK 3, HP 3) - Invoca Archer aleatório quando destrói inimigo
    void ArcherTier2Effect1_InvokeOnKill()
    {
        // Este efeito é ativado quando a carta destrói um inimigo
        // Ver CardDisplay - TakeDamage()
        Debug.Log($"[ArcherTier2Effect1] {cardDisplay.card.cardName}: Pronta para invocar Archer ao matar");
    }

    // Efeito 2: Archer 2 (ATK 3, HP 2) - Para ataque de Healer e stuna atacante (ativado via popup)
    public void ArcherTier2Effect2_ShieldArrow(CardDisplay attackingCard)
    {
        if (cardDisplay == null || attackingCard == null) return;

        if (cardDisplay.archerShieldArrowUsed)
        {
            Debug.Log($"[ArcherTier2Effect2] {cardDisplay.card.cardName}: Efeito já foi usado nesta partida!");
            return;
        }

        // Para o ataque
        attackingCard.Stun();
        cardDisplay.archerShieldArrowUsed = true;

        Debug.Log($"[ArcherTier2Effect2] {cardDisplay.card.cardName}: Parou ataque e stunou {attackingCard.card.cardName}!");
    }

    // Efeito 3: Archer 2 (ATK 3, HP 1) - Stuna o atacante ao receber ataque
    void ArcherTier2Effect3_StunOnHit()
    {
        // Este efeito é ativado via popup quando recebe ataque
        Debug.Log($"[ArcherTier2Effect3] {cardDisplay.card.cardName}: Pronta para stunar ao receber ataque");
    }

    public void ArcherTier2Effect3_ActivateStun(CardDisplay attackingCard)
    {
        if (cardDisplay == null || attackingCard == null) return;

        if (cardDisplay.archerStunOnHitUsed)
        {
            Debug.Log($"[ArcherTier2Effect3] {cardDisplay.card.cardName}: Efeito já foi usado nesta partida!");
            return;
        }

        attackingCard.Stun();
        cardDisplay.archerStunOnHitUsed = true;

        Debug.Log($"[ArcherTier2Effect3] {cardDisplay.card.cardName}: Stuneu {attackingCard.card.cardName}!");
    }

    // Combo: Quando as 3 Archers tier-2 estão em campo, todas ganham +5 ATK
    void CheckArcherTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int archersInPlay = 0;

        // Conta quantos Archers tier-2 estão em campo
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Arqueiro && ally.card.tier == CardTier.Tier2)
            {
                if ((ally.card.attack == 3 && ally.card.health == 2) ||
                    (ally.card.attack == 3 && ally.card.health == 3) ||
                    (ally.card.attack == 3 && ally.card.health == 1))
                {
                    archersInPlay++;
                }
            }
        }

        // Se os 3 Archers tier-2 estão em campo, ativa combo
        if (archersInPlay >= 3)
        {
            foreach (var ally in allies)
            {
                if (ally != null && ally.card.cardClass == CardClass.Arqueiro && ally.card.tier == CardTier.Tier2)
                {
                    if ((ally.card.attack == 3 && ally.card.health == 2) ||
                        (ally.card.attack == 3 && ally.card.health == 3) ||
                        (ally.card.attack == 3 && ally.card.health == 1))
                    {
                        ally.currentAttack += 5;
                        ally.archerComboActivated = true;
                        ally.UpdateDisplay();
                    }
                }
            }

            Debug.Log($"[ArcherCombo] Os 3 Archers tier-2 estão em campo! +5 ATK para todos!");
        }
    }

    // ===== ARCHER TIER-1 =====
    public void ArcherEffect()
    {
        if (cardDisplay == null) return;

        // Identifica qual efeito rodar baseado nos stats da carta
        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseHp == 3)
            ArcherEffect1_SelfDamageForAttack();
        else if (baseAtk == 1 && baseHp == 2)
            ArcherEffect2_DamageRow();
        else if (baseAtk == 2 && baseHp == 2)
            ArcherEffect3_CopyIfTankAlly();
        else if (baseAtk == 3 && baseHp == 2)
            ArcherEffect4_TreeDodge();
    }

    // Efeito 1: Perca 2 de vida para ganhar +1 de ataque
    void ArcherEffect1_SelfDamageForAttack()
    {
        cardDisplay.currentHealth -= 2;
        cardDisplay.currentAttack += 1;

        if (cardDisplay.currentHealth < 0)
            cardDisplay.currentHealth = 0;

        cardDisplay.UpdateDisplay();

        Debug.Log($"[ArcherEffect1] {cardDisplay.card.cardName}: -2 HP, +1 ATK. HP agora: {cardDisplay.currentHealth}, ATK agora: {cardDisplay.currentAttack}");
    }

    // Efeito 2: Ao entrar em campo, cause 1 de dano à carta inimiga diretamente à sua frente
    void ArcherEffect2_DamageRow()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null)
        {
            Debug.LogWarning($"[ArcherEffect2] Board ou currentTile é null!");
            return;
        }

        int currentRow = cardDisplay.currentTile.row;
        int currentCol = cardDisplay.currentTile.column;
        int playerNum = cardDisplay.ownerPlayerNumber;

        Debug.Log($"[ArcherEffect2 Debug] {cardDisplay.card.cardName} (P{playerNum}) está em Row:{currentRow}, Col:{currentCol}");

        // Determina qual row está "à frente" baseado no dono
        int targetRow = (playerNum == 1) ? currentRow + 1 : currentRow - 1;

        Debug.Log($"[ArcherEffect2 Debug] Procurando inimigo em Row:{targetRow}, Col:{currentCol}");

        // Verifica limites
        if (targetRow < 0 || targetRow >= board.rows)
        {
            Debug.Log($"[ArcherEffect2] Row {targetRow} fora dos limites!");
            return;
        }

        // Procura a carta inimiga diretamente à frente (mesma coluna)
        CardTile targetTile = board.GetTile(targetRow, currentCol);
        if (targetTile == null)
        {
            Debug.LogWarning($"[ArcherEffect2] Tile em {targetRow},{currentCol} é null!");
            return;
        }

        if (targetTile.occupiedCard != null)
        {
            CardDisplay targetCard = targetTile.occupiedCard.GetComponent<CardDisplay>();
            if (targetCard != null)
            {
                Debug.Log($"[ArcherEffect2 Debug] Encontrou carta: {targetCard.card.cardName} (P{targetCard.ownerPlayerNumber})");

                if (targetCard.ownerPlayerNumber != playerNum)
                {
                    targetCard.TakeDamage(1);
                    Debug.Log($"[ArcherEffect2] {cardDisplay.card.cardName}: Causou 1 de dano a {targetCard.card.cardName}");
                    return;
                }
                else
                {
                    Debug.Log($"[ArcherEffect2] Carta à frente é aliada, sem dano");
                    return;
                }
            }
        }

        Debug.Log($"[ArcherEffect2] Nenhuma carta em Row:{targetRow}, Col:{currentCol}");
    }

    // Efeito 3: Faz uma cópia de si se estiver com um tanque aliado em campo
    void ArcherEffect3_CopyIfTankAlly()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null) return;

        bool hasTankAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);

        if (hasTankAlly)
        {
            // Tenta encontrar um tile vazio adjacent para spawnar a cópia
            var emptyTile = board.FindAdjacentEmptyTile(cardDisplay.currentTile, cardDisplay.ownerPlayerNumber);

            if (emptyTile != null)
            {
                CardDisplay copy = cardDisplay.SpawnCardCopy(emptyTile);
                if (copy != null)
                {
                    Debug.Log($"[ArcherEffect3] {cardDisplay.card.cardName}: Criou uma cópia em {emptyTile.row},{emptyTile.column}");
                }
                else
                {
                    Debug.Log($"[ArcherEffect3] {cardDisplay.card.cardName}: Falhou ao criar cópia (sem espaço)");
                }
            }
        }
        else
        {
            Debug.Log($"[ArcherEffect3] {cardDisplay.card.cardName}: Nenhum tanque aliado em campo - efeito não ativado");
        }
    }

    // Efeito 4: Pode 1 vez por partida subir em uma árvore e ficar intangível por 1 turno
    void ArcherEffect4_TreeDodge()
    {
        if (cardDisplay == null) return;

        // O efeito é ativado quando a carta toma dano e o jogador clica em "Sim" no popup
        // Aqui apenas marcamos que a carta está pronta para usar o efeito
        Debug.Log($"[ArcherEffect4] {cardDisplay.card.cardName}: Pronta para usar a árvore (pode esquivar uma vez)");
    }

    // ===== HEALER TIER-4 =====
    public void HealerTier4Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 3 && baseHp == 3)
            HealerTier4Effect1_PeriodicCure();
        else if (baseAtk == 4 && baseHp == 3)
            HealerTier4Effect2_GoldOnOpponentTurnEnd();
        else if (baseAtk == 5 && baseHp == 3)
            HealerTier4Effect3_GrantInvulnerability();
        else if (baseAtk == 4 && baseHp == 4)
            HealerTier4Effect4_BoostAllWithCombo();
    }

    // Efeito 1: Healer 4 (ATK 3, HP 3) - Cura 4 a cada 2 rounds, ganha 2 ouro se tem Mago
    void HealerTier4Effect1_PeriodicCure()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via CheckPeriodicEffects a cada 2 rounds
        Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Pronta para curar aliados a cada 2 rounds");
    }

    public void ActivatePeriodicCure()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Cura um aliado aleatório
        CardDisplay targetAlly = allies[Random.Range(0, allies.Count)];
        if (targetAlly != null)
        {
            targetAlly.Heal(4, cardDisplay);
            Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Curou {targetAlly.card.cardName} em 4!");

            // Se tem Mago aliado, ganha 2 de ouro
            if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago))
            {
                PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
                if (player != null)
                {
                    player.AddGold(2);
                    Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Tem Mago aliado - ganhou 2 de ouro!");
                }
            }
        }
    }

    // Efeito 2: Healer 4 (ATK 4, HP 3) - Recebe 1 ouro extra ao fim do turno do oponente
    void HealerTier4Effect2_GoldOnOpponentTurnEnd()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook quando o turno do oponente acaba
        Debug.Log($"[HealerTier4Effect2] {cardDisplay.card.cardName}: Pronta para ganhar ouro ao fim do turno do oponente");
    }

    public void ActivateGoldOnOpponentTurnEnd()
    {
        if (cardDisplay == null) return;

        PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.AddGold(1);
            Debug.Log($"[HealerTier4Effect2] {cardDisplay.card.cardName}: Ganhou 1 ouro ao fim do turno do oponente!");
        }
    }

    // Efeito 3: Healer 4 (ATK 5, HP 3) - Concede invunerabilidade a uma carta por 3 rounds
    void HealerTier4Effect3_GrantInvulnerability()
    {
        if (cardDisplay == null || cardDisplay.healerTier4Effect3Used) return;

        // Este efeito é ativado via popup para escolher qual carta
        ShowInvulnerabilityPopup();
    }

    void ShowInvulnerabilityPopup()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // O dono clica no aliado desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 8,
                new List<CardDisplay>(allies),
                "Escolha qual aliado receberá invulnerabilidade por 3 rounds");
        }
    }

    public void ActivateInvulnerability(CardDisplay targetAlly)
    {
        if (cardDisplay == null || targetAlly == null) return;

        if (TurnManager.Instance == null) return;

        // Marca a carta como invunerável por 3 rounds
        targetAlly.treeDefenseActive = true;
        cardDisplay.healerTier4Effect3Used = true;
        Debug.Log($"[HealerTier4Effect3] {cardDisplay.card.cardName}: Concedeu invunerabilidade a {targetAlly.card.cardName} por 3 rounds!");
    }

    public void CheckAndRemoveInvulnerability(CardDisplay card)
    {
        if (card == null || TurnManager.Instance == null) return;

        // Remove invunerabilidade após 3 rounds
        // Isso precisa ser rastreado melhor, mas por enquanto usamos o treeDefenseActive
        // que é resetado a cada turno em TurnManager
    }

    // Efeito 4: Healer 4 (ATK 4, HP 4) - +3 todos status a todos aliados se tem Tank, Arqueiro e Mago
    void HealerTier4Effect4_BoostAllWithCombo()
    {
        if (cardDisplay == null || cardDisplay.healerTier4Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasTankAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);
        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);
        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);

        if (hasTankAlly && hasArcherAlly && hasMageAlly)
        {
            var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
            foreach (var ally in allies)
            {
                if (ally != null)
                {
                    ally.currentAttack += 3;
                    ally.currentShield += 3;
                    ally.currentHealth += 3;
                    ally.UpdateDisplay();
                }
            }

            cardDisplay.healerTier4Effect4Used = true;
            Debug.Log($"[HealerTier4Effect4] {cardDisplay.card.cardName}: Tem Tank, Arqueiro e Mago! Todos aliados ganharam +3 em todos status!");
        }
        else
        {
            Debug.Log($"[HealerTier4Effect4] {cardDisplay.card.cardName}: Faltam aliados da combo (Tank, Arqueiro, Mago)");
        }
    }

    // ===== HEALER TIER-3 =====
    public void HealerTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 3 && baseHp == 3)
            HealerTier3Effect1_GoldIfMage();
        else if (baseAtk == 3 && baseHp == 1)
            HealerTier3Effect2_CureTankOnDamage();
        else if (baseAtk == 2 && baseHp == 1)
            HealerTier3Effect3_GoldPerHealerAndMage();
        else if (baseAtk == 1 && baseHp == 2)
            HealerTier3Effect4_GoldPerMage();
    }

    // Efeito 1: Healer 3 (ATK 3, HP 3) - Ganha 2 de ouro se houver Mago em campo
    void HealerTier3Effect1_GoldIfMage()
    {
        if (cardDisplay == null || cardDisplay.healerTier3Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);

        if (hasMageAlly)
        {
            PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                player.AddGold(2);
                cardDisplay.healerTier3Effect1Used = true;
                Debug.Log($"[HealerTier3Effect1] {cardDisplay.card.cardName}: Tem um mago aliado - ganhou 2 de ouro!");
            }
        }
        else
        {
            Debug.Log($"[HealerTier3Effect1] {cardDisplay.card.cardName}: Nenhum mago aliado - efeito não ativado");
        }
    }

    // Efeito 2: Healer 3 (ATK 3, HP 1) - Cura Tank em 2 sempre que ele recebe dano
    void HealerTier3Effect2_CureTankOnDamage()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook no método TakeDamage() quando um Tank recebe dano
        Debug.Log($"[HealerTier3Effect2] {cardDisplay.card.cardName}: Pronta para curar Tanks que recebem dano");
    }

    public void HealerTier3Effect2_CureTankWhenDamaged(CardDisplay damagedTank)
    {
        if (cardDisplay == null || damagedTank == null) return;

        damagedTank.currentHealth += 2;
        if (damagedTank.currentHealth > damagedTank.card.health)
            damagedTank.currentHealth = damagedTank.card.health;

        damagedTank.UpdateDisplay();
        Debug.Log($"[HealerTier3Effect2] {cardDisplay.card.cardName}: Curou {damagedTank.card.cardName} em 2 (HP agora: {damagedTank.currentHealth})");
    }

    // Efeito 3: Healer 3 (ATK 2, HP 1) - Ganha 1 de ouro por cada Healer, +1 por cada Mago
    void HealerTier3Effect3_GoldPerHealerAndMage()
    {
        if (cardDisplay == null || cardDisplay.healerTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int healerCount = 0;
        int mageCount = 0;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        foreach (var ally in allies)
        {
            if (ally != null)
            {
                if (ally.card.cardClass == CardClass.Healer)
                    healerCount++;
                if (ally.card.cardClass == CardClass.Mago)
                    mageCount++;
            }
        }

        int totalGold = healerCount + mageCount;

        if (totalGold > 0)
        {
            PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                player.AddGold(totalGold);
                cardDisplay.healerTier3Effect3Used = true;
                Debug.Log($"[HealerTier3Effect3] {cardDisplay.card.cardName}: Ganhou {totalGold} de ouro ({healerCount} Healers + {mageCount} Magos)!");
            }
        }
    }

    // Efeito 4: Healer 3 (ATK 1, HP 2) - Ganha 1 de ouro por cada Mago em campo
    void HealerTier3Effect4_GoldPerMage()
    {
        if (cardDisplay == null || cardDisplay.healerTier3Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int mageCount = 0;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Mago)
                mageCount++;
        }

        if (mageCount > 0)
        {
            PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                player.AddGold(mageCount);
                cardDisplay.healerTier3Effect4Used = true;
                Debug.Log($"[HealerTier3Effect4] {cardDisplay.card.cardName}: Ganhou {mageCount} de ouro ({mageCount} Magos em campo)!");
            }
        }
    }

    // ===== HEALER TIER-2 =====
    public void HealerTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseHp == 1)
            HealerTier2Effect1_MaxHealthOnHeal();
        else if (baseAtk == 1 && baseHp == 3)
            HealerTier2Effect2_ShieldTank();
        else if (baseAtk == 0 && baseHp == 3)
            HealerTier2Effect3_ArcherDamageBoost();
        else if (baseAtk == 2 && baseHp == 3)
            HealerTier2Effect4_ComboOnly();

        // Verifica combo das 3 Healers tier-2
        CheckHealerTier2Combo();
    }

    // Efeito 1: Healer 2 (ATK 2, HP 1) - Sempre que aliado for curado, +1 vida máxima
    public void HealerTier2Effect1_OnAllyHealed()
    {
        if (cardDisplay == null) return;

        cardDisplay.maxHealthBonus += 1;
        cardDisplay.currentHealth = cardDisplay.card.health + cardDisplay.maxHealthBonus;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[HealerTier2Effect1] Aliado ganhou +1 vida máxima! Total: {cardDisplay.card.health + cardDisplay.maxHealthBonus}");
    }

    void HealerTier2Effect1_MaxHealthOnHeal()
    {
        // Este efeito é ativado via hook no método Heal()
        Debug.Log($"[HealerTier2Effect1] {cardDisplay.card.cardName}: Pronta para aumentar vida máxima ao curar");
    }

    // Efeito 2: Healer 2 (ATK 1, HP 3) - Conceda +2 armadura a um Tank (2 vezes por turno)
    public void HealerTier2Effect2_BoostTankShield(CardDisplay targetTank)
    {
        if (cardDisplay == null || targetTank == null) return;

        if (cardDisplay.healerShieldUseCount >= 2)
        {
            Debug.Log($"[HealerTier2Effect2] {cardDisplay.card.cardName}: Já usou +2 vezes neste turno!");
            return;
        }

        targetTank.currentShield += 2;
        targetTank.UpdateDisplay();
        cardDisplay.healerShieldUseCount++;

        Debug.Log($"[HealerTier2Effect2] {cardDisplay.card.cardName}: Deu +2 armadura a {targetTank.card.cardName}! Usos: {cardDisplay.healerShieldUseCount}/2");
    }

    void HealerTier2Effect2_ShieldTank()
    {
        // Este efeito é ativado manualmente pelo jogador
        Debug.Log($"[HealerTier2Effect2] {cardDisplay.card.cardName}: Pronta para dar +2 armadura a Tanks");
    }

    // Efeito 3: Healer 2 (ATK 0, HP 3) - +2 dano a todos os Arqueiros
    void HealerTier2Effect3_ArcherDamageBoost()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int boostedCount = 0;

        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Arqueiro)
            {
                ally.currentAttack += 2;
                ally.UpdateDisplay();
                boostedCount++;
            }
        }

        Debug.Log($"[HealerTier2Effect3] {cardDisplay.card.cardName}: Aumentou +2 dano de {boostedCount} Arqueiro(s)");
    }

    // Efeito 4: Healer 2 (ATK 2, HP 3) - Apenas participa do combo
    void HealerTier2Effect4_ComboOnly()
    {
        Debug.Log($"[HealerTier2Effect4] {cardDisplay.card.cardName}: Pronta para participar do combo");
    }

    // Combo: Quando as 3 Healers tier-2 estão em campo, restaura ouro + vida do jogador
    void CheckHealerTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.healerComboActivated) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int healersInPlay = 0;

        // Conta quantas Healers tier-2 estão em campo
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Healer && ally.card.tier == CardTier.Tier2)
            {
                if ((ally.card.attack == 2 && ally.card.health == 1) ||
                    (ally.card.attack == 1 && ally.card.health == 3) ||
                    (ally.card.attack == 0 && ally.card.health == 3) ||
                    (ally.card.attack == 2 && ally.card.health == 3))
                {
                    healersInPlay++;
                }
            }
        }

        // Se as 3 Healers tier-2 estão em campo, ativa combo
        if (healersInPlay >= 3)
        {
            PlayerData player = TurnManager.Instance?.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                // Restaura ouro máximo (10)
                player.gold = 10;
                // Restaura vida máxima do jogador (10)
                player.health = 10;

                // Marca que o combo foi ativado para todas as 3
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer && ally.card.tier == CardTier.Tier2)
                    {
                        if ((ally.card.attack == 2 && ally.card.health == 1) ||
                            (ally.card.attack == 1 && ally.card.health == 3) ||
                            (ally.card.attack == 0 && ally.card.health == 3) ||
                            (ally.card.attack == 2 && ally.card.health == 3))
                        {
                            ally.healerComboActivated = true;
                        }
                    }
                }

                Debug.Log($"[HealerCombo] As 3 Healers tier-2 estão em campo! Ouro e Vida restaurados ao máximo!");
            }
        }
    }

    // ===== HEALER TIER-1 =====
    public void HealerEffect()
    {
        if (cardDisplay == null) return;

        // Identifica qual efeito rodar baseado nos stats da carta
        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 0 && baseHp == 3)
            HealerEffect1_RandomAllyPeriodicHeal();
        else if (baseAtk == 1 && baseHp == 3)
            HealerEffect2_HealAllAlliesOnEnter();
        // Efeitos 3 e 4 são ativados em outras situações (não ao entrar em campo)
    }

    // Efeito 1: Cura 2 HP a um aliado aleatório a cada 2 rounds
    void HealerEffect1_RandomAllyPeriodicHeal()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        CardDisplay targetAlly = allies[Random.Range(0, allies.Count)];
        targetAlly.Heal(2, cardDisplay);

        Debug.Log($"[HealerEffect1] {cardDisplay.card.cardName}: Curou {targetAlly.card.cardName} por 2 HP");
    }

    // Efeito 2: Ao entrar em campo, cura 2 HP em todos os aliados (apenas 1 vez)
    void HealerEffect2_HealAllAlliesOnEnter()
    {
        if (cardDisplay == null) return;

        // Verifica se já usou o efeito
        if (cardDisplay.healOnEnterUsed)
        {
            Debug.Log($"[HealerEffect2] {cardDisplay.card.cardName}: Efeito já foi usado!");
            return;
        }

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int healed = 0;

        foreach (var ally in allies)
        {
            if (ally != cardDisplay && ally != null)
            {
                ally.Heal(2, cardDisplay);
                healed++;
            }
        }

        cardDisplay.healOnEnterUsed = true;
        Debug.Log($"[HealerEffect2] {cardDisplay.card.cardName}: Curou {healed} aliado(s) por 2 HP");
    }

    // Efeito 3: Anula um ataque a cada 3 turnos (popup para aliado atacado)
    // Este efeito é ativado quando um aliado sofre dano - ver CardDisplay.cs
    public bool CanBlockAttackThisTurn()
    {
        if (cardDisplay == null) return false;

        int currentRound = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 0;
        return currentRound - lastBlockAttackRound >= 3;
    }

    public void ActivateBlockAttack()
    {
        if (cardDisplay == null) return;

        int currentRound = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 0;
        lastBlockAttackRound = currentRound;

        Debug.Log($"[HealerEffect3] {cardDisplay.card.cardName}: Bloqueou um ataque! Próxima ativação em {currentRound + 3}");
    }

    // Efeito 4: Sempre que um aliado for curado, receba 1 de ouro
    public void OnAllyHealed()
    {
        if (cardDisplay == null || cardDisplay.ownerPlayerNumber == 0) return;

        PlayerData player = TurnManager.Instance?.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.AddGold(1);
            Debug.Log($"[HealerEffect4] {cardDisplay.card.cardName}: Ganhou 1 ouro! Total: {player.gold}");
        }
    }

    // ===== MAGE TIER-4 =====
    public void MageTier4Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 7 && baseHp == 4)
            MageTier4Effect1_RemoveBonus();
        else if (baseAtk == 6 && baseHp == 6)
            MageTier4Effect2_BoostOnHealerEnter();
        else if (baseAtk == 5 && baseHp == 4)
            MageTier4Effect3_DestroyLowerTier();
        else if (baseAtk == 6 && baseHp == 3)
            MageTier4Effect4_GoldPerRound();
    }

    // Efeito 1: Mage 4 (ATK 7, HP 4) - Remove bônus de um inimigo, pode usar 2 vezes se tem Healer + Arqueiro
    void MageTier4Effect1_RemoveBonus()
    {
        if (cardDisplay == null || cardDisplay.mageTier4Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Verifica se tem Healer E Arqueiro para usar 2 vezes
        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);

        if (hasHealerAlly && hasArcherAlly)
        {
            cardDisplay.mageTier4Effect1UsesLeft = 2;
            Debug.Log($"[MageTier4Effect1] {cardDisplay.card.cardName}: Tem Healer + Arqueiro - pode usar feitiço 2 vezes!");
        }
        else
        {
            cardDisplay.mageTier4Effect1UsesLeft = 1;
        }

        // Mostra popup para escolher inimigo
        ShowRemoveBonusPopup();
    }

    void ShowRemoveBonusPopup()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // O dono clica no inimigo desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 6,
                new List<CardDisplay>(enemies),
                "Escolha um inimigo para remover os bônus");
        }
    }

    public void ActivateRemoveBonus(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        // Remove todos os status bônus (reseta para base)
        targetEnemy.currentAttack = targetEnemy.card.attack;
        targetEnemy.currentShield = targetEnemy.card.shield;
        targetEnemy.currentHealth = targetEnemy.card.health;
        targetEnemy.UpdateDisplay();

        cardDisplay.mageTier4Effect1UsesLeft--;

        if (cardDisplay.mageTier4Effect1UsesLeft <= 0)
        {
            cardDisplay.mageTier4Effect1Used = true;
        }

        Debug.Log($"[MageTier4Effect1] {cardDisplay.card.cardName}: Removeu bônus de {targetEnemy.card.cardName}! Usos restantes: {cardDisplay.mageTier4Effect1UsesLeft}");
    }

    // Efeito 2: Mage 4 (ATK 6, HP 6) - Ganha +1 ATK quando Healer entra em campo
    void MageTier4Effect2_BoostOnHealerEnter()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook quando um Healer entra em campo
        Debug.Log($"[MageTier4Effect2] {cardDisplay.card.cardName}: Pronta para ganhar +1 ATK quando Healer entrar");
    }

    public void ActivateBoostOnHealerEnter()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.UpdateDisplay();
        Debug.Log($"[MageTier4Effect2] {cardDisplay.card.cardName}: Ganhou +1 ATK! Total: {cardDisplay.currentAttack}");
    }

    // Efeito 3: Mage 4 (ATK 5, HP 4) - Destruir inimigo de nível inferior, absorve 50% ataque se tem Tank + Healer + Arqueiro
    void MageTier4Effect3_DestroyLowerTier()
    {
        if (cardDisplay == null || cardDisplay.mageTier4Effect3Used) return;

        // Mostra popup para escolher inimigo de nível inferior
        ShowDestroyLowerTierPopup();
    }

    void ShowDestroyLowerTierPopup()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        // Filtra apenas inimigos de nível inferior
        var lowerTierEnemies = enemies.FindAll(e => e != null && e.card.tier < cardDisplay.card.tier);

        if (lowerTierEnemies.Count == 0)
        {
            Debug.Log($"[MageTier4Effect3] {cardDisplay.card.cardName}: Nenhum inimigo de nível inferior!");
            return;
        }

        // O dono clica no inimigo desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 5,
                lowerTierEnemies,
                "Escolha um inimigo de nível inferior para destruir");
        }
    }

    public void ActivateDestroyLowerTier(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        BoardManager board = BoardManager.Instance;
        bool hasTankAlly = board != null && board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);
        bool hasHealerAlly = board != null && board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasArcherAlly = board != null && board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);

        // Copia os status da carta a ser destruída antes de destruir
        int destroyedAtk = targetEnemy.currentAttack;
        int destroyedShield = targetEnemy.currentShield;
        int destroyedHp = targetEnemy.currentHealth;

        targetEnemy.DestroyCard();
        cardDisplay.mageTier4Effect3Used = true;

        // Se tem Tank, Healer E Arqueiro, ganha metade dos status da carta destruída
        if (hasTankAlly && hasHealerAlly && hasArcherAlly)
        {
            int gainAtk = destroyedAtk / 2;
            int gainShield = destroyedShield / 2;
            int gainHp = destroyedHp / 2;

            cardDisplay.currentAttack += gainAtk;
            cardDisplay.currentShield += gainShield;
            cardDisplay.currentHealth += gainHp;
            cardDisplay.UpdateDisplay();

            Debug.Log($"[MageTier4Effect3] {cardDisplay.card.cardName}: Destruiu {targetEnemy.card.cardName} e ganhou metade dos status: ATK +{gainAtk}, Shield +{gainShield}, HP +{gainHp}!");
        }
        else
        {
            Debug.Log($"[MageTier4Effect3] {cardDisplay.card.cardName}: Destruiu {targetEnemy.card.cardName}! (Faltam aliados para ganhar status)");
        }
    }

    // Efeito 4: Mage 4 (ATK 6, HP 3) - Uma vez por round ganha +1 ouro
    void MageTier4Effect4_GoldPerRound()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado quando a carta entra em campo
        Debug.Log($"[MageTier4Effect4] {cardDisplay.card.cardName}: Pronta para ganhar +1 ouro uma vez por round");
    }

    public void ActivateGoldPerRound()
    {
        if (cardDisplay == null) return;

        if (TurnManager.Instance == null) return;

        // Verifica se já usou neste round
        if (cardDisplay.mageTier4Effect4LastUsedRound >= TurnManager.Instance.currentRound)
        {
            Debug.Log($"[MageTier4Effect4] {cardDisplay.card.cardName}: Já usou neste round!");
            return;
        }

        PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.AddGold(1);
            cardDisplay.mageTier4Effect4LastUsedRound = TurnManager.Instance.currentRound;
            Debug.Log($"[MageTier4Effect4] {cardDisplay.card.cardName}: Ganhou +1 ouro! Total: {player.gold}");
        }
    }

    // ===== MAGE TIER-3 =====
    public void MageTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 0 && baseHp == 1)
            MageTier3Effect1_StealStats();
        else if (baseAtk == 4 && baseHp == 4)
            MageTier3Effect2_BoostAllWithArcher();
        else if (baseAtk == 3 && baseHp == 2)
            MageTier3Effect3_FreezeOrDamage();
        else if (baseAtk == 3 && baseHp == 3)
            MageTier3Effect4_BoostHandCards();
    }

    // Efeito 1: Mage 3 (ATK 0, HP 1) - Rouba todos os status de um inimigo aleatório (inimigo fica com 0)
    void MageTier3Effect1_StealStats()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Escolhe um inimigo aleatório
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];

        if (targetEnemy != null)
        {
            // Copia os status do inimigo
            int stolenAtk = targetEnemy.currentAttack;
            int stolenShield = targetEnemy.currentShield;
            int stolenHp = targetEnemy.currentHealth;

            // Aplica ao Mago
            cardDisplay.currentAttack += stolenAtk;
            cardDisplay.currentShield += stolenShield;
            cardDisplay.currentHealth += stolenHp;

            // Se houver Arqueiro aliado, duplica o ATK roubado
            if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro))
            {
                cardDisplay.currentAttack += stolenAtk;
                Debug.Log($"[MageTier3Effect1] {cardDisplay.card.cardName}: Roubou stats de {targetEnemy.card.cardName} (ATK: {stolenAtk}, Shield: {stolenShield}, HP: {stolenHp}) e duplicou ATK! ATK total agora: {cardDisplay.currentAttack}");
            }
            else
            {
                Debug.Log($"[MageTier3Effect1] {cardDisplay.card.cardName}: Roubou stats de {targetEnemy.card.cardName} (ATK: {stolenAtk}, Shield: {stolenShield}, HP: {stolenHp})");
            }

            // Inimigo fica com status 0, mas NÃO é destruído — a carta permanece
            // em campo com os status zerados (morre se levar qualquer dano depois)
            targetEnemy.currentAttack = 0;
            targetEnemy.currentShield = 0;
            targetEnemy.currentHealth = 0;
            targetEnemy.UpdateDisplay();

            cardDisplay.mageTier3Effect1Used = true;
            cardDisplay.UpdateDisplay();
        }
    }

    // Efeito 2: Mage 3 (ATK 4, HP 4) - Concede +1 de ataque a todos no campo se houver Arqueiro
    void MageTier3Effect2_BoostAllWithArcher()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect2Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);

        if (hasArcherAlly)
        {
            var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
            foreach (var ally in allies)
            {
                if (ally != null)
                {
                    ally.currentAttack += 1;
                    ally.UpdateDisplay();
                }
            }

            cardDisplay.mageTier3Effect2Used = true;
            Debug.Log($"[MageTier3Effect2] {cardDisplay.card.cardName}: Tem um arqueiro aliado - todos os aliados ganharam +1 ATK!");
        }
        else
        {
            Debug.Log($"[MageTier3Effect2] {cardDisplay.card.cardName}: Nenhum arqueiro aliado - efeito não ativado");
        }
    }

    // Efeito 3: Mage 3 (ATK 3, HP 2) - Escolhe congelar OU dano (popup) ou ambos se tiver Healer e Tank
    void MageTier3Effect3_FreezeOrDamage()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasTankAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);

        if (hasHealerAlly && hasTankAlly)
        {
            // Tem ambos: Congela E causa dano no mesmo inimigo (pergunta qual)
            ShowFreezeAndDamagePopup();
        }
        else
        {
            // Sem ambos: Popup escolhendo entre congelar OU dano
            ShowFreezeOrDamageChoicePopup();
        }

        Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Pronta para congelar ou causar dano");
    }

    void ShowFreezeOrDamageChoicePopup()
    {
        // Decisão sincronizada: o dono do Mago escolhe (Random dos callbacks roda re-seedado)
        PhotonGameManager.AskEffectDecision(cardDisplay.ownerPlayerNumber,
            $"{cardDisplay.card.cardName} vai congelar ou causar dano?",
            "Congelar", "Causar Dano",
            accepted =>
            {
                if (accepted) ActivateFreezeOnly();
                else ActivateDamageOnly();
            });
    }

    void ShowFreezeAndDamagePopup()
    {
        // Decisão sincronizada: o dono do Mago escolhe
        PhotonGameManager.AskEffectDecision(cardDisplay.ownerPlayerNumber,
            $"{cardDisplay.card.cardName} vai congelar E causar dano!",
            "Escolher Inimigo", "Cancelar",
            accepted =>
            {
                if (accepted) ActivateFreezeAndDamageChoice();
            });
    }

    public void ActivateFreezeOnly()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Encontra inimigos
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Congela um inimigo aleatório
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];
        if (targetEnemy != null)
        {
            targetEnemy.Freeze();
            cardDisplay.mageTier3Effect3Used = true;
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Congelou {targetEnemy.card.cardName}!");
        }
    }

    public void ActivateDamageOnly()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Encontra inimigos
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Causa dano a um inimigo aleatório
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];
        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(1);
            cardDisplay.mageTier3Effect3Used = true;
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Causou 1 de dano a {targetEnemy.card.cardName}!");
        }
    }

    public void ActivateFreezeAndDamageChoice()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Encontra inimigos
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Escolhe um inimigo aleatório e congela + causa dano
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];
        if (targetEnemy != null)
        {
            targetEnemy.Freeze();
            targetEnemy.TakeDamage(1);
            cardDisplay.mageTier3Effect3Used = true;
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Congelou E causou 1 de dano a {targetEnemy.card.cardName}!");
        }
    }

    // Efeito 4: Mage 3 (ATK 3, HP 3) - Concede +1 de ataque a todas as cartas aliadas em campo
    void MageTier3Effect4_BoostHandCards()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        foreach (var ally in allies)
        {
            if (ally != null)
            {
                ally.currentAttack += 1;
                ally.UpdateDisplay();
            }
        }

        cardDisplay.mageTier3Effect4Used = true;
        Debug.Log($"[MageTier3Effect4] {cardDisplay.card.cardName}: Concedeu +1 ATK a {allies.Count} carta(s) aliada(s) em campo!");
    }

    // ===== MAGE TIER-2 =====
    public void MageTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseHp == 3)
            MageTier2Effect1_AttackOnHealerHit();
        else if (baseAtk == 3 && baseHp == 2)
            MageTier2Effect2_FireballOnTankHit();
        else if (baseAtk == 4 && baseHp == 3)
            MageTier2Effect3_ShieldBreak();
        else if (baseAtk == 3 && baseHp == 1)
            MageTier2Effect4_FreezeOnArcherHit();

        // Verifica combo dos 3 Magos tier-2
        CheckMageTier2Combo();
    }

    // Efeito 1: Mage 2 (ATK 2, HP 3) - +1 ATK quando Healer for atacado
    void MageTier2Effect1_AttackOnHealerHit()
    {
        // Este efeito é ativado via hook quando um Healer é atacado
        Debug.Log($"[MageTier2Effect1] {cardDisplay.card.cardName}: Pronta para ganhar +1 ATK ao ataque em Healer");
    }

    public void MageTier2Effect1_BoostAttack()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[MageTier2Effect1] {cardDisplay.card.cardName}: Ganhou +1 ATK! ATK agora: {cardDisplay.currentAttack}");
    }

    // Efeito 2: Mage 2 (ATK 3, HP 2) - 2 de dano quando inimigo ataca Tank
    void MageTier2Effect2_FireballOnTankHit()
    {
        // Este efeito é ativado via hook quando um Tank é atacado
        Debug.Log($"[MageTier2Effect2] {cardDisplay.card.cardName}: Pronta para soltar bola de fogo no atacante");
    }

    public void MageTier2Effect2_CastFireball(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        attacker.TakeDamage(2);

        Debug.Log($"[MageTier2Effect2] {cardDisplay.card.cardName}: Lançou bola de fogo em {attacker.card.cardName}! 2 de dano");
    }

    // Efeito 3: Mage 2 (ATK 4, HP 3) - 1 de dano na armadura de 2 inimigos (ao entrar em campo)
    void MageTier2Effect3_ShieldBreak()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int hasTank = board.CountCardsByClass(cardDisplay.ownerPlayerNumber, CardClass.Tank);
        if (hasTank == 0)
        {
            Debug.Log($"[MageTier2Effect3] {cardDisplay.card.cardName}: Nenhum tanque em campo, efeito não ativado");
            return;
        }

        // Este efeito requer seleção do jogador para escolher 2 inimigos
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.StartShieldBreakSelection(cardDisplay);
        }

        Debug.Log($"[MageTier2Effect3] {cardDisplay.card.cardName}: Aguardando seleção de 2 inimigos");
    }

    public void BreakEnemyShield(CardDisplay targetEnemy)
    {
        if (targetEnemy == null) return;

        if (targetEnemy.currentShield > 0)
        {
            targetEnemy.currentShield -= 1;
            targetEnemy.UpdateDisplay();
            Debug.Log($"[MageTier2Effect3] Armadura de {targetEnemy.card.cardName} reduzida em 1! Shield agora: {targetEnemy.currentShield}");
        }
        else
        {
            Debug.Log($"[MageTier2Effect3] {targetEnemy.card.cardName} não tem armadura!");
        }
    }

    // Efeito 4: Mage 2 (ATK 3, HP 1) - Congela inimigo que ataca Arqueiro
    void MageTier2Effect4_FreezeOnArcherHit()
    {
        // Este efeito é ativado via hook quando um Arqueiro é atacado
        Debug.Log($"[MageTier2Effect4] {cardDisplay.card.cardName}: Pronta para congelar atacante de Arqueiro");
    }

    public void MageTier2Effect4_FreezeAttacker(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        attacker.Stun();

        Debug.Log($"[MageTier2Effect4] {cardDisplay.card.cardName}: Congelou {attacker.card.cardName}!");
    }

    // Combo: Quando os 3 Magos tier-2 estão em campo, invoca Mago lendário
    void CheckMageTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return; // Reusa flag de combo

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int magesInPlay = 0;

        // Conta quantos Magos tier-2 estão em campo
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Mago && ally.card.tier == CardTier.Tier2)
            {
                if ((ally.card.attack == 2 && ally.card.health == 3) ||
                    (ally.card.attack == 3 && ally.card.health == 2) ||
                    (ally.card.attack == 3 && ally.card.health == 1))
                {
                    magesInPlay++;
                }
            }
        }

        // Se os 3 Magos tier-2 estão em campo, ativa combo
        if (magesInPlay >= 3)
        {
            CardManager cardManager = CardManager.Instance;
            if (cardManager != null)
            {
                cardManager.InvokeRandomLegendaryMage(cardDisplay.ownerPlayerNumber, cardDisplay.currentTile);

                // Marca que o combo foi ativado para todos os 3
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Mago && ally.card.tier == CardTier.Tier2)
                    {
                        if ((ally.card.attack == 2 && ally.card.health == 3) ||
                            (ally.card.attack == 3 && ally.card.health == 2) ||
                            (ally.card.attack == 3 && ally.card.health == 1))
                        {
                            ally.archerComboActivated = true; // Reusa flag
                        }
                    }
                }

                Debug.Log($"[MageCombo] Os 3 Magos tier-2 estão em campo! Invocando Mago Lendário!");
            }
        }
    }

    // ===== MAGE TIER-1 =====
    public void MageEffect()
    {
        if (cardDisplay == null) return;

        // Identifica qual efeito rodar baseado nos stats da carta
        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseHp == 2)
            MageEffect1_BuffHealerOnDamage();
        else if (baseAtk == 3 && baseHp == 3)
            MageEffect2_RandomEnemyDamage();
        else if (baseAtk == 1 && baseHp == 2)
            MageEffect3_FreezeEnemy();
        else if (baseAtk == 1 && baseHp == 1)
            MageEffect5_DamageAllEnemyMages();
    }

    // Efeito 1: Conceda +1 de ataque ao healer que levar dano (ou +2 com tanque)
    void MageEffect1_BuffHealerOnDamage()
    {
        // Este efeito é ativado quando um Healer toma dano
        // Ver CardDisplay.ApplyMageEffect()
        Debug.Log($"[MageEffect1] {cardDisplay.card.cardName}: Pronta para bufar Healers");
    }

    // Efeito 2: Cause 1 de dano a um inimigo aleatório ao entrar em campo
    void MageEffect2_RandomEnemyDamage()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Pega todos os inimigos
        var allCards = board.GetAllCards();
        var enemies = allCards.FindAll(c => c.ownerPlayerNumber != cardDisplay.ownerPlayerNumber && c.ownerPlayerNumber != 0);

        if (enemies.Count == 0)
        {
            Debug.Log($"[MageEffect2] {cardDisplay.card.cardName}: Nenhum inimigo em campo");
            return;
        }

        // Escolhe um inimigo aleatório
        CardDisplay targetEnemy = enemies[Random.Range(0, enemies.Count)];
        targetEnemy.TakeDamage(1);

        Debug.Log($"[MageEffect2] {cardDisplay.card.cardName}: Causou 1 de dano a {targetEnemy.card.cardName}");
    }

    // Efeito 3: Congele um monstro inimigo por 1 turno (player seleciona o alvo)
    void MageEffect3_FreezeEnemy()
    {
        // Este efeito requer seleção do jogador
        // O jogo deve estar bloqueado até que o jogador selecione um inimigo
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.StartFreezeSelection(cardDisplay);
        }
        else
        {
            Debug.LogWarning("[MageEffect3] GameManager não encontrado!");
        }
    }

    // Efeito 5: Cause 1 de dano a todos os inimigos magos em campo
    void MageEffect5_DamageAllEnemyMages()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allCards = board.GetAllCards();
        int damageCount = 0;

        foreach (var card in allCards)
        {
            if (card.ownerPlayerNumber != cardDisplay.ownerPlayerNumber &&
                card.ownerPlayerNumber != 0 &&
                card.card.cardClass == CardClass.Mago)
            {
                card.TakeDamage(1);
                damageCount++;
            }
        }

        Debug.Log($"[MageEffect5] {cardDisplay.card.cardName}: Causou 1 de dano a {damageCount} mago(s) inimigo(s)");
    }

    public void ApplyMageEffectOnHealerDamage(CardDisplay healerThatTookDamage)
    {
        if (cardDisplay == null || healerThatTookDamage == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int bonus = 1;

        bool hasTankOnBoard = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);
        if (hasTankOnBoard)
            bonus = 2;

        healerThatTookDamage.currentAttack += bonus;
        healerThatTookDamage.UpdateDisplay();

        Debug.Log($"[MageEffect1] {cardDisplay.card.cardName}: Deu +{bonus} ATK ao {healerThatTookDamage.card.cardName}");
    }

    // ===== MAGE TIER-5 =====
    public void MageTier5Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 8 && baseHp == 4)
            MageTier5Effect1_RandomFreezePerRound();
        else if (baseAtk == 6 && baseHp == 5)
            MageTier5Effect2_CopyEnemyStats();
        else if (baseAtk == 7 && baseHp == 5)
            MageTier5Effect3_FireballAndMageBoost();
    }

    // Efeito 1: Mage 5 (ATK 8, HP 4) - Congela um inimigo aleatório por round, duplicado se tem Tank
    void MageTier5Effect1_RandomFreezePerRound()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em TurnManager.EndTurn()
        Debug.Log($"[MageTier5Effect1] {cardDisplay.card.cardName}: Pronta para congelar inimigos aleatórios");
    }

    public void ActivateRandomFreeze()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Verifica se tem Tank aliado para duplicar o efeito
        bool hasTankAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank);

        // Congela 1 inimigo aleatório
        CardDisplay target1 = enemies[Random.Range(0, enemies.Count)];
        if (target1 != null)
        {
            target1.Freeze();
            Debug.Log($"[MageTier5Effect1] {cardDisplay.card.cardName}: Congelou {target1.card.cardName}!");
        }

        // Se tem Tank aliado, congela outro inimigo aleatório
        if (hasTankAlly && enemies.Count > 1)
        {
            CardDisplay target2 = enemies[Random.Range(0, enemies.Count)];
            if (target2 != null && target2 != target1)
            {
                target2.Freeze();
                Debug.Log($"[MageTier5Effect1] {cardDisplay.card.cardName}: Tem Tank aliado - congelou {target2.card.cardName}!");
            }
        }
    }

    // Efeito 2: Mage 5 (ATK 6, HP 5) - Copia stats de ataque e vida de um inimigo
    void MageTier5Effect2_CopyEnemyStats()
    {
        if (cardDisplay == null || cardDisplay.mageTier5Effect2Used) return;

        ShowCopyStatsPopup();
    }

    void ShowCopyStatsPopup()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count == 0) return;

        // Cria lista de opções para escolher qual inimigo copiar
        List<string> enemyNames = new List<string>();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                enemyNames.Add(enemy.card.cardName);
        }

        // O dono clica no inimigo desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 4,
                new List<CardDisplay>(enemies),
                "Escolha qual inimigo terá os stats copiados");
        }
    }

    public void ActivateCopyStats(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        // Copia ataque e vida do inimigo
        cardDisplay.currentAttack = targetEnemy.currentAttack;
        cardDisplay.currentHealth = targetEnemy.currentHealth;

        cardDisplay.UpdateDisplay();
        cardDisplay.mageTier5Effect2Used = true;
        Debug.Log($"[MageTier5Effect2] {cardDisplay.card.cardName}: Copiou stats de {targetEnemy.card.cardName} (ATK: {targetEnemy.currentAttack}, HP: {targetEnemy.currentHealth})!");
    }

    // Efeito 3: Mage 5 (ATK 7, HP 5) - Causa 5 de dano ao entrar, aumenta ATK de todos Magos ao resetar turno
    void MageTier5Effect3_FireballAndMageBoost()
    {
        if (cardDisplay == null) return;

        // Causa 5 de dano a um inimigo aleatório ao entrar
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayerNumber);

        if (enemies.Count > 0)
        {
            CardDisplay target = enemies[Random.Range(0, enemies.Count)];
            if (target != null)
            {
                target.TakeDamage(5);
                Debug.Log($"[MageTier5Effect3] {cardDisplay.card.cardName}: Lançou bola de fogo em {target.card.cardName}!");
            }
        }
    }

    // ===== TANK TIER-5 =====
    public void TankTier5Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 5 && baseShield == 9 && baseHp == 6)
            TankTier5Effect1_ShieldOnKill();
        else if (baseAtk == 2 && baseShield == 6 && baseHp == 8)
            TankTier5Effect2_AttackOnDamageAndPeriodicShield();
        else if (baseAtk == 4 && baseShield == 5 && baseHp == 10)
            TankTier5Effect3_AttackOnDamageWithBonus();
    }

    // Efeito 1: Tank 5 (ATK 5, Shield 9, HP 6) - Concede armadura a aliados ao matar inimigo
    void TankTier5Effect1_ShieldOnKill()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em AttackAdjacentEnemy ou TakeDamage
        Debug.Log($"[TankTier5Effect1] {cardDisplay.card.cardName}: Pronta para conceder armadura ao matar");
    }

    public void ActivateShieldOnKill()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Concede armadura a todos os aliados
        int shieldAmount = 2;
        foreach (var ally in allies)
        {
            if (ally != null)
            {
                ally.currentShield += shieldAmount;
                ally.UpdateDisplay();
            }
        }

        Debug.Log($"[TankTier5Effect1] {cardDisplay.card.cardName}: Concedeu {shieldAmount} de armadura a todos aliados!");

        // Magos e Arqueiros ganham +1 ATK adicional
        foreach (var ally in allies)
        {
            if (ally != null && (ally.card.cardClass == CardClass.Mago || ally.card.cardClass == CardClass.Arqueiro))
            {
                ally.currentAttack += 1;
                ally.UpdateDisplay();
                Debug.Log($"[TankTier5Effect1] {cardDisplay.card.cardName}: {ally.card.cardName} ganhou +1 ATK adicional!");
            }
        }
    }

    // Efeito 2: Tank 5 (ATK 2, Shield 6, HP 8) - +1 ATK ao receber dano, concede armadura a cada 2 turnos
    void TankTier5Effect2_AttackOnDamageAndPeriodicShield()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em TakeDamage (para +1 ATK) e TurnManager (para armadura periódica)
        Debug.Log($"[TankTier5Effect2] {cardDisplay.card.cardName}: Pronta para ganhar +1 ATK ao receber dano");
    }

    public void ActivateAttackBoostOnDamage_Tier5Effect2()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.UpdateDisplay();
        Debug.Log($"[TankTier5Effect2] {cardDisplay.card.cardName}: Ganhou +1 ATK ao receber dano!");
    }

    public void ActivatePeriodicShieldTier5Effect2()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Escolhe um aliado aleatório para ganhar armadura
        CardDisplay targetAlly = allies[Random.Range(0, allies.Count)];
        if (targetAlly != null)
        {
            targetAlly.currentShield += 2;
            targetAlly.UpdateDisplay();
            Debug.Log($"[TankTier5Effect2] {cardDisplay.card.cardName}: Concedeu +2 de armadura a {targetAlly.card.cardName}!");
        }
    }

    // Efeito 3: Tank 5 (ATK 4, Shield 5, HP 10) - +1 ATK ao receber dano, +armadura se tem Healer ou Mago
    void TankTier5Effect3_AttackOnDamageWithBonus()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em TakeDamage
        Debug.Log($"[TankTier5Effect3] {cardDisplay.card.cardName}: Pronta para ganhar +1 ATK ao receber dano e armadura com aliados");
    }

    public void ActivateAttackBoostOnDamage_Tier5Effect3()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.UpdateDisplay();
        Debug.Log($"[TankTier5Effect3] {cardDisplay.card.cardName}: Ganhou +1 ATK ao receber dano!");

        // Verifica se tem Healer ou Mago aliado e ganha armadura
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);

        if (hasHealerAlly || hasMageAlly)
        {
            cardDisplay.currentShield += 1;
            cardDisplay.UpdateDisplay();
            Debug.Log($"[TankTier5Effect3] {cardDisplay.card.cardName}: Ganhou +1 de armadura por ter Healer/Mago aliado!");
        }
    }

    public void ActivateMageBoostPerTurn()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        // Aumenta ATK de todos os Magos em 1
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Mago)
            {
                ally.currentAttack += 1;
                ally.UpdateDisplay();
            }
        }

        Debug.Log($"[MageTier5Effect3] {cardDisplay.card.cardName}: Aumentou ATK de todos os Magos em 1!");
    }

    // ===== TANK TIER-4 =====
    public void TankTier4Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 1 && baseShield == 6 && baseHp == 3)
            TankTier4Effect1_BoostWithArcherMage();
        else if (baseAtk == 2 && baseShield == 6 && baseHp == 5)
            TankTier4Effect2_InterceptOncePerTurn();
        else if (baseAtk == 2 && baseShield == 3 && baseHp == 5)
            TankTier4Effect3_ArcherDoubleAttack();
        else if (baseAtk == 5 && baseShield == 10 && baseHp == 10)
            TankTier4Effect4_DamageReductionAndShield();
    }

    // Efeito 1: Tank 4 (ATK 1, Shield 6, HP 3) - +5 HP +2 Shield se tem Arqueiro e Mago
    void TankTier4Effect1_BoostWithArcherMage()
    {
        if (cardDisplay == null || cardDisplay.tankTier4Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);
        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);

        if (hasArcherAlly && hasMageAlly)
        {
            cardDisplay.currentHealth += 5;
            cardDisplay.currentShield += 2;
            cardDisplay.UpdateDisplay();
            cardDisplay.tankTier4Effect1Used = true;
            Debug.Log($"[TankTier4Effect1] {cardDisplay.card.cardName}: Tem Arqueiro + Mago - ganhou +5 HP e +2 Shield!");
        }
        else
        {
            Debug.Log($"[TankTier4Effect1] {cardDisplay.card.cardName}: Faltam aliados (Arqueiro e/ou Mago)");
        }
    }

    // Efeito 2: Tank 4 (ATK 2, Shield 6, HP 5) - Recebe ataque 1x por turno, 50% menos se tem Healer
    void TankTier4Effect2_InterceptOncePerTurn()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em TakeDamage
        Debug.Log($"[TankTier4Effect2] {cardDisplay.card.cardName}: Pronta para interceptar ataques 1x por turno");
    }

    public int GetTankTier4Effect2Reduction()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return 0;

        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        return hasHealerAlly ? 50 : 0; // Retorna 50% se tem Healer, senão 0%
    }

    // Efeito 3: Tank 4 (ATK 2, Shield 3, HP 5) - Arqueiros atacam 2 vezes se tem 4 classes
    void TankTier4Effect3_ArcherDoubleAttack()
    {
        if (cardDisplay == null || cardDisplay.tankTier4Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);
        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);
        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasTankAlly = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber)
            .Exists(c => c != null && c.card.cardClass == CardClass.Tank && c != cardDisplay);

        if (hasArcherAlly && hasMageAlly && hasHealerAlly && hasTankAlly)
        {
            // Ativa double attack para todos os Arqueiros
            var alliedArchers = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber)
                .FindAll(c => c != null && c.card.cardClass == CardClass.Arqueiro);

            foreach (var archer in alliedArchers)
            {
                archer.lastAttackedRound = -1; // Reset para permitir ataque extra
            }

            cardDisplay.tankTier4Effect3Used = true;
            Debug.Log($"[TankTier4Effect3] {cardDisplay.card.cardName}: Tem 4 classes! Arqueiros podem atacar 2 vezes este turno!");
        }
        else
        {
            Debug.Log($"[TankTier4Effect3] {cardDisplay.card.cardName}: Faltam classes para ativar (precisa Arqueiro, Mago, Healer e Tank)");
        }
    }

    // Efeito 4: Tank 4 (ATK 5, Shield 10, HP 10) - 50% menos dano se tem Healer+Mago+Arqueiro, aliados +1 armadura por turno
    void TankTier4Effect4_DamageReductionAndShield()
    {
        if (cardDisplay == null) return;

        // Este efeito é periódico e ativado via hook
        Debug.Log($"[TankTier4Effect4] {cardDisplay.card.cardName}: Pronta para reduzir dano e dar armadura a aliados");
    }

    public bool TankTier4Effect4_HasCombo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return false;

        bool hasHealerAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Healer);
        bool hasMageAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago);
        bool hasArcherAlly = board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Arqueiro);

        return hasHealerAlly && hasMageAlly && hasArcherAlly;
    }

    public void ActivateTankTier4Effect4Periodic()
    {
        if (cardDisplay == null) return;

        if (!TankTier4Effect4_HasCombo()) return;

        // Dá +1 armadura a todos os aliados
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        foreach (var ally in allies)
        {
            if (ally != null)
            {
                ally.currentShield += 1;
                ally.UpdateDisplay();
            }
        }

        Debug.Log($"[TankTier4Effect4] {cardDisplay.card.cardName}: Aliados ganharam +1 armadura este turno!");
    }

    // ===== TANK TIER-3 =====
    public void TankTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseShield == 3 && baseHp == 4)
            TankTier3Effect1_BoostHealersEvery2Turns();
        else if (baseAtk == 3 && baseShield == 2 && baseHp == 4)
            TankTier3Effect2_ReduceDamageAllTanks();
        else if (baseAtk == 2 && baseShield == 2 && baseHp == 5)
            TankTier3Effect3_BoostShieldPerTank();
        else if (baseAtk == 2 && baseShield == 2 && baseHp == 6)
            TankTier3Effect4_BoostMagoShield();
    }

    // Efeito 1: Tank 3 (ATK 2, Shield 3, HP 4) - Concede +2 armadura a todos Healers a cada 2 turnos
    void TankTier3Effect1_BoostHealersEvery2Turns()
    {
        if (cardDisplay == null || cardDisplay.tankTier3Effect1Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int healersBuffed = 0;

        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Healer)
            {
                ally.currentShield += 2;
                ally.UpdateDisplay();
                healersBuffed++;
            }
        }

        cardDisplay.tankTier3Effect1Used = true;
        Debug.Log($"[TankTier3Effect1] {cardDisplay.card.cardName}: Concedeu +2 armadura a {healersBuffed} Healer(s)!");
    }

    // Efeito 2: Tank 3 (ATK 3, Shield 2, HP 4) - Todos Tanks recebem 50% menos dano
    void TankTier3Effect2_ReduceDamageAllTanks()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook no método TakeDamage() quando um Tank recebe dano
        Debug.Log($"[TankTier3Effect2] {cardDisplay.card.cardName}: Pronta para reduzir dano de Tanks em 50%");
    }

    public int ReduceTankDamage(int originalDamage)
    {
        // Reduz dano em 50%, arredondando para baixo
        return originalDamage / 2;
    }

    // Efeito 3: Tank 3 (ATK 2, Shield 2, HP 5) - Recebe +2 armadura por cada outro Tank em campo
    void TankTier3Effect3_BoostShieldPerTank()
    {
        if (cardDisplay == null || cardDisplay.tankTier3Effect3Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int tankCount = 0;

        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Tank && ally != cardDisplay)
                tankCount++;
        }

        if (tankCount > 0)
        {
            int shieldBonus = tankCount * 2;
            cardDisplay.currentShield += shieldBonus;
            cardDisplay.UpdateDisplay();
            cardDisplay.tankTier3Effect3Used = true;
            Debug.Log($"[TankTier3Effect3] {cardDisplay.card.cardName}: Ganhou {shieldBonus} de armadura ({tankCount} outro(s) Tank(s) em campo)!");
        }
    }

    // Efeito 4: Tank 3 (ATK 2, Shield 2, HP 6) - Concede +3 armadura a um Mago à escolha
    void TankTier3Effect4_BoostMagoShield()
    {
        if (cardDisplay == null || cardDisplay.tankTier3Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        var magoAllies = allies.FindAll(c => c != null && c.card.cardClass == CardClass.Mago);

        if (magoAllies.Count == 0)
        {
            Debug.Log($"[TankTier3Effect4] {cardDisplay.card.cardName}: Nenhum Mago aliado em campo");
            return;
        }

        // Mostra popup para escolher qual Mago
        ShowMagoChoicePopup(magoAllies);
    }

    void ShowMagoChoicePopup(List<CardDisplay> magoAllies)
    {
        // O dono clica no Mago desejado (a escolha viaja por RPC como tile)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartEffectTargetSelection(cardDisplay, 9,
                magoAllies,
                "Escolha qual Mago receberá +3 de armadura");
        }
    }

    public void ActivateBoostMagoShield(CardDisplay targetMago)
    {
        if (cardDisplay == null || targetMago == null) return;

        targetMago.currentShield += 3;
        targetMago.UpdateDisplay();
        cardDisplay.tankTier3Effect4Used = true;
        Debug.Log($"[TankTier3Effect4] {cardDisplay.card.cardName}: Concedeu +3 de armadura a {targetMago.card.cardName}!");
    }

    // ===== TANK TIER-2 =====
    public void TankTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseShield == 1 && baseHp == 3)
            TankTier2Effect1_DefendHealer();
        else if (baseAtk == 2 && baseShield == 2 && baseHp == 2)
            TankTier2Effect2_DefendArcher();
        else if (baseAtk == 0 && baseShield == 4 && baseHp == 1)
            TankTier2Effect3_DefendMago();
        else if (baseAtk == 1 && baseShield == 3 && baseHp == 2)
            TankTier2Effect4_DefendAny();

        // Verifica combo dos 3 Tanks tier-2 (exceto o 4º)
        CheckTankTier2Combo();
    }

    // Efeito 1: Tank 2 (ATK 2, Shield 1, HP 3) - Recebe ataque de Healer
    void TankTier2Effect1_DefendHealer()
    {
        // Este efeito é ativado via hook quando um Healer é atacado
        Debug.Log($"[TankTier2Effect1] {cardDisplay.card.cardName}: Pronta para receber ataque de Healer");
    }

    public void TankTier2Effect1_TakeHealerAttack(CardDisplay victim, int damage)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Healer (ApplyDamageNormally evita
        // disparar novos redirecionamentos em cadeia)
        cardDisplay.ApplyDamageNormally(damage);

        Debug.Log($"[TankTier2Effect1] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 2: Tank tier 2 (ATK 2, Shield 2, HP 2) - Recebe ataque de Arqueiro
    void TankTier2Effect2_DefendArcher()
    {
        // Este efeito é ativado via hook quando um Arqueiro é atacado
        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Pronta para receber ataque de Arqueiro");
    }

    public void TankTier2Effect2_TakeArcherAttack(CardDisplay victim, int damage)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Arqueiro
        cardDisplay.ApplyDamageNormally(damage);

        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 3: Tank tier 2 (ATK 0, Shield 4, HP 1) - Recebe ataque de Mago
    void TankTier2Effect3_DefendMago()
    {
        // Este efeito é ativado via hook quando um Mago é atacado
        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Pronta para receber ataque de Mago");
    }

    public void TankTier2Effect3_TakeMagoAttack(CardDisplay victim, int damage)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Mago
        cardDisplay.ApplyDamageNormally(damage);

        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 4: Tank tier 2 (ATK 1, Shield 3, HP 2) - Pode receber qualquer ataque
    void TankTier2Effect4_DefendAny()
    {
        // Este efeito é ativado via hook quando qualquer aliado é atacado
        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Pronta para receber qualquer ataque");
    }

    public void TankTier2Effect4_TakeAnyAttack(CardDisplay victim, int damage)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano no lugar do aliado
        cardDisplay.ApplyDamageNormally(damage);

        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Combo: Quando os 3 Tanks tier-2 defensores estão em campo, +10 armadura a todos
    void CheckTankTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return; // Reusa flag de combo

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int tanksInPlay = 0;

        // Conta quantos Tanks tier-2 defensores estão em campo (exclui o 4º que defende qualquer um)
        foreach (var ally in allies)
        {
            if (ally != null && ally.card.cardClass == CardClass.Tank && ally.card.tier == CardTier.Tier2)
            {
                if ((ally.card.attack == 2 && ally.card.shield == 1 && ally.card.health == 3) ||
                    (ally.card.attack == 2 && ally.card.shield == 2 && ally.card.health == 2) ||
                    (ally.card.attack == 0 && ally.card.shield == 4 && ally.card.health == 1))
                {
                    tanksInPlay++;
                }
            }
        }

        // Se os 3 Tanks tier-2 defensores estão em campo, ativa combo
        if (tanksInPlay >= 3)
        {
            foreach (var ally in allies)
            {
                if (ally != null && ally.card.cardClass == CardClass.Tank)
                {
                    ally.currentShield += 10;
                    ally.archerComboActivated = true; // Reusa flag
                    ally.UpdateDisplay();
                }
            }

            Debug.Log($"[TankCombo] Os 3 Tanks tier-2 defensores estão em campo! +10 armadura para todos os Tanks!");
        }
    }

    // ===== TANK TIER-1 =====
    public void TankEffect()
    {
        if (cardDisplay == null) return;

        // Identifica qual efeito rodar baseado nos stats da carta
        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 0 && baseShield == 2 && baseHp == 3)
            TankEffect1_AttackPerMago();
        else if (baseAtk == 1 && baseShield == 1 && baseHp == 1)
            TankEffect2_BoostOnHeal();
        else if (baseAtk == 0 && baseShield == 2 && baseHp == 1)
            TankEffect3_AttackOnHeal();
        else if (baseAtk == 2 && baseShield == 2 && baseHp == 3)
            TankEffect5_ShieldHandIfMagoOnBoard();
    }

    // Efeito 1: Ganha +1 de ataque por cada mago em campo
    void TankEffect1_AttackPerMago()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int magoCount = board.CountCardsByClass(cardDisplay.ownerPlayerNumber, CardClass.Mago);
        int bonus = magoCount;

        cardDisplay.currentAttack += bonus;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[TankEffect1] {cardDisplay.card.cardName}: +{bonus} ATK ({magoCount} magos em campo). ATK agora: {cardDisplay.currentAttack}");
    }

    // Efeito 2: Ganha +1 de todos os atributos sempre que for curado
    public void TankEffect2_BoostOnHeal()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.currentShield += 1;
        cardDisplay.currentHealth += 1;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[TankEffect2] {cardDisplay.card.cardName}: +1 ATK, +1 Shield, +1 HP. Stats: ATK {cardDisplay.currentAttack}, Shield {cardDisplay.currentShield}, HP {cardDisplay.currentHealth}");
    }

    // Efeito 3: Ganha +1 de ataque toda vez que ganhar vida
    public void TankEffect3_AttackOnHeal()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentAttack += 1;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[TankEffect3] {cardDisplay.card.cardName}: +1 ATK. ATK agora: {cardDisplay.currentAttack}");
    }

    // Efeito 5: Se houver um mago em campo, conceda +1 de armadura as cartas na sua mão
    void TankEffect5_ShieldHandIfMagoOnBoard()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Verifica se há mago aliado em campo
        int magoCount = board.CountCardsByClass(cardDisplay.ownerPlayerNumber, CardClass.Mago);
        if (magoCount == 0)
        {
            Debug.Log($"[TankEffect5] {cardDisplay.card.cardName}: Nenhum mago em campo, efeito não ativado");
            return;
        }

        // Aumenta escudo de todas as cartas na mão
        HandManager[] handManagers = FindObjectsOfType<HandManager>();
        foreach (HandManager hand in handManagers)
        {
            if (hand.playerNumber == cardDisplay.ownerPlayerNumber)
            {
                hand.BoostHandShield(cardDisplay.ownerPlayerNumber, 1);
                Debug.Log($"[TankEffect5] {cardDisplay.card.cardName}: Concedeu +1 Shield a todas as cartas na mão");
                break;
            }
        }
    }

    // Checa e aplica efeitos periódicos (chamado a cada round)
    public void CheckPeriodicEffects(int currentRound)
    {
        if (cardDisplay == null) return;
        if (!cardDisplay.isOnBoard) return; // Só funciona se a carta está no tabuleiro

        // Healer: A cada 2 rounds
        if (cardDisplay.card.cardClass == CardClass.Healer)
        {
            // Se passaram pelo menos 2 rounds desde a última ativação
            if (currentRound - lastHealerEffectRound >= 2)
            {
                HealerEffect();
                lastHealerEffectRound = currentRound;
            }
        }
    }
}
