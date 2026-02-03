using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class AchievementItemUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;

    [Tooltip("LocalizeStringEvent del título (en el TMP del título)")]
    public LocalizeStringEvent titleLocalized;

    [Tooltip("LocalizeStringEvent de la descripción (en el TMP de la desc)")]
    public LocalizeStringEvent descLocalized;

    [Header("Locked Visual")]
    [Tooltip("Sprite del signo de interrogación (logro bloqueado)")]
    public Sprite lockedSprite;

    [Tooltip("Tint del icono cuando está bloqueado")]
    public Color lockedTint = new Color(1f, 1f, 1f, 0.6f);

    [Header("Localization")]
    [Tooltip("Nombre EXACTO de tu String Table Collection (como sale en el inspector)")]
    public string tableName = "TablaIdiomas";

    private string id;
    private Sprite unlockedSprite;
    private Color unlockedColor;

    // ============================================================
    // INIT: solo recibimos ID + icono desbloqueado
    // (el texto lo resuelve Localization por keys)
    // ============================================================
    public void Init(string achievementId, Sprite unlockedIcon)
    {
        id = achievementId; // ✅ guardamos el id para refrescar luego

        unlockedSprite = unlockedIcon; // ✅ icono real cuando esté desbloqueado
        unlockedColor = icon != null ? icon.color : Color.white; // ✅ color original del icono

        Refresh(); // ✅ dibuja el estado actual (bloqueado o desbloqueado)
    }

    // ============================================================
    // REFRESH: se llama al crear y cuando algo cambia (unlock/idioma)
    // ============================================================
    public void Refresh()
    {
        bool unlocked = AchievementManager.IsUnlocked(id); // ✅ consultamos PlayerPrefs

        // ---------------- ICONO ----------------
        if (icon != null)
        {
            icon.sprite = unlocked ? unlockedSprite : lockedSprite; // ✅ cambia sprite según estado
            icon.color = unlocked ? unlockedColor : lockedTint;    // ✅ cambia tint según estado
        }

        // ---------------- TEXTO ----------------
        if (unlocked)
        {
            // ✅ keys por id (de tu CSV): ACH_<id>_T y ACH_<id>_D
            SetLocalizedKeys($"ACH_{id}_T", $"ACH_{id}_D");
        }
        else
        {
            // ✅ keys genéricas para bloqueado (también en tu CSV)
            SetLocalizedKeys("ACH_LOCKED_T", "ACH_LOCKED_D");
        }
    }

    // ============================================================
    // SetLocalizedKeys: asigna Table + Entry correctamente
    // Compatible con tu versión del paquete
    // ============================================================
    private void SetLocalizedKeys(string titleKey, string descKey)
    {
        // --- Título ---
        if (titleLocalized != null)
        {
            // ✅ Selecciona la tabla por nombre (Table Collection)
            titleLocalized.StringReference.TableReference = tableName;

            // ✅ Selecciona la entrada por Key (Entry Name)
            titleLocalized.StringReference.TableEntryReference = titleKey;

            // ✅ Fuerza actualización inmediata (por si se instanció hace 1 frame)
            titleLocalized.RefreshString();
        }

        // --- Descripción ---
        if (descLocalized != null)
        {
            // ✅ Selecciona la tabla por nombre (Table Collection)
            descLocalized.StringReference.TableReference = tableName;

            // ✅ Selecciona la entrada por Key (Entry Name)
            descLocalized.StringReference.TableEntryReference = descKey;

            // ✅ Fuerza actualización inmediata
            descLocalized.RefreshString();
        }
    }
}
