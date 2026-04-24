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
    Lose,

    UIButton,
    UIBack
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

        if (musicSource == null || sfxSource == null)
            Debug.LogError("AudioManager: Asigna musicSource y sfxSource en el Inspector.");

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

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    // ---------- SFX ----------

    public void PlaySFX(SfxId id, float volumeMultiplier = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (sfxSource == null) return;

        if (sfxMap == null) BuildSfxMap();

        if (sfxMap.TryGetValue(id, out AudioClip clip) && clip != null)
        {
            float vol = Mathf.Clamp01(sfxVolume * volumeMultiplier);
            float randomPitch = Random.Range(pitchMin, pitchMax);

            sfxSource.pitch = randomPitch;
            sfxSource.PlayOneShot(clip, vol);
            sfxSource.pitch = 1f;
        }
    }

    public void PlayMove()
    {
        float randomVolume = Random.Range(0.80f, 0.90f);
        PlaySFX(SfxId.Move, randomVolume, 0.95f, 1.05f);
    }

    public void PlayOrbPickup(Orb.OrbType type)
    {
        switch (type)
        {
            case Orb.OrbType.Light:
                PlaySFX(SfxId.OrbPickupLight, 0.90f, 0.97f, 1.03f);
                break;

            case Orb.OrbType.Dark:
                PlaySFX(SfxId.OrbPickupDark, 0.90f, 0.97f, 1.03f);
                break;

            case Orb.OrbType.Dual:
                PlaySFX(SfxId.OrbPickupDual, 0.95f, 0.98f, 1.02f);
                break;
        }
    }

    public void PlayAttack(ElementType type)
    {
        if (type == ElementType.Light)
            PlaySFX(SfxId.AttackLight, 1.05f, 0.98f, 1.03f);
        else
            PlaySFX(SfxId.AttackDark, 1.05f, 0.98f, 1.03f);
    }

    public void PlayWallBreak(int count)
    {
        // Más suave para no tapar el ataque
        float mult = 0.45f + Mathf.Min((count - 1) * 0.04f, 0.12f);
        PlaySFX(SfxId.WallBreak, mult, 0.96f, 1.04f);
    }

    public void PlayLose() => PlaySFX(SfxId.Lose, 1f);
    public void PlayUIButton() => PlaySFX(SfxId.UIButton, 1f);
    public void PlayUIBack() => PlaySFX(SfxId.UIBack, 1f);

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

    public void StopAllSFX()
    {
        if (sfxSource == null) return;

        sfxSource.Stop();
    }
}