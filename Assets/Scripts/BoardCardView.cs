using UnityEngine;

// O que aparece EMBAIXO do personagem no tabuleiro
public enum BoardCardStyle
{
    Circulo = 0,   // NENHUM: só o ARO da base, o contorno limpo
    Carta = 1,     // a carta inteira fica sob a miniatura (sem base no chão:
                   // a borda colorida da própria carta já marca o dono)
    Simbolo = 2,   // o círculo PREENCHIDO na cor do dono (padrão)
}

// ════════════════════════════════════════════════════════════════════════════
//  Preferência VISUAL e LOCAL de cada jogador (Configurações da partida).
//
//  Não viaja pela rede e não encosta no lockstep: os dois jogadores podem
//  escolher estilos diferentes na mesma partida sem nenhum problema, porque
//  isto só decide o que é DESENHADO — nada de estado de jogo.
//
//  Fica salvo em PlayerPrefs, então a escolha vale para as próximas partidas.
// ════════════════════════════════════════════════════════════════════════════
public static class BoardCardView
{
    const string Key = "board_card_style";

    static BoardCardStyle? cache;

    public static BoardCardStyle Style
    {
        get
        {
            // Padrão: SÍMBOLO (escolha do Carlos)
            if (cache == null)
                cache = (BoardCardStyle)PlayerPrefs.GetInt(Key, (int)BoardCardStyle.Simbolo);
            return cache.Value;
        }
        set
        {
            if (cache != null && cache.Value == value) return;
            cache = value;
            PlayerPrefs.SetInt(Key, (int)value);
            PlayerPrefs.Save();
            AplicarEmTodas();
        }
    }

    public static string Rotulo(BoardCardStyle s)
    {
        return s == BoardCardStyle.Carta ? "Carta"
             : s == BoardCardStyle.Simbolo ? "Símbolo" : "Nenhum";
    }

    // Troca no meio da partida: as unidades que já estão em campo se
    // reajustam na hora, sem precisar rejogar nada
    static void AplicarEmTodas()
    {
        CardBoardPlaque[] todas = Object.FindObjectsByType<CardBoardPlaque>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CardBoardPlaque p in todas)
            if (p != null) p.RefreshStyle();
    }
}
