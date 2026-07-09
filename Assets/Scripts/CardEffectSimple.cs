using UnityEngine;

public class CardEffectSimple : MonoBehaviour
{
    CardDisplay cardDisplay;
    int lastHealerEffectRound = -2; // Rastreia o último round que o efeito foi ativado
    int lastBlockAttackRound = -3; // Rastreia o último round que bloqueou um ataque (Healer 3)

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
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

    public void TankTier2Effect1_TakeHealerAttack(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        // O Tank recebe o dano ao invés do Healer
        cardDisplay.TakeDamage(attacker.currentAttack);

        Debug.Log($"[TankTier2Effect1] {cardDisplay.card.cardName}: Recebeu ataque de {attacker.card.cardName}");
    }

    // Efeito 2: Tank tier 2 (ATK 2, Shield 2, HP 2) - Recebe ataque de Arqueiro
    void TankTier2Effect2_DefendArcher()
    {
        // Este efeito é ativado via hook quando um Arqueiro é atacado
        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Pronta para receber ataque de Arqueiro");
    }

    public void TankTier2Effect2_TakeArcherAttack(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        // O Tank recebe o dano ao invés do Arqueiro
        cardDisplay.TakeDamage(attacker.currentAttack);

        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Recebeu ataque de {attacker.card.cardName}");
    }

    // Efeito 3: Tank tier 2 (ATK 0, Shield 4, HP 1) - Recebe ataque de Mago
    void TankTier2Effect3_DefendMago()
    {
        // Este efeito é ativado via hook quando um Mago é atacado
        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Pronta para receber ataque de Mago");
    }

    public void TankTier2Effect3_TakeMagoAttack(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        // O Tank recebe o dano ao invés do Mago
        cardDisplay.TakeDamage(attacker.currentAttack);

        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Recebeu ataque de {attacker.card.cardName}");
    }

    // Efeito 4: Tank tier 2 (ATK 1, Shield 3, HP 2) - Pode receber qualquer ataque
    void TankTier2Effect4_DefendAny()
    {
        // Este efeito é ativado via hook quando qualquer aliado é atacado
        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Pronta para receber qualquer ataque");
    }

    public void TankTier2Effect4_TakeAnyAttack(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        // O Tank recebe o dano
        cardDisplay.TakeDamage(attacker.currentAttack);

        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Recebeu ataque de {attacker.card.cardName}");
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
            TankEffect2_BoostAllOnHeal();
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
