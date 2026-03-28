using System.Collections;
using UnityEngine;

/// <summary>
/// Efecto simple al destruir una pared:
/// - desactiva colisión
/// - hace un pequeño parpadeo
/// - reproduce partículas
/// - oculta el sprite
/// - destruye el objeto al final
/// </summary>
public class WallDestroyVFX : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Collider2D wallCollider;
    public ParticleSystem destroyParticles;

    [Header("Animation")]
    [Tooltip("Duración total del efecto antes de destruir la pared.")]
    public float duration = 0.12f;

    [Tooltip("Cantidad de parpadeos antes de desaparecer.")]
    public int blinkCount = 2;

    [Tooltip("Escala final ligera al desaparecer.")]
    public float scaleMultiplier = 1.04f;

    private bool isPlaying;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (wallCollider == null)
            wallCollider = GetComponent<Collider2D>();
    }

    public void PlayAndDestroy()
    {
        if (isPlaying) return;
        isPlaying = true;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // Desactivar colisión de inmediato
        if (wallCollider != null)
            wallCollider.enabled = false;

        // Reproducir partículas
        if (destroyParticles != null)
            destroyParticles.Play();

        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * scaleMultiplier;

        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float t = 0f;
        float blinkTimer = 0f;
        float blinkInterval = duration / Mathf.Max(1, blinkCount * 2);

        while (t < duration)
        {
            t += Time.deltaTime;
            blinkTimer += Time.deltaTime;

            float k = Mathf.Clamp01(t / duration);
            transform.localScale = Vector3.Lerp(startScale, endScale, k);

            if (spriteRenderer != null)
            {
                // Parpadeo rápido entre visible e invisible
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    Color c = spriteRenderer.color;
                    c.a = (c.a > 0.5f) ? 0.15f : 1f;
                    spriteRenderer.color = c;
                }
            }

            yield return null;
        }

        // Restaurar por limpieza
        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        Destroy(gameObject);
    }
}