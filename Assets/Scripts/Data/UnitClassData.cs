using UnityEngine;

/// <summary>
/// Defines a unit class/type with stats, requirements, and visuals.
/// Create assets via: Right-click > Create > Cantus > Unit Class
///
/// Unit Classes from GDD:
/// - Squire: Base unit, no requirements
/// - Armsman: Low STR+END, melee bonus
/// - Infantry: Low DEX+END, ranged bonus
/// - Knight: High STR+OBE, mounts allowed
/// - Crusader: High END+OBE, ally buffs
/// - Hospitaller: High END+OBE, healing
/// </summary>
[CreateAssetMenu(fileName = "NewUnitClass", menuName = "Cantus/Unit Class")]
public class UnitClassData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name for this unit class")]
    public string className = "New Unit";

    [Tooltip("Icon shown in UI panels")]
    public Sprite icon;

    [Tooltip("Sprite used for the unit on the battlefield")]
    public Sprite unitSprite;

    [Header("Base Stats")]
    [Tooltip("Damage dealt in combat - improved by weapons and XP")]
    [Range(1, 100)]
    public int strength = 10;

    [Tooltip("Movement and attack speed - affected by armor, weapons, mounts")]
    [Range(1, 100)]
    public int dexterity = 10;

    [Tooltip("Damage reduction - increased by heavier armor")]
    [Range(1, 100)]
    public int endurance = 10;

    [Tooltip("Responsiveness to commands and buff effectiveness")]
    [Range(1, 100)]
    public int obedience = 10;

    [Header("Class Requirements")]
    [Tooltip("Minimum strength stat to unlock this class (0 = no requirement)")]
    [Range(0, 100)]
    public int requiredStrength = 0;

    [Tooltip("Minimum dexterity stat to unlock this class (0 = no requirement)")]
    [Range(0, 100)]
    public int requiredDexterity = 0;

    [Tooltip("Minimum endurance stat to unlock this class (0 = no requirement)")]
    [Range(0, 100)]
    public int requiredEndurance = 0;

    [Tooltip("Minimum obedience stat to unlock this class (0 = no requirement)")]
    [Range(0, 100)]
    public int requiredObedience = 0;

    [Header("Economy")]
    [Tooltip("Gold cost to recruit a new unit of this class")]
    [Range(0, 10000)]
    public int goldCost = 100;

    [Tooltip("XP required to promote a unit to this class")]
    [Range(0, 10000)]
    public int xpToUnlock = 0;

    [Header("Class Bonuses")]
    [Tooltip("Damage multiplier for melee attacks (1.0 = normal)")]
    [Range(0.5f, 2.0f)]
    public float meleeBonus = 1.0f;

    [Tooltip("Damage multiplier for ranged attacks (1.0 = normal)")]
    [Range(0.5f, 2.0f)]
    public float rangedBonus = 1.0f;

    [Tooltip("Can this unit equip mounts (horses)?")]
    public bool canMount = false;

    [Tooltip("Provides passive buffs to nearby allies")]
    public bool hasAura = false;

    [Tooltip("Can heal nearby allies during battle")]
    public bool canHeal = false;

    /// <summary>
    /// Checks if a unit with given stats meets the requirements for this class
    /// </summary>
    public bool MeetsRequirements(int str, int dex, int end, int obe)
    {
        return str >= requiredStrength &&
               dex >= requiredDexterity &&
               end >= requiredEndurance &&
               obe >= requiredObedience;
    }

    /// <summary>
    /// Calculate effective damage for this unit class
    /// </summary>
    public float CalculateDamage(bool isMelee)
    {
        float bonus = isMelee ? meleeBonus : rangedBonus;
        return strength * bonus;
    }
}
