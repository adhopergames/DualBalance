using System;
using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// Controlador central de anuncios de Dual Balance.
///
/// REWARDED:
/// - Permite al jugador revivir.
/// - La recompensa se registra cuando Google la concede.
/// - El jugador revive SOLO después de cerrar completamente el anuncio.
///
/// INTERSTITIAL:
/// - Se utiliza entre runs.
/// - Aparece aproximadamente cada 6–7 runs.
/// - Si el jugador utilizó un Rewarded en esa run,
///   el Interstitial se omite para no mostrar dos anuncios seguidos.
///
/// GENERAL:
/// - Se mantiene vivo entre escenas.
/// - Inicializa Google Mobile Ads una sola vez.
/// - Precarga Rewarded e Interstitial.
/// - Recarga cada anuncio después de utilizarlo.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    // ============================================================
    // REWARDED IDS
    // ============================================================

    [Header("AdMob Rewarded IDs")]

    [SerializeField]
    private string androidRewardedId =
        "ca-app-pub-7911615200205097/2013465959";

    [SerializeField]
    private string iosRewardedId = "";

    // ============================================================
    // INTERSTITIAL IDS
    // ============================================================

    [Header("AdMob Interstitial IDs")]

    [Tooltip(
        "ID REAL del Interstitial de Android. " +
        "Créalo en AdMob antes de publicar."
    )]
    [SerializeField]
    private string androidInterstitialId = "";

    [Tooltip(
        "ID REAL del Interstitial de iOS."
    )]
    [SerializeField]
    private string iosInterstitialId = "";

    // ============================================================
    // TESTING
    // ============================================================

    [Header("Testing")]

    [Tooltip(
        "Mientras esté activo se utilizan los IDs oficiales " +
        "de prueba de Google."
    )]
    [SerializeField]
    private bool useTestAds = true;

    // ID oficial de prueba Rewarded Android.
    private const string AndroidRewardedTestId =
        "ca-app-pub-3940256099942544/5224354917";

    // ID oficial de prueba Rewarded iOS.
    private const string IOSRewardedTestId =
        "ca-app-pub-3940256099942544/1712485313";

    // ID oficial de prueba Interstitial Android.
    private const string AndroidInterstitialTestId =
        "ca-app-pub-3940256099942544/1033173712";

    // ID oficial de prueba Interstitial iOS.
    private const string IOSInterstitialTestId =
        "ca-app-pub-3940256099942544/4411468910";

    // ============================================================
    // INTERSTITIAL FREQUENCY
    // ============================================================

    [Header("Interstitial Frequency")]

    [Tooltip(
        "Mínimo de runs entre Interstitials."
    )]
    [Min(1)]
    [SerializeField]
    private int minRunsBetweenInterstitial = 6;

    [Tooltip(
        "Máximo de runs entre Interstitials."
    )]
    [Min(1)]
    [SerializeField]
    private int maxRunsBetweenInterstitial = 7;

    /*
     * Guardamos estos valores en PlayerPrefs para que cerrar
     * y volver a abrir la aplicación NO reinicie el contador.
     */
    private const string InterstitialRunCountKey =
        "ADS_INTERSTITIAL_RUN_COUNT";

    private const string InterstitialNextRunKey =
        "ADS_INTERSTITIAL_NEXT_RUN";

    private int runsSinceInterstitial;
    private int nextInterstitialRun;

    // ============================================================
    // AD OBJECTS
    // ============================================================

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;

    private string rewardedAdUnitId;
    private string interstitialAdUnitId;

    // ============================================================
    // STATE
    // ============================================================

    private bool isInitialized;

    private bool isLoadingRewarded;
    private bool isLoadingInterstitial;

    /*
     * True mientras existe un anuncio fullscreen en pantalla.
     * Evita intentar abrir dos anuncios simultáneamente.
     */
    private bool isFullScreenAdShowing;

    /*
     * Se vuelve true cuando Google confirma que el jugador
     * ganó la recompensa.
     *
     * IMPORTANTE:
     * Esto NO significa que el anuncio haya sido cerrado todavía.
     */
    private bool rewardEarned;

    /*
     * Callback que se ejecutará después de cerrar el Interstitial.
     *
     * GameManager lo utiliza para continuar con el Retry
     * únicamente cuando el anuncio haya desaparecido.
     */
    private Action interstitialFinishedCallback;

    /*
     * Nos permite saber si el Interstitial realmente llegó
     * a abrirse antes de resetear el contador.
     */
    private bool interstitialWasOpened;

    // ============================================================
    // PUBLIC PROPERTIES
    // ============================================================

    public bool IsRewardedReady =>
        isInitialized &&
        rewardedAd != null &&
        rewardedAd.CanShowAd();

    public bool IsInterstitialReady =>
        isInitialized &&
        interstitialAd != null &&
        interstitialAd.CanShowAd();

    public bool IsFullScreenAdShowing =>
        isFullScreenAdShowing;

    public int RunsSinceInterstitial =>
        runsSinceInterstitial;

    public int NextInterstitialRun =>
        nextInterstitialRun;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        // --------------------------------------------------------
        // Singleton
        // --------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        // --------------------------------------------------------
        // Configuración
        // --------------------------------------------------------

        SelectAdUnitIds();

        LoadInterstitialSchedule();

        // --------------------------------------------------------
        // AdMob solo se inicializa en plataformas compatibles.
        // --------------------------------------------------------

        if (AdsSupportedOnThisPlatform())
        {
            InitializeAds();
        }
        else
        {
            Debug.Log(
                "AdMob: plataforma sin anuncios móviles. " +
                "AdManager permanecerá inactivo."
            );
        }
    }

    // ============================================================
    // PLATFORM
    // ============================================================

    /// <summary>
    /// Evita intentar utilizar AdMob en una futura build nativa
    /// de Windows/macOS.
    ///
    /// En Editor lo dejamos activo para poder utilizar
    /// los anuncios de prueba.
    /// </summary>
    private bool AdsSupportedOnThisPlatform()
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        return true;
#else
        return false;
#endif
    }

    // ============================================================
    // AD UNIT IDS
    // ============================================================

    /// <summary>
    /// Selecciona automáticamente los IDs correspondientes
    /// a Android/iOS y decide entre TEST y PRODUCCIÓN.
    /// </summary>
    private void SelectAdUnitIds()
    {
#if UNITY_ANDROID

        rewardedAdUnitId =
            useTestAds
                ? AndroidRewardedTestId
                : androidRewardedId;

        interstitialAdUnitId =
            useTestAds
                ? AndroidInterstitialTestId
                : androidInterstitialId;

#elif UNITY_IOS

        rewardedAdUnitId =
            useTestAds
                ? IOSRewardedTestId
                : iosRewardedId;

        interstitialAdUnitId =
            useTestAds
                ? IOSInterstitialTestId
                : iosInterstitialId;

#else

        /*
         * En Editor usamos los IDs de prueba Android.
         */
        rewardedAdUnitId =
            AndroidRewardedTestId;

        interstitialAdUnitId =
            AndroidInterstitialTestId;

#endif
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    /// <summary>
    /// Inicializa Google Mobile Ads UNA SOLA VEZ.
    ///
    /// Cuando termina, precargamos:
    /// - Rewarded
    /// - Interstitial
    /// </summary>
    private void InitializeAds()
    {
        if (isInitialized)
            return;

        MobileAds.Initialize(
            initializationStatus =>
            {
                isInitialized = true;

                Debug.Log(
                    "AdMob: inicialización completada."
                );

                LoadRewarded();
                LoadInterstitial();
            }
        );
    }

    // ============================================================
    // REWARDED - LOAD
    // ============================================================

    /// <summary>
    /// Precarga un anuncio recompensado.
    /// </summary>
    public void LoadRewarded()
    {
        if (!isInitialized)
            return;

        if (isLoadingRewarded)
            return;

        if (
            rewardedAd != null &&
            rewardedAd.CanShowAd()
        )
        {
            return;
        }

        isLoadingRewarded = true;

        DestroyRewardedAd();

        AdRequest request =
            new AdRequest();

        RewardedAd.Load(
            rewardedAdUnitId,
            request,
            (RewardedAd ad, LoadAdError error) =>
            {
                isLoadingRewarded = false;

                if (
                    error != null ||
                    ad == null
                )
                {
                    Debug.LogError(
                        $"AdMob: Rewarded no pudo cargar. {error}"
                    );

                    rewardedAd = null;

                    return;
                }

                rewardedAd = ad;

                RegisterRewardedEvents(
                    rewardedAd
                );

                Debug.Log(
                    "AdMob: Rewarded cargado correctamente."
                );
            }
        );
    }

    // ============================================================
    // REWARDED - SHOW
    // ============================================================

    /// <summary>
    /// Muestra el anuncio de segunda oportunidad.
    ///
    /// La recompensa se registra aquí,
    /// pero el revive NO ocurre hasta que el anuncio cierre.
    /// </summary>
    public void ShowRewarded()
    {
        if (isFullScreenAdShowing)
        {
            Debug.LogWarning(
                "AdMob: ya existe un anuncio fullscreen abierto."
            );

            return;
        }

        if (!IsRewardedReady)
        {
            Debug.LogWarning(
                "AdMob: Rewarded todavía no está disponible."
            );

            LoadRewarded();

            return;
        }

        rewardEarned = false;

        rewardedAd.Show(
            reward =>
            {
                /*
                 * Google confirmó la recompensa.
                 *
                 * NO revivimos todavía.
                 */
                rewardEarned = true;

                Debug.Log(
                    $"AdMob: recompensa recibida. " +
                    $"Tipo: {reward.Type}, " +
                    $"cantidad: {reward.Amount}. " +
                    $"Esperando cierre del anuncio..."
                );
            }
        );
    }

    // ============================================================
    // REWARDED - EVENTS
    // ============================================================

    private void RegisterRewardedEvents(
        RewardedAd ad
    )
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            isFullScreenAdShowing = true;

            /*
             * Pausamos el audio del juego mientras
             * el anuncio está en pantalla.
             */
            AudioListener.pause = true;

            Debug.Log(
                "AdMob: Rewarded abierto."
            );
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            isFullScreenAdShowing = false;

            AudioListener.pause = false;

            Debug.Log(
                "AdMob: Rewarded cerrado."
            );

            /*
             * Revivimos únicamente si:
             *
             * 1. Google concedió la recompensa.
             * 2. El anuncio ya cerró completamente.
             */
            if (rewardEarned)
            {
                rewardEarned = false;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance
                        .ContinueAfterAd();
                }
                else
                {
                    Debug.LogError(
                        "AdMob: no se encontró GameManager.Instance."
                    );
                }
            }
            else
            {
                Debug.Log(
                    "AdMob: Rewarded cerrado sin recompensa."
                );
            }

            /*
             * Un Rewarded solo puede utilizarse una vez.
             * Destruimos el anterior y cargamos otro.
             */
            DestroyRewardedAd();

            LoadRewarded();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            isFullScreenAdShowing = false;
            rewardEarned = false;

            AudioListener.pause = false;

            Debug.LogError(
                $"AdMob: Rewarded falló al mostrarse. {error}"
            );

            DestroyRewardedAd();

            LoadRewarded();
        };
    }

    // ============================================================
    // INTERSTITIAL - LOAD
    // ============================================================

    /// <summary>
    /// Precarga un Interstitial para que esté preparado
    /// cuando lleguemos al número de runs requerido.
    /// </summary>
    public void LoadInterstitial()
    {
        if (!isInitialized)
            return;

        if (isLoadingInterstitial)
            return;

        if (
            interstitialAd != null &&
            interstitialAd.CanShowAd()
        )
        {
            return;
        }

        /*
         * Si estamos en producción y todavía no configuraste
         * un ID real, evitamos hacer una petición inválida.
         */
        if (
            !useTestAds &&
            string.IsNullOrWhiteSpace(
                interstitialAdUnitId
            )
        )
        {
            Debug.LogWarning(
                "AdMob: falta configurar el ID REAL del Interstitial."
            );

            return;
        }

        isLoadingInterstitial = true;

        DestroyInterstitialAd();

        AdRequest request =
            new AdRequest();

        InterstitialAd.Load(
            interstitialAdUnitId,
            request,
            (InterstitialAd ad, LoadAdError error) =>
            {
                isLoadingInterstitial = false;

                if (
                    error != null ||
                    ad == null
                )
                {
                    Debug.LogError(
                        $"AdMob: Interstitial no pudo cargar. {error}"
                    );

                    interstitialAd = null;

                    return;
                }

                interstitialAd = ad;

                RegisterInterstitialEvents(
                    interstitialAd
                );

                Debug.Log(
                    "AdMob: Interstitial cargado correctamente."
                );
            }
        );
    }

    // ============================================================
    // INTERSTITIAL - RUN COUNTER
    // ============================================================

    /// <summary>
    /// Registra que una run terminó.
    ///
    /// IMPORTANTE:
    /// GameManager se asegura de llamarlo UNA sola vez
    /// por run, aunque el jugador posteriormente reviva.
    /// </summary>
    public void RegisterCompletedRun()
    {
        runsSinceInterstitial++;

        SaveInterstitialSchedule();

        Debug.Log(
            $"AdMob: run registrada. " +
            $"{runsSinceInterstitial}/{nextInterstitialRun} " +
            $"para próximo Interstitial."
        );
    }

    /// <summary>
    /// Comprueba si corresponde mostrar un Interstitial.
    ///
    /// Si se muestra:
    /// - espera a que cierre;
    /// - después ejecuta onFinished.
    ///
    /// Si NO corresponde mostrarlo:
    /// - ejecuta onFinished inmediatamente.
    ///
    /// rewardedUsedThisRun evita mostrar un Interstitial
    /// justo después de una run donde el jugador ya vio un Rewarded.
    /// </summary>
    public bool TryShowInterstitialIfDue(
        bool rewardedUsedThisRun,
        Action onFinished
    )
    {
        // --------------------------------------------------------
        // Plataforma sin AdMob.
        // --------------------------------------------------------

        if (!AdsSupportedOnThisPlatform())
        {
            onFinished?.Invoke();
            return false;
        }

        // --------------------------------------------------------
        // Todavía no llegamos al número de runs.
        // --------------------------------------------------------

        if (
            runsSinceInterstitial <
            nextInterstitialRun
        )
        {
            onFinished?.Invoke();
            return false;
        }

        // --------------------------------------------------------
        // El jugador ya vio un Rewarded en esta run.
        //
        // NO reseteamos el contador.
        // El Interstitial queda pendiente para la próxima run
        // donde no haya Rewarded.
        // --------------------------------------------------------

        if (rewardedUsedThisRun)
        {
            Debug.Log(
                "AdMob: Interstitial omitido porque " +
                "esta run ya utilizó Rewarded."
            );

            onFinished?.Invoke();
            return false;
        }

        // --------------------------------------------------------
        // Ya existe otro fullscreen.
        // --------------------------------------------------------

        if (isFullScreenAdShowing)
        {
            onFinished?.Invoke();
            return false;
        }

        // --------------------------------------------------------
        // El anuncio todavía no está listo.
        //
        // No reseteamos el contador.
        // Volveremos a intentarlo en la próxima oportunidad.
        // --------------------------------------------------------

        if (!IsInterstitialReady)
        {
            Debug.LogWarning(
                "AdMob: toca Interstitial, pero aún no está cargado. " +
                "Se intentará nuevamente en otra run."
            );

            LoadInterstitial();

            onFinished?.Invoke();
            return false;
        }

        // --------------------------------------------------------
        // Mostrar.
        // --------------------------------------------------------

        interstitialFinishedCallback =
            onFinished;

        interstitialWasOpened = false;

        Debug.Log(
            "AdMob: mostrando Interstitial periódico."
        );

        interstitialAd.Show();

        return true;
    }

    // ============================================================
    // INTERSTITIAL - EVENTS
    // ============================================================

    private void RegisterInterstitialEvents(
        InterstitialAd ad
    )
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            isFullScreenAdShowing = true;

            interstitialWasOpened = true;

            AudioListener.pause = true;

            /*
             * Solo ahora consideramos que el anuncio realmente
             * fue mostrado.
             *
             * Reiniciamos el contador y elegimos otro objetivo
             * aleatorio entre 6 y 7.
             */
            ResetInterstitialCounter();

            Debug.Log(
                "AdMob: Interstitial abierto."
            );
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            isFullScreenAdShowing = false;

            AudioListener.pause = false;

            Debug.Log(
                "AdMob: Interstitial cerrado."
            );

            FinishInterstitialFlow();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            isFullScreenAdShowing = false;

            AudioListener.pause = false;

            Debug.LogError(
                $"AdMob: Interstitial falló al mostrarse. {error}"
            );

            /*
             * Si nunca llegó a abrirse,
             * NO reseteamos el contador.
             */
            if (!interstitialWasOpened)
            {
                Debug.Log(
                    "AdMob: el contador del Interstitial " +
                    "permanece pendiente."
                );
            }

            FinishInterstitialFlow();
        };
    }

    /// <summary>
    /// Limpieza común cuando el Interstitial termina.
    ///
    /// Recarga el siguiente anuncio y permite que
    /// GameManager continúe con el Retry.
    /// </summary>
    private void FinishInterstitialFlow()
    {
        DestroyInterstitialAd();

        LoadInterstitial();

        Action callback =
            interstitialFinishedCallback;

        interstitialFinishedCallback = null;

        interstitialWasOpened = false;

        callback?.Invoke();
    }

    // ============================================================
    // INTERSTITIAL - PERSISTENCE
    // ============================================================

    /// <summary>
    /// Recupera el contador entre sesiones.
    ///
    /// Así cerrar la aplicación no reinicia el ciclo
    /// de los 6–7 runs.
    /// </summary>
    private void LoadInterstitialSchedule()
    {
        int safeMin =
            Mathf.Max(
                1,
                Mathf.Min(
                    minRunsBetweenInterstitial,
                    maxRunsBetweenInterstitial
                )
            );

        int safeMax =
            Mathf.Max(
                safeMin,
                Mathf.Max(
                    minRunsBetweenInterstitial,
                    maxRunsBetweenInterstitial
                )
            );

        runsSinceInterstitial =
            PlayerPrefs.GetInt(
                InterstitialRunCountKey,
                0
            );

        nextInterstitialRun =
            PlayerPrefs.GetInt(
                InterstitialNextRunKey,
                0
            );

        /*
         * Primera ejecución o configuración inválida.
         */
        if (
            nextInterstitialRun < safeMin ||
            nextInterstitialRun > safeMax
        )
        {
            nextInterstitialRun =
                UnityEngine.Random.Range(
                    safeMin,
                    safeMax + 1
                );

            SaveInterstitialSchedule();
        }

        Debug.Log(
            $"AdMob: próximo Interstitial en " +
            $"{nextInterstitialRun} runs. " +
            $"Actual: {runsSinceInterstitial}."
        );
    }

    /// <summary>
    /// Se ejecuta únicamente después de que un
    /// Interstitial realmente abrió.
    /// </summary>
    private void ResetInterstitialCounter()
    {
        runsSinceInterstitial = 0;

        int safeMin =
            Mathf.Max(
                1,
                Mathf.Min(
                    minRunsBetweenInterstitial,
                    maxRunsBetweenInterstitial
                )
            );

        int safeMax =
            Mathf.Max(
                safeMin,
                Mathf.Max(
                    minRunsBetweenInterstitial,
                    maxRunsBetweenInterstitial
                )
            );

        nextInterstitialRun =
            UnityEngine.Random.Range(
                safeMin,
                safeMax + 1
            );

        SaveInterstitialSchedule();

        Debug.Log(
            $"AdMob: próximo Interstitial en " +
            $"{nextInterstitialRun} runs."
        );
    }

    private void SaveInterstitialSchedule()
    {
        PlayerPrefs.SetInt(
            InterstitialRunCountKey,
            runsSinceInterstitial
        );

        PlayerPrefs.SetInt(
            InterstitialNextRunKey,
            nextInterstitialRun
        );

        PlayerPrefs.Save();
    }

    // ============================================================
    // DESTROY REWARDED
    // ============================================================

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null)
            return;

        rewardedAd.Destroy();

        rewardedAd = null;
    }

    // ============================================================
    // DESTROY INTERSTITIAL
    // ============================================================

    private void DestroyInterstitialAd()
    {
        if (interstitialAd == null)
            return;

        interstitialAd.Destroy();

        interstitialAd = null;
    }

    // ============================================================
    // DESTROY MANAGER
    // ============================================================

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        AudioListener.pause = false;

        DestroyRewardedAd();
        DestroyInterstitialAd();

        Instance = null;
    }
}