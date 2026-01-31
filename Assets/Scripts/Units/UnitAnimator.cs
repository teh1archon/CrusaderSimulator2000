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
    private bool hasAnimator;

    // Cache which parameters exist to avoid checking every frame
    private bool hasIsMoving;
    private bool hasIsAttacking;
    private bool hasIsReady;
    private bool hasDie;
    private bool hasTakeHit;

    // Track last values to only update on change
    private bool lastIsMoving;
    private bool lastIsAttacking;
    private bool lastIsReady;

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
            return;
        }

        // Cache parameter existence once
        CacheParameters();
    }

    private void CacheParameters()
    {
        foreach (var param in animator.parameters)
        {
            if (param.nameHash == IsMovingHash) hasIsMoving = true;
            else if (param.nameHash == IsAttackingHash) hasIsAttacking = true;
            else if (param.nameHash == IsReadyHash) hasIsReady = true;
            else if (param.nameHash == DieHash) hasDie = true;
            else if (param.nameHash == TakeHitHash) hasTakeHit = true;
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
        if (!hasAnimator || !hasIsMoving) return;

        // Check if moving based on actual velocity
        bool isMoving = agent != null && agent.velocity.sqrMagnitude > moveSpeedThreshold * moveSpeedThreshold;

        // Only update animator if value changed
        if (isMoving != lastIsMoving)
        {
            lastIsMoving = isMoving;
            animator.SetBool(IsMovingHash, isMoving);
        }
    }

    private void OnUnitStateChanged(UnitState newState)
    {
        if (!hasAnimator) return;

        // Determine new values
        bool newIsAttacking = newState == UnitState.Attacking;
        bool newIsReady = newState == UnitState.Defending;

        // Only update if changed
        if (hasIsAttacking && newIsAttacking != lastIsAttacking)
        {
            lastIsAttacking = newIsAttacking;
            animator.SetBool(IsAttackingHash, newIsAttacking);
        }

        if (hasIsReady && newIsReady != lastIsReady)
        {
            lastIsReady = newIsReady;
            animator.SetBool(IsReadyHash, newIsReady);
        }

        // Handle death
        if (newState == UnitState.Dead)
        {
            TriggerDeath();
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        // Could trigger hit animation here if tracking previous health
    }

    /// <summary>
    /// Trigger the hit reaction animation
    /// </summary>
    public void TriggerHit()
    {
        if (!hasAnimator || !hasTakeHit) return;
        animator.SetTrigger(TakeHitHash);
    }

    /// <summary>
    /// Trigger the death animation
    /// </summary>
    public void TriggerDeath()
    {
        if (!hasAnimator) return;

        if (hasDie)
            animator.SetTrigger(DieHash);

        // Force stop movement animation
        if (hasIsMoving)
        {
            lastIsMoving = false;
            animator.SetBool(IsMovingHash, false);
        }
        if (hasIsAttacking)
        {
            lastIsAttacking = false;
            animator.SetBool(IsAttackingHash, false);
        }
        if (hasIsReady)
        {
            lastIsReady = false;
            animator.SetBool(IsReadyHash, false);
        }
    }

    /// <summary>
    /// Set the unit to "ready" stance (idle but alert, has enemies in sight)
    /// </summary>
    public void SetReady(bool ready)
    {
        if (!hasAnimator || !hasIsReady) return;
        if (ready != lastIsReady)
        {
            lastIsReady = ready;
            animator.SetBool(IsReadyHash, ready);
        }
    }

    private void OnUnitDeath(Unit deadUnit)
    {
        TriggerDeath();
    }
}
