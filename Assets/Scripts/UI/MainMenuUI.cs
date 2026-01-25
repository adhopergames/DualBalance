using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes")]
    [Tooltip("Nombre exacto de la escena del juego en Build Settings.")]
    public string gameSceneName = "Game";

    [Tooltip("Nombre exacto de la escena del menú en Build Settings.")]
    public string menuSceneName = "Menu";

    /// Play: carga la escena del juego desde cero.

    public void OnPlayPressed()
    {
        Time.timeScale = 1f; // seguridad
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnAchievementsPressed()
    {
        Debug.Log("Achievements: pendiente de implementar UI");
    }

    /// Cerrar app (en editor no cierra).

    public void OnQuitPressed()
    {
        Application.Quit();
    }
}
