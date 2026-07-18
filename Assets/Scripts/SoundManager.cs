using System.Collections.Generic;
using UnityEngine;

// Toca efeitos sonoros. Os áudios NÃO ficam no prefab nem no Inspector: são
// carregados em runtime de "Assets/Resources/Sounds/<nome>.wav|ogg|mp3".
// Enquanto os arquivos não existirem, cada Play() é um no-op silencioso —
// então dá para ligar os sons depois só jogando os arquivos na pasta, sem
// mexer em código. Áudio não sofre o problema de shader/material do build.
public class SoundManager : MonoBehaviour
{
    public enum Sound { Attack, Hit, Heal, Buff, Death, Buy, Place, Effect }

    // Nome do arquivo esperado em Resources/Sounds/ para cada evento
    private static readonly Dictionary<Sound, string> FileNames = new Dictionary<Sound, string>
    {
        { Sound.Attack, "attack" },
        { Sound.Hit,    "hit" },
        { Sound.Heal,   "heal" },
        { Sound.Buff,   "buff" },
        { Sound.Death,  "death" },
        { Sound.Buy,    "buy" },
        { Sound.Place,  "place" },
        { Sound.Effect, "effect" },
    };

    private static SoundManager instance;
    private AudioSource source;
    private readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    private bool warnedMissingFolder;

    // Volume master dos EFEITOS (0..1), salvo em PlayerPrefs — separado do
    // volume da música (MusicManager). Multiplica o volume de cada Play().
    private const string PrefVolume = "sfx_volume";
    private static float masterVolume = 1f;
    private static bool volumeLoaded = false;

    static void EnsureVolumeLoaded()
    {
        if (volumeLoaded) return;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefVolume, 1f));
        volumeLoaded = true;
    }

    public static float GetVolume()
    {
        EnsureVolumeLoaded();
        return masterVolume;
    }

    public static void SetVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        volumeLoaded = true;
        PlayerPrefs.SetFloat(PrefVolume, masterVolume);
        PlayerPrefs.Save();
    }

    public static void Ensure()
    {
        if (instance != null) return;
        GameObject go = new GameObject("SoundManager");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SoundManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D, volume constante
        LoadClips();
    }

    void LoadClips()
    {
        int found = 0;
        foreach (var kv in FileNames)
        {
            AudioClip clip = Resources.Load<AudioClip>("Sounds/" + kv.Value);
            if (clip != null) { clips[kv.Key] = clip; found++; }
        }
        if (found == 0 && !warnedMissingFolder)
        {
            warnedMissingFolder = true;
            Debug.Log("[SoundManager] Nenhum som encontrado. Adicione arquivos em " +
                      "Assets/Resources/Sounds/ (attack, hit, heal, buff, death, buy, place, effect).");
        }
    }

    // Ponto único de disparo — cria o manager na hora se preciso e ignora
    // silenciosamente se o clipe daquele evento ainda não existir.
    public static void Play(Sound sound, float volume = 1f)
    {
        if (instance == null) Ensure();
        if (instance == null) return;
        instance.PlayInternal(sound, volume);
    }

    void PlayInternal(Sound sound, float volume)
    {
        if (source == null) return;
        EnsureVolumeLoaded();
        if (clips.TryGetValue(sound, out AudioClip clip) && clip != null)
        {
            source.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
        }
    }
}
