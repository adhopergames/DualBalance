using UnityEngine;

[RequireComponent(typeof(PlayerEnergy))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Arrastra aquí el componente que implemente IPlayerInput (PCInputReader, MobileInputReader, etc.).")]
    [SerializeField] private MonoBehaviour inputSource; // Debe implementar IPlayerInput

    private IPlayerInput input;
    private PlayerEnergy energy;

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();

        // Convertimos el inputSource al interface común
        input = inputSource as IPlayerInput;
        if (input == null)
        {
            Debug.LogError("PlayerCombat: inputSource no implementa IPlayerInput. Asigna PCInputReader/MobileInputReader.");
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (input == null) return;

        // Ataque Luz (tap derecha / tecla K)
        if (input.GetLightAttack())
            TryAttack(ElementType.Light);

        // Ataque Oscuridad (tap izquierda / tecla J)
        if (input.GetDarkAttack())
            TryAttack(ElementType.Dark);
    }

    private void TryAttack(ElementType type)
    {
        // 1) Validar que haya energía suficiente (si no hay, no hacemos nada)
        if (!energy.CanAttack(type)) return;

        // 2) Calcular el rango del ataque basado en la posición del jugador
        //    (se calcula antes para poder saber si hay algo que romper)
        float yMin = transform.position.y - GameManager.Instance.config.attackRangeBelowPlayer;
        float yMax = transform.position.y + GameManager.Instance.config.attackRangeAbovePlayer;

        // 3) Validar que exista al menos una pared válida en rango
        //    Si no hay nada, NO gastamos energía y NO disparamos ataque.
        if (ElementalAttackSystem.Instance == null) return;

        bool hasTarget = ElementalAttackSystem.Instance.HasTargetInRange(type, yMin, yMax);
        if (!hasTarget) return;

        // 4) Ahora sí: gastar energía (porque sabemos que el ataque tendrá efecto)
        energy.SpendAttack(type);

        // 5) Ejecutar ataque global (destruye paredes en rango)
        //    (DoAttack devuelve true si destruyó algo; debería ser true si hasTarget fue true,
        //     pero lo dejamos por robustez.)
        bool destroyedAny = ElementalAttackSystem.Instance.DoAttack(type, yMin, yMax);

        // ✅ Stats: contar ataque lanzado SOLO si fue válido
        StatsManager.AddAttack(type);

        // 6) Si realmente destruyó algo, aquí disparas FX/sonido
        if (destroyedAny)
        {
            // AttackVFX.Play(type);
            // AudioManager.Instance.PlaySFX("Attack");
        }
    }
}
