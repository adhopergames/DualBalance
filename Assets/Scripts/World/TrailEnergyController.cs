using UnityEngine;

public class TrailEnergyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEnergy energy;

    [Tooltip("ParticleSystem de la estela de Luz")]
    public ParticleSystem lightTrail;

    [Tooltip("ParticleSystem de la estela de Estela de Oscuridad")]
    public ParticleSystem darkTrail;

    [Header("Death / Game State")]
    [Tooltip("Si está activo, apaga las estelas cuando NO está en Playing (Pausa, GameOver, Pending).")]
    public bool disableWhenNotPlaying = true;

    [Header("Debug")]
    public bool debugLogs = true;

    // Guardamos los rate originales usando multiplier (más compatible que constant)
    private float lightBaseRateMult = 1f;
    private float darkBaseRateMult = 1f;

    private bool allDisabled;

    // ✅ Para detectar cambios de estado (Paused -> Playing) y reactivar correctamente
    private GameState lastState = GameState.Playing;
    private bool hasGameManager;

    private void Awake()
    {
        if (energy == null) energy = GetComponent<PlayerEnergy>();

        if (lightTrail != null) lightBaseRateMult = lightTrail.emission.rateOverTimeMultiplier;
        if (darkTrail != null) darkBaseRateMult = darkTrail.emission.rateOverTimeMultiplier;

        hasGameManager = (GameManager.Instance != null);
        if (hasGameManager) lastState = GameManager.Instance.State;

        if (debugLogs)
        {
            Debug.Log($"[TrailEnergyController] Awake. energy={(energy != null)} " +
                      $"lightTrail={(lightTrail != null)} darkTrail={(darkTrail != null)}", this);
        }
    }

    private void Start()
    {
        // Estado inicial coherente
        if (disableWhenNotPlaying && GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            DisableAll();
        }
        else
        {
            ApplyEnergyState(forceRestart: true);
        }
    }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            // ✅ Detectar cambio de estado (ej: Paused -> Playing)
            if (!hasGameManager)
            {
                hasGameManager = true;
                lastState = gm.State;
            }
            else if (gm.State != lastState)
            {
                var prev = lastState;
                lastState = gm.State;

                if (debugLogs) Debug.Log($"[TrailEnergyController] State changed: {prev} -> {lastState}", this);

                // Si entramos a Playing, rearmamos estelas según energía actual
                if (!disableWhenNotPlaying || lastState == GameState.Playing)
                {
                    allDisabled = false;
                    ApplyEnergyState(forceRestart: true); // ✅ clave
                }
                else
                {
                    DisableAll();
                    return;
                }
            }

            // 1) Apagado global si NO está en Playing
            if (disableWhenNotPlaying && gm.State != GameState.Playing)
            {
                if (!allDisabled)
                {
                    if (debugLogs) Debug.Log("[TrailEnergyController] GameState != Playing -> DisableAll()", this);
                    DisableAll();
                }
                return;
            }
        }

        allDisabled = false;

        // 2) Control por energía (polling)
        if (energy == null)
        {
            if (debugLogs) Debug.LogWarning("[TrailEnergyController] No PlayerEnergy found.", this);
            return;
        }

        ApplyEnergyState(forceRestart: false);
    }

    // ✅ Evalúa energía y enciende/apaga cada estela.
    // forceRestart=true se usa al reanudar (para forzar Play/Clear y evitar el bug).
    private void ApplyEnergyState(bool forceRestart)
    {
        // Luz
        if (energy != null && energy.IsLightDepleted) DisableTrail(lightTrail, "Light");
        else EnableTrail(lightTrail, lightBaseRateMult, "Light", forceRestart);

        // Oscuridad
        if (energy != null && energy.IsDarkDepleted) DisableTrail(darkTrail, "Dark");
        else EnableTrail(darkTrail, darkBaseRateMult, "Dark", forceRestart);
    }

    private void EnableTrail(ParticleSystem ps, float baseRateMult, string name, bool forceRestart)
    {
        if (ps == null) return;

        var em = ps.emission;

        // ✅ Siempre restauramos settings básicos
        em.enabled = true;
        em.rateOverTimeMultiplier = baseRateMult;

        // ✅ Si venimos de pausa/disable, forzamos rearmado
        if (forceRestart)
        {
            // Clear limpia partículas “viejas” y Play reinicia emisión correctamente
            ps.Clear(true);
            ps.Play(true);

            if (debugLogs) Debug.Log($"[TrailEnergyController] ForceRestart -> Enable {name}", this);
            return;
        }

        // En runtime normal, solo aseguramos que esté emitiendo
        if (!ps.isEmitting)
            ps.Play(true);
    }

    private void DisableTrail(ParticleSystem ps, string name)
    {
        if (ps == null) return;

        var em = ps.emission;

        // Corta emisión y deja morir lo que ya salió
        em.rateOverTimeMultiplier = 0f;
        em.enabled = false;

        // StopEmitting (no clear) para que desaparezca natural
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (debugLogs) Debug.Log($"[TrailEnergyController] Disable {name}", this);
    }

    private void DisableAll()
    {
        allDisabled = true;
        DisableTrail(lightTrail, "Light");
        DisableTrail(darkTrail, "Dark");
    }

    // ✅ Útil si tu muerte no usa GameState (puedes llamarlo manual)
    public void OnPlayerDied()
    {
        DisableAll();
    }
}
