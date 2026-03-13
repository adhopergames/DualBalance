using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class LanguageController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Awake()
    {
        // Leer idioma guardado
        int savedLocale = PlayerPrefs.GetInt("LocaleKey", 0);

        // Esperar a que el sistema de localization esté listo
        LocalizationSettings.InitializationOperation.WaitForCompletion();

        var locales = LocalizationSettings.AvailableLocales.Locales;
        savedLocale = Mathf.Clamp(savedLocale, 0, locales.Count - 1);

        // Aplicar idioma inmediatamente
        LocalizationSettings.SelectedLocale = locales[savedLocale];

        // Configurar dropdown sin disparar evento
        if (languageDropdown != null)
            languageDropdown.SetValueWithoutNotify(savedLocale);
    }

    private void Start()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(ChangeLocale);
    }

    public void ChangeLocale(int localeId)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        localeId = Mathf.Clamp(localeId, 0, locales.Count - 1);

        LocalizationSettings.SelectedLocale = locales[localeId];

        PlayerPrefs.SetInt("LocaleKey", localeId);
        PlayerPrefs.Save();
    }
}