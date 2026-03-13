using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la animación visual de un popup UI:
/// - Fade In
/// - Scale In
/// - Fade Out
///
/// Ideal para popups de logros, notificaciones, etc.
///
/// Requiere:
/// - RectTransform
/// - CanvasGroup (si no existe, se agrega automáticamente)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AchievementPopupAnimator : MonoBehaviour
{
    [Header("Animation Durations")]
    [Tooltip("Duración de la animación de entrada.")]
    public float showDuration = 0.25f;

    [Tooltip("Duración de la animación de salida.")]
    public float hideDuration = 0.20f;

    [Header("Scale")]
    [Tooltip("Escala inicial al aparecer.")]
    public Vector3 hiddenScale = new Vector3(0.85f, 0.85f, 1f);

    [Tooltip("Escala final visible.")]
    public Vector3 visibleScale = Vector3.one;

    [Tooltip("Escala final al ocultarse.")]
    public Vector3 hideScale = new Vector3(0.92f, 0.92f, 1f);

    [Header("Curve")]
    [Tooltip("Curva para suavizar la entrada.")]
    public AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Curva para suavizar la salida.")]
    public AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine currentAnimation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Buscar o crear CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Dejar el popup inicialmente oculto visualmente
        ResetToHiddenState();
    }

    /// <summary>
    /// Reproduce la animación de entrada.
    /// </summary>
    public void PlayShow()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        gameObject.SetActive(true);
        currentAnimation = StartCoroutine(AnimateShowRoutine());
    }

    /// <summary>
    /// Reproduce la animación de salida.
    /// </summary>
    public void PlayHide()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateHideRoutine());
    }

    /// <summary>
    /// Deja el popup listo en estado oculto.
    /// NO mueve la posición del elemento.
    /// </summary>
    public void ResetToHiddenState()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rectTransform.localScale = hiddenScale;
        canvasGroup.alpha = 0f;
    }

    private IEnumerator AnimateShowRoutine()
    {
        float t = 0f;

        Vector3 fromScale = hiddenScale;
        Vector3 toScale = visibleScale;

        canvasGroup.alpha = 0f;
        rectTransform.localScale = fromScale;

        while (t < showDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / showDuration);
            float eased = showCurve.Evaluate(normalized);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = toScale;

        currentAnimation = null;
    }

    private IEnumerator AnimateHideRoutine()
    {
        float t = 0f;

        Vector3 fromScale = rectTransform.localScale;
        Vector3 toScale = hideScale;

        float fromAlpha = canvasGroup.alpha;
        float toAlpha = 0f;

        while (t < hideDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / hideDuration);
            float eased = hideCurve.Evaluate(normalized);

            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
            rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = toScale;

        gameObject.SetActive(false);
        currentAnimation = null;
    }
}