using UnityEngine;

public class HandEmitterWiggle : MonoBehaviour
{
    [Header("References")]
    public Transform emitterLeft;   // el que se ve a la izquierda en pantalla
    public Transform emitterRight;  // el que se ve a la derecha en pantalla

    [Header("Wiggle Settings")]
    public Vector2 amplitude = new Vector2(0.06f, 0.04f);
    public float frequency = 2.5f;

    [Tooltip("Si está activo, el movimiento en Y también será alternado (uno sube mientras el otro baja).")]
    public bool alternateY = false;

    private Vector3 baseLeftPos;
    private Vector3 baseRightPos;

    private void Awake()
    {
        if (emitterLeft) baseLeftPos = emitterLeft.localPosition;
        if (emitterRight) baseRightPos = emitterRight.localPosition;
    }

    private void Update()
    {
        float t = Time.time * frequency;

        // Offset base suave
        float x = Mathf.Sin(t) * amplitude.x;
        float y = Mathf.Cos(t * 2f) * amplitude.y;

        // Mano izquierda (en pantalla)
        if (emitterLeft)
        {
            float yL = alternateY ? +y : y;
            emitterLeft.localPosition = baseLeftPos + new Vector3(+x, yL, 0f);
        }

        // Mano derecha (en pantalla) - ESPEJO en X
        if (emitterRight)
        {
            float yR = alternateY ? -y : y;
            emitterRight.localPosition = baseRightPos + new Vector3(-x, yR, 0f);
        }
    }
}
