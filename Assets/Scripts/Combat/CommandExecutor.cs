using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Executes commands based on rhythm performance.
/// Bridges the rhythm system and unit effects.
/// </summary>
public class CommandExecutor : MonoBehaviour
{
    public static CommandExecutor Instance { get; private set; }

    [Header("Available Commands")]
    [Tooltip("Commands the player can execute in this battle")]
    [SerializeField] private List<CommandData> availableCommands = new List<CommandData>();

    [Header("Current State")]
    [SerializeField] private CommandData activeCommand;
    [SerializeField] private bool isExecutingCommand;

    [Header("References")]
    [SerializeField] private RhythmGameManager rhythmManager;
    [SerializeField] private MoraleManager moraleManager;
    [SerializeField] private UnitSpawner unitSpawner;

    // Events for UI
    public event Action<CommandData> OnCommandStarted;
    public event Action<CommandData, float, CommandResult> OnCommandCompleted;
    public event Action<CommandData> OnCommandInterrupted;
    public event Action<List<CommandData>> OnAvailableCommandsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find references if not set
        if (rhythmManager == null)
            rhythmManager = RhythmGameManager.Instance;
        if (moraleManager == null)
            moraleManager = MoraleManager.Instance;
        if (unitSpawner == null)
            unitSpawner = UnitSpawner.Instance;

        // Subscribe to rhythm events
        if (rhythmManager != null)
        {
            rhythmManager.OnMelodyCompleted += HandleMelodyCompleted;
            rhythmManager.OnMelodyInterrupted += HandleMelodyInterrupted;
            Debug.Log("[CommandExecutor] Subscribed to RhythmGameManager events");
        }
        else
        {
            Debug.LogError("[CommandExecutor] RhythmGameManager.Instance is null - cannot subscribe to events!");
        }
    }

    private void OnDestroy()
    {
        if (rhythmManager != null)
        {
            rhythmManager.OnMelodyCompleted -= HandleMelodyCompleted;
            rhythmManager.OnMelodyInterrupted -= HandleMelodyInterrupted;
        }
    }

    /// <summary>
    /// Set the available commands for this battle
    /// </summary>
    public void SetAvailableCommands(List<CommandData> commands)
    {
        availableCommands = new List<CommandData>(commands);
        OnAvailableCommandsChanged?.Invoke(availableCommands);
    }

    /// <summary>
    /// Add a command to available list
    /// </summary>
    public void AddCommand(CommandData command)
    {
        if (command != null && !availableCommands.Contains(command))
        {
            availableCommands.Add(command);
            OnAvailableCommandsChanged?.Invoke(availableCommands);
        }
    }

    /// <summary>
    /// Start executing a command (begins the melody)
    /// Per GDD: Switching during countdown = no penalty, switching during actual melody = penalty
    /// </summary>
    public bool StartCommand(CommandData command)
    {
        if (command == null || command.melody == null)
        {
            Debug.LogError("Cannot start command: null command or melody");
            return false;
        }

        // If already executing, check if we should penalize
        if (isExecutingCommand && activeCommand != null && rhythmManager != null)
        {
            // Only penalize if melody was actually playing (not in countdown)
            if (rhythmManager.IsPlaying)
            {
                // Apply morale penalty for interrupting during actual playback
                if (moraleManager != null)
                {
                    moraleManager.ChangeMorale(-activeCommand.moraleLossOnFail);
                }
                OnCommandInterrupted?.Invoke(activeCommand);
                Debug.Log($"Switched command during melody - morale penalty applied");
            }
            else if (rhythmManager.IsCountingDown)
            {
                // Was in countdown - no penalty, just switch
                Debug.Log($"Switched command during countdown - no penalty");
            }
        }

        activeCommand = command;
        isExecutingCommand = true;

        // Start the melody in rhythm manager
        rhythmManager.StartMelody(command.melody);

        OnCommandStarted?.Invoke(command);
        Debug.Log($"Started command: {command.commandName}");
        return true;
    }

    /// <summary>
    /// Start command by index in available list
    /// </summary>
    public bool StartCommand(int index)
    {
        if (index < 0 || index >= availableCommands.Count)
        {
            Debug.LogWarning($"Invalid command index: {index}");
            return false;
        }
        return StartCommand(availableCommands[index]);
    }

    /// <summary>
    /// Interrupt the current command (player cancelled or switched)
    /// </summary>
    public void InterruptCommand()
    {
        if (!isExecutingCommand) return;

        rhythmManager.InterruptMelody();
        // HandleMelodyInterrupted will be called by the event
    }

    private void HandleMelodyCompleted(int rawScore)
    {
        Debug.Log($"[CommandExecutor] HandleMelodyCompleted called with score {rawScore}, isExecuting={isExecutingCommand}, activeCommand={activeCommand?.commandName ?? "null"}");

        if (!isExecutingCommand || activeCommand == null)
        {
            Debug.LogWarning("[CommandExecutor] Not executing or no active command - ignoring melody completion");
            return;
        }

        // Convert raw score to percentage (0-100)
        float scorePercent = CalculateScorePercent(rawScore);

        // Calculate effect strength
        float effectStrength = activeCommand.CalculateEffectStrength(scorePercent);

        // Determine result
        CommandResult result;
        if (effectStrength <= 0)
            result = CommandResult.Failed;
        else if (scorePercent >= activeCommand.perfectScoreThreshold)
            result = CommandResult.Perfect;
        else if (scorePercent >= 50)
            result = CommandResult.Good;
        else
            result = CommandResult.Weak;

        // Apply morale change
        if (moraleManager != null)
        {
            int moraleChange = activeCommand.CalculateMoraleChange(scorePercent);
            moraleManager.ChangeMorale(moraleChange);
        }

        // Execute the effect if successful
        if (effectStrength > 0)
        {
            ExecuteEffect(activeCommand, effectStrength);
        }

        OnCommandCompleted?.Invoke(activeCommand, scorePercent, result);

        Debug.Log($"Command '{activeCommand.commandName}' completed: Score={scorePercent:F1}%, Strength={effectStrength:F2}x, Result={result}");

        isExecutingCommand = false;
        activeCommand = null;
    }

    private void HandleMelodyInterrupted()
    {
        if (!isExecutingCommand || activeCommand == null) return;

        // Morale penalty for interruption
        if (moraleManager != null)
        {
            moraleManager.ChangeMorale(-activeCommand.moraleLossOnFail);
        }

        OnCommandInterrupted?.Invoke(activeCommand);

        Debug.Log($"Command '{activeCommand.commandName}' interrupted! Morale penalty applied.");

        isExecutingCommand = false;
        activeCommand = null;
    }

    /// <summary>
    /// Convert raw rhythm score to percentage (0-100)
    /// </summary>
    private float CalculateScorePercent(int rawScore)
    {
        if (activeCommand?.melody == null) return 0f;

        // Max possible score = all notes hit perfectly = noteCount * 5
        int noteCount = activeCommand.melody.notes.Count;
        int maxScore = noteCount * TimingJudge.PERFECT_POINTS;

        if (maxScore <= 0) return 0f;

        // Clamp to 0-100 range
        float percent = (float)rawScore / maxScore * 100f;
        return Mathf.Clamp(percent, 0f, 100f);
    }

    /// <summary>
    /// Apply the command effect to units
    /// </summary>
    private void ExecuteEffect(CommandData command, float strength)
    {
        if (unitSpawner == null)
        {
            unitSpawner = UnitSpawner.Instance;
            if (unitSpawner == null)
            {
                Debug.LogError("[CommandExecutor] UnitSpawner.Instance is null - cannot apply effects!");
                return;
            }
        }

        List<Unit> targets = GetTargetUnits(command);

        if (targets.Count == 0)
        {
            Debug.LogWarning($"[CommandExecutor] No valid targets for command {command.commandName}");
            return;
        }

        Debug.Log($"[CommandExecutor] Applying {command.effectType} to {targets.Count} units at {strength:F2}x strength");

        foreach (var unit in targets)
        {
            ApplyEffectToUnit(unit, command, strength);
        }
    }

    private List<Unit> GetTargetUnits(CommandData command)
    {
        if (unitSpawner == null) return new List<Unit>();

        switch (command.targetType)
        {
            case CommandTarget.AllPlayerUnits:
                return unitSpawner.GetPlayerUnits();

            case CommandTarget.NearbyUnits:
                // For now, treat as all units (would need commander position for area targeting)
                return unitSpawner.GetPlayerUnits();

            default:
                return unitSpawner.GetPlayerUnits();
        }
    }

    private void ApplyEffectToUnit(Unit unit, CommandData command, float strength)
    {
        if (unit == null || !unit.IsAlive) return;

        float duration = command.duration;
        float multiplier = strength;

        switch (command.effectType)
        {
            case CommandEffect.Attack:
                // Aggressive mode - damage boost, seek enemies
                unit.ApplyBuff(BuffType.Damage, multiplier, duration);
                unit.EngageNearestEnemy();  // Trigger engagement with nearest enemy
                break;

            case CommandEffect.Charge:
                // Rush with damage and speed boost
                unit.ApplyBuff(BuffType.Damage, multiplier * 1.3f, duration);
                unit.ApplyBuff(BuffType.Speed, multiplier * 1.5f, duration);
                unit.EngageNearestEnemy();  // Trigger engagement with nearest enemy
                break;

            case CommandEffect.Defend:
                // Hold position with defense boost
                unit.ApplyBuff(BuffType.Defense, multiplier * 1.5f, duration);
                unit.CommandState(UnitState.Defending, strength);
                break;

            case CommandEffect.Retreat:
                // Fall back (would need rally point implementation)
                unit.ApplyBuff(BuffType.Speed, multiplier * 1.3f, duration);
                unit.CommandState(UnitState.Retreating, strength);
                break;

            case CommandEffect.Rally:
                // Morale is handled separately, give small all-around buff
                unit.ApplyBuff(BuffType.Damage, 1f + (multiplier - 1f) * 0.5f, duration);
                unit.ApplyBuff(BuffType.Defense, 1f + (multiplier - 1f) * 0.5f, duration);
                break;

            case CommandEffect.Inspire:
                // Pure damage buff
                unit.ApplyBuff(BuffType.Damage, multiplier * 1.5f, duration);
                break;

            case CommandEffect.Fortify:
                // Pure defense buff
                unit.ApplyBuff(BuffType.Defense, multiplier * 1.5f, duration);
                unit.CommandState(UnitState.Defending, strength);
                break;

            case CommandEffect.Haste:
                // Speed buff
                unit.ApplyBuff(BuffType.Speed, multiplier * 1.5f, duration);
                break;

            case CommandEffect.Heal:
                // Direct healing (scaled by strength)
                int healAmount = Mathf.RoundToInt(20 * multiplier);
                unit.Heal(healAmount);
                break;

            case CommandEffect.Reform:
                // Stop and reset state
                unit.CommandState(UnitState.Idle, strength);
                break;

            case CommandEffect.Hold:
                // Stop all action
                unit.CommandState(UnitState.Idle, 1f); // Always obey hold
                break;
        }
    }

    // Public accessors
    public List<CommandData> AvailableCommands => availableCommands;
    public CommandData ActiveCommand => activeCommand;
    public bool IsExecutingCommand => isExecutingCommand;
    public int CommandCount => availableCommands.Count;
}

/// <summary>
/// Result of command execution
/// </summary>
public enum CommandResult
{
    Failed,     // Score below minimum threshold
    Weak,       // Low score, reduced effectiveness
    Good,       // Decent score, normal effectiveness
    Perfect     // High score, bonus effectiveness
}
