using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class MenuButtonFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Scale (Hover)")]
    public float normalScale = 1f;
    public float highlightedScale = 1.06f;
    [Tooltip("Más alto = más rápido")]
    public float scaleSpeed = 16f;

    [Header("Button Colors (Image)")]
    public Image targetImage; // si está vacío, usa el Image del mismo GO
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Text (optional)")]
    [Tooltip("Si usas TextMeshPro, asígnalo. Si no, puede quedarse vacío.")]
    public TextMeshProUGUI tmpText;

    [Tooltip("Si usas Text (legacy), asígnalo. Si no, puede quedarse vacío.")]
    public Text legacyText;

    public Color normalTextColor = Color.white;
    public Color pressedTextColor = Color.white;

    [Header("Smoothing")]
    public bool smoothColor = true;
    public float colorSpeed = 18f;

    [Header("Special Intro (Solo cuando tú lo llames)")]
    [Tooltip("Escala inicial de la animación especial.")]
    public float introStartScale = 0.88f;

    [Tooltip("Duración total de la animación especial.")]
    public float introDuration = 0.35f;

    [Tooltip("Overshoot para el 'pop'. 1.02 - 1.08 recomendado.")]
    public float introOvershoot = 1.05f;

    [Tooltip("Delay random para escalonar entrada (0 - 0.12 recomendado).")]
    public Vector2 introDelayRange = new Vector2(0f, 0.08f);

    public AnimationCurve introEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rt;

    private Vector3 targetScale;
    private Color targetBgColor;
    private Color targetTxColor;

    private bool isHover;
    private bool isPressed;

    private Coroutine introCo;

    private void Awake()
    {
        rt = transform as RectTransform;

        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Auto-detect texto si no lo asignaste
        if (tmpText == null)
            tmpText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (legacyText == null)
            legacyText = GetComponentInChildren<Text>(true);

        ApplyImmediateNormal();
    }

    private void OnEnable()
    {
        ApplyImmediateNormal();
    }

    private void OnDisable()
    {
        ApplyImmediateNormal();
        StopIntroIfRunning();
    }

    private void Update()
    {
        if (rt != null)
            rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.unscaledDeltaTime * scaleSpeed);

        if (targetImage != null)
        {
            if (smoothColor)
                targetImage.color = Color.Lerp(targetImage.color, targetBgColor, Time.unscaledDeltaTime * colorSpeed);
            else
                targetImage.color = targetBgColor;
        }

        // Texto (lerp)
        if (smoothColor)
        {
            if (tmpText != null)
                tmpText.color = Color.Lerp(tmpText.color, targetTxColor, Time.unscaledDeltaTime * colorSpeed);

            if (legacyText != null)
                legacyText.color = Color.Lerp(legacyText.color, targetTxColor, Time.unscaledDeltaTime * colorSpeed);
        }
        else
        {
            if (tmpText != null) tmpText.color = targetTxColor;
            if (legacyText != null) legacyText.color = targetTxColor;
        }
    }

    // ---------- Public ----------
    public void ForceNormal()
    {
        isHover = false;
        isPressed = false;
        ApplyImmediateNormal();
    }

    /// <summary>
    /// Animación especial SOLO cuando tú la llames (no interfiere con hover/pressed).
    /// </summary>
    public void PlayIntro()
    {
        StopIntroIfRunning();
        introCo = StartCoroutine(IntroRoutine());
    }

    // ---------- Pointer ----------
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
        RefreshTargets();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;
        isPressed = false;
        RefreshTargets();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        RefreshTargets();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        RefreshTargets();
    }

    // ---------- Selection ----------
    public void OnSelect(BaseEventData eventData)
    {
        isHover = true;
        RefreshTargets();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHover = false;
        isPressed = false;
        RefreshTargets();
    }

    // ---------- Internals ----------
    private void RefreshTargets()
    {
        // Scale por hover
        float s = isHover ? highlightedScale : normalScale;
        targetScale = Vector3.one * s;

        // Color bg por pressed
        targetBgColor = isPressed ? pressedColor : normalColor;

        // Color texto por pressed
        targetTxColor = isPressed ? pressedTextColor : normalTextColor;
    }

    private void ApplyImmediateNormal()
    {
        targetScale = Vector3.one * normalScale;
        targetBgColor = normalColor;
        targetTxColor = normalTextColor;

        if (rt != null) rt.localScale = targetScale;

        if (targetImage != null) targetImage.color = targetBgColor;

        if (tmpText != null) tmpText.color = targetTxColor;
        if (legacyText != null) legacyText.color = targetTxColor;
    }

    private IEnumerator IntroRoutine()
    {
        float delay = Random.Range(introDelayRange.x, introDelayRange.y);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // Respeta el estado actual (si ya está hovered al terminar)
        RefreshTargets();
        Vector3 end = targetScale;

        // Start pequeño
        if (rt != null)
            rt.localScale = Vector3.one * introStartScale;

        float t = 0f;
        float upTime = introDuration * 0.6f;
        float downTime = Mathf.Max(0.0001f, introDuration - upTime);

        // Subida a overshoot
        while (t < upTime)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / upTime);
            float e = introEase != null ? introEase.Evaluate(n) : n;

            float s = Mathf.Lerp(introStartScale, introOvershoot, e);
            if (rt != null) rt.localScale = Vector3.one * s;

            yield return null;
        }

        // Bajada a end (normal o highlighted según hover)
        t = 0f;
        Vector3 start = Vector3.one * introOvershoot;

        while (t < downTime)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / downTime);
            float e = introEase != null ? introEase.Evaluate(n) : n;

            if (rt != null) rt.localScale = Vector3.Lerp(start, end, e);

            yield return null;
        }

        if (rt != null) rt.localScale = end;

        introCo = null;
    }

    private void StopIntroIfRunning()
    {
        if (introCo != null)
        {
            StopCoroutine(introCo);
            introCo = null;
        }
    }
}
