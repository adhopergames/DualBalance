using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuFlyByUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rect;      // HawtepMenuFly
    [SerializeField] private Image image;             // Image del personaje
    [SerializeField] private Animator animator;       // Animator del personaje

    [Header("Fly Area")]
    [Tooltip("Área UI donde debe moverse (Panel/Viewport). Si es null usa el parent.")]
    [SerializeField] private RectTransform flyArea;

    [Header("Margins (px)")]
    [Tooltip("Qué tan fuera del área nace y desaparece (más = más fuera).")]
    [SerializeField] private float marginOutside = 150f;

    [Header("Horizontal")]
    [Tooltip("Margen lateral para que no nazca pegado a los bordes.")]
    [SerializeField] private float sidePadding = 80f;

    [Tooltip("Cuánto se mueve en X mientras sube (0 = recto).")]
    [SerializeField] private float driftX = 250f;

    [Header("Speed")]
    [Tooltip("Velocidad vertical en px/seg.")]
    [SerializeField] private float verticalSpeed = 450f;

    [Header("Timing")]
    [Tooltip("Tiempo random entre apariciones (segundos).")]
    [SerializeField] private Vector2 delayBetweenPasses = new Vector2(2.5f, 6f);

    [Tooltip("Tiempo extra visible al final antes de apagar.")]
    [SerializeField] private float endHoldSeconds = 0f;

    private Coroutine routine;

    private void Awake()
    {
        // ✅ Autoreferencias por si olvidas arrastrar
        if (rect == null) rect = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<Image>();
        if (animator == null) animator = GetComponent<Animator>();

        // ✅ Si no defines flyArea, usa el padre (común en UI)
        if (flyArea == null && rect != null)
            flyArea = rect.parent as RectTransform;
    }

    private void OnEnable()
    {
        routine = StartCoroutine(FlyLoop());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        SetVisible(false);
    }

    private IEnumerator FlyLoop()
    {
        // ✅ Empieza apagado
        SetVisible(false);

        // Espera 1 frame para asegurar que Layout/Canvas ya calcularon tamaños
        yield return null;

        while (true)
        {
            // ✅ Espera real (no depende de timeScale)
            float wait = Random.Range(delayBetweenPasses.x, delayBetweenPasses.y);
            yield return new WaitForSecondsRealtime(wait);

            if (rect == null || flyArea == null) continue;

            // -------------------------
            // 1) Calcular límites del área
            // -------------------------
            // En UI, si estás centrado, el área va de -w/2..+w/2 y -h/2..+h/2
            float halfW = flyArea.rect.width * 0.5f;
            float halfH = flyArea.rect.height * 0.5f;

            // Límites dentro del área (para X), y fuera del área (para Y spawn/exit)
            float leftX = -halfW + sidePadding;
            float rightX = halfW - sidePadding;

            float spawnY = -halfH - marginOutside; // ✅ abajo fuera
            float exitY = halfH + marginOutside;   // ✅ arriba fuera

            // -------------------------
            // 2) Elegir start y end
            // -------------------------
            float startX = Random.Range(leftX, rightX);

            // drift random (izq/der), pero sin salirse demasiado del área
            float drift = Random.Range(-driftX, driftX);
            float endX = Mathf.Clamp(startX + drift, leftX, rightX);

            Vector2 start = new Vector2(startX, spawnY);
            Vector2 end = new Vector2(endX, exitY);

            // -------------------------
            // 3) Encender y mover
            // -------------------------
            rect.anchoredPosition = start;

            // ✅ IMPORTANTE: asegurar que el objeto se vea y el animator corra
            SetVisible(true);

            // Duración según distancia vertical y velocidad
            float distanceY = Mathf.Abs(exitY - spawnY);
            float duration = Mathf.Max(0.1f, distanceY / Mathf.Max(1f, verticalSpeed));

            float t = 0f;
            while (t < 1f)
            {
                // ✅ unscaled: funciona aunque el juego esté pausado
                t += Time.unscaledDeltaTime / duration;

                rect.anchoredPosition = Vector2.Lerp(start, end, Mathf.Clamp01(t));
                yield return null;
            }

            if (endHoldSeconds > 0f)
                yield return new WaitForSecondsRealtime(endHoldSeconds);

            // ✅ Apaga todo al salir
            SetVisible(false);
        }
    }

    private void SetVisible(bool visible)
    {
        // ✅ no renderiza cuando está apagado
        if (image != null) image.enabled = visible;

        // ✅ no consume animación cuando está apagado
        if (animator != null) animator.enabled = visible;
    }
}
