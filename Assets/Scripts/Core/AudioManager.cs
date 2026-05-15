using System.Collections;
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

    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Music Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Auto Play")]
    [SerializeField] private bool playMenuMusicOnStart = true;

    [Header("Fade")]
    [SerializeField] private float musicFadeDuration = 0.8f;

    [Header("SFX Clips")]
    public List<SfxEntry> sfxEntries = new List<SfxEntry>();

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private Dictionary<SfxId, AudioClip> sfxMap;
    private Coroutine fadeCoroutine;

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

        LoadVolumes();
        ConfigureSources();
        BuildSfxMap();
    }

    private void Start()
    {
        if (playMenuMusicOnStart)
            PlayMenuMusic();
    }

    private void LoadVolumes()
    {
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
    }

    private void ConfigureSources()
    {
        if (musicSource == null)
            Debug.LogError("AudioManager: falta asignar Music Source.");

        if (sfxSource == null)
            Debug.LogError("AudioManager: falta asignar Sfx Source.");

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.mute = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.mute = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = sfxVolume;
        }
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

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    private void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void FadeOutCurrentMusic()
    {
        if (musicSource == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / musicFadeDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = musicVolume;
        fadeCoroutine = null;
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);

        if (musicSource != null)
            musicSource.volume = musicVolume;

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSfxVolume()
    {
        return sfxVolume;
    }

    public void PlaySFX(SfxId id, float volumeMultiplier = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (sfxSource == null) return;

        if (sfxMap == null)
            BuildSfxMap();

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
        PlaySFX(SfxId.Move, Random.Range(0.80f, 0.90f), 0.95f, 1.05f);
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
        float mult = 0.45f + Mathf.Min((count - 1) * 0.04f, 0.12f);
        PlaySFX(SfxId.WallBreak, mult, 0.96f, 1.04f);
    }

    public void PlayLose()
    {
        PlaySFX(SfxId.Lose, 1f);
    }

    public void PlayUIButton()
    {
        PlaySFX(SfxId.UIButton, 1f);
    }

    public void PlayUIBack()
    {
        PlaySFX(SfxId.UIBack, 1f);
    }

    public void StopAllSFX()
    {
        if (sfxSource == null) return;
        sfxSource.Stop();
    }
}