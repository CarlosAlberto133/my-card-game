using UnityEngine;

// ⚠️ ARQUIVO PRÓPRIO DE PROPÓSITO. Esta classe morava dentro de DecorProps.cs e
// a Unity SÓ serializa um MonoBehaviour direito quando ele está num arquivo
// com o MESMO nome. Fora disso ela grava um "MonoScript embutido" na cena e,
// ao recarregar, o componente vira Missing Script — morto, sem Awake, sem
// Update, sem OnEnable. Foi assim que as tochas ficaram sem tremular e as
// chamas sem material por um dia inteiro (04/set/2026).

// Luz pontual com tremulação de fogo (tochas/velas). Usa PerlinNoise com
// Time.time — puramente visual, nada de UnityEngine.Random (lockstep intocado).
public class FlickerLight : MonoBehaviour
{
    // PÚBLICOS de propósito: precisam ser SERIALIZADOS para sobreviver ao
    // assador de mapas (uma chama assada no MesaStage volta do disco sem
    // passar pelo código que a criou). Os valores de fábrica reproduzem
    // exatamente a tremulação que as tochas já tinham: 0.82 + 0.36 * ruído.
    [Tooltip("Quanto a luz varia. 0.36 = tocha; 0.14 = cristal respirando")]
    public float amplitude = 0.36f;

    [Tooltip("Velocidade da variação. 5.5 = fogo; ~0.8 = brilho lento")]
    public float velocidade = 5.5f;

    [Tooltip("Desmarcado tira esta luz do ajuste em massa das tochas " +
             "(o cristal não é tocha)")]
    public bool eTocha = true;

    Light lt;
    float baseIntensity;
    float seedOffset;

    // O valor DE FÁBRICA de cada tocha (o que a cena ou o código pediu antes
    // de qualquer calibração). Guardar o original é o que deixa o
    // multiplicador da iluminação ir e voltar sem acumular a cada rebase.
    float origIntensity;
    float origRange;

    // Os campos privados NÃO são serializados: num FlickerLight "assado" na
    // cena (MesaStage), eles chegam zerados ao carregar — reconstruímos aqui
    // a partir do próprio Light (a intensidade dele é serializada normal)
    void Awake()
    {
        if (lt == null) lt = GetComponent<Light>();
        if (lt != null && baseIntensity <= 0f) baseIntensity = lt.intensity;
        if (seedOffset == 0f)
            seedOffset = transform.position.x * 3.7f + transform.position.z * 1.3f;
        if (lt != null && origIntensity <= 0f)
        {
            origIntensity = lt.intensity;
            origRange = lt.range;
        }

        // Rede de segurança: componente antigo, serializado antes destes
        // campos existirem, chegaria zerado e a luz ficaria apagada
        if (amplitude <= 0f) amplitude = 0.36f;
        if (velocidade <= 0f) velocidade = 5.5f;
    }

    // Recalibra TODAS as tochas da cena a partir do valor de fábrica de cada
    // uma. Quem chama é a iluminação da Mesa de RPG: com o ambiente escuro,
    // a tocha passa a ser a luz que conta a história, e não um enfeite.
    // Multiplicadores 1, 1 devolvem tudo ao que era.
    public static void RebaseAll(float multIntensidade, float multAlcance)
    {
        foreach (FlickerLight f in Object.FindObjectsByType<FlickerLight>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (f == null || !f.eTocha) continue;
            if (f.lt == null) f.lt = f.GetComponent<Light>();
            if (f.lt == null) continue;

            // Uma tocha assada na cena pode nem ter passado pelo Awake ainda
            if (f.origIntensity <= 0f)
            {
                f.origIntensity = f.lt.intensity;
                f.origRange = f.lt.range;
            }

            f.baseIntensity = f.origIntensity * multIntensidade;
            f.lt.range = f.origRange * multAlcance;
        }
    }

    // Reanima as luzes que perderam o componente (Missing Script — ver o aviso
    // no topo do arquivo). Reconhece as nossas pelo nome do objeto, que é o
    // que sobrevive a um script morto.
    public static int HealAll()
    {
        int n = 0;
        foreach (Light l in Object.FindObjectsByType<Light>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (l == null || l.type != LightType.Point) continue;
            string nome = l.gameObject.name;
            if (nome != "FlickerLight" && nome != "LuzCristal") continue;
            if (l.GetComponent<FlickerLight>() != null) continue;

            FlickerLight f = l.gameObject.AddComponent<FlickerLight>();
            if (nome == "LuzCristal")
            {
                f.amplitude = 0.14f;
                f.velocidade = 0.8f;
                f.eTocha = false;
            }
            n++;
        }
        return n;
    }

    public static void Attach(Transform parent, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject("FlickerLight");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;

        FlickerLight f = go.AddComponent<FlickerLight>();
        f.lt = l;
        f.baseIntensity = intensity;
        f.seedOffset = pos.x * 3.7f + pos.z * 1.3f;
    }

    void Update()
    {
        if (lt != null)
            lt.intensity = baseIntensity * (1f - amplitude * 0.5f
                + amplitude * Mathf.PerlinNoise(Time.time * velocidade, seedOffset));
    }
}
