using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI bestText;
    public TextMeshProUGUI newRecordText;

    [Header("Continue (Rewarded)")]
    public Button continueButton;
    public TextMeshProUGUI continueButtonLabel;

    [Header("Menu")]
    public string menuSceneName = "MainMenu";

    private bool isPendingVisible;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += HandleGameOverFinal;
            GameManager.Instance.OnGameOverPending += HandleGameOverPending;
            GameManager.Instance.OnRevive += HandleRevive;
        }

        if (continueButton != null) continueButton.gameObject.SetActive(false);
        isPendingVisible = false;
    }

    private void Update()
    {
        // ✅ Mientras estemos en pending, actualizamos el texto del botón según si el ad está listo.
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

    private void HandleGameOverPending(int scoreNow, int bestScore, bool canContinue, bool isNewRecordNow)
    {
        isPendingVisible = true;

        if (panel != null) panel.SetActive(true);

        if (scoreText != null) scoreText.text = $"Score: {scoreNow}";
        if (bestText != null) bestText.text = $"Best: {bestScore}";

        // ✅ NEW RECORD en pending basado en best ANTES de guardar (flag correcto)
        if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecordNow);

        if (continueButton != null)
        {
            bool show = canContinue && AdManager.Instance != null;
            continueButton.gameObject.SetActive(show);

            // ✅ Pedimos precarga del rewarded cuando entramos a pending
            if (show) AdManager.Instance.LoadRewarded();
        }
    }

    private void HandleGameOverFinal(int scoreFinal, int bestScore, bool isNewRecord)
    {
        isPendingVisible = false;

        if (panel != null) panel.SetActive(true);

        if (scoreText != null) scoreText.text = $"Score: {scoreFinal}";
        if (bestText != null) bestText.text = $"Best: {bestScore}";

        if (newRecordText != null)
            newRecordText.gameObject.SetActive(isNewRecord);

        if (continueButton != null) continueButton.gameObject.SetActive(false);
    }

    private void HandleRevive()
    {
        isPendingVisible = false;

        if (panel != null) panel.SetActive(false);
    }

    public void OnRetryPressed()
    {
        // ✅ Si estabas en pending y NO viste anuncio, el best YA se guardó en pending
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

        // ✅ Si está listo, se muestra. Si no, el AdManager intentará cargarlo.
        AdManager.Instance.ShowRewarded();
    }
}
