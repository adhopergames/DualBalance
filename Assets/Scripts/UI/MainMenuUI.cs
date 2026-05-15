using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [System.Serializable]
    public class MenuCanvasSet
    {
        [Header("Canvas Root")]
        public GameObject canvasRoot;

        [Header("Panels")]
        public GameObject panelMainMenu;
        public GameObject panelHistoria;
        public GameObject panelLogros;
        public GameObject panelAjustes;

        [Header("Exit Confirm")]
        public GameObject panelExitConfirm;
        public RectTransform exitDialogBox;

        [Header("Logo")]
        public GameObject logo;

        [Header("Root Input Block")]
        public CanvasGroup canvasRootGroup;

        [Header("Settings Sliders")]
        public Slider musicSlider;
        public Slider sfxSlider;
    }

    [Header("Scenes")]
    public string gameSceneName = "Game";

    [Header("Canvas Sets")]
    [SerializeField] private MenuCanvasSet mobileCanvas;
    [SerializeField] private MenuCanvasSet pcCanvas;

    [Header("Canvas Selection")]
    [SerializeField] private bool useAutoCanvas = true;
    [SerializeField] private bool forcePCCanvas = false;

    [Header("UI Actions (NEW Input System)")]
    public InputActionReference backAction;

    [Header("Level Loader")]
    [SerializeField] private LevelLoader levelLoader;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private float slideDistance = 900f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Soft Fade")]
    [SerializeField] private bool useSoftFade = true;
    [Range(0.5f, 1f)]
    [SerializeField] private float softFadeMinAlpha = 0.92f;

    [Header("Logo Fade")]
    [SerializeField] private float logoFadeDuration = 0.18f;

    [Header("Exit Confirm Animation")]
    [SerializeField] private float exitAnimDuration = 0.18f;
    [Range(0.5f, 1f)]
    [SerializeField] private float exitPopStartScale = 0.94f;

    private MenuCanvasSet activeCanvas;

    private GameObject panelMainMenu;
    private GameObject panelHistoria;
    private GameObject panelLogros;
    private GameObject panelAjustes;
    private GameObject panelExitConfirm;
    private GameObject logo;

    private RectTransform exitDialogBox;
    private CanvasGroup canvasRootGroup;
    private CanvasGroup logoCanvasGroup;

    private RectTransform currentPanelRT;
    private RectTransform mainMenuRT;
    private RectTransform exitConfirmRT;

    private Coroutine transitionCo;
    private Coroutine exitAnimCo;
    private Coroutine logoFadeCo;

    private bool isTransitioning;
    private bool isExitConfirmOpen;
    private bool isPCCanvasActive;
    private bool isLoadingGame;

    private bool backQueued;
    private bool backHooked;
    private bool pendingBack;

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

    private void Start()
    {
        SelectActiveCanvas();
        ApplyActiveCanvasReferences();

        mainMenuRT = panelMainMenu != null ? panelMainMenu.GetComponent<RectTransform>() : null;
        exitConfirmRT = panelExitConfirm != null ? panelExitConfirm.GetComponent<RectTransform>() : null;

        HideAllPanels();

        if (logo != null)
            SetLogoVisibleInstant(true);

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

        SetupVolumeSliders();

        AudioManager.Instance?.PlayMenuMusic();
    }

    private void SelectActiveCanvas()
    {
        bool usePC = forcePCCanvas;

        if (useAutoCanvas)
        {
            usePC =
                Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.LinuxPlayer;
        }

        activeCanvas = usePC ? pcCanvas : mobileCanvas;
        isPCCanvasActive = usePC;

        if (mobileCanvas != null && mobileCanvas.canvasRoot != null)
            mobileCanvas.canvasRoot.SetActive(!usePC);

        if (pcCanvas != null && pcCanvas.canvasRoot != null)
            pcCanvas.canvasRoot.SetActive(usePC);
    }

    private void ApplyActiveCanvasReferences()
    {
        if (activeCanvas == null) return;

        panelMainMenu = activeCanvas.panelMainMenu;
        panelHistoria = activeCanvas.panelHistoria;
        panelLogros = activeCanvas.panelLogros;
        panelAjustes = activeCanvas.panelAjustes;
        panelExitConfirm = activeCanvas.panelExitConfirm;
        exitDialogBox = activeCanvas.exitDialogBox;
        canvasRootGroup = activeCanvas.canvasRootGroup;
        logo = activeCanvas.logo;

        if (logo != null)
        {
            logoCanvasGroup = logo.GetComponent<CanvasGroup>();

            if (logoCanvasGroup == null)
                logoCanvasGroup = logo.AddComponent<CanvasGroup>();
        }
    }

    private void SetupVolumeSliders()
    {
        if (activeCanvas == null || AudioManager.Instance == null) return;

        if (activeCanvas.musicSlider != null)
        {
            activeCanvas.musicSlider.minValue = 0f;
            activeCanvas.musicSlider.maxValue = 1f;
            activeCanvas.musicSlider.wholeNumbers = false;

            activeCanvas.musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            activeCanvas.musicSlider.onValueChanged.RemoveAllListeners();
            activeCanvas.musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }

        if (activeCanvas.sfxSlider != null)
        {
            activeCanvas.sfxSlider.minValue = 0f;
            activeCanvas.sfxSlider.maxValue = 1f;
            activeCanvas.sfxSlider.wholeNumbers = false;

            activeCanvas.sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetSfxVolume());
            activeCanvas.sfxSlider.onValueChanged.RemoveAllListeners();
            activeCanvas.sfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
        }
    }

    private void Update()
    {
        if (!backQueued) return;
        backQueued = false;

        if (isLoadingGame) return;

        if (isTransitioning)
            pendingBack = true;
        else
            HandleBackButton();
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
        if (isExitConfirmOpen)
        {
            AudioManager.Instance?.PlayUIBack();
            CloseExitConfirm();
            return;
        }

        if (mainMenuRT != null && currentPanelRT != mainMenuRT)
        {
            AudioManager.Instance?.PlayUIBack();
            GoTo(panelMainMenu);
            return;
        }

        AudioManager.Instance?.PlayUIBack();
        OpenExitConfirm();
    }

    public void OnPlayPressed()
    {
        if (isLoadingGame) return;

        isLoadingGame = true;
        Time.timeScale = 1f;
        BlockGlobalInput(true);

        AudioManager.Instance?.PlayUIButton();
        AudioManager.Instance?.FadeOutCurrentMusic();

        if (levelLoader != null)
            levelLoader.LoadScene(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnHistoriaPressed()
    {
        AudioManager.Instance?.PlayUIButton();
        GoTo(panelHistoria);
    }

    public void OnAchievementsPressed()
    {
        AudioManager.Instance?.PlayUIButton();
        GoTo(panelLogros);
    }

    public void OnSettingsPressed()
    {
        AudioManager.Instance?.PlayUIButton();
        GoTo(panelAjustes);
    }

    public void OnBackPressed()
    {
        AudioManager.Instance?.PlayUIBack();
        GoTo(panelMainMenu);
    }

    public void OnQuitPressed()
    {
        AudioManager.Instance?.PlayUIButton();
        OpenExitConfirm();
    }

    public void OnExitYesPressed()
    {
        AudioManager.Instance?.PlayUIButton();
        Application.Quit();
    }

    public void OnExitNoPressed()
    {
        AudioManager.Instance?.PlayUIBack();
        CloseExitConfirm();
    }

    private void GoTo(GameObject panelToShow)
    {
        if (panelToShow == null) return;
        if (isTransitioning) return;
        if (isLoadingGame) return;

        if (isExitConfirmOpen)
            CloseExitConfirm();

        RectTransform nextRT = panelToShow.GetComponent<RectTransform>();
        if (nextRT == null) return;
        if (currentPanelRT == nextRT) return;

        UpdateLogoVisibility(panelToShow);
        ClearUISelection();

        if (currentPanelRT != null)
            ResetMenuFX(currentPanelRT.gameObject);

        if (transitionCo != null)
            StopCoroutine(transitionCo);

        transitionCo = StartCoroutine(TransitionPanels(currentPanelRT, nextRT));
    }

    private void UpdateLogoVisibility(GameObject targetPanel)
    {
        if (!isPCCanvasActive || logo == null) return;

        bool shouldShowLogo = targetPanel == panelMainMenu;

        bool shouldHideLogo =
            targetPanel == panelHistoria ||
            targetPanel == panelLogros ||
            targetPanel == panelAjustes;

        if (shouldShowLogo)
            FadeLogo(true);
        else if (shouldHideLogo)
            FadeLogo(false);
    }

    private void FadeLogo(bool show)
    {
        if (logo == null || logoCanvasGroup == null) return;

        if (logoFadeCo != null)
            StopCoroutine(logoFadeCo);

        logoFadeCo = StartCoroutine(FadeLogoRoutine(show));
    }

    private IEnumerator FadeLogoRoutine(bool show)
    {
        if (show)
            logo.SetActive(true);

        float startAlpha = logoCanvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;

        float t = 0f;

        while (t < logoFadeDuration)
        {
            t += Time.unscaledDeltaTime;

            float n = Mathf.Clamp01(t / logoFadeDuration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            logoCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);

            yield return null;
        }

        logoCanvasGroup.alpha = endAlpha;

        if (!show)
            logo.SetActive(false);

        logoFadeCo = null;
    }

    private void SetLogoVisibleInstant(bool show)
    {
        if (logo == null) return;

        logo.SetActive(show);

        if (logoCanvasGroup != null)
            logoCanvasGroup.alpha = show ? 1f : 0f;
    }

    private void OpenExitConfirm()
    {
        if (panelExitConfirm == null || exitConfirmRT == null) return;
        if (isExitConfirmOpen) return;
        if (isLoadingGame) return;

        isExitConfirmOpen = true;

        panelExitConfirm.SetActive(true);
        SetInteractable(exitConfirmRT, false);
        SetAlpha(exitConfirmRT, 0f);

        if (currentPanelRT != null)
            SetInteractable(currentPanelRT, false);

        ClearUISelection();

        if (currentPanelRT != null)
            ResetMenuFX(currentPanelRT.gameObject);

        if (exitAnimCo != null)
            StopCoroutine(exitAnimCo);

        exitAnimCo = StartCoroutine(AnimateExitConfirm(open: true));
    }

    private void CloseExitConfirm()
    {
        if (panelExitConfirm == null || exitConfirmRT == null) return;
        if (!isExitConfirmOpen) return;

        isExitConfirmOpen = false;
        ClearUISelection();

        if (exitAnimCo != null)
            StopCoroutine(exitAnimCo);

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

    private IEnumerator TransitionPanels(RectTransform from, RectTransform to)
    {
        isTransitioning = true;
        BlockGlobalInput(true);

        to.gameObject.SetActive(true);

        bool goingBackToMain = mainMenuRT != null && to == mainMenuRT;
        float dir = goingBackToMain ? -1f : 1f;

        float fromStartX = 0f;
        float fromEndX = -dir * slideDistance;
        float toStartX = dir * slideDistance;

        SetPanelAnchoredX(to, toStartX);
        SetInteractable(to, false);

        if (from != null)
            SetInteractable(from, false);

        if (useSoftFade)
        {
            if (from != null)
                SetAlpha(from, 1f);

            SetAlpha(to, softFadeMinAlpha);
        }
        else
        {
            if (from != null)
                SetAlpha(from, 1f);

            SetAlpha(to, 1f);
        }

        float t = 0f;

        while (t < transitionDuration)
        {
            t += Time.unscaledDeltaTime;

            float n = Mathf.Clamp01(t / transitionDuration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            if (from != null)
                SetPanelAnchoredX(from, Mathf.Lerp(fromStartX, fromEndX, eased));

            SetPanelAnchoredX(to, Mathf.Lerp(toStartX, 0f, eased));

            if (useSoftFade)
            {
                if (from != null)
                    SetAlpha(from, Mathf.Lerp(1f, softFadeMinAlpha, eased));

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

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
            SetPanelAnchoredX(rt, 0f);

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
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

        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg != null)
            cg.alpha = a;
    }

    private void SetInteractable(RectTransform rt, bool value)
    {
        if (rt == null) return;

        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = value;
            cg.blocksRaycasts = value;
        }
    }

    private void ResetMenuFX(GameObject root)
    {
        if (root == null) return;

        MenuButtonFX[] fx = root.GetComponentsInChildren<MenuButtonFX>(true);

        for (int i = 0; i < fx.Length; i++)
            fx[i].ForceNormal();
    }

    private void PlayIntroOnPanel(GameObject root)
    {
        if (root == null) return;

        MenuButtonFX[] fx = root.GetComponentsInChildren<MenuButtonFX>(true);

        for (int i = 0; i < fx.Length; i++)
            fx[i].PlayIntro();
    }
}