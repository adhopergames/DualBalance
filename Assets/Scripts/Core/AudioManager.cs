using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("Music Base Levels")]
    [Tooltip("Volumen base de la música del menú.")]
    [Range(0f, 1f)]
    [SerializeField] private float menuMusicMultiplier = 1f;

    [Tooltip("Volumen base de la música del gameplay.")]
    [Range(0f, 1f)]
    [SerializeField] private float gameMusicMultiplier = 0.65f;

    [Header("Scene Names")]
    [Tooltip("Nombre exacto de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Tooltip("Nombre exacto de la escena de gameplay.")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Music Fade")]
    [Tooltip("Duración del fade-out de la música actual.")]
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Tooltip("Duración del fade-in de la nueva música.")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.55f;

    [Header("SFX Clips")]
    [SerializeField] private List<SfxEntry> sfxEntries = new List<SfxEntry>();

    [Header("General Volumes")]
    [Tooltip("Volumen general de toda la música. Puede modificarse con el slider.")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.7f;

    [Tooltip("Volumen general de los efectos de sonido.")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private Dictionary<SfxId, AudioClip> sfxMap;
    private Coroutine musicTransitionCoroutine;

    /*
     * Guarda el multiplicador de la canción actual.
     * Esto permite que el slider general siga respetando el volumen
     * diferente del menú y del gameplay.
     */
    private float currentMusicMultiplier = 1f;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(
            SceneManager.GetActiveScene().name,
            true
        );
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name, true);
    }

    private void PlayMusicForScene(string sceneName, bool useFade)
    {
        if (sceneName == mainMenuSceneName)
        {
            PlayMusic(
                menuMusic,
                menuMusicMultiplier,
                useFade
            );

            return;
        }

        if (sceneName == gameSceneName)
        {
            PlayMusic(
                gameMusic,
                gameMusicMultiplier,
                useFade
            );

            return;
        }

        Debug.Log(
            $"AudioManager: la escena '{sceneName}' no tiene música configurada."
        );
    }

    public void PlayMenuMusic()
    {
        PlayMusic(
            menuMusic,
            menuMusicMultiplier,
            true
        );
    }

    public void PlayGameMusic()
    {
        PlayMusic(
            gameMusic,
            gameMusicMultiplier,
            true
        );
    }

    private void PlayMusic(
        AudioClip clip,
        float volumeMultiplier,
        bool useFade
    )
    {
        if (musicSource == null)
        {
            Debug.LogWarning(
                "AudioManager: falta asignar Music Source."
            );

            return;
        }

        if (clip == null)
        {
            Debug.LogWarning(
                "AudioManager: el AudioClip de música es nulo."
            );

            return;
        }

        volumeMultiplier = Mathf.Clamp01(volumeMultiplier);

        /*
         * Si ya está reproduciendo la misma canción,
         * actualizamos su multiplicador sin reiniciarla.
         */
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            currentMusicMultiplier = volumeMultiplier;
            musicSource.volume = GetCurrentTargetMusicVolume();
            return;
        }

        StopMusicTransition();

        if (useFade)
        {
            musicTransitionCoroutine = StartCoroutine(
                ChangeMusicWithFadeRoutine(
                    clip,
                    volumeMultiplier
                )
            );
        }
        else
        {
            StartMusicImmediately(
                clip,
                volumeMultiplier
            );
        }
    }

    private void StartMusicImmediately(
        AudioClip clip,
        float volumeMultiplier
    )
    {
        currentMusicMultiplier = Mathf.Clamp01(
            volumeMultiplier
        );

        musicSource.Stop();
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = GetCurrentTargetMusicVolume();
        musicSource.Play();
    }

    private IEnumerator ChangeMusicWithFadeRoutine(
        AudioClip newClip,
        float newVolumeMultiplier
    )
    {
        /*
         * FADE OUT
         */
        if (musicSource.isPlaying && fadeOutDuration > 0f)
        {
            float initialVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / fadeOutDuration
                );

                musicSource.volume = Mathf.Lerp(
                    initialVolume,
                    0f,
                    progress
                );

                yield return null;
            }
        }

        /*
         * Cambio de canción.
         */
        currentMusicMultiplier = Mathf.Clamp01(
            newVolumeMultiplier
        );

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.Play();

        /*
         * FADE IN
         */
        float targetVolume = GetCurrentTargetMusicVolume();

        if (fadeInDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / fadeInDuration
                );

                /*
                 * Se vuelve a calcular para respetar cambios
                 * del slider durante el fade.
                 */
                targetVolume = GetCurrentTargetMusicVolume();

                musicSource.volume = Mathf.Lerp(
                    0f,
                    targetVolume,
                    progress
                );

                yield return null;
            }
        }

        musicSource.volume = GetCurrentTargetMusicVolume();
        musicTransitionCoroutine = null;
    }

    private float GetCurrentTargetMusicVolume()
    {
        return Mathf.Clamp01(
            musicVolume * currentMusicMultiplier
        );
    }

    public void FadeOutCurrentMusic()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        StopMusicTransition();

        musicTransitionCoroutine = StartCoroutine(
            FadeOutRoutine()
        );
    }

    private IEnumerator FadeOutRoutine()
    {
        float initialVolume = musicSource.volume;
        float elapsed = 0f;

        if (fadeOutDuration <= 0f)
        {
            musicSource.Stop();
            musicSource.clip = null;
            musicSource.volume = GetCurrentTargetMusicVolume();

            musicTransitionCoroutine = null;
            yield break;
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / fadeOutDuration
            );

            musicSource.volume = Mathf.Lerp(
                initialVolume,
                0f,
                progress
            );

            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = GetCurrentTargetMusicVolume();

        musicTransitionCoroutine = null;
    }

    private void StopMusicTransition()
    {
        if (musicTransitionCoroutine == null)
            return;

        StopCoroutine(musicTransitionCoroutine);
        musicTransitionCoroutine = null;
    }

    private void LoadVolumes()
    {
        musicVolume = PlayerPrefs.GetFloat(
            MusicVolumeKey,
            musicVolume
        );

        sfxVolume = PlayerPrefs.GetFloat(
            SfxVolumeKey,
            sfxVolume
        );
    }

    private void ConfigureSources()
    {
        if (musicSource == null)
        {
            Debug.LogError(
                "AudioManager: falta asignar Music Source."
            );
        }
        else
        {
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.mute = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = GetCurrentTargetMusicVolume();
        }

        if (sfxSource == null)
        {
            Debug.LogError(
                "AudioManager: falta asignar Sfx Source."
            );
        }
        else
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

        foreach (SfxEntry entry in sfxEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            sfxMap[entry.id] = entry.clip;
        }
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicSource != null)
        {
            /*
             * Volumen general × volumen base
             * de la música que está sonando.
             */
            musicSource.volume = GetCurrentTargetMusicVolume();
        }

        PlayerPrefs.SetFloat(
            MusicVolumeKey,
            musicVolume
        );

        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        PlayerPrefs.SetFloat(
            SfxVolumeKey,
            sfxVolume
        );

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

    public void PlaySFX(
        SfxId id,
        float volumeMultiplier = 1f,
        float pitchMin = 1f,
        float pitchMax = 1f
    )
    {
        if (sfxSource == null)
            return;

        if (sfxMap == null)
            BuildSfxMap();

        if (!sfxMap.TryGetValue(id, out AudioClip clip))
            return;

        if (clip == null)
            return;

        float volume = Mathf.Clamp01(
            sfxVolume * volumeMultiplier
        );

        float minimumPitch = Mathf.Min(
            pitchMin,
            pitchMax
        );

        float maximumPitch = Mathf.Max(
            pitchMin,
            pitchMax
        );

        float randomPitch = Random.Range(
            minimumPitch,
            maximumPitch
        );

        sfxSource.pitch = randomPitch;
        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = 1f;
    }

    public void PlayMove()
    {
        PlaySFX(
            SfxId.Move,
            Random.Range(0.80f, 0.90f),
            0.95f,
            1.05f
        );
    }

    public void PlayOrbPickup(Orb.OrbType type)
    {
        switch (type)
        {
            case Orb.OrbType.Light:
                PlaySFX(
                    SfxId.OrbPickupLight,
                    0.90f,
                    0.97f,
                    1.03f
                );
                break;

            case Orb.OrbType.Dark:
                PlaySFX(
                    SfxId.OrbPickupDark,
                    0.90f,
                    0.97f,
                    1.03f
                );
                break;

            case Orb.OrbType.Dual:
                PlaySFX(
                    SfxId.OrbPickupDual,
                    0.95f,
                    0.98f,
                    1.02f
                );
                break;
        }
    }

    public void PlayAttack(ElementType type)
    {
        if (type == ElementType.Light)
        {
            PlaySFX(
                SfxId.AttackLight,
                1.05f,
                0.98f,
                1.03f
            );
        }
        else
        {
            PlaySFX(
                SfxId.AttackDark,
                1.05f,
                0.98f,
                1.03f
            );
        }
    }

    public void PlayWallBreak(int count)
    {
        int safeCount = Mathf.Max(1, count);

        float multiplier =
            0.45f +
            Mathf.Min(
                (safeCount - 1) * 0.04f,
                0.12f
            );

        PlaySFX(
            SfxId.WallBreak,
            multiplier,
            0.96f,
            1.04f
        );
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
        if (sfxSource == null)
            return;

        sfxSource.Stop();
        sfxSource.pitch = 1f;
    }

    public void StopMusic()
    {
        StopMusicTransition();

        if (musicSource == null)
            return;

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = GetCurrentTargetMusicVolume();
    }
}