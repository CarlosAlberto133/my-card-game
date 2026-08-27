using UnityEngine;

// Nick do jogador mostrado no perfil do lobby e no HUD da partida.
// Prioridade: nick custom (editado no lápis do perfil) → nome da conta
// Google (cacheado pelo MatchReporter a cada FetchStats) → "Jogador".
// O nick viaja pelo Photon (PhotonNetwork.playerName) para o oponente ver.
public static class PlayerNick
{
    const string CustomKey = "player_nick";
    const string AccountKey = "account_name";

    public const int MaxLength = 20;       // limite ao salvar o nick custom
    public const int MatchDisplayMax = 14; // truncagem no HUD da partida

    public static bool HasCustom =>
        !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(CustomKey, ""));

    public static string Get()
    {
        string custom = PlayerPrefs.GetString(CustomKey, "");
        if (!string.IsNullOrWhiteSpace(custom)) return custom.Trim();
        string account = PlayerPrefs.GetString(AccountKey, "");
        if (!string.IsNullOrWhiteSpace(account)) return account.Trim();
        return "Jogador";
    }

    // Salva o nick custom; vazio = apagar (volta a valer o nome da conta)
    public static void SetCustom(string nick)
    {
        nick = (nick ?? "").Trim();
        if (nick.Length > MaxLength) nick = nick.Substring(0, MaxLength).TrimEnd();
        if (nick.Length == 0) PlayerPrefs.DeleteKey(CustomKey);
        else PlayerPrefs.SetString(CustomKey, nick);
        PlayerPrefs.Save();
    }

    // Guarda o nome da conta Google visto no último FetchStats — vira o
    // fallback do nick mesmo sem abrir o perfil de novo
    public static void CacheAccountName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        PlayerPrefs.SetString(AccountKey, name.Trim());
        PlayerPrefs.Save();
    }

    // "Carlos Alberto Claudio de Lima" → "Carlos Alberto…" (nome longo não
    // estoura o HUD da partida)
    public static string Truncate(string name, int max)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Trim();
        if (name.Length <= max) return name;
        return name.Substring(0, max).TrimEnd() + "…";
    }
}
