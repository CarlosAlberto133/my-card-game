using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Grava TODO o histórico da partida (cada Debug.Log/Warning/Error/Exception do
// jogo) em memória, desde a abertura do app, para poder exportar em arquivo e
// investigar bugs depois. 100% local (só leitura), nenhum impacto na sincronização.
public static class MatchLogRecorder
{
    // Limite generoso: uma partida longa gera alguns milhares de linhas.
    // Acima disso descartamos as mais antigas para não crescer sem fim.
    private const int MaxEntries = 40000;

    private static readonly List<string> entries = new List<string>(4096);
    private static bool hooked = false;
    private static float startupRealtime = 0f;

    // Roda antes de qualquer cena carregar — captura inclusive os logs do lobby
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        if (hooked) return;
        hooked = true;
        startupRealtime = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLogMessage;
        entries.Add($"[00:00.0] === Log iniciado em {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} — versão {Application.version} ===");
    }

    private static void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (entries.Count >= MaxEntries)
            entries.RemoveRange(0, 4096); // abre espaço em bloco (barato, raro)

        float t = Time.realtimeSinceStartup - startupRealtime;
        string stamp = $"[{(int)(t / 60f):00}:{t % 60f:00.0}]";

        string prefix = "";
        if (type == LogType.Warning) prefix = "[AVISO] ";
        else if (type == LogType.Error) prefix = "[ERRO] ";
        else if (type == LogType.Exception) prefix = "[EXCEPTION] ";
        else if (type == LogType.Assert) prefix = "[ASSERT] ";

        entries.Add($"{stamp} {prefix}{condition}");

        // Stack trace só para erros/exceptions — é o que interessa para achar
        // onde o jogo quebrou
        if ((type == LogType.Exception || type == LogType.Error) &&
            !string.IsNullOrEmpty(stackTrace))
        {
            entries.Add("    " + stackTrace.TrimEnd().Replace("\n", "\n    "));
        }
    }

    // Marca eventos importantes da partida direto no histórico (turnos, ações...)
    public static void Note(string message)
    {
        if (!hooked) Hook();
        OnLogMessage(message, null, LogType.Log);
    }

    // Devolve o histórico completo como texto (para upload ao banco no fim da
    // partida). Se passar do limite, mantém o FIM (as linhas mais recentes —
    // que são as da partida que acabou de terminar).
    public static string GetFullText(int maxChars = int.MaxValue)
    {
        var sb = new System.Text.StringBuilder(entries.Count * 64);
        foreach (string line in entries)
            sb.AppendLine(line);
        if (sb.Length > maxChars)
            sb.Remove(0, sb.Length - maxChars);
        return sb.ToString();
    }

    // Salva tudo em um .txt e devolve o caminho do arquivo (null se falhar).
    // extraReport: texto adicional anexado ao fim (ex.: raio-X do estado atual).
    // filePrefix: nome-base do arquivo (ex.: "partida_finalizada").
    public static string ExportToFile(string extraReport = null, string filePrefix = "partida")
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "match-logs");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir,
                $"{filePrefix}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");

            var sb = new System.Text.StringBuilder(entries.Count * 64);
            sb.AppendLine("===== HISTÓRICO COMPLETO DA PARTIDA =====");
            sb.AppendLine($"Exportado em: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Linhas: {entries.Count}");
            sb.AppendLine();
            foreach (string line in entries)
                sb.AppendLine(line);

            if (!string.IsNullOrEmpty(extraReport))
            {
                sb.AppendLine();
                sb.AppendLine("===== ESTADO ATUAL (RAIO-X) =====");
                // Remove as tags de cor do TextMeshPro para o .txt ficar legível
                sb.AppendLine(System.Text.RegularExpressions.Regex.Replace(
                    extraReport, "<.*?>", ""));
            }

            File.WriteAllText(file, sb.ToString());
            return file;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MatchLog] Falha ao exportar logs: {e.Message}");
            return null;
        }
    }

    // Abre a pasta do arquivo no Explorer com o arquivo selecionado
    public static void RevealInExplorer(string filePath)
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe",
                "/select,\"" + filePath.Replace('/', '\\') + "\"");
        }
        catch
        {
            Application.OpenURL("file://" + Path.GetDirectoryName(filePath));
        }
    }
}
