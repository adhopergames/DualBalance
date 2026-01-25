using UnityEngine;

public class FallingObject : MonoBehaviour
{
    [Tooltip("Multiplicador por si un objeto debe caer más rápido/lento.")]
    public float speedMultiplier = 1f;

    [Tooltip("Y donde el objeto se destruye para ahorrar recursos.")]
    public float destroyY = -12f;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        float speed = GameManager.Instance.CurrentWorldSpeed * speedMultiplier;
        transform.Translate(Vector3.down * speed * Time.deltaTime);

        if (transform.position.y < destroyY)
            Destroy(gameObject);
    }
}
