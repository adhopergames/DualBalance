using UnityEngine;

public class LaneManager : MonoBehaviour
{
    [Tooltip("Cantidad de carriles (ej: 3).")]
    public int laneCount = 3;

    [Tooltip("X del carril central (normalmente 0).")]
    public float centerX = 0f;

    [Tooltip("Porcentaje del ancho visible que ocuparán los carriles (0–1).")]
    [Range(0.2f, 0.9f)]
    public float widthPercent = 0.8f;

    [Header("PC Width Limit")]
    [Tooltip("Si está activo, limita el ancho jugable máximo (ideal para PC horizontal).")]
    public bool clampPlayableWidth = true;

    [Tooltip("Ancho jugable máximo en UNIDADES DE MUNDO. Piensa en 'como una tablet'.")]
    public float maxPlayableWorldWidth = 5.5f;

    private float laneSpacing;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        RecalculateLanes();

    }

    private void RecalculateLanes()
    {
        // Distancia de la cámara al plano 2D
        float z = Mathf.Abs(cam.transform.position.z);

        // Bordes visibles del mundo
        float leftX = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;
        float rightX = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;

        float worldWidth = rightX - leftX;

        // Ancho que queremos usar para carriles (basado en %)
        float usableWidth = worldWidth * widthPercent;

        // ✅ Si estamos en PC/pantalla ancha, capamos el ancho máximo
        if (clampPlayableWidth)
            usableWidth = Mathf.Min(usableWidth, maxPlayableWorldWidth);

        // Espaciado entre carriles
        laneSpacing = usableWidth / Mathf.Max(1, (laneCount - 1));
    }

    // 0 = izquierda, 1 = centro, 2 = derecha
    public float GetLaneX(int laneIndex)
    {
        return centerX + (laneIndex - (laneCount - 1) / 2f) * laneSpacing;
    }
}
