using UnityEngine;

[CreateAssetMenu(menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("World Speed / Difficulty")]
    [Tooltip("Velocidad base con la que bajan los objetos.")]
    public float baseWorldSpeed = 5f;

    [Tooltip("Cuánto aumenta la velocidad por segundo (dificultad).")]
    public float difficultyRamp = 0.15f;

    [Header("World Speed Cap")]
    [Tooltip("Velocidad máxima del mundo (hard cap).")]
    public float maxWorldSpeed = 8f;

    [Header("Spawn")]
    [Tooltip("Intervalo base entre spawns (antes de aplicar dificultad).")]
    public float baseSpawnInterval = 1.2f;

    [Tooltip("Mientras más avance el juego, más rápido spawnea (reduce intervalo).")]
    public float spawnIntervalRamp = 0.01f;

    [Tooltip("Límite mínimo de intervalo entre spawns.")]
    public float minSpawnInterval = 0.45f;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float idleDrainPerSecond = 0.10f; // gasto pasivo por volar
    public float moveDrain = 3f;             // gasto al cambiar de carril
    public float attackDrain = 8f;           // gasto al atacar

    [Header("Orbs")]
    public float orbAmount = 15f;
    public float dualOrbAmount = 10f;

    [Header("Attack Range (relative to player)")]
    [Tooltip("Cuánto hacia abajo del jugador afecta el ataque.")]
    public float attackRangeBelowPlayer = 1f;

    [Tooltip("Cuánto hacia arriba del jugador afecta el ataque.")]
    public float attackRangeAbovePlayer = 8f;

    [Header("Spawn Chances (Difficulty-based)")]
    [Tooltip("Chance de pared elemental al inicio (0..1).")]
    [Range(0f, 1f)] public float elementalWallChanceStart = 0.10f;

    [Tooltip("Chance de pared elemental a máxima dificultad (0..1).")]
    [Range(0f, 1f)] public float elementalWallChanceEnd = 0.28f;

    [Tooltip("Chance total de spawnear orbe (0..1). Lo demás será neutral/obstáculo.")]
    [Range(0f, 1f)] public float orbChance = 0.10f;

    [Header("Orb Balance Weights")]
    [Tooltip("Peso base de orbes Light/Dark. Mayor => más frecuente.")]
    public float orbBaseWeight = 1f;

    [Tooltip("Cuánto se sesga hacia el elemento más vacío. Mayor => ayuda más al balance.")]
    public float orbBiasWeight = 1.2f;

    [Tooltip("Peso del orbe dual. Debe ser menor que los normales para que sea raro.")]
    public float dualOrbWeight = 0.25f;

}
