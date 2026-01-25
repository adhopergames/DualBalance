using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {

        // Si choca con obstáculo neutral => GameOver
        if (other.GetComponent<NeutralObstacle>() != null)
        {
            if (GameManager.Instance.IsReviveInvulnerable) return;

            // ✅ Stats
            StatsManager.AddDeathObstacle();

            GameManager.Instance.TriggerGameOver();
            return;
        }

        // Si choca con pared elemental => GameOver
        if (other.GetComponent<ElementalWall>() != null)
        {
            if (GameManager.Instance.IsReviveInvulnerable) return;

            // ✅ Stats (ahora por tipo de pared)
            var wall = other.GetComponent<ElementalWall>();
            StatsManager.AddDeathWall(wall.elementType);

            GameManager.Instance.TriggerGameOver();
            return;
        }

        // Si choca con orbe => lo recoge (maneja energía) y se destruye el orbe
        var orb = other.GetComponent<Orb>();
        if (orb != null)
        {
            // ✅ Stats: contar orbs recogidos
            StatsManager.AddOrb(orb.orbType);

            var energy = GetComponent<PlayerEnergy>();
            energy.AddOrb(orb.orbType);
            AudioManager.Instance?.PlayOrbPickup(orb.orbType);
            Destroy(orb.gameObject);
        }
    }
}
