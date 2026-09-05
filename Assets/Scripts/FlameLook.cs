using UnityEngine;

// ⚠️ ARQUIVO PRÓPRIO DE PROPÓSITO. Esta classe morava dentro de DecorProps.cs e
// a Unity SÓ serializa um MonoBehaviour direito quando ele está num arquivo
// com o MESMO nome. Fora disso ela grava um "MonoScript embutido" na cena e,
// ao recarregar, o componente vira Missing Script — morto, sem Awake, sem
// Update, sem OnEnable. Foi assim que as tochas ficaram sem tremular e as
// chamas sem material por um dia inteiro (04/set/2026).

// ════════════════════════════════════════════════════════════════════════════
// Garante que a CHAMA tenha material — sempre, em qualquer situação.
//
// Por que isto existe: o material da chama nasce em memória, e por um tempo a
// gente contou com o assador de mapas para salvá-lo como arquivo. Deu errado
// duas vezes seguidas (nomes que colidiam, referência perdida ao recarregar) e
// o sintoma é sempre o mesmo — partículas MAGENTA, o material de erro da Unity.
//
// A cura definitiva é não depender de arquivo: um material de partícula é
// barato e reconstruir custa nada, então ele é remontado toda vez que a cena
// carrega. [ExecuteAlways] faz valer também no editor, não só no Play.
// ════════════════════════════════════════════════════════════════════════════
[ExecuteAlways]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class FlameLook : MonoBehaviour
{
    // Passa em TODAS as chamas da cena: devolve o material a quem perdeu e
    // planta este componente em quem não tem. Chamado no Build do cenário
    // (Play) e pelo menu de conserto (editor) — os dois caminhos que rodam de
    // verdade, sem depender de o componente já estar vivo na cena.
    public static int HealAll()
    {
        int n = 0;
        foreach (ParticleSystemRenderer r in Object.FindObjectsByType<ParticleSystemRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Só as NOSSAS chamas — o poof das cartas e afins ficam em paz
            if (r == null || r.gameObject.name != "Chama") continue;

            if (r.GetComponent<FlameLook>() == null)
                r.gameObject.AddComponent<FlameLook>();

            if (r.sharedMaterial == null)
            {
                Material mat = DecorProps.FlameMaterial();
                if (mat != null) { r.sharedMaterial = mat; n++; }
            }
        }
        return n;
    }

    void OnEnable()
    {
        ParticleSystemRenderer r = GetComponent<ParticleSystemRenderer>();
        if (r == null || r.sharedMaterial != null) return;

        Material mat = DecorProps.FlameMaterial();
        if (mat != null) r.sharedMaterial = mat;
    }
}
