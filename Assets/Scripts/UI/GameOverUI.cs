using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    // ============================================================
    // UI
    // ============================================================

    [Header("UI")]

    public GameObject panel;

    public TextMeshProUGUI scoreText;

    public TextMeshProUGUI bestText;

    public TextMeshProUGUI newRecordText;


    // ============================================================
    // PLATFORM / VERSION
    // ============================================================

    [Header("Version")]

    [Tooltip(
        "ACTIVAR solamente en el GameOverUI que pertenece " +
        "a CanvasInterfazPC. " +
        "Déjalo desactivado en CanvasInterfazMovil."
    )]
    [SerializeField]
    private bool isPCVersion = false;


    // ============================================================
    // CONTINUE
    // ============================================================

    [Header("Continue")]

    public Button continueButton;

    public TextMeshProUGUI continueButtonLabel;

    [Header("Continue Text")]

    [Tooltip(
        "Texto mostrado en PC, donde el continue es gratuito."
    )]
    [SerializeField]
    private string pcContinueText =
        "Continuar";


    // ============================================================
    // MENU
    // ============================================================

    [Header("Menu")]

    public string menuSceneName =
        "MainMenu";


    // ============================================================
    // OTHER UI
    // ============================================================

    [Header("Other UI to Hide")]

    [Tooltip(
        "Botón de pausa. Se ocultará mientras GameOver esté visible."
    )]
    [SerializeField]
    private GameObject pauseButton;

    [Tooltip(
        "Opcional: HUD root. Se ocultará durante GameOver."
    )]
    [SerializeField]
    private GameObject hudRoot;


    // ============================================================
    // ANIMATION
    // ============================================================

    [Header("Animation")]

    [SerializeField]
    private float animInDuration = 0.18f;

    [SerializeField]
    private float animOutDuration = 0.12f;

    [Range(0.5f, 1f)]
    [SerializeField]
    private float popStartScale = 0.92f;

    [SerializeField]
    private AnimationCurve easeCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );


    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private bool isPendingVisible;

    private CanvasGroup panelGroup;

    private RectTransform panelRT;

    private Coroutine animCo;

    private bool isVisible;


    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (panel != null)
        {
            panelRT =
                panel.GetComponent<RectTransform>();

            panelGroup =
                panel.GetComponent<CanvasGroup>();

            /*
             * Si el panel no tiene CanvasGroup,
             * lo añadimos automáticamente.
             */
            if (panelGroup == null)
            {
                panelGroup =
                    panel.AddComponent<CanvasGroup>();
            }
        }
    }


    private void Start()
    {
        // --------------------------------------------------------
        // Estado inicial del panel
        // --------------------------------------------------------

        if (panel != null)
        {
            panel.SetActive(false);
        }

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;

            panelGroup.interactable = false;

            panelGroup.blocksRaycasts = false;
        }

        if (panelRT != null)
        {
            panelRT.localScale =
                Vector3.one *
                popStartScale;
        }


        // --------------------------------------------------------
        // Eventos del GameManager
        // --------------------------------------------------------

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver +=
                HandleGameOverFinal;

            GameManager.Instance.OnGameOverPending +=
                HandleGameOverPending;

            GameManager.Instance.OnRevive +=
                HandleRevive;
        }


        // --------------------------------------------------------
        // Continue oculto inicialmente
        // --------------------------------------------------------

        if (continueButton != null)
        {
            continueButton
                .gameObject
                .SetActive(false);
        }

        isPendingVisible = false;

        isVisible = false;
    }


    private void Update()
    {
        /*
         * Solo actualizamos información del botón mientras
         * estamos en GameOverPending.
         */
        if (!isPendingVisible)
            return;

        if (continueButtonLabel == null)
            return;


        // ========================================================
        // PC
        // ========================================================

        if (isPCVersion)
        {
            /*
             * PC no espera ningún anuncio.
             * El continue siempre está disponible.
             */
            continueButtonLabel.text =
                pcContinueText;

            if (continueButton != null)
            {
                continueButton.interactable =
                    true;
            }

            return;
        }


        // ========================================================
        // MÓVIL
        // ========================================================

        /*
         * IMPORTANTE:
         *
         * Esta es la misma lógica que tenía el script original.
         * No cambiamos cómo funciona el Rewarded.
         */
        bool ready =
            AdManager.Instance != null &&
            AdManager.Instance.IsRewardedReady;

        continueButtonLabel.text =
            ready
                ? "Continuar (Ad)"
                : "Continuar (Cargando...)";
    }


    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -=
                HandleGameOverFinal;

            GameManager.Instance.OnGameOverPending -=
                HandleGameOverPending;

            GameManager.Instance.OnRevive -=
                HandleRevive;
        }
    }


    // ============================================================
    // GAMEMANAGER EVENTS
    // ============================================================

    /// <summary>
    /// Primera muerte de la run.
    ///
    /// Móvil:
    /// prepara el Rewarded exactamente como antes.
    ///
    /// PC:
    /// muestra el continue gratuito.
    /// </summary>
    private void HandleGameOverPending(
        int scoreNow,
        int bestScore,
        bool canContinue,
        bool isNewRecordNow
    )
    {
        isPendingVisible = true;


        // --------------------------------------------------------
        // Score
        // --------------------------------------------------------

        if (scoreText != null)
        {
            scoreText.text =
                scoreNow.ToString();
        }

        if (bestText != null)
        {
            bestText.text =
                bestScore.ToString();
        }

        if (newRecordText != null)
        {
            newRecordText
                .gameObject
                .SetActive(
                    isNewRecordNow
                );
        }


        // ========================================================
        // CONTINUE PC
        // ========================================================

        if (isPCVersion)
        {
            if (continueButton != null)
            {
                continueButton
                    .gameObject
                    .SetActive(
                        canContinue
                    );

                continueButton.interactable =
                    canContinue;
            }

            if (continueButtonLabel != null)
            {
                continueButtonLabel.text =
                    pcContinueText;
            }
        }

        // ========================================================
        // CONTINUE MÓVIL
        // ========================================================

        else
        {
            /*
             * IMPORTANTE:
             *
             * Este bloque mantiene prácticamente literalmente
             * la implementación móvil que ya tenías funcionando.
             */
            if (continueButton != null)
            {
                bool show =
                    canContinue &&
                    AdManager.Instance != null;

                continueButton
                    .gameObject
                    .SetActive(show);

                if (show)
                {
                    AdManager.Instance
                        .LoadRewarded();
                }
            }
        }


        ShowGameOver(true);
    }


    /// <summary>
    /// Game Over definitivo.
    ///
    /// Aquí ya se utilizó la única segunda oportunidad,
    /// por lo que ocultamos Continue en ambas versiones.
    /// </summary>
    private void HandleGameOverFinal(
        int scoreFinal,
        int bestScore,
        bool isNewRecord
    )
    {
        isPendingVisible = false;

        if (scoreText != null)
        {
            scoreText.text =
                scoreFinal.ToString();
        }

        if (bestText != null)
        {
            bestText.text =
                bestScore.ToString();
        }

        if (newRecordText != null)
        {
            newRecordText
                .gameObject
                .SetActive(
                    isNewRecord
                );
        }

        if (continueButton != null)
        {
            continueButton
                .gameObject
                .SetActive(false);
        }

        ShowGameOver(true);
    }


    /// <summary>
    /// Cierra el Game Over después de revivir.
    /// </summary>
    private void HandleRevive()
    {
        isPendingVisible = false;

        ShowGameOver(false);
    }


    // ============================================================
    // SHOW / HIDE
    // ============================================================

    private void ShowGameOver(
        bool show
    )
    {
        if (isVisible == show)
            return;

        isVisible = show;


        // --------------------------------------------------------
        // Ocultar/restaurar HUD
        // --------------------------------------------------------

        if (pauseButton != null)
        {
            pauseButton.SetActive(
                !show
            );
        }

        if (hudRoot != null)
        {
            hudRoot.SetActive(
                !show
            );
        }


        // --------------------------------------------------------
        // Animación
        // --------------------------------------------------------

        if (animCo != null)
        {
            StopCoroutine(
                animCo
            );
        }

        animCo =
            StartCoroutine(
                AnimatePanel(show)
            );
    }


    /// <summary>
    /// Fade + pop del panel Game Over.
    ///
    /// Utiliza unscaledDeltaTime porque GameOverPending
    /// tiene Time.timeScale = 0.
    /// </summary>
    private IEnumerator AnimatePanel(
        bool open
    )
    {
        if (
            panel == null ||
            panelGroup == null
        )
        {
            animCo = null;

            yield break;
        }


        float duration =
            open
                ? animInDuration
                : animOutDuration;

        float a0 =
            open
                ? 0f
                : 1f;

        float a1 =
            open
                ? 1f
                : 0f;


        Vector3 s0 =
            Vector3.one *
            (
                open
                    ? popStartScale
                    : 1f
            );

        Vector3 s1 =
            Vector3.one *
            (
                open
                    ? 1f
                    : popStartScale
            );


        // --------------------------------------------------------
        // Preparar apertura/cierre
        // --------------------------------------------------------

        if (open)
        {
            panel.SetActive(true);

            panelGroup.alpha = 0f;

            panelGroup.interactable =
                false;

            panelGroup.blocksRaycasts =
                false;

            if (panelRT != null)
            {
                panelRT.localScale =
                    s0;
            }
        }
        else
        {
            panelGroup.interactable =
                false;

            panelGroup.blocksRaycasts =
                false;
        }


        float t = 0f;


        // --------------------------------------------------------
        // Animar
        // --------------------------------------------------------

        while (t < duration)
        {
            t +=
                Time.unscaledDeltaTime;

            float n =
                Mathf.Clamp01(
                    t / duration
                );

            float eased =
                easeCurve != null
                    ? easeCurve.Evaluate(n)
                    : n;


            panelGroup.alpha =
                Mathf.Lerp(
                    a0,
                    a1,
                    eased
                );


            if (panelRT != null)
            {
                panelRT.localScale =
                    Vector3.Lerp(
                        s0,
                        s1,
                        eased
                    );
            }


            yield return null;
        }


        // --------------------------------------------------------
        // Estado final
        // --------------------------------------------------------

        panelGroup.alpha =
            a1;

        if (panelRT != null)
        {
            panelRT.localScale =
                s1;
        }


        if (open)
        {
            panelGroup.interactable =
                true;

            panelGroup.blocksRaycasts =
                true;
        }
        else
        {
            panel.SetActive(false);

            if (pauseButton != null)
            {
                pauseButton.SetActive(
                    true
                );
            }

            if (hudRoot != null)
            {
                hudRoot.SetActive(
                    true
                );
            }
        }


        animCo = null;
    }


    // ============================================================
    // BUTTON - RETRY
    // ============================================================

    /// <summary>
    /// Reinicia la partida.
    ///
    /// PC:
    /// reinicia directamente, sin AdMob.
    ///
    /// Móvil:
    /// utiliza el Restart original, que conserva
    /// el sistema de Interstitial cada 6–7 runs.
    /// </summary>
    public void OnRetryPressed()
    {
        AudioManager.Instance
            ?.StopAllSFX();


        // --------------------------------------------------------
        // PC
        // --------------------------------------------------------

        if (isPCVersion)
        {
            GameManager.Instance
                ?.RestartWithoutAds();

            return;
        }


        // --------------------------------------------------------
        // MÓVIL - CÓDIGO ORIGINAL
        // --------------------------------------------------------

        GameManager.Instance
            ?.Restart();
    }


    // ============================================================
    // BUTTON - MENU
    // ============================================================

    public void OnMenuPressed()
    {
        AudioManager.Instance
            ?.StopAllSFX();

        /*
         * Quitar cualquier efecto temporal de pausa
         * o Game Over antes de volver al menú.
         */
        AudioManager.Instance
            ?.ResetMusicStateImmediate();

        AudioManager.Instance
            ?.ResetGameOverMusicImmediate();

        Time.timeScale = 1f;


        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance
                .LoadScene(
                    menuSceneName
                );
        }
        else
        {
            Debug.LogWarning(
                "GameOverUI: no existe LevelLoader.Instance. " +
                "Cargando MainMenu directamente."
            );

            SceneManager.LoadScene(
                menuSceneName
            );
        }
    }


    // ============================================================
    // BUTTON - CONTINUE
    // ============================================================

    /// <summary>
    /// MISMO OnClick para ambos Canvas.
    ///
    /// CanvasInterfazPC:
    /// → Continue gratuito.
    ///
    /// CanvasInterfazMovil:
    /// → Rewarded original de AdMob.
    /// </summary>
    public void OnContinuePressed()
    {
        AudioManager.Instance
            ?.StopAllSFX();


        // ========================================================
        // PC
        // ========================================================

        if (isPCVersion)
        {
            GameManager.Instance
                ?.ContinueFreeOnPC();

            return;
        }


        // ========================================================
        // MÓVIL - CÓDIGO ORIGINAL
        // ========================================================

        /*
         * Desde aquí hacia abajo dejamos exactamente
         * la ruta que ya tenías funcionando.
         */
        if (AdManager.Instance == null)
            return;

        AdManager.Instance
            .ShowRewarded();
    }
}