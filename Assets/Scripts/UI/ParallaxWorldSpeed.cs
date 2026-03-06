using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxWorldSpeed : MonoBehaviour
{
    [Tooltip("Qué tan rápido se mueve la textura por cada unidad de velocidad del mundo.")]
    public float parallaxMultiplier = 0.02f;

    [Tooltip("Si tu mundo se mueve hacia abajo, normalmente esto debe ser true.")]
    public bool invert = false;

    private Material mat; // OJO: esto instancia material por objeto (ok si es 1 fondo)
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = sr.material; // usa la instancia del material para poder modificar offset
    }

    void Update()
    {
        // Si no existe GameManager o está pausado / gameover, no muevas.
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State != GameState.Playing) return;

        float speed = GameManager.Instance.CurrentWorldSpeed;

        // Avance por frame basado en velocidad del mundo
        float dir = invert ? 1f : -1f;
        float delta = speed * parallaxMultiplier * Time.deltaTime;

        Vector2 off = mat.mainTextureOffset;
        off.y += dir * delta;
        mat.mainTextureOffset = off;
    }
}