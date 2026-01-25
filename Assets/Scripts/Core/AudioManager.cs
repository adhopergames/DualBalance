using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    Move,
    OrbPickupLight,
    OrbPickupDark,
    OrbPickupDual,
    AttackLight,
    AttackDark,
    WallBreak,
    Lose
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Fuente para música (loop).")]
    public AudioSource musicSource;

    [Tooltip("Fuente para efectos (one-shot).")]
    public AudioSource sfxSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("SFX Clips")]
    [Tooltip("Lista de SFX asignables desde el Inspector.")]
    public List<SfxEntry> sfxEntries = new List<SfxEntry>();

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    private Dictionary<SfxId, AudioClip> sfxMap;

    [System.Serializable]
    public class SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //Validaciones

        if (musicSource == null || sfxSource == null)
            Debug.LogError("AudioManager: Asigna musicSource y sfxSource en el Inspector.");

        //Configurar Fuentes
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        BuildSfxMap();
    }

    private void BuildSfxMap()
    {
        sfxMap = new Dictionary<SfxId, AudioClip>();

        foreach (var entry in sfxEntries)
        {
            if (entry.clip == null) continue;

            //Si hay duplicados, el ultimo gana

            sfxMap[entry.id] = entry.clip;
        }
    }

    // ---------- MUSIC ----------


    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) return;
        if (clip == null) return;

        //Si ya esta sonando esa misma musica, no reiniciar

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    // ---------- SFX ----------

    /// Reproduce un sonido SFX una vez, sin cortar otros sonidos (OneShot).

    public void PlaySFX(SfxId id, float volumeMulltiplier = 1f)
    {
        if (sfxSource == null) return;

        if (sfxMap == null) BuildSfxMap();

        if(sfxMap.TryGetValue(id, out AudioClip clip) && clip != null)
        {
            float vol = Mathf.Clamp01(sfxVolume * volumeMulltiplier);
            sfxSource.PlayOneShot(clip, vol);
        }

        // Si no hay clip asignado, no hace nada (no rompe).
    }

    public void PlayMove() => PlaySFX(SfxId.Move,0.8f);

    public void PlayOrbPickup(Orb.OrbType type)
    {
        switch (type)
        {
            case Orb.OrbType.Light: PlaySFX(SfxId.OrbPickupLight); break;
            case Orb.OrbType.Dark: PlaySFX(SfxId.OrbPickupDark); break;
            case Orb.OrbType.Dual: PlaySFX(SfxId.OrbPickupDual); break;
        }
    }

    public void PlayAttack(ElementType type)
    {
        if (type == ElementType.Light) PlaySFX(SfxId.AttackLight);
        else PlaySFX(SfxId.AttackDark);
    }

    public void PlayWallBreak(int count)
    {
        // Si rompe varias paredes, puedes subir el volumen un poco
        float mult = Mathf.Clamp01(1f + (count - 1) * 0.05f);
        PlaySFX(SfxId.WallBreak, mult);
    }

    public void PlayLose() => PlaySFX(SfxId.Lose, 1f);

    // Ajustes de volumen en runtime (opcional)
    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }
}
