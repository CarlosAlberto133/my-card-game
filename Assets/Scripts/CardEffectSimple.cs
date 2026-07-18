using UnityEngine;
using System.Collections.Generic;

public class CardEffectSimple : MonoBehaviour
{
    CardDisplay cardDisplay;

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

        if (baseAtk == 6 && baseHp == 3)
            ArcherTier5Effect4_CopyOnKill();
        else if (baseAtk == 6 && baseHp == 4)
            ArcherTier5Effect2_RemoveEnemyArmor();
        else if (baseAtk == 5 && baseHp == 4)
            ArcherTier5Effect3_IgnoreArmorAndExecute();
    }

    // Efeito 4: Archer 5 (ATK 6, HP 3, subiu do tier 4) - Cria cópia de si ao
    // matar (o gancho fica em CardDisplay.DestroyCard → ActivateCopyOnKill)
    void ArcherTier5Effect4_CopyOnKill()
    {
        if (cardDisplay == null) return;

        Debug.Log($"[ArcherTier5Effect4] {cardDisplay.card.cardName}: Pronta para copiar ao matar");
    }

    public bool IsTargetTank(CardDisplay target)
    {
        return target != null && target.card.cardClass == CardClass.Tank;
    }

    // Efeito 2: Archer 5 (ATK 6, HP 4) - Remove 2 armadura de todos inimigos ao entrar, ignora armadura se tem Tank
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

    // Efeito 3: Archer 5 (ATK 5, HP 4) - Ignora armadura, executa se inimigo tem 2 HP ou menos
    void ArcherTier5Effect3_IgnoreArmorAndExecute()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook em AttackAdjacentEnemy
        Debug.Log($"[ArcherTier5Effect3] {cardDisplay.card.cardName}: Pronta para ignorar armadura e executar inimigos com 2 HP");
    }

    public void CheckArcherTier5Effect3_Execute(CardDisplay targetEnemy)
    {
        if (cardDisplay == null || targetEnemy == null) return;

        // Carta invulnerável não pode ser executada
        if (targetEnemy.invulnerableRoundsLeft > 0) return;

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

        if (baseAtk == 3 && baseHp == 6)
            HealerTier5Effect1_FreeCardPurchase();
        else if (baseAtk == 2 && baseHp == 7)
            HealerTier5Effect2_PeriodicAllyHeal();
        else if (baseAtk == 3 && baseHp == 7)
            HealerTier5Effect3_DoubleAllyStats();
    }

    // Efeito 1: Healer 5 (ATK 3, HP 6) - Ao entrar concede 1 compra grátis.
    // "Grátis" DE VERDADE: a próxima compra não gasta ouro nem o limite do turno
    // (antes só devolvia o slot e ainda cobrava ouro — sem ouro, não servia de nada)
    void HealerTier5Effect1_FreeCardPurchase()
    {
        if (cardDisplay == null) return;

        PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.freePurchases++;
            Debug.Log($"[HealerTier5Effect1] {cardDisplay.card.cardName}: Concedeu 1 compra grátis (sem custo de ouro)!");
        }
    }

    // Efeito 2: Healer 5 (ATK 2, HP 7) - Cura todos aliados em 2 HP e 2 shield a cada turno
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

        // CURA todos os aliados em 2 de HP e RESTAURA até 2 de escudo.
        // BUGFIX: (a) a vida agora vai por Heal() — dispara os gatilhos
        // "quando curado" como toda cura; (b) o escudo era +2 SEM TETO por
        // turno (buff infinito) — agora só restaura até o escudo base
        foreach (var ally in allies)
        {
            if (ally == null || ally.card == null) continue;

            // Vida: cura de verdade (com clamp e gatilhos dentro de Heal)
            if (ally.currentHealth < ally.card.health + ally.maxHealthBonus)
                ally.Heal(2, cardDisplay);

            // Escudo: restaura até o valor base (não empilha acima dele)
            if (ally.currentShield < ally.card.shield)
            {
                ally.currentShield = Mathf.Min(ally.currentShield + 2, ally.card.shield);
                ally.UpdateDisplay();
            }
        }

        Debug.Log($"[HealerTier5Effect2] {cardDisplay.card.cardName}: Curou os aliados (2 HP / restaura 2 de escudo)!");
    }

    // Efeito 3: Healer 5 (ATK 3, HP 7) - Duplica todos os status de um aliado à escolha
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
        else if (baseAtk == 4 && baseHp == 2)
            ArcherTier4Effect2_StunEvery2Turns();
        else if (baseAtk == 5 && baseHp == 2)
            ArcherTier4Effect5_DoubleDamageVsTank();
        else if (baseAtk == 4 && baseHp == 3)
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

            // 2ª flechada só se o alvo ainda está vivo — antes batia no
            // "cadáver" e destruía a carta duas vezes (poof/som duplicados).
            // Re-seta o atacante: o 1º TakeDamage o consumiu
            if (targetEnemy != null && targetEnemy.currentHealth > 0)
            {
                targetEnemy.attackerCardDisplay = cardDisplay;
                targetEnemy.TakeDamage(cardDisplay.currentAttack);
            }
            Debug.Log($"[ArcherTier4Effect1] {cardDisplay.card.cardName}: Atacou {targetEnemy.card.cardName} 2 vezes!");

            // Se mata o Healer, pode se movimentar novamente
            if (targetEnemy == null || targetEnemy.currentHealth <= 0)
            {
                cardDisplay.lastMovedRound = -1;
                Debug.Log($"[ArcherTier4Effect1] {cardDisplay.card.cardName}: Matou o Healer - pode se mover novamente!");
            }
        }
    }

    // Efeito 2: Archer 4 (ATK 4, HP 2) - Stuna um inimigo aleatório AO ENTRAR em
    // campo e repete a cada 2 turnos (contador amarelo na carta). Antes o stun
    // era disparado ao ATACAR, o que não batia com a descrição.
    void ArcherTier4Effect2_StunEvery2Turns()
    {
        if (cardDisplay == null) return;

        // Stun imediato ao entrar; a repetição vem pelo contador periódico
        // (SetupPeriodicCounter/OnPeriodicCounterExpired)
        ActivateRandomStun();
    }

    // Stuna um inimigo aleatório (determinístico: roda dentro de RPC nos dois
    // clientes com o Random já semeado — mesmo padrão do congelamento do Mage 5)
    public void ActivateRandomStun()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayer = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayer);
        if (enemies.Count == 0)
        {
            Debug.Log($"[ArcherTier4Effect2] {cardDisplay.card.cardName}: Nenhum inimigo em campo para stunar");
            return;
        }

        CardDisplay target = enemies[Random.Range(0, enemies.Count)];
        if (target != null)
        {
            EffectProjectileFX.Launch(cardDisplay, target, EffectProjectileFX.Arrow);
            target.Stun(cardDisplay);
            Debug.Log($"[ArcherTier4Effect2] {cardDisplay.card.cardName}: Stuneu {target.card.cardName}!");
        }
    }

    // Efeito 5: Archer 4 (ATK 5, HP 2, desceu do tier 5) - Dano em dobro
    // contra Tank em 1 ataque, 1 vez a cada 2 turnos (gancho em
    // CardDisplay.PerformAttackOn, cooldown em doubleVsTankLastUsedRound)
    void ArcherTier4Effect5_DoubleDamageVsTank()
    {
        if (cardDisplay == null) return;

        Debug.Log($"[ArcherTier4Effect5] {cardDisplay.card.cardName}: Pronta para dobrar dano contra Tank (1x a cada 2 turnos)");
    }

    public void ActivateCopyOnKill()
    {
        if (cardDisplay == null || cardDisplay.currentTile == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Cria uma cópia. A cópia nasce SEM ataque neste round (pode andar; só
        // ataca quando a vez voltar para o dono) — freio no snowball: cada
        // geração de cópias precisa esperar um round para entrar no combate
        var emptyTile = board.FindAdjacentEmptyTile(cardDisplay.currentTile, cardDisplay.ownerPlayerNumber);
        if (emptyTile != null)
        {
            CardDisplay copy = cardDisplay.SpawnCardCopy(emptyTile);
            if (copy != null) copy.BlockAttackThisRound();
            FloatingTextFX.ShowAboveCard(cardDisplay, "CÓPIA!", FloatingTextFX.EffectColor, 4.5f);
            Debug.Log($"[ArcherTier5Effect4] {cardDisplay.card.cardName}: Criou uma cópia (ataca só no próximo round)!");
        }

        // Se tem Tank aliado, pode se mover novamente
        if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank))
        {
            cardDisplay.lastMovedRound = -1;
            Debug.Log($"[ArcherTier5Effect4] {cardDisplay.card.cardName}: Tem Tank aliado - pode se mover novamente!");
        }
    }

    // Efeito 4: Archer 4 (ATK 4, HP 3) - Move novamente se atacar alvo ao lado
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

        if (baseAtk == 3 && baseHp == 3)
            ArcherTier3Effect1_InvokeEagle();
        else if (baseAtk == 4 && baseHp == 2)
            ArcherTier3Effect2_CopyIfMageAlly();
        else if (baseAtk == 3 && baseHp == 2)
            ArcherTier3Effect3_DamageToTowerAndExtraMove();
        else if (baseAtk == 4 && baseHp == 3)
            ArcherTier3Effect4_CrussDamageAndShield();
    }

    // Efeito 1: Archer 3 (ATK 3, HP 3) - Invoca uma águia para perseguir um inimigo aleatório
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

    // Efeito 2: Archer 3 (ATK 4, HP 2) - Faz uma cópia se houver Mago aliado
    void ArcherTier3Effect2_CopyIfMageAlly()
    {
        // Cópias NÃO copiam a si mesmas — sem esta trava, a cópia (com a flag
        // dela zerada) copiava de novo em cadeia até inundar o tabuleiro
        if (CardDisplay.spawningCopy)
        {
            Debug.Log("[ArcherTier3Effect2] Cópia entrou em campo - não gera nova cópia");
            return;
        }

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
                    copy.WeakenAsEcho(); // metade dos stats (min 1) + só anda neste round
                    cardDisplay.archerTier3Effect2Used = true;
                    Debug.Log($"[ArcherTier3Effect2] {cardDisplay.card.cardName}: Criou um eco ({copy.currentAttack}/{copy.currentHealth}) em {emptyTile.row},{emptyTile.column}!");
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

    // Efeito 4: Archer 3 (ATK 4, HP 3) - +4 de armadura se houver Tank (AO ENTRAR).
    // (O dano em cruz "ao atacar" foi removido a pedido do Carlos — agora a carta
    // só tem o bônus de armadura ao entrar em campo.)
    void ArcherTier3Effect4_CrussDamageAndShield()
    {
        if (cardDisplay == null || cardDisplay.archerTier3Effect4Used) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Armadura +4 com Tank aliado (1x por partida, na entrada)
        if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Tank))
        {
            cardDisplay.currentShield += 4;
            cardDisplay.UpdateDisplay();
            cardDisplay.archerTier3Effect4Used = true;
            Debug.Log($"[ArcherTier3Effect4] {cardDisplay.card.cardName}: Tem um tank aliado - ganhou 4 de armadura! Total: {cardDisplay.currentShield}");
        }
    }

    // ===== ARCHER TIER-2 =====
    public void ArcherTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        // Os membros da tríade (4/2, 3/3, 3/1) NÃO têm mais efeito solo — só a
        // tríade. (Archer 2 [3/2], fora da tríade, mantém seu efeito reativo.)

        // Verifica combo das 3 cartas
        CheckArcherTier2Combo();
    }

    // Efeito 1: Archer 2 (ATK 3, HP 2) - Invoca Archer aleatório quando destrói inimigo
    void ArcherTier2Effect1_InvokeOnKill()
    {
        // Este efeito é ativado quando a carta destrói um inimigo
        // Ver CardDisplay - TakeDamage()
        Debug.Log($"[ArcherTier2Effect1] {cardDisplay.card.cardName}: Pronta para invocar Archer ao matar");
    }

    // Efeito 2: Archer 2 (ATK 3, HP 3) - Para ataque de Healer e stuna o ATACANTE
    // (ativado via popup). Retorna true se o ataque foi de fato parado — se não
    // foi, o chamador aplica o dano normalmente (antes o dano simplesmente sumia)
    public bool ArcherTier2Effect2_ShieldArrow(CardDisplay attackingCard)
    {
        if (cardDisplay == null || attackingCard == null) return false;

        if (cardDisplay.archerShieldArrowUsed)
        {
            Debug.Log($"[ArcherTier2Effect2] {cardDisplay.card.cardName}: Efeito já foi usado nesta partida!");
            return false;
        }

        // Para o ataque
        attackingCard.Stun(cardDisplay);
        cardDisplay.archerShieldArrowUsed = true;

        FloatingTextFX.ShowAboveCard(cardDisplay, "FLECHA PROTETORA!", FloatingTextFX.EffectColor, 4.2f);
        Debug.Log($"[ArcherTier2Effect2] {cardDisplay.card.cardName}: Parou ataque e stunou {attackingCard.card.cardName}!");
        return true;
    }

    // Efeito 3: Archer 2 (ATK 2, HP 2) - Stuna o atacante ao receber ataque
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

        attackingCard.Stun(cardDisplay);
        cardDisplay.archerStunOnHitUsed = true;

        Debug.Log($"[ArcherTier2Effect3] {cardDisplay.card.cardName}: Stuneu {attackingCard.card.cardName}!");
    }

    // Combo: Quando as 3 Archers tier-2 estão em campo, todas ganham +5 ATK
    void CheckArcherTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);

        // Membros DISTINTOS da tríade: (2/2), (3/2) e (2/3 — só tríade).
        // BUGFIX: (a) duplicatas de um membro contavam como membros diferentes;
        // (b) depois de ativada, qualquer arqueiro tier-2 novo re-disparava o
        // bônus (a trava só olhava a flag da carta que ENTROU) — agora, se
        // algum membro em campo já ativou, a tríade está gasta
        bool has23 = false, has32 = false, has22 = false;
        foreach (var ally in allies)
        {
            if (ally == null || ally.card == null) continue;
            if (ally.card.cardClass != CardClass.Arqueiro || ally.card.tier != CardTier.Tier2) continue;

            if (ally.card.attack == 2 && ally.card.health == 3) { if (ally.archerComboActivated) return; has23 = true; }
            else if (ally.card.attack == 3 && ally.card.health == 2) { if (ally.archerComboActivated) return; has32 = true; }
            else if (ally.card.attack == 2 && ally.card.health == 2) { if (ally.archerComboActivated) return; has22 = true; }
        }

        // Se os 3 Archers tier-2 estão em campo, ativa combo:
        // "+5 de ataque a todos" = TODOS os aliados no tabuleiro
        if (has23 && has32 && has22)
        {
            foreach (var ally in allies)
            {
                if (ally == null || ally.card == null) continue;

                ally.currentAttack += 5;
                ally.UpdateDisplay();

                // A trava de "1x por partida" fica SÓ nos membros da tríade:
                // a flag é reutilizada pelas tríades de Mago/Tank — marcá-la
                // em outras cartas bloquearia as outras tríades
                if (ally.card.cardClass == CardClass.Arqueiro && ally.card.tier == CardTier.Tier2 &&
                    ((ally.card.attack == 2 && ally.card.health == 3) ||
                     (ally.card.attack == 3 && ally.card.health == 2) ||
                     (ally.card.attack == 2 && ally.card.health == 2)))
                {
                    ally.archerComboActivated = true;
                }
            }

            Debug.Log($"[ArcherCombo] Os 3 Archers tier-2 estão em campo! +5 ATK para TODOS os aliados!");
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
        else if (baseAtk == 2 && baseHp == 2)
            ArcherEffect2_DamageRow();
        else if (baseAtk == 1 && baseHp == 3)
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

    // Efeito 2: Ao entrar em campo, cause 1 de dano à FILEIRA à sua frente.
    // Como ao entrar a carta nasce na zona de colocação (longe do inimigo), a
    // fileira colada à frente costuma estar vazia. Em vez de não fazer nada, o
    // efeito "mira" na PRIMEIRA fileira à frente que tiver inimigos e bate em
    // todos eles. (Opção escolhida pelo Carlos — foge um pouco do texto original.)
    void ArcherEffect2_DamageRow()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null)
        {
            Debug.LogWarning($"[ArcherEffect2] Board ou currentTile é null!");
            return;
        }

        int currentRow = cardDisplay.currentTile.row;
        int playerNum = cardDisplay.ownerPlayerNumber;

        // Direção "para frente" (em direção ao inimigo): P1 sobe de fileira, P2 desce
        int step = (playerNum == 1) ? 1 : -1;

        // Varre fileira a fileira à frente e para na PRIMEIRA com inimigos.
        // Ordem fixa (fileira por fileira, coluna 0..columns) = determinístico
        // nos 2 clientes, então não quebra a sincronização do Photon.
        for (int targetRow = currentRow + step; targetRow >= 0 && targetRow < board.rows; targetRow += step)
        {
            var enemiesInRow = new System.Collections.Generic.List<CardDisplay>();
            for (int col = 0; col < board.columns; col++)
            {
                CardTile targetTile = board.GetTile(targetRow, col);
                if (targetTile == null || targetTile.occupiedCard == null) continue;

                CardDisplay targetCard = targetTile.occupiedCard.GetComponent<CardDisplay>();
                if (targetCard == null || targetCard.ownerPlayerNumber == playerNum ||
                    targetCard.ownerPlayerNumber == 0) continue;

                enemiesInRow.Add(targetCard);
            }

            // Fileira sem inimigos → continua procurando mais à frente
            if (enemiesInRow.Count == 0) continue;

            // Primeira fileira à frente com inimigos: bate em todos
            foreach (var targetCard in enemiesInRow)
            {
                EffectProjectileFX.Launch(cardDisplay, targetCard, EffectProjectileFX.Arrow);
                targetCard.TakeDamage(1);
            }

            Debug.Log($"[ArcherEffect2] {cardDisplay.card.cardName}: Causou 1 de dano a {enemiesInRow.Count} inimigo(s) na fileira {targetRow} (primeira à frente com inimigos)");
            return;
        }

        Debug.Log($"[ArcherEffect2] {cardDisplay.card.cardName}: Nenhum inimigo à frente para atingir.");
    }

    // Efeito 3: Faz uma cópia de si se estiver com um tanque aliado em campo
    void ArcherEffect3_CopyIfTankAlly()
    {
        // Cópias NÃO copiam a si mesmas (senão cada cópia gerava outra em cadeia)
        if (CardDisplay.spawningCopy)
        {
            Debug.Log("[ArcherEffect3] Cópia entrou em campo - não gera nova cópia");
            return;
        }

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
                    copy.WeakenAsEcho(); // metade dos stats (min 1) + só anda neste round
                    Debug.Log($"[ArcherEffect3] {cardDisplay.card.cardName}: Criou um eco ({copy.currentAttack}/{copy.currentHealth}) em {emptyTile.row},{emptyTile.column}");
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

        if (baseAtk == 2 && baseHp == 5)
            HealerTier4Effect1_PeriodicCure();
        else if (baseAtk == 2 && baseHp == 4)
            HealerTier4Effect2_GoldOnOpponentTurnEnd();
        else if (baseAtk == 1 && baseHp == 4)
            HealerTier4Effect3_GrantInvulnerability();
        else if (baseAtk == 3 && baseHp == 4)
            HealerTier4Effect4_BoostAllWithCombo();
    }

    // Efeito 1: Healer 4 (ATK 2, HP 5) - Cura 3 no MAIS FERIDO todo round,
    // +1 ouro se tem Mago (era cura 4 em aleatório a cada 2 rounds: sumia da
    // partida e ainda sorteava aliado de vida cheia)
    void HealerTier4Effect1_PeriodicCure()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via contador periódico a cada round
        Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Pronta para curar o aliado mais ferido todo round");
    }

    public void ActivatePeriodicCure()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Cura o aliado mais ferido (sem sorteio — nunca desperdiça)
        CardDisplay targetAlly = MostWoundedAlly();
        if (targetAlly != null)
        {
            targetAlly.Heal(3, cardDisplay);
            Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Curou {targetAlly.card.cardName} (o mais ferido) em 3!");
        }

        // Se tem Mago aliado, ganha 1 de ouro (o proc agora é todo round)
        if (board.HasClassOnBoard(cardDisplay.ownerPlayerNumber, CardClass.Mago))
        {
            PlayerData player = TurnManager.Instance.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                player.AddGold(1);
                MatchStatsTracker.RecordGold(cardDisplay, 1);
                FloatingTextFX.ShowAboveCard(cardDisplay, "+1 ouro", FloatingTextFX.GoldColor);
                Debug.Log($"[HealerTier4Effect1] {cardDisplay.card.cardName}: Tem Mago aliado - ganhou 1 de ouro!");
            }
        }
    }

    // Efeito 2: Healer 4 (ATK 2, HP 4) - Recebe 1 ouro extra ao fim do turno do oponente
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
        if (player == null) return;

        // Ouro no teto (10): o efeito não desperdiça — vira cura 2 no aliado
        // mais ferido (antes o "+1 ouro" simplesmente evaporava)
        if (player.gold >= 10)
        {
            CardDisplay wounded = MostWoundedAlly();
            if (wounded != null)
            {
                EffectProjectileFX.Launch(cardDisplay, wounded, EffectProjectileFX.HealGreen);
                wounded.Heal(2, cardDisplay);
                Debug.Log($"[HealerTier4Effect2] {cardDisplay.card.cardName}: Ouro cheio - curou {wounded.card.cardName} em 2!");
            }
            return;
        }

        player.AddGold(1);
        MatchStatsTracker.RecordGold(cardDisplay, 1);
        FloatingTextFX.ShowAboveCard(cardDisplay, "+1 ouro", FloatingTextFX.GoldColor);
        Debug.Log($"[HealerTier4Effect2] {cardDisplay.card.cardName}: Ganhou 1 ouro ao fim do turno do oponente!");
    }

    // Efeito 3: Healer 4 (ATK 1, HP 4) - Concede invunerabilidade a uma carta por 3 rounds
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

        // Invulnerável por 3 ROUNDS de verdade (contador rosa na carta).
        // Antes usava treeDefenseActive, que é zerado TODO turno → durava ~1 turno
        targetAlly.invulnerableRoundsLeft = 3;
        targetAlly.UpdateDisplay();
        FloatingTextFX.ShowAboveCard(targetAlly, "INVULNERÁVEL!", FloatingTextFX.GoldColor, 4.5f);
        cardDisplay.healerTier4Effect3Used = true;
        Debug.Log($"[HealerTier4Effect3] {cardDisplay.card.cardName}: Concedeu invunerabilidade a {targetAlly.card.cardName} por 3 rounds!");
    }

    // Efeito 4: Healer 4 (ATK 3, HP 4) - +3 todos status a todos aliados se tem Tank, Arqueiro e Mago
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

        if (baseAtk == 1 && baseHp == 4)
            HealerTier3Effect1_GoldIfMage();
        else if (baseAtk == 2 && baseHp == 4)
            HealerTier3Effect2_CureTankOnDamage();
        else if (baseAtk == 1 && baseHp == 3)
            HealerTier3Effect3_GoldPerHealerAndMage();
        else if (baseAtk == 2 && baseHp == 3)
            HealerTier3Effect4_GoldPerMage();
    }

    // Efeito 1: Healer 3 (ATK 1, HP 4) - Ganha 2 de ouro se houver Mago em
    // campo; SEM Mago, cura 2 no aliado mais ferido (o efeito nunca é "nada")
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
                MatchStatsTracker.RecordGold(cardDisplay, 2);
                cardDisplay.healerTier3Effect1Used = true;
                FloatingTextFX.ShowAboveCard(cardDisplay, "+2 ouro", FloatingTextFX.GoldColor);
                Debug.Log($"[HealerTier3Effect1] {cardDisplay.card.cardName}: Tem um mago aliado - ganhou 2 de ouro!");
            }
        }
        else
        {
            CardDisplay wounded = MostWoundedAlly();
            if (wounded != null)
            {
                EffectProjectileFX.Launch(cardDisplay, wounded, EffectProjectileFX.HealGreen);
                wounded.Heal(2, cardDisplay);
                Debug.Log($"[HealerTier3Effect1] {cardDisplay.card.cardName}: Sem Mago - curou {wounded.card.cardName} em 2!");
            }
            else
            {
                Debug.Log($"[HealerTier3Effect1] {cardDisplay.card.cardName}: Sem Mago e ninguém ferido - nada a fazer");
            }
            cardDisplay.healerTier3Effect1Used = true;
        }
    }

    // Efeito 2: Healer 3 (ATK 2, HP 4) - Cura Tank em 3 sempre que ele recebe
    // dano (era 2 — pouco para a régua de dano da v3.9)
    void HealerTier3Effect2_CureTankOnDamage()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook no método TakeDamage() quando um Tank recebe dano
        Debug.Log($"[HealerTier3Effect2] {cardDisplay.card.cardName}: Pronta para curar Tanks que recebem dano");
    }

    public void HealerTier3Effect2_CureTankWhenDamaged(CardDisplay damagedTank)
    {
        if (cardDisplay == null || damagedTank == null) return;

        EffectProjectileFX.Launch(cardDisplay, damagedTank, EffectProjectileFX.HealGreen);
        // Cura +3 sem NUNCA reduzir (mesma regra do Heal): um tank buffado
        // acima do máximo base não perde vida ao ser curado
        int tankMaxHp = damagedTank.card.health + damagedTank.maxHealthBonus;
        damagedTank.currentHealth = Mathf.Max(damagedTank.currentHealth,
            Mathf.Min(damagedTank.currentHealth + 3, tankMaxHp));

        damagedTank.UpdateDisplay();
        Debug.Log($"[HealerTier3Effect2] {cardDisplay.card.cardName}: Curou {damagedTank.card.cardName} em 3 (HP agora: {damagedTank.currentHealth})");
    }

    // Efeito 3: Healer 3 (ATK 1, HP 3) - Ganha 1 de ouro por cada Healer, +1 por cada Mago
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
                MatchStatsTracker.RecordGold(cardDisplay, totalGold);
                cardDisplay.healerTier3Effect3Used = true;
                FloatingTextFX.ShowAboveCard(cardDisplay, $"+{totalGold} ouro", FloatingTextFX.GoldColor);
                Debug.Log($"[HealerTier3Effect3] {cardDisplay.card.cardName}: Ganhou {totalGold} de ouro ({healerCount} Healers + {mageCount} Magos)!");
            }
        }
    }

    // Efeito 4: Healer 3 (ATK 2, HP 3) - Enquanto estiver em campo, a PRIMEIRA
    // compra do turno do dono custa 2 a menos. (Era "+1 ouro por Mago", uma
    // vez só — com o teto de 10 de ouro, virava efeito nulo no meio do jogo.)
    // A mecânica vive em CardDisplay.DiscountedCost — aqui só o anúncio.
    void HealerTier3Effect4_GoldPerMage()
    {
        if (cardDisplay == null) return;

        FloatingTextFX.ShowAboveCard(cardDisplay, "LOJA -2!", FloatingTextFX.GoldColor, 4.2f);
        Debug.Log($"[HealerTier3Effect4] {cardDisplay.card.cardName}: 1ª compra de cada turno custa 2 a menos enquanto ela estiver em campo");
    }

    // ===== HEALER TIER-2 =====
    public void HealerTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        // Membros da tríade (1/4, 0/4, 0/3): SÓ tríade, sem efeito solo.
        // (Healer 2/1, fora da tríade, mantém o "+vida máxima ao curar".)
        if (baseAtk == 1 && baseHp == 3)
            HealerTier2Effect1_MaxHealthOnHeal();

        // Verifica combo das 3 Healers tier-2
        CheckHealerTier2Combo();
    }

    // Efeito 1: Healer 2 (ATK 1, HP 3) - Sempre que um aliado for curado,
    // aumenta a vida máxima DELE (do aliado curado) em +1.
    // BUGFIX: a versão antiga aumentava a vida máxima da PRÓPRIA Healer 2 e
    // ainda a curava por completo a cada cura de qualquer aliado
    public void HealerTier2Effect1_OnAllyHealed(CardDisplay healedAlly)
    {
        if (cardDisplay == null || healedAlly == null) return;

        healedAlly.maxHealthBonus += 1;
        healedAlly.currentHealth += 1; // preenche o novo espaço de vida
        healedAlly.UpdateDisplay();

        Debug.Log($"[HealerTier2Effect1] {healedAlly.card.cardName} ganhou +1 vida máxima! Total: {healedAlly.card.health + healedAlly.maxHealthBonus}");
    }

    void HealerTier2Effect1_MaxHealthOnHeal()
    {
        // Este efeito é ativado via hook no método Heal()
        Debug.Log($"[HealerTier2Effect1] {cardDisplay.card.cardName}: Pronta para aumentar vida máxima ao curar");
    }

    // (Efeito solo do Healer 2 (ATK 1, HP 4) — +2 armadura em Tank — foi
    // REMOVIDO: a carta é tríade pura. O gancho antigo em SelectCardFromBoard
    // dava armadura em QUALQUER clique em Tank, até no tank inimigo.)

    // Efeito 3: Healer 2 (ATK 0, HP 4) - +2 dano a todos os Arqueiros
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

    // Efeito 4: Healer 2 (ATK 0, HP 3) - Apenas participa do combo
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

        // Membros DISTINTOS da tríade: (1/4), (0/4) e (0/3 — só tríade).
        // BUGFIX: duplicatas não contam como membros diferentes, e se algum
        // membro em campo já ativou, a tríade está gasta (antes qualquer
        // Healer tier-2 novo re-disparava o "ouro + vida da torre no máximo")
        bool has14 = false, has04 = false, has03 = false;
        foreach (var ally in allies)
        {
            if (ally == null || ally.card == null) continue;
            if (ally.card.cardClass != CardClass.Healer || ally.card.tier != CardTier.Tier2) continue;

            if (ally.card.attack == 1 && ally.card.health == 4) { if (ally.healerComboActivated) return; has14 = true; }
            else if (ally.card.attack == 0 && ally.card.health == 4) { if (ally.healerComboActivated) return; has04 = true; }
            else if (ally.card.attack == 0 && ally.card.health == 3) { if (ally.healerComboActivated) return; has03 = true; }
        }

        // Se as 3 Healers tier-2 estão em campo, ativa combo
        if (has14 && has04 && has03)
        {
            PlayerData player = TurnManager.Instance?.GetPlayer(cardDisplay.ownerPlayerNumber);
            if (player != null)
            {
                // Restaura ouro máximo (10)
                player.gold = 10;
                // Restaura vida máxima do jogador
                player.health = PlayerData.MaxTowerHealth;

                // Marca que o combo foi ativado para todas as 3
                foreach (var ally in allies)
                {
                    if (ally != null && ally.card.cardClass == CardClass.Healer && ally.card.tier == CardTier.Tier2)
                    {
                        if ((ally.card.attack == 1 && ally.card.health == 4) ||
                            (ally.card.attack == 0 && ally.card.health == 4) ||
                            (ally.card.attack == 0 && ally.card.health == 3))
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

        // (Efeito 1 do 0/3 é SÓ periódico — a cura sai pelo contador de 2 TURNOS
        // via OnPeriodicCounterExpired; antes também curava na entrada em campo,
        // um proc extra que não está na descrição)
        if (baseAtk == 1 && baseHp == 3)
            HealerEffect2_HealAllAlliesOnEnter();
        // Efeitos 3 e 4 são ativados em outras situações (não ao entrar em campo)
    }

    // Aliado MAIS FERIDO do dono (maior vida faltando até o máximo, contando
    // bônus de vida máxima). Null se ninguém está ferido. Determinístico: a
    // lista tem a mesma ordem nos dois clientes, sem sorteio nenhum.
    CardDisplay MostWoundedAlly()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay == null) return null;

        CardDisplay best = null;
        int bestMissing = 0;
        foreach (var ally in board.GetCardsByOwner(cardDisplay.ownerPlayerNumber))
        {
            if (ally == null || ally.card == null || ally.currentHealth <= 0) continue;
            int maxHp = ally.card.health + ally.maxHealthBonus;
            int missing = maxHp - ally.currentHealth;
            if (missing > bestMissing)
            {
                bestMissing = missing;
                best = ally;
            }
        }
        return best;
    }

    // Efeito 1: Cura 3 HP no aliado MAIS FERIDO a cada 2 TURNOS (era a cada 2
    // rounds — demorava demais; pedido do Carlos no rebalance v4.1)
    void HealerEffect1_RandomAllyPeriodicHeal()
    {
        if (cardDisplay == null) return;

        CardDisplay targetAlly = MostWoundedAlly();
        if (targetAlly == null)
        {
            Debug.Log($"[HealerEffect1] {cardDisplay.card.cardName}: Ninguém ferido — cura guardada");
            return;
        }

        targetAlly.Heal(3, cardDisplay);
        Debug.Log($"[HealerEffect1] {cardDisplay.card.cardName}: Curou {targetAlly.card.cardName} (o mais ferido) por 3 HP");
    }

    // Efeito 2: Ao entrar em campo, cura 2 HP em todos os aliados — e desde o
    // rebalance v4.1 REATIVA a cada 2 turnos (contador amarelo, ver
    // SetupPeriodicCounter/OnPeriodicCounterExpired)
    void HealerEffect2_HealAllAlliesOnEnter()
    {
        if (cardDisplay == null) return;

        // A cura de ENTRADA só sai 1 vez (as seguintes vêm pelo contador)
        if (cardDisplay.healOnEnterUsed)
        {
            Debug.Log($"[HealerEffect2] {cardDisplay.card.cardName}: Cura de entrada já foi usada!");
            return;
        }

        cardDisplay.healOnEnterUsed = true;
        ActivateHealAllAlliesPeriodic();
    }

    // Núcleo da cura em área do Healer 1 (1/3): usado na entrada e no tique
    // periódico de 2 turnos
    public void ActivateHealAllAlliesPeriodic()
    {
        if (cardDisplay == null) return;

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

        Debug.Log($"[HealerEffect2] {cardDisplay.card.cardName}: Curou {healed} aliado(s) por 2 HP");
    }

    // Efeito 3: Anula um ataque a cada 3 turnos (popup para aliado atacado)
    // Este efeito é ativado quando um aliado sofre dano - ver CardDisplay.cs
    // Cooldown em TURNOS de verdade (antes comparava rounds = 2x mais lento)
    public bool CanBlockAttackThisTurn()
    {
        if (cardDisplay == null) return false;
        return cardDisplay.effectCounter <= 0;
    }

    public void ActivateBlockAttack()
    {
        if (cardDisplay == null) return;

        cardDisplay.StartEffectCounter(3, false, false); // cooldown: 3 TURNOS (amarelo)
        Debug.Log($"[HealerEffect3] {cardDisplay.card.cardName}: Bloqueou um ataque! Recarrega em 3 turnos");
    }

    // Efeito 4: Sempre que um aliado for curado, receba 1 de ouro
    public void OnAllyHealed()
    {
        if (cardDisplay == null || cardDisplay.ownerPlayerNumber == 0) return;

        PlayerData player = TurnManager.Instance?.GetPlayer(cardDisplay.ownerPlayerNumber);
        if (player != null)
        {
            player.AddGold(1);
            MatchStatsTracker.RecordGold(cardDisplay, 1);
            FloatingTextFX.ShowAboveCard(cardDisplay, "+1 ouro", FloatingTextFX.GoldColor);
            Debug.Log($"[HealerEffect4] {cardDisplay.card.cardName}: Ganhou 1 ouro! Total: {player.gold}");
        }
    }

    // ===== MAGE TIER-4 =====
    public void MageTier4Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 4 && baseHp == 5)
            MageTier4Effect1_RemoveBonus();
        else if (baseAtk == 4 && baseHp == 6)
            MageTier4Effect2_BoostOnHealerEnter();
        else if (baseAtk == 3 && baseHp == 5)
            MageTier4Effect3_DestroyLowerTier();
        else if (baseAtk == 4 && baseHp == 4)
            MageTier4Effect4_LightningEvery2Turns();
    }

    // Efeito 1: Mage 4 (ATK 4, HP 5) - Remove bônus de um inimigo, pode usar 2 vezes se tem Healer + Arqueiro
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

        // Remove só o que é BÔNUS: Min com o valor atual, nunca sobe stat.
        // BUGFIX: o reset "seco" para os valores base CURAVA um inimigo ferido
        // de volta à vida/escudo cheios
        targetEnemy.currentAttack = Mathf.Min(targetEnemy.currentAttack, targetEnemy.card.attack);
        targetEnemy.currentShield = Mathf.Min(targetEnemy.currentShield, targetEnemy.card.shield);
        targetEnemy.maxHealthBonus = 0;
        targetEnemy.currentHealth = Mathf.Min(targetEnemy.currentHealth, targetEnemy.card.health);
        targetEnemy.UpdateDisplay();

        cardDisplay.mageTier4Effect1UsesLeft--;

        if (cardDisplay.mageTier4Effect1UsesLeft <= 0)
        {
            cardDisplay.mageTier4Effect1Used = true;
        }

        Debug.Log($"[MageTier4Effect1] {cardDisplay.card.cardName}: Removeu bônus de {targetEnemy.card.cardName}! Usos restantes: {cardDisplay.mageTier4Effect1UsesLeft}");

        // Segunda carga (Healer + Arqueiro em campo): reabre a seleção de alvo.
        // BUGFIX: o contador de usos existia mas nada reabria a escolha — o
        // segundo feitiço era perdido em silêncio. Em multiplayer isto roda nos
        // dois clientes; StartEffectTargetSelection já ignora o não-dono
        if (cardDisplay.mageTier4Effect1UsesLeft > 0)
        {
            Debug.Log($"[MageTier4Effect1] {cardDisplay.card.cardName}: Pode jogar o feitiço mais uma vez!");
            ShowRemoveBonusPopup();
        }
    }

    // Efeito 2: Mage 4 (ATK 4, HP 6) - Ganha +1 ATK quando Healer entra em campo
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

    // Efeito 3: Mage 4 (ATK 3, HP 5) - Destruir inimigo de nível inferior, absorve 50% ataque se tem Tank + Healer + Arqueiro
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

        // Guarda o ataque da carta a ser destruída antes de destruir
        int destroyedAtk = targetEnemy.currentAttack;

        targetEnemy.DestroyCard();
        cardDisplay.mageTier4Effect3Used = true;

        // Se tem Tank, Healer E Arqueiro, absorve 50% do ATAQUE da carta
        // destruída (o texto promete só ataque — antes absorvia escudo e vida também)
        if (hasTankAlly && hasHealerAlly && hasArcherAlly)
        {
            int gainAtk = destroyedAtk / 2;

            cardDisplay.currentAttack += gainAtk;
            cardDisplay.UpdateDisplay();

            Debug.Log($"[MageTier4Effect3] {cardDisplay.card.cardName}: Destruiu {targetEnemy.card.cardName} e absorveu ATK +{gainAtk}!");
        }
        else
        {
            Debug.Log($"[MageTier4Effect3] {cardDisplay.card.cardName}: Destruiu {targetEnemy.card.cardName}! (Faltam aliados para ganhar status)");
        }
    }

    // Efeito 4: Mage 4 (ATK 4, HP 4) - RAIO a cada 2 turnos: 2 de dano num
    // inimigo aleatório e 1 nos adjacentes. Era "+1 ouro por round" —
    // economia é trabalho de healer, não de mago (v4.1)
    void MageTier4Effect4_LightningEvery2Turns()
    {
        if (cardDisplay == null) return;

        // O raio sai pelo contador amarelo (SetupPeriodicCounter/
        // OnPeriodicCounterExpired), a cada 2 turnos
        Debug.Log($"[MageTier4Effect4] {cardDisplay.card.cardName}: Pronta para lançar raios a cada 2 turnos");
    }

    public void ActivateLightningPeriodic()
    {
        CastAreaBlast(2, 1, "MageTier4Effect4");
    }

    // ===== MAGE TIER-3 =====
    public void MageTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 1 && baseHp == 3)
            MageTier3Effect1_StealStats();
        else if (baseAtk == 3 && baseHp == 5)
            MageTier3Effect2_SplashAttacks();
        else if (baseAtk == 3 && baseHp == 4)
            MageTier3Effect3_FreezeOrDamage();
        else if (baseAtk == 2 && baseHp == 5)
            MageTier3Effect4_Explosion();
    }

    // Efeito 1: Mage 3 (ATK 1, HP 3) - Rouba todos os status de um inimigo aleatório (inimigo fica com 0)
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

    // Efeito 2: Mage 3 (ATK 3, HP 5) - Concede +1 de ataque a todos no campo se houver Arqueiro
    // Efeito 2: Mage 3 (ATK 3, HP 5) - RESPINGO: os ataques dela causam 1 de
    // dano também aos inimigos adjacentes ao alvo (gancho em
    // CardDisplay.PerformAttackOn → SplashDamageAroundTile). Era "+1 ATK a
    // todos com arqueiro" — buff genérico sem identidade (v4.1)
    void MageTier3Effect2_SplashAttacks()
    {
        if (cardDisplay == null) return;

        Debug.Log($"[MageTier3Effect2] {cardDisplay.card.cardName}: Ataques com respingo (1 de dano nos adjacentes ao alvo)");
    }

    // Efeito 3: Mage 3 (ATK 3, HP 4) - Escolhe congelar OU dano (popup) ou ambos se tiver Healer e Tank
    void MageTier3Effect3_FreezeOrDamage()
    {
        // Dispara SÓ pelo contador amarelo (1x por turno). O proc extra na
        // entrada fazia o efeito rodar DUAS vezes no turno em que era jogada
        // (entrada + contador de 1 expirando no fim do mesmo turno)
        Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Pronta - congelar/dano 1x por turno (contador)");
    }

    // Dispara a escolha congelar/dano (chamado na entrada e a cada turno pelo contador)
    public void ActivateFreezeOrDamagePerTurn()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        // Sem inimigos em campo, não há o que fazer — evita popup à toa
        int enemyPlayerNumber = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        if (board.GetCardsByOwner(enemyPlayerNumber).Count == 0)
        {
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Nenhum inimigo em campo");
            return;
        }

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
    }

    void ShowFreezeOrDamageChoicePopup()
    {
        // Captura AGORA se estamos no tique de fim de turno: o callback da
        // decisão resolve DEPOIS da troca de jogador, e o cálculo de duração
        // erraria (a vítima ficava congelada por 2 turnos em vez de 1)
        bool fromCounterTick = TurnManager.TickingCounterEffects;

        // Decisão sincronizada: o dono do Mago escolhe (Random dos callbacks roda re-seedado)
        PhotonGameManager.AskEffectDecision(cardDisplay.ownerPlayerNumber,
            $"{cardDisplay.card.cardName} vai congelar ou causar dano?",
            "Congelar", "Causar Dano",
            accepted =>
            {
                if (accepted) ActivateFreezeOnly(fromCounterTick);
                else ActivateDamageOnly();
            });
    }

    void ShowFreezeAndDamagePopup()
    {
        bool fromCounterTick = TurnManager.TickingCounterEffects;

        // Decisão sincronizada: o dono do Mago escolhe
        PhotonGameManager.AskEffectDecision(cardDisplay.ownerPlayerNumber,
            $"{cardDisplay.card.cardName} vai congelar E causar dano!",
            "Ativar", "Cancelar",
            accepted =>
            {
                if (accepted) ActivateFreezeAndDamageChoice(fromCounterTick);
            });
    }

    public void ActivateFreezeOnly(bool forceOneTurn = false)
    {
        if (cardDisplay == null) return;

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
            EffectProjectileFX.Launch(cardDisplay, targetEnemy, EffectProjectileFX.Ice);
            targetEnemy.Freeze(forceOneTurn, cardDisplay);
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Congelou {targetEnemy.card.cardName}!");
        }
    }

    public void ActivateDamageOnly()
    {
        if (cardDisplay == null) return;

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
            EffectProjectileFX.Launch(cardDisplay, targetEnemy, EffectProjectileFX.Fire);
            targetEnemy.TakeDamage(1);
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Causou 1 de dano a {targetEnemy.card.cardName}!");
        }
    }

    public void ActivateFreezeAndDamageChoice(bool forceOneTurn = false)
    {
        if (cardDisplay == null) return;

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
            EffectProjectileFX.Launch(cardDisplay, targetEnemy, EffectProjectileFX.Ice);
            targetEnemy.Freeze(forceOneTurn, cardDisplay);
            MatchStatsTracker.EffectSource = cardDisplay;
            targetEnemy.TakeDamage(1);
            MatchStatsTracker.EffectSource = null;
            Debug.Log($"[MageTier3Effect3] {cardDisplay.card.cardName}: Congelou E causou 1 de dano a {targetEnemy.card.cardName}!");
        }
    }

    // Efeito 4: Mage 3 (ATK 2, HP 5) - Concede +1 de ataque a todas as cartas aliadas em campo
    // Efeito 4: Mage 3 (ATK 2, HP 5) - EXPLOSÃO ao entrar: 2 de dano num
    // inimigo aleatório e 1 nos adjacentes a ele. Era "+1 ATK a todas as
    // cartas no campo" — buff genérico sem identidade (v4.1)
    void MageTier3Effect4_Explosion()
    {
        if (cardDisplay == null || cardDisplay.mageTier3Effect4Used) return;

        cardDisplay.mageTier3Effect4Used = true;
        CastAreaBlast(2, 1, "MageTier3Effect4");
    }

    // ===== MAGE TIER-2 =====
    public void MageTier2Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseHp = cardDisplay.card.health;

        // Membros da tríade (2/4, 2/3, 3/3): SÓ tríade, sem efeito solo.
        // (Mago 3/4, fora da tríade, mantém o efeito de quebrar armadura.)
        if (baseAtk == 3 && baseHp == 4)
            MageTier2Effect3_ShieldBreak();

        // Verifica combo dos 3 Magos tier-2
        CheckMageTier2Combo();
    }

    // Efeito 1: Mage 2 (ATK 2, HP 4) - +1 ATK quando Healer for atacado
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

    // (Efeito 2 do Mage 2 ATK 2/HP 3 — bola de fogo ao Tank ser atacado — foi
    // REMOVIDO: a carta agora tem apenas o efeito de tríade)

    // Efeito 3: Mage 2 (ATK 3, HP 4) - 1 de dano na armadura de 2 inimigos (ao entrar em campo)
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

        EffectProjectileFX.Launch(cardDisplay, targetEnemy, EffectProjectileFX.ShieldBlue);

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

    // Efeito 4: Mage 2 (ATK 3, HP 3) - Congela inimigo que ataca Arqueiro
    void MageTier2Effect4_FreezeOnArcherHit()
    {
        // Este efeito é ativado via hook quando um Arqueiro é atacado
        Debug.Log($"[MageTier2Effect4] {cardDisplay.card.cardName}: Pronta para congelar atacante de Arqueiro");
    }

    public void MageTier2Effect4_FreezeAttacker(CardDisplay attacker)
    {
        if (cardDisplay == null || attacker == null) return;

        // A descrição diz CONGELE (o código antigo stunava por engano)
        EffectProjectileFX.Launch(cardDisplay, attacker, EffectProjectileFX.Ice);
        attacker.Freeze(false, cardDisplay);

        Debug.Log($"[MageTier2Effect4] {cardDisplay.card.cardName}: Congelou {attacker.card.cardName}!");
    }

    // Combo: Quando os 3 Magos tier-2 estão em campo, invoca Mago lendário
    void CheckMageTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return; // Reusa flag de combo

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);

        // Membros DISTINTOS da tríade: (2/4), (2/3) e (3/3).
        // BUGFIX: duplicatas não contam, e se algum membro em campo já ativou,
        // a tríade está gasta (antes jogar o Mago 4/3 — ou qualquer cópia —
        // com a tríade fechada invocava OUTRO mago lendário de graça)
        bool has24m = false, has23m = false, has33m = false;
        foreach (var ally in allies)
        {
            if (ally == null || ally.card == null) continue;
            if (ally.card.cardClass != CardClass.Mago || ally.card.tier != CardTier.Tier2) continue;

            if (ally.card.attack == 2 && ally.card.health == 4) { if (ally.archerComboActivated) return; has24m = true; }
            else if (ally.card.attack == 2 && ally.card.health == 3) { if (ally.archerComboActivated) return; has23m = true; }
            else if (ally.card.attack == 3 && ally.card.health == 3) { if (ally.archerComboActivated) return; has33m = true; }
        }

        // Se os 3 Magos tier-2 estão em campo, ativa combo
        if (has24m && has23m && has33m)
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
                        if ((ally.card.attack == 2 && ally.card.health == 4) ||
                            (ally.card.attack == 2 && ally.card.health == 3) ||
                            (ally.card.attack == 3 && ally.card.health == 3))
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

        if (baseAtk == 2 && baseHp == 3)
            MageEffect1_BuffHealerOnDamage();
        else if (baseAtk == 2 && baseHp == 4)
            MageEffect2_RandomEnemyDamage();
        else if (baseAtk == 1 && baseHp == 3)
            MageEffect3_FreezeEnemy();
        else if (baseAtk == 1 && baseHp == 4)
            MageEffect5_DamageColumnAhead();
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
        EffectProjectileFX.Launch(cardDisplay, targetEnemy, EffectProjectileFX.Fire);
        MatchStatsTracker.EffectSource = cardDisplay;
        targetEnemy.TakeDamage(1);
        MatchStatsTracker.EffectSource = null;

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

    // Efeito 5: Ao entrar, causa 1 de dano a TODOS os inimigos na COLUNA à
    // frente (identidade de área dos magos, v4.1 — antes só atingia magos
    // inimigos, nicho demais). Alvos coletados antes do dano (ver respingo).
    void MageEffect5_DamageColumnAhead()
    {
        if (cardDisplay == null || cardDisplay.currentTile == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int col = cardDisplay.currentTile.column;
        int row = cardDisplay.currentTile.row;
        bool marchaSubindo = cardDisplay.ownerPlayerNumber == 1; // P1 sobe, P2 desce

        var targets = new System.Collections.Generic.List<CardDisplay>();
        foreach (var c in board.GetAllCards())
        {
            if (c == null || c.card == null || c.currentTile == null) continue;
            if (c.ownerPlayerNumber == cardDisplay.ownerPlayerNumber || c.ownerPlayerNumber == 0) continue;
            if (c.currentTile.column != col) continue;
            if (marchaSubindo ? c.currentTile.row <= row : c.currentTile.row >= row) continue;
            targets.Add(c);
        }

        MatchStatsTracker.EffectSource = cardDisplay;
        foreach (var t in targets)
        {
            EffectProjectileFX.Launch(cardDisplay, t, EffectProjectileFX.Fire);
            t.TakeDamage(1);
        }
        MatchStatsTracker.EffectSource = null;

        Debug.Log($"[MageEffect5] {cardDisplay.card.cardName}: Causou 1 de dano a {targets.Count} inimigo(s) na coluna à frente");
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

        if (baseAtk == 5 && baseHp == 5)
            MageTier5Effect1_RandomFreezePerRound();
        else if (baseAtk == 4 && baseHp == 6)
            MageTier5Effect2_CopyEnemyStats();
        else if (baseAtk == 5 && baseHp == 6)
            MageTier5Effect3_FireballAndMageBoost();
    }

    // Efeito 1: Mage 5 (ATK 5, HP 5) - Congela um inimigo aleatório por round, duplicado se tem Tank
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
            EffectProjectileFX.Launch(cardDisplay, target1, EffectProjectileFX.Ice);
            target1.Freeze(false, cardDisplay);
            Debug.Log($"[MageTier5Effect1] {cardDisplay.card.cardName}: Congelou {target1.card.cardName}!");
        }

        // Se tem Tank aliado, congela OUTRO inimigo aleatório. Sorteia da
        // lista SEM o primeiro alvo — antes sorteava da lista inteira e, se
        // repetia o alvo, o segundo congelamento era perdido em silêncio
        if (hasTankAlly && enemies.Count > 1)
        {
            var others = enemies.FindAll(e => e != null && e != target1);
            CardDisplay target2 = others.Count > 0 ? others[Random.Range(0, others.Count)] : null;
            if (target2 != null)
            {
                EffectProjectileFX.Launch(cardDisplay, target2, EffectProjectileFX.Ice);
                target2.Freeze(false, cardDisplay);
                Debug.Log($"[MageTier5Effect1] {cardDisplay.card.cardName}: Tem Tank aliado - congelou {target2.card.cardName}!");
            }
        }
    }

    // Efeito 2: Mage 5 (ATK 4, HP 6) - Copia stats de ataque e vida de um inimigo
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

    // Efeito 3: Mage 5 (ATK 5, HP 6) - Causa 5 de dano ao entrar, aumenta ATK de todos Magos ao resetar turno
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
                // Bola de fogo grande (dano 5) + respingo de 2 nos adjacentes
                // (identidade de área dos magos, v4.1). O tile é capturado
                // ANTES do dano — se o alvo morrer, o tile dele é liberado
                CardTile centerTile = target.currentTile;
                EffectProjectileFX.Launch(cardDisplay, target, EffectProjectileFX.Fire, 0.9f);
                MatchStatsTracker.EffectSource = cardDisplay;
                target.TakeDamage(5);
                MatchStatsTracker.EffectSource = null;
                Debug.Log($"[MageTier5Effect3] {cardDisplay.card.cardName}: Lançou bola de fogo em {target.card.cardName}!");
                SplashDamageAroundTile(centerTile, 2, "MageTier5Effect3");
            }
        }
    }

    // ===== NÚCLEO DE DANO EM ÁREA DOS MAGOS (identidade v4.1) =====
    // Explosão num inimigo ALEATÓRIO (sorteio sincronizado: roda dentro de
    // RPC nos dois clientes com o Random já semeado — mesmo padrão do stun
    // do Archer 4): mainDamage no alvo e splashDamage nos adjacentes a ele.
    public void CastAreaBlast(int mainDamage, int splashDamage, string logTag)
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayer = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var enemies = board.GetCardsByOwner(enemyPlayer);
        if (enemies.Count == 0)
        {
            Debug.Log($"[{logTag}] {cardDisplay.card.cardName}: Nenhum inimigo em campo");
            return;
        }

        CardDisplay center = enemies[Random.Range(0, enemies.Count)];
        if (center == null || center.currentTile == null) return;

        // Tile capturado ANTES do dano (se o alvo morrer, o tile é liberado)
        CardTile centerTile = center.currentTile;
        EffectProjectileFX.Launch(cardDisplay, center, EffectProjectileFX.Fire, 0.7f);
        Debug.Log($"[{logTag}] {cardDisplay.card.cardName}: {mainDamage} de dano em {center.card.cardName} + respingo de {splashDamage}");
        MatchStatsTracker.EffectSource = cardDisplay;
        center.TakeDamage(mainDamage);
        MatchStatsTracker.EffectSource = null;

        SplashDamageAroundTile(centerTile, splashDamage, logTag);
    }

    // Respingo: dano nos inimigos ADJACENTES ao tile central. Quem está NO
    // tile central (o alvo principal, se sobreviveu) não é atingido de novo.
    // Os alvos são coletados ANTES de aplicar o dano — TakeDamage pode matar
    // e mexer nas listas do tabuleiro no meio da iteração.
    public void SplashDamageAroundTile(CardTile center, int damage, string logTag)
    {
        if (cardDisplay == null || center == null || damage <= 0) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemyPlayer = cardDisplay.ownerPlayerNumber == 1 ? 2 : 1;
        var targets = new System.Collections.Generic.List<CardDisplay>();
        foreach (var c in board.GetCardsByOwner(enemyPlayer))
        {
            if (c == null || c.card == null || c.currentTile == null) continue;
            int dr = Mathf.Abs(c.currentTile.row - center.row);
            int dc = Mathf.Abs(c.currentTile.column - center.column);
            if (dr == 0 && dc == 0) continue; // tile central fica de fora
            if (dr <= 1 && dc <= 1) targets.Add(c);
        }

        MatchStatsTracker.EffectSource = cardDisplay;
        foreach (var t in targets)
        {
            EffectProjectileFX.Launch(cardDisplay, t, EffectProjectileFX.Fire, 0.5f);
            t.TakeDamage(damage);
        }
        MatchStatsTracker.EffectSource = null;

        if (targets.Count > 0)
            Debug.Log($"[{logTag}] {cardDisplay.card.cardName}: Respingo de {damage} em {targets.Count} inimigo(s)");
    }

    // ===== TANK TIER-5 =====
    public void TankTier5Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 3 && baseShield == 8 && baseHp == 9)
            TankTier5Effect1_ShieldOnKill();
        else if (baseAtk == 3 && baseShield == 7 && baseHp == 10)
            TankTier5Effect2_AttackOnDamageAndPeriodicShield();
        else if (baseAtk == 3 && baseShield == 7 && baseHp == 11)
            TankTier5Effect3_AttackOnDamageWithBonus();
    }

    // Efeito 1: Tank 5 (ATK 3, Shield 7, HP 8) - Concede armadura a aliados ao matar inimigo
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

    // Efeito 2: Tank 5 (ATK 3, Shield 6, HP 9) - +1 ATK ao receber dano, concede armadura a cada 2 turnos
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

    // Efeito 3: Tank 5 (ATK 3, Shield 6, HP 10) - +1 ATK ao receber dano, +armadura se tem Healer ou Mago
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

        if (baseAtk == 2 && baseShield == 6 && baseHp == 7)
            TankTier4Effect1_BoostWithArcherMage();
        else if (baseAtk == 2 && baseShield == 7 && baseHp == 7)
            TankTier4Effect2_InterceptOncePerTurn();
        else if (baseAtk == 2 && baseShield == 6 && baseHp == 8)
            TankTier4Effect3_ArcherDoubleAttack();
        else if (baseAtk == 3 && baseShield == 7 && baseHp == 8)
            TankTier4Effect4_DamageReductionAndShield();
    }

    // Efeito 1: Tank 4 (ATK 2, Shield 5, HP 6) - +5 HP +2 Shield se tem Arqueiro e Mago
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

    // Efeito 2: Tank 4 (ATK 2, Shield 6, HP 6) - Recebe ataque 1x por turno, 50% menos se tem Healer
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

    // Efeito 3: Tank 4 (ATK 2, Shield 5, HP 7) - Arqueiros atacam 2 vezes se tem 4 classes.
    // AGORA É UMA AURA CONTÍNUA: CardDisplay.CanAttackThisRound consulta
    // HasArcherDoubleAttackAura() a cada ataque. A versão antiga era um one-shot
    // na entrada do tank que só resetava quem JÁ tinha atacado naquele momento —
    // na prática os arqueiros nunca ganhavam o 2º ataque
    void TankTier4Effect3_ArcherDoubleAttack()
    {
        if (cardDisplay == null) return;
        Debug.Log($"[TankTier4Effect3] {cardDisplay.card.cardName}: Aura ativa - com as 4 classes em campo, Arqueiros atacam 2x por turno");
    }

    // Efeito 4: Tank 4 (ATK 3, Shield 6, HP 7) - 50% menos dano se tem Healer+Mago+Arqueiro, aliados +1 armadura por turno
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

        // Dá +1 armadura aos aliados ADJACENTES (até 1 casa): a proteção emana
        // de perto — quem quiser armadura marcha em formação com o Tank
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int granted = 0;
        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        foreach (var ally in allies)
        {
            // "As DEMAIS cartas recebem 1 de armadura" — o próprio tank fica de fora
            if (ally != null && ally != cardDisplay && CardDisplay.IsNextTo(ally, cardDisplay))
            {
                ally.currentShield += 1;
                ally.UpdateDisplay();
                granted++;
            }
        }

        if (granted > 0)
            Debug.Log($"[TankTier4Effect4] {cardDisplay.card.cardName}: {granted} aliado(s) adjacente(s) ganharam +1 armadura este turno!");
    }

    // ===== TANK TIER-3 =====
    public void TankTier3Effect()
    {
        if (cardDisplay == null) return;

        int baseAtk = cardDisplay.card.attack;
        int baseShield = cardDisplay.card.shield;
        int baseHp = cardDisplay.card.health;

        if (baseAtk == 2 && baseShield == 3 && baseHp == 7)
            TankTier3Effect1_BoostHealersEvery2Turns();
        else if (baseAtk == 2 && baseShield == 3 && baseHp == 6)
            TankTier3Effect2_ReduceDamageAllTanks();
        else if (baseAtk == 2 && baseShield == 4 && baseHp == 6)
            TankTier3Effect3_BoostShieldPerTank();
        else if (baseAtk == 2 && baseShield == 4 && baseHp == 7)
            TankTier3Effect4_BoostMagoShield();
    }

    // Efeito 1: Tank 3 (ATK 2, Shield 3, HP 6) - Concede +2 armadura a todos Healers a cada 2 turnos.
    // Era one-shot (disparava UMA vez, bug); agora é periódico de verdade, dirigido
    // pelo contador amarelo da carta (SetupPeriodicCounter/OnPeriodicCounterExpired)
    void TankTier3Effect1_BoostHealersEvery2Turns()
    {
        if (cardDisplay == null) return;
        Debug.Log($"[TankTier3Effect1] {cardDisplay.card.cardName}: Pronta para dar +2 armadura aos Healers a cada 2 turnos");
    }

    public void ActivateBoostHealersPeriodic()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        int healersBuffed = 0;

        foreach (var ally in allies)
        {
            if (ally != null && ally.card != null && ally.card.cardClass == CardClass.Healer)
            {
                ally.currentShield += 2;
                ally.UpdateDisplay();
                healersBuffed++;
            }
        }

        if (healersBuffed > 0)
            Debug.Log($"[TankTier3Effect1] {cardDisplay.card.cardName}: Concedeu +2 armadura a {healersBuffed} Healer(s)!");
    }

    // Efeito 2: Tank 3 (ATK 2, Shield 3, HP 5) - Todos Tanks recebem 50% menos dano
    void TankTier3Effect2_ReduceDamageAllTanks()
    {
        if (cardDisplay == null) return;

        // Este efeito é ativado via hook no método TakeDamage() quando um Tank recebe dano
        Debug.Log($"[TankTier3Effect2] {cardDisplay.card.cardName}: Pronta para reduzir dano de Tanks em 50%");
    }

    public int ReduceTankDamage(int originalDamage)
    {
        // Reduz dano em 50%, arredondando para CIMA: a redução nunca pode
        // zerar o dano (1 de dano virava 0 e parecia "tank não leva dano")
        return (originalDamage + 1) / 2;
    }

    // Efeito 3: Tank 3 (ATK 2, Shield 4, HP 5) - Recebe +2 armadura por cada outro Tank em campo
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

    // Efeito 4: Tank 3 (ATK 2, Shield 4, HP 6) - Concede +3 armadura a um Mago à escolha
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

        // Membros da tríade (1/2/5, 1/3/4, 0/3/5): SÓ tríade — não interceptam
        // mais ataques. (O Tank 1/3/5, fora da tríade, ainda pode assumir o dano.)
        if (baseAtk == 1 && baseShield == 3 && baseHp == 5)
            TankTier2Effect4_DefendAny();

        // Verifica combo dos 3 Tanks tier-2 (exceto o 4º)
        CheckTankTier2Combo();
    }

    // Efeito 1: Tank 2 (ATK 1, Shield 2, HP 4) - Recebe ataque de Healer
    void TankTier2Effect1_DefendHealer()
    {
        // Este efeito é ativado via hook quando um Healer é atacado
        Debug.Log($"[TankTier2Effect1] {cardDisplay.card.cardName}: Pronta para receber ataque de Healer");
    }

    public void TankTier2Effect1_TakeHealerAttack(CardDisplay victim, int damage, CardDisplay attacker = null)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Healer, com as PRÓPRIAS defesas
        // (invulnerabilidade/reduções) e sem redirecionamentos em cadeia
        cardDisplay.TakeRedirectedDamage(damage, attacker);

        Debug.Log($"[TankTier2Effect1] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 2: Tank tier 2 (ATK 1, Shield 3, HP 3) - Recebe ataque de Arqueiro
    void TankTier2Effect2_DefendArcher()
    {
        // Este efeito é ativado via hook quando um Arqueiro é atacado
        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Pronta para receber ataque de Arqueiro");
    }

    public void TankTier2Effect2_TakeArcherAttack(CardDisplay victim, int damage, CardDisplay attacker = null)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Arqueiro (com as próprias defesas)
        cardDisplay.TakeRedirectedDamage(damage, attacker);

        Debug.Log($"[TankTier2Effect2] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 3: Tank tier 2 (ATK 0, Shield 3, HP 4) - Recebe ataque de Mago
    void TankTier2Effect3_DefendMago()
    {
        // Este efeito é ativado via hook quando um Mago é atacado
        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Pronta para receber ataque de Mago");
    }

    public void TankTier2Effect3_TakeMagoAttack(CardDisplay victim, int damage, CardDisplay attacker = null)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano ao invés do Mago (com as próprias defesas)
        cardDisplay.TakeRedirectedDamage(damage, attacker);

        Debug.Log($"[TankTier2Effect3] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Efeito 4: Tank tier 2 (ATK 1, Shield 3, HP 4) - Pode receber o ataque de
    // um aliado ADJACENTE (a adjacência é checada no CardDisplay antes do popup)
    void TankTier2Effect4_DefendAny()
    {
        // Este efeito é ativado via hook quando qualquer aliado é atacado
        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Pronta para receber qualquer ataque");
    }

    public void TankTier2Effect4_TakeAnyAttack(CardDisplay victim, int damage, CardDisplay attacker = null, bool alreadyHalved = false)
    {
        if (cardDisplay == null || victim == null) return;

        // O Tank recebe o dano no lugar do aliado (com as próprias defesas;
        // alreadyHalved: se o dano já levou 50% na vítima, aqui não soma outro)
        cardDisplay.TakeRedirectedDamage(damage, attacker, alreadyHalved);

        // Recompensa da escolta: sobreviveu ao golpe assumido → +1 armadura
        // ("apanhar é o combo dele")
        if (cardDisplay.currentHealth > 0)
        {
            cardDisplay.currentShield += 1;
            cardDisplay.UpdateDisplay();
            FloatingTextFX.ShowAboveCard(cardDisplay, "+1 armadura", FloatingTextFX.EffectColor);
        }

        Debug.Log($"[TankTier2Effect4] {cardDisplay.card.cardName}: Assumiu {damage} de dano no lugar de {victim.card.cardName}");
    }

    // Combo: Quando os 3 Tanks tier-2 defensores estão em campo, +10 armadura a todos
    void CheckTankTier2Combo()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.archerComboActivated) return; // Reusa flag de combo

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);

        // Membros DISTINTOS da tríade: (1/2/5), (1/3/4) e (0/3/5) — exclui o
        // 4º (1/3/5) que defende qualquer um. BUGFIX: duplicatas não contam, e
        // se algum membro em campo já ativou, a tríade está gasta (antes cada
        // cópia nova re-disparava o +10 de armadura para todos)
        bool has124 = false, has133 = false, has034 = false;
        foreach (var ally in allies)
        {
            if (ally == null || ally.card == null) continue;
            if (ally.card.cardClass != CardClass.Tank || ally.card.tier != CardTier.Tier2) continue;

            if (ally.card.attack == 1 && ally.card.shield == 2 && ally.card.health == 5) { if (ally.archerComboActivated) return; has124 = true; }
            else if (ally.card.attack == 1 && ally.card.shield == 3 && ally.card.health == 4) { if (ally.archerComboActivated) return; has133 = true; }
            else if (ally.card.attack == 0 && ally.card.shield == 3 && ally.card.health == 5) { if (ally.archerComboActivated) return; has034 = true; }
        }

        // Se os 3 Tanks tier-2 defensores estão em campo, ativa combo:
        // "+10 de armadura a todos" = TODOS os aliados no tabuleiro
        if (has124 && has133 && has034)
        {
            foreach (var ally in allies)
            {
                if (ally == null || ally.card == null) continue;

                ally.currentShield += 10;
                ally.UpdateDisplay();

                // Trava de "1x por partida" SÓ nos 3 defensores da tríade
                // (a flag é compartilhada com as tríades de Arqueiro/Mago)
                if (ally.card.cardClass == CardClass.Tank && ally.card.tier == CardTier.Tier2 &&
                    ((ally.card.attack == 1 && ally.card.shield == 2 && ally.card.health == 5) ||
                     (ally.card.attack == 1 && ally.card.shield == 3 && ally.card.health == 4) ||
                     (ally.card.attack == 0 && ally.card.shield == 3 && ally.card.health == 5)))
                {
                    ally.archerComboActivated = true; // Reusa flag
                }
            }

            Debug.Log($"[TankCombo] Os 3 Tanks tier-2 defensores estão em campo! +10 armadura para TODOS os aliados!");
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

        if (baseAtk == 0 && baseShield == 2 && baseHp == 5)
            TankEffect1_AttackPerMago();
        else if (baseAtk == 1 && baseShield == 1 && baseHp == 5)
            TankEffect2_BoostOnHeal();
        else if (baseAtk == 0 && baseShield == 2 && baseHp == 4)
            TankEffect3_AttackOnHeal();
        else if (baseAtk == 1 && baseShield == 2 && baseHp == 4)
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

    // Checa e aplica efeitos periódicos (chamado a cada round).
    // Vazio de propósito: os efeitos periódicos agora são dirigidos pelos
    // contadores das cartas (SetupPeriodicCounter/OnPeriodicCounterExpired,
    // ticados pelo TurnManager) — mantido para não quebrar o chamador.
    public void CheckPeriodicEffects(int currentRound)
    {
    }

    // ===== CONTADORES DE EFEITO PERIÓDICO (número acima da carta) =====
    // Amarelo = conta TURNOS (toda passagem de vez); rosa = conta ROUNDS
    // (os dois jogadores jogaram). Ao chegar em 0: dispara e renova.

    // Chamado quando a carta entra em campo (ApplyCardEffect)
    public void SetupPeriodicCounter()
    {
        if (cardDisplay == null) cardDisplay = GetComponent<CardDisplay>();
        if (cardDisplay == null || cardDisplay.card == null) return;

        Card c = cardDisplay.card;

        // Healer 1 (ATK 0, HP 3): cura o mais ferido a cada 2 TURNOS (era a
        // cada 2 rounds — lento demais, pedido do Carlos no v4.1)
        if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier1 && c.attack == 0 && c.health == 3)
            cardDisplay.StartEffectCounter(2, false, true);

        // Healer 1 (ATK 1, HP 3): cura 2 em TODOS os aliados a cada 2 TURNOS
        // (além da cura de entrada, que dispara via HealerEffect)
        else if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier1 && c.attack == 1 && c.health == 3)
            cardDisplay.StartEffectCounter(2, false, true);

        // Healer 4 (ATK 2, HP 5): cura 3 no mais ferido TODO round
        else if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier4 && c.attack == 2 && c.health == 5)
            cardDisplay.StartEffectCounter(1, true, true);

        // Tank 5 (ATK 3, Shield 7, HP 10): +2 armadura a aliado a cada 2 TURNOS
        else if (c.cardClass == CardClass.Tank && c.tier == CardTier.Tier5 && c.attack == 3 && c.shield == 7 && c.health == 10)
            cardDisplay.StartEffectCounter(2, false, true);

        // Tank 3 (ATK 2, Shield 3, HP 7): +2 armadura aos Healers a cada 2 TURNOS
        else if (c.cardClass == CardClass.Tank && c.tier == CardTier.Tier3 && c.attack == 2 && c.shield == 3 && c.health == 7)
            cardDisplay.StartEffectCounter(2, false, true);

        // Archer 4 (ATK 4, HP 2): stun em inimigo aleatório a cada 2 TURNOS
        // (o primeiro stun sai na entrada em campo, via ArcherTier4Effect2)
        else if (c.cardClass == CardClass.Arqueiro && c.tier == CardTier.Tier4 && c.attack == 4 && c.health == 2)
            cardDisplay.StartEffectCounter(2, false, true);

        // Mage 4 (ATK 4, HP 4): RAIO em área a cada 2 TURNOS (era +1 ouro
        // por round — trocado no v4.1 pela identidade de dano em área)
        else if (c.cardClass == CardClass.Mago && c.tier == CardTier.Tier4 && c.attack == 4 && c.health == 4)
            cardDisplay.StartEffectCounter(2, false, true);

        // Mage 3 (ATK 3, HP 4): congelar OU causar 1 de dano, a cada 2 TURNOS
        // (delay pedido pelo Carlos — antes disparava todo turno, forte demais)
        else if (c.cardClass == CardClass.Mago && c.tier == CardTier.Tier3 && c.attack == 3 && c.health == 4)
            cardDisplay.StartEffectCounter(2, false, true);
    }

    // Chamado pelo CardDisplay quando o contador periódico chega a 0
    public void OnPeriodicCounterExpired()
    {
        if (cardDisplay == null || cardDisplay.card == null || !cardDisplay.isOnBoard) return;

        // Regra das duplicadas: só uma cópia igual ativa efeito por turno
        // (o contador renova normalmente; só o disparo é engolido)
        if (!DuplicateEffectGate.TryActivate(cardDisplay)) return;

        Card c = cardDisplay.card;

        if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier1 && c.attack == 0 && c.health == 3)
            HealerEffect1_RandomAllyPeriodicHeal();
        else if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier1 && c.attack == 1 && c.health == 3)
            ActivateHealAllAlliesPeriodic();
        else if (c.cardClass == CardClass.Healer && c.tier == CardTier.Tier4 && c.attack == 2 && c.health == 5)
            ActivatePeriodicCure();
        else if (c.cardClass == CardClass.Tank && c.tier == CardTier.Tier5 && c.attack == 3 && c.shield == 7 && c.health == 10)
            ActivatePeriodicShieldTier5Effect2();
        else if (c.cardClass == CardClass.Tank && c.tier == CardTier.Tier3 && c.attack == 2 && c.shield == 3 && c.health == 7)
            ActivateBoostHealersPeriodic();
        else if (c.cardClass == CardClass.Arqueiro && c.tier == CardTier.Tier4 && c.attack == 4 && c.health == 2)
            ActivateRandomStun();
        else if (c.cardClass == CardClass.Mago && c.tier == CardTier.Tier4 && c.attack == 4 && c.health == 4)
            ActivateLightningPeriodic();
        else if (c.cardClass == CardClass.Mago && c.tier == CardTier.Tier3 && c.attack == 3 && c.health == 4)
            ActivateFreezeOrDamagePerTurn();
    }
}
