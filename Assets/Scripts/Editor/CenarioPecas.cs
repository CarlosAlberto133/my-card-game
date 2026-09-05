using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// ════════════════════════════════════════════════════════════════════════════
// Larga na cena uma PEÇA DE CENÁRIO para ser posicionada à MÃO. Dois tipos:
//
//  • as peças PRÓPRIAS do Meshy (Resources/decor/cenario) — tendas, baús,
//    jardineira, estandartes, barril;
//  • as FONTES DE LUZ — as do pack KayKit (CC0, já no projeto: tocha, vela,
//    candelabro, lanterna) e as próprias do Meshy (fogueira, lamparina) —
//    que saem daqui JÁ ACESAS, e o cristal.
//
// Nenhuma delas é montada por código em lugar nenhum: o menu só as coloca na
// cena, com material e luz certos, e dali em diante é gizmo — mover, girar,
// escalar, duplicar. Salve a cena (Ctrl+S).
//
// COMO USAR:
//   Card Game → Cenário: adicionar peça → (escolha)
//   Card Game → Cenário: adicionar luz  → (escolha)
//
// Elas entram DENTRO do MesaStage quando ele existe, para acender e apagar
// junto com o resto do cenário. Sem MesaStage, ficam soltas na cena.
//
// ⚠️ Re-assar o cenário do zero (o menu "assar cenário na cena") APAGA o
// MesaStage inteiro — e com ele estas peças.
// ════════════════════════════════════════════════════════════════════════════
public static class CenarioPecas
{
    // Cura AUTOMÁTICA ao recarregar os scripts: chamas sem material e luzes
    // sem FlickerLight são refeitas na hora, na Scene view, sem menu nenhum.
    // Existe porque, no dia em que estes componentes moravam no arquivo errado,
    // "rode o menu" não resolveu três vezes seguidas — o que se conserta
    // sozinho é o que fica consertado.
    [InitializeOnLoadMethod]
    static void CuraAoCarregar()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isCompiling) return;
            int chamas = ReparaChamas();
            if (chamas > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
                Debug.Log("[Cenário] " + chamas + " chama(s) sem material curada(s) ao carregar.");
            }
        };
    }

    // Cor do fogo: o miolo quente vira brasa escura ao subir. A luz usa o
    // mesmo laranja das tochas que já estavam nos 4 cantos da moldura.
    static readonly Color FogoQuente = new Color(1.00f, 0.78f, 0.35f);
    static readonly Color FogoFrio = new Color(0.90f, 0.25f, 0.06f);
    static readonly Color LuzFogo = new Color(1.00f, 0.62f, 0.25f);

    // ── Peças próprias (Meshy) ────────────────────────────────────────────
    // Alturas de partida na régua do tabuleiro: uma casa tem 6 de lado.

    // Peça do fundo do tabuleiro (arquivo próprio, NÃO é o piso das casas).
    //
    // É um BLOCO, não uma lajota fina: 1.90 x 1.90 de área por 0.89 de altura.
    // Na escala do fundo isso vira ~3.7 de espessura, mas só o topo aparece — o
    // resto fica enterrado dentro do corpo da mesa (que desce até y -3.9).
    // O PlaceSceneryFloor mede a malha em runtime e já sabe que esta peça nasce
    // deitada (a anterior nascia em pé e ele girava), então a troca é direta.
    const string FundoLajota = "laje-ardosia";

    [MenuItem("Card Game/Cenário: adicionar peça/Tenda azul (pavilhão)")]
    public static void AddTenda() { Meshy("tenda", "Tenda", 34f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Tenda vermelha (comando)")]
    public static void AddTendaVermelha() { Meshy("tenda-vermelha", "TendaVermelha", 34f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Baú do tesouro")]
    public static void AddBau() { Meshy("bau", "Bau", 6f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Baú de relíquia (safira)")]
    public static void AddBauAzul() { Meshy("bau-azul", "BauAzul", 6f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Jardineira de pedra")]
    public static void AddJardineira() { Meshy("jardineira", "Jardineira", 8f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Barril com bonsai")]
    public static void AddBarril() { Meshy("barril-bonsai", "Barril", 9f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Estandarte dourado")]
    public static void AddEstandarte() { Meshy("estandarte", "Estandarte", 22f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Estandarte azul")]
    public static void AddEstandarteAzul() { Meshy("estandarte-azul", "EstandarteAzul", 22f); }

    [MenuItem("Card Game/Cenário: adicionar peça/Estandarte vermelho")]
    public static void AddEstandarteVermelho() { Meshy("estandarte-vermelho", "EstandarteVermelho", 22f); }

    // ── Fundo do tabuleiro ──────────────────────────────────
    //
    // A moldura do tabuleiro se apoia na mesa, mas por DENTRO não havia chão:
    // entre as casas sobra 0.6 de folga (tileSpacing) e da última casa até a
    // mureta sobra mais uma faixa — por tudo isso aparecia a madeira da mesa.
    // Este menu fecha o vão com uma laje de lajotas.
    //
    // Ela fica ABAIXO das casas: as casas são montadas em runtime pelo
    // BoardThemeManager (por isso só aparecem em Play) e o topo delas é y=0.

    // Meia-largura do tabuleiro é 22.8 (7 casas de 6 + 6 folgas de 0.6).
    // Um tico a mais para a laje entrar POR BAIXO da mureta e não deixar
    // costura visível na junta.
    const float FundoMeiaLargura = 23.4f;

    // Cor do fundo. A ardósia crua é cinza-FRIO (RGB 76,73,73) e destoava: tudo
    // à volta dela é quente — as casas do tabuleiro (141,114,88), a tábua da mesa
    // (97,60,34). O problema nunca foi ela ser escura, foi ser cinza.
    //
    // Multiplica a textura (o URP faz base = textura × _BaseColor), então valores
    // acima de 1 clareiam sem lavar o relevo da pedra:
    //     76×1.30 = 99 | 73×1.06 = 77 | 73×0.82 = 60  →  RGB(99, 77, 60)
    //
    // Esse tom cai entre a casa escura (110,87,63) e a madeira (97,60,34): fica
    // na mesma família quente e continua um degrau mais escuro que as casas, que
    // é o que faz a grade do tabuleiro ler em vez de sumir.
    public static readonly Color FundoCor = new Color(1.30f, 1.06f, 0.82f);

    // 6×6 lajotas. Mais pedaços = padrão mais miúdo, mas também mais malha:
    // o bloco do Meshy tem ~4.7 mil vértices cada (36 × = ~170 mil).
    const int FundoLajotas = 6;

    // Topo da laje: logo abaixo da superfície das casas (y=0) e ACIMA do tampo
    // da mesa (-0.15), que é o que faz a pedra tapar a madeira nas frestas.
    const float FundoTopo = -0.05f;

    // O fundo que já está na cena tem material próprio (assado pelo MesaStage),
    // então mudar FundoCor no código não mexe nele. Este menu aplica no lugar.
    [MenuItem("Card Game/Cenário: cor do fundo do tabuleiro")]
    public static void CorDoFundo()
    {
        if (Bloqueado()) return;

        GameObject stage = MesaStageBaker.FindStage();
        Transform fundo = stage != null ? stage.transform.Find("FundoTabuleiro") : null;
        if (fundo == null)
        {
            EditorUtility.DisplayDialog("Cor do fundo",
                "Não achei \"FundoTabuleiro\" dentro do MesaStage. Monte o fundo " +
                "primeiro (Cenário → adicionar fundo do tabuleiro).", "OK");
            return;
        }

        // Um material por slug, mas o assar do MesaStage pode ter gerado vários:
        // pinta cada material distinto uma vez só
        var vistos = new System.Collections.Generic.HashSet<Material>();
        int pecas = 0;
        foreach (Renderer r in fundo.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r is ParticleSystemRenderer) continue;
            pecas++;
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null || !vistos.Add(m)) continue;
                Undo.RecordObject(m, "Cor do fundo");
                m.color = FundoCor;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", FundoCor);
                EditorUtility.SetDirty(m);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Cor do fundo",
            vistos.Count + " material(is) do fundo tingido(s) em " + pecas + " peça(s).\n\n" +
            "Tom quente de pedra, para casar com as casas e a madeira da mesa.\n\n" +
            "Para afinar: FundoCor em CenarioPecas.cs e rode este menu de novo.",
            "OK");
    }

    [MenuItem("Card Game/Cenário: adicionar fundo do tabuleiro")]
    public static void AddFundo()
    {
        if (Bloqueado()) return;

        Vector3 center = Centro();
        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        GameObject go = new GameObject("FundoTabuleiro");
        if (pai != null) go.transform.SetParent(pai, false);
        go.transform.position = center;
        go.AddComponent<SelectionRoot>();   // clique/Delete pegam a laje toda

        float lado = 2f * FundoMeiaLargura / FundoLajotas;

        // System.Random com semente fixa: o giro sorteado das lajotas sai
        // igual toda vez, então re-adicionar o fundo dá o mesmo desenho
        System.Random rng = new System.Random(20260904);

        int postas = 0;
        for (int i = 0; i < FundoLajotas; i++)
            for (int j = 0; j < FundoLajotas; j++)
            {
                Vector3 topo = center + new Vector3(
                    -FundoMeiaLargura + lado * (i + 0.5f),
                    FundoTopo,
                    -FundoMeiaLargura + lado * (j + 0.5f));

                Renderer rend;
                GameObject laje = DecorProps.PlaceSceneryFloor(go.transform,
                    FundoLajota, topo, lado, 90f * rng.Next(4), out rend);
                if (laje != null) postas++;
                // O material da peça é cacheado por slug (DecorProps), ou seja as
                // 36 lajotas dividem UM material — tingir uma tinge todas
                if (rend != null && rend.sharedMaterial != null)
                    rend.sharedMaterial.color = FundoCor;
            }

        if (postas == 0)
        {
            Object.DestroyImmediate(go);
            EditorUtility.DisplayDialog("Fundo do tabuleiro",
                "Não achei Resources/decor/cenario/" + FundoLajota + " — veja o Console.",
                "OK");
            return;
        }

        Finaliza(go, "FundoTabuleiro", stage,
            postas + " lajotas de " + lado.ToString("0.0") + " fechando o vão de " +
            (2f * FundoMeiaLargura).ToString("0.0") + " da moldura.\n\n" +
            "As CASAS não aparecem na Scene view — elas são montadas em runtime, " +
            "então aqui você vê a laje sozinha e em Play ela fica por baixo.\n\n" +
            "Se sobrar pedra para fora da mureta, encolha o pai no gizmo; se " +
            "aparecer madeira nas frestas, suba o pai uns centésimos no Y.");
    }

    // O chão já está assado dentro do MesaStage; trocar o código não mexe no que
    // está na cena. Este menu refaz o material dele no lugar, com o que estiver
    // em TabletopEnvironment.GroundSlug — serve para qualquer troca de chão.
    [MenuItem("Card Game/Cenário: refazer o chão")]
    public static void RefazerChao()
    {
        if (Bloqueado()) return;

        GameObject stage = MesaStageBaker.FindStage();
        if (stage == null)
        {
            EditorUtility.DisplayDialog("Chão",
                "Não há MesaStage nesta cena — o chão já vem do código, que sempre " +
                "usa a textura atual. Nada a fazer.", "OK");
            return;
        }

        Texture2D tex = Resources.Load<Texture2D>(
            "decor/cenario/" + TabletopEnvironment.GroundSlug + "_tex");
        Texture2D nrm = Resources.Load<Texture2D>(
            "decor/cenario/" + TabletopEnvironment.GroundSlug + "_normal");
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Chão",
                "Não achei Resources/decor/cenario/" +
                TabletopEnvironment.GroundSlug + "_tex.", "OK");
            return;
        }

        // Wrap Mode Clamp aqui não repete: estica o pixel da borda pelo chão
        // inteiro e ele vira uma cor chapada (foi o bug de 04/set/2026). No
        // editor dá para consertar de verdade, no próprio importador.
        foreach (Texture2D img in new[] { tex, nrm })
        {
            if (img == null || img.wrapMode == TextureWrapMode.Repeat) continue;
            string caminho = AssetDatabase.GetAssetPath(img);
            TextureImporter imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
            if (imp == null) continue;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.SaveAndReimport();
            Debug.Log("[Cenário] '" + img.name + "' estava em Clamp; virou Repeat.");
        }

        int n = 0;
        float lado = TabletopEnvironment.GroundTiles;
        foreach (Transform tr in stage.GetComponentsInChildren<Transform>(true))
        {
            if (tr == null || tr.name != "TavernFloor") continue;
            Renderer r = tr.GetComponent<Renderer>();
            Material m = r != null ? r.sharedMaterial : null;
            if (m == null) continue;

            Undo.RecordObject(m, "Refazer chão");
            m.color = TabletopEnvironment.GroundTint;
            m.mainTexture = tex;
            m.mainTextureScale = new Vector2(lado, lado);
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", new Vector2(lado, lado));
            }
            if (nrm != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", nrm);
                m.SetTextureScale("_BumpMap", new Vector2(lado, lado));
                m.EnableKeyword("_NORMALMAP");
            }
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.10f);
            EditorUtility.SetDirty(m);
            n++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Chão",
            n == 0
                ? "Não achei nenhum \"TavernFloor\" dentro do MesaStage."
                : n + " chão(ões) refeito(s) com '" + TabletopEnvironment.GroundSlug +
                  "', repetida " +
                  lado.ToString("0") + "×.\n\nSalve a cena (Ctrl+S).",
            "OK");
    }

    // ── Fontes de luz do KayKit ───────────────────────────────────────────
    //
    // ⚠️ Os números de LUZ aqui são grandes de propósito. Ver "ESCALA DAS
    // LUZES" em TabletopEnvironment: uma casa tem 6 unidades, a atenuação é
    // 1/d², então o que ilumina numa cena de 1 unidade = 1 metro precisa ser
    // ~25× maior aqui. Regra de bolso: intensidade ÷ 36 = o quanto a luz
    // entrega a UMA CASA de distância.

    [MenuItem("Card Game/Cenário: adicionar luz/Tocha de chão (acesa)")]
    public static void AddTocha()
    {
        Acesa("torch_lit", "Tocha", 7.5f, 7.0f, 1.10f, 1, 0.45f, 90f, 70f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Tocha de parede (acesa)")]
    public static void AddTochaParede()
    {
        Acesa("torch_mounted", "TochaParede", 6.0f, 5.4f, 1.00f, 1, 0.45f, 70f, 60f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Vela (acesa)")]
    public static void AddVela()
    {
        Acesa("candle_lit", "Vela", 2.2f, 2.1f, 0.32f, 1, 0.45f, 12f, 20f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Candelabro de 3 velas (aceso)")]
    public static void AddCandelabro()
    {
        Acesa("candle_triple", "Candelabro", 3.0f, 2.9f, 0.30f, 3, 0.45f, 28f, 32f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Lanterna KayKit (acesa)")]
    public static void AddLanterna()
    {
        Acesa("lantern", "Lanterna", 4.0f, 2.4f, 0.45f, 1, 0.45f, 50f, 45f);
    }

    // ── Fontes de luz próprias (Meshy) ────────────────────────────────────
    // Aqui a chama não pode ir numa altura fixa: cada FBX do Meshy chega numa
    // proporção diferente. A peça é medida DEPOIS de colocada e o fogo entra
    // numa FRAÇÃO da altura real dela.

    [MenuItem("Card Game/Cenário: adicionar luz/Fogueira em braseiro (acesa)")]
    public static void AddFogueira()
    {
        // Braseiro: a lenha arde numa bacia larga no topo, então são três
        // chamas espalhadas em vez de uma língua fina de tocha. É a luz mais
        // forte do menu — uma só já muda o clima de um canto inteiro.
        AcesaMeshy("fogueira", "Fogueira", 9f, 0.86f, 1.70f, 3, 0.30f, 170f, 110f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Lamparina (acesa)")]
    public static void AddLamparina()
    {
        // Lamparina: o pavio fica DENTRO do vidro, mais ou menos na metade
        AcesaMeshy("lamparina", "Lamparina", 7f, 0.55f, 0.50f, 1, 0f, 60f, 55f);
    }

    [MenuItem("Card Game/Cenário: adicionar luz/Cristal aceso (para a borda)")]
    public static void AddCristal()
    {
        if (Bloqueado()) return;

        Vector3 center = Centro();
        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        // Perto do canto da moldura (meia-largura ~22.8), que é onde fica o
        // diamante entalhado na peça de canto — dali é só encostar no gizmo
        GameObject go = DecorProps.GlowGem(pai, center + new Vector3(24f, 1.2f, 24f),
            0.55f, new Color(0.45f, 0.80f, 1.00f), 2.6f);

        Finaliza(go, "Cristal", stage,
            "O brilho é uma bolinha acesa + uma luz, POR CIMA do diamante da " +
            "peça de canto — o diamante é pedra do mesmo material que o resto " +
            "do bloco, então acender o material acenderia a mureta inteira.\n\n" +
            "Encoste a bolinha no diamante e ajuste a escala. Quem faz o brilho " +
            "de verdade é o Bloom: a cor está multiplicada além de 1 de propósito.");
    }

    // ── Conserto do que já está na cena ───────────────────────────
    //
    // Dois estragos que mudar o código NÃO desfaz, porque já estão salvos na
    // cena. Este menu passa neles uma vez, e é seguro rodar de novo.
    //
    //  1. INTENSIDADE na escala errada (ver ESCALA DAS LUZES em
    //     TabletopEnvironment): tudo colocado antes de 04/set/2026 iluminava
    //     um raio de ~2 unidades, ou seja, nada.
    //  2. CHAMA SEM MATERIAL, que a Unity desenha em magenta. O assador
    //     nomeava o material por objeto + contador, e o contador reiniciava a
    //     cada peça: a segunda tocha gerava "mat-chama-1" de novo e o
    //     CreateAsset APAGAVA o material da primeira. Corrigido na origem com
    //     GenerateUniqueAssetPath, mas quem já perdeu precisa dele de volta.

    // Intensidade velha → (intensidade nova, alcance novo). Alcance <= 0
    // significa "multiplique o que já tem" — é o caso do cristal, cujo
    // alcance é proporcional ao raio da bolinha.
    static readonly float[,] Tabela =
    {
        // velha   nova   alcance
        {  2.3f,   90f,    70f },   // tocha de chão (cantos da moldura, menu)
        {  2.0f,   70f,    60f },   // tocha de parede
        {  1.8f,   60f,    55f },   // lamparina
        {  1.6f,   22f,    -1f },   // cristal (alcance é proporcional ao raio)
        {  1.5f,   50f,    45f },   // lanterna KayKit
        {  1.4f,   28f,    32f },   // vela tripla do cenário
        {  1.3f,   28f,    32f },   // candelabro do menu
        {  0.9f,   12f,    20f },   // vela
        {  3.1f,  170f,   110f },   // fogueira
        // A primeira leva de valores "corrigidos" (04/set) ainda era tímida
        {   30f,   90f,    70f },   // tocha de chão
        {   24f,   70f,    60f },   // tocha de parede
        {   20f,   60f,    55f },   // lamparina
        {   16f,   50f,    45f },   // lanterna
        {    9f,   28f,    32f },   // candelabro
        {    8f,   22f,    -1f },   // cristal
        {    4f,   12f,    20f },   // vela
        {   55f,  170f,   110f },   // fogueira
    };

    // A luz mais fraca da leva certa é a vela, 12. Abaixo disso e fora da
    // tabela, é luz na escala velha que ninguém calibrou — vale a conversão
    // genérica. Acima, presume-se que foi você quem escolheu.
    const float MaisFracaCerta = 12f;

    [MenuItem("Card Game/Cenário: consertar luzes e chamas da cena")]
    public static void RecalibrarLuzes()
    {
        if (Bloqueado()) return;

        int chamas = ReparaChamas();

        FlickerLight[] todas = Object.FindObjectsByType<FlickerLight>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        int mexidas = 0, puladas = 0;
        foreach (FlickerLight f in todas)
        {
            Light l = f != null ? f.GetComponent<Light>() : null;
            if (l == null) continue;

            // 1º) Já está num valor de CHEGADA da tabela? Então já passou por
            //     aqui. É este teste que deixa rodar o menu duas vezes sem
            //     multiplicar tudo de novo.
            bool jaCerta = false;
            for (int i = 0; i < Tabela.GetLength(0); i++)
                if (Mathf.Abs(l.intensity - Tabela[i, 1]) < 0.05f) { jaCerta = true; break; }
            if (jaCerta) { puladas++; continue; }

            // 2º) É um valor de PARTIDA conhecido? Converte para o par certo.
            float nova = -1f;
            float alcance = l.range;
            for (int i = 0; i < Tabela.GetLength(0); i++)
                if (Mathf.Abs(l.intensity - Tabela[i, 0]) < 0.05f)
                {
                    nova = Tabela[i, 1];
                    alcance = Tabela[i, 2] > 0f ? Tabela[i, 2] : l.range * 1.85f;
                    break;
                }

            // 3º) Fora da tabela: só mexe no que está claramente na escala velha.
            //     Luz que VOCÊ ajustou à mão para um valor forte fica em paz.
            if (nova < 0f)
            {
                if (l.intensity >= MaisFracaCerta) { puladas++; continue; }
                nova = l.intensity * 25f;      // a correção de escala pura
                alcance = l.range * 2f;
            }

            Undo.RecordObject(l, "Recalibrar luz");
            l.intensity = nova;
            l.range = alcance;
            EditorUtility.SetDirty(l);
            mexidas++;
        }

        if (mexidas + chamas > 0)
            EditorSceneManager.MarkAllScenesDirty();

        EditorUtility.DisplayDialog("Consertar cenário",
            mexidas + " luz(es) recalibrada(s), " + puladas + " já estava(m) certa(s).\n" +
            chamas + " chama(s) sem material recuperada(s) — as magenta.\n\n" +
            (mexidas + chamas > 0
                ? "Salve a cena (Ctrl+S). Se a luz ficar forte demais para o seu " +
                  "gosto, o ajuste de TODAS de uma vez é o slider \"tochas\" do " +
                  "LightingTuning — não precisa mexer uma por uma."
                : "Nada a fazer: está tudo certo."),
            "OK");
    }

    // Chama que perdeu o material (magenta na tela) recebe o material das
    // chamas de volta. Só mexe em quem está sem NADA: uma chama cujo material
    // você tenha trocado à mão fica como está.
    static int ReparaChamas()
    {
        // Primeiro fora os cadáveres: componentes "Missing Script" que ficaram
        // do tempo em que FlickerLight/FlameLook moravam no arquivo errado
        int mortos = 0;
        foreach (Transform tr in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr == null) continue;
            string nome = tr.gameObject.name;
            if (nome != "Chama" && nome != "FlickerLight" && nome != "LuzCristal") continue;
            mortos += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
        }
        if (mortos > 0) Debug.Log("[Cenário] " + mortos + " script(s) morto(s) removido(s).");

        int chamas = FlameLook.HealAll();
        int luzes = FlickerLight.HealAll();
        if (luzes > 0) Debug.Log("[Cenário] " + luzes + " luz(es) recuperaram o FlickerLight.");

        // Envelopes de antes do LightPiece: ganham o [SelectionBase] agora,
        // para o próximo Delete levar a peça inteira
        foreach (Light l in Object.FindObjectsByType<Light>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform env = l != null ? l.transform.parent : null;
            if (env == null || env.GetComponent<LightPiece>() != null) continue;
            string n = env.name;
            if (n == "Tocha" || n == "TochaParede" || n == "Vela" || n == "Candelabro" ||
                n == "Lanterna" || n == "Fogueira" || n == "Lamparina" || n == "Cristal")
                Undo.AddComponent<LightPiece>(env.gameObject);
        }

        // O material pode ter acabado de nascer em memória: sem virar arquivo,
        // a cena guardaria referência a nada e ele voltaria magenta ao reabrir
        if (chamas > 0)
        {
            GameObject stage = MesaStageBaker.FindStage();
            if (stage != null) MesaStageBaker.PersistRuntimeAssets(stage);
        }
        return chamas;
    }

    // Duas sobras que deixam luz acesa sem peça nenhuma à vista:
    //  • luz pendurada DIRETO no MesaStage — as 4 tochas dos cantos e a vela do
    //    assador nasciam com a luz num objeto irmão; apagar a tocha não a levava;
    //  • envelope SEM MODELO — clicar na tocha na Scene view selecionava só a
    //    malha, Delete apagava só ela, e ficava um "Tocha" invisível com chama e
    //    luz (resolvido daqui em diante pelo LightPiece/[SelectionBase]).
    [MenuItem("Card Game/Cenário: remover luzes sem peça")]
    public static void RemoverLuzesSoltas()
    {
        if (Bloqueado()) return;

        GameObject stage = MesaStageBaker.FindStage();
        if (stage == null)
        {
            EditorUtility.DisplayDialog("Luzes sem peça",
                "Não há MesaStage nesta cena — nada a fazer.", "OK");
            return;
        }

        var alvo = new System.Collections.Generic.HashSet<GameObject>();
        int soltas = 0, semModelo = 0;
        foreach (Light l in stage.GetComponentsInChildren<Light>(true))
        {
            if (l == null || l.type != LightType.Point) continue;
            Transform env = l.transform.parent;
            if (env == null) continue;

            if (env == stage.transform)
            {
                // Luz direto no palco, sem envelope: sobra do assador
                if (alvo.Add(l.gameObject)) soltas++;
                continue;
            }

            // Envelope sem NENHUMA malha por baixo: tocha/lamparina "apagada"
            // que continuou acesa. (Partícula não conta como malha.)
            bool temMalha = false;
            foreach (Renderer r in env.GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer)) { temMalha = true; break; }
            if (!temMalha && alvo.Add(env.gameObject)) semModelo++;
        }

        if (alvo.Count == 0)
        {
            EditorUtility.DisplayDialog("Luzes sem peça",
                "Nenhuma: toda luz da cena tem uma peça visível em cima.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Luzes sem peça",
                soltas + " luz(es) solta(s) do assador e " + semModelo +
                " envelope(s) de tocha/lamparina sem modelo (só chama e luz).\n\n" +
                "Apagar tudo isso? (Ctrl+Z desfaz.)", "Apagar", "Cancelar"))
            return;

        foreach (GameObject go in alvo)
            Undo.DestroyObjectImmediate(go);

        EditorSceneManager.MarkSceneDirty(stage.scene);
        Debug.Log("[Cenário] " + alvo.Count + " objeto(s) de luz sem peça removido(s). Salve a cena.");
    }

    // ── Motor ─────────────────────────────────────────────────────────────

    static void Meshy(string slug, string rotulo, float altura)
    {
        if (Bloqueado()) return;

        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        GameObject go = DecorProps.PlaceSceneryProp(pai, slug, Partida(), altura, 0f);
        if (go == null)
        {
            EditorUtility.DisplayDialog("Peça de cenário",
                "Não achei Resources/decor/cenario/" + slug + " — veja o Console.",
                "OK");
            return;
        }

        Finaliza(go, rotulo, stage,
            "Posicione com o gizmo e salve a cena (Ctrl+S). Se vier tombada, é " +
            "um giro no Inspector; o tamanho também é só ponto de partida.");
    }

    // Modelo do KayKit + CHAMA + luz que tremula. O fogo não vem no FBX: o
    // modelo é madeira parada, quem faz arder é o sistema de partículas.
    static void Acesa(string modelo, string rotulo, float altura, float alturaChama,
        float tamanhoChama, int quantasChamas, float raioChamas, float luz, float alcance)
    {
        if (Bloqueado()) return;

        Vector3 pos = Partida();
        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        GameObject go = Envelope(rotulo, pai, pos);

        GameObject modelo3d = DecorProps.Place(go.transform, modelo, pos, altura,
            Vector3.up, Vector3.forward);
        if (modelo3d == null)
        {
            Object.DestroyImmediate(go);
            EditorUtility.DisplayDialog("Fonte de luz",
                "Não achei Resources/decor/kaykit/" + modelo + " — veja o Console.",
                "OK");
            return;
        }

        Fogo(go, pos, alturaChama, tamanhoChama, quantasChamas, raioChamas, luz, alcance);
        Finaliza(go, rotulo, stage, DicaFogo());
    }

    // Mesma ideia, mas com peça PRÓPRIA (Resources/decor/cenario). A diferença
    // que importa: a altura da chama vem como FRAÇÃO (0..1) e é convertida
    // medindo a peça já colocada — cada FBX do Meshy tem uma proporção sua, e
    // um número fixo acertaria numa peça e erraria feio na seguinte.
    static void AcesaMeshy(string slug, string rotulo, float altura, float fracaoChama,
        float tamanhoChama, int quantasChamas, float raioChamas, float luz, float alcance)
    {
        if (Bloqueado()) return;

        Vector3 pos = Partida();
        GameObject stage = MesaStageBaker.FindStage();
        Transform pai = stage != null ? stage.transform : null;

        GameObject go = Envelope(rotulo, pai, pos);

        GameObject modelo3d = DecorProps.PlaceSceneryProp(go.transform, slug, pos, altura, 0f);
        if (modelo3d == null)
        {
            Object.DestroyImmediate(go);
            EditorUtility.DisplayDialog("Fonte de luz",
                "Não achei Resources/decor/cenario/" + slug + " — veja o Console.",
                "OK");
            return;
        }

        Bounds b = Medida(modelo3d);
        float alturaChama = b.size.y > 0.0001f ? b.size.y * fracaoChama : altura * fracaoChama;
        // Raio das chamas também na escala da peça (largura), não em unidades
        // soltas: assim o braseiro grande espalha o fogo e o pequeno não
        float raio = raioChamas * Mathf.Max(b.size.x, b.size.z);

        Fogo(go, pos, alturaChama, tamanhoChama, quantasChamas, raio, luz, alcance);
        Finaliza(go, rotulo, stage, DicaFogo());
    }

    // ── Utilidades ────────────────────────────────────────────────────────

    // Um pai só, para a peça e o fogo andarem juntos no gizmo
    static GameObject Envelope(string rotulo, Transform pai, Vector3 pos)
    {
        GameObject go = new GameObject(rotulo);
        if (pai != null) go.transform.SetParent(pai, false);
        go.transform.position = pos;
        go.AddComponent<LightPiece>();   // clique no modelo = peça inteira
        return go;
    }

    static void Fogo(GameObject go, Vector3 pos, float alturaChama, float tamanhoChama,
        int quantasChamas, float raioChamas, float luz, float alcance)
    {
        for (int i = 0; i < quantasChamas; i++)
        {
            // Uma chama = no eixo. Várias = espalhadas em círculo, para o
            // candelabro ter as três velas acesas e o braseiro ter a bacia
            // toda ardendo (o ajuste fino fica por conta do gizmo)
            Vector2 off = Vector2.zero;
            if (quantasChamas > 1 && raioChamas > 0f)
            {
                float ang = Mathf.PI * 2f * i / quantasChamas;
                off = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioChamas;
            }
            DecorProps.Flame(go.transform,
                pos + new Vector3(off.x, alturaChama, off.y),
                tamanhoChama, FogoQuente, FogoFrio);
        }

        FlickerLight.Attach(go.transform, pos + Vector3.up * alturaChama,
            LuzFogo, luz, alcance);
    }

    static string DicaFogo()
    {
        return "Vem com a chama (partículas) e a luz que tremula, tudo dentro do " +
            "mesmo objeto — mova o pai e o fogo vai junto.\n\n" +
            "⚠️ A chama só se mexe em PLAY (ou com o Play do preview de " +
            "partículas na Scene view); parada no editor ela é um borrãozinho.\n\n" +
            "A intensidade das tochas todas de uma vez é o LightingTuning.";
    }

    // Caixa que envolve TODOS os renderers da peça. É medida na hora porque
    // confiar na escala do import de um FBX do Meshy nunca deu certo.
    static Bounds Medida(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static bool Bloqueado()
    {
        if (!Application.isPlaying) return false;
        EditorUtility.DisplayDialog("Peça de cenário",
            "Saia do Play mode antes — o que é criado em Play se perde ao parar.",
            "OK");
        return true;
    }

    static Vector3 Centro()
    {
        BoardManager bm = Object.FindObjectOfType<BoardManager>();
        return bm != null ? bm.transform.position : Vector3.zero;
    }

    // Ponto de partida: em cima da mesa, à direita do tabuleiro — longe da
    // loja (x −29.5) e das mãos (z ±29.5), onde dá para ver e arrastar
    static Vector3 Partida()
    {
        return Centro() + new Vector3(42f, TabletopEnvironment.TableTopSurface, 0f);
    }

    static void Finaliza(GameObject go, string rotulo, GameObject stage, string dica)
    {
        go.name = rotulo;
        Undo.RegisterCreatedObjectUndo(go, "Adicionar " + rotulo);

        // Materiais e texturas nascem em memória; sem salvar como asset a cena
        // guardaria referência a nada e tudo abriria rosa "missing"
        MesaStageBaker.PersistRuntimeAssets(go);

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        if (SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.FrameSelected();

        EditorUtility.DisplayDialog("Peça de cenário",
            rotulo + " entrou na cena" +
            (stage != null ? ", dentro do MesaStage" : " (não há MesaStage: ficou solta)") +
            " — já enquadrada na Scene view.\n\n" + dica, "OK");
    }
}
