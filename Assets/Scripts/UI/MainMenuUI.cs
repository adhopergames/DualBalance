using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "Game";

    [Header("UI Actions (NEW Input System)")]
    public InputActionReference backAction; // UI/Cancel

    [Header("Panels (RectTransform + CanvasGroup)")]
    public GameObject panelMainMenu;
    public GameObject panelHistoria;
    public GameObject panelLogros;
    public GameObject panelAjustes;

    [Header("Exit Confirm (Overlay)")]
    public GameObject panelExitConfirm;
    public RectTransform exitDialogBox;

    [Header("Root Input Block (RECOMENDADO)")]
    public CanvasGroup canvasRootGroup;


    [Header("Level Loader (en esta escena)")]
    [SerializeField] private LevelLoader levelLoader;


    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private float slideDistance = 900f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Soft Fade (sutil)")]
    [SerializeField] private bool useSoftFade = true;
    [Range(0.5f, 1f)]
    [SerializeField] private float softFadeMinAlpha = 0.92f;

    [Header("Exit Confirm Animation")]
    [SerializeField] private float exitAnimDuration = 0.18f;
    [Range(0.5f, 1f)]
    [SerializeField] private float exitPopStartScale = 0.94f;

    private RectTransform currentPanelRT;
    private RectTransform mainMenuRT;
    private RectTransform exitConfirmRT;

    private Coroutine transitionCo;
    private Coroutine exitAnimCo;

    private bool isTransitioning;
    private bool isExitConfirmOpen;

    // Back robusto
    private bool backQueued;
    private bool backHooked;
    private bool pendingBack;

    private void OnEnable()
    {
        HookBack(true);

        // ✅ Captura low-level: evita que el primer Back “se pierda”
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

        // Activa ActionMap completo si existe
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
        mainMenuRT = panelMainMenu != null ? panelMainMenu.GetComponent<RectTransform>() : null;
        exitConfirmRT = panelExitConfirm != null ? panelExitConfirm.GetComponent<RectTransform>() : null;

        HideAllPanels();

        if (panelMainMenu != null && mainMenuRT != null)
        {
            panelMainMenu.SetActive(true);
            currentPanelRT = mainMenuRT;

            SetPanelAnchoredX(currentPanelRT, 0f);
            SetAlpha(currentPanelRT, 1f);
            SetInteractable(currentPanelRT, true);
        }

        ClearUISelection();
        ResetMenuFX(panelMainMenu);
        PlayIntroOnPanel(panelMainMenu);
    }

    private void Update()
    {
        if (!backQueued) return;
        backQueued = false;

        if (isTransitioning)
            pendingBack = true;
        else
            HandleBackButton();
    }

    private void OnBackPerformed(InputAction.CallbackContext ctx)
    {
        backQueued = true;
    }

    // ✅ Low-level: detecta Escape aunque Keyboard.current sea null (Android primer back)
    private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
    {
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        // Si el dispositivo tiene KeyControl Escape, lo leemos
        if (device is Keyboard kb)
        {
            // OJO: esto funciona incluso si Keyboard.current estaba null al inicio
            if (kb.escapeKey.ReadValueFromEvent(eventPtr) > 0.5f)
                backQueued = true;
        }
    }

    private void HandleBackButton()
    {
        if (isExitConfirmOpen)
        {
            CloseExitConfirm();
            return;
        }

        if (mainMenuRT != null && currentPanelRT != mainMenuRT)
        {
            GoTo(panelMainMenu);
            return;
        }

        OpenExitConfirm();
    }

    // -------------------------
    // BOTONES
    // -------------------------

    public void OnPlayPressed()
    {
        Time.timeScale = 1f;

        if (levelLoader != null)
            levelLoader.LoadScene(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }


    public void OnHistoriaPressed() => GoTo(panelHistoria);
    public void OnAchievementsPressed() => GoTo(panelLogros);
    public void OnSettingsPressed() => GoTo(panelAjustes);
    public void OnBackPressed() => GoTo(panelMainMenu);

    public void OnQuitPressed() => OpenExitConfirm();
    public void OnExitYesPressed() => Application.Quit();
    public void OnExitNoPressed() => CloseExitConfirm();

    // -------------------------
    // EXIT CONFIRM
    // -------------------------
    private void OpenExitConfirm()
    {
        if (panelExitConfirm == null || exitConfirmRT == null) return;
        if (isExitConfirmOpen) return;

        isExitConfirmOpen = true;

        panelExitConfirm.SetActive(true);
        SetInteractable(exitConfirmRT, false);
        SetAlpha(exitConfirmRT, 0f);

        if (currentPanelRT != null) SetInteractable(currentPanelRT, false);

        ClearUISelection();
        if (currentPanelRT != null) ResetMenuFX(currentPanelRT.gameObject);

        if (exitAnimCo != null) StopCoroutine(exitAnimCo);
        exitAnimCo = StartCoroutine(AnimateExitConfirm(open: true));
    }

    private void CloseExitConfirm()
    {
        if (panelExitConfirm == null || exitConfirmRT == null) return;
        if (!isExitConfirmOpen) return;

        isExitConfirmOpen = false;
        ClearUISelection();

        if (exitAnimCo != null) StopCoroutine(exitAnimCo);
        exitAnimCo = StartCoroutine(AnimateExitConfirm(open: false));
    }

    private IEnumerator AnimateExitConfirm(bool open)
    {
        BlockGlobalInput(true);

        Vector3 endScale = Vector3.one;
        Vector3 startScale = Vector3.one * exitPopStartScale;

        if (exitDialogBox != null)
            exitDialogBox.localScale = open ? startScale : endScale;

        float t = 0f;
        float a0 = open ? 0f : 1f;
        float a1 = open ? 1f : 0f;

        while (t < exitAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / exitAnimDuration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            SetAlpha(exitConfirmRT, Mathf.Lerp(a0, a1, eased));

            if (exitDialogBox != null)
            {
                Vector3 s0 = open ? startScale : endScale;
                Vector3 s1 = open ? endScale : startScale;
                exitDialogBox.localScale = Vector3.Lerp(s0, s1, eased);
            }

            yield return null;
        }

        SetAlpha(exitConfirmRT, a1);

        if (open)
        {
            SetInteractable(exitConfirmRT, true);
            ResetMenuFX(panelExitConfirm);
        }
        else
        {
            SetInteractable(exitConfirmRT, false);
            panelExitConfirm.SetActive(false);

            if (currentPanelRT != null)
            {
                SetInteractable(currentPanelRT, true);
                ResetMenuFX(currentPanelRT.gameObject);
            }
        }

        ClearUISelection();
        BlockGlobalInput(false);

        exitAnimCo = null;
    }

    // -------------------------
    // TRANSICIONES
    // -------------------------
    private void GoTo(GameObject panelToShow)
    {
        if (panelToShow == null) return;
        if (isTransitioning) return;

        if (isExitConfirmOpen) CloseExitConfirm();

        var nextRT = panelToShow.GetComponent<RectTransform>();
        if (nextRT == null) return;
        if (currentPanelRT == nextRT) return;

        ClearUISelection();
        if (currentPanelRT != null) ResetMenuFX(currentPanelRT.gameObject);

        if (transitionCo != null) StopCoroutine(transitionCo);
        transitionCo = StartCoroutine(TransitionPanels(currentPanelRT, nextRT));
    }

    private IEnumerator TransitionPanels(RectTransform from, RectTransform to)
    {
        isTransitioning = true;
        BlockGlobalInput(true);

        to.gameObject.SetActive(true);

        bool goingBackToMain = (mainMenuRT != null && to == mainMenuRT);
        float dir = goingBackToMain ? -1f : 1f;

        float fromStartX = 0f;
        float fromEndX = -dir * slideDistance;
        float toStartX = dir * slideDistance;

        SetPanelAnchoredX(to, toStartX);
        SetInteractable(to, false);

        if (from != null) SetInteractable(from, false);

        if (useSoftFade)
        {
            if (from != null) SetAlpha(from, 1f);
            SetAlpha(to, softFadeMinAlpha);
        }
        else
        {
            if (from != null) SetAlpha(from, 1f);
            SetAlpha(to, 1f);
        }

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;

            float n = Mathf.Clamp01(t / transitionDuration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            if (from != null) SetPanelAnchoredX(from, Mathf.Lerp(fromStartX, fromEndX, eased));
            SetPanelAnchoredX(to, Mathf.Lerp(toStartX, 0f, eased));

            if (useSoftFade)
            {
                if (from != null) SetAlpha(from, Mathf.Lerp(1f, softFadeMinAlpha, eased));
                SetAlpha(to, Mathf.Lerp(softFadeMinAlpha, 1f, eased));
            }

            yield return null;
        }

        if (from != null)
        {
            SetPanelAnchoredX(from, fromEndX);
            SetAlpha(from, 1f);
            from.gameObject.SetActive(false);
        }

        SetPanelAnchoredX(to, 0f);
        SetAlpha(to, 1f);
        SetInteractable(to, true);

        currentPanelRT = to;

        ClearUISelection();
        ResetMenuFX(to.gameObject);

        if (goingBackToMain)
            PlayIntroOnPanel(panelMainMenu);

        yield return null;

        BlockGlobalInput(false);

        isTransitioning = false;
        transitionCo = null;

        if (pendingBack)
        {
            pendingBack = false;
            HandleBackButton();
        }
    }

    // -------------------------
    // HELPERS
    // -------------------------
    private void HideAllPanels()
    {
        SafeDisable(panelMainMenu);
        SafeDisable(panelHistoria);
        SafeDisable(panelLogros);
        SafeDisable(panelAjustes);
        SafeDisable(panelExitConfirm);
    }

    private void SafeDisable(GameObject go)
    {
        if (go == null) return;

        go.SetActive(false);

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) SetPanelAnchoredX(rt, 0f);

        var cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
    }

    private void ClearUISelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void BlockGlobalInput(bool block)
    {
        if (canvasRootGroup == null) return;
        canvasRootGroup.blocksRaycasts = !block;
        canvasRootGroup.interactable = !block;
    }

    private void SetPanelAnchoredX(RectTransform rt, float x)
    {
        if (rt == null) return;
        Vector2 p = rt.anchoredPosition;
        p.x = x;
        rt.anchoredPosition = p;
    }

    private void SetAlpha(RectTransform rt, float a)
    {
        if (rt == null) return;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = a;
    }

    private void SetInteractable(RectTransform rt, bool value)
    {
        if (rt == null) return;
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = value;
            cg.blocksRaycasts = value;
        }
    }

    private void ResetMenuFX(GameObject root)
    {
        if (root == null) return;
        var fx = root.GetComponentsInChildren<MenuButtonFX>(true);
        for (int i = 0; i < fx.Length; i++)
            fx[i].ForceNormal();
    }

    private void PlayIntroOnPanel(GameObject root)
    {
        if (root == null) return;
        var fx = root.GetComponentsInChildren<MenuButtonFX>(true);
        for (int i = 0; i < fx.Length; i++)
            fx[i].PlayIntro();
    }
}
