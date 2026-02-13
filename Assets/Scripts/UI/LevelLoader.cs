using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator transition;
    [SerializeField] private string triggerName = "Start";

    [Header("Timing")]
    [SerializeField] private float transitionTime = 1f;

    private bool isLoading;

    /// Carga por nombre (ej: "Game")
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    /// Carga por build index
    public void LoadScene(int buildIndex)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    /// Recarga escena actual
    public void ReloadCurrentScene()
    {
        if (isLoading) return;
        int idx = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(idx));
    }

    /// Carga la siguiente escena en Build Settings
    public void LoadNextScene()
    {
        if (isLoading) return;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        StartCoroutine(LoadSceneRoutine(next));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        // Asegura que el juego no se quede pausado (por tu GameManager/ads)
        Time.timeScale = 1f;

        if (transition != null)
        {
            transition.ResetTrigger(triggerName);
            transition.SetTrigger(triggerName);
        }

        // Realtime = no se rompe si Time.timeScale estaba en 0
        yield return new WaitForSecondsRealtime(transitionTime);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        isLoading = true;

        Time.timeScale = 1f;

        if (transition != null)
        {
            transition.ResetTrigger(triggerName);
            transition.SetTrigger(triggerName);
        }

        yield return new WaitForSecondsRealtime(transitionTime);

        SceneManager.LoadScene(buildIndex);
    }
}
