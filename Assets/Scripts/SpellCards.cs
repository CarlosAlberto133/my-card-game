using System.Collections.Generic;
using UnityEngine;

// Definições dos FEITIÇOS (cartas mágicas de uso único, leva 1 — v4.5).
// Diferente das cartas mágicas de TORRE (TowerCards, equipáveis): o feitiço é
// comprado no 6º slot da loja da PARTIDA, vai para a mão e é lançado uma vez
// num alvo. Regras da casa: máx. 2 feitiços na mão, 1 lançamento por turno,
// não conta Devoção (nunca entra em campo) e não pode ser copiado.
//
// Dados puros — os efeitos são aplicados em GameManager.ExecuteCastSpell.
// Os Cards de runtime seguem o padrão dos lendários da tríade
// (ScriptableObject.CreateInstance, sem .asset no projeto).
public class SpellCard
{
    public int id;
    public string cardName;
    public string description;
    public int cost;          // Custo em ouro (independente do tier)
    public CardTier tier;     // Só controla QUANDO aparece na loja (TierOdds)
    public string artSlug;    // Resources/cards/spells/<artSlug>.png

    public SpellCard(int id, string cardName, int cost, CardTier tier,
                     string artSlug, string description)
    {
        this.id = id;
        this.cardName = cardName;
        this.cost = cost;
        this.tier = tier;
        this.artSlug = artSlug;
        this.description = description;
    }
}

public static class SpellCards
{
    // IDs ESTÁVEIS (viajam no RPC_CastSpell e na telemetria). NÃO renumerar.
    public const int ArmaduraArcana = 0, LaminaEncantada = 1, PeleDePedra = 2,
                     Concentracao = 3, PocaoRevigorante = 4, HinoDeCoragem = 5,
                     TrocaTatica = 6, Transmutacao = 7, AdagaMental = 8,
                     Sono = 9, Teia = 10, Amedrontar = 11, ExplosaoDeChamas = 12,
                     ToqueChocante = 13, SetaInfalivel = 14, DissiparMagia = 15,
                     // Leva 2
                     ConjuraMonstro = 16, Hipnotismo = 17, MuroDePedra = 18,
                     BotasDoVento = 19;

    public static readonly SpellCard[] All =
    {
        // ── Equipamentos e buffs (alvo: aliado) ──
        new SpellCard(ArmaduraArcana, "Armadura Arcana", 1, CardTier.Tier1, "armadura-arcana",
            "Feitiço. Equipa uma carta aliada com +1 de armadura permanente"),
        new SpellCard(LaminaEncantada, "Lâmina Encantada", 3, CardTier.Tier3, "lamina-encantada",
            "Feitiço. Equipa uma carta aliada com +2 de ataque permanente"),
        new SpellCard(PeleDePedra, "Pele de Pedra", 2, CardTier.Tier2, "pele-de-pedra",
            "Feitiço. Uma carta aliada reduz em 1 todo dano recebido por 2 rounds"),
        new SpellCard(Concentracao, "Concentração de Combate", 3, CardTier.Tier3, "concentracao-de-combate",
            "Feitiço. Uma carta aliada pode atacar 2 vezes neste turno"),
        new SpellCard(PocaoRevigorante, "Poção Revigorante", 2, CardTier.Tier1, "pocao-revigorante",
            "Feitiço. Cura 3 de vida numa carta aliada ferida"),
        new SpellCard(HinoDeCoragem, "Hino de Coragem", 4, CardTier.Tier4, "hino-de-coragem",
            "Feitiço. Todos os seus aliados em campo ganham +1 de ataque neste turno"),

        // ── Utilidade e economia (alvo: aliado) ──
        new SpellCard(TrocaTatica, "Troca Tática", 1, CardTier.Tier1, "troca-tatica",
            "Feitiço. Duas cartas aliadas trocam de lugar no tabuleiro"),
        new SpellCard(Transmutacao, "Transmutação em Ouro", 1, CardTier.Tier2, "transmutacao-em-ouro",
            "Feitiço. Destrua uma carta aliada em campo e receba o custo dela +1 em ouro"),

        // ── Debuffs e controle (alvo: inimigo) ──
        new SpellCard(AdagaMental, "Adaga Mental", 4, CardTier.Tier4, "adaga-mental",
            "Feitiço. Atordoa um inimigo por 1 round e causa 2 de dano"),
        new SpellCard(Sono, "Sono", 4, CardTier.Tier4, "sono",
            "Feitiço. Um inimigo dorme por 2 rounds (não age) ou até levar dano"),
        new SpellCard(Teia, "Teia", 3, CardTier.Tier3, "teia",
            "Feitiço. Prende um inimigo e os inimigos adjacentes por 2 rounds: não se movem, mas ainda atacam"),
        new SpellCard(Amedrontar, "Amedrontar", 2, CardTier.Tier2, "amedrontar",
            "Feitiço. Um inimigo recua até 2 casas, apavorado"),

        // ── Dano direto ──
        new SpellCard(ExplosaoDeChamas, "Explosão de Chamas", 3, CardTier.Tier3, "explosao-de-chamas",
            "Feitiço. 2 de dano num inimigo e 1 de dano nos inimigos nas 3 casas atrás dele"),
        new SpellCard(ToqueChocante, "Toque Chocante", 4, CardTier.Tier4, "toque-chocante",
            "Feitiço. Corrente elétrica: 1 de dano e atordoa por 1 round o alvo e até 2 inimigos encadeados"),
        new SpellCard(SetaInfalivel, "Seta Infalível", 3, CardTier.Tier3, "seta-infalivel",
            "Feitiço. Dispara 3 setas: 1 de dano em cada um dos 3 inimigos mais avançados"),

        // ── Anti-magia ──
        new SpellCard(DissiparMagia, "Dissipar Magia", 2, CardTier.Tier2, "dissipar-magia",
            "Feitiço. Remove de uma carta inimiga todos os bônus dados por feitiços"),

        // ── Leva 2 ──
        new SpellCard(ConjuraMonstro, "Conjura Monstro", 3, CardTier.Tier3, "conjura-monstro",
            "Feitiço. Invoca um Monstro 2/0/3 numa casa livre das suas fileiras de casa"),
        new SpellCard(Hipnotismo, "Hipnotismo", 5, CardTier.Tier5, "hipnotismo",
            "Feitiço. Um inimigo ataca imediatamente um aliado DELE adjacente — você escolhe quem bate e quem apanha"),
        new SpellCard(MuroDePedra, "Muro de Pedra", 2, CardTier.Tier2, "muro-de-pedra",
            "Feitiço. Ergue um muro 0/0/4 numa casa livre; ele não age e desmorona após 3 rounds"),
        new SpellCard(BotasDoVento, "Botas do Vento", 3, CardTier.Tier3, "botas-do-vento",
            "Feitiço. Equipa uma carta aliada: ela pode se mover 2 vezes por turno"),
    };

    // ── Unidades criadas por feitiço (Cards de runtime, padrão dos lendários) ──
    // Stats escolhidos para NÃO colidir com nenhuma statline existente (regra
    // do dispatch por stats): Tank 2/0/3 e Tank 0/0/4 estão livres — todos os
    // tanks reais têm escudo ≥ 1 (conferido em Assets/cards em ago/2026).
    static Card monsterCard;
    static Card wallCard;

    public static Card GetMonsterCard()
    {
        if (monsterCard != null) return monsterCard;
        Card c = ScriptableObject.CreateInstance<Card>();
        c.cardName = "Monstro Conjurado";
        c.cardClass = CardClass.Tank;
        c.tier = CardTier.Tier1;
        c.attack = 2; c.shield = 0; c.health = 3;
        c.effectDescription = "Invocado pelo feitiço Conjura Monstro. Luta como uma carta normal";
        c.artwork = Resources.Load<Sprite>("cards/spells/conjura-monstro");
        monsterCard = c;
        return c;
    }

    public static Card GetWallCard()
    {
        if (wallCard != null) return wallCard;
        Card c = ScriptableObject.CreateInstance<Card>();
        c.cardName = "Muro de Pedra";
        c.cardClass = CardClass.Tank;
        c.tier = CardTier.Tier1;
        c.attack = 0; c.shield = 0; c.health = 4;
        c.effectDescription = "Não age nem se move. Bloqueia o caminho e desmorona após 3 rounds";
        c.artwork = Resources.Load<Sprite>("cards/spells/muro-de-pedra");
        wallCard = c;
        return c;
    }

    public static SpellCard Get(int id)
    {
        foreach (var s in All) if (s.id == id) return s;
        return null;
    }

    // Cards de runtime (um por feitiço, cacheado). Nunca entram no CardPool —
    // o 6º slot da loja sorteia direto daqui, com cópias ilimitadas; o filtro
    // de "já tenho essa" (ownedNames) evita oferta repetida.
    static readonly Dictionary<int, Card> runtimeCards = new Dictionary<int, Card>();

    public static Card GetCard(int id)
    {
        Card cached;
        if (runtimeCards.TryGetValue(id, out cached) && cached != null) return cached;

        SpellCard s = Get(id);
        if (s == null) return null;

        Card c = ScriptableObject.CreateInstance<Card>();
        c.cardName = s.cardName;
        // A classe é irrelevante em jogo (feitiço nunca entra em campo, não
        // conta Devoção); Mago só empresta o gradiente roxo do fundo
        c.cardClass = CardClass.Mago;
        c.tier = s.tier;
        c.attack = 0; c.shield = 0; c.health = 0;
        c.effectDescription = s.description;
        c.isSpell = true;
        c.spellId = s.id;
        c.spellCost = s.cost;
        c.artwork = Resources.Load<Sprite>("cards/spells/" + s.artSlug);
        if (c.artwork == null)
            Debug.LogWarning($"[SpellCards] arte 'cards/spells/{s.artSlug}' não encontrada — feitiço sem imagem");

        runtimeCards[id] = c;
        return c;
    }

    // Sorteia o feitiço do 6º slot da loja. DETERMINÍSTICO: usa o
    // UnityEngine.Random já semeado pelo SpawnShopForPlayer (mesma seed nos 2
    // clientes). Respeita a curva de tiers (TierOdds) e pula feitiços que o
    // dono já tem na mão (ownedNames) — com limite de tentativas, igual ao
    // sorteio de cartas normais.
    public static Card DrawRandomSpell(int round, HashSet<string> ownedNames)
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            SpellCard s = All[Random.Range(0, All.Length)];
            if (ownedNames != null && ownedNames.Contains(s.cardName)) continue;
            int roll = Random.Range(0, 100);
            if (roll < TierOdds.GetChance(s.tier, false, round)) return GetCard(s.id);
        }
        // Fallback: qualquer feitiço que o dono não tenha (a loja nunca fica
        // com o slot vazio)
        for (int i = 0; i < All.Length; i++)
        {
            SpellCard s = All[Random.Range(0, All.Length)];
            if (ownedNames == null || !ownedNames.Contains(s.cardName)) return GetCard(s.id);
        }
        return GetCard(All[0].id);
    }

    // Quantos feitiços o jogador tem na MÃO (limite: 2)
    public const int MaxSpellsInHand = 2;

    public static int CountSpellsInHand(int playerNumber)
    {
        int count = 0;
        foreach (CardDisplay cd in Object.FindObjectsOfType<CardDisplay>())
        {
            if (cd == null || cd.card == null || !cd.card.isSpell) continue;
            if (!cd.isInHand || cd.ownerPlayerNumber != playerNumber) continue;
            count++;
        }
        return count;
    }
}
