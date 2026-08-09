using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    public static event Action OnEntryTransitionFinished;

    [Header("Animator")]
    [SerializeField] private Animator transition;

    [Header("Animator States")]
    [Tooltip("Estado que cubre completamente la pantalla.")]
    [SerializeField] private string closeStateName = "CrossfadeStart";

    [Tooltip("Estado que descubre la pantalla.")]
    [SerializeField] private string openStateName = "CrossfadeEnd";

    [Header("Timing")]
    [Tooltip("Duración real de la animación que cubre la pantalla.")]
    [SerializeField] private float closeDuration = 0.25f;

    [Tooltip("Duración real de la animación que descubre la pantalla.")]
    [SerializeField] private float openDuration = 0.25f;

    [Tooltip(
        "Cantidad de frames que esperamos después de cargar una escena " +
        "antes de comenzar a descubrirla."
    )]
    [SerializeField] private int warmupFrames = 2;

    private bool isLoading;

    public bool IsLoading => isLoading;

    private void Awake()
    {
        // ---------------------------------------------------------
        // SINGLETON PERSISTENTE
        // ---------------------------------------------------------

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        /*
         * El mismo LevelLoader permanece vivo durante
         * toda la aplicación.
         */
        DontDestroyOnLoad(gameObject);

        // ---------------------------------------------------------
        // ANIMATOR
        // ---------------------------------------------------------

        if (transition != null)
        {
            /*
             * Permite que el loader siga funcionando incluso
             * si Tutorial / Pause / GameOver usan Time.timeScale = 0.
             */
            transition.updateMode = AnimatorUpdateMode.UnscaledTime;
            transition.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
    }

    private IEnumerator Start()
    {
        /*
         * IMPORTANTE:
         *
         * NO reproducimos CrossfadeEnd desde código.
         *
         * El Animator ya tiene:
         *
         * Entry -> CrossfadeEnd
         *
         * Por lo tanto Unity reproduce automáticamente
         * la animación de apertura al iniciar la aplicación.
         *
         * Nosotros simplemente esperamos a que termine
         * para avisar a otros sistemas, como el TutorialManager.
         */

        yield return new WaitForSecondsRealtime(openDuration);

        OnEntryTransitionFinished?.Invoke();
    }

    // ============================================================
    // PUBLIC
    // ============================================================

    public void LoadScene(string sceneName)
    {
        if (isLoading)
            return;

        StartCoroutine(
            LoadSceneRoutine(sceneName)
        );
    }

    public void LoadScene(int buildIndex)
    {
        if (isLoading)
            return;

        StartCoroutine(
            LoadSceneRoutine(buildIndex)
        );
    }

    public void ReloadCurrentScene()
    {
        if (isLoading)
            return;

        LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void LoadNextScene()
    {
        if (isLoading)
            return;

        LoadScene(
            SceneManager.GetActiveScene().buildIndex + 1
        );
    }

    // ============================================================
    // LOAD BY NAME
    // ============================================================

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        Time.timeScale = 1f;

        // 1. Cubrir la pantalla.
        yield return StartCoroutine(
            CloseTransition()
        );

        // 2. Cargar mientras seguimos completamente negros.
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError(
                $"LevelLoader: no se pudo cargar '{sceneName}'."
            );

            isLoading = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        // 3. Dejar inicializar la escena mientras seguimos negros.
        yield return StartCoroutine(
            WaitForSceneWarmup()
        );

        // 4. Descubrir la nueva escena.
        yield return StartCoroutine(
            OpenTransition()
        );

        isLoading = false;

        OnEntryTransitionFinished?.Invoke();
    }

    // ============================================================
    // LOAD BY INDEX
    // ============================================================

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        isLoading = true;

        Time.timeScale = 1f;

        yield return StartCoroutine(
            CloseTransition()
        );

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(buildIndex);

        if (operation == null)
        {
            Debug.LogError(
                $"LevelLoader: no se pudo cargar índice {buildIndex}."
            );

            isLoading = false;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(
            WaitForSceneWarmup()
        );

        yield return StartCoroutine(
            OpenTransition()
        );

        isLoading = false;

        OnEntryTransitionFinished?.Invoke();
    }

    // ============================================================
    // CLOSE
    // ============================================================

    private IEnumerator CloseTransition()
    {
        if (transition == null)
        {
            Debug.LogWarning(
                "LevelLoader: no hay Animator para cerrar."
            );

            yield break;
        }

        transition.Play(
            closeStateName,
            0,
            0f
        );

        yield return new WaitForSecondsRealtime(
            closeDuration
        );

        /*
         * Garantizamos un frame totalmente negro antes
         * de comenzar la carga.
         */
        yield return new WaitForEndOfFrame();
    }

    // ============================================================
    // OPEN
    // ============================================================

    private IEnumerator OpenTransition()
    {
        if (transition == null)
        {
            Debug.LogWarning(
                "LevelLoader: no hay Animator para abrir."
            );

            yield break;
        }

        transition.Play(
            openStateName,
            0,
            0f
        );

        yield return new WaitForSecondsRealtime(
            openDuration
        );
    }

    // ============================================================
    // WARMUP
    // ============================================================

    private IEnumerator WaitForSceneWarmup()
    {
        int frames =
            Mathf.Max(1, warmupFrames);

        for (int i = 0; i < frames; i++)
        {
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}