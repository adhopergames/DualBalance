using UnityEngine;
using System;

public class PlayerEnergy : MonoBehaviour
{
    [Header("References")]
    public GameConfig config;

    [Header("Runtime Energy (ReadOnly)")]
    public float lightEnergy;
    public float darkEnergy;

    // Evento para UI (manda porcentajes 0..1)
    public event Action<float, float> OnEnergyChanged;

    public bool IsLightDepleted => lightEnergy <= 0.001f;
    public bool IsDarkDepleted => darkEnergy <= 0.001f;


    private void Awake()
    {
        // Inicializar con energía máxima
        lightEnergy = config.maxEnergy;
        darkEnergy = config.maxEnergy;
        RaiseChanged();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // Gasto pasivo por "volar"
        float drain = config.idleDrainPerSecond * Time.deltaTime;
        lightEnergy = Mathf.Max(0, lightEnergy - drain);
        darkEnergy = Mathf.Max(0, darkEnergy - drain);

        RaiseChanged();

    }

    /// <summary>
    /// Gasta energía según dirección del movimiento.
    /// movingRight = true gasta Luz, false gasta Oscuridad.
    /// </summary>
    public void SpendMove(bool movingRight)
    {
        if (movingRight)
            lightEnergy = Mathf.Max(0, lightEnergy - config.moveDrain);
        else
            darkEnergy = Mathf.Max(0, darkEnergy - config.moveDrain);

        RaiseChanged();
    }

    public bool CanAttack(ElementType type)
    {
        if (type == ElementType.Light)
            return lightEnergy >= config.attackDrain;
        else
            return darkEnergy >= config.attackDrain;
    }

    public void SpendAttack(ElementType type)
    {
        if (type == ElementType.Light)
            lightEnergy = Mathf.Max(0, lightEnergy - config.attackDrain);
        else
            darkEnergy = Mathf.Max(0, darkEnergy - config.attackDrain);

        RaiseChanged();
    }

    public void AddOrb(Orb.OrbType orbType)
    {
        switch (orbType)
        {
            case Orb.OrbType.Light:
                lightEnergy = Mathf.Min(config.maxEnergy, lightEnergy + config.orbAmount);

                // ✅ Buff score: x2 por 10s
                if (GameManager.Instance != null)
                    GameManager.Instance.ApplyScoreMultiplier(2f, 7f);
                break;

            case Orb.OrbType.Dark:
                darkEnergy = Mathf.Min(config.maxEnergy, darkEnergy + config.orbAmount);

                // ✅ Buff score: x2 por 10s
                if (GameManager.Instance != null)
                    GameManager.Instance.ApplyScoreMultiplier(2f, 7f);
                break;

            case Orb.OrbType.Dual:
                lightEnergy = Mathf.Min(config.maxEnergy, lightEnergy + config.dualOrbAmount);
                darkEnergy = Mathf.Min(config.maxEnergy, darkEnergy + config.dualOrbAmount);

                // ✅ Buff score: x4 por 5s
                if (GameManager.Instance != null)
                    GameManager.Instance.ApplyScoreMultiplier(4f, 4f);
                break;
        }

        RaiseChanged();
    }


    private void RaiseChanged()
    {
        OnEnergyChanged?.Invoke(lightEnergy / config.maxEnergy, darkEnergy / config.maxEnergy);
    }

    // Multiplicador de velocidad lateral del player
    public float MoveSpeedFactor
    {
        get
        {
            // Si SOLO uno de los dos está vacío → penalización leve
            if (IsLightDepleted ^ IsDarkDepleted)
                return 0.89f; //89% del moveSpeed normal

            return 1f; // movimiento normal
        }
    }
}
