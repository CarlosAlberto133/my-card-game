using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public int playerNumber; // 1 ou 2
    public string playerName;
    public int gold;
    public int cardsBoughtThisTurn;

    public PlayerData(int playerNum)
    {
        playerNumber = playerNum;
        playerName = $"Jogador {playerNum}";
        gold = 10;
        cardsBoughtThisTurn = 0;
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
        cardsBoughtThisTurn = 0;
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
    }
}
