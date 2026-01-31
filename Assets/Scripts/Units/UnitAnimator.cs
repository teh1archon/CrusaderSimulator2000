using UnityEngine;

/// <summary>
/// Handles unit animations based on unit state.
/// Works with any Animator Controller that has these parameters:
/// - IsMoving (bool): true when unit is moving
/// - IsAttacking (bool): true when unit is attacking
/// - IsReady (bool): true when unit is in ready/alert state
/// - Die (trigger): triggered on death
/// - TakeHit (trigger): triggered when taking damage
///
/// Expected states in the Animator:
/// - Idle: Default state
/// - Run: When IsMoving is true
/// - Attack: When IsAttacking is true
/// - Ready/Standing: When IsReady is true (idle but alert)
/// - Death: Triggered by Die parameter
/// </summary>
[RequireComponent(typeof(Unit))]
public class UnitAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Configuration")]
    [Tooltip("Speed threshold to consider unit as moving")]
    [SerializeField] private float moveSpeedThreshold = 0.1f;

    // Cached parameter hashes for performance
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsReadyHash = Animator.StringToHash("IsReady");
    private static readonly int DieHash = Animator.StringToHash("Die");
    private static readonly int TakeHitHash = Animator.StringToHash("TakeHit");

    private Unit unit;
    private UnityEngine.AI.NavMeshAgent agent;
    private UnitState lastState = UnitState.Idle;
    private bool hasAnimator;

    private void Awake()
    {
        unit = GetComponent<Unit>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        hasAnimator = animator != null;

        if (!hasAnimator)
        {
            Debug.LogWarning($"[UnitAnimator] {name} has no Animator component - animations disabled");
        }
    }

    private void Start()
    {
        if (unit != null)
        {
            unit.OnStateChanged += OnUnitStateChanged;
            unit.OnHealthChanged += OnHealthChanged;
            unit.OnDeath += OnUnitDeath;
        }
    }

    private void OnDestroy()
    {
        if (unit != null)
        {
            unit.OnStateChanged -= OnUnitStateChanged;
            unit.OnHealthChanged -= OnHealthChanged;
            unit.OnDeath -= OnUnitDeath;
        }
    }

    private void Update()
    {
        if (!hasAnimator) return;

        // Update movement animation based on actual velocity
        bool isMoving = agent != null && agent.velocity.sqrMagnitude > moveSpeedThreshold * moveSpeedThreshold;
        SetBool(IsMovingHash, isMoving);
    }

    private void OnUnitStateChanged(UnitState newState)
    {
        if (!hasAnimator) return;

        // Reset all state bools first
        SetBool(IsAttackingHash, false);
        SetBool(IsReadyHash, false);

        switch (newState)
        {
            case UnitState.Idle:
                // Just idle - no special parameters needed
                break;

            case UnitState.Moving:
                // IsMoving is handled in Update based on velocity
                break;

            case UnitState.Attacking:
                SetBool(IsAttackingHash, true);
                break;

            case UnitState.Defending:
                // Defending = ready stance
                SetBool(IsReadyHash, true);
                break;

            case UnitState.Retreating:
                // Retreating uses same animation as moving
                break;

            case UnitState.Dead:
                TriggerDeath();
                break;
        }

        lastState = newState;
    }

    private void OnHealthChanged(int current, int max)
    {
        if (!hasAnimator) return;

        // Trigger hit animation when taking damage (not on heal)
        // We detect damage by checking if health decreased
        // Note: This is called even on death, so check if still alive
        if (unit != null && unit.IsAlive && current < max)
        {
            // Could track previous health to detect damage vs heal
            // For now, just use the TakeHit trigger on any health change that isn't max
        }
    }

    /// <summary>
    /// Trigger the hit reaction animation
    /// </summary>
    public void TriggerHit()
    {
        if (!hasAnimator) return;
        SetTrigger(TakeHitHash);
    }

    /// <summary>
    /// Trigger the death animation
    /// </summary>
    public void TriggerDeath()
    {
        if (!hasAnimator) return;
        SetTrigger(DieHash);

        // Stop all other animations
        SetBool(IsMovingHash, false);
        SetBool(IsAttackingHash, false);
        SetBool(IsReadyHash, false);
    }

    /// <summary>
    /// Set the unit to "ready" stance (idle but alert, has enemies in sight)
    /// </summary>
    public void SetReady(bool ready)
    {
        if (!hasAnimator) return;
        SetBool(IsReadyHash, ready);
    }

    // Safe parameter setters that check if parameter exists
    private void SetBool(int hash, bool value)
    {
        if (animator != null && HasParameter(hash))
        {
            animator.SetBool(hash, value);
        }
    }

    private void SetTrigger(int hash)
    {
        if (animator != null && HasParameter(hash))
        {
            animator.SetTrigger(hash);
        }
    }

    private bool HasParameter(int hash)
    {
        // Check if animator has this parameter
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == hash)
                return true;
        }
        return false;
    }

    private void OnUnitDeath(Unit deadUnit)
    {
        TriggerDeath();
    }
}
