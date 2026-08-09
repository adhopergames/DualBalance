using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    private const string TutorialSeenKey = "TUTORIAL_SEEN";

    [Header("UI")]
    [Tooltip("Root completo del tutorial.")]
    public GameObject tutorialRoot;

    [Tooltip("Cada slide es un GameObject completo con imagen y textos.")]
    public GameObject[] slides;

    [Tooltip("Texto tipo 1/3, 2/3, 3/3.")]
    public TMP_Text stepCounterText;

    [Tooltip("Botón para pasar al siguiente slide.")]
    public Button nextButton;

    [Tooltip("Botón que aparece solo en el último slide para empezar.")]
    public Button startButton;

    [Tooltip("Botón para saltar el tutorial.")]
    public Button skipButton;

    [Header("Show Timing")]
    [Tooltip(
        "Espera adicional en tiempo real después de terminar " +
        "la transición del LevelLoader."
    )]
    [SerializeField]
    private float showDelayRealtime = 0.1f;

    [Header("Animation")]
    [SerializeField]
    private float animInDuration = 0.15f;

    [SerializeField]
    private float animOutDuration = 0.12f;

    [SerializeField, Range(0.5f, 1f)]
    private float popStartScale = 0.92f;

    [SerializeField]
    private AnimationCurve easeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Slide Transition")]
    [SerializeField]
    private float slideAnimDuration = 0.16f;

    [SerializeField]
    private float slideMoveOffset = 120f;

    private int currentSlide;
    private bool alreadySeenTutorial;
    private bool entryTransitionFinished;

    private CanvasGroup tutorialGroup;
    private RectTransform tutorialRT;

    private Coroutine animCo;
    private Coroutine slideCo;
    private Coroutine showTutorialCo;

    private void Awake()
    {
        PrepareTutorialInitialState();
    }

    private void OnEnable()
    {
        LevelLoader.OnEntryTransitionFinished +=
            HandleEntryTransitionFinished;
    }

    private void Start()
    {
        ConfigureButtonListeners();

        alreadySeenTutorial =
            PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

        if (alreadySeenTutorial)
        {
            HideTutorialImmediately();
            return;
        }

        /*
         * Esperamos a que LevelLoader confirme que la animación
         * de entrada terminó antes de mostrar y pausar el tutorial.
         */
        showTutorialCo =
            StartCoroutine(ShowTutorialAfterTransitionRoutine());
    }

    private void OnDisable()
    {
        LevelLoader.OnEntryTransitionFinished -=
            HandleEntryTransitionFinished;
    }

    private void OnDestroy()
    {
        RemoveButtonListeners();
    }

    private void PrepareTutorialInitialState()
    {
        if (tutorialRoot != null)
        {
            tutorialRT =
                tutorialRoot.GetComponent<RectTransform>();

            tutorialGroup =
                tutorialRoot.GetComponent<CanvasGroup>();

            if (tutorialGroup == null)
            {
                tutorialGroup =
                    tutorialRoot.AddComponent<CanvasGroup>();
            }

            tutorialRoot.SetActive(false);

            tutorialGroup.alpha = 0f;
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;

            if (tutorialRT != null)
            {
                tutorialRT.localScale =
                    Vector3.one * popStartScale;
            }
        }

        HideAllSlides();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }
    }

    private void ConfigureButtonListeners()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(NextStep);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(FinishTutorial);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(FinishTutorial);
        }
    }

    private void RemoveButtonListeners()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextStep);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(FinishTutorial);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(FinishTutorial);
        }
    }

    private void HandleEntryTransitionFinished()
    {
        entryTransitionFinished = true;
    }

    private IEnumerator ShowTutorialAfterTransitionRoutine()
    {
        /*
         * Esperamos explícitamente la señal del LevelLoader.
         * Esto evita que Time.timeScale llegue a 0 mientras
         * el círculo negro todavía está animándose.
         */
        while (!entryTransitionFinished)
        {
            yield return null;
        }

        if (showDelayRealtime > 0f)
        {
            yield return new WaitForSecondsRealtime(
                showDelayRealtime
            );
        }

        ShowTutorial();
        showTutorialCo = null;
    }

    private void ShowTutorial()
    {
        currentSlide = 0;

        RefreshStep();

        /*
         * Pausamos después de que el loader terminó.
         */
        GameManager.Instance?.EnterTutorial();

        if (animCo != null)
        {
            StopCoroutine(animCo);
        }

        animCo = StartCoroutine(
            AnimateTutorial(open: true)
        );
    }

    private void RefreshStep()
    {
        if (slides == null || slides.Length == 0)
        {
            return;
        }

        if (currentSlide < 0 ||
            currentSlide >= slides.Length)
        {
            return;
        }

        if (stepCounterText != null)
        {
            stepCounterText.text =
                $"{currentSlide + 1}/{slides.Length}";
        }

        bool isLast =
            currentSlide == slides.Length - 1;

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(!isLast);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(isLast);
        }

        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(!isLast);
        }

        if (slideCo != null)
        {
            StopCoroutine(slideCo);
        }

        slideCo =
            StartCoroutine(AnimateSlideTransition());
    }

    private void NextStep()
    {
        currentSlide++;

        if (slides == null ||
            currentSlide >= slides.Length)
        {
            FinishTutorial();
            return;
        }

        RefreshStep();
    }

    public void FinishTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();

        if (animCo != null)
        {
            StopCoroutine(animCo);
        }

        animCo =
            StartCoroutine(FinishTutorialRoutine());
    }

    private IEnumerator FinishTutorialRoutine()
    {
        yield return StartCoroutine(
            AnimateTutorial(open: false)
        );

        HideAllSlides();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        GameManager.Instance?.FinishTutorial();
    }

    private IEnumerator AnimateTutorial(bool open)
    {
        if (tutorialRoot == null ||
            tutorialGroup == null)
        {
            animCo = null;
            yield break;
        }

        float duration =
            open ? animInDuration : animOutDuration;

        float initialAlpha = open ? 0f : 1f;
        float finalAlpha = open ? 1f : 0f;

        Vector3 initialScale =
            Vector3.one *
            (open ? popStartScale : 1f);

        Vector3 finalScale =
            Vector3.one *
            (open ? 1f : popStartScale);

        if (open)
        {
            tutorialRoot.SetActive(true);

            tutorialGroup.alpha = initialAlpha;
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;

            if (tutorialRT != null)
            {
                tutorialRT.localScale =
                    initialScale;
            }
        }
        else
        {
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;
        }

        if (duration <= 0f)
        {
            tutorialGroup.alpha = finalAlpha;

            if (tutorialRT != null)
            {
                tutorialRT.localScale =
                    finalScale;
            }
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(elapsed / duration);

                float easedTime =
                    easeCurve != null
                        ? easeCurve.Evaluate(
                            normalizedTime
                        )
                        : normalizedTime;

                tutorialGroup.alpha =
                    Mathf.Lerp(
                        initialAlpha,
                        finalAlpha,
                        easedTime
                    );

                if (tutorialRT != null)
                {
                    tutorialRT.localScale =
                        Vector3.Lerp(
                            initialScale,
                            finalScale,
                            easedTime
                        );
                }

                yield return null;
            }
        }

        tutorialGroup.alpha = finalAlpha;

        if (tutorialRT != null)
        {
            tutorialRT.localScale = finalScale;
        }

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
        if (slides == null ||
            slides.Length == 0)
        {
            slideCo = null;
            yield break;
        }

        GameObject newSlide =
            slides[currentSlide];

        GameObject oldSlide = null;

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null &&
                slides[i].activeSelf &&
                i != currentSlide)
            {
                oldSlide = slides[i];
                break;
            }
        }

        if (oldSlide == null)
        {
            HideAllSlides();

            if (newSlide != null)
            {
                CanvasGroup newOnlyGroup =
                    GetOrAddCanvasGroup(newSlide);

                RectTransform newOnlyRT =
                    newSlide.GetComponent<RectTransform>();

                newSlide.SetActive(true);

                if (newOnlyGroup != null)
                {
                    newOnlyGroup.alpha = 1f;
                    newOnlyGroup.interactable = true;
                    newOnlyGroup.blocksRaycasts = true;
                }

                if (newOnlyRT != null)
                {
                    newOnlyRT.anchoredPosition =
                        Vector2.zero;
                }
            }

            slideCo = null;
            yield break;
        }

        CanvasGroup newGroup =
            GetOrAddCanvasGroup(newSlide);

        RectTransform newRT =
            newSlide != null
                ? newSlide.GetComponent<RectTransform>()
                : null;

        CanvasGroup oldGroup =
            GetOrAddCanvasGroup(oldSlide);

        RectTransform oldRT =
            oldSlide != null
                ? oldSlide.GetComponent<RectTransform>()
                : null;

        if (newSlide != null)
        {
            newSlide.SetActive(true);
        }

        Vector2 oldStartPosition = Vector2.zero;

        Vector2 oldEndPosition =
            new Vector2(-slideMoveOffset, 0f);

        Vector2 newStartPosition =
            new Vector2(slideMoveOffset, 0f);

        Vector2 newEndPosition = Vector2.zero;

        if (oldGroup != null)
        {
            oldGroup.alpha = 1f;
            oldGroup.interactable = false;
            oldGroup.blocksRaycasts = false;
        }

        if (oldRT != null)
        {
            oldRT.anchoredPosition =
                oldStartPosition;
        }

        if (newGroup != null)
        {
            newGroup.alpha = 0f;
            newGroup.interactable = false;
            newGroup.blocksRaycasts = false;
        }

        if (newRT != null)
        {
            newRT.anchoredPosition =
                newStartPosition;
        }

        if (slideAnimDuration <= 0f)
        {
            ApplyFinalSlideState(
                newGroup,
                newRT,
                newEndPosition
            );
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < slideAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float normalizedTime =
                    Mathf.Clamp01(
                        elapsed / slideAnimDuration
                    );

                float easedTime =
                    easeCurve != null
                        ? easeCurve.Evaluate(
                            normalizedTime
                        )
                        : normalizedTime;

                if (oldGroup != null)
                {
                    oldGroup.alpha =
                        Mathf.Lerp(
                            1f,
                            0f,
                            easedTime
                        );
                }

                if (oldRT != null)
                {
                    oldRT.anchoredPosition =
                        Vector2.Lerp(
                            oldStartPosition,
                            oldEndPosition,
                            easedTime
                        );
                }

                if (newGroup != null)
                {
                    newGroup.alpha =
                        Mathf.Lerp(
                            0f,
                            1f,
                            easedTime
                        );
                }

                if (newRT != null)
                {
                    newRT.anchoredPosition =
                        Vector2.Lerp(
                            newStartPosition,
                            newEndPosition,
                            easedTime
                        );
                }

                yield return null;
            }

            ApplyFinalSlideState(
                newGroup,
                newRT,
                newEndPosition
            );
        }

        if (oldRT != null)
        {
            oldRT.anchoredPosition =
                Vector2.zero;
        }

        if (oldSlide != null)
        {
            oldSlide.SetActive(false);
        }

        slideCo = null;
    }

    private static void ApplyFinalSlideState(
        CanvasGroup newGroup,
        RectTransform newRT,
        Vector2 finalPosition
    )
    {
        if (newGroup != null)
        {
            newGroup.alpha = 1f;
            newGroup.interactable = true;
            newGroup.blocksRaycasts = true;
        }

        if (newRT != null)
        {
            newRT.anchoredPosition =
                finalPosition;
        }
    }

    private void HideAllSlides()
    {
        if (slides == null)
        {
            return;
        }

        for (int i = 0; i < slides.Length; i++)
        {
            if (slides[i] != null)
            {
                slides[i].SetActive(false);
            }
        }
    }

    private void HideTutorialImmediately()
    {
        if (tutorialRoot != null)
        {
            tutorialRoot.SetActive(false);
        }

        if (tutorialGroup != null)
        {
            tutorialGroup.alpha = 0f;
            tutorialGroup.interactable = false;
            tutorialGroup.blocksRaycasts = false;
        }

        HideAllSlides();
    }

    private CanvasGroup GetOrAddCanvasGroup(
        GameObject target
    )
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup =
            target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                target.AddComponent<CanvasGroup>();
        }

        return canvasGroup;
    }

    [ContextMenu("DEBUG/Reset Tutorial")]
    public void ResetTutorialFlag()
    {
        PlayerPrefs.DeleteKey(TutorialSeenKey);
        PlayerPrefs.Save();

        Debug.Log("Tutorial reset.");
    }
}