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

    public PlayerData(int playerNum)
    {
        playerNumber = playerNum;
        playerName = $"Jogador {playerNum}";
        gold = 10;
        health = 10;
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

        gold = 10;
        health = 10;
        cardsBoughtThisTurn = 0;
        storeResetsThisTurn = 0;
    }

    public bool CanBuyCard()
    {
        return cardsBoughtThisTurn < 1; // Apenas 1 carta por turno
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

    public bool CanResetStore()
    {
        return storeResetsThisTurn < 1; // Apenas 1 reset por turno
    }

    public bool PayForStoreReset(int cost)
    {
        if (!HasEnoughGold(cost))
        {
            Debug.Log($"{playerName} não tem ouro suficiente para resetar a loja (custo: {cost})");
            return false;
        }

        if (!CanResetStore())
        {
            Debug.Log($"{playerName} já resetou a loja neste turno!");
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
