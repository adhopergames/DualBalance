using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class LanguageController : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown mobileLanguageDropdown;
    [SerializeField] private TMP_Dropdown pcLanguageDropdown;

    private bool isChanging;

    private void Awake()
    {
        int savedLocale = PlayerPrefs.GetInt("LocaleKey", 0);

        LocalizationSettings.InitializationOperation.WaitForCompletion();

        var locales = LocalizationSettings.AvailableLocales.Locales;
        savedLocale = Mathf.Clamp(savedLocale, 0, locales.Count - 1);

        LocalizationSettings.SelectedLocale = locales[savedLocale];

        SetDropdownValueWithoutEvent(mobileLanguageDropdown, savedLocale);
        SetDropdownValueWithoutEvent(pcLanguageDropdown, savedLocale);
    }

    private void Start()
    {
        if (mobileLanguageDropdown != null)
            mobileLanguageDropdown.onValueChanged.AddListener(ChangeLocale);

        if (pcLanguageDropdown != null)
            pcLanguageDropdown.onValueChanged.AddListener(ChangeLocale);
    }

    private void OnDestroy()
    {
        if (mobileLanguageDropdown != null)
            mobileLanguageDropdown.onValueChanged.RemoveListener(ChangeLocale);

        if (pcLanguageDropdown != null)
            pcLanguageDropdown.onValueChanged.RemoveListener(ChangeLocale);
    }

    public void ChangeLocale(int localeId)
    {
        if (isChanging) return;

        isChanging = true;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        localeId = Mathf.Clamp(localeId, 0, locales.Count - 1);

        LocalizationSettings.SelectedLocale = locales[localeId];

        PlayerPrefs.SetInt("LocaleKey", localeId);
        PlayerPrefs.Save();

        SetDropdownValueWithoutEvent(mobileLanguageDropdown, localeId);
        SetDropdownValueWithoutEvent(pcLanguageDropdown, localeId);

        isChanging = false;
    }

    private void SetDropdownValueWithoutEvent(TMP_Dropdown dropdown, int value)
    {
        if (dropdown == null) return;

        dropdown.SetValueWithoutNotify(value);
        dropdown.RefreshShownValue();
    }
}