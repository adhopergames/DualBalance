using UnityEngine;

/// <summary>
/// Guardado simple usando PlayerPrefs.
/// </summary>
public class SaveManager : MonoBehaviour
{
    private const string BestScoreKey = "BEST_SCORE";

    public static int GetBestScore()
    {
        return PlayerPrefs.GetInt(BestScoreKey, 0);
    }

    public static bool TrySetBestScore(int newScore)
    {
        int best = GetBestScore();

        if (newScore <= best) return false;

        PlayerPrefs.SetInt(BestScoreKey, newScore);
        PlayerPrefs.Save();

        // ✅ Chequeo de logros (por milestones de best score)
        AchievementManager.TryCheckAll();

        return true;
    }


    /// ✅ Borra el best score (para pruebas rápidas).

    public static void ClearBestScore()
    {
        PlayerPrefs.DeleteKey(BestScoreKey);
        PlayerPrefs.Save();
    }

    // ✅ Opcional: botón en el Inspector si este MonoBehaviour está en escena
    [ContextMenu("DEBUG/Clear Best Score")]
    private void DebugClearBestScore()
    {
        ClearBestScore();
        Debug.Log("SaveManager: Best Score borrado ✅");
    }
}
