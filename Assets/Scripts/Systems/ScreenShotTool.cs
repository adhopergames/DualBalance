using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;

public class ScreenshotTool : MonoBehaviour
{
    void Update()
    {
        // Verifica teclado
        if (Keyboard.current == null) return;

        // Presiona P para screenshot
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            // Ruta: carpeta Screenshots en la raíz del proyecto
            string folderPath = Application.dataPath + "/../Screenshots/";

            // Crear carpeta si no existe
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Nombre del archivo
            string fileName = "screenshot_" + System.DateTime.Now.Ticks + ".png";

            // Ruta completa
            string fullPath = Path.Combine(folderPath, fileName);

            // Captura
            ScreenCapture.CaptureScreenshot(fullPath);

            Debug.Log("Screenshot guardado en: " + fullPath);
        }
    }
}