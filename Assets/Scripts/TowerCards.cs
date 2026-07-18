using System.Collections.Generic;

// Definições das CARTAS MÁGICAS DE TORRE (sistema de torres v4.2).
// Dados puros — os efeitos são aplicados pelo TowerSystem (equip/round/hooks).
// 3 cartas por classe + 7 universais. Custo fixo em ouro.
public class TowerCard
{
    public int id;
    public string cardName;
    public string description;
    public int classIdx;   // (int)CardClass da torre, ou -1 = universal
    public const int GoldCost = 3;

    public TowerCard(int id, int classIdx, string cardName, string description)
    {
        this.id = id;
        this.classIdx = classIdx;
        this.cardName = cardName;
        this.description = description;
    }
}

public static class TowerCards
{
    // IDs estáveis (0-11 = classe; 12-18 = universais). NÃO renumerar depois
    // de lançado: o id viaja no RPC de compra e na telemetria.
    public const int Muralha = 0, Guarnicao = 1, Represalia = 2;
    public const int FonteDaVida = 3, Bencao = 4, VinculoSagrado = 5;
    public const int Tempestade = 6, Nevasca = 7, Sobrecarga = 8;
    public const int Sentinelas = 9, PontaAfiada = 10, Emboscada = 11;
    public const int Cofres = 12, Muros = 13, Estandarte = 14, MercadoNegro = 15,
                     Canhoneira = 16, Ressurgimento = 17, Recrutamento = 18;

    public static readonly TowerCard[] All =
    {
        // Torre Tank (Bastião)
        new TowerCard(Muralha,     (int)CardClass.Tank, "Muralha", "A torre ganha +10 de vida"),
        new TowerCard(Guarnicao,   (int)CardClass.Tank, "Guarnição", "Aliados nas 2 fileiras de casa ganham +1 de armadura a cada round"),
        new TowerCard(Represalia,  (int)CardClass.Tank, "Represália", "Quando a torre toma dano, o atacante leva 2 de dano de volta"),

        // Torre Healer (Santuário)
        new TowerCard(FonteDaVida, (int)CardClass.Healer, "Fonte da Vida", "A cada 2 rounds, cura 2 no aliado mais ferido"),
        new TowerCard(Bencao,      (int)CardClass.Healer, "Bênção", "Cura a torre em 6"),
        new TowerCard(VinculoSagrado, (int)CardClass.Healer, "Vínculo Sagrado", "Sempre que um aliado for curado, a torre recupera 1 de vida"),

        // Torre Mago (Obelisco)
        new TowerCard(Tempestade,  (int)CardClass.Mago, "Tempestade Arcana", "A cada 3 rounds, um raio causa 2 de dano num inimigo aleatório e 1 nos adjacentes"),
        new TowerCard(Nevasca,     (int)CardClass.Mago, "Nevasca", "A cada 3 rounds, congela um inimigo aleatório"),
        new TowerCard(Sobrecarga,  (int)CardClass.Mago, "Sobrecarga", "Seus magos ganham +1 de ataque (os em campo e os que entrarem)"),

        // Torre Arqueiro (Atalaia)
        new TowerCard(Sentinelas,  (int)CardClass.Arqueiro, "Sentinelas", "A torre atira: 1 de dano num inimigo aleatório todo round"),
        new TowerCard(PontaAfiada, (int)CardClass.Arqueiro, "Ponta Afiada", "Seus arqueiros ganham +1 de ataque (os em campo e os que entrarem)"),
        new TowerCard(Emboscada,   (int)CardClass.Arqueiro, "Emboscada", "Sua torre embosca os reforços: toda carta que o INIMIGO colocar em campo toma 1 de dano ao entrar"),

        // Universais (qualquer torre)
        new TowerCard(Cofres,        -1, "Cofres Reais", "+1 de ouro por round"),
        new TowerCard(Muros,         -1, "Muros Reforçados", "A torre ganha +5 de vida"),
        new TowerCard(Estandarte,    -1, "Estandarte de Guerra", "Suas cartas entram em campo com +1 de vida"),
        new TowerCard(MercadoNegro,  -1, "Mercado Negro", "A primeira compra do seu turno custa 1 a menos"),
        new TowerCard(Canhoneira,    -1, "Canhoneira", "A cada 3 rounds, a torre dá 2 de dano no inimigo mais avançado"),
        new TowerCard(Ressurgimento, -1, "Ressurgimento", "Quando sua torre ficar abaixo de 15 de vida, seus aliados ganham +1 de ataque (1x)"),
        new TowerCard(Recrutamento,  -1, "Recrutamento", "Ao equipar, ganhe 3 de ouro"),
    };

    public static TowerCard Get(int id)
    {
        foreach (var c in All) if (c.id == id) return c;
        return null;
    }

    // As 3 cartas de classe de uma torre (para a tela de escolha e o sorteio)
    public static List<TowerCard> OfClass(int classIdx)
    {
        var list = new List<TowerCard>();
        foreach (var c in All) if (c.classIdx == classIdx) list.Add(c);
        return list;
    }

    public static List<TowerCard> Universals()
    {
        var list = new List<TowerCard>();
        foreach (var c in All) if (c.classIdx < 0) list.Add(c);
        return list;
    }

    // Nome temático da torre de cada classe
    public static string TowerName(int classIdx)
    {
        switch ((CardClass)classIdx)
        {
            case CardClass.Tank: return "Bastião";
            case CardClass.Healer: return "Santuário";
            case CardClass.Mago: return "Obelisco";
            case CardClass.Arqueiro: return "Atalaia";
            default: return "Torre";
        }
    }

    public static string ClassLabel(int classIdx)
    {
        switch ((CardClass)classIdx)
        {
            case CardClass.Tank: return "Tank";
            case CardClass.Healer: return "Healer";
            case CardClass.Mago: return "Mago";
            case CardClass.Arqueiro: return "Arqueiro";
            default: return "?";
        }
    }
}
