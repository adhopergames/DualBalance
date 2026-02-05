using UnityEngine;

/// Detecta colisiones del Player con:
/// - Obstáculos (muerte)
/// - Paredes elementales (muerte)
/// - Orbes (pickup + energía + score multiplier)
public class PlayerCollision : MonoBehaviour
{
    private PlayerEnergy energy; // cache para no hacer GetComponent en cada orb

    private void Awake()
    {
        // Cache del componente (más eficiente)
        energy = GetComponent<PlayerEnergy>();
        if (energy == null)
            Debug.LogWarning("[PlayerCollision] No se encontró PlayerEnergy en el Player.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Seguridad: si no hay GameManager, no hacemos nada raro
        if (GameManager.Instance == null) return;

        // -------------------------
        // 1) NeutralObstacle => muerte
        // -------------------------
        if (other.GetComponent<NeutralObstacle>() != null)
        {
            // ✅ Si está invulnerable por revive, ignoramos
            if (GameManager.Instance.IsReviveInvulnerable) return;

            // ✅ Stats
            StatsManager.AddDeathObstacle();

            // ✅ GameOver (pending/final lo decide el GameManager)
            GameManager.Instance.TriggerGameOver();
            return;
        }

        // -------------------------
        // 2) ElementalWall => muerte
        // -------------------------
        var wall = other.GetComponent<ElementalWall>();
        if (wall != null)
        {
            // ✅ Si está invulnerable por revive, ignoramos
            if (GameManager.Instance.IsReviveInvulnerable) return;

            // ✅ Stats por tipo de pared
            StatsManager.AddDeathWall(wall.elementType);

            GameManager.Instance.TriggerGameOver();
            return;
        }

        // -------------------------
        // 3) Orb => pickup
        // -------------------------
        var orb = other.GetComponent<Orb>();
        if (orb != null)
        {
            // ✅ Stats: contar orbes recogidos
            StatsManager.AddOrb(orb.orbType);

            // ✅ Energía (+ ahora también activa buff de score dentro de AddOrb)
            if (energy != null)
                energy.AddOrb(orb.orbType);

            // ✅ Audio
            AudioManager.Instance?.PlayOrbPickup(orb.orbType);

            // ✅ Destruir el orbe
            Destroy(orb.gameObject);
        }
    }
}
