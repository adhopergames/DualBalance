using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [Header("Tipo de sonido")]
    [SerializeField] private bool useBackSound = false;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (AudioManager.Instance == null) return;

        if (useBackSound)
            AudioManager.Instance.PlayUIBack();
        else
            AudioManager.Instance.PlayUIButton();
    }
}