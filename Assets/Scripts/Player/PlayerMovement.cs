using UnityEngine;

[RequireComponent(typeof(PlayerEnergy))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public LaneManager laneManager;

    [Tooltip("Arrastra aquí el componente que implemente IPlayerInput (PCInputReader, MobileInputReader, etc.).")]
    [SerializeField] private MonoBehaviour inputSource;

    [Header("Movement")]
    [Tooltip("Velocidad base (unidades por segundo) para cambiar de carril EN TU PANTALLA DE REFERENCIA. " +
             "La usaremos para calcular un tiempo base consistente por cambio de carril.")]
    public float baseMoveSpeed = 16f;

    [Tooltip("Si está activado, el cambio de carril siempre tomará el mismo TIEMPO relativo en cualquier pantalla " +
             "(porque se adapta a la distancia real entre carriles). Recomendado para consistencia entre PC/móvil.")]
    public bool useAdaptiveLaneSpeed = true;

    [Tooltip("Tiempo mínimo permitido para un lane change (evita cambios instantáneos si la pantalla es muy pequeña).")]
    public float minLaneChangeTime = 0.06f;

    [Tooltip("Tiempo máximo permitido para un lane change (evita que sea demasiado lento si la pantalla es muy grande).")]
    public float maxLaneChangeTime = 0.25f;

    [Header("Lane Change Smoothing")]
    [Tooltip("Curva para acelerar/frenar al cambiar de carril (0..1 -> 0..1).")]
    public AnimationCurve laneEase = AnimationCurve.EaseInOut(0.6f, 0.6f, 1, 1);

    private bool isLaneMoving;
    private float laneStartX;
    private float laneMoveElapsed;
    private float laneMoveDuration;


    // 0 = izquierda, 1 = centro, 2 = derecha
    public int currentLane = 1;
    private int targetLane = 1;

    private IPlayerInput input;
    private PlayerEnergy energy;
    private Animator animator;

    // Parámetros/Triggers del Animator
    private static readonly int MoveDirectionHash = Animator.StringToHash("MoveDirection");
    private static readonly int StepRightHash = Animator.StringToHash("StepRight");
    private static readonly int StepLeftHash = Animator.StringToHash("StepLeft");

    private void Awake()
    {
        energy = GetComponent<PlayerEnergy>();
        animator = GetComponent<Animator>();

        input = inputSource as IPlayerInput;
        if (input == null)
        {
            Debug.LogError("PlayerMovement: inputSource no implementa IPlayerInput. Asigna PCInputReader/MobileInputReader.");
        }
    }

    private void Start()
    {
        // Empezar centrado e idle
        SetLaneInstant(1);
        SetAnimIdle();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (input == null) return;

        HandleLaneInput();
        MoveToTargetLane();
    }

    private void HandleLaneInput()
    {
        // Lee input: -1 (izq), 0 (nada), +1 (der)
        int delta = input.GetLaneDelta();
        if (delta == 0) return;

        // Usamos targetLane para permitir cambios rápidos encadenados
        int newLane = Mathf.Clamp(targetLane + delta, 0, 2);
        if (newLane == targetLane) return;

        bool movingRight = newLane > targetLane;

        // Gasta energía según dirección (derecha = luz, izquierda = oscuridad)
        energy.SpendMove(movingRight);

        // Actualizamos el carril destino
        targetLane = newLane;

        // Reinicia animación de paso para que se vea cada input
        if (movingRight) animator.SetTrigger(StepRightHash);
        else animator.SetTrigger(StepLeftHash);

        // ✅ IMPORTANTE: reinicia el movimiento con easing desde la posición ACTUAL
        // Esto hace que si el jugador presiona rápido, el cambio se sienta fluido (sin saltos).
        laneStartX = transform.position.x;
        laneMoveElapsed = 0f;
        isLaneMoving = true;
    }


    private void MoveToTargetLane()
    {
        float targetX = laneManager.GetLaneX(targetLane);

        // Si no estamos moviéndonos y ya estamos en el carril, no hacemos nada.
        float distToTarget = Mathf.Abs(targetX - transform.position.x);
        if (!isLaneMoving && distToTarget < 0.01f) return;

        // Si estamos prácticamente ahí, snap + confirmación
        if (distToTarget < 0.01f)
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            currentLane = targetLane;
            isLaneMoving = false;
            SetAnimIdle();
            return;
        }

        // Factor de energía (tu penalización se mantiene)
        float factor = (energy != null) ? energy.MoveSpeedFactor : 1f;

        // ✅ Calculamos cuánto debe durar el cambio (laneMoveDuration)
        // Mantiene tu modo consistente entre pantallas
        if (useAdaptiveLaneSpeed)
        {
            // Spacing real entre carriles (centro y derecha)
            float lane0 = laneManager.GetLaneX(1); // centro
            float lane1 = laneManager.GetLaneX(2); // derecha
            float laneSpacing = Mathf.Abs(lane1 - lane0);

            // Tiempo base para cambiar 1 carril con baseMoveSpeed
            float baseTime = laneSpacing / Mathf.Max(0.001f, baseMoveSpeed);

            // Clamp para evitar extremos
            baseTime = Mathf.Clamp(baseTime, minLaneChangeTime, maxLaneChangeTime);

            // Penalización: si factor baja, dura más
            laneMoveDuration = baseTime / Mathf.Max(0.001f, factor);
        }
        else
        {
            // Modo clásico: convierte velocidad a duración usando la distancia actual
            float moveSpeed = baseMoveSpeed * factor;
            laneMoveDuration = distToTarget / Mathf.Max(0.001f, moveSpeed);
        }

        // Si por algún motivo no se había inicializado el tween, lo inicializamos aquí
        if (!isLaneMoving)
        {
            laneStartX = transform.position.x;
            laneMoveElapsed = 0f;
            isLaneMoving = true;
        }

        // Avanza el tiempo del tween
        laneMoveElapsed += Time.deltaTime;

        // Progreso normalizado 0..1
        float t = Mathf.Clamp01(laneMoveElapsed / Mathf.Max(0.001f, laneMoveDuration));

        // ✅ Aplicamos ease in/out usando la curva (0..1 -> 0..1)
        float easedT = laneEase.Evaluate(t);

        // Interpolamos la X suavemente
        float newX = Mathf.Lerp(laneStartX, targetX, easedT);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // Fin del movimiento
        if (t >= 1f)
        {
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
            currentLane = targetLane;
            isLaneMoving = false;
            SetAnimIdle();
        }
    }


    private void SetLaneInstant(int lane)
    {
        currentLane = targetLane = lane;
        float x = laneManager.GetLaneX(lane);
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    private void SetAnimIdle()
    {
        animator.SetInteger(MoveDirectionHash, 0);
    }
}
