using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Main unit component that manages runtime state.
/// Units act AUTONOMOUSLY - player influences them via commands, not direct control.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class Unit : MonoBehaviour
{
    [Header("Class Data")]
    [SerializeField] private UnitClassData classData;

    [Header("Runtime Stats")]
    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentXP;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime;

    [Header("State")]
    [SerializeField] private UnitState currentState = UnitState.Idle;
    [SerializeField] private Unit currentTarget;
    [SerializeField] private bool isPlayerUnit = true;

    // Components
    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;

    // Buff modifiers (applied by commands)
    private float damageMultiplier = 1f;
    private float speedMultiplier = 1f;
    private float defenseMultiplier = 1f;

    // Events
    public event Action<Unit> OnDeath;
    public event Action<int, int> OnHealthChanged;  // current, max
    public event Action<UnitState> OnStateChanged;

    // Properties
    public UnitClassData ClassData => classData;
    public string ClassName => classData != null ? classData.className : "Unknown";
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentXP => currentXP;
    public bool IsAlive => currentHealth > 0;
    public bool IsPlayerUnit => isPlayerUnit;
    public UnitState CurrentState => currentState;
    public NavMeshAgent Agent => agent;

    // Effective stats (base + modifiers)
    public float EffectiveStrength => classData != null ? classData.strength * damageMultiplier : 10;
    public float EffectiveDexterity => classData != null ? classData.dexterity * speedMultiplier : 10;
    public float EffectiveEndurance => classData != null ? classData.endurance * defenseMultiplier : 10;
    public float EffectiveObedience => classData != null ? classData.obedience : 10;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Configure NavMeshAgent for 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Start()
    {
        InitializeFromClassData();
    }

    private void Update()
    {
        if (!IsAlive) return;

        // Autonomous behavior based on state
        switch (currentState)
        {
            case UnitState.Idle:
                SearchForTarget();
                break;
            case UnitState.Moving:
                UpdateMovement();
                break;
            case UnitState.Attacking:
                UpdateAttack();
                break;
            case UnitState.Defending:
                UpdateDefense();
                break;
        }

        // Flip sprite based on movement direction
        UpdateSpriteDirection();
    }

    /// <summary>
    /// Initialize unit stats from class data
    /// </summary>
    public void InitializeFromClassData()
    {
        if (classData == null)
        {
            Debug.LogWarning($"Unit {name} has no ClassData assigned!");
            maxHealth = 100;
            currentHealth = maxHealth;
            return;
        }

        // Calculate max health from endurance
        maxHealth = classData.endurance * 10;
        currentHealth = maxHealth;

        // Set movement speed from dexterity
        if (agent != null)
        {
            agent.speed = 2f + (classData.dexterity * 0.05f);
        }

        // Set sprite if available
        if (spriteRenderer != null && classData.unitSprite != null)
        {
            spriteRenderer.sprite = classData.unitSprite;
        }
    }

    /// <summary>
    /// Initialize unit with specific class data (legacy API for dynamic creation)
    /// </summary>
    public void Initialize(UnitClassData data, bool playerUnit)
    {
        classData = data;
        isPlayerUnit = playerUnit;
        InitializeFromClassData();
    }

    /// <summary>
    /// Initialize unit using the classData already assigned in the prefab
    /// </summary>
    public void Initialize(bool playerUnit)
    {
        isPlayerUnit = playerUnit;
        InitializeFromClassData();
    }

    /// <summary>
    /// Reset unit state for object pooling reuse
    /// </summary>
    public void ResetForPooling()
    {
        // Reset runtime state
        currentState = UnitState.Idle;
        currentTarget = null;
        lastAttackTime = 0f;

        // Reset buffs
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        defenseMultiplier = 1f;

        // Re-enable agent
        if (agent != null)
        {
            agent.enabled = true;
            agent.ResetPath();
        }

        // Will be fully initialized when spawned
    }

    #region Autonomous Behavior

    private void SearchForTarget()
    {
        // Find nearest enemy
        Unit nearestEnemy = FindNearestEnemy();
        if (nearestEnemy != null)
        {
            currentTarget = nearestEnemy;
            SetState(UnitState.Moving);
        }
    }

    private void UpdateMovement()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
        {
            currentTarget = null;
            SetState(UnitState.Idle);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);

        if (distanceToTarget <= attackRange)
        {
            agent.ResetPath();
            SetState(UnitState.Attacking);
        }
        else
        {
            agent.SetDestination(currentTarget.transform.position);
        }
    }

    private void UpdateAttack()
    {
        if (currentTarget == null || !currentTarget.IsAlive)
        {
            currentTarget = null;
            SetState(UnitState.Idle);
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.transform.position);

        // If target moved out of range, chase them
        if (distanceToTarget > attackRange * 1.2f)
        {
            SetState(UnitState.Moving);
            return;
        }

        // Attack if cooldown ready
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
        }
    }

    private void UpdateDefense()
    {
        // In defense mode, don't chase - only attack if enemy comes close
        Unit nearbyEnemy = FindNearestEnemy();
        if (nearbyEnemy != null)
        {
            float distance = Vector2.Distance(transform.position, nearbyEnemy.transform.position);
            if (distance <= attackRange)
            {
                currentTarget = nearbyEnemy;
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack();
                }
            }
        }
    }

    private void PerformAttack()
    {
        if (currentTarget == null) return;

        // Calculate damage
        float baseDamage = EffectiveStrength;
        bool isMelee = true; // TODO: determine from weapon
        float damage = classData != null ? classData.CalculateDamage(isMelee) * damageMultiplier : baseDamage;

        currentTarget.TakeDamage(Mathf.RoundToInt(damage), this);
        lastAttackTime = Time.time;

        // TODO: Play attack animation/effect
    }

    private Unit FindNearestEnemy()
    {
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        Unit nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == this) continue;
            if (unit.isPlayerUnit == this.isPlayerUnit) continue; // Same team
            if (!unit.IsAlive) continue;

            float dist = Vector2.Distance(transform.position, unit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = unit;
            }
        }

        return nearest;
    }

    #endregion

    #region Combat

    public void TakeDamage(int damage, Unit attacker = null)
    {
        // Apply defense
        float reducedDamage = damage / (1f + EffectiveEndurance * 0.01f);
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(reducedDamage));

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        SetState(UnitState.Dead);
        if (agent != null)
            agent.enabled = false;

        // Notify listeners (UnitSpawner will handle cleanup/pooling)
        OnDeath?.Invoke(this);

        // Note: Don't destroy - UnitSpawner handles pooling after death animation delay
    }

    #endregion

    #region Command Effects

    /// <summary>
    /// Apply buff from a command (called by CommandExecutor)
    /// </summary>
    public void ApplyBuff(BuffType type, float multiplier, float duration)
    {
        switch (type)
        {
            case BuffType.Damage:
                damageMultiplier = multiplier;
                break;
            case BuffType.Speed:
                speedMultiplier = multiplier;
                if (agent != null)
                    agent.speed = (2f + (classData?.dexterity ?? 10) * 0.05f) * speedMultiplier;
                break;
            case BuffType.Defense:
                defenseMultiplier = multiplier;
                break;
        }

        // Reset after duration
        if (duration > 0)
        {
            Invoke(nameof(ResetBuffs), duration);
        }
    }

    /// <summary>
    /// Force unit into a specific state (from commands)
    /// Effectiveness based on obedience stat
    /// </summary>
    public void CommandState(UnitState newState, float commandStrength)
    {
        // Higher obedience = more likely to follow command
        float obedienceRoll = UnityEngine.Random.value * 100f;
        if (obedienceRoll <= EffectiveObedience * commandStrength)
        {
            SetState(newState);
        }
    }

    private void ResetBuffs()
    {
        damageMultiplier = 1f;
        speedMultiplier = 1f;
        defenseMultiplier = 1f;

        if (agent != null && classData != null)
        {
            agent.speed = 2f + (classData.dexterity * 0.05f);
        }
    }

    #endregion

    #region XP & Progression

    public void AddXP(int amount)
    {
        currentXP += amount;
        // TODO: Check for level up / class change eligibility
    }

    #endregion

    #region Helpers

    private void SetState(UnitState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    private void UpdateSpriteDirection()
    {
        if (spriteRenderer == null || agent == null) return;

        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            spriteRenderer.flipX = agent.velocity.x < 0;
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw line to target
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }
}

public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Defending,
    Retreating,
    Dead
}

public enum BuffType
{
    Damage,
    Speed,
    Defense
}
