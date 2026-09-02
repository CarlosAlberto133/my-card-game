using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// "ASSA" o cenário da Mesa de RPG (gerado por código no TabletopEnvironment)
// para DENTRO da cena, como objetos de verdade editáveis no editor.
//
// COMO USAR:
// 1. Abra a SampleScene (fora do Play mode) e use o menu:
//    Card Game → Mesa de RPG: assar cenário na cena (MesaStage)
// 2. Aparece o objeto "MesaStage" na Hierarchy com TUDO dentro — mesa, pernas,
//    chão da taverna, moldura, árvores, tochas, dados, livros, estandartes,
//    heróis, vagalumes. Edite à vontade: mova, gire, apague, duplique,
//    acrescente peças novas de qualquer pack.
// 3. Salve a cena (Ctrl+S). Pronto: o jogo passa a usar o MesaStage da cena
//    (o código só o liga/desliga e pinta o fundo da câmera).
//
// Voltar ao cenário por código = apagar o MesaStage da cena.
// Gerar de novo do zero = rodar o menu de novo (avisa antes de apagar).
//
// Materiais/texturas que o código cria em memória (a madeira da mesa, os
// vagalumes...) são salvos como assets em Assets/BakedMaps/MesaStage — sem
// isso a cena salvaria referências a objetos que não existem em disco e tudo
// viraria rosa "missing" ao reabrir o projeto.
// ════════════════════════════════════════════════════════════════════════════
public static class MesaStageBaker
{
    const string RootFolder = "Assets/BakedMaps";
    const string BakeFolder = "Assets/BakedMaps/MesaStage";

    // Seed fixa: o jitter das árvores/dados congela do jeito que sair aqui —
    // daí em diante a variação é edição manual mesmo
    const int BakeSeed = 12345;

    [MenuItem("Card Game/Mesa de RPG: assar cenário na cena (MesaStage)")]
    public static void Bake()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Assar MesaStage",
                "Saia do Play mode antes de assar o cenário — o que é criado " +
                "em Play se perde ao parar.", "OK");
            return;
        }

        GameObject old = FindStage();
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Assar MesaStage",
                "Já existe um MesaStage na cena. Gerar de novo APAGA o atual — " +
                "inclusive tudo o que você editou à mão nele. Continuar?",
                "Apagar e gerar de novo", "Cancelar"))
                return;
            Object.DestroyImmediate(old);
        }

        // Pasta dos assets do bake sempre fresca, e os caches zerados (senão
        // apontariam para os assets recém-apagados)
        if (AssetDatabase.IsValidFolder(BakeFolder))
            AssetDatabase.DeleteAsset(BakeFolder);
        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "BakedMaps");
        AssetDatabase.CreateFolder(RootFolder, "MesaStage");
        DecorProps.ResetCachesForBake();
        TabletopEnvironment.ResetCachesForBake();

        TabletopEnvironment.Build(BakeSeed);
        GameObject stage = TabletopEnvironment.TakeRootForBake();
        if (stage == null)
        {
            EditorUtility.DisplayDialog("Assar MesaStage",
                "O TabletopEnvironment não gerou nada — veja o Console.", "OK");
            return;
        }
        stage.name = TabletopEnvironment.BakedStageName;

        int salvos = PersistRuntimeAssets(stage);

        EditorSceneManager.MarkSceneDirty(stage.scene);
        Selection.activeGameObject = stage;
        EditorGUIUtility.PingObject(stage);
        EditorUtility.DisplayDialog("Assar MesaStage",
            "MesaStage criado na cena (" + salvos + " materiais salvos em " +
            BakeFolder + ").\n\nEdite à vontade e salve a cena (Ctrl+S). " +
            "O jogo agora usa o que estiver dentro dele; para voltar ao " +
            "cenário por código, apague o MesaStage.", "OK");
    }

    // Troca SÓ o tampo dentro de um MesaStage já assado: as tábuas novas
    // entram e o resto do cenário (moldura, tochas, dados, tudo o que foi
    // movido à mão) fica exatamente como está. Re-assar tudo apagaria isso.
    [MenuItem("Card Game/Mesa de RPG: refazer só o TAMPO com as tábuas")]
    public static void RebakeTableTop()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Tampo em tábuas",
                "Saia do Play mode antes — o que é criado em Play se perde ao parar.",
                "OK");
            return;
        }

        GameObject stage = FindStage();
        if (stage == null)
        {
            EditorUtility.DisplayDialog("Tampo em tábuas",
                "Não há MesaStage nesta cena. Sem ele o cenário é montado por " +
                "código e o tampo já sai em tábuas sozinho — não precisa deste menu.",
                "OK");
            return;
        }

        BoardManager bm = Object.FindObjectOfType<BoardManager>();
        Vector3 center = bm != null ? bm.transform.position : Vector3.zero;

        // Fora o tampo velho: o "Tampo" (tábuas) de uma rodada anterior OU a
        // laje "TableTop" do bake original
        int apagados = 0;
        foreach (Transform t in stage.GetComponentsInChildren<Transform>(true))
        {
            if (t == null || t == stage.transform) continue;
            if (t.name == "Tampo" || t.name == "TableTop" || t.name == "TableBody")
            {
                Object.DestroyImmediate(t.gameObject);
                apagados++;
            }
        }

        if (!AssetDatabase.IsValidFolder(RootFolder))
            AssetDatabase.CreateFolder("Assets", "BakedMaps");
        if (!AssetDatabase.IsValidFolder(BakeFolder))
            AssetDatabase.CreateFolder(RootFolder, "MesaStage");

        // Sem ResetCachesForBake aqui, de propósito: a pasta do bake continua
        // no lugar, então o cache que aponta para os assets já salvos é o que
        // evita salvar uma segunda cópia da mesma madeira.
        GameObject tampo = TabletopEnvironment.BuildTableTopForBake(center);
        if (tampo == null)
        {
            EditorUtility.DisplayDialog("Tampo em tábuas",
                "Não consegui montar o tampo — veja o Console.", "OK");
            return;
        }

        tampo.transform.SetParent(stage.transform, true);
        tampo.transform.SetAsFirstSibling();

        int salvos = PersistRuntimeAssets(tampo);

        EditorSceneManager.MarkSceneDirty(stage.scene);
        Selection.activeGameObject = tampo;
        EditorGUIUtility.PingObject(tampo);
        EditorUtility.DisplayDialog("Tampo em tábuas",
            "Tampo refeito dentro do MesaStage (" + apagados + " peça(s) antiga(s) " +
            "apagada(s), " + salvos + " material(is) salvo(s)).\n\nO resto do " +
            "cenário ficou intocado. Salve a cena (Ctrl+S).", "OK");
    }

    static GameObject FindStage()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name == TabletopEnvironment.BakedStageName)
                return t.gameObject;
        return null;
    }

    // Salva como asset todo material/textura criado em memória pelo código,
    // deduplicando os idênticos (os 12 vagalumes dividem 1 material; cada
    // caixa de madeira tem o seu por causa de cor/tiling)
    static int PersistRuntimeAssets(GameObject stage)
    {
        Dictionary<string, Material> canonicos = new Dictionary<string, Material>();
        int salvos = 0;

        foreach (Renderer r in stage.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            bool trocou = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                // Persistente = já é um arquivo (atlas do KayKit, etc.) — pula
                if (m == null || EditorUtility.IsPersistent(m)) continue;

                string chave = KeyOf(m);
                Material canon;
                if (canonicos.TryGetValue(chave, out canon))
                {
                    mats[i] = canon; // idêntico a um já salvo → reusa o asset
                    trocou = true;
                    continue;
                }

                SaveTextures(m); // textura procedural (madeira) primeiro

                string nome = "mat-" + Sanitize(r.gameObject.name) + "-" + salvos;
                m.name = nome;
                AssetDatabase.CreateAsset(m, BakeFolder + "/" + nome + ".mat");
                canonicos[chave] = m;
                salvos++;
            }
            if (trocou) r.sharedMaterials = mats;
        }

        AssetDatabase.SaveAssets();
        return salvos;
    }

    // Assinatura de igualdade de um material gerado: mesmo shader + cor +
    // textura + tiling = mesmo material (uma instância só no disco)
    static string KeyOf(Material m)
    {
        Texture t = m.mainTexture;
        return m.shader.name + "|" + ColorUtility.ToHtmlStringRGBA(m.color) + "|"
             + (t != null ? t.GetInstanceID() : 0) + "|" + m.mainTextureScale;
    }

    static void SaveTextures(Material m)
    {
        // Cor e normal map; as texturas dos packs (KayKit/Meshy) já são
        // arquivos e ficam de fora. CreateAsset torna o objeto persistente —
        // os próximos materiais que usarem a mesma textura já a veem salva.
        Texture[] texs =
        {
            m.mainTexture,
            m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null,
        };
        foreach (Texture tex in texs)
        {
            if (tex == null || EditorUtility.IsPersistent(tex)) continue;
            string nome = "tex-" + Sanitize(tex.name == "" ? "gerada" : tex.name)
                        + "-" + Mathf.Abs(tex.GetInstanceID());
            AssetDatabase.CreateAsset(tex, BakeFolder + "/" + nome + ".asset");
        }
    }

    static string Sanitize(string s)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char c in s)
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        return sb.ToString();
    }
}
