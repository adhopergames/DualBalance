using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Controla la interfaz principal del gameplay:
/// - Puntaje actual
/// - Barras de energía de Luz y Oscuridad
/// - Porcentajes de energía
/// - Indicadores visuales del multiplicador (x2 / x4)
/// - Botón de pausa
///
/// Nota:
/// Los labels como "Score", "Luz", "Oscuridad", "Atq. Luz", etc.
/// se colocan directamente en la UI con TMP aparte para soportar localización.
public class GameplayHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Referencia al GameManager de la escena.")]
    public GameManager gameManager;

    [Tooltip("Referencia al PlayerEnergy del jugador.")]
    public PlayerEnergy playerEnergy;

    [Header("Score UI")]
    [Tooltip("Texto que muestra SOLO el valor numérico del score.")]
    public TMP_Text scoreValueText;

    [Header("Energy UI - Sliders")]
    [Tooltip("Barra visual de energía de Luz (0..1).")]
    public Slider lightEnergySlider;

    [Tooltip("Barra visual de energía de Oscuridad (0..1).")]
    public Slider darkEnergySlider;

    [Header("Energy UI - Percent Text")]
    [Tooltip("Texto que muestra el porcentaje de Luz. Ej: 85%")]
    public TMP_Text lightPercentText;

    [Tooltip("Texto que muestra el porcentaje de Oscuridad. Ej: 62%")]
    public TMP_Text darkPercentText;

    [Header("Multiplier UI")]
    [Tooltip("GameObject visual para el multiplicador x2.")]
    public GameObject multiplierX2Indicator;

    [Tooltip("GameObject visual para el multiplicador x4.")]
    public GameObject multiplierX4Indicator;

    [Header("Attack Hint UI (Optional)")]
    [Tooltip("Texto breve del lado izquierdo. Ej: ATQ. OSC.")]
    public TMP_Text leftAttackHintText;

    [Tooltip("Texto breve del lado derecho. Ej: ATQ. LUZ")]
    public TMP_Text rightAttackHintText;

    [Header("Pause UI")]
    [Tooltip("Botón de pausa del HUD.")]
    public Button pauseButton;

    private void Awake()
    {
        // Si no fue asignado manualmente, intentamos tomar el singleton
        if (gameManager == null)
            gameManager = GameManager.Instance;

        // Configuramos sliders para trabajar como barras visuales
        SetupSlider(lightEnergySlider);
        SetupSlider(darkEnergySlider);
    }

    private void OnEnable()
    {
        // Escuchar cambios de energía del jugador
        if (playerEnergy != null)
            playerEnergy.OnEnergyChanged += HandleEnergyChanged;

        // Escuchar cambios del multiplicador de score
        if (gameManager != null)
            gameManager.OnScoreMultiplierChanged += HandleScoreMultiplierChanged;

        // Listener del botón de pausa
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPausePressed);

        // Refrescamos toda la UI al habilitar
        RefreshAllUI();
    }

    private void OnDisable()
    {
        // Quitar suscripciones para evitar listeners duplicados
        if (playerEnergy != null)
            playerEnergy.OnEnergyChanged -= HandleEnergyChanged;

        if (gameManager != null)
            gameManager.OnScoreMultiplierChanged -= HandleScoreMultiplierChanged;

        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPausePressed);
    }

    private void Update()
    {
        // El score cambia constantemente, por eso lo actualizamos cada frame
        UpdateScoreUI();
    }

    /// Configura un slider para usarse solo como barra de energía.
    private void SetupSlider(Slider slider)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    /// Refresca score, energía e indicadores del multiplicador.
    private void RefreshAllUI()
    {
        UpdateScoreUI();

        // Refrescar energía actual al iniciar
        if (playerEnergy != null && playerEnergy.config != null)
        {
            float maxE = playerEnergy.config.maxEnergy;

            float light01 = maxE > 0f ? playerEnergy.lightEnergy / maxE : 0f;
            float dark01 = maxE > 0f ? playerEnergy.darkEnergy / maxE : 0f;

            HandleEnergyChanged(light01, dark01);
        }

        // Al iniciar ocultamos ambos indicadores del multiplicador
        HideAllMultiplierIndicators();
    }

    /// Actualiza SOLO el valor numérico del score.
    /// El texto "Score" o "Puntaje" va en otro TMP aparte en la UI.
    private void UpdateScoreUI()
    {
        if (scoreValueText == null || gameManager == null) return;

        int scoreInt = Mathf.RoundToInt(gameManager.Score);
        scoreValueText.text = scoreInt.ToString();
    }

    /// Actualiza sliders y porcentajes de energía.
    /// Recibe valores normalizados entre 0 y 1.
    private void HandleEnergyChanged(float light01, float dark01)
    {
        // Actualizar barras
        if (lightEnergySlider != null)
            lightEnergySlider.value = light01;

        if (darkEnergySlider != null)
            darkEnergySlider.value = dark01;

        // Actualizar porcentajes
        if (lightPercentText != null)
            lightPercentText.text = $"{Mathf.RoundToInt(light01 * 100f)}%";

        if (darkPercentText != null)
            darkPercentText.text = $"{Mathf.RoundToInt(dark01 * 100f)}%";
    }

    /// Muestra el indicador correcto del multiplicador:
    /// - x2 => mostrar solo multiplierX2Indicator
    /// - x4 => mostrar solo multiplierX4Indicator
    /// - sin buff => ocultar ambos
    private void HandleScoreMultiplierChanged(float multiplier, float remaining)
    {
        // Primero ocultamos todo
        HideAllMultiplierIndicators();

        // Si no hay buff activo, no mostramos nada
        if (remaining <= 0f || multiplier <= 1f) return;

        // Mostrar el indicador correcto según el multiplicador
        if (Mathf.Approximately(multiplier, 2f))
        {
            if (multiplierX2Indicator != null)
                multiplierX2Indicator.SetActive(true);
        }
        else if (Mathf.Approximately(multiplier, 4f))
        {
            if (multiplierX4Indicator != null)
                multiplierX4Indicator.SetActive(true);
        }
    }

    /// Oculta todos los indicadores visuales de multiplicador.
    private void HideAllMultiplierIndicators()
    {
        if (multiplierX2Indicator != null)
            multiplierX2Indicator.SetActive(false);

        if (multiplierX4Indicator != null)
            multiplierX4Indicator.SetActive(false);
    }

    /// Ejecuta pausa desde el botón del HUD.
    private void OnPausePressed()
    {
        if (gameManager == null) return;
        gameManager.TogglePause();
    }
}