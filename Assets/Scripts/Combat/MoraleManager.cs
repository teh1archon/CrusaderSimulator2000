using System;
using UnityEngine;

/// <summary>
/// Manages the shared morale gauge for the player's army.
/// Morale affects command effectiveness and unit behavior.
/// Per GDD: Single shared morale gauge displayed at top of battlefield.
/// </summary>
public class MoraleManager : MonoBehaviour
{
    public static MoraleManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int maxMorale = 100;
    [SerializeField] private int startingMorale = 75;

    [Header("Thresholds")]
    [Tooltip("Below this, units become unreliable")]
    [SerializeField] private int lowMoraleThreshold = 30;
    [Tooltip("Above this, units get bonus effectiveness")]
    [SerializeField] private int highMoraleThreshold = 80;

    [Header("Passive Changes")]
    [Tooltip("Morale lost per friendly unit death")]
    [SerializeField] private int moraleLossPerDeath = 5;
    [Tooltip("Morale gained per enemy kill")]
    [SerializeField] private int moraleGainPerKill = 2;
    [Tooltip("Morale drain rate when under pressure (per second, 0 = disabled)")]
    [SerializeField] private float pressureDrainRate = 0f;

    [Header("Current State")]
    [SerializeField] private int currentMorale;
    [SerializeField] private MoraleState currentState;

    // Events for UI and other systems
    public event Action<int, int> OnMoraleChanged;  // current, max
    public event Action<MoraleState> OnMoraleStateChanged;
    public event Action OnMoraleCollapsed;  // Morale hit 0
    public event Action OnMoraleMaxed;      // Morale hit max

    private UnitSpawner unitSpawner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize morale in Awake so UI can read it in Start
        currentMorale = startingMorale;
        UpdateMoraleState();
        OnMoraleChanged?.Invoke(currentMorale, maxMorale);
    }

    private void Start()
    {

        // Subscribe to unit events
        unitSpawner = UnitSpawner.Instance;
        if (unitSpawner == null)
            unitSpawner = FindFirstObjectByType<UnitSpawner>();

        // Note: Would need to subscribe to unit death events
        // This requires modification to UnitSpawner or a battle manager
    }

    private void Update()
    {
        // Apply pressure drain if active
        if (pressureDrainRate > 0)
        {
            // Convert to int periodically to avoid micro-changes
            float drainThisFrame = pressureDrainRate * Time.deltaTime;
            // Accumulate and apply when >= 1
            // For simplicity, just apply small changes
            if (UnityEngine.Random.value < drainThisFrame)
            {
                ChangeMorale(-1);
            }
        }
    }

    /// <summary>
    /// Change morale by amount (positive or negative)
    /// </summary>
    public void ChangeMorale(int amount)
    {
        int previousMorale = currentMorale;
        currentMorale = Mathf.Clamp(currentMorale + amount, 0, maxMorale);

        if (currentMorale != previousMorale)
        {
            OnMoraleChanged?.Invoke(currentMorale, maxMorale);
            UpdateMoraleState();

            if (currentMorale == 0 && previousMorale > 0)
            {
                OnMoraleCollapsed?.Invoke();
                Debug.Log("MORALE COLLAPSED! Army is broken!");
            }
            else if (currentMorale == maxMorale && previousMorale < maxMorale)
            {
                OnMoraleMaxed?.Invoke();
                Debug.Log("MORALE MAXED! Army is inspired!");
            }
        }
    }

    /// <summary>
    /// Set morale to specific value
    /// </summary>
    public void SetMorale(int value)
    {
        int previousMorale = currentMorale;
        currentMorale = Mathf.Clamp(value, 0, maxMorale);

        if (currentMorale != previousMorale)
        {
            OnMoraleChanged?.Invoke(currentMorale, maxMorale);
            UpdateMoraleState();
        }
    }

    /// <summary>
    /// Reset morale to starting value
    /// </summary>
    public void ResetMorale()
    {
        SetMorale(startingMorale);
    }

    /// <summary>
    /// Called when a friendly unit dies
    /// </summary>
    public void OnFriendlyDeath()
    {
        ChangeMorale(-moraleLossPerDeath);
        Debug.Log($"Friendly death! Morale -{moraleLossPerDeath} (now {currentMorale})");
    }

    /// <summary>
    /// Called when an enemy unit is killed
    /// </summary>
    public void OnEnemyKill()
    {
        ChangeMorale(moraleGainPerKill);
        Debug.Log($"Enemy killed! Morale +{moraleGainPerKill} (now {currentMorale})");
    }

    /// <summary>
    /// Set the pressure drain rate (for difficult situations)
    /// </summary>
    public void SetPressure(float drainPerSecond)
    {
        pressureDrainRate = Mathf.Max(0, drainPerSecond);
    }

    private void UpdateMoraleState()
    {
        MoraleState newState;

        if (currentMorale == 0)
            newState = MoraleState.Broken;
        else if (currentMorale <= lowMoraleThreshold)
            newState = MoraleState.Wavering;
        else if (currentMorale >= highMoraleThreshold)
            newState = MoraleState.Inspired;
        else
            newState = MoraleState.Steady;

        if (newState != currentState)
        {
            currentState = newState;
            OnMoraleStateChanged?.Invoke(currentState);
            Debug.Log($"Morale state changed to: {currentState}");
        }
    }

    /// <summary>
    /// Get effectiveness multiplier based on current morale
    /// </summary>
    public float GetMoraleMultiplier()
    {
        switch (currentState)
        {
            case MoraleState.Broken:
                return 0.25f;   // Severe penalty
            case MoraleState.Wavering:
                return 0.75f;   // Moderate penalty
            case MoraleState.Steady:
                return 1.0f;    // Normal
            case MoraleState.Inspired:
                return 1.25f;   // Bonus
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Get obedience modifier based on morale (affects command responsiveness)
    /// </summary>
    public float GetObedienceModifier()
    {
        // Low morale = units less likely to follow commands
        float normalizedMorale = (float)currentMorale / maxMorale;
        return Mathf.Lerp(0.5f, 1.2f, normalizedMorale);
    }

    // Public accessors
    public int CurrentMorale => currentMorale;
    public int MaxMorale => maxMorale;
    public float MoralePercent => (float)currentMorale / maxMorale;
    public MoraleState CurrentState => currentState;
    public bool IsLowMorale => currentMorale <= lowMoraleThreshold;
    public bool IsHighMorale => currentMorale >= highMoraleThreshold;
    public bool IsBroken => currentMorale == 0;
}

/// <summary>
/// Morale states affect unit behavior and command effectiveness
/// </summary>
public enum MoraleState
{
    Broken,     // 0 morale - army routs/surrenders
    Wavering,   // Low morale - penalties, units may disobey
    Steady,     // Normal morale - standard effectiveness
    Inspired    // High morale - bonuses, improved responsiveness
}
