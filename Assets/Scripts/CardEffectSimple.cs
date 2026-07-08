using UnityEngine;

public class CardEffectSimple : MonoBehaviour
{
    CardDisplay cardDisplay;
    int lastHealerEffectRound = -2; // Rastreia o último round que o efeito foi ativado

    void Start()
    {
        cardDisplay = GetComponent<CardDisplay>();
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

    // Efeito 2: Ao entrar em campo, cause 1 de dano na fileira toda à sua frente
    void ArcherEffect2_DamageRow()
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || cardDisplay.currentTile == null) return;

        int damageDealCount = 0;
        var targetTile = board.GetAdjacentTile(cardDisplay.currentTile, "forward", cardDisplay.ownerPlayerNumber);

        if (targetTile != null && targetTile.occupiedCard != null)
        {
            CardDisplay targetCard = targetTile.occupiedCard.GetComponent<CardDisplay>();
            if (targetCard != null)
            {
                targetCard.TakeDamage(1);
                damageDealCount++;
            }
        }

        Debug.Log($"[ArcherEffect2] {cardDisplay.card.cardName}: Causei 1 de dano a {damageDealCount} carta(s) na frente");
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
                    Debug.Log($"[ArcherEffect3] {cardDisplay.card.cardName}: Criou uma cópia em {emptyTile.Position}");
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

        // Marca que a carta usou o dodge (será resetado no início de um novo jogo)
        cardDisplay.treeDefenseUsed = true;
        cardDisplay.isInvulnerable = true;

        cardDisplay.UpdateDisplay();

        Debug.Log($"[ArcherEffect4] {cardDisplay.card.cardName}: Subiu na árvore! Intangível por 1 turno");
    }

    // ===== HEALER TIER-1 =====
    // Cura 2 HP a um aliado aleatório a cada 2 rounds
    public void HealerEffect()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        var allies = board.GetCardsByOwner(cardDisplay.ownerPlayerNumber);
        if (allies.Count == 0) return;

        CardDisplay targetAlly = allies[Random.Range(0, allies.Count)];
        targetAlly.Heal(2, cardDisplay);

        Debug.Log($"[HealerEffect] {cardDisplay.card.cardName}: Curou {targetAlly.card.cardName} por 2 HP");
    }

    // ===== MAGE TIER-1 =====
    // Conceda +1 de ataque ao healer que levar dano. Se tiver um tanque em campo, conceda +2
    public void MageEffect(CardDisplay healerThatTookDamage)
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

        Debug.Log($"[MageEffect] {cardDisplay.card.cardName}: Deu +{bonus} ATK ao {healerThatTookDamage.card.cardName}");
    }

    // ===== TANK TIER-1 =====
    // Ganha +1 de ataque por cada mago em campo
    public void TankEffect()
    {
        if (cardDisplay == null) return;

        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int magoCount = board.CountCardsByClass(cardDisplay.ownerPlayerNumber, CardClass.Mago);
        int bonus = magoCount;

        cardDisplay.currentAttack += bonus;
        cardDisplay.UpdateDisplay();

        Debug.Log($"[TankEffect] {cardDisplay.card.cardName}: +{bonus} ATK ({magoCount} magos em campo). ATK agora: {cardDisplay.currentAttack}");
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
