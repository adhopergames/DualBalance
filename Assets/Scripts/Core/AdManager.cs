using GoogleMobileAds.Api;
using UnityEngine;

/// <summary>
/// Maneja el anuncio recompensado de AdMob.
///
/// - Se mantiene entre escenas.
/// - Inicializa Google Mobile Ads una sola vez.
/// - Precarga el anuncio recompensado.
/// - Revive al jugador únicamente cuando recibe la recompensa.
/// - Recarga un anuncio nuevo después de cerrarlo.
/// </summary>
public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    [Header("AdMob Rewarded IDs")]
    [SerializeField]
    private string androidRewardedId =
        "ca-app-pub-7911615200205097/2013465959";

    [SerializeField]
    private string iosRewardedId = "";

    [Header("Testing")]
    [Tooltip("Usa el ID oficial de prueba de Google mientras desarrollas.")]
    [SerializeField]
    private bool useTestAds = true;

    private const string AndroidRewardedTestId =
        "ca-app-pub-3940256099942544/5224354917";

    private RewardedAd rewardedAd;
    private string rewardedAdUnitId;

    private bool isInitialized;
    private bool isLoading;

    public bool IsRewardedReady =>
        isInitialized &&
        rewardedAd != null &&
        rewardedAd.CanShowAd();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SelectAdUnitId();
        InitializeAds();
    }

    private void SelectAdUnitId()
    {
#if UNITY_ANDROID
        rewardedAdUnitId = useTestAds
            ? AndroidRewardedTestId
            : androidRewardedId;
#elif UNITY_IOS
        rewardedAdUnitId = iosRewardedId;
#else
        rewardedAdUnitId = AndroidRewardedTestId;
#endif
    }

    private void InitializeAds()
    {
        if (isInitialized)
        {
            return;
        }

        MobileAds.Initialize(initializationStatus =>
        {
            isInitialized = true;

            Debug.Log("AdMob: inicialización completada.");

            LoadRewarded();
        });
    }

    public void LoadRewarded()
    {
        if (!isInitialized)
        {
            return;
        }

        if (isLoading)
        {
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            return;
        }

        isLoading = true;

        DestroyRewardedAd();

        AdRequest request = new AdRequest();

        RewardedAd.Load(
            rewardedAdUnitId,
            request,
            (RewardedAd ad, LoadAdError error) =>
            {
                isLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogError(
                        $"AdMob: rewarded no pudo cargar. {error}"
                    );

                    rewardedAd = null;
                    return;
                }

                rewardedAd = ad;

                RegisterRewardedEvents(rewardedAd);

                Debug.Log("AdMob: rewarded cargado correctamente.");
            }
        );
    }

    public void ShowRewarded()
    {
        if (!IsRewardedReady)
        {
            Debug.LogWarning(
                "AdMob: el rewarded todavía no está disponible."
            );

            LoadRewarded();
            return;
        }

        rewardedAd.Show(reward =>
        {
            Debug.Log(
                $"AdMob: recompensa recibida. " +
                $"Tipo: {reward.Type}, cantidad: {reward.Amount}"
            );

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ContinueAfterAd();
            }
            else
            {
                Debug.LogError(
                    "AdMob: no se encontró GameManager.Instance."
                );
            }
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("AdMob: rewarded abierto.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("AdMob: rewarded cerrado.");

            DestroyRewardedAd();
            LoadRewarded();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogError(
                $"AdMob: rewarded falló al mostrarse. {error}"
            );

            DestroyRewardedAd();
            LoadRewarded();
        };
    }

    private void DestroyRewardedAd()
    {
        if (rewardedAd == null)
        {
            return;
        }

        rewardedAd.Destroy();
        rewardedAd = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            DestroyRewardedAd();
            Instance = null;
        }
    }
}