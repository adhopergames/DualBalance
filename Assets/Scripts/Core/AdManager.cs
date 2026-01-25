using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Rewarded Ads simple y estable:
/// - Inicializa una vez (DontDestroyOnLoad)
/// - Precarga rewarded al iniciar
/// - ShowRewarded solo si está listo (si no, vuelve a cargar)
/// - Recarga después de cada show
/// </summary>
public class AdManager : MonoBehaviour,
    IUnityAdsInitializationListener,
    IUnityAdsLoadListener,
    IUnityAdsShowListener
{
    public static AdManager Instance;

    [Header("Game IDs")]
    public string androidGameId;
    public string iOSGameId;

    [Header("Placement IDs (Rewarded)")]
    public string idAndroidAd;
    public string idIOSAd;

    [Header("Settings")]
    public bool testMode = true;

    private string gameIdSelected;
    private string rewardedPlacementId;

    private bool isInitialized;
    private bool isLoading;
    private bool isRewardedReady;

    public bool IsRewardedReady => isInitialized && isRewardedReady;

    private void Awake()
    {
        // -------------------------
        // Singleton seguro
        // -------------------------
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAds();
    }

    private void InitializeAds()
    {
#if UNITY_ANDROID
        gameIdSelected = androidGameId;
        rewardedPlacementId = idAndroidAd;
#elif UNITY_IOS
        gameIdSelected = iOSGameId;
        rewardedPlacementId = idIOSAd;
#else
        gameIdSelected = androidGameId;
        rewardedPlacementId = idAndroidAd;
#endif

        isInitialized = false;
        isLoading = false;
        isRewardedReady = false;

        if (!Advertisement.isInitialized)
        {
            Advertisement.Initialize(gameIdSelected, testMode, this);
        }
        else
        {
            isInitialized = true;
            LoadRewarded();
        }
    }

    public void OnInitializationComplete()
    {
        isInitialized = true;

        // ✅ Precarga inicial
        LoadRewarded();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"Ads: Init FAILED => {error} - {message}");
        isInitialized = false;
        isRewardedReady = false;
        isLoading = false;
    }

    /// <summary>
    /// Precarga rewarded.
    /// </summary>
    public void LoadRewarded()
    {
        if (!isInitialized) return;
        if (isLoading) return;
        if (isRewardedReady) return; // ya está listo

        isLoading = true;
        Advertisement.Load(rewardedPlacementId, this);
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId != rewardedPlacementId) return;

        isLoading = false;
        isRewardedReady = true;
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId != rewardedPlacementId) return;

        Debug.LogError($"Ads: Load FAILED => {error} - {message}");
        isLoading = false;
        isRewardedReady = false;
    }

    /// <summary>
    /// Llamado por UI. Si está listo, lo muestra. Si no, intenta cargar.
    /// </summary>
    public void ShowRewarded()
    {
        if (!isInitialized) return;

        if (!isRewardedReady)
        {
            // Si no está listo, pedimos carga y listo (UI mostrará "Cargando...")
            LoadRewarded();
            return;
        }

        // Consumimos ready (hay que recargar luego)
        isRewardedReady = false;

        Advertisement.Show(rewardedPlacementId, this);
    }

    public void OnUnityAdsShowStart(string placementId) { }

    public void OnUnityAdsShowClick(string placementId) { }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Ads: Show FAILED => {error} - {message}");

        // ✅ Recargar para próximo intento
        isRewardedReady = false;
        isLoading = false;
        LoadRewarded();
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state)
    {
        if (placementId != rewardedPlacementId) return;

        // ✅ Recargar para el próximo continue
        isRewardedReady = false;
        isLoading = false;
        LoadRewarded();

        if (state == UnityAdsShowCompletionState.COMPLETED)
        {
            GameManager.Instance.ContinueAfterAd();
        }
    }
}
