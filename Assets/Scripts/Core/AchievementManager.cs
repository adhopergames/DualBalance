using System;
using System.Collections.Generic;
using UnityEngine;

/// AchievementManager:
/// - Define la lista de logros (15 aprox)
/// - Guarda desbloqueados en PlayerPrefs
/// - Chequea condiciones cuando cambian stats / best score
/// - Dispara evento OnAchievementUnlocked para UI (popup)

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    // Evento para UI:
    // (id, title, description)
    public event Action<string, string, string> OnAchievementUnlocked;

    // Prefix para keys en PlayerPrefs
    private const string AchKeyPrefix = "ACH_";

    // Estructura interna de un logro
    private class Achievement
    {
        public string id;
        public string title;
        public string desc;
        public Func<bool> condition;
    }

    private readonly List<Achievement> achievements = new();

    private void Awake()
    {
        // -------------------------
        // Singleton + Persistencia
        // -------------------------
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // -------------------------
        // Definimos logros
        // -------------------------
        BuildAchievements();
    }

    /// Llamado desde StatsManager/SaveManager para chequear logros sin acoplar.

    public static void TryCheckAll()
    {
        if (Instance == null) return;
        Instance.CheckAll();
    }

    /// Devuelve true si un logro ya está desbloqueado.
       
    public static bool IsUnlocked(string id)
    {
        return PlayerPrefs.GetInt(AchKeyPrefix + id, 0) == 1;
    }

    /// Cantidad total de logros desbloqueados (para meta-logros).
 
    public static int GetUnlockedCount()
    {
        if (Instance == null) return 0;

        int count = 0;
        foreach (var a in Instance.achievements)
        {
            if(IsUnlocked(a.id)) count++;
        }

        return count;
    }

    /// Chequea TODOS los logros.
    /// Importante: lo hacemos en loop por si desbloquear uno provoca el meta-logro.
    
    private void CheckAll()
    {
        // Evita que el meta-logro se quede atrás:
        // repetimos hasta que no haya cambios.
        bool unlockedSomething;

        do
        {
            unlockedSomething = false;

            foreach (var a in achievements)
            {
                if (IsUnlocked(a.id)) continue;
                if (!a.condition()) continue;

                Unlock(a);
                unlockedSomething = true;
            }
        } while (unlockedSomething);
    }

    /// Marca el logro como desbloqueado, guarda y dispara evento para UI.
    
    private void Unlock(Achievement a)
    {
        PlayerPrefs.SetInt(AchKeyPrefix + a.id, 1);
        PlayerPrefs.Save();

        Debug.Log($"🏆 ACH UNLOCKED => {a.title} ({a.id})");

        OnAchievementUnlocked?.Invoke(a.id, a.title, a.desc);
    }

    /// Define la lista de ~15 logros.

    private void BuildAchievements()
    {
        achievements.Clear();

        // Helpers para leer stats cómodamente
        int Best() => SaveManager.GetBestScore();

        // -------------------------
        // TEMÁTICOS
        // -------------------------
        achievements.Add(new Achievement
        {
            id = "between_light_shadow",
            title = "Entre Luces y Sombras",
            desc = "Choca al menos una vez con una pared de Luz y una de Oscuridad.",
            condition = () => StatsManager.GetDeathsWallLight() >= 1 && StatsManager.GetDeathsWallDark() >= 1
        });

        achievements.Add(new Achievement
        {
            id = "furia_elemental",
            title = "Furia Elemental",
            desc = "Usa 16 ataques de Luz y 16 de Oscuridad.",
            condition = () => StatsManager.GetAttacksLight() >= 16 && StatsManager.GetAttacksDark() >= 16
        });

        // -------------------------
        // COMBATE
        // -------------------------
        achievements.Add(new Achievement
        {
            id = "first_attack",
            title = "Primer Ataque",
            desc = "Lanza tu primer ataque.",
            condition = () => StatsManager.GetAttacksTotal() >= 1
        });

        achievements.Add(new Achievement
        {
            id = "light_specialist",
            title = "Especialista Lumínico",
            desc = "Lanza 30 ataques de Luz.",
            condition = () => StatsManager.GetAttacksLight() >= 30
        });

        achievements.Add(new Achievement
        {
            id = "dark_mastery",
            title = "Dominio Oscuro",
            desc = "Lanza 30 ataques de Oscuridad.",
            condition = () => StatsManager.GetAttacksDark() >= 30
        });

        achievements.Add(new Achievement
        {
            id = "perfect_balance",
            title = "Equilibrio Perfecto",
            desc = "Lanza al menos 20 ataques de Luz y 20 de Oscuridad (casi igualados).",
            condition = () =>
            {
                int l = StatsManager.GetAttacksLight();
                int d = StatsManager.GetAttacksDark();
                if (l < 20 || d < 20) return false;
                return Mathf.Abs(l - d) <= 1;
            }
        });

        // -------------------------
        // MUERTES / APRENDIZAJE
        // -------------------------
        achievements.Add(new Achievement
        {
            id = "first_death",
            title = "Golpe Duro",
            desc = "Muere por primera vez.",
            condition = () => StatsManager.GetDeathsTotal() >= 1
        });

        achievements.Add(new Achievement
        {
            id = "learning_to_fly",
            title = "Aprendiendo a Volar",
            desc = "Muere 5 veces.",
            condition = () => StatsManager.GetDeathsTotal() >= 5
        });

        achievements.Add(new Achievement
        {
            id = "walls_do_not_forgive",
            title = "Las Paredes No Perdonan",
            desc = "Muere 5 veces por paredes elementales.",
            condition = () => StatsManager.GetDeathsWall() >= 5
        });

        achievements.Add(new Achievement
        {
            id = "fate_obstacles",
            title = "Obstáculos del Destino",
            desc = "Muere 5 veces por obstáculos neutrales.",
            condition = () => StatsManager.GetDeathsObstacle() >= 5
        });

        // -------------------------
        // SCORE (BEST SCORE)
        // -------------------------
        achievements.Add(new Achievement
        {
            id = "long_first_flight",
            title = "Primer Vuelo Largo",
            desc = "Alcanza 1000 de score (Best).",
            condition = () => Best() >= 1000
        });

        achievements.Add(new Achievement
        {
            id = "max_speed",
            title = "Velocidad Máxima",
            desc = "Alcanza 3000 de score (Best).",
            condition = () => Best() >= 3000
        });

        achievements.Add(new Achievement
        {
            id = "beyond_balance",
            title = "Más Allá del Equilibrio",
            desc = "Alcanza 5000 de score (Best).",
            condition = () => Best() >= 5000
        });

        // -------------------------
        // MAESTRÍA
        // -------------------------
        achievements.Add(new Achievement
        {
            id = "unstoppable",
            title = "Imparable",
            desc = "Destruye 50 paredes en total.",
            condition = () => StatsManager.GetWallsTotal() >= 50
        });

        achievements.Add(new Achievement
        {
            id = "master_of_dualbalance",
            title = "Maestro del DualBalance",
            desc = "Desbloquea 10 logros.",
            condition = () => GetUnlockedCount() >= 10
        });
    }
    public static List<AchievementInfo> GetAll()
    {
        if (Instance == null) return new List<AchievementInfo>();

        var list = new List<AchievementInfo>(Instance.achievements.Count);
        foreach (var a in Instance.achievements)
        {
            list.Add(new AchievementInfo(a.id, a.title, a.desc));
        }
        return list;
    }


    [ContextMenu("DEBUG/Clear Achievements")]
    public void ClearAllAchievements()
    {
        foreach (var a in achievements)
        {
            PlayerPrefs.DeleteKey("ACH_" + a.id);
        }

        PlayerPrefs.Save();
        Debug.Log("🏆 Todos los logros borrados");
    }


}

[Serializable]
public struct AchievementInfo
{
    public string id;
    public string title;
    public string desc;

    public AchievementInfo(string id, string title, string desc)
    {
        this.id = id;
        this.title = title;
        this.desc = desc;
    }
}
