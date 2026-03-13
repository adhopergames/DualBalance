using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

/// <summary>
/// AchievementPopupUI:
/// - Espera a que exista AchievementManager.Instance
/// - Se suscribe a OnAchievementUnlocked
/// - Muestra un popup por X segundos (unscaled)
/// - Cola por si caen varios logros seguidos
/// - Muestra icono + texto fijo + título localizado
/// - Usa un animador externo para mostrar/ocultar el popup
/// </summary>
public class AchievementPopupUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Root completo del popup")]
    public GameObject root;

    [Tooltip("Imagen del logro desbloqueado")]
    public Image achievementIcon;

    [Tooltip("Texto localizado fijo, por ejemplo: Logro desbloqueado")]
    public LocalizeStringEvent unlockedLocalized;

    [Tooltip("Texto localizado del título del logro")]
    public LocalizeStringEvent titleLocalized;

    [Header("Animation")]
    [Tooltip("Script encargado de animar el popup")]
    public AchievementPopupAnimator popupAnimator;

    [Header("Localization")]
    [Tooltip("Nombre EXACTO de tu String Table Collection")]
    public string tableName = "TablaIdiomas";

    [Header("Timing")]
    public float showSeconds = 2.0f;

    [Header("Icons")]
    [Tooltip("Lista de iconos por id de logro")]
    public List<AchievementIcon> icons = new();

    [System.Serializable]
    public struct AchievementIcon
    {
        public string id;
        public Sprite icon;
    }

    private readonly Queue<PopupData> queue = new();
    private bool isShowing;
    private AchievementManager subscribedManager;
    private readonly Dictionary<string, Sprite> iconMap = new();

    private struct PopupData
    {
        public string id;
        public Sprite icon;

        public PopupData(string id, Sprite icon)
        {
            this.id = id;
            this.icon = icon;
        }
    }

    private void Awake()
    {
        // Construimos el mapa id -> icono una sola vez
        iconMap.Clear();

        foreach (var entry in icons)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.icon != null && !iconMap.ContainsKey(entry.id))
                iconMap.Add(entry.id, entry.icon);
        }
    }

    private void Start()
    {
        // Dejamos el popup oculto al inicio
        if (root != null)
            root.SetActive(false);

        // Si existe animador, lo reseteamos a estado oculto
        if (popupAnimator != null)
            popupAnimator.ResetToHiddenState();

        isShowing = false;

        // Esperar al manager por si el orden de ejecución varía
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        while (AchievementManager.Instance == null)
            yield return null;

        subscribedManager = AchievementManager.Instance;
        subscribedManager.OnAchievementUnlocked += HandleUnlocked;
    }

    private void OnDestroy()
    {
        if (subscribedManager != null)
            subscribedManager.OnAchievementUnlocked -= HandleUnlocked;
    }

    private void HandleUnlocked(string id, string title, string desc)
    {
        Sprite icon = null;
        iconMap.TryGetValue(id, out icon);

        // Guardamos el id para resolver la localización y el icono para mostrarlo
        queue.Enqueue(new PopupData(id, icon));

        if (!isShowing)
            StartCoroutine(ShowQueueRoutine());
    }

    private IEnumerator ShowQueueRoutine()
    {
        isShowing = true;

        while (queue.Count > 0)
        {
            PopupData item = queue.Dequeue();

            // -------------------------
            // Mostrar popup con animación si existe
            // -------------------------
            if (popupAnimator != null)
            {
                popupAnimator.PlayShow();
            }
            else if (root != null)
            {
                root.SetActive(true);
            }

            // -------------------------
            // Texto fijo: "Logro desbloqueado"
            // -------------------------
            if (unlockedLocalized != null)
            {
                unlockedLocalized.StringReference.TableReference = tableName;
                unlockedLocalized.StringReference.TableEntryReference = "ACH_UNLOCKED_POPUP";
                unlockedLocalized.RefreshString();
            }

            // -------------------------
            // Título del logro por id
            // -------------------------
            if (titleLocalized != null)
            {
                titleLocalized.StringReference.TableReference = tableName;
                titleLocalized.StringReference.TableEntryReference = $"ACH_{item.id}_T";
                titleLocalized.RefreshString();
            }

            // -------------------------
            // Icono
            // -------------------------
            if (achievementIcon != null)
            {
                achievementIcon.sprite = item.icon;
                achievementIcon.enabled = item.icon != null;
            }

            // Tiempo visible del popup
            float t = showSeconds;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            // -------------------------
            // Ocultar popup con animación si existe
            // -------------------------
            if (popupAnimator != null)
            {
                popupAnimator.PlayHide();

                // Esperamos el tiempo de salida para no cortar la animación
                yield return new WaitForSecondsRealtime(popupAnimator.hideDuration);
            }
            else if (root != null)
            {
                root.SetActive(false);
            }

            yield return null;
        }

        isShowing = false;
    }
}