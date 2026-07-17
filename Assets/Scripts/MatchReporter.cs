using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Sobe a partida para o Supabase, na CONTA DO JOGADOR logado pelo launcher.
//
// Salva em 4 momentos (na conta de quem está logado; sem login = pula):
//   1) FIM normal (tela de vitória)           -> status "finalizada"
//   2) Sair da partida (voltar ao lobby)       -> status "abandonada"
//   3) Oponente cair / eu perder a conexão      -> status "abandonada"
//   4) Fechar o jogo no meio                     -> grava LOG PENDENTE local e
//      sobe no PRÓXIMO boot (não dá tempo de upload ao fechar)
//
// Rede de segurança: qualquer upload que falhe por conexão vira log pendente e
// tenta de novo no próximo boot. 100% HTTP fora do lockstep do Photon.
public class MatchReporter : MonoBehaviour
{
    const string SupabaseUrl = "https://zutdbgltjphsbakeeoda.supabase.co";
    const string SupabaseKey = "sb_publishable_sIC5NDivItmQ_IuVOmWSdQ_LnyaSSOO";
    const int MaxLogChars = 200000;

    static MatchReporter instance;
    static bool matchActive = false;  // partida em andamento (elegível a salvar)
    static bool reported = false;     // já salvou esta partida (não duplicar)

    [Serializable]
    class SessionData
    {
        public string access_token, refresh_token, user_id, email, name;
        public long expires_at;
    }

    [Serializable]
    class RefreshResponse { public string access_token, refresh_token; public long expires_in; }

    [Serializable]
    class MatchRow
    {
        public string user_id;
        public int duration_seconds, winner_player, my_player, seed, rounds;
        public bool i_won;
        public string map, status;
        public string log_path; // caminho do .txt no Storage (bucket match-logs)
    }

    // Pendente gravado em disco: a linha + o texto do log (que ainda não subiu)
    [Serializable]
    class PendingMatch
    {
        public MatchRow row;
        public string log;
    }

    static string Dir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CardGame");
    static string SessionPath => Path.Combine(Dir, "session.json");
    static string PendingPath => Path.Combine(Dir, "pending_match.json");

    static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject go = new GameObject("MatchReporter");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MatchReporter>();
    }

    // ── Ciclo da partida ─────────────────────────────────────────────────

    // Início da partida (StartGame): habilita o salvamento e garante a instância
    // (pra o OnApplicationQuit ser capturado se fecharem no meio)
    public static void ResetForNewMatch()
    {
        reported = false;
        matchActive = true;
        EnsureInstance();
    }

    // Fim normal (tela de vitória) — nos DOIS clientes
    public static void ReportMatchEnd(int winnerPlayerNumber)
    {
        Report(winnerPlayerNumber, "finalizada");
    }

    // Sair da partida / desconexão no meio
    public static void ReportMatchAbandoned()
    {
        if (!matchActive || reported) return;
        Report(0, "abandonada");
    }

    static void Report(int winner, string status)
    {
        if (reported) return;
        reported = true;
        matchActive = false;

        // SEMPRE salva um .txt local da partida (logado ou não) — cópia fácil
        // de abrir/mandar, além do banco. Pasta: persistentDataPath/match-logs
        string txt = MatchLogRecorder.ExportToFile(null, "partida_" + status);
        if (txt != null) Debug.Log($"[MatchReporter] Log salvo em txt: {txt}");

        // Treino contra o bot: não sobe pro banco (não é partida de verdade;
        // só o .txt local acima é gerado)
        if (BotMode.Enabled) return;

        MatchRow row = BuildRow(winner, status);
        if (row == null) return; // não logado: fica só o txt local
        EnsureInstance();
        instance.StartCoroutine(instance.SendMatch(row, MatchLogRecorder.GetFullText(MaxLogChars), false));
    }

    // Fechar o jogo no meio da partida: não há tempo de upload → grava o log
    // PENDENTE (síncrono) e sobe no próximo boot. Chamar ANTES de desconectar/
    // fechar (o botão "Fechar o jogo"), e também no OnApplicationQuit (fechar pelo
    // X / Alt-F4). É idempotente (só grava 1x).
    public static void SaveAbandonedNow()
    {
        if (!matchActive || reported) return;
        reported = true;
        matchActive = false;

        // .txt local sempre (é síncrono, dá tempo mesmo fechando o jogo)
        MatchLogRecorder.ExportToFile(null, "partida_abandonada");

        // Treino contra o bot: não deixa upload pendente (não é partida de verdade)
        if (BotMode.Enabled) return;

        MatchRow row = BuildRow(0, "abandonada");
        if (row != null) WritePending(row, MatchLogRecorder.GetFullText(MaxLogChars));
    }

    void OnApplicationQuit() { SaveAbandonedNow(); }

    // ── Montagem da linha ────────────────────────────────────────────────

    static MatchRow BuildRow(int winner, string status)
    {
        SessionData s = LoadSession();
        if (s == null || string.IsNullOrEmpty(s.refresh_token)) return null; // não logado

        var row = new MatchRow();
        row.user_id = s.user_id;
        row.winner_player = winner;

        int myPlayer = winner;
        if (PhotonGameManager.Instance != null && PhotonGameManager.Instance.myPlayerNumber != 0)
            myPlayer = PhotonGameManager.Instance.myPlayerNumber;
        row.my_player = myPlayer;
        row.i_won = (winner != 0 && winner == myPlayer);

        row.rounds = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 0;
        row.duration_seconds = (TurnManager.Instance != null && TurnManager.Instance.matchStartRealtime > 0f)
            ? Mathf.Max(0, (int)(Time.realtimeSinceStartup - TurnManager.Instance.matchStartRealtime))
            : 0;
        row.seed = PhotonGameManager.Instance != null ? PhotonGameManager.Instance.currentGameSeed : 0;
        row.map = BoardThemeManager.Current == BoardTheme.Tabletop ? "mesa"
                : BoardThemeManager.Current == BoardTheme.Space ? "espaco" : "?";
        row.status = status;
        return row;
    }

    // ── Sessão: renovação ÚNICA e só quando precisa ──────────────────────
    // O refresh token do Supabase ROTACIONA a cada renovação (o antigo morre).
    // Renovar em fluxos concorrentes (upload pendente no boot + perfil do
    // lobby + envio de partida) fazia dois fluxos usarem/salvarem tokens fora
    // de ordem → o Supabase revogava a família inteira e o session.json
    // ficava órfão ("refresh_token_not_found" para sempre, até relogar).
    // Regras: (1) single-flight — só UMA renovação por vez, os demais esperam
    // e releem o session.json salvo; (2) access_token ainda válido (>60s) =
    // NÃO renova (zero rotação na maioria dos fluxos).
    static bool refreshingToken = false;
    static bool sessionInvalid = false; // refresh 400: só relogando no launcher

    IEnumerator EnsureFreshSession(Action<SessionData> onDone)
    {
        // Outra renovação em andamento: espera e usa o resultado gravado
        while (refreshingToken) yield return null;

        SessionData session = LoadSession();
        if (session == null || string.IsNullOrEmpty(session.refresh_token) || sessionInvalid)
        {
            if (onDone != null) onDone(null);
            yield break;
        }

        // Token atual ainda vale: usa direto, sem rotacionar nada
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!string.IsNullOrEmpty(session.access_token) && session.expires_at - now > 60)
        {
            if (onDone != null) onDone(session);
            yield break;
        }

        refreshingToken = true;
        string refreshBody = "{\"refresh_token\":\"" + session.refresh_token + "\"}";
        using (UnityWebRequest req = MakeJsonPost(
            SupabaseUrl + "/auth/v1/token?grant_type=refresh_token", refreshBody, null))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                // 400 = token revogado/rotacionado fora daqui — insistir não
                // resolve e ainda pioraria; o jogador precisa relogar
                if (req.responseCode == 400) sessionInvalid = true;
                Debug.LogWarning($"[MatchReporter] Falha ao renovar sessão ({req.responseCode}): {req.downloadHandler.text}");
                refreshingToken = false;
                if (onDone != null) onDone(null);
                yield break;
            }

            RefreshResponse fresh = JsonUtility.FromJson<RefreshResponse>(req.downloadHandler.text);
            if (fresh == null || string.IsNullOrEmpty(fresh.access_token))
            {
                refreshingToken = false;
                if (onDone != null) onDone(null);
                yield break;
            }

            session.access_token = fresh.access_token;
            if (!string.IsNullOrEmpty(fresh.refresh_token)) session.refresh_token = fresh.refresh_token;
            session.expires_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + fresh.expires_in;
            SaveSession(session); // grava ANTES de liberar o lock (quem espera relê)
        }
        refreshingToken = false;
        if (onDone != null) onDone(session);
    }

    // ── Upload: refresh do token → .txt do log no STORAGE → linha na tabela ──
    // O log NÃO vai mais como texto na tabela: sobe como ARQUIVO .txt no bucket
    // "match-logs" (Storage) e a tabela guarda só o caminho (log_path).

    IEnumerator SendMatch(MatchRow row, string logText, bool isPending)
    {
        SessionData pre = LoadSession();
        if (pre == null || string.IsNullOrEmpty(pre.refresh_token))
            yield break; // não logado (pendente: mantém pra quando logar)

        // Pendente de OUTRA conta que não a logada agora: descarta
        if (isPending && pre.user_id != row.user_id) { DeletePending(); yield break; }

        // 1) Garante sessão válida (renovação única/preguiçosa)
        SessionData session = null;
        yield return EnsureFreshSession(s => session = s);
        if (session == null)
        {
            if (!isPending) WritePending(row, logText); // sobe quando relogar/no próximo boot
            yield break;
        }

        row.user_id = session.user_id; // garante que bate com o token (RLS)

        // 2) Sobe o log como ARQUIVO .txt no Storage (pasta do usuário)
        if (!string.IsNullOrEmpty(logText) && string.IsNullOrEmpty(row.log_path))
        {
            string path = session.user_id + "/partida_" + row.status + "_" +
                          DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
            var up = new UnityWebRequest(SupabaseUrl + "/storage/v1/object/match-logs/" + path, "POST");
            up.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(logText));
            up.downloadHandler = new DownloadHandlerBuffer();
            up.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");
            up.SetRequestHeader("apikey", SupabaseKey);
            up.SetRequestHeader("Authorization", "Bearer " + session.access_token);
            up.timeout = 60;
            yield return up.SendWebRequest();

            if (up.result == UnityWebRequest.Result.Success)
            {
                row.log_path = path;
            }
            else if (up.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning("[MatchReporter] Sem conexão no upload do log — pendente pra depois.");
                if (!isPending) WritePending(row, logText);
                up.Dispose();
                yield break;
            }
            else
            {
                // Servidor rejeitou o arquivo (bucket/policy?): registra a partida
                // mesmo assim, só sem o log anexado
                Debug.LogWarning($"[MatchReporter] Storage rejeitou o log ({up.responseCode}): {up.downloadHandler.text}");
            }
            up.Dispose();
        }

        // 3) Grava a linha da partida (leve — sem o texto do log)
        string json = JsonUtility.ToJson(row);
        using (UnityWebRequest req = MakeJsonPost(SupabaseUrl + "/rest/v1/matches", json, session.access_token))
        {
            req.SetRequestHeader("Prefer", "return=minimal");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[MatchReporter] Partida enviada ({row.status}, {row.duration_seconds}s, {row.rounds} rounds, log={row.log_path}).");
                if (isPending) DeletePending();
            }
            else if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning("[MatchReporter] Sem conexão — guardando pendente pra tentar depois.");
                if (!isPending) WritePending(row, logText); // log_path já setado: não re-sobe o arquivo
            }
            else
            {
                // Erro do servidor (dados/RLS) — insistir não resolve
                Debug.LogWarning($"[MatchReporter] Servidor rejeitou ({req.responseCode}): {req.downloadHandler.text}");
                if (isPending) DeletePending();
            }
        }
    }

    // ── Pendentes (fechar no meio / falha de rede) ───────────────────────

    static void WritePending(MatchRow row, string logText)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var pm = new PendingMatch { row = row, log = logText };
            File.WriteAllText(PendingPath, JsonUtility.ToJson(pm));
            Debug.Log("[MatchReporter] Partida pendente gravada — sobe no próximo boot.");
        }
        catch (Exception e) { Debug.LogWarning($"[MatchReporter] Não gravou pendente: {e.Message}"); }
    }

    static void DeletePending()
    {
        try { if (File.Exists(PendingPath)) File.Delete(PendingPath); } catch { }
    }

    // No boot do jogo: se houver log pendente, tenta subir
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void UploadPendingOnBoot()
    {
        if (!File.Exists(PendingPath)) return;
        EnsureInstance();
        instance.StartCoroutine(instance.UploadPendingRoutine());
    }

    IEnumerator UploadPendingRoutine()
    {
        PendingMatch pm = null;
        try { pm = JsonUtility.FromJson<PendingMatch>(File.ReadAllText(PendingPath)); }
        catch { }
        if (pm == null || pm.row == null || string.IsNullOrEmpty(pm.row.user_id)) { DeletePending(); yield break; }
        yield return SendMatch(pm.row, pm.log, true);
    }

    // ── Estatísticas do jogador (painel de perfil do lobby) ──────────────

    [Serializable]
    public class PlayerStats
    {
        public bool loggedIn;       // Há sessão do launcher (session.json)
        public bool sessionExpired; // Sessão órfã (refresh 400) — relogar no launcher
        public bool statsLoaded;    // A busca no Supabase deu certo
        public string playerName;
        public int total, wins, losses, abandoned, totalSeconds;
    }

    [Serializable] class StatsItem { public bool i_won; public int duration_seconds; public string status; }
    [Serializable] class StatsList { public StatsItem[] items; }

    // Busca as partidas do jogador logado e agrega (total/vitórias/derrotas/
    // tempo). O callback SEMPRE é chamado — sem login ou sem rede, vem com as
    // flags desligadas e o painel mostra o estado adequado.
    public static void FetchStats(Action<PlayerStats> onDone)
    {
        EnsureInstance();
        instance.StartCoroutine(instance.FetchStatsRoutine(onDone));
    }

    IEnumerator FetchStatsRoutine(Action<PlayerStats> onDone)
    {
        var stats = new PlayerStats();

        SessionData pre = LoadSession();
        if (pre == null || string.IsNullOrEmpty(pre.refresh_token))
        {
            if (onDone != null) onDone(stats); // não logado
            yield break;
        }

        stats.loggedIn = true;
        stats.playerName = !string.IsNullOrEmpty(pre.name) ? pre.name : pre.email;

        // Sessão válida via caminho ÚNICO (single-flight + renovação preguiçosa
        // — antes o perfil e o upload pendente do boot renovavam AO MESMO TEMPO
        // e a rotação dupla revogava a família de tokens no Supabase)
        SessionData session = null;
        yield return EnsureFreshSession(s => session = s);
        if (session == null)
        {
            stats.sessionExpired = sessionInvalid;
            if (onDone != null) onDone(stats); // mostra ao menos o nome
            yield break;
        }

        // Busca só as colunas necessárias das partidas do próprio usuário (RLS)
        string url = SupabaseUrl + "/rest/v1/matches?user_id=eq." + session.user_id +
                     "&select=i_won,duration_seconds,status";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("apikey", SupabaseKey);
            req.SetRequestHeader("Authorization", "Bearer " + session.access_token);
            req.timeout = 20;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                // JsonUtility não lê array na raiz: embrulha em {"items": [...]}
                StatsList list = null;
                try { list = JsonUtility.FromJson<StatsList>("{\"items\":" + req.downloadHandler.text + "}"); }
                catch (Exception e) { Debug.LogWarning($"[MatchReporter] Perfil: JSON inesperado: {e.Message}"); }

                if (list != null && list.items != null)
                {
                    foreach (StatsItem m in list.items)
                    {
                        stats.total++;
                        stats.totalSeconds += Mathf.Max(0, m.duration_seconds);
                        if (m.i_won) stats.wins++;
                        else if (m.status == "finalizada") stats.losses++;
                        else stats.abandoned++;
                    }
                    stats.statsLoaded = true;
                }
            }
            else
            {
                Debug.LogWarning($"[MatchReporter] Perfil: falha ao buscar partidas ({req.responseCode}): {req.downloadHandler.text}");
            }
        }

        if (onDone != null) onDone(stats);
    }

    // ── HTTP / sessão ────────────────────────────────────────────────────

    static UnityWebRequest MakeJsonPost(string url, string json, string bearer)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", SupabaseKey);
        if (!string.IsNullOrEmpty(bearer)) req.SetRequestHeader("Authorization", "Bearer " + bearer);
        req.timeout = 30;
        return req;
    }

    static SessionData LoadSession()
    {
        try
        {
            if (!File.Exists(SessionPath)) return null;
            return JsonUtility.FromJson<SessionData>(File.ReadAllText(SessionPath));
        }
        catch (Exception e) { Debug.LogWarning($"[MatchReporter] session.json ilegível: {e.Message}"); return null; }
    }

    static void SaveSession(SessionData s)
    {
        try { Directory.CreateDirectory(Dir); File.WriteAllText(SessionPath, JsonUtility.ToJson(s)); }
        catch (Exception e) { Debug.LogWarning($"[MatchReporter] Não salvou session.json: {e.Message}"); }
    }
}
