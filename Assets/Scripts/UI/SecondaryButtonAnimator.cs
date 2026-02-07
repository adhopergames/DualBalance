using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class SecondaryButtonAnimator : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Scale Settings")]
    public float hoverScale = 1.05f;
    public float pressedScale = 0.95f;
    public float animationSpeed = 10f;

    private RectTransform rect;
    private Vector3 defaultScale;
    private Vector3 targetScale;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        defaultScale = rect.localScale;
        targetScale = defaultScale;
    }

    void Update()
    {
        rect.localScale = Vector3.Lerp(
            rect.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = defaultScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = defaultScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = defaultScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = defaultScale * hoverScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = defaultScale * hoverScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = defaultScale;
    }

    /// Llamar manualmente si vuelves a un menú
    public void ResetState()
    {
        rect.localScale = defaultScale;
        targetScale = defaultScale;
    }
}
