using UnityEngine;

public class StatsManager : MonoBehaviour
{
    // Keys
    private const string RunsKey = "STATS_RUNS";
    private const string OrbsTotalKey = "STATS_ORBS_TOTAL";
    private const string OrbsLightKey = "STATS_ORBS_LIGHT";
    private const string OrbsDarkKey = "STATS_ORBS_DARK";
    private const string OrbsDualKey = "STATS_ORBS_DUAL";
    private const string WallsTotalKey = "STATS_WALLS_TOTAL";
    private const string WallsLightKey = "STATS_WALLS_LIGHT";
    private const string WallsDarkKey = "STATS_WALLS_DARK";

    // -------------------------
    // Walls
    // -------------------------

    public static int GetWallsTotal() => PlayerPrefs.GetInt(WallsTotalKey, 0);
    public static int GetWallsLight() => PlayerPrefs.GetInt(WallsLightKey, 0);
    public static int GetWallsDark() => PlayerPrefs.GetInt(WallsDarkKey, 0);

    public static void AddWallsDestroyed(ElementType wallType, int amount)
    {
        if (amount <= 0) return;

        // Total
        PlayerPrefs.SetInt(WallsTotalKey, GetWallsTotal() + amount);

        // Por tipo de pared destruida
        if (wallType == ElementType.Light)
            PlayerPrefs.SetInt(WallsLightKey, GetWallsLight() + amount);
        else if (wallType == ElementType.Dark)
            PlayerPrefs.SetInt(WallsDarkKey, GetWallsDark() + amount);

        PlayerPrefs.Save();

        // ✅ Chequear logros si lo tienes implementado
        AchievementManager.TryCheckAll();
    }

    // -------------------------
    // Runs
    // -------------------------
    public static int GetRuns() => PlayerPrefs.GetInt(RunsKey, 0);

    public static void AddRun()
    {
        int v = GetRuns() + 1;
        PlayerPrefs.SetInt(RunsKey, v);
        PlayerPrefs.Save();

        // ✅ Chequear logros
        AchievementManager.TryCheckAll();
    }

    // -------------------------
    // Orbs
    // -------------------------
    public static int GetOrbsTotal() => PlayerPrefs.GetInt(OrbsTotalKey, 0);
    public static int GetOrbsLight() => PlayerPrefs.GetInt(OrbsLightKey, 0);
    public static int GetOrbsDark() => PlayerPrefs.GetInt(OrbsDarkKey, 0);
    public static int GetOrbsDual() => PlayerPrefs.GetInt(OrbsDualKey, 0);

    public static void AddOrb(Orb.OrbType type)
    {
        // Total
        PlayerPrefs.SetInt(OrbsTotalKey, GetOrbsTotal() + 1);

        // Por Tipo
        switch (type)
        {
            case Orb.OrbType.Light:
                PlayerPrefs.SetInt(OrbsLightKey, GetOrbsLight() + 1);
                break;
            case Orb.OrbType.Dark:
                PlayerPrefs.SetInt(OrbsDarkKey, GetOrbsDark() + 1);
                break;
            case Orb.OrbType.Dual:
                PlayerPrefs.SetInt(OrbsDualKey, GetOrbsDual() + 1);
                break;
        }

        PlayerPrefs.Save();

        // ✅ Chequear logros
        AchievementManager.TryCheckAll();
    }

    // -------------------------
    // Attacks
    // -------------------------
    private const string AttacksTotalKey = "STATS_ATTACKS_TOTAL";
    private const string AttacksLightKey = "STATS_ATTACKS_LIGHT";
    private const string AttacksDarkKey = "STATS_ATTACKS_DARK";

    public static int GetAttacksTotal() => PlayerPrefs.GetInt(AttacksTotalKey, 0);
    public static int GetAttacksLight() => PlayerPrefs.GetInt(AttacksLightKey, 0);
    public static int GetAttacksDark() => PlayerPrefs.GetInt(AttacksDarkKey, 0);

    public static void AddAttack(ElementType type)
    {
        // -------------------------
        // GLOBAL (persistente)
        // -------------------------
        PlayerPrefs.SetInt(AttacksTotalKey, GetAttacksTotal() + 1);

        if (type == ElementType.Light)
            PlayerPrefs.SetInt(AttacksLightKey, GetAttacksLight() + 1);
        else if (type == ElementType.Dark)
            PlayerPrefs.SetInt(AttacksDarkKey, GetAttacksDark() + 1);

        PlayerPrefs.Save();

        // -------------------------
        // RUN (solo esta partida)
        // -------------------------
        runAttacksTotal++;

        if (type == ElementType.Light)
            runAttacksLight++;
        else if (type == ElementType.Dark)
            runAttacksDark++;

        // ✅ Chequear logros
        AchievementManager.TryCheckAll();
    }


    // -------------------------
    // Run Stats (NO PlayerPrefs)
    // -------------------------
    // ✅ Estos valores solo viven durante la partida actual.
    // Se reinician al iniciar una nueva run (GameManager.Awake / Restart).
    private static int runAttacksTotal = 0;
    private static int runAttacksLight = 0;
    private static int runAttacksDark = 0;

    public static int GetRunAttacksTotal() => runAttacksTotal;
    public static int GetRunAttacksLight() => runAttacksLight;
    public static int GetRunAttacksDark() => runAttacksDark;

    /// ✅ Reinicia contadores de la run actual (NO afecta los stats globales en PlayerPrefs).
    /// Llamar al empezar una run nueva.
    public static void ResetRunStats()
    {
        runAttacksTotal = 0;
        runAttacksLight = 0;
        runAttacksDark = 0;
    }


    // -------------------------
    // Deaths
    // -------------------------
    private const string DeathsTotalKey = "STATS_DEATHS_TOTAL";
    private const string DeathsWallKey = "STATS_DEATHS_WALL";
    private const string DeathsObstacleKey = "STATS_DEATHS_OBSTACLE";

    // ✅ NUEVO: muertes por paredes según tipo
    private const string DeathsWallLightKey = "STATS_DEATHS_WALL_LIGHT";
    private const string DeathsWallDarkKey = "STATS_DEATHS_WALL_DARK";

    public static int GetDeathsTotal() => PlayerPrefs.GetInt(DeathsTotalKey, 0);
    public static int GetDeathsWall() => PlayerPrefs.GetInt(DeathsWallKey, 0);
    public static int GetDeathsObstacle() => PlayerPrefs.GetInt(DeathsObstacleKey, 0);

    // ✅ NUEVO getters
    public static int GetDeathsWallLight() => PlayerPrefs.GetInt(DeathsWallLightKey, 0);
    public static int GetDeathsWallDark() => PlayerPrefs.GetInt(DeathsWallDarkKey, 0);

    /// <summary>
    /// ✅ Ahora registra muerte por pared + por tipo (Luz/Oscuridad).
    /// Esto permite el logro "Entre Luces y Sombras".
    /// </summary>
    public static void AddDeathWall(ElementType wallType)
    {
        PlayerPrefs.SetInt(DeathsTotalKey, GetDeathsTotal() + 1);
        PlayerPrefs.SetInt(DeathsWallKey, GetDeathsWall() + 1);

        // Por tipo de pared
        if (wallType == ElementType.Light)
            PlayerPrefs.SetInt(DeathsWallLightKey, GetDeathsWallLight() + 1);
        else if (wallType == ElementType.Dark)
            PlayerPrefs.SetInt(DeathsWallDarkKey, GetDeathsWallDark() + 1);

        PlayerPrefs.Save();

        // ✅ Chequear logros
        AchievementManager.TryCheckAll();
    }

    public static void AddDeathObstacle()
    {
        PlayerPrefs.SetInt(DeathsTotalKey, GetDeathsTotal() + 1);
        PlayerPrefs.SetInt(DeathsObstacleKey, GetDeathsObstacle() + 1);
        PlayerPrefs.Save();

        // ✅ Chequear logros
        AchievementManager.TryCheckAll();
    }

    /// ✅ Reset rápido para pruebas.
    [ContextMenu("DEBUG/Clear Stats")]
    public static void ClearAll()
    {
        PlayerPrefs.DeleteKey(RunsKey);
        PlayerPrefs.DeleteKey(OrbsTotalKey);
        PlayerPrefs.DeleteKey(OrbsLightKey);
        PlayerPrefs.DeleteKey(OrbsDarkKey);
        PlayerPrefs.DeleteKey(OrbsDualKey);

        PlayerPrefs.DeleteKey(WallsTotalKey);
        PlayerPrefs.DeleteKey(WallsLightKey);
        PlayerPrefs.DeleteKey(WallsDarkKey);

        PlayerPrefs.DeleteKey(AttacksTotalKey);
        PlayerPrefs.DeleteKey(AttacksLightKey);
        PlayerPrefs.DeleteKey(AttacksDarkKey);

        PlayerPrefs.DeleteKey(DeathsTotalKey);
        PlayerPrefs.DeleteKey(DeathsWallKey);
        PlayerPrefs.DeleteKey(DeathsObstacleKey);

        // ✅ NUEVO: limpiar muertes por tipo de pared
        PlayerPrefs.DeleteKey(DeathsWallLightKey);
        PlayerPrefs.DeleteKey(DeathsWallDarkKey);

        PlayerPrefs.Save();
        Debug.Log("🏆 Todos los stats borrados");
    }
}
