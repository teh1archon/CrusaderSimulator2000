using UnityEngine;

/// <summary>
/// Defines a command that links a melody to battlefield effects.
/// Commands are executed by playing melodies; score determines effectiveness.
/// Create assets via: Right-click > Create > Cantus > Command
/// </summary>
[CreateAssetMenu(fileName = "NewCommand", menuName = "Cantus/Command")]
public class CommandData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name for this command")]
    public string commandName;

    [Tooltip("Icon shown in command selection UI")]
    public Sprite icon;

    [TextArea(2, 4)]
    [Tooltip("Description shown to player")]
    public string description;

    [Header("Melody")]
    [Tooltip("The melody that must be played to execute this command")]
    public MelodyData melody;

    [Header("Effect Type")]
    [Tooltip("What this command does when executed")]
    public CommandEffect effectType;

    [Header("Targeting")]
    [Tooltip("Which units are affected by this command")]
    public CommandTarget targetType;

    [Tooltip("For area effects, the radius around the target point")]
    [Range(0, 50)]
    public float effectRadius = 10f;

    [Header("Effect Values")]
    [Tooltip("Base strength multiplier (1.0 = normal, 1.5 = 50% stronger)")]
    [Range(0.5f, 3f)]
    public float baseStrength = 1.0f;

    [Tooltip("How long the effect lasts in seconds (0 = instant)")]
    [Range(0, 30)]
    public float duration = 5f;

    [Header("Morale")]
    [Tooltip("Morale gained on successful execution (scaled by score)")]
    [Range(0, 20)]
    public int moraleGainOnSuccess = 5;

    [Tooltip("Morale lost if melody is interrupted or failed badly")]
    [Range(0, 20)]
    public int moraleLossOnFail = 3;

    [Header("Score Thresholds")]
    [Tooltip("Minimum score (0-100) to execute command at all")]
    [Range(0, 100)]
    public int minimumScoreToExecute = 20;

    [Tooltip("Score needed for full effect strength")]
    [Range(0, 100)]
    public int perfectScoreThreshold = 80;

    /// <summary>
    /// Calculate effect strength based on melody performance score (0-100)
    /// </summary>
    public float CalculateEffectStrength(float score)
    {
        if (score < minimumScoreToExecute)
            return 0f; // Command fails

        // Scale from 0.5x at minimum to 1.0x at perfect threshold
        float normalizedScore = Mathf.InverseLerp(minimumScoreToExecute, perfectScoreThreshold, score);
        float strengthMultiplier = Mathf.Lerp(0.5f, 1.0f, normalizedScore);

        // Bonus for exceeding perfect threshold
        if (score > perfectScoreThreshold)
        {
            float bonusScore = Mathf.InverseLerp(perfectScoreThreshold, 100f, score);
            strengthMultiplier += bonusScore * 0.25f; // Up to 1.25x for perfect score
        }

        return baseStrength * strengthMultiplier;
    }

    /// <summary>
    /// Calculate morale change based on performance score
    /// </summary>
    public int CalculateMoraleChange(float score)
    {
        if (score < minimumScoreToExecute)
            return -moraleLossOnFail;

        // Scale morale gain by score
        float normalizedScore = score / 100f;
        return Mathf.RoundToInt(moraleGainOnSuccess * normalizedScore);
    }
}

/// <summary>
/// Types of command effects
/// </summary>
public enum CommandEffect
{
    // Offensive
    Attack,         // Units aggressively seek and attack enemies
    Charge,         // Units rush forward with damage bonus

    // Defensive
    Defend,         // Units hold position and gain defense bonus
    Retreat,        // Units fall back to rally point

    // Buffs
    Rally,          // Restore morale, temporary stat boost
    Inspire,        // Increase damage for duration
    Fortify,        // Increase defense for duration
    Haste,          // Increase movement/attack speed

    // Special
    Heal,           // Hospitallers heal nearby units
    Reform,         // Units regroup into formation
    Hold,           // Units stop and wait for next command
}

/// <summary>
/// Which units are affected by a command
/// </summary>
public enum CommandTarget
{
    AllPlayerUnits,     // Every player unit on the field
    NearbyUnits,        // Units within effectRadius of commander
    SelectedClass,      // Specific unit class only (future feature)
    SingleUnit,         // Target one unit (future feature)
}
