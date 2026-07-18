using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Telemetria de BALANCEAMENTO: acumula, durante a partida, o que cada CARTA fez
// (comprada, jogada, kills, mortes, dano causado/recebido, cura, ouro gerado,
// debuffs aplicados) e sobe tudo para a tabela `card_stats` no Supabase no fim
// da partida. O dashboard do fórum (só-admin) lê e desenha os gráficos.
//
// Determinismo/rede: como o Photon é lockstep, TODO cliente simula o tabuleiro
// inteiro (as cartas dos DOIS jogadores) de forma idêntica — então o acumulador
// de UM cliente já é a foto completa da partida. Para não duplicar, só o MASTER
// client sobe. Treino contra bot é ignorado (não é dado real de PvP).
//
// 100% HTTP fora do lockstep, com a chave publishable (insert anônimo, igual ao
// sistema de report). Falha de rede só perde a telemetria daquela partida — não
// afeta o jogo nem a partida em si.
public static class MatchStatsTracker
{
    const string SupabaseUrl = "https://zutdbgltjphsbakeeoda.supabase.co";
    const string SupabaseKey = "sb_publishable_sIC5NDivItmQ_IuVOmWSdQ_LnyaSSOO";

    // Fonte de dano/debuff de EFEITO (bola de fogo, respingo, congelar em área…):
    // esses não passam por um "atacante" no fluxo de dano. O efeito seta esta
    // referência ANTES de aplicar o dano e limpa depois; o funil de dano a usa
    // SÓ para atribuir a estatística (nunca para os ganchos reativos).
    public static CardDisplay EffectSource;

    // Uma linha acumulada por (dono, identidade da carta)
    [Serializable]
    public class Row
    {
        public string game_version, map, card_key, card_name, card_class;
        public int tier, owner_player, match_seed;
        public bool won;
        public int bought, played, kills, deaths;
        public int damage_dealt, damage_taken, healing_done, gold_generated, debuffs_applied;
    }

    static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
    static bool active = false;

    // ── Ciclo de vida ────────────────────────────────────────────────────
    public static void Reset()
    {
        rows.Clear();
        EffectSource = null;
        active = true;
    }

    // Identidade estável da carta: classe|tier|atk|esc|hp (única no jogo). O bot
    // e as cartas sem dono (loja) não entram como "dono", mas a compra/uso são
    // atribuídos ao jogador que agiu.
    static string ClassName(CardClass c)
    {
        switch (c)
        {
            case CardClass.Tank: return "Tank";
            case CardClass.Mago: return "Mago";
            case CardClass.Healer: return "Healer";
            case CardClass.Arqueiro: return "Arqueiro";
            default: return "?";
        }
    }

    static string CardKey(Card card)
    {
        // Usa os stats BASE (identidade), não os atuais (que mudam com buffs)
        return ClassName(card.cardClass) + "|" + (int)card.tier + "|" +
               card.attack + "|" + card.shield + "|" + card.health;
    }

    // Pega/cria a linha acumuladora de uma carta pertencente a um jogador
    static Row RowFor(Card card, int ownerPlayer)
    {
        if (!active || card == null || ownerPlayer <= 0) return null;
        string key = ownerPlayer + "#" + CardKey(card);
        Row r;
        if (!rows.TryGetValue(key, out r))
        {
            r = new Row
            {
                card_key = CardKey(card),
                card_name = card.cardName,
                card_class = ClassName(card.cardClass),
                tier = (int)card.tier,
                owner_player = ownerPlayer,
            };
            rows[key] = r;
        }
        return r;
    }

    // ── Registro de eventos (chamado pelos ganchos do jogo) ──────────────
    public static void RecordBought(Card card, int buyerPlayer)
    {
        var r = RowFor(card, buyerPlayer);
        if (r != null) r.bought++;
    }

    public static void RecordPlayed(CardDisplay cd)
    {
        if (cd == null || cd.card == null) return;
        var r = RowFor(cd.card, cd.ownerPlayerNumber);
        if (r != null) r.played++;
    }

    // Dano efetivamente aplicado (escudo + vida). source pode ser o atacante ou,
    // em dano de efeito, o EffectSource. victim sempre existe.
    public static void RecordDamage(CardDisplay source, CardDisplay victim, int amount)
    {
        if (amount <= 0) return;
        if (source != null && source.card != null)
        {
            var rs = RowFor(source.card, source.ownerPlayerNumber);
            if (rs != null) rs.damage_dealt += amount;
        }
        if (victim != null && victim.card != null)
        {
            var rv = RowFor(victim.card, victim.ownerPlayerNumber);
            if (rv != null) rv.damage_taken += amount;
        }
    }

    public static void RecordKill(CardDisplay killer, CardDisplay victim)
    {
        if (killer != null && killer.card != null)
        {
            var rk = RowFor(killer.card, killer.ownerPlayerNumber);
            if (rk != null) rk.kills++;
        }
        if (victim != null && victim.card != null)
        {
            var rv = RowFor(victim.card, victim.ownerPlayerNumber);
            if (rv != null) rv.deaths++;
        }
    }

    public static void RecordHealing(CardDisplay source, int amount)
    {
        if (amount <= 0 || source == null || source.card == null) return;
        var r = RowFor(source.card, source.ownerPlayerNumber);
        if (r != null) r.healing_done += amount;
    }

    public static void RecordGold(CardDisplay source, int amount)
    {
        if (amount <= 0 || source == null || source.card == null) return;
        var r = RowFor(source.card, source.ownerPlayerNumber);
        if (r != null) r.gold_generated += amount;
    }

    public static void RecordDebuff(CardDisplay source)
    {
        if (source == null || source.card == null) return;
        var r = RowFor(source.card, source.ownerPlayerNumber);
        if (r != null) r.debuffs_applied++;
    }

    // ── Upload ───────────────────────────────────────────────────────────
    // Chamado no FIM da partida (só "finalizada", não abandonada). Sobe só do
    // master client e nunca em treino contra bot.
    public static void Upload(int winnerPlayer, string gameVersion, int seed, string map)
    {
        if (!active) return;
        active = false;

        if (BotMode.Enabled) { rows.Clear(); return; }
        if (!PhotonNetwork.isMasterClient) { rows.Clear(); return; }
        if (rows.Count == 0) return;

        // Preenche o contexto e o vencedor em cada linha
        foreach (var r in rows.Values)
        {
            r.game_version = gameVersion;
            r.map = map;
            r.match_seed = seed;
            r.won = (winnerPlayer != 0 && r.owner_player == winnerPlayer);
        }

        // Monta o array JSON (JsonUtility não serializa array na raiz)
        var sb = new StringBuilder();
        sb.Append('[');
        bool first = true;
        foreach (var r in rows.Values)
        {
            if (!first) sb.Append(',');
            sb.Append(JsonUtility.ToJson(r));
            first = false;
        }
        sb.Append(']');
        string body = sb.ToString();

        rows.Clear();
        Runner.Run(PostRoutine(body));
    }

    static IEnumerator PostRoutine(string body)
    {
        var req = new UnityWebRequest(SupabaseUrl + "/rest/v1/card_stats", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SupabaseKey);
        req.SetRequestHeader("Prefer", "return=minimal");
        req.timeout = 30;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("[MatchStatsTracker] Telemetria de cartas enviada.");
        else
            Debug.LogWarning($"[MatchStatsTracker] Falha ao enviar telemetria ({req.responseCode}): {req.downloadHandler.text}");
        req.Dispose();
    }

    // Roda coroutines sem depender de outro MonoBehaviour
    class Runner : MonoBehaviour
    {
        static Runner inst;
        public static void Run(IEnumerator co)
        {
            if (inst == null)
            {
                var go = new GameObject("MatchStatsRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                inst = go.AddComponent<Runner>();
            }
            inst.StartCoroutine(co);
        }
    }
}
