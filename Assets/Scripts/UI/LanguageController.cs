using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class LanguageController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private bool active = false;

    private void Start()
    {
        // Leer idioma guardado
        int savedLocale = PlayerPrefs.GetInt("LocaleKey", 0);

        // Inicializar dropdown SIN disparar evento
        languageDropdown.SetValueWithoutNotify(savedLocale);

        // Aplicar idioma al iniciar
        ChangeLocale(savedLocale);

        // Escuchar cambios del dropdown
        languageDropdown.onValueChanged.AddListener(ChangeLocale);
    }

    public void ChangeLocale(int localeId)
    {
        if (active) return;
        StartCoroutine(SetLocale(localeId));
    }

    private IEnumerator SetLocale(int localeId)
    {
        active = true;

        // Esperar a que Localization esté listo
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        localeId = Mathf.Clamp(localeId, 0, locales.Count - 1);

        LocalizationSettings.SelectedLocale = locales[localeId];

        PlayerPrefs.SetInt("LocaleKey", localeId);
        PlayerPrefs.Save();

        active = false;
    }
}
