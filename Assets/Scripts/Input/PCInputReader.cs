using UnityEngine;
using UnityEngine.InputSystem;

public class PCInputReader : MonoBehaviour, IPlayerInput
{
    public int GetLaneDelta()
    {
        // Por seguridad: si no hay teclado detectado
        if (Keyboard.current == null) return 0;

        // A o Flecha izquierda -> mover a la izquierda
        if (Keyboard.current.aKey.wasPressedThisFrame ||
            Keyboard.current.leftArrowKey.wasPressedThisFrame)
            return -1;

        // D o Flecha derecha -> mover a la derecha
        if (Keyboard.current.dKey.wasPressedThisFrame ||
            Keyboard.current.rightArrowKey.wasPressedThisFrame)
            return 1;

        return 0;
    }

    public bool GetLightAttack()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.kKey.wasPressedThisFrame;
    }

    public bool GetDarkAttack()
    {
        if (Keyboard.current == null) return false;
        return Keyboard.current.jKey.wasPressedThisFrame;
    }
}
