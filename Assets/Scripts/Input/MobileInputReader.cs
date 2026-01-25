using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerInput
{
    // -1 izquierda, 0 nada, +1 derecha
    int GetLaneDelta();

    // Ataques (true solo ese frame)
    bool GetLightAttack();
    bool GetDarkAttack();
}


/// Input para móvil usando Touchscreen (New Input System).
/// - Swipe izquierda/derecha => cambiar carril
/// - Tap lado izquierdo => ataque Dark
/// - Tap lado derecho => ataque Light
/// Nota: separa swipe vs tap con un umbral para evitar confusiones.

public class MobileInputReader : MonoBehaviour, IPlayerInput
{
    [Header("Swipe Settings")]
    [Tooltip("Distancia mínima (en pixeles) para considerar un swipe.")]
    public float swipeThresholdPixels = 70f;

    [Tooltip("Debe ser más horizontal que vertical para contar como swipe.")]
    public float horizontalDominance = 1.2f; // absX debe ser > absY * 1.2

    [Header("Tap Settings")]
    [Tooltip("Tiempo máximo (segundos) para considerar un toque como tap.")]
    public float tapMaxTime = 0.25f;

    [Tooltip("Movimiento máximo (pixeles) para seguir siendo tap.")]
    public float tapMoveTolerancePixels = 25f;

    // Estado del gesto
    private bool tracking;
    private Vector2 startPos;
    private float startTime;

    // Para no disparar swipe múltiples veces en el mismo toque
    private bool swipeFired;

    // Salidas one-shot por frame
    private int laneDeltaThisFrame;
    private bool lightAttackThisFrame;
    private bool darkAttackThisFrame;

    private void Update()
    {
        // Limpieza por frame
        laneDeltaThisFrame = 0;
        lightAttackThisFrame = false;
        darkAttackThisFrame = false;

        // Si no hay touchscreen, no hacemos nada (en PC)
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (touch == null) return;

        bool pressed = touch.press.isPressed;
        Vector2 pos = touch.position.ReadValue();

        // Inicio del toque
        if (pressed && !tracking)
        {
            tracking = true;
            swipeFired = false;
            startPos = pos;
            startTime = Time.unscaledTime;
            return;
        }

        // Mientras está presionado: detectar swipe INMEDIATO
        if (pressed && tracking && !swipeFired)
        {
            Vector2 delta = pos - startPos;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            // Debe superar umbral y ser principalmente horizontal
            bool isSwipe =
                absX >= swipeThresholdPixels &&
                absX > absY * horizontalDominance;

            if (isSwipe)
            {
                // Disparar movimiento instantáneo
                laneDeltaThisFrame = (delta.x > 0f) ? 1 : -1;

                // Marcamos que ya se usó este toque para swipe
                swipeFired = true;
                return;
            }
        }

        // Fin del toque: si NO hubo swipe, puede ser tap (ataque)
        if (!pressed && tracking)
        {
            float dt = Time.unscaledTime - startTime;
            Vector2 totalDelta = pos - startPos;

            tracking = false;

            // Si ya disparó swipe, no hacemos tap
            if (swipeFired) return;

            float absX = Mathf.Abs(totalDelta.x);
            float absY = Mathf.Abs(totalDelta.y);

            bool isTap =
                dt <= tapMaxTime &&
                absX <= tapMoveTolerancePixels &&
                absY <= tapMoveTolerancePixels;

            if (isTap)
            {
                // Tap izquierda = Dark, derecha = Light
                bool rightSide = pos.x >= Screen.width * 0.5f;

                if (rightSide) lightAttackThisFrame = true;
                else darkAttackThisFrame = true;
            }
        }
    }

    public int GetLaneDelta() => laneDeltaThisFrame;
    public bool GetLightAttack() => lightAttackThisFrame;
    public bool GetDarkAttack() => darkAttackThisFrame;
}
