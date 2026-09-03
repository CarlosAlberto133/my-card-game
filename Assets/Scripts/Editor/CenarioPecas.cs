using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Larga na cena uma PEÇA PRÓPRIA de cenário (as do Meshy, em
// Resources/decor/cenario) para ser posicionada à MÃO.
//
// Diferente do resto do cenário: estas peças não são montadas por código em
// lugar nenhum. O menu só as coloca na cena — ao lado do tabuleiro, com o
// material certo (textura + normal map) e num tamanho de partida. Dali em
// diante é gizmo: mover, girar, escalar, duplicar. Salve a cena (Ctrl+S).
//
// COMO USAR:
//   Card Game → Cenário: adicionar peça → (escolha a peça)
//
// Elas entram DENTRO do MesaStage quando ele existe, para acender e apagar
// junto com o resto do cenário. Sem MesaStage, ficam soltas na cena.
//
// ⚠️ Re-assar o cenário do zero (o menu "assar cenário na cena") APAGA o
// MesaStage inteiro — e com ele estas peças. Use o menu do tampo para
// mudanças pontuais, como já fazemos.
// ════════════════════════════════════════════════════════════════════════════
public static class CenarioPecas
{
    // Alturas de partida na régua do tabuleiro: uma casa tem 6 de lado.
    // São chutes bons, não medidas — a ideia é aparecer num tamanho que dá
    // para ver e julgar, e ser escalado à mão em seguida.

    [MenuItem("Card Game/Cenário: adicionar peça/Tenda real (pavilhão)")]
    public static void AddTenda() { Adicionar("tenda", "Tenda", 34f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Baú do tesouro")]
    public static void AddBau() { Adicionar("bau", "Bau", 6f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Jardineira de pedra")]
    public static void AddJardineira() { Adicionar("jardineira", "Jardineira", 8f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Estandarte dourado")]
    public static void AddEstandarte() { Adicionar("estandarte", "Estandarte", 22f); }

    static void Adicionar(string slug, string rotulo, float altura)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Peça de cenário",
                "Saia do Play mode antes — o que é criado em Play se perde ao parar.",
                "OK");
            return;
        }

        BoardManager bm = Object.FindObjectOfType<BoardManager>();
        Vector3 center = bm != null ? bm.transform.position : Vector3.zero;

        // Ponto de partida: em cima da mesa, à direita do tabuleiro — longe da
        // loja (x −29.5) e das mãos (z ±29.5), onde dá para ver e arrastar
        Vector3 pos = center + new Vector3(42f, TabletopEnvironment.TableTopSurface, 0f);

        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        GameObject go = DecorProps.PlaceSceneryProp(pai, slug, pos, altura, 0f);
        if (go == null)
        {
            EditorUtility.DisplayDialog("Peça de cenário",
                "Não achei Resources/decor/cenario/" + slug + " — veja o Console.",
                "OK");
            return;
        }
        go.name = rotulo;

        Undo.RegisterCreatedObjectUndo(go, "Adicionar " + rotulo);

        // O material é criado em memória pelo código; sem salvar como asset a
        // cena guardaria uma referência a nada e a peça abriria rosa "missing"
        MesaStageBaker.PersistRuntimeAssets(go);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        EditorUtility.DisplayDialog("Peça de cenário",
            rotulo + " entrou na cena" +
            (stage != null ? ", dentro do MesaStage" : " (não há MesaStage: ficou solta)") +
            ", à direita do tabuleiro — já enquadrada na Scene view.\n\n" +
            "Posicione com o gizmo e salve a cena (Ctrl+S). Se vier tombada, é " +
            "um giro no Inspector; o tamanho também é só ponto de partida.",
            "OK");
    }
}
