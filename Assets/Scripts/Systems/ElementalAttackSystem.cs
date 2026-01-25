using UnityEngine;
using System.Collections.Generic;
public class ElementalAttackSystem : MonoBehaviour
{
    public static ElementalAttackSystem Instance { get; private set; }

    // Lista de paredes elementales activas en escena (registradas por ElementalWall)
    private readonly List<ElementalWall> activeWalls = new();

    private void Awake()
    {
        // Singleton seguro para evitar duplicados en escena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }


    /// Una pared llama esto cuando aparece en escena (OnEnable).
    /// La registramos para poder encontrarla al atacar.
    /// </summary>
    public void RegisterWall(ElementalWall wall)
    {
        // Evita duplicados
        if (wall != null && !activeWalls.Contains(wall))
            activeWalls.Add(wall);
    }


    /// Una pared llama esto cuando se desactiva / destruye (OnDisable).
    /// Así la lista se mantiene limpia.

    public void UnregisterWall(ElementalWall wall)
    {
        if (wall == null) return;
        activeWalls.Remove(wall);
    }


    /// Chequea si existe al menos una pared "objetivo" (tipo opuesto) dentro del rango yMin..yMax.
    /// Úsalo para bloquear ataques al aire (no gastar energía si no hay nada que romper).

    public bool HasTargetInRange(ElementType attackType, float yMin, float yMax)
    {
        // Determina qué tipo de pared destruye este ataque
        ElementType targetType = (attackType == ElementType.Light) ? ElementType.Dark : ElementType.Light;

        // Recorremos al revés para poder limpiar nulls si aparecen
        for (int i = activeWalls.Count - 1; i >= 0; i--)
        {
            // Protección extra por si la lista cambió en medio del loop
            if (i >= activeWalls.Count) continue;

            var wall = activeWalls[i];

            // Si hay referencias muertas, las limpiamos
            if (wall == null)
            {
                activeWalls.RemoveAt(i);
                continue;
            }

            // Chequeo de rango vertical
            float y = wall.transform.position.y;

            // Si es del tipo objetivo y está en rango, ya hay algo atacable
            if (wall.elementType == targetType && y > yMin && y < yMax)
                return true;
        }

        // No hay objetivos en rango
        return false;
    }


    /// Ejecuta el ataque y destruye todas las paredes del tipo opuesto dentro del rango.
    /// Retorna true si destruyó al menos una pared (útil para decidir si gastar energía/sonido/FX).

    public bool DoAttack(ElementType attackType, float yMin, float yMax)
    {
        // Determina qué tipo de pared destruye este ataque
        ElementType targetType = (attackType == ElementType.Light) ? ElementType.Dark : ElementType.Light;

        bool destroyedAny = false;
        int destroyedCount = 0;

        // Recorremos de atrás hacia adelante para limpiar nulls sin problemas
        for (int i = activeWalls.Count - 1; i >= 0; i--)
        {
            // Protección extra por si la lista cambió por OnDisable en medio del loop
            if (i >= activeWalls.Count) continue;

            var wall = activeWalls[i];

            // Limpieza si algo ya fue destruido
            if (wall == null)
            {
                activeWalls.RemoveAt(i);
                continue;
            }

            float y = wall.transform.position.y;

            // Si es del tipo objetivo y está dentro del rango, destruimos
            if (wall.elementType == targetType && y > yMin && y < yMax)
            {
                destroyedAny = true;
                destroyedCount++;

                // Solo destruimos. NO removemos de la lista aquí.
                // El OnDisable() del ElementalWall llamará UnregisterWall() y la quitará.
                Destroy(wall.gameObject);
            }
        }

        // ✅ Stats: contar paredes destruidas por tipo (Light/Dark) y total
        if (destroyedCount > 0)
            StatsManager.AddWallsDestroyed(targetType, destroyedCount);

        return destroyedAny;
    }


}
