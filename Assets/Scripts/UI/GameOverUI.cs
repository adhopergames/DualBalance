using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;                 // CanvasInterfaz/GameOver
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestText;
    public TextMeshProUGUI newRecordText;

    [Header("Continue (Rewarded)")]
    public Button continueButton;
    public TextMeshProUGUI continueButtonLabel;

    [Header("Menu")]
    public string menuSceneName = "MainMenu";

    [Header("Other UI to Hide")]
    [Tooltip("Botón de pausa (icono). Se ocultará mientras GameOver esté visible.")]
    [SerializeField] private GameObject pauseButton;

    [Tooltip("Opcional: HUD root (score/energía). Si lo asignas, se ocultará durante GameOver.")]
    [SerializeField] private GameObject hudRoot;

    [Header("Animation (code)")]
    [SerializeField] private float animInDuration = 0.18f;
    [SerializeField] private float animOutDuration = 0.12f;
    [Range(0.5f, 1f)]
    [SerializeField] private float popStartScale = 0.92f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isPendingVisible;

    // Anim internals
    private CanvasGroup panelGroup;
    private RectTransform panelRT;
    private Coroutine animCo;
    private bool isVisible;

    private void Awake()
    {
        if (panel != null)
        {
            panelRT = panel.GetComponent<RectTransform>();

            panelGroup = panel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                panelGroup = panel.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        if (panelRT != null)
            panelRT.localScale = Vector3.one * popStartScale;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOverFinal;
            GameManager.Instance.OnGameOverPending += HandleGameOverPending;
            GameManager.Instance.OnRevive += HandleRevive;
        }

        if (continueButton != null) continueButton.gameObject.SetActive(false);

        isPendingVisible = false;
        isVisible = false;
    }

    private void Update()
    {
        // Mientras estemos en pending, actualizamos el texto del botón según si el ad está listo.
        if (!isPendingVisible) return;
        if (continueButtonLabel == null) return;

        bool ready = (AdManager.Instance != null && AdManager.Instance.IsRewardedReady);
        continueButtonLabel.text = ready ? "Continuar (Ad)" : "Continuar (Cargando...)";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOverFinal;
            GameManager.Instance.OnGameOverPending -= HandleGameOverPending;
            GameManager.Instance.OnRevive -= HandleRevive;
        }
    }

    // -------------------------
    // EVENTS FROM GAMEMANAGER
    // -------------------------

    private void HandleGameOverPending(int scoreNow, int bestScore, bool canContinue, bool isNewRecordNow)
    {
        isPendingVisible = true;

        if (scoreText != null) scoreText.text = scoreNow.ToString();
        if (bestText != null) bestText.text = bestScore.ToString();
        if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecordNow);

        if (continueButton != null)
        {
            bool show = canContinue && AdManager.Instance != null;
            continueButton.gameObject.SetActive(show);

            // Pedimos precarga del rewarded cuando entramos a pending
            if (show) AdManager.Instance.LoadRewarded();
        }

        ShowGameOver(true);
    }

    private void HandleGameOverFinal(int scoreFinal, int bestScore, bool isNewRecord)
    {
        isPendingVisible = false;

        if (scoreText != null) scoreText.text = scoreFinal.ToString();
        if (bestText != null) bestText.text = bestScore.ToString();
        if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecord);

        if (continueButton != null) continueButton.gameObject.SetActive(false);

        ShowGameOver(true);
    }

    private void HandleRevive()
    {
        isPendingVisible = false;
        ShowGameOver(false);
    }

    // -------------------------
    // SHOW / HIDE (ANIMATED)
    // -------------------------

    private void ShowGameOver(bool show)
    {
        if (isVisible == show) return;
        isVisible = show;

        // Ocultar UI de juego
        if (pauseButton != null) pauseButton.SetActive(!show);
        if (hudRoot != null) hudRoot.SetActive(!show); // opcional

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimatePanel(show));
    }

    private IEnumerator AnimatePanel(bool open)
    {
        if (panel == null || panelGroup == null)
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
            panel.SetActive(true);

            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;

            if (panelRT != null) panelRT.localScale = s0;
        }
        else
        {
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // ✅ funciona aunque haya Time.timeScale 0 en pending
            float n = Mathf.Clamp01(t / duration);
            float eased = easeCurve != null ? easeCurve.Evaluate(n) : n;

            panelGroup.alpha = Mathf.Lerp(a0, a1, eased);

            if (panelRT != null)
                panelRT.localScale = Vector3.Lerp(s0, s1, eased);

            yield return null;
        }

        panelGroup.alpha = a1;
        if (panelRT != null) panelRT.localScale = s1;

        if (open)
        {
            panelGroup.interactable = true;
            panelGroup.blocksRaycasts = true;
        }
        else
        {
            panel.SetActive(false);

            // Al cerrar (revive), regresamos UI de juego
            if (pauseButton != null) pauseButton.SetActive(true);
            if (hudRoot != null) hudRoot.SetActive(true);
        }

        animCo = null;
    }

    // -------------------------
    // BUTTONS
    // -------------------------

    public void OnRetryPressed()
    {
        GameManager.Instance.Restart();
    }

    public void OnMenuPressed()
    {
        Time.timeScale = 1f; // por si estaba en 0 en pending
        SceneManager.LoadScene(menuSceneName);
    }

    public void OnContinuePressed()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.ShowRewarded();
    }
}
