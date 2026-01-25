using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// AchievementPopupUI:
/// - Espera a que exista AchievementManager.Instance (por orden de ejecución)
/// - Se suscribe a OnAchievementUnlocked
/// - Muestra un popup por X segundos (unscaled)
/// - Cola por si caen varios logros seguidos
/// </summary>
public class AchievementPopupUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject root;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    [Header("Timing")]
    public float showSeconds = 2.0f;

    private readonly Queue<(string id, string title, string desc)> queue = new();
    private bool isShowing;

    // Guardamos referencia al manager al que nos suscribimos (para desuscribir bien)
    private AchievementManager subscribedManager;

    private void Start()
    {
        if (root != null) root.SetActive(false);
        isShowing = false;

        // ✅ Importante: esperar al AchievementManager (por orden de carga)
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Espera hasta que exista el manager
        while (AchievementManager.Instance == null)
            yield return null;

        subscribedManager = AchievementManager.Instance;
        subscribedManager.OnAchievementUnlocked += HandleUnlocked;

        // Debug opcional
        // Debug.Log("AchievementPopupUI: suscrito a AchievementManager ✅");
    }

    private void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnAchievementUnlocked -= HandleUnlocked;
    }

    private void HandleUnlocked(string id, string title, string desc)
    {
        queue.Enqueue((id, title, desc));

        if (!isShowing)
            StartCoroutine(ShowQueueRoutine());
    }

    private IEnumerator ShowQueueRoutine()
    {
        isShowing = true;

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();

            if (root != null) root.SetActive(true);
            if (titleText != null) titleText.text = item.title;
            if (descText != null) descText.text = item.desc;

            float t = showSeconds;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (root != null) root.SetActive(false);
            yield return null;
        }

        isShowing = false;
    }
}
