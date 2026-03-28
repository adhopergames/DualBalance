using System.Collections;
using UnityEngine;

public class OrbPickupVFX : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Collider2D col;
    public ParticleSystem pickupParticles;

    [Header("Animation")]
    public float duration = 0.18f;
    public Vector3 endScale = new Vector3(1.35f, 1.35f, 1f);

    private bool picked;

    public void PlayAndDestroy()
    {
        if (picked) return;
        picked = true;

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (col != null)
            col.enabled = false;

        if (pickupParticles != null)
            pickupParticles.Play();

        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            transform.localScale = Vector3.Lerp(startScale, endScale, k);

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, k);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}