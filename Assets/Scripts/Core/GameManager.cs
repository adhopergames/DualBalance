using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
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
    public float reviveInvulnerabilitySeconds = 1.5f;

    /// True mientras el jugador está protegido tras revivir.
    /// Úsalo en el script de colisiones para NO morir durante este tiempo.

    public bool IsReviveInvulnerable { get; private set; }

    // Controla si ya se usó el continue en esta run
    private int continuesUsed = 0;

    // Snapshot en memoria para revivir (no se guarda en PlayerPrefs)
    private RunSnapshot snapshot;
    private bool hasSnapshot;

    // Estructura interna con lo mínimo necesario para “seguir donde iba”
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
        Score += scorePerSecond * Time.deltaTime;

    }


    /// Velocidad actual del mundo (aumenta con el tiempo y se limita por maxWorldSpeed).
    /// Todos los objetos que caen deberían usar esto (en vez de una speed fija).
    public float CurrentWorldSpeed
    {
        get
        {
            float speed = config.baseWorldSpeed + (ElapsedTime * config.difficultyRamp);
            return Mathf.Min(speed, config.maxWorldSpeed);
        }
    }

    /// Intervalo de spawn que se reduce con el tiempo y se limita por minSpawnInterval.
    /// El SpawnManager lo usa para spawnear más rápido con la dificultad.

    public float CurrentSpawnInterval
    {
        get
        {
            float interval = config.baseSpawnInterval - (ElapsedTime * config.spawnIntervalRamp);
            return Mathf.Max(config.minSpawnInterval, interval);
        }
    }


    /// Llamar esto cuando el jugador pierde.
    /// Decide si va a GameOverPending (puede revivir) o a GameOver final.

    public void TriggerGameOver()
    {
        // ✅ Si no estamos jugando, ignoramos triggers extra.
        if (State != GameState.Playing) return;

        if (continuesUsed < maxContinuesPerRun)
        {
            EnterGameOverPending();
        }
        else
        {
            GameOverFinal();
        }
    }

    /// Entra en estado GameOverPending:
    /// - Pausa juego
    /// - Guarda snapshot
    /// - Guarda best score (aunque sea pending)
    /// - Notifica UI para mostrar botón Continue

    private void EnterGameOverPending()
    {
        State = GameState.GameOverPending;

        // Pausamos para que no sigan cayendo cosas mientras decide
        Time.timeScale = 0f;

        // Guardamos snapshot (en memoria)
        SaveSnapshot();

        // ✅ Calculamos NEW RECORD usando el best ANTES de guardar.
        int scoreNowInt = Mathf.RoundToInt(Score);
        int prevBest = SaveManager.GetBestScore();
        bool isNewRecordNow = scoreNowInt > prevBest;

        // ✅ Guardamos best score INCLUSO EN PENDING
        SaveManager.TrySetBestScore(scoreNowInt);

        int bestNow = SaveManager.GetBestScore();

        // Notificamos UI: puede mostrar botón Continue
        OnGameOverPending?.Invoke(scoreNowInt, bestNow, true, isNewRecordNow);
    }


    /// GameOver FINAL:
    /// - Guarda best score si aplica
    /// - Notifica UI final (aquí sí aparece NEW RECORD)

    private void GameOverFinal()
    {
        State = GameState.GameOver;

        // Aseguramos tiempo normal
        Time.timeScale = 1f;

        int finalScoreInt = Mathf.RoundToInt(Score);
        bool isNewRecord = SaveManager.TrySetBestScore(finalScoreInt);
        int best = SaveManager.GetBestScore();

        OnGameOver?.Invoke(finalScoreInt, best, isNewRecord);
        StatsManager.AddRun();
    }

    /// Se llama cuando el anuncio rewarded se completa.
    /// Restaura snapshot, limpia peligros y vuelve a Playing.
    /// (Este método lo llama AdManager cuando el anuncio termina COMPLETADO)

    public void ContinueAfterAd()
    {
        // Solo se puede revivir desde pending
        if (State != GameState.GameOverPending) return;

        // Si no hay snapshot, no podemos revivir => final
        if (!hasSnapshot)
        {
            GameOverFinal();
            return;
        }

        // Consumimos un continue
        continuesUsed++;

        // Volvemos a tiempo normal
        Time.timeScale = 1f;

        // Restauramos run
        Score = snapshot.score;
        ElapsedTime = snapshot.elapsedTime;

        // Restauramos energías
        playerEnergy.lightEnergy = snapshot.lightEnergy;
        playerEnergy.darkEnergy = snapshot.darkEnergy;

        // Restauramos carril actual
        playerMovement.currentLane = snapshot.currentLane;

        // Restauramos posición X
        Vector3 p = playerTransform.position;
        playerTransform.position = new Vector3(snapshot.playerX, p.y, p.z);

        // Limpieza de peligros cerca del jugador para evitar muerte instantánea
        ClearNearbyHazards();

        // Periodo de gracia: invencibilidad temporal
        IsReviveInvulnerable = true;
        StartCoroutine(ReviveInvulnerabilityRoutine());

        // Volvemos a jugar
        State = GameState.Playing;

        // Avisamos UI para ocultar panel
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
    /// Usamos tiempo real (unscaled) porque venimos de Time.timeScale = 0.

    private System.Collections.IEnumerator ReviveInvulnerabilityRoutine()
    {
        float t = reviveInvulnerabilitySeconds;

        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        IsReviveInvulnerable = false;
    }


    /// Limpia obstáculos/paredes cerca del jugador (zona segura).
    /// - Borra ElementalWall cerca
    /// - Borra FallingObject cerca, EXCEPTO los orbes (Orb)
    /// - Amplía rango según velocidad actual (si va rápido, limpia más)

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    /// Pausa o reanuda el juego.

    public void Pause(bool paused)
    {
        State = paused ? GameState.Paused : GameState.Playing;
    }
}
