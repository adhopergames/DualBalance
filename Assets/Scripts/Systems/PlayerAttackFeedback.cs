using System.Collections;
using UnityEngine;

/// <summary>
/// Feedback visual del ataque:
/// - Light sale desde la mano derecha
/// - Dark sale desde la mano izquierda
/// - Hace pop + expansión + fade out
/// - Guarda color y escala originales para no romperse después del primer uso
/// </summary>
public class PlayerAttackFeedback : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer lightAttackFX;
    public SpriteRenderer darkAttackFX;

    [Tooltip("Punto desde donde sale el ataque de Luz")]
    public Transform lightHandAnchor;

    [Tooltip("Punto desde donde sale el ataque de Oscuridad")]
    public Transform darkHandAnchor;

    [Header("Animation")]
    [Tooltip("Duración total del efecto")]
    public float duration = 0.16f;

    [Tooltip("Cuánto crece respecto a su escala original")]
    public float scaleMultiplier = 1.6f;

    [Tooltip("Pequeño empuje hacia afuera desde la mano")]
    public float outwardDistance = 0.18f;

    [Header("Curves")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine lightRoutine;
    private Coroutine darkRoutine;

    private Color lightBaseColor;
    private Color darkBaseColor;

    private Vector3 lightBaseScale;
    private Vector3 darkBaseScale;

    private Vector3 lightBaseLocalPos;
    private Vector3 darkBaseLocalPos;

    private void Awake()
    {
        if (lightAttackFX != null)
        {
            lightBaseColor = lightAttackFX.color;
            lightBaseScale = lightAttackFX.transform.localScale;
            lightBaseLocalPos = lightAttackFX.transform.localPosition;

            SetRendererAlpha(lightAttackFX, 0f);
            lightAttackFX.gameObject.SetActive(false);
        }

        if (darkAttackFX != null)
        {
            darkBaseColor = darkAttackFX.color;
            darkBaseScale = darkAttackFX.transform.localScale;
            darkBaseLocalPos = darkAttackFX.transform.localPosition;

            SetRendererAlpha(darkAttackFX, 0f);
            darkAttackFX.gameObject.SetActive(false);
        }
    }

    public void PlayLight()
    {
        if (lightAttackFX == null) return;

        if (lightRoutine != null)
            StopCoroutine(lightRoutine);

        lightRoutine = StartCoroutine(
            PlayRoutine(
                lightAttackFX,
                lightHandAnchor,
                lightBaseColor,
                lightBaseScale,
                lightBaseLocalPos,
                Vector3.right
            )
        );
    }

    public void PlayDark()
    {
        if (darkAttackFX == null) return;

        if (darkRoutine != null)
            StopCoroutine(darkRoutine);

        darkRoutine = StartCoroutine(
            PlayRoutine(
                darkAttackFX,
                darkHandAnchor,
                darkBaseColor,
                darkBaseScale,
                darkBaseLocalPos,
                Vector3.left
            )
        );
    }

    private IEnumerator PlayRoutine(
        SpriteRenderer target,
        Transform anchor,
        Color baseColor,
        Vector3 baseScale,
        Vector3 fallbackLocalPos,
        Vector3 direction
    )
    {
        if (target == null) yield break;

        target.gameObject.SetActive(true);

        // Posición de salida: desde el anchor si existe, si no usa su posición local base
        if (anchor != null)
        {
            target.transform.position = anchor.position;
            target.transform.rotation = anchor.rotation;
        }
        else
        {
            target.transform.localPosition = fallbackLocalPos;
        }

        Vector3 startScale = baseScale;
        Vector3 endScale = baseScale * scaleMultiplier;

        Vector3 startPos = target.transform.position;
        Vector3 endPos = startPos + direction.normalized * outwardDistance;

        // Reset inicial
        target.transform.localScale = startScale;
        SetRendererAlpha(target, baseColor.a);

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            float scaleK = scaleCurve.Evaluate(k);
            float alphaK = alphaCurve.Evaluate(k);

            target.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, scaleK);
            target.transform.position = Vector3.LerpUnclamped(startPos, endPos, scaleK);

            Color c = baseColor;
            c.a = baseColor.a * alphaK;
            target.color = c;

            yield return null;
        }

        // Restaurar por limpieza
        target.transform.localScale = baseScale;

        if (anchor != null)
            target.transform.position = anchor.position;
        else
            target.transform.localPosition = fallbackLocalPos;

        SetRendererAlpha(target, 0f);
        target.gameObject.SetActive(false);
    }

    private void SetRendererAlpha(SpriteRenderer sr, float alpha)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }
}