using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class PauseUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject pausePanel;   // CanvasInterfaz/Pausa/PausaPanel
    [SerializeField] private GameObject pauseButton;  // Botón de pausa (icono)

    [Header("UI Actions (NEW Input System)")]
    [Tooltip("Asigna UI/Cancel (Back/Escape). Igual que en MainMenuUI.")]
    public InputActionReference backAction;

    [Header("Animation (code)")]
    [SerializeField] private float animInDuration = 0.15f;
    [SerializeField] private float animOutDuration = 0.12f;
    [Range(0.5f, 1f)]
    [SerializeField] private float popStartScale = 0.92f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup pauseGroup;
    private RectTransform pauseRT;

    private Coroutine animCo;
    private bool lastPaused;

    private bool backQueued;
    private bool backHooked;
    private bool pendingBack;

    private void Awake()
    {
        if (pausePanel == null) return;

        pauseRT = pausePanel.GetComponent<RectTransform>();

        pauseGroup = pausePanel.GetComponent<CanvasGroup>();
        if (pauseGroup == null)
            pauseGroup = pausePanel.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        HookBack(true);
        InputSystem.onEvent += OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -= OnInputEvent;
        HookBack(false);
    }

    private void HookBack(bool enable)
    {
        if (backAction == null || backAction.action == null) return;

        if (enable)
        {
            backQueued = false;
            pendingBack = false;
        }

        if (backAction.action.actionMap != null)
        {
            if (enable) backAction.action.actionMap.Enable();
            else backAction.action.actionMap.Disable();
        }
        else
        {
            if (enable) backAction.action.Enable();
            else backAction.action.Disable();
        }

        if (enable && !backHooked)
        {
            backAction.action.performed += OnBackPerformed;
            backHooked = true;
        }
        else if (!enable && backHooked)
        {
            backAction.action.performed -= OnBackPerformed;
            backHooked = false;
        }
    }

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);

        if (pauseGroup != null)
        {
            pauseGroup.alpha = 0f;
            pauseGroup.interactable = false;
            pauseGroup.blocksRaycasts = false;
        }

        if (pauseRT != null)
            pauseRT.localScale = Vector3.one * popStartScale;

        lastPaused = false;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // Durante tutorial: ocultar botón de pausa y no procesar back para pausa
        bool isTutorial = GameManager.Instance.State == GameState.Tutorial;
        if (pauseButton != null)
            pauseButton.SetActive(!isTutorial && GameManager.Instance.State != GameState.Paused);

        if (isTutorial)
            return;

        // Procesar back
        if (backQueued)
        {
            backQueued = false;

            if (animCo != null)
                pendingBack = true;
            else
                HandleBackButton();
        }

        bool isPaused = (GameManager.Instance.State == GameState.Paused);

        if (isPaused != lastPaused)
        {
            lastPaused = isPaused;

            if (animCo != null) StopCoroutine(animCo);
            animCo = StartCoroutine(AnimatePause(open: isPaused));
        }
    }

    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        backQueued = true;
    }

    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        if (device is Keyboard kb)
        {
            if (kb.escapeKey.ReadValueFromEvent(eventPtr) > 0.5f)
                backQueued = true;
        }
    }

    private void HandleBackButton()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State == GameState.Paused)
        {
            AudioManager.Instance?.PlayUIBack();
            GameManager.Instance.TogglePause();
            return;
        }
    }

    private IEnumerator AnimatePause(bool open)
    {
        if (pausePanel == null || pauseGroup == null)
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
            pausePanel.SetActive(true);
            if (pauseButton != null) pauseButton.SetActive(false);

            pauseGroup.alpha = 0f;
            pauseGroup.interactable = false;
            pauseGroup.blocksRaycasts = false;

            if (pauseRT != null) pauseRT.localScale = s0;
        }
        else
        {
            pauseGroup.interactable = false;
            pauseGroup.blocksRaycasts = false;
        }

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / duration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            pauseGroup.alpha = Mathf.Lerp(a0, a1, eased);

            if (pauseRT != null)
                pauseRT.localScale = Vector3.Lerp(s0, s1, eased);

            yield return null;
        }

        pauseGroup.alpha = a1;

        if (pauseRT != null)
            pauseRT.localScale = s1;

        if (open)
        {
            pauseGroup.interactable = true;
            pauseGroup.blocksRaycasts = true;
        }
        else
        {
            pausePanel.SetActive(false);

            if (pauseButton != null && GameManager.Instance != null && GameManager.Instance.State != GameState.Tutorial)
                pauseButton.SetActive(true);
        }

        animCo = null;

        if (pendingBack)
        {
            pendingBack = false;
            HandleBackButton();
        }
    }
}