using System.Collections.Generic;

// Tabela e utilidades das TRÍADES tier-2 (os combos de 3 cartas distintas da
// mesma classe). Usada pela UI (tooltip de carta mostra "2/3 em campo") — a
// mesma identidade de membros que o CardEffectSimple checa nos combos.
// Statlines {ataque, escudo, vida} — ver [[balance-stats-v39]].
public static class Triads
{
    static readonly Dictionary<CardClass, int[][]> Members = new Dictionary<CardClass, int[][]>
    {
        { CardClass.Arqueiro, new[] { new[] {2,0,3}, new[] {3,0,2}, new[] {2,0,2} } },
        { CardClass.Healer,   new[] { new[] {1,0,4}, new[] {0,0,4}, new[] {0,0,3} } },
        { CardClass.Mago,     new[] { new[] {2,0,4}, new[] {2,0,3}, new[] {3,0,3} } },
        { CardClass.Tank,     new[] { new[] {1,2,5}, new[] {1,3,4}, new[] {0,3,5} } },
    };

    // Índice do membro (0..2) que esta carta representa, ou -1 se não é de tríade
    public static int MemberIndex(Card card)
    {
        if (card == null || card.tier != CardTier.Tier2) return -1;
        int[][] members;
        if (!Members.TryGetValue(card.cardClass, out members)) return -1;
        for (int i = 0; i < members.Length; i++)
            if (card.attack == members[i][0] && card.shield == members[i][1] &&
                card.health == members[i][2]) return i;
        return -1;
    }

    public static bool IsTriadCard(Card card)
    {
        return MemberIndex(card) >= 0;
    }

    // Quantos membros DISTINTOS da tríade daquela carta o dono já tem em campo
    // (0..3). Retorna -1 se a carta não é de tríade.
    public static int OwnedDistinct(int ownerPlayerNumber, Card card)
    {
        int idx = MemberIndex(card);
        if (idx < 0) return -1;
        BoardManager board = BoardManager.Instance;
        if (board == null) return 0;

        bool[] owned = new bool[3];
        foreach (var c in board.GetCardsByOwner(ownerPlayerNumber))
        {
            if (c == null || c.card == null || c.card.cardClass != card.cardClass) continue;
            int m = MemberIndex(c.card);
            if (m >= 0) owned[m] = true;
        }
        int n = 0;
        for (int i = 0; i < 3; i++) if (owned[i]) n++;
        return n;
    }
}
