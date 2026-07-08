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
    // Perca 2 de vida para ganhar +1 de ataque
    public void ArcherEffect()
    {
        if (cardDisplay == null) return;

        cardDisplay.currentHealth -= 2;
        cardDisplay.currentAttack += 1;

        if (cardDisplay.currentHealth < 0)
            cardDisplay.currentHealth = 0;

        cardDisplay.UpdateDisplay();

        Debug.Log($"[ArcherEffect] {cardDisplay.card.cardName}: -2 HP, +1 ATK. HP agora: {cardDisplay.currentHealth}, ATK agora: {cardDisplay.currentAttack}");
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
