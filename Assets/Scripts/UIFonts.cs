using UnityEngine;
using TMPro;

// Fontes do tema medieval (mockup desing-jogo.png): Cinzel Decorative para
// títulos ("FASE DE COMPRA") e Cinzel para nomes/botões. Os .ttf ficam em
// Resources/ui e viram TMP_FontAsset DINÂMICO em runtime — sem precisar do
// Font Asset Creator no editor. Se algo falhar (fonte ausente, API), os
// getters devolvem null e quem usa mantém a LiberationSans padrão.
public static class UIFonts
{
    static TMP_FontAsset title, body;
    static bool tried;

    public static TMP_FontAsset Title { get { Load(); return title; } }
    public static TMP_FontAsset Body { get { Load(); return body; } }

    static void Load()
    {
        if (tried) return;
        tried = true;
        try
        {
            Font t = Resources.Load<Font>("ui/CinzelTitle");
            if (t != null) title = TMP_FontAsset.CreateFontAsset(t);
            Font b = Resources.Load<Font>("ui/Cinzel");
            if (b != null) body = TMP_FontAsset.CreateFontAsset(b);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[UIFonts] Fonte do tema indisponível: " + e.Message);
        }
    }

    // Troca a fonte só se ela carregou — nunca deixa o texto sem fonte
    public static void Set(TMP_Text tmp, TMP_FontAsset f)
    {
        if (tmp != null && f != null) tmp.font = f;
    }
}
