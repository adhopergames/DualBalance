using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "Game";

    [Header("Panels")]
    public GameObject panelMainMenu;
    public GameObject panelHistoria;
    public GameObject panelLogros;
    public GameObject panelAjustes;

    private GameObject currentPanel;

    private void Start()
    {
        // Al iniciar, mostramos el menú principal
        ShowPanel(panelMainMenu);
    }

    // -------------------------
    // BOTONES
    // -------------------------

    public void OnPlayPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnHistoriaPressed()
    {
        ShowPanel(panelHistoria);
    }

    public void OnAchievementsPressed()
    {
        ShowPanel(panelLogros);
    }

    public void OnSettingsPressed()
    {
        ShowPanel(panelAjustes);
    }

    public void OnBackPressed()
    {
        ShowPanel(panelMainMenu);
    }

    public void OnQuitPressed()
    {
        Application.Quit();
    }

    // -------------------------
    // CORE
    // -------------------------

    private void ShowPanel(GameObject panelToShow)
    {
        if (panelToShow == null) return;

        // Apaga el actual
        if (currentPanel != null)
            currentPanel.SetActive(false);

        // Enciende el nuevo
        panelToShow.SetActive(true);
        currentPanel = panelToShow;
    }
}
