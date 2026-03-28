using UnityEngine;

public static class HapticManager
{
    /// <summary>
    /// Vibración corta y sutil para Android.
    /// Úsala para ataques o micro-feedback.
    /// </summary>
    public static void LightTap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(15, 90);
#endif
    }

    /// <summary>
    /// Vibración un poco más fuerte.
    /// Útil para muerte, choque, etc.
    /// </summary>
    public static void MediumTap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(25, 140);
#endif
    }

    /// <summary>
    /// Implementación interna Android.
    /// durationMs: duración en milisegundos
    /// amplitude: intensidad entre 1 y 255
    /// </summary>
    private static void VibrateAndroid(long durationMs, int amplitude)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator == null) return;

                // Verificar si el dispositivo tiene vibrador
                bool hasVibrator = vibrator.Call<bool>("hasVibrator");
                if (!hasVibrator) return;

                using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    int sdkInt = version.GetStatic<int>("SDK_INT");

                    if (sdkInt >= 26)
                    {
                        using (AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                                "createOneShot",
                                durationMs,
                                amplitude
                            );

                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", durationMs);
                    }
                }
            }
        }
        catch
        {
            // Ignorar silenciosamente en dispositivos incompatibles
        }
#endif
    }
}