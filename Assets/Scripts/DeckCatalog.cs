// ═══════════════ [DECKMODE] arquivo exclusivo do MODO DECK (teste) ═══════════════
// Catálogo ESTÁTICO das cartas-unidade para o construtor de decks do LOBBY.
// O lobby não enxerga os .asset (eles não estão em Resources), então o builder
// usa esta cópia dos dados. Na PARTIDA o deck resolve por NOME contra o
// CardPool.allBaseCards (a fonte da verdade) — este arquivo só alimenta a UI
// e a validação. GERADO por script a partir de Assets/cards em ago/2026;
// se uma carta mudar de nome, é só regerar (ver MODO-DECK-REMOVER.md).
// Para REMOVER o modo deck: apagar este arquivo inteiro.

public class DeckCatalogEntry
{
    public string cardName;
    public int classIdx;   // (int)CardClass
    public int tier;
    public int attack, shield, health;
    public bool usesGold;  // depende de ouro/loja — fora do modo deck

    public DeckCatalogEntry(string cardName, int classIdx, int tier,
                            int attack, int shield, int health, bool usesGold)
    {
        this.cardName = cardName;
        this.classIdx = classIdx;
        this.tier = tier;
        this.attack = attack;
        this.shield = shield;
        this.health = health;
        this.usesGold = usesGold;
    }
}

public static class DeckCatalog
{
    public static readonly DeckCatalogEntry[] Units =
    {
        new DeckCatalogEntry("Abençoado", 0, 1, 1, 1, 5, false),
        new DeckCatalogEntry("Escudeiro Arcano", 0, 1, 1, 2, 4, false),
        new DeckCatalogEntry("Guarda Rúnico", 0, 1, 0, 2, 5, false),
        new DeckCatalogEntry("Penitente", 0, 1, 0, 2, 4, false),
        new DeckCatalogEntry("Vanguarda", 0, 1, 1, 2, 5, false),
        new DeckCatalogEntry("Centurião", 0, 2, 1, 3, 4, false),
        new DeckCatalogEntry("Guarda-Costas", 0, 2, 1, 3, 5, false),
        new DeckCatalogEntry("Legionário", 0, 2, 1, 2, 5, false),
        new DeckCatalogEntry("Pretoriano", 0, 2, 0, 3, 5, false),
        new DeckCatalogEntry("Capitão de Ferro", 0, 3, 2, 3, 6, false),
        new DeckCatalogEntry("Guardião da Fé", 0, 3, 2, 3, 7, false),
        new DeckCatalogEntry("Irmão de Armas", 0, 3, 2, 4, 6, false),
        new DeckCatalogEntry("Égide Arcana", 0, 3, 2, 4, 7, false),
        new DeckCatalogEntry("Baluarte", 0, 4, 3, 7, 8, false),
        new DeckCatalogEntry("Porta-Bandeira", 0, 4, 2, 6, 8, false),
        new DeckCatalogEntry("Quebra-Golpes", 0, 4, 2, 7, 7, false),
        new DeckCatalogEntry("Titã de Bronze", 0, 4, 2, 6, 7, false),
        new DeckCatalogEntry("Colosso", 0, 5, 3, 7, 11, false),
        new DeckCatalogEntry("Senhor da Guerra", 0, 5, 3, 8, 9, false),
        new DeckCatalogEntry("Titã de Ferro", 0, 5, 3, 7, 10, false),
        new DeckCatalogEntry("Conjurador", 1, 1, 2, 0, 4, false),
        new DeckCatalogEntry("Criomante", 1, 1, 1, 0, 3, false),
        new DeckCatalogEntry("Encantador", 1, 1, 2, 0, 3, false),
        new DeckCatalogEntry("Evocador", 1, 1, 3, 0, 4, false),
        new DeckCatalogEntry("Fagulha", 1, 1, 1, 0, 4, false),
        new DeckCatalogEntry("Ferrugem", 1, 2, 3, 0, 4, false),
        new DeckCatalogEntry("Glifo", 1, 2, 2, 0, 4, false),
        new DeckCatalogEntry("Runa", 1, 2, 2, 0, 3, false),
        new DeckCatalogEntry("Sigilo", 1, 2, 3, 0, 3, false),
        new DeckCatalogEntry("Estilhaço", 1, 3, 3, 0, 5, false),
        new DeckCatalogEntry("Invernal", 1, 3, 3, 0, 4, false),
        new DeckCatalogEntry("Pirotécnico", 1, 3, 2, 0, 5, false),
        new DeckCatalogEntry("Usurpador", 1, 3, 1, 0, 3, false),
        new DeckCatalogEntry("Aniquilador", 1, 4, 3, 0, 5, false),
        new DeckCatalogEntry("Canalizador", 1, 4, 4, 0, 6, false),
        new DeckCatalogEntry("Eletromante", 1, 4, 4, 0, 4, false),
        new DeckCatalogEntry("Purificador", 1, 4, 4, 0, 5, false),
        new DeckCatalogEntry("Arquimago", 1, 5, 5, 0, 6, false),
        new DeckCatalogEntry("Lorde do Gelo", 1, 5, 5, 0, 5, false),
        new DeckCatalogEntry("Metamorfo", 1, 5, 4, 0, 6, false),
        new DeckCatalogEntry("Curandeira", 2, 1, 0, 0, 3, false),
        new DeckCatalogEntry("Devota", 2, 1, 2, 0, 3, false),
        new DeckCatalogEntry("Esmoleira", 2, 1, 1, 0, 2, true),
        new DeckCatalogEntry("Missionária", 2, 1, 1, 0, 3, false),
        new DeckCatalogEntry("Vestal", 2, 1, 0, 0, 4, false),
        new DeckCatalogEntry("Caridade", 2, 2, 1, 0, 4, false),
        new DeckCatalogEntry("Esperança", 2, 2, 0, 0, 4, false),
        new DeckCatalogEntry("Fé", 2, 2, 0, 0, 3, false),
        new DeckCatalogEntry("Matriarca", 2, 2, 1, 0, 3, false),
        new DeckCatalogEntry("Mecenas", 2, 3, 2, 0, 3, true),
        new DeckCatalogEntry("Provedora", 2, 3, 1, 0, 4, true),
        new DeckCatalogEntry("Samaritana", 2, 3, 2, 0, 4, false),
        new DeckCatalogEntry("Tesoureira", 2, 3, 1, 0, 3, true),
        new DeckCatalogEntry("Alta Sacerdotisa", 2, 4, 3, 0, 4, false),
        new DeckCatalogEntry("Anjo da Guarda", 2, 4, 1, 0, 4, false),
        new DeckCatalogEntry("Guardiã do Cofre", 2, 4, 2, 0, 4, true),
        new DeckCatalogEntry("Milagreira", 2, 4, 2, 0, 5, false),
        new DeckCatalogEntry("Benfeitora", 2, 5, 3, 0, 6, true),
        new DeckCatalogEntry("Oráculo", 2, 5, 3, 0, 7, false),
        new DeckCatalogEntry("Padroeira", 2, 5, 2, 0, 7, false),
        new DeckCatalogEntry("Batedora", 3, 1, 3, 0, 4, false),
        new DeckCatalogEntry("Furtiva", 3, 1, 3, 0, 2, false),
        new DeckCatalogEntry("Miragem", 3, 1, 1, 0, 3, false),
        new DeckCatalogEntry("Rajada", 3, 1, 2, 0, 2, false),
        new DeckCatalogEntry("Sanguinária", 3, 1, 2, 0, 3, false),
        new DeckCatalogEntry("Flecha Fiel", 3, 2, 3, 0, 3, false),
        new DeckCatalogEntry("Tufão", 3, 2, 3, 0, 2, false),
        new DeckCatalogEntry("Vendaval", 3, 2, 2, 0, 2, false),
        new DeckCatalogEntry("Zéfiro", 3, 2, 2, 0, 3, false),
        new DeckCatalogEntry("Couraçada", 3, 3, 4, 0, 3, false),
        new DeckCatalogEntry("Falcoeira", 3, 3, 3, 0, 3, false),
        new DeckCatalogEntry("Reflexo", 3, 3, 4, 0, 2, false),
        new DeckCatalogEntry("Sabotadora", 3, 3, 3, 0, 2, false),
        new DeckCatalogEntry("Acrobata", 3, 4, 4, 0, 3, false),
        new DeckCatalogEntry("Inquisidora", 3, 4, 5, 0, 3, false),
        new DeckCatalogEntry("Perfuradora", 3, 4, 5, 0, 2, false),
        new DeckCatalogEntry("Víbora", 3, 4, 4, 0, 2, false),
        new DeckCatalogEntry("Executora", 3, 5, 5, 0, 4, false),
        new DeckCatalogEntry("Hidra", 3, 5, 6, 0, 3, false),
        new DeckCatalogEntry("Quebra-Escudos", 3, 5, 6, 0, 4, false),
    };
}
