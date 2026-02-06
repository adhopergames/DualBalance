using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// UIElementFX (NEW Input System + TMP)
/// - Hover: escala
/// - Pressed: escala + color (fondo y texto)
/// - Watchdog: si se pierde PointerUp en móvil, suelta pressed cuando ya no hay touch down
/// Reutilizable en cualquier UI con raycasts.
public class UIElementFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler,
    IBeginDragHandler, IEndDragHandler
{
    [Header("Targets")]
    [Tooltip("RectTransform que se escalará (si está vacío usa este mismo objeto).")]
    public RectTransform scaleTarget;

    [Tooltip("Graphic de fondo (normalmente Image). Opcional.")]
    public Graphic backgroundGraphic;

    [Tooltip("Texto TMP opcional.")]
    public TextMeshProUGUI tmpText;

    [Header("Scale")]
    public float normalScale = 1f;
    public float hoverScale = 1.06f;
    public float pressedScale = 0.98f;
    [Tooltip("Más alto = más rápido")]
    public float scaleSpeed = 16f;

    [Header("Background Color")]
    public bool changeBackgroundColor = true;
    public Color normalBgColor = Color.white;
    public Color pressedBgColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Text Color (TMP)")]
    public bool changeTextColor = true;
    public Color normalTextColor = Color.white;
    public Color pressedTextColor = Color.white;

    [Header("Smoothing")]
    public bool smoothColor = true;
    public float colorSpeed = 18f;

    [Header("Mobile Safety (New Input System)")]
    public bool enablePointerWatchdog = true;
    [Tooltip("Delay para no soltar por micro-cortes del touch (0.03 - 0.08 recomendado).")]
    public float watchdogReleaseDelay = 0.05f;

    // State
    private bool isHover;
    private bool isPressed;

    private Vector3 targetScale;
    private Color targetBgColor;
    private Color targetTextColor;

    // Para saber si este elemento recibió el down
    private bool ownsPress;
    private float watchdogTimer;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform as RectTransform;

        // Auto detect TMP si no lo asignaste
        if (tmpText == null)
            tmpText = GetComponentInChildren<TextMeshProUGUI>(true);

        ApplyImmediateNormal();
    }

    private void OnEnable() => ApplyImmediateNormal();

    private void OnDisable()
    {
        ApplyImmediateNormal();
        ownsPress = false;
        watchdogTimer = 0f;
    }

    private void Update()
    {
        // Scale
        if (scaleTarget != null)
            scaleTarget.localScale = Vector3.Lerp(
                scaleTarget.localScale,
                targetScale,
                Time.unscaledDeltaTime * scaleSpeed
            );

        // Background
        if (changeBackgroundColor && backgroundGraphic != null)
        {
            if (smoothColor)
                backgroundGraphic.color = Color.Lerp(
                    backgroundGraphic.color,
                    targetBgColor,
                    Time.unscaledDeltaTime * colorSpeed
                );
            else
                backgroundGraphic.color = targetBgColor;
        }

        // TMP Text
        if (changeTextColor && tmpText != null)
        {
            if (smoothColor)
                tmpText.color = Color.Lerp(tmpText.color, targetTextColor, Time.unscaledDeltaTime * colorSpeed);
            else
                tmpText.color = targetTextColor;
        }

        // ✅ Watchdog anti "pressed pegado" (solo si este control recibió el down)
        if (enablePointerWatchdog && ownsPress && isPressed)
        {
            if (!IsAnyPointerStillDown_NewInputSystem())
            {
                watchdogTimer += Time.unscaledDeltaTime;
                if (watchdogTimer >= watchdogReleaseDelay)
                {
                    isPressed = false;
                    ownsPress = false;
                    watchdogTimer = 0f;
                    RefreshTargets();
                }
            }
            else
            {
                watchdogTimer = 0f;
            }
        }
    }

    // ---------------- Public ----------------
    public void ForceNormal()
    {
        isHover = false;
        isPressed = false;
        ownsPress = false;
        watchdogTimer = 0f;
        ApplyImmediateNormal();
    }

    // ---------------- Events ----------------
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHover = true;
        RefreshTargets();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHover = false;

        // Si sale del botón y ya no hay touch down, suelta pressed.
        if (!IsAnyPointerStillDown_NewInputSystem())
        {
            isPressed = false;
            ownsPress = false;
            watchdogTimer = 0f;
        }

        RefreshTargets();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ownsPress = true;
        watchdogTimer = 0f;
        RefreshTargets();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ownsPress = false;
        watchdogTimer = 0f;
        RefreshTargets();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isHover = true;
        RefreshTargets();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHover = false;
        isPressed = false;
        ownsPress = false;
        watchdogTimer = 0f;
        RefreshTargets();
    }

    // Si inicia drag (scroll, etc.), consideramos cancel visual del pressed
    public void OnBeginDrag(PointerEventData eventData)
    {
        isPressed = false;
        RefreshTargets();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isPressed = false;
        ownsPress = false;
        watchdogTimer = 0f;
        RefreshTargets();
    }

    // ---------------- Internals ----------------
    private void RefreshTargets()
    {
        if (isPressed)
            targetScale = Vector3.one * pressedScale;
        else if (isHover)
            targetScale = Vector3.one * hoverScale;
        else
            targetScale = Vector3.one * normalScale;

        targetBgColor = isPressed ? pressedBgColor : normalBgColor;
        targetTextColor = isPressed ? pressedTextColor : normalTextColor;
    }

    private void ApplyImmediateNormal()
    {
        targetScale = Vector3.one * normalScale;
        targetBgColor = normalBgColor;
        targetTextColor = normalTextColor;

        if (scaleTarget != null)
            scaleTarget.localScale = targetScale;

        if (backgroundGraphic != null)
            backgroundGraphic.color = targetBgColor;

        if (tmpText != null)
            tmpText.color = targetTextColor;
    }

    private bool IsAnyPointerStillDown_NewInputSystem()
    {
        // Touch principal
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;

        // Mouse (por si pruebas en desktop con Input System UI)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        // Pen (opcional)
        if (Pen.current != null && Pen.current.tip.isPressed)
            return true;

        return false;
    }
}
