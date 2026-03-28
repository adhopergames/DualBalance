using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Tutorial,
    Paused,
    GameOver,        // Final definitivo
    GameOverPending  // Perdió, pero puede revivir con anuncio
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Config")]
    [Tooltip("ScriptableObject con parámetros de dificultad, spawn, energía, etc.")]
    public GameConfig config;

    [Header("State")]
    [Tooltip("Estado actual del juego.")]
    public GameState State { get; private set; } = GameState.Playing;

    [Header("Score")]
    [Tooltip("Cuántos puntos gana el jugador por segundo.")]
    public float scorePerSecond = 10f;

    [Tooltip("Score actual (se incrementa en runtime).")]
    public float Score { get; private set; }

    [Tooltip("Tiempo transcurrido desde que empezó la partida (para dificultad).")]
    public float ElapsedTime { get; private set; }

    [Header("Level Loader (en esta escena)")]
    [SerializeField] private LevelLoader levelLoader;

    // Evento para UI cuando hay GameOver FINAL:
    // (scoreFinalInt, bestScoreInt, isNewRecord)
    public event Action<int, int, bool> OnGameOver;

    // Evento para UI cuando hay GameOver PENDING (con opción de continue):
    // (scoreNowInt, bestScoreInt, canContinue, isNewRecordNow)
    public event Action<int, int, bool, bool> OnGameOverPending;

    // Evento para UI cuando el jugador revive (para ocultar panel, etc.)
    public event Action OnRevive;

    [Header("Revive References (asignar en Inspector)")]
    [Tooltip("Referencia al PlayerEnergy del jugador.")]
    public PlayerEnergy playerEnergy;

    [Tooltip("Referencia al PlayerMovement del jugador.")]
    public PlayerMovement playerMovement;

    [Tooltip("Transform del jugador (para restaurar posición X).")]
    public Transform playerTransform;

    [Header("Revive Settings")]
    [Tooltip("Cuántas veces se puede revivir por partida (recomendado 1).")]
    public int maxContinuesPerRun = 1;

    [Tooltip("Radio vertical base (unidades) para limpiar peligros al revivir.")]
    public float reviveClearRangeY = 8f;

    [Header("Revive Grace Period")]
    [Tooltip("Segundos de invencibilidad tras revivir.")]
    public float reviveInvulnerabilitySeconds = 2.5f;

    /// True mientras el jugador está protegido tras revivir.
    /// Úsalo en el script de colisiones para NO morir durante este tiempo.
    public bool IsReviveInvulnerable { get; private set; }

    // Controla si ya se usó el continue en esta run
    private int continuesUsed = 0;

    // Snapshot en memoria para revivir (no se guarda en PlayerPrefs)
    private RunSnapshot snapshot;
    private bool hasSnapshot;

    [Header("Score Multiplier (Orbs)")]
    [Tooltip("Multiplicador actual aplicado al score por segundo. 1 = normal.")]
    [SerializeField] private float scoreMultiplier = 1f;

    [Tooltip("Tiempo restante (segundos) del multiplicador actual.")]
    [SerializeField] private float scoreMultiplierRemaining = 0f;

    // Evento opcional para UI si quieres mostrar información del buff
    public event Action<float, float> OnScoreMultiplierChanged; // (mult, remaining)

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

        // -------------------------
        // Seguridad al cargar escena
        // -------------------------
        Time.timeScale = 1f;
        State = GameState.Playing;

        // -------------------------
        // Reset de stats POR RUN
        // -------------------------
        // ✅ Importante: esto NO borra PlayerPrefs, solo contadores temporales de la partida.
        StatsManager.ResetRunStats();

        // -------------------------
        // Reset de run (Retry / volver a jugar)
        // -------------------------
        continuesUsed = 0;
        hasSnapshot = false;
        IsReviveInvulnerable = false;
    }

    private void Update()
    {
        // Solo avanzamos score/tiempo mientras se juega
        if (State != GameState.Playing) return;

        ElapsedTime += Time.deltaTime;

        // ✅ Si hay multiplicador activo, reducimos el tiempo restante
        if (scoreMultiplierRemaining > 0f)
        {
            scoreMultiplierRemaining -= Time.deltaTime;

            if (scoreMultiplierRemaining <= 0f)
            {
                scoreMultiplierRemaining = 0f;
                scoreMultiplier = 1f;
                OnScoreMultiplierChanged?.Invoke(scoreMultiplier, scoreMultiplierRemaining);
            }
        }

        // ✅ Score por segundo * multiplicador actual
        Score += scorePerSecond * scoreMultiplier * Time.deltaTime;
    }

    /// Velocidad actual del mundo (aumenta con el tiempo y se limita por maxWorldSpeed).
    public float CurrentWorldSpeed
    {
        get
        {
            float speed = config.baseWorldSpeed + (ElapsedTime * config.difficultyRamp);
            return Mathf.Min(speed, config.maxWorldSpeed);
        }
    }

    /// Intervalo de spawn que se reduce con el tiempo y se limita por minSpawnInterval.
    public float CurrentSpawnInterval
    {
        get
        {
            float interval = config.baseSpawnInterval - (ElapsedTime * config.spawnIntervalRamp);
            return Mathf.Max(config.minSpawnInterval, interval);
        }
    }

    /// Entra al estado Tutorial.
    /// Se congela el gameplay, pero la UI del tutorial sigue funcionando.
    public void EnterTutorial()
    {
        // No entrar en tutorial desde estados finales
        if (State == GameState.GameOver || State == GameState.GameOverPending) return;

        State = GameState.Tutorial;
        Time.timeScale = 0f;
    }

    /// Finaliza el tutorial y comienza el gameplay normal.
    public void FinishTutorial()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    /// Llamar esto cuando el jugador pierde.
    public void TriggerGameOver()
    {
        // ✅ Si no estamos jugando, ignoramos triggers extra.
        if (State != GameState.Playing) return;

        if (continuesUsed < maxContinuesPerRun)
            EnterGameOverPending();
        else
            GameOverFinal();
    }

    /// Entra en estado GameOverPending.
    private void EnterGameOverPending()
    {
        State = GameState.GameOverPending;
        Time.timeScale = 0f;

        SaveSnapshot();

        int scoreNowInt = Mathf.RoundToInt(Score);
        int prevBest = SaveManager.GetBestScore();
        bool isNewRecordNow = scoreNowInt > prevBest;

        SaveManager.TrySetBestScore(scoreNowInt);
        int bestNow = SaveManager.GetBestScore();

        OnGameOverPending?.Invoke(scoreNowInt, bestNow, true, isNewRecordNow);
    }

    /// GameOver FINAL.
    private void GameOverFinal()
    {
        State = GameState.GameOver;
        Time.timeScale = 1f;

        int finalScoreInt = Mathf.RoundToInt(Score);
        bool isNewRecord = SaveManager.TrySetBestScore(finalScoreInt);
        int best = SaveManager.GetBestScore();

        OnGameOver?.Invoke(finalScoreInt, best, isNewRecord);
        StatsManager.AddRun();
    }

    /// Se llama cuando el anuncio rewarded se completa.
    public void ContinueAfterAd()
    {
        if (State != GameState.GameOverPending) return;

        if (!hasSnapshot)
        {
            GameOverFinal();
            return;
        }

        continuesUsed++;
        Time.timeScale = 1f;

        Score = snapshot.score;
        ElapsedTime = snapshot.elapsedTime;

        // ✅ Al revivir, dejamos el score multiplier en normal
        scoreMultiplier = 1f;
        scoreMultiplierRemaining = 0f;
        OnScoreMultiplierChanged?.Invoke(scoreMultiplier, scoreMultiplierRemaining);

        playerEnergy.lightEnergy = snapshot.lightEnergy;
        playerEnergy.darkEnergy = snapshot.darkEnergy;

        playerMovement.currentLane = snapshot.currentLane;

        Vector3 p = playerTransform.position;
        playerTransform.position = new Vector3(snapshot.playerX, p.y, p.z);

        ClearNearbyHazards();

        IsReviveInvulnerable = true;
        StartCoroutine(ReviveInvulnerabilityRoutine());

        State = GameState.Playing;
        OnRevive?.Invoke();
    }

    /// Guarda snapshot mínimo para poder revivir.
    private void SaveSnapshot()
    {
        if (playerEnergy == null || playerMovement == null || playerTransform == null)
        {
            Debug.LogWarning("GameManager: faltan referencias (playerEnergy/playerMovement/playerTransform). No se guardó snapshot.");
            hasSnapshot = false;
            return;
        }

        snapshot = new RunSnapshot
        {
            score = Score,
            elapsedTime = ElapsedTime,
            lightEnergy = playerEnergy.lightEnergy,
            darkEnergy = playerEnergy.darkEnergy,
            currentLane = playerMovement.currentLane,
            playerX = playerTransform.position.x
        };

        hasSnapshot = true;
    }

    /// Invencibilidad temporal tras revivir.
    private System.Collections.IEnumerator ReviveInvulnerabilityRoutine()
    {
        IsReviveInvulnerable = true;
        yield return new WaitForSecondsRealtime(reviveInvulnerabilitySeconds);
        IsReviveInvulnerable = false;
    }

    /// Limpia obstáculos/paredes cerca del jugador (zona segura).
    private void ClearNearbyHazards()
    {
        if (playerTransform == null) return;

        float py = playerTransform.position.y;
        float dynamicRange = reviveClearRangeY + (CurrentWorldSpeed * 1.0f);

        // 1) Paredes elementales
        var walls = FindObjectsByType<ElementalWall>(FindObjectsSortMode.None);
        foreach (var w in walls)
        {
            if (w == null) continue;

            float dy = Mathf.Abs(w.transform.position.y - py);
            if (dy <= dynamicRange)
                Destroy(w.gameObject);
        }

        // 2) Objetos que caen (obstáculos)
        var falling = FindObjectsByType<FallingObject>(FindObjectsSortMode.None);
        foreach (var f in falling)
        {
            if (f == null) continue;

            // Si es un orbe, NO lo borramos
            if (f.GetComponent<Orb>() != null) continue;

            float dy = Mathf.Abs(f.transform.position.y - py);
            if (dy <= dynamicRange)
                Destroy(f.gameObject);
        }
    }

    /// Reinicia la escena actual (Retry).
    public void Restart()
    {
        Time.timeScale = 1f;
        StatsManager.ResetRunStats();

        if (levelLoader != null)
            levelLoader.ReloadCurrentScene();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// Toggle para UI (Button.onClick) - SIN parámetros
    public void TogglePause()
    {
        // No permitimos pausar en estados finales ni durante tutorial
        if (State == GameState.GameOver || State == GameState.GameOverPending || State == GameState.Tutorial)
            return;

        bool shouldPause = State != GameState.Paused;
        Pause(shouldPause);
    }

    /// Pausa o reanuda el juego (modo correcto)
    public void Pause(bool paused)
    {
        // No permitimos que una lógica externa "despause" durante tutorial
        if (State == GameState.Tutorial && !paused)
            return;

        if (paused)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
        }
        else
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    /// Activa un multiplicador temporal de score.
    public void ApplyScoreMultiplier(float multiplier, float durationSeconds)
    {
        if (multiplier <= 1f || durationSeconds <= 0f) return;

        if (multiplier > scoreMultiplier)
        {
            scoreMultiplier = multiplier;
            scoreMultiplierRemaining = durationSeconds;
        }
        else if (Mathf.Approximately(multiplier, scoreMultiplier))
        {
            scoreMultiplierRemaining = durationSeconds;
        }

        OnScoreMultiplierChanged?.Invoke(scoreMultiplier, scoreMultiplierRemaining);
    }
}