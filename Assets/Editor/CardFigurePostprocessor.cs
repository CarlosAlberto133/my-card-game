using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Otimização automática dos modelos 3D das cartas.
//
// Todo FBX/PNG que cair em Assets/Resources/cards/figures/ é importado já
// enxuto, sem ninguém precisar lembrar de configurar nada no Inspector.
//
// Por que isso existe: os modelos gerados por IA vêm com QUATRO texturas
// 4096x4096 embutidas (~25 MB por personagem, 95% do arquivo). Multiplicado
// pelas 80 cartas daria ~1,8 GB de textura no build — para bonecos que
// aparecem com menos de 200 pixels de altura na tela. A malha em si é leve
// (6k-10k triângulos), então o corte é todo no lado da textura:
//
//   - materialImportMode = None  -> as 4 texturas embutidas NÃO são importadas
//                                   e não entram no build. A cor vem do
//                                   <nome-da-carta>_tex.png ao lado, que o
//                                   CardDisplay aplica num material URP.
//   - maxTextureSize = 512       -> 64x menos pixels que 4096. Sobra resolução
//                                   de sobra para o tamanho que aparece.
//   - crunchedCompression        -> encolhe ainda mais o arquivo no disco.
//   - sem animação/rig           -> estes modelos são estáticos; quem anima é
//                                   o FigureAnimator procedural.
//
// Se um dia chegar um modelo COM rig de verdade, tire-o desta pasta ou
// acrescente o nome dele em ModelosComRig abaixo.
// ════════════════════════════════════════════════════════════════════════════
public class CardFigurePostprocessor : AssetPostprocessor
{
    const string Pasta = "/resources/cards/figures/";
    const int MaxTextura = 512;

    // Exceções: modelos que precisam manter o esqueleto e as animações
    static readonly string[] ModelosComRig = { };

    // ── Modelos que entram deitados ────────────────────────────────────────
    // Quem exporta do Blender sem converter o eixo manda o modelo em Z-up e o
    // Unity (Y-up) mostra o personagem caído — já no preview do Inspector.
    //
    // O sentido do giro NÃO é chutado: o Unity ainda inverte a mão do sistema
    // de coordenadas ao importar FBX, então medir o arquivo cru dá o sinal
    // trocado. Aqui a malha JÁ importada é medida e o código escolhe entre as
    // orientações possíveis a que deixa o personagem de pé, usando uma regra
    // que vale para qualquer humanoide: a metade de baixo (pernas, capa, base)
    // é mais larga que a de cima (cabeça).
    //
    // Só entram nesta lista os modelos que realmente precisam: vários destes
    // personagens têm bounding box quase cúbica e seriam girados por engano se
    // a checagem fosse automática para todos.
    // NÃO usar a bounding box para decidir quem entra aqui: o Guarda-Costas foi
    // colocado nesta lista porque media Z=0.89 contra Y=0.63, mas o que era
    // comprido era o ESCUDO dele, não o corpo — estava de pé, e o giro é que o
    // derrubou. A caixa não distingue "deitado" de "carrega algo largo".
    //
    // O jeito certo é OLHAR o modelo antes: renderizar a malha do FBX e ver.
    // Só entra aqui quem estiver mesmo tombado.
    static readonly HashSet<string> ModelosParaEndireitar = new HashSet<string>
    {
        "abencoado",   // esse sim: exportado em Z-up, chega deitado de barriga

        // Os três magos que vieram no formato pequeno do Meshy (FBX ~3 MB com
        // textura em PNG separado) — mesmo exportador Z-up do abençoado.
        // Conferido no render: X -90 põe os três de pé.
        "conjurador",
        "estilhaco",
        "metamorfo",

        // As três healers no mesmo formato pequeno do Meshy.
        // Conferido no render (arte-ia/render_fbx.py): X -90 põe as três de pé.
        "matriarca",
        "milagreira",
        "tesoureira",
    };

    // Unity reimporta os modelos desta pasta quando este número muda. Subir a
    // cada vez que a regra de giro mudar, senão o Unity mantém o resultado
    // antigo em cache e a correção não aparece.
    public override uint GetVersion()
    {
        return 4;
    }

    // Roda depois da importação: gira os VÉRTICES, não o transform. Assim o
    // modelo fica de pé em qualquer lugar que for usado — preview do Inspector,
    // tabuleiro, lobby — sem depender de quem o instanciou.
    void OnPostprocessModel(GameObject root)
    {
        if (!NaPasta(assetPath)) return;

        string nome = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        if (!ModelosParaEndireitar.Contains(nome)) return;

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            Endireitar(mf.sharedMesh, nome);

        foreach (SkinnedMeshRenderer sm in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            Endireitar(sm.sharedMesh, nome);
    }

    // As quatro orientações que podem deixar um modelo tombado de pé
    static readonly Vector3[] Candidatos =
    {
        new Vector3(0f, 0f, 0f),        // já está certo
        new Vector3(-90f, 0f, 0f),      // Z-up  -> Y-up
        new Vector3(90f, 0f, 0f),       // Z-down -> Y-up
        new Vector3(0f, 0f, 90f),       // X-up  -> Y-up
        new Vector3(0f, 0f, -90f),      // X-down -> Y-up
    };

    static void Endireitar(Mesh m, string nome)
    {
        if (m == null) return;
        Vector3[] orig = m.vertices;
        if (orig == null || orig.Length < 30) return;

        Vector3 melhor = Vector3.zero;
        float melhorNota = float.NegativeInfinity;
        string relato = "";

        foreach (Vector3 euler in Candidatos)
        {
            Quaternion q = Quaternion.Euler(euler);
            float nota = Nota(orig, q);
            relato += $"  {euler} => {nota:F2}\n";
            if (nota > melhorNota)
            {
                melhorNota = nota;
                melhor = euler;
            }
        }

        Debug.Log($"[CardFigure] '{nome}' notas de orientação:\n{relato}  escolhida: {melhor}");

        if (melhor == Vector3.zero) return;
        GirarMalha(m, Quaternion.Euler(melhor));
    }

    // Nota de "está de pé": alta quando o eixo Y é o mais comprido E a metade
    // de baixo é mais larga que a de cima.
    static float Nota(Vector3[] v, Quaternion q)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 p = q * v[i];
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }
        Vector3 tam = max - min;
        if (tam.y <= 0.0001f) return float.NegativeInfinity;

        // 1) esbeltez: altura contra a maior das duas larguras
        float esbeltez = tam.y / Mathf.Max(0.0001f, Mathf.Max(tam.x, tam.z));

        // 2) base mais larga que o topo
        float meio = (min.y + max.y) * 0.5f;
        Vector2 baixoMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 baixoMax = new Vector2(float.MinValue, float.MinValue);
        Vector2 cimaMin = baixoMin, cimaMax = baixoMax;
        for (int i = 0; i < v.Length; i++)
        {
            Vector3 p = q * v[i];
            Vector2 xz = new Vector2(p.x, p.z);
            if (p.y < meio) { baixoMin = Vector2.Min(baixoMin, xz); baixoMax = Vector2.Max(baixoMax, xz); }
            else            { cimaMin  = Vector2.Min(cimaMin,  xz); cimaMax  = Vector2.Max(cimaMax,  xz); }
        }
        float largBaixo = Mathf.Max(baixoMax.x - baixoMin.x, baixoMax.y - baixoMin.y);
        float largCima  = Mathf.Max(cimaMax.x - cimaMin.x, cimaMax.y - cimaMin.y);
        float baseFirme = largBaixo / Mathf.Max(0.0001f, largCima);

        return esbeltez + baseFirme;
    }

    static bool GirarMalha(Mesh m, Quaternion q)
    {
        if (m == null) return false;

        Vector3[] v = m.vertices;
        if (v == null || v.Length == 0) return false;
        for (int i = 0; i < v.Length; i++) v[i] = q * v[i];
        m.vertices = v;

        Vector3[] n = m.normals;
        if (n != null && n.Length == v.Length)
        {
            for (int i = 0; i < n.Length; i++) n[i] = q * n[i];
            m.normals = n;
        }

        Vector4[] t = m.tangents;
        if (t != null && t.Length == v.Length)
        {
            for (int i = 0; i < t.Length; i++)
            {
                Vector3 dir = q * new Vector3(t[i].x, t[i].y, t[i].z);
                t[i] = new Vector4(dir.x, dir.y, dir.z, t[i].w); // w = sinal da bitangente
            }
            m.tangents = t;
        }

        m.RecalculateBounds();
        return true;
    }

    static bool NaPasta(string path)
    {
        return path.Replace('\\', '/').ToLowerInvariant().Contains(Pasta);
    }

    void OnPreprocessModel()
    {
        if (!NaPasta(assetPath)) return;

        ModelImporter m = assetImporter as ModelImporter;
        if (m == null) return;

        // A cor vem do _tex.png; as texturas de dentro do FBX ficam de fora
        m.materialImportMode = ModelImporterMaterialImportMode.None;

        bool temRig = System.Array.Exists(ModelosComRig,
            n => assetPath.ToLowerInvariant().Contains(n.ToLowerInvariant()));

        if (!temRig)
        {
            m.importAnimation = false;
            m.animationType = ModelImporterAnimationType.None;
        }

        m.importBlendShapes = false;
        m.importCameras = false;
        m.importLights = false;
        m.importVisibility = false;
        m.isReadable = false;                 // metade da RAM: sem cópia na CPU
        m.meshOptimizationFlags = MeshOptimizationFlags.Everything;
        m.weldVertices = true;
    }

    void OnPreprocessTexture()
    {
        if (!NaPasta(assetPath)) return;

        TextureImporter t = assetImporter as TextureImporter;
        if (t == null) return;

        t.textureType = TextureImporterType.Default;
        t.maxTextureSize = MaxTextura;
        t.textureCompression = TextureImporterCompression.Compressed;
        t.crunchedCompression = true;
        t.compressionQuality = 50;
        t.mipmapEnabled = true;               // sem mipmap a figura pequena cintila
        t.isReadable = false;
        t.wrapMode = TextureWrapMode.Clamp;
        t.alphaIsTransparency = false;
    }
}
