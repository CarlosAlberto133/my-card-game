using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

// Ferramentas de Editor para o CardPool.
// Menu: "Card Game" na barra de topo do Unity.
public static class CardPoolTools
{
    private const string CardsFolder = "Assets/cards";

    [MenuItem("Card Game/Preencher CardPool com todas as cartas")]
    public static void FillCardPool()
    {
        CardPool pool = Object.FindObjectOfType<CardPool>();
        if (pool == null)
        {
            EditorUtility.DisplayDialog("CardPool não encontrado",
                "Não há nenhum CardPool na cena aberta. Abre a cena do jogo e tenta de novo.", "Ok");
            return;
        }

        // Encontra todos os assets do tipo Card dentro de Assets/cards
        string[] guids = AssetDatabase.FindAssets("t:Card", new[] { CardsFolder });
        List<Card> cards = new List<Card>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Card card = AssetDatabase.LoadAssetAtPath<Card>(path);
            if (card != null) cards.Add(card);
        }

        // Organiza por classe, depois tier, depois nome — só para a lista ficar arrumada
        cards = cards
            .OrderBy(c => c.cardClass)
            .ThenBy(c => (int)c.tier)
            .ThenBy(c => c.name)
            .ToList();

        Undo.RecordObject(pool, "Preencher CardPool");
        pool.allBaseCards = cards;
        EditorUtility.SetDirty(pool);
        EditorSceneManager.MarkSceneDirty(pool.gameObject.scene);

        // Conta quantas estão realmente preenchidas (tier 1-5 e com nome)
        int validas = cards.Count(c => (int)c.tier >= 1 && (int)c.tier <= 5 && !string.IsNullOrWhiteSpace(c.cardName));
        int porPreencher = cards.Count - validas;

        Debug.Log($"[CardPoolTools] {cards.Count} cartas atribuídas ao CardPool " +
                  $"({validas} preenchidas, {porPreencher} ainda por preencher). " +
                  $"Grava a cena (Ctrl+S) para guardar.");

        EditorUtility.DisplayDialog("CardPool preenchido",
            $"{cards.Count} cartas atribuídas ao CardPool.\n" +
            $"- Preenchidas (aparecem na loja): {validas}\n" +
            $"- Ainda por preencher (ignoradas): {porPreencher}\n\n" +
            "Não te esqueças de gravar a cena (Ctrl+S).", "Ok");
    }
}
