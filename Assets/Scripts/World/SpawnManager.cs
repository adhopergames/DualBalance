using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("References")]
    public LaneManager laneManager;
    public PlayerEnergy playerEnergy;

    [Tooltip("Configuración general del juego (spawn, dificultad, energía, etc.).")]
    public GameConfig config;

    [Header("Prefabs")]
    public GameObject neutralObstaclePrefab;
    public GameObject elementalWallLightPrefab;
    public GameObject elementalWallDarkPrefab;
    public GameObject orbLightPrefab;
    public GameObject orbDarkPrefab;
    public GameObject orbDualPrefab;

    // Temporizador interno para saber cuándo toca hacer el próximo spawn
    private float timer;

    private void Update()
    {
        // Si no se está jugando, no spawneamos nada
        if (GameManager.Instance.State != GameState.Playing) return;

        // Acumulamos tiempo
        timer += Time.deltaTime;

        // Obtenemos el intervalo actual desde el GameManager
        // (este cambia con la dificultad)
        float interval = GameManager.Instance.CurrentSpawnInterval;

        // Cuando el timer alcanza el intervalo, hacemos un nuevo spawn
        if (timer >= interval)
        {
            timer = 0f;
            SpawnPattern(interval);
        }
    }

    /// Decide qué patrón spawnear:
    /// - Obstáculos neutrales
    /// - Pared elemental
    /// - Orbe (solo o acompañado de un neutral)
    ///
    /// Usa la dificultad actual y los valores del GameConfig.
    private void SpawnPattern(float currentInterval)
    {
        if (config == null)
        {
            Debug.LogError("SpawnManager: Falta asignar GameConfig.");
            return;
        }

        // difficulty01 va de 0 a 1:
        // 0 = inicio del juego
        // 1 = dificultad alta / intervalo mínimo
        float difficulty01 = Mathf.InverseLerp(
            config.baseSpawnInterval,
            config.minSpawnInterval,
            currentInterval
        );

        // La probabilidad de pared aumenta con la dificultad
        float wallChance = Mathf.Lerp(
            config.elementalWallChanceStart,
            config.elementalWallChanceEnd,
            difficulty01
        );

        // La probabilidad de orbe viene fija desde el config
        float orbChance = config.orbChance;

        // Lo que sobra se toma como neutral
        float neutralChance = 1f - wallChance - orbChance;

        // Seguridad por si alguien pone valores raros en el config
        neutralChance = Mathf.Clamp01(neutralChance);
        wallChance = Mathf.Clamp01(wallChance);
        orbChance = Mathf.Clamp01(orbChance);

        // Valor aleatorio para decidir qué patrón sale
        float r = Random.value;

        // 1) Patrón neutral
        if (r < neutralChance)
        {
            SpawnNeutralRow();
            return;
        }

        // 2) Patrón pared elemental
        r -= neutralChance;
        if (r < wallChance)
        {
            SpawnElementalWall();
            return;
        }

        // 3) Patrón orbe
        // Aquí entra la nueva lógica:
        // el orbe puede salir solo o acompañado por un neutral
        SpawnOrbPattern();
    }

    /// Spawnea una fila neutral:
    /// - bloquea 1 o 2 carriles aleatorios
    private void SpawnNeutralRow()
    {
        // Random.Range(1, 3) en int devuelve 1 o 2
        int lanesBlocked = Random.Range(1, 3);

        // Array para marcar carriles ya usados y no repetirlos
        bool[] used = new bool[3];

        for (int i = 0; i < lanesBlocked; i++)
        {
            int lane;

            // Buscar un carril que aún no haya sido usado
            do { lane = Random.Range(0, 3); }
            while (used[lane]);

            used[lane] = true;

            float x = laneManager.GetLaneX(lane);
            Vector3 pos = new Vector3(x, transform.position.y, 0f);

            Instantiate(neutralObstaclePrefab, pos, Quaternion.identity);
        }
    }

    /// Spawnea una pared elemental que ocupa el centro
    /// (normalmente cubre visualmente los 3 carriles).
    private void SpawnElementalWall()
    {
        // 50/50 entre pared Light y Dark
        bool isLight = Random.value < 0.5f;

        GameObject prefab = isLight ? elementalWallLightPrefab : elementalWallDarkPrefab;

        Vector3 pos = new Vector3(laneManager.centerX, transform.position.y, 0f);
        Instantiate(prefab, pos, Quaternion.identity);
    }

    /// Patrón de orbe:
    /// - Siempre spawnea 1 orbe en un carril aleatorio
    /// - Con cierta probabilidad, además spawnea 1 obstáculo neutral
    ///   en otro carril diferente
    private void SpawnOrbPattern()
    {
        // Elegimos el carril del orbe
        int orbLane = Random.Range(0, 3);

        // Siempre spawneamos el orbe
        SpawnOrbInLane(orbLane);

        // Probabilidad de que el orbe venga acompañado por un neutral
        if (config != null && Random.value <= config.orbWithNeutralChance)
        {
            // Elegimos otro carril distinto al del orbe
            int neutralLane = GetRandomLaneExcluding(orbLane);

            float x = laneManager.GetLaneX(neutralLane);
            Vector3 pos = new Vector3(x, transform.position.y, 0f);

            Instantiate(neutralObstaclePrefab, pos, Quaternion.identity);
        }
    }

    /// Devuelve un carril aleatorio excluyendo uno específico.
    /// Ejemplo:
    /// si excludedLane = 1, entonces solo puede devolver 0 o 2.
    private int GetRandomLaneExcluding(int excludedLane)
    {
        int lane;
        do { lane = Random.Range(0, 3); }
        while (lane == excludedLane);

        return lane;
    }

    /// Spawnea un orbe en un carril específico.
    /// El tipo de orbe (Light, Dark o Dual) se decide según:
    /// - energía actual del jugador
    /// - pesos configurados en GameConfig
    private void SpawnOrbInLane(int lane)
    {
        if (config == null) return;

        float x = laneManager.GetLaneX(lane);
        Vector3 pos = new Vector3(x, transform.position.y, 0f);

        // Si maxEnergy no está bien configurado, usamos 100 como respaldo
        float maxE = (config.maxEnergy <= 0f) ? 100f : config.maxEnergy;

        // Si playerEnergy no existe, asumimos energía llena
        float lightE = (playerEnergy != null) ? playerEnergy.lightEnergy : maxE;
        float darkE = (playerEnergy != null) ? playerEnergy.darkEnergy : maxE;

        // Normalizamos energías a rango 0..1
        float light01 = Mathf.Clamp01(lightE / maxE);
        float dark01 = Mathf.Clamp01(darkE / maxE);

        // Déficit:
        // 1 = muy vacío
        // 0 = lleno
        float lightDeficit = 1f - light01;
        float darkDeficit = 1f - dark01;

        // Pesos para decidir qué tipo de orbe sale
        // Mientras más vacío esté un elemento, más chance tiene su orbe
        float wLight = config.orbBaseWeight + lightDeficit * config.orbBiasWeight;
        float wDark = config.orbBaseWeight + darkDeficit * config.orbBiasWeight;

        // El dual suele ser raro, por eso normalmente tiene un peso pequeño
        float wDual = config.dualOrbWeight;

        // Suma total de pesos
        float total = wLight + wDark + wDual;

        // Random para selección por ruleta
        float r = Random.value * total;

        GameObject prefab;

        if (r < wLight)
            prefab = orbLightPrefab;
        else if (r < wLight + wDark)
            prefab = orbDarkPrefab;
        else
            prefab = orbDualPrefab;

        Instantiate(prefab, pos, Quaternion.identity);
    }
}