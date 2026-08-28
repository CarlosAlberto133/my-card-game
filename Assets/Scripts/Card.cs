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

    [Header("Feitiço (v4.5)")]
    // Feitiços são Cards de runtime criados por SpellCards (nunca .asset):
    // vão para a mão como cartas normais, mas são LANÇADOS num alvo em vez de
    // colocados em campo. Os assets existentes ficam com os padrões (false/-1)
    public bool isSpell = false;
    public int spellId = -1;   // id em SpellCards (viaja no RPC_CastSpell)
    public int spellCost = 0;  // custo em ouro (feitiço não segue o tier)

    // Retorna o custo em gold baseado no tier (feitiço tem custo próprio)
    public int GetGoldCost()
    {
        return isSpell ? spellCost : (int)tier;
    }
}
