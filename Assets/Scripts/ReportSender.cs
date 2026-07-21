using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Envia um REPORT do jogador (bug, hacker ou feedback) para a tabela `reports`
// do Supabase, com o máximo de contexto para o Carlos investigar: quem enviou,
// dados da partida (seed/mapa/jogador), o ROUND atual, o log completo e o
// trecho do log daquele round. 100% HTTP fora do lockstep do Photon.
//
// Inserção com a chave publishable (anônima) — a tabela `reports` deve ter uma
// policy de INSERT liberada (with check true) e SELECT só para o admin.
public class ReportSender : MonoBehaviour
{
    static ReportSender instance;

    static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("ReportSender");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ReportSender>();
    }

    // Envia o report. onDone(true) em sucesso; onDone(false) em falha.
    public static void Submit(string type, string description, Action<bool> onDone)
    {
        Ensure();
        instance.StartCoroutine(instance.SubmitRoutine(type, description, onDone));
    }

    [Serializable]
    class ReportRow
    {
        public string type;
        public string description;
        public string user_id;      // null se anônimo
        public string player_name;
        public string player_email;
        public int match_seed;
        public int my_player;
        public string map;
        public int round;
        public string game_version;
        public string full_log;
        public string round_log;
    }

    IEnumerator SubmitRoutine(string type, string description, Action<bool> onDone)
    {
        var row = new ReportRow();
        row.type = string.IsNullOrEmpty(type) ? "bug" : type;
        row.description = description ?? "";

        // Quem reportou (se logado pelo launcher)
        string uid, email, name;
        if (MatchReporter.TryGetPlayerIdentity(out uid, out email, out name))
        {
            row.user_id = uid;
            row.player_email = email;
            row.player_name = name;
        }

        // Contexto da partida (0/vazio se reportou do lobby)
        row.round = TurnManager.Instance != null ? TurnManager.Instance.currentRound : 0;
        row.my_player = (PhotonGameManager.Instance != null) ? PhotonGameManager.Instance.myPlayerNumber : 0;
        row.match_seed = (PhotonGameManager.Instance != null) ? PhotonGameManager.Instance.currentGameSeed : 0;
        row.map = BoardThemeManager.Current == BoardTheme.Tabletop ? "mesa"
                : BoardThemeManager.Current == BoardTheme.Forest ? "floresta"
                : BoardThemeManager.Current == BoardTheme.Space ? "espaco"
                : BoardThemeManager.Current == BoardTheme.Teste ? "teste" : null;
        row.game_version = Application.version;

        // Logs: completo (limitado) + trecho do round reportado
        row.full_log = MatchLogRecorder.GetFullText(200000);
        row.round_log = MatchLogRecorder.GetRoundSegment(row.round);

        string json = JsonUtility.ToJson(row);

        string url = MatchReporter.SupabaseEndpoint + "/rest/v1/reports";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", MatchReporter.SupabaseAnonKey);
            req.SetRequestHeader("Authorization", "Bearer " + MatchReporter.SupabaseAnonKey);
            req.SetRequestHeader("Prefer", "return=minimal");
            req.timeout = 30;

            yield return req.SendWebRequest();

            bool ok = req.result == UnityWebRequest.Result.Success;
            if (ok)
                Debug.Log($"[ReportSender] Report '{row.type}' enviado (round {row.round}).");
            else
                Debug.LogWarning($"[ReportSender] Falha ao enviar report ({req.responseCode}): {req.downloadHandler.text}");

            if (onDone != null) onDone(ok);
        }
    }
}
