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

    private void Start()
    {
        if (transition == null)
        {
            return;
        }

        /*
         * Al entrar a la escena Game, el tutorial puede colocar
         * Time.timeScale en 0 inmediatamente.
         *
         * UnscaledTime permite que la animación inicial del loader
         * termine aunque el gameplay esté pausado.
         */
        StartCoroutine(AllowInitialTransitionToFinish());
    }

    private IEnumerator AllowInitialTransitionToFinish()
    {
        transition.updateMode = AnimatorUpdateMode.UnscaledTime;
        transition.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        yield return new WaitForSecondsRealtime(transitionTime);

        transition.updateMode = AnimatorUpdateMode.Normal;
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    public void LoadScene(int buildIndex)
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadSceneRoutine(buildIndex));
    }

    public void ReloadCurrentScene()
    {
        if (isLoading)
        {
            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(LoadSceneRoutine(currentIndex));
    }

    public void LoadNextScene()
    {
        if (isLoading)
        {
            return;
        }

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        StartCoroutine(LoadSceneRoutine(nextIndex));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        Time.timeScale = 1f;

        if (transition != null)
        {
            transition.updateMode = AnimatorUpdateMode.UnscaledTime;
            transition.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            transition.ResetTrigger(triggerName);
            transition.SetTrigger(triggerName);
        }

        yield return new WaitForSecondsRealtime(transitionTime);

        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator LoadSceneRoutine(int buildIndex)
    {
        isLoading = true;

        Time.timeScale = 1f;

        if (transition != null)
        {
            transition.updateMode = AnimatorUpdateMode.UnscaledTime;
            transition.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            transition.ResetTrigger(triggerName);
            transition.SetTrigger(triggerName);
        }

        yield return new WaitForSecondsRealtime(transitionTime);

        SceneManager.LoadScene(buildIndex);
    }
}