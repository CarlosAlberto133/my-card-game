using UnityEngine;

// ═══════════════ DEVOÇÃO DE CLASSE ═══════════════
// Recompensa builds focadas numa classe só (pedido do Carlos, jul/2026): conta
// as unidades da MESMA classe que o jogador tem em campo e liga bônus
// automáticos em dois degraus:
//
//   3+ em campo = degrau 1        5+ em campo = degrau 2 (soma com o 1)
//
//   🛡 Tanks    "Falange"       : ataques causam -1 de dano (mín. 1)
//                                | atacante ADJACENTE leva 1 de volta
//   🔮 Magos    "Escola Arcana" : efeitos de dano dos magos +1
//                                | raio de 1 no inimigo mais avançado por round
//   🏹 Arqueiros "Matilha"      : +1 ATK contra alvos já feridos
//                                | quebra 1 de escudo do alvo antes do golpe
//   ✨ Healers  "Oferendas"     : +1 de ouro por round
//                                | +2 de ouro por round
//
// REGRAS: cópias e invocações de efeito NÃO contam (CardDisplay.isEffectSpawn)
// — só cartas jogadas da mão. A contagem é do estado VIVO do tabuleiro: se as
// unidades morrem e o degrau desliga, o bônus some sozinho.
// LOCKSTEP: tudo determinístico — os ganchos rodam dentro de fluxos que chegam
// por RPC aos dois clientes (ataques, danos, virada de round no EndTurn).
public static class ClassDevotion
{
    public const int Tier1Count = 3;
    public const int Tier2Count = 5;

    // Quantas unidades DE VERDADE (jogadas da mão) desta classe o jogador tem
    public static int CountOnBoard(int playerNumber, CardClass cls)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || playerNumber == 0) return 0;

        int count = 0;
        foreach (CardDisplay cd in board.GetCardsByOwner(playerNumber))
        {
            if (cd == null || cd.card == null) continue;
            if (cd.card.cardClass != cls) continue;
            if (cd.isEffectSpawn) continue; // cópia/invocação não conta
            count++;
        }
        return count;
    }

    // 0 = nada, 1 = degrau 1 (3+), 2 = degrau 2 (5+)
    public static int TierOf(int playerNumber, CardClass cls)
    {
        int count = CountOnBoard(playerNumber, cls);
        if (count >= Tier2Count) return 2;
        if (count >= Tier1Count) return 1;
        return 0;
    }

    // Nome temático da devoção (UI e mensagens)
    public static string DevotionName(CardClass cls)
    {
        switch (cls)
        {
            case CardClass.Tank: return "Falange";
            case CardClass.Mago: return "Escola Arcana";
            case CardClass.Arqueiro: return "Matilha";
            case CardClass.Healer: return "Oferendas";
            default: return "?";
        }
    }

    // ── MAGOS: +1 nos danos de EFEITO dos magos (degrau 1) ───────────────
    public static int MageEffectBonus(int playerNumber)
    {
        return TierOf(playerNumber, CardClass.Mago) >= 1 ? 1 : 0;
    }

    // ── TANKS degrau 1: ataques contra tanks causam -1 (nunca abaixo de 1).
    // Só ATAQUES (attacker != null no chamador) — dano de efeito passa cheio
    public static int ReduceAttackDamageOnTank(CardDisplay defender, int damage)
    {
        if (defender == null || defender.card == null) return damage;
        if (defender.card.cardClass != CardClass.Tank) return damage;
        if (damage <= 1) return damage;
        if (TierOf(defender.ownerPlayerNumber, CardClass.Tank) < 1) return damage;

        Debug.Log($"[Devoção/Falange] {defender.card.cardName}: -1 de dano (fica {damage - 1})");
        return damage - 1;
    }

    // ── TANKS degrau 2: atacante ADJACENTE (corpo a corpo) leva 1 de volta.
    // O reflexo entra por ApplyDamageNormally com attacker nulo: não dispara
    // intercepto/redirecionamento nem outro reflexo (sem correntes infinitas)
    public static void TryReflect(CardDisplay defender, CardDisplay attacker)
    {
        if (defender == null || attacker == null) return;
        if (defender.card == null || attacker.card == null) return;
        if (defender.card.cardClass != CardClass.Tank) return;
        if (!attacker.isOnBoard || attacker.currentHealth <= 0) return;
        if (defender.currentTile == null || attacker.currentTile == null) return;
        if (TierOf(defender.ownerPlayerNumber, CardClass.Tank) < 2) return;

        // Corpo a corpo = casas encostadas (arqueiro atirando de longe escapa)
        int dr = Mathf.Abs(defender.currentTile.row - attacker.currentTile.row);
        int dc = Mathf.Abs(defender.currentTile.column - attacker.currentTile.column);
        if (dr > 1 || dc > 1) return;

        FloatingTextFX.ShowAboveCard(attacker, "FALANGE! -1", FloatingTextFX.EffectColor, 4.0f);
        Debug.Log($"[Devoção/Falange] {attacker.card.cardName} levou 1 de volta ao atacar {defender.card.cardName}");
        attacker.ApplyDamageNormally(1, null);
    }

    // ── VIRADA DE ROUND: ouro dos Healers + raio dos Magos (degrau 2) ─────
    // Chamado pelo TurnManager DEPOIS da renda do round (dentro do fluxo
    // sincronizado de fim de turno — idêntico nos dois clientes)
    public static void OnRoundChanged()
    {
        for (int p = 1; p <= 2; p++)
        {
            // Diagnóstico: contagens REAIS (cópias/ecos fora) a cada round —
            // se um degrau não ligar, o log mostra exatamente o porquê
            Debug.Log($"[Devoção] P{p}: Tanks={CountOnBoard(p, CardClass.Tank)} " +
                      $"Magos={CountOnBoard(p, CardClass.Mago)} " +
                      $"Arqueiros={CountOnBoard(p, CardClass.Arqueiro)} " +
                      $"Healers={CountOnBoard(p, CardClass.Healer)} (cópias não contam)");

            // Healers (Oferendas): +1 de ouro no degrau 1, +2 no degrau 2
            int healerTier = TierOf(p, CardClass.Healer);
            if (healerTier >= 1)
            {
                int gold = healerTier >= 2 ? 2 : 1;
                PlayerData pl = GetPlayer(p);
                if (pl != null)
                {
                    pl.AddGold(gold);
                    Debug.Log($"[Devoção/Oferendas] P{p}: +{gold} de ouro ({CountOnBoard(p, CardClass.Healer)} healers em campo)");
                }
            }

            // Magos (Escola Arcana, degrau 2): raio no inimigo mais avançado
            if (TierOf(p, CardClass.Mago) >= 2)
                FireArcaneRay(p);
        }
    }

    static PlayerData GetPlayer(int playerNumber)
    {
        TurnManager tm = TurnManager.Instance;
        if (tm == null) return null;
        return playerNumber == 2 ? tm.player2 : tm.player1;
    }

    // Raio arcano: 1 de dano no inimigo mais AVANÇADO (o mais perto do lado
    // do dono da devoção). Desempate determinístico pela menor coluna.
    static void FireArcaneRay(int playerNumber)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null) return;

        int enemy = playerNumber == 1 ? 2 : 1;
        CardDisplay target = null;
        foreach (CardDisplay cd in board.GetCardsByOwner(enemy))
        {
            if (cd == null || cd.card == null || cd.currentTile == null) continue;
            if (target == null) { target = cd; continue; }

            // P1 defende as fileiras baixas: inimigo mais avançado = menor row.
            // P2 defende as altas: maior row.
            int a = cd.currentTile.row, b = target.currentTile.row;
            bool moreAdvanced = playerNumber == 1 ? a < b : a > b;
            bool tie = a == b && cd.currentTile.column < target.currentTile.column;
            if (moreAdvanced || tie) target = cd;
        }
        if (target == null) return;

        // O mago mais avançado do dono "conjura" o raio (visual + telemetria)
        CardDisplay caster = null;
        foreach (CardDisplay cd in board.GetCardsByOwner(playerNumber))
        {
            if (cd == null || cd.card == null || cd.currentTile == null) continue;
            if (cd.card.cardClass != CardClass.Mago) continue;
            if (caster == null) { caster = cd; continue; }
            int a = cd.currentTile.row, b = caster.currentTile.row;
            if (playerNumber == 1 ? a > b : a < b) caster = cd;
        }

        if (caster != null)
            EffectProjectileFX.Launch(caster, target, EffectProjectileFX.Arcane);
        FloatingTextFX.ShowAboveCard(target, "RAIO ARCANO! -1", FloatingTextFX.EffectColor, 4.2f);
        Debug.Log($"[Devoção/Escola Arcana] P{playerNumber}: raio em {target.card.cardName}");

        MatchStatsTracker.EffectSource = caster;
        target.TakeDamage(1);
        MatchStatsTracker.EffectSource = null;
    }
}
