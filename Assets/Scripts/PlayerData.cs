using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int playerNumber; // 1 ou 2
    public string playerName;
    public int gold;
    public int health; // Vida do jogador (torre)
    public int cardsBoughtThisTurn;
    public int storeResetsThisTurn; // Quantas vezes resetou a loja neste turno
    public int spellsCastThisTurn;  // Feitiços lançados neste turno (máx. 1) — só muda dentro de RPCs

    // MANA (v4.6): recurso de INVOCAÇÃO. Colocar uma carta da mão em campo
    // custa o TIER dela; restaura para o máximo quando o jogador PASSA o
    // turno (ResetTurn). Feitiços e invocações de efeito NÃO gastam mana.
    // Só muda dentro de RPCs (lockstep)
    public const int MaxMana = 6;
    public int mana = MaxMana;
    // [DECKMODE] Teto ATUAL de mana: no modo padrão é sempre 6 (comportamento
    // idêntico ao anterior); no modo deck sobe com a rampa (DeckMode.OnTurnStarted)
    public int manaCap = MaxMana;
    public int freePurchases = 0; // Compras grátis pendentes (Healer 5) — não gastam ouro nem limite; persistem até usar

    // Fase inicial de compras: começa com 20 de ouro (teto 20); quando a partida
    // começa, sobras acima de 10 voltam para 10 (TurnManager.StartGame)
    public const int LobbyStartingGold = 20;

    // Vida da torre (ponto único de verdade — usada na criação, no display e no
    // combo de Healers tier-2 que restaura a vida cheia)
    public const int MaxTowerHealth = 30;

    public PlayerData(int playerNum)
    {
        playerNumber = playerNum;
        playerName = $"Jogador {playerNum}";
        gold = LobbyStartingGold;
        health = MaxTowerHealth;
        cardsBoughtThisTurn = 0;
        storeResetsThisTurn = 0;
    }

    public PlayerData(string name)
    {
        playerName = name;
        // Extrai número do nome se possível
        if (name.Contains("1"))
            playerNumber = 1;
        else if (name.Contains("2"))
            playerNumber = 2;
        else
            playerNumber = 1;

        gold = LobbyStartingGold;
        health = MaxTowerHealth;
        cardsBoughtThisTurn = 0;
        storeResetsThisTurn = 0;
    }

    public const int MaxCardsPerTurn = 2;
    public const int MaxCardsInLobby = 5; // Fase inicial: até 5 das 10 cartas

    public bool CanBuyCard()
    {
        return cardsBoughtThisTurn < MaxCardsPerTurn; // Até 2 cartas por turno
    }

    // Fase inicial de compras (sem turnos): o contador não é resetado, então
    // vale como total de compras da fase
    public bool CanBuyCardInLobby()
    {
        return cardsBoughtThisTurn < MaxCardsInLobby;
    }

    public bool HasEnoughGold(int cost)
    {
        return gold >= cost;
    }

    public void BuyCard(int cost)
    {
        gold -= cost;
        cardsBoughtThisTurn++;
    }

    public void ResetTurn()
    {
        cardsBoughtThisTurn = 0;
        storeResetsThisTurn = 0;
        spellsCastThisTurn = 0;
        mana = manaCap; // mana volta cheia ao passar o turno (não acumula)
                        // [DECKMODE] manaCap = 6 fixo no padrão; rampa no modo deck
    }

    public bool CanPayMana(int cost)
    {
        return mana >= cost;
    }

    public void SpendMana(int cost)
    {
        mana -= cost;
        if (mana < 0) mana = 0;
        Debug.Log($"{playerName} gastou {cost} de mana. Restante: {mana}/{MaxMana}");
    }

    // Regra da casa: 1 feitiço por turno
    public bool CanCastSpell()
    {
        return spellsCastThisTurn < 1;
    }

    public void AddGold(int amount, int maxGold = 10)
    {
        gold += amount;
        if (gold > maxGold)
        {
            gold = maxGold;
        }
        Debug.Log($"{playerName} ganhou {amount} de ouro. Total: {gold}");
    }

    public bool CanResetStore(int maxResets = 1)
    {
        return storeResetsThisTurn < maxResets; // Padrão: 1 reset por turno
    }

    public bool PayForStoreReset(int cost, int maxResets = 1)
    {
        if (!HasEnoughGold(cost))
        {
            Debug.Log($"{playerName} não tem ouro suficiente para resetar a loja (custo: {cost})");
            return false;
        }

        if (!CanResetStore(maxResets))
        {
            Debug.Log($"{playerName} atingiu o limite de resets ({maxResets})!");
            return false;
        }

        gold -= cost;
        storeResetsThisTurn++;
        Debug.Log($"{playerName} resetou a loja por {cost} de ouro. Ouro restante: {gold}");
        return true;
    }

    // Sistema de vida da torre
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
        Debug.Log($">>> {playerName} levou {damage} de dano! Vida restante: {health}");
    }

    public bool IsDefeated()
    {
        return health <= 0;
    }
}
