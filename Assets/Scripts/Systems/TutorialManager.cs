using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    private const string TutorialSeenKey = "TUTORIAL_SEEN";

    [Header("UI")]
    [Tooltip("Root completo del tutorial")]
    public GameObject tutorialRoot;

    [Tooltip("Cada slide es un GameObject completo con imagen + textos localizados")]
    public GameObject[] slides;

    [Tooltip("Texto tipo 1/3, 2/3, 3/3")]
    public TMP_Text stepCounterText;

    [Tooltip("Botón para pasar al siguiente slide")]
    public Button nextButton;

    [Tooltip("Botón que aparece solo en el último slide para empezar")]
    public Button startButton;

    [Tooltip("Botón para saltar el tutorial")]
    public Button skipButton;

    [Header("Show Timing")]
    [Tooltip("Pequeña espera en tiempo real antes de mostrar el tutorial. Sirve para no pelear visualmente con la transición.")]
    public float showDelayRealtime = 0.20f;

    [Header("Animation (like PauseUI)")]
    [SerializeField] private float animInDuration = 0.15f;
    [SerializeField] private float animOutDuration = 0.12f;
    [SerializeField, Range(0.5f, 1f)] private float popStartScale = 0.92f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("Slide Transition")]
    [SerializeField] private float slideAnimDuration = 0.16f;
    [SerializeField] private float slideMoveOffset = 120f;

    private Coroutine slideCo;

    private int currentSlide = 0;
    private bool alreadySeenTutorial;

    private CanvasGroup tutorialGroup;
    private RectTransform tutorialRT;
    private Coroutine animCo;

    private void Awake()
    {
        // -------------------------
        // Estado inicial seguro
        // -------------------------
        if (tutorialRoot != null)
        {
            tutorialRT = tutorialRoot.GetComponent<RectTransform>();

            tutorialGroup = tutorialRoot.GetComponent<CanvasGroup>();
            if (tutorialGroup == null)
                tutorialGroup = tutorialRoot.AddComponent<CanvasGroup>();

            tutorialRoot.SetActive(false);
            tutorialGroup.alpha = 0f;
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;

            if (tutorialRT != null)
                tutorialRT.localScale = Vector3.one * popStartScale;
        }

        // Todos los slides arrancan ocultos
        HideAllSlides();

        // Estado inicial de botones
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (startButton != null) startButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        // -------------------------
        // Listeners de botones
        // -------------------------
        if (nextButton != null)
            nextButton.onClick.AddListener(NextStep);

        if (startButton != null)
            startButton.onClick.AddListener(FinishTutorial);

        if (skipButton != null)
            skipButton.onClick.AddListener(FinishTutorial);

        alreadySeenTutorial = PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

        // Si ya vio el tutorial, no hacemos nada
        if (alreadySeenTutorial)
        {
            if (tutorialRoot != null)
                tutorialRoot.SetActive(false);

            return;
        }

        // Mostrar tutorial después de una pequeña espera
        StartCoroutine(ShowTutorialAfterDelayRoutine());
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextStep);

        if (startButton != null)
            startButton.onClick.RemoveListener(FinishTutorial);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(FinishTutorial);
    }

    /// Espera unos instantes en tiempo real antes de mostrar el tutorial.
    /// Esto evita que aparezca encima de la transición de entrada.
    private IEnumerator ShowTutorialAfterDelayRoutine()
    {
        // Esperar al menos un frame para que la escena termine de armarse
        yield return null;

        // Espera adicional opcional para no chocar con el fade/transition
        if (showDelayRealtime > 0f)
            yield return new WaitForSecondsRealtime(showDelayRealtime);

        ShowTutorial();
    }

    /// Muestra el tutorial y entra al estado Tutorial del juego.
    private void ShowTutorial()
    {
        currentSlide = 0;

        // Entramos al estado Tutorial solo cuando realmente vamos a mostrarlo
        GameManager.Instance?.EnterTutorial();

        // Refrescamos primero el contenido
        RefreshStep();

        // Luego animamos la entrada
        if (animCo != null)
            StopCoroutine(animCo);

        animCo = StartCoroutine(AnimateTutorial(open: true));
    }

    /// Actualiza la UI del tutorial:
    /// - oculta todos los slides
    /// - activa solo el slide actual
    /// - actualiza contador
    /// - cambia entre botón "Siguiente" y botón "Empezar"
    private void RefreshStep()
    {
        if (slides == null || slides.Length == 0) return;
        if (currentSlide < 0 || currentSlide >= slides.Length) return;

        // Contador
        if (stepCounterText != null)
            stepCounterText.text = $"{currentSlide + 1}/{slides.Length}";

        // Botones
        bool isLast = currentSlide == slides.Length - 1;

        if (nextButton != null)
            nextButton.gameObject.SetActive(!isLast);

        if (startButton != null)
            startButton.gameObject.SetActive(isLast);

        // 🔥 Ocultar "Omitir" en el último slide
        if (skipButton != null)
            skipButton.gameObject.SetActive(!isLast);

        // Animación de transición
        if (slideCo != null)
            StopCoroutine(slideCo);

        slideCo = StartCoroutine(AnimateSlideTransition());
    }

    /// Oculta todos los slides.
    private void HideAllSlides()
    {
        if (slides == null) return;

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
                slides[i].SetActive(false);
        }
    }

    /// Avanza al siguiente slide.
    private void NextStep()
    {
        currentSlide++;

        if (slides == null || currentSlide >= slides.Length)
        {
            FinishTutorial();
            return;
        }

        RefreshStep();
    }

    /// Finaliza el tutorial:
    /// - guarda flag en PlayerPrefs
    /// - oculta slides
    /// - anima salida
    /// - devuelve el juego a Playing
    public void FinishTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();

        HideAllSlides();

        if (animCo != null)
            StopCoroutine(animCo);

        animCo = StartCoroutine(FinishTutorialRoutine());
    }

    private IEnumerator FinishTutorialRoutine()
    {
        // Animación de salida
        yield return StartCoroutine(AnimateTutorial(open: false));

        // Reset visual de botones para próximos usos/debug
        if (nextButton != null)
            nextButton.gameObject.SetActive(true);

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        // Volvemos al gameplay
        GameManager.Instance?.FinishTutorial();
    }

    /// Animación tipo PauseUI:
    /// fade + scale pop, usando tiempo no escalado.
    private IEnumerator AnimateTutorial(bool open)
    {
        if (tutorialRoot == null || tutorialGroup == null)
        {
            animCo = null;
            yield break;
        }

        float duration = open ? animInDuration : animOutDuration;

        float a0 = open ? 0f : 1f;
        float a1 = open ? 1f : 0f;

        Vector3 s0 = Vector3.one * (open ? popStartScale : 1f);
        Vector3 s1 = Vector3.one * (open ? 1f : popStartScale);

        if (open)
        {
            tutorialRoot.SetActive(true);

            tutorialGroup.alpha = 0f;
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;

            if (tutorialRT != null)
                tutorialRT.localScale = s0;
        }
        else
        {
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;
        }

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / duration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            tutorialGroup.alpha = Mathf.Lerp(a0, a1, eased);

            if (tutorialRT != null)
                tutorialRT.localScale = Vector3.Lerp(s0, s1, eased);

            yield return null;
        }

        tutorialGroup.alpha = a1;

        if (tutorialRT != null)
            tutorialRT.localScale = s1;

        if (open)
        {
            tutorialGroup.interactable = true;
            tutorialGroup.blocksRaycasts = true;
        }
        else
        {
            tutorialRoot.SetActive(false);
        }

        animCo = null;
    }

    private IEnumerator AnimateSlideTransition()
    {
        if (slides == null || slides.Length == 0)
            yield break;

        GameObject newSlide = slides[currentSlide];

        // Buscar el slide activo anterior
        GameObject oldSlide = null;
        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null && slides[i].activeSelf && i != currentSlide)
            {
                oldSlide = slides[i];
                break;
            }
        }

        // Si no hay slide anterior, solo mostrar el actual sin transición rara
        if (oldSlide == null)
        {
            HideAllSlides();

            if (newSlide != null)
            {
                CanvasGroup newOnlyGroup = GetOrAddCanvasGroup(newSlide);
                RectTransform newOnlyRT = newSlide.GetComponent<RectTransform>();

                newSlide.SetActive(true);

                if (newOnlyGroup != null)
                {
                    newOnlyGroup.alpha = 1f;
                    newOnlyGroup.interactable = true;
                    newOnlyGroup.blocksRaycasts = true;
                }

                if (newOnlyRT != null)
                    newOnlyRT.anchoredPosition = Vector2.zero;
            }

            slideCo = null;
            yield break;
        }

        CanvasGroup newGroup = GetOrAddCanvasGroup(newSlide);
        RectTransform newRT = newSlide.GetComponent<RectTransform>();

        CanvasGroup oldGroup = GetOrAddCanvasGroup(oldSlide);
        RectTransform oldRT = oldSlide.GetComponent<RectTransform>();

        // Activar el nuevo slide
        newSlide.SetActive(true);

        // Posiciones iniciales/finales
        Vector2 oldStartPos = Vector2.zero;
        Vector2 oldEndPos = new Vector2(-slideMoveOffset, 0f);

        Vector2 newStartPos = new Vector2(slideMoveOffset, 0f);
        Vector2 newEndPos = Vector2.zero;

        // Estado inicial
        if (oldGroup != null)
        {
            oldGroup.alpha = 1f;
            oldGroup.interactable = false;
            oldGroup.blocksRaycasts = false;
        }

        if (oldRT != null)
            oldRT.anchoredPosition = oldStartPos;

        if (newGroup != null)
        {
            newGroup.alpha = 0f;
            newGroup.interactable = false;
            newGroup.blocksRaycasts = false;
        }

        if (newRT != null)
            newRT.anchoredPosition = newStartPos;

        float t = 0f;

        while (t < slideAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / slideAnimDuration);
            float eased = easeCurve != null ? easeCurve.Evaluate(k) : k;

            // Viejo sale hacia la izquierda
            if (oldGroup != null)
                oldGroup.alpha = Mathf.Lerp(1f, 0f, eased);

            if (oldRT != null)
                oldRT.anchoredPosition = Vector2.Lerp(oldStartPos, oldEndPos, eased);

            // Nuevo entra desde la derecha
            if (newGroup != null)
                newGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            if (newRT != null)
                newRT.anchoredPosition = Vector2.Lerp(newStartPos, newEndPos, eased);

            yield return null;
        }

        // Estado final del nuevo slide
        if (newGroup != null)
        {
            newGroup.alpha = 1f;
            newGroup.interactable = true;
            newGroup.blocksRaycasts = true;
        }

        if (newRT != null)
            newRT.anchoredPosition = newEndPos;

        // Apagar y resetear el viejo slide
        if (oldRT != null)
            oldRT.anchoredPosition = Vector2.zero;

        if (oldSlide != null)
            oldSlide.SetActive(false);

        slideCo = null;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();

        return cg;
    }

    [ContextMenu("DEBUG/Reset Tutorial")]
    public void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey(TutorialSeenKey);
        PlayerPrefs.Save();
        Debug.Log("Tutorial reset.");
    }
}