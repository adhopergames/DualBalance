using System.Collections.Generic;
using UnityEngine;

public class AchievementsPanelUI : MonoBehaviour
{
    [Header("List")]
    [Tooltip("Content del ScrollView (donde van los items instanciados)")]
    public Transform contentParent;

    [Tooltip("Prefab del item (tiene AchievementItemUI + LocalizeStringEvent en título/desc)")]
    public AchievementItemUI itemPrefab;

    [Header("Icons (optional)")]
    public List<AchievementIcon> icons = new();

    [System.Serializable]
    public struct AchievementIcon
    {
        public string id;
        public Sprite icon;
    }

    // Mapa id -> sprite para buscar iconos rápido
    private readonly Dictionary<string, Sprite> iconMap = new();

    // Lista de instancias creadas (por si quieres destruir/rebuild)
    private readonly List<AchievementItemUI> spawned = new();

    // ✅ NUEVO: mapa id -> item instanciado para refrescar solo 1 cuando se desbloquea
    private readonly Dictionary<string, AchievementItemUI> spawnedById = new();

    private void Awake()
    {
        // Construimos el mapa de iconos una vez
        iconMap.Clear();
        foreach (var e in icons)
        {
            if (!string.IsNullOrEmpty(e.id) && e.icon != null && !iconMap.ContainsKey(e.id))
                iconMap.Add(e.id, e.icon);
        }
    }

    private void OnEnable()
    {
        BuildList();

        // Nos suscribimos al evento para refrescar cuando se desbloquee un logro
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked += HandleUnlocked;
    }

    private void OnDisable()
    {
        // Quitamos la suscripción para evitar leaks/doble llamada
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.OnAchievementUnlocked -= HandleUnlocked;
    }

    private void BuildList()
    {
        // -------- LIMPIEZA --------
        foreach (var it in spawned)
            if (it != null) Destroy(it.gameObject);

        spawned.Clear();
        spawnedById.Clear();

        // -------- OBTENER LISTA COMPLETA --------
        // OJO: esto requiere que tú tengas un GetAll() público (te lo explico abajo)
        var all = AchievementManager.GetAll();

        // -------- CREAR ITEMS --------
        foreach (var a in all)
        {
            // Instanciamos el prefab dentro del content
            var item = Instantiate(itemPrefab, contentParent);

            // Buscamos icono por id (si no existe, queda null)
            Sprite icon = iconMap.TryGetValue(a.id, out var s) ? s : null;

            // ✅ NUEVO Init: SOLO id + icono
            item.Init(a.id, icon);

            spawned.Add(item);

            // ✅ Guardamos referencia por id para refrescar solo el que toca
            if (!spawnedById.ContainsKey(a.id))
                spawnedById.Add(a.id, item);
        }
    }

    private void HandleUnlocked(string id, string title, string desc)
    {
        // Ahora title/desc NO los usamos, porque el texto sale del localization table.
        // El evento te los manda igual, pero los ignoramos.

        // ✅ Refresca SOLO el item desbloqueado
        if (spawnedById.TryGetValue(id, out var item) && item != null)
        {
            item.Refresh(); // cambia de ACH_LOCKED_* a ACH_<id>_* y actualiza icono/tint
        }
        else
        {
            // Fallback: si por alguna razón no lo encontramos, refrescamos todo
            foreach (var it in spawned)
                if (it != null) it.Refresh();
        }
    }
}
