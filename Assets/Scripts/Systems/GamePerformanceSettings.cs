using UnityEngine;

public class GamePerformanceSettings : MonoBehaviour
{
    void Awake()
    {
        // Desactiva VSync para que targetFrameRate controle el FPS
        QualitySettings.vSyncCount = 0;

        // Fuerza el juego a 60 FPS
        Application.targetFrameRate = 60;
    }
}