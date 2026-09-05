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
    // A rotação de cada um é EXPLÍCITA, e isso é de propósito.
    //
    // Antes, o código escolhia sozinho entre as quatro orientações possíveis,
    // pontuando cada uma por "esbelto + base mais larga que o topo". O problema
    // é que X-90 e X90 dão a MESMA caixa — uma é a outra de cabeça para baixo —,
    // então quem desempatava era só "a base é mais larga que o topo". Qualquer
    // personagem de chapéu largo ou braços abertos quebra essa regra.
    //
    // Auditoria de 05/set/2026 (render das 84 figuras + a nota reproduzida em
    // Python): X-90 é a certa para os 13, mas a heurística só acertava em 9.
    // Os quatro erros, e o que o jogador via:
    //   conjurador  -> escolhia X90   = DE CABEÇA PARA BAIXO (o chapéu de mago
    //                  é a parte mais larga; a nota do errado dava 2.52 x 1.60)
    //   milagreira  -> escolhia X90   = DE CABEÇA PARA BAIXO (braços abertos)
    //   flecha-fiel -> não girava    = DEITADA
    //   hidra       -> escolhia Z-90  = DEITADA de lado
    //
    // Modelo novo aqui: renderizar antes (`python arte-ia/render_fbx.py <nome>
    // --giros=x:-90,x:90`) e anotar a rotação que deixa ele de pé E encarando a
    // câmera. NÃO voltar a adivinhar: a caixa não distingue "de pé" de "de
    // cabeça para baixo", e nunca vai distinguir.
    //
    // (O Guarda-Costas já tinha ensinado a lição irmã: entrou nesta lista por
    // medir Z=0.89 contra Y=0.63, mas o comprido era o ESCUDO dele — estava de
    // pé, e o giro é que o derrubou.)
    static readonly Dictionary<string, Vector3> ModelosParaEndireitar =
        new Dictionary<string, Vector3>
    {
        // Z-up: chegam deitados de barriga. X-90 põe de pé e de frente.
        { "abencoado",   new Vector3(-90f, 0f, 0f) },

        // Os três magos do formato pequeno do Meshy (FBX ~3 MB + PNG separado)
        { "conjurador",  new Vector3(-90f, 0f, 0f) },
        { "estilhaco",   new Vector3(-90f, 0f, 0f) },
        { "metamorfo",   new Vector3(-90f, 0f, 0f) },

        // As três healers no mesmo formato pequeno
        { "matriarca",   new Vector3(-90f, 0f, 0f) },
        { "milagreira",  new Vector3(-90f, 0f, 0f) },
        { "tesoureira",  new Vector3(-90f, 0f, 0f) },

        // Seis arqueiras no mesmo formato pequeno (as outras 15 vieram de pé)
        { "acrobata",    new Vector3(-90f, 0f, 0f) },
        { "batedora",    new Vector3(-90f, 0f, 0f) },
        { "flecha-fiel", new Vector3(-90f, 0f, 0f) },
        { "hidra",       new Vector3(-90f, 0f, 0f) },
        { "miragem",     new Vector3(-90f, 0f, 0f) },
        { "zefiro",      new Vector3(-90f, 0f, 0f) },
    };

    // Unity reimporta os modelos desta pasta quando este número muda. Subir a
    // cada vez que a regra de giro mudar, senão o Unity mantém o resultado
    // antigo em cache e a correção não aparece.
    public override uint GetVersion()
    {
        // 6 = rotação explícita por modelo no lugar da heurística (05/set/2026),
        // que punha conjurador e milagreira de cabeça para baixo e deixava
        // flecha-fiel e hidra deitadas.
        return 6;
    }

    // Roda depois da importação: gira os VÉRTICES, não o transform. Assim o
    // modelo fica de pé em qualquer lugar que for usado — preview do Inspector,
    // tabuleiro, lobby — sem depender de quem o instanciou.
    void OnPostprocessModel(GameObject root)
    {
        if (!NaPasta(assetPath)) return;

        string nome = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        Vector3 euler;
        if (!ModelosParaEndireitar.TryGetValue(nome, out euler)) return;

        Quaternion q = Quaternion.Euler(euler);
        int malhas = 0;

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (GirarMalha(mf.sharedMesh, q)) malhas++;

        foreach (SkinnedMeshRenderer sm in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (GirarMalha(sm.sharedMesh, q)) malhas++;

        Debug.Log("[CardFigure] '" + nome + "' endireitado com " + euler +
                  " (" + malhas + " malha(s)).");
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
