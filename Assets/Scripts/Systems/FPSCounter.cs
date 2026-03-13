using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public TMP_Text fpsText;

    float timer;
    int frameCount;

    void Start()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        gameObject.SetActive(false);
#endif
    }

    void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= 0.5f)
        {
            float fps = frameCount / timer;

            fpsText.text = $"{Mathf.RoundToInt(fps)} FPS";

            frameCount = 0;
            timer = 0f;
        }
    }
}