using System.Collections.Generic;
using UnityEngine;

// REGRA DAS DUPLICADAS: se um jogador tem 2+ cartas IGUAIS no tabuleiro, só
// UMA delas ativa efeitos automáticos por turno (periódicos, de fim de turno
// e gatilhos tipo "quando X levar dano"). A primeira que disparar no turno
// vira a "designada" e funciona normalmente; as outras cópias ficam mudas até
// o próximo turno. Com apenas 1 cópia em campo, nada muda.
//
// Determinismo (lockstep Photon): todos os pontos que consultam o gate rodam
// dentro de fluxos de RPC com varredura do tabuleiro em ordem fixa — a
// designação acontece na mesma ordem nos dois clientes. O reset roda na fase 3
// do TickBoardOnTurnEnd (dentro do RPC_EndTurn).
public static class DuplicateEffectGate
{
    // Por identidade de carta: qual instância é a "ativa" neste turno
    private static readonly Dictionary<string, CardDisplay> activeDuplicate =
        new Dictionary<string, CardDisplay>();

    // Pode esta carta ativar seu efeito automático agora?
    public static bool TryActivate(CardDisplay card)
    {
        if (card == null || card.card == null) return true;

        // Sem duplicada no tabuleiro do dono: sempre pode (caso comum)
        if (!HasDuplicateOnBoard(card)) return true;

        string key = Key(card);
        CardDisplay chosen;
        if (activeDuplicate.TryGetValue(key, out chosen) && chosen != null)
        {
            bool allowed = chosen == card;
            if (!allowed)
                Debug.Log($"[DuplicateGate] {card.card.cardName} (duplicada): só uma cópia ativa efeito por turno");
            return allowed;
        }

        // Primeira do turno a disparar: vira a designada
        activeDuplicate[key] = card;
        return true;
    }

    // Chamado a cada passagem de turno (fase 3 do TickBoardOnTurnEnd) e no
    // início da partida
    public static void ResetTurn()
    {
        activeDuplicate.Clear();
    }

    static bool HasDuplicateOnBoard(CardDisplay card)
    {
        BoardManager board = BoardManager.Instance;
        if (board == null || card.ownerPlayerNumber == 0) return false;

        int count = 0;
        foreach (var c in board.GetCardsByOwner(card.ownerPlayerNumber))
        {
            if (c != null && c.card != null && SameIdentity(c.card, card.card))
                count++;
            if (count > 1) return true;
        }
        return false;
    }

    static bool SameIdentity(Card a, Card b)
    {
        return a.cardClass == b.cardClass && a.tier == b.tier &&
               a.attack == b.attack && a.shield == b.shield && a.health == b.health &&
               a.cardName == b.cardName;
    }

    static string Key(CardDisplay card)
    {
        Card c = card.card;
        return $"{card.ownerPlayerNumber}|{c.cardClass}|{(int)c.tier}|{c.attack}|{c.shield}|{c.health}|{c.cardName}";
    }
}
