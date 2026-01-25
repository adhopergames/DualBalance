using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    public LaneManager laneManager;
    public PlayerEnergy playerEnergy;

    [Tooltip("GameConfig")]
    public GameConfig config;

    [Header("Prefabs")]
    public GameObject neutralObstaclePrefab;
    public GameObject elementalWallLightPrefab;
    public GameObject elementalWallDarkPrefab;
    public GameObject orbLightPrefab;
    public GameObject orbDarkPrefab;
    public GameObject orbDualPrefab;

    private float timer;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        timer += Time.deltaTime;

        // Intervalo dinámico controlado por GameManager + config
        float interval = GameManager.Instance.CurrentSpawnInterval;

        if (timer >= interval)
        {
            timer = 0f;
            SpawnPattern(interval);
        }
    }

    /// Decide qué patrón spawnear (neutral / pared / orbe) usando dificultad + pesos del config.
    private void SpawnPattern(float currentInterval)
    {
        if (config == null)
        {
            Debug.LogError("SpawnManager: Falta asignar GameConfig.");
            return;
        }

        // Dificultad 0..1 basada en el intervalo actual:
        // - Al inicio interval ~ baseSpawnInterval => difficulty01 ~ 0
        // - En dificultad alta interval ~ minSpawnInterval => difficulty01 ~ 1
        float difficulty01 = Mathf.InverseLerp(config.baseSpawnInterval, config.minSpawnInterval, currentInterval);

        // Chance de pared elemental crece con la dificultad (pocas al inicio, más luego)
        float wallChance = Mathf.Lerp(config.elementalWallChanceStart, config.elementalWallChanceEnd, difficulty01);

        // Chance total de orbe (constante, editable desde config)
        float orbChance = config.orbChance;

        // Lo demás se vuelve neutral (obstáculo normal)
        float neutralChance = 1f - wallChance - orbChance;

        // Seguridad por si alguien pone valores que sumen > 1
        neutralChance = Mathf.Clamp01(neutralChance);
        wallChance = Mathf.Clamp01(wallChance);
        orbChance = Mathf.Clamp01(orbChance);

        float r = Random.value;

        // Neutral primero (lo más común)
        if (r < neutralChance)
        {
            SpawnNeutralRow();
            return;
        }

        // Luego paredes elementales
        r -= neutralChance;
        if (r < wallChance)
        {
            SpawnElementalWall();
            return;
        }

        // Si no fue neutral ni pared, entonces orbe
        SpawnOrb();
    }

    private void SpawnNeutralRow()
    {
        // Bloquea 1 o 2 carriles
        int lanesBlocked = Random.Range(1, 3);

        bool[] used = new bool[3];

        for (int i = 0; i < lanesBlocked; i++)
        {
            int lane;
            do { lane = Random.Range(0, 3); }
            while (used[lane]);

            used[lane] = true;

            float x = laneManager.GetLaneX(lane);
            Vector3 pos = new Vector3(x, transform.position.y, 0f);

            Instantiate(neutralObstaclePrefab, pos, Quaternion.identity);
        }
    }

    private void SpawnElementalWall()
    {
        // 50/50 de tipo de pared (si quieres, luego lo sesgamos según energía)
        bool isLight = Random.value < 0.5f;

        GameObject prefab = isLight ? elementalWallLightPrefab : elementalWallDarkPrefab;

        // La pared cubre los 3 carriles, centrada
        Vector3 pos = new Vector3(laneManager.centerX, transform.position.y, 0f);
        Instantiate(prefab, pos, Quaternion.identity);
    }

    private void SpawnOrb()
    {
        if (config == null) return;

        int lane = Random.Range(0, 3);
        float x = laneManager.GetLaneX(lane);
        Vector3 pos = new Vector3(x, transform.position.y, 0f);

        // Energía normalizada (0..1). Si playerEnergy no está, asumimos lleno.
        float maxE = (config.maxEnergy <= 0f) ? 100f : config.maxEnergy;

        float lightE = (playerEnergy != null) ? playerEnergy.lightEnergy : maxE;
        float darkE = (playerEnergy != null) ? playerEnergy.darkEnergy : maxE;

        float light01 = Mathf.Clamp01(lightE / maxE);
        float dark01 = Mathf.Clamp01(darkE / maxE);

        // Déficit: 1 = muy vacío, 0 = lleno
        float lightDeficit = 1f - light01;
        float darkDeficit = 1f - dark01;

        // Pesos (weights) para decidir tipo de orbe:
        // - Light y Dark salen “parejo” por el baseWeight
        // - Se sesga hacia el que esté más vacío (deficit * biasWeight)
        float wLight = config.orbBaseWeight + lightDeficit * config.orbBiasWeight;
        float wDark = config.orbBaseWeight + darkDeficit * config.orbBiasWeight;

        // Dual raro (peso pequeño)
        float wDual = config.dualOrbWeight;

        // Selección por ruleta (weighted random)
        float total = wLight + wDark + wDual;
        float r = Random.value * total;

        GameObject prefab;
        if (r < wLight) prefab = orbLightPrefab;
        else if (r < wLight + wDark) prefab = orbDarkPrefab;
        else prefab = orbDualPrefab;

        Instantiate(prefab, pos, Quaternion.identity);
    }
}
