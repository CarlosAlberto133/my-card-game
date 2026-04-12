using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card Game/Card")]
public class Card : ScriptableObject
{
    [Header("Informações Básicas")]
    public string cardName;
    public CardClass cardClass;
    public CardTier tier;

    [Header("Artwork")]
    public Sprite artwork;

    [Header("Stats")]
    public int attack;
    public int shield;
    public int health;

    [Header("Efeito (Futuro)")]
    [TextArea(3, 5)]
    public string effectDescription;

    // Retorna o custo em gold baseado no tier
    public int GetGoldCost()
    {
        return (int)tier;
    }
}
