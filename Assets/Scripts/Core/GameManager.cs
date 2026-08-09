using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Tutorial,
    Paused,
    GameOver,
    GameOverPending
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ============================================================
    // CONFIG
    // ============================================================

    [Header("Config")]
    [Tooltip(
        "ScriptableObject con parámetros de dificultad, " +
        "spawn, energía, etc."
    )]
    public GameConfig config;

    // ============================================================
    // STATE
    // ============================================================

    [Header("State")]
    [Tooltip("Estado actual del juego.")]
    public GameState State { get; private set; } =
        GameState.Playing;

    // ============================================================
    // SCORE
    // ============================================================

    [Header("Score")]

    [Tooltip(
        "Cuántos puntos gana el jugador por segundo."
    )]
    public float scorePerSecond = 10f;

    [Tooltip(
        "Score actual de la run."
    )]
    public float Score { get; private set; }

    [Tooltip(
        "Tiempo transcurrido desde que empezó la run."
    )]
    public float ElapsedTime { get; private set; }

    // ============================================================
    // EVENTS
    // ============================================================

    /*
     * Game Over FINAL:
     *
     * scoreFinalInt
     * bestScoreInt
     * isNewRecord
     */
    public event Action<int, int, bool>
        OnGameOver;

    /*
     * Game Over PENDING:
     *
     * scoreNowInt
     * bestScoreInt
     * canContinue
     * isNewRecordNow
     */
    public event Action<int, int, bool, bool>
        OnGameOverPending;

    /*
     * UI utiliza este evento para esconder el panel
     * cuando el jugador revive.
     */
    public event Action OnRevive;

    // ============================================================
    // REVIVE REFERENCES
    // ============================================================

    [Header("Revive References")]

    [Tooltip(
        "Referencia al PlayerEnergy."
    )]
    public PlayerEnergy playerEnergy;

    [Tooltip(
        "Referencia al PlayerMovement."
    )]
    public PlayerMovement playerMovement;

    [Tooltip(
        "Transform del jugador."
    )]
    public Transform playerTransform;

    // ============================================================
    // REVIVE SETTINGS
    // ============================================================

    [Header("Revive Settings")]

    [Tooltip(
        "Cantidad máxima de continues por run."
    )]
    public int maxContinuesPerRun = 1;

    [Tooltip(
        "Radio vertical para limpiar peligros al revivir."
    )]
    public float reviveClearRangeY = 8f;

    [Header("Revive Grace Period")]

    [Tooltip(
        "Segundos de invencibilidad después de revivir."
    )]
    public float reviveInvulnerabilitySeconds = 2.5f;

    public bool IsReviveInvulnerable
    {
        get;
        private set;
    }

    /*
     * Cantidad de continues utilizados en esta run.
     */
    private int continuesUsed;

    /*
     * True si el jugador realmente utilizó un Rewarded
     * para revivir durante esta run.
     *
     * Se utiliza también para evitar mostrar
     * inmediatamente un Interstitial.
     */
    private bool rewardedUsedThisRun;

    // ============================================================
    // AD RUN TRACKING
    // ============================================================

    /*
     * Evita contar dos veces la misma run.
     *
     * Ejemplo:
     *
     * muere
     * → se registra run
     * → mira Rewarded
     * → revive
     * → vuelve a morir
     *
     * Sigue siendo UNA sola run.
     */
    private bool runRegisteredForAds;

    // ============================================================
    // SNAPSHOT
    // ============================================================

    private RunSnapshot snapshot;
    private bool hasSnapshot;

    // ============================================================
    // SCORE MULTIPLIER
    // ============================================================

    [Header("Score Multiplier (Orbs)")]

    [Tooltip(
        "Multiplicador actual del score. 1 = normal."
    )]
    [SerializeField]
    private float scoreMultiplier = 1f;

    [Tooltip(
        "Tiempo restante del multiplicador."
    )]
    [SerializeField]
    private float scoreMultiplierRemaining = 0f;

    public event Action<float, float>
        OnScoreMultiplierChanged;

    // ============================================================
    // SNAPSHOT STRUCT
    // ============================================================

    [Serializable]
    private struct RunSnapshot
    {
        public float score;
        public float elapsedTime;

        public float lightEnergy;
        public float darkEnergy;

        public int currentLane;
        public float playerX;
    }

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

        // --------------------------------------------------------
        // Estado inicial
        // --------------------------------------------------------

        Time.timeScale = 1f;

        State =
            GameState.Playing;

        // --------------------------------------------------------
        // Stats temporales
        // --------------------------------------------------------

        StatsManager.ResetRunStats();

        // --------------------------------------------------------
        // Reset de nueva run
        // --------------------------------------------------------

        continuesUsed = 0;

        rewardedUsedThisRun = false;

        runRegisteredForAds = false;

        hasSnapshot = false;

        IsReviveInvulnerable = false;
    }

    private void Update()
    {
        /*
         * Score y dificultad solo avanzan mientras
         * realmente estamos jugando.
         */
        if (State != GameState.Playing)
            return;

        ElapsedTime +=
            Time.deltaTime;

        // --------------------------------------------------------
        // Score multiplier
        // --------------------------------------------------------

        if (scoreMultiplierRemaining > 0f)
        {
            scoreMultiplierRemaining -=
                Time.deltaTime;

            if (scoreMultiplierRemaining <= 0f)
            {
                scoreMultiplierRemaining = 0f;
                scoreMultiplier = 1f;

                OnScoreMultiplierChanged?.Invoke(
                    scoreMultiplier,
                    scoreMultiplierRemaining
                );
            }
        }

        Score +=
            scorePerSecond *
            scoreMultiplier *
            Time.deltaTime;
    }

    // ============================================================
    // DIFFICULTY
    // ============================================================

    public float CurrentWorldSpeed
    {
        get
        {
            float speed =
                config.baseWorldSpeed +
                (
                    ElapsedTime *
                    config.difficultyRamp
                );

            return Mathf.Min(
                speed,
                config.maxWorldSpeed
            );
        }
    }

    public float CurrentSpawnInterval
    {
        get
        {
            float interval =
                config.baseSpawnInterval -
                (
                    ElapsedTime *
                    config.spawnIntervalRamp
                );

            return Mathf.Max(
                config.minSpawnInterval,
                interval
            );
        }
    }

    // ============================================================
    // TUTORIAL
    // ============================================================

    /// <summary>
    /// Congela el gameplay mientras el tutorial está visible.
    /// </summary>
    public void EnterTutorial()
    {
        if (
            State == GameState.GameOver ||
            State == GameState.GameOverPending
        )
        {
            return;
        }

        State =
            GameState.Tutorial;

        Time.timeScale = 0f;
    }

    /// <summary>
    /// Finaliza el tutorial y comienza el gameplay.
    /// </summary>
    public void FinishTutorial()
    {
        State =
            GameState.Playing;

        Time.timeScale = 1f;
    }

    // ============================================================
    // GAME OVER
    // ============================================================

    /// <summary>
    /// Punto de entrada cuando el jugador pierde.
    /// </summary>
    public void TriggerGameOver()
    {
        if (State != GameState.Playing)
            return;

        /*
         * Una run se registra UNA sola vez para
         * el contador del Interstitial.
         */
        RegisterRunForAdsIfNeeded();

        if (
            continuesUsed <
            maxContinuesPerRun
        )
        {
            EnterGameOverPending();
        }
        else
        {
            GameOverFinal();
        }
    }

    /// <summary>
    /// Registra la run para el sistema periódico de anuncios.
    ///
    /// Aunque posteriormente el jugador reviva,
    /// no volverá a incrementar el contador.
    /// </summary>
    private void RegisterRunForAdsIfNeeded()
    {
        if (runRegisteredForAds)
            return;

        runRegisteredForAds = true;

        AdManager.Instance?.RegisterCompletedRun();
    }

    // ============================================================
    // GAME OVER PENDING
    // ============================================================

    /// <summary>
    /// El jugador perdió pero todavía puede utilizar
    /// el Rewarded para continuar.
    /// </summary>
    private void EnterGameOverPending()
    {
        State =
            GameState.GameOverPending;

        Time.timeScale = 0f;

        /*
         * Bajamos la música mientras estamos
         * en Game Over.
         */
        AudioManager.Instance
            ?.SetGameOverMusicEffect(true);

        SaveSnapshot();

        int scoreNowInt =
            Mathf.RoundToInt(Score);

        int previousBest =
            SaveManager.GetBestScore();

        bool isNewRecordNow =
            scoreNowInt >
            previousBest;

        SaveManager.TrySetBestScore(
            scoreNowInt
        );

        int bestNow =
            SaveManager.GetBestScore();

        OnGameOverPending?.Invoke(
            scoreNowInt,
            bestNow,
            true,
            isNewRecordNow
        );
    }

    // ============================================================
    // FINAL GAME OVER
    // ============================================================

    /// <summary>
    /// Game Over definitivo.
    /// </summary>
    private void GameOverFinal()
    {
        /*
         * Por seguridad, si llegamos aquí sin haber
         * registrado todavía la run, lo hacemos.
         */
        RegisterRunForAdsIfNeeded();

        State =
            GameState.GameOver;

        Time.timeScale = 1f;

        AudioManager.Instance
            ?.SetGameOverMusicEffect(true);

        int finalScoreInt =
            Mathf.RoundToInt(Score);

        bool isNewRecord =
            SaveManager.TrySetBestScore(
                finalScoreInt
            );

        int best =
            SaveManager.GetBestScore();

        OnGameOver?.Invoke(
            finalScoreInt,
            best,
            isNewRecord
        );

        StatsManager.AddRun();
    }

    // ============================================================
    // REWARDED CONTINUE
    // ============================================================

    /// <summary>
    /// Se llama DESPUÉS de que:
    ///
    /// 1. Google concedió la recompensa.
    /// 2. El anuncio Rewarded fue cerrado completamente.
    /// </summary>
    public void ContinueAfterAd()
    {
        if (
            State !=
            GameState.GameOverPending
        )
        {
            return;
        }

        /*
         * Marcamos inmediatamente que esta run
         * ya utilizó Rewarded.
         *
         * Esto impedirá un Interstitial al finalizarla.
         */
        rewardedUsedThisRun = true;

        if (!hasSnapshot)
        {
            GameOverFinal();
            return;
        }

        continuesUsed++;

        Time.timeScale = 1f;

        // --------------------------------------------------------
        // Restaurar score / tiempo
        // --------------------------------------------------------

        Score =
            snapshot.score;

        ElapsedTime =
            snapshot.elapsedTime;

        // --------------------------------------------------------
        // Reset score multiplier
        // --------------------------------------------------------

        scoreMultiplier = 1f;

        scoreMultiplierRemaining = 0f;

        OnScoreMultiplierChanged?.Invoke(
            scoreMultiplier,
            scoreMultiplierRemaining
        );

        // --------------------------------------------------------
        // Restaurar energía
        // --------------------------------------------------------

        if (playerEnergy != null)
        {
            playerEnergy.lightEnergy =
                snapshot.lightEnergy;

            playerEnergy.darkEnergy =
                snapshot.darkEnergy;
        }

        // --------------------------------------------------------
        // Restaurar lane
        // --------------------------------------------------------

        if (playerMovement != null)
        {
            playerMovement.currentLane =
                snapshot.currentLane;
        }

        // --------------------------------------------------------
        // Restaurar posición
        // --------------------------------------------------------

        if (playerTransform != null)
        {
            Vector3 position =
                playerTransform.position;

            playerTransform.position =
                new Vector3(
                    snapshot.playerX,
                    position.y,
                    position.z
                );
        }

        // --------------------------------------------------------
        // Crear zona segura
        // --------------------------------------------------------

        ClearNearbyHazards();

        // --------------------------------------------------------
        // Invencibilidad temporal
        // --------------------------------------------------------

        IsReviveInvulnerable = true;

        StartCoroutine(
            ReviveInvulnerabilityRoutine()
        );

        // --------------------------------------------------------
        // Restaurar música
        // --------------------------------------------------------

        AudioManager.Instance
            ?.SetGameOverMusicEffect(false);

        // --------------------------------------------------------
        // Volver al gameplay
        // --------------------------------------------------------

        State =
            GameState.Playing;

        OnRevive?.Invoke();
    }

    // ============================================================
    // SAVE SNAPSHOT
    // ============================================================

    /// <summary>
    /// Guarda la información mínima necesaria
    /// para poder revivir al jugador.
    /// </summary>
    private void SaveSnapshot()
    {
        if (
            playerEnergy == null ||
            playerMovement == null ||
            playerTransform == null
        )
        {
            Debug.LogWarning(
                "GameManager: faltan referencias " +
                "(playerEnergy/playerMovement/playerTransform). " +
                "No se guardó snapshot."
            );

            hasSnapshot = false;

            return;
        }

        snapshot =
            new RunSnapshot
            {
                score =
                    Score,

                elapsedTime =
                    ElapsedTime,

                lightEnergy =
                    playerEnergy.lightEnergy,

                darkEnergy =
                    playerEnergy.darkEnergy,

                currentLane =
                    playerMovement.currentLane,

                playerX =
                    playerTransform.position.x
            };

        hasSnapshot = true;
    }

    // ============================================================
    // REVIVE INVULNERABILITY
    // ============================================================

    private System.Collections.IEnumerator
        ReviveInvulnerabilityRoutine()
    {
        IsReviveInvulnerable = true;

        yield return
            new WaitForSecondsRealtime(
                reviveInvulnerabilitySeconds
            );

        IsReviveInvulnerable = false;
    }

    // ============================================================
    // CLEAR HAZARDS
    // ============================================================

    /// <summary>
    /// Limpia peligros cercanos después del revive
    /// para evitar morir inmediatamente otra vez.
    /// </summary>
    private void ClearNearbyHazards()
    {
        if (playerTransform == null)
            return;

        float playerY =
            playerTransform.position.y;

        float dynamicRange =
            reviveClearRangeY +
            (
                CurrentWorldSpeed *
                1f
            );

        // --------------------------------------------------------
        // Elemental Walls
        // --------------------------------------------------------

        ElementalWall[] walls =
            FindObjectsByType<ElementalWall>(
                FindObjectsSortMode.None
            );

        foreach (
            ElementalWall wall
            in walls
        )
        {
            if (wall == null)
                continue;

            float distanceY =
                Mathf.Abs(
                    wall.transform.position.y -
                    playerY
                );

            if (
                distanceY <=
                dynamicRange
            )
            {
                Destroy(
                    wall.gameObject
                );
            }
        }

        // --------------------------------------------------------
        // Falling objects
        // --------------------------------------------------------

        FallingObject[] fallingObjects =
            FindObjectsByType<FallingObject>(
                FindObjectsSortMode.None
            );

        foreach (
            FallingObject falling
            in fallingObjects
        )
        {
            if (falling == null)
                continue;

            /*
             * Los orbes NO se eliminan.
             */
            if (
                falling.GetComponent<Orb>() !=
                null
            )
            {
                continue;
            }

            float distanceY =
                Mathf.Abs(
                    falling.transform.position.y -
                    playerY
                );

            if (
                distanceY <=
                dynamicRange
            )
            {
                Destroy(
                    falling.gameObject
                );
            }
        }
    }

    // ============================================================
    // RESTART
    // ============================================================

    /// <summary>
    /// Reinicia la run.
    ///
    /// Si corresponde un Interstitial:
    ///
    /// 1. Esperamos a que cierre.
    /// 2. Después reiniciamos.
    ///
    /// Si esta run utilizó Rewarded,
    /// el Interstitial será omitido.
    /// </summary>
    public void Restart()
    {
        /*
         * Solo consideramos una run terminada para mostrar
         * Interstitial si ya ocurrió Game Over.
         *
         * Reiniciar manualmente desde pausa no dispara anuncio.
         */
        bool finishedRun =
            runRegisteredForAds &&
            (
                State ==
                GameState.GameOverPending ||

                State ==
                GameState.GameOver
            );

        if (
            finishedRun &&
            AdManager.Instance != null
        )
        {
            /*
             * Congelamos todo mientras AdManager decide
             * si debe mostrar el anuncio.
             */
            Time.timeScale = 0f;

            AdManager.Instance
                .TryShowInterstitialIfDue(
                    rewardedUsedThisRun,
                    ReloadCurrentSceneNow
                );

            return;
        }

        /*
         * Reinicio normal.
         */
        ReloadCurrentSceneNow();
    }

    /// <summary>
    /// Realiza realmente la recarga de Game.
    ///
    /// Este método puede ejecutarse:
    /// - inmediatamente;
    /// - o después de cerrar un Interstitial.
    /// </summary>
    private void ReloadCurrentSceneNow()
    {
        Time.timeScale = 1f;

        // --------------------------------------------------------
        // Restaurar cualquier efecto temporal de audio.
        // --------------------------------------------------------

        AudioManager.Instance
            ?.ResetMusicStateImmediate();

        AudioManager.Instance
            ?.ResetGameOverMusicImmediate();

        StatsManager.ResetRunStats();

        // --------------------------------------------------------
        // Utilizar el LevelLoader persistente.
        // --------------------------------------------------------

        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance
                .ReloadCurrentScene();
        }
        else
        {
            SceneManager.LoadScene(
                SceneManager
                    .GetActiveScene()
                    .buildIndex
            );
        }
    }

    // ============================================================
    // PAUSE
    // ============================================================

    /// <summary>
    /// Alterna pausa/reanudar desde la UI.
    /// </summary>
    public void TogglePause()
    {
        if (
            State == GameState.GameOver ||
            State == GameState.GameOverPending ||
            State == GameState.Tutorial
        )
        {
            return;
        }

        bool shouldPause =
            State != GameState.Paused;

        Pause(
            shouldPause
        );
    }

    /// <summary>
    /// Aplica o elimina la pausa.
    ///
    /// También controla el efecto Low Pass
    /// de la música.
    /// </summary>
    public void Pause(
        bool paused
    )
    {
        /*
         * No permitimos que otro sistema quite
         * accidentalmente la pausa del tutorial.
         */
        if (
            State == GameState.Tutorial &&
            !paused
        )
        {
            return;
        }

        if (paused)
        {
            State =
                GameState.Paused;

            Time.timeScale = 0f;

            AudioManager.Instance
                ?.SetPauseMusicEffect(true);
        }
        else
        {
            State =
                GameState.Playing;

            Time.timeScale = 1f;

            AudioManager.Instance
                ?.SetPauseMusicEffect(false);
        }
    }

    // ============================================================
    // SCORE MULTIPLIER
    // ============================================================

    /// <summary>
    /// Activa o renueva un multiplicador temporal
    /// de puntuación.
    /// </summary>
    public void ApplyScoreMultiplier(
        float multiplier,
        float durationSeconds
    )
    {
        if (
            multiplier <= 1f ||
            durationSeconds <= 0f
        )
        {
            return;
        }

        if (
            multiplier >
            scoreMultiplier
        )
        {
            scoreMultiplier =
                multiplier;

            scoreMultiplierRemaining =
                durationSeconds;
        }
        else if (
            Mathf.Approximately(
                multiplier,
                scoreMultiplier
            )
        )
        {
            scoreMultiplierRemaining =
                durationSeconds;
        }

        OnScoreMultiplierChanged?.Invoke(
            scoreMultiplier,
            scoreMultiplierRemaining
        );
    }

    // ============================================================
    // DESTROY
    // ============================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}