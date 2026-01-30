#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to create sample ScriptableObject assets for the game designer.
/// Access via: Tools > Cantus Crucis > Create Sample Assets
/// </summary>
public class SampleAssetCreator : Editor
{
    private const string UNITS_PATH = "Assets/ScriptableObjects/Units";
    private const string MELODIES_PATH = "Assets/ScriptableObjects/Melodies";
    private const string COMMANDS_PATH = "Assets/ScriptableObjects/Commands";

    [MenuItem("Tools/Cantus Crucis/Create Sample Assets")]
    public static void CreateAllSampleAssets()
    {
        CreateSampleUnitClasses();
        CreateSampleMelodies();
        CreateSampleCommands();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sample assets created! Check ScriptableObjects folder.");
    }

    [MenuItem("Tools/Cantus Crucis/Create Sample Unit Classes")]
    public static void CreateSampleUnitClasses()
    {
        EnsureDirectoryExists(UNITS_PATH);

        // Squire - Base unit
        CreateUnitClass("Squire",
            strength: 8, dexterity: 8, endurance: 8, obedience: 8,
            reqStr: 0, reqDex: 0, reqEnd: 0, reqObe: 0,
            goldCost: 50, xpToUnlock: 0);

        // Armsman - Melee specialist
        CreateUnitClass("Armsman",
            strength: 12, dexterity: 10, endurance: 10, obedience: 8,
            reqStr: 10, reqDex: 0, reqEnd: 10, reqObe: 0,
            goldCost: 100, xpToUnlock: 50,
            meleeBonus: 1.3f);

        // Infantry - Ranged specialist
        CreateUnitClass("Infantry",
            strength: 8, dexterity: 12, endurance: 10, obedience: 8,
            reqStr: 0, reqDex: 10, reqEnd: 10, reqObe: 0,
            goldCost: 100, xpToUnlock: 50,
            rangedBonus: 1.3f);

        // Knight - Elite melee with mount
        CreateUnitClass("Knight",
            strength: 15, dexterity: 12, endurance: 12, obedience: 12,
            reqStr: 15, reqDex: 0, reqEnd: 0, reqObe: 15,
            goldCost: 250, xpToUnlock: 150,
            meleeBonus: 1.5f, canMount: true);

        // Crusader - Support with aura
        CreateUnitClass("Crusader",
            strength: 10, dexterity: 8, endurance: 15, obedience: 15,
            reqStr: 0, reqDex: 0, reqEnd: 15, reqObe: 15,
            goldCost: 200, xpToUnlock: 100,
            hasAura: true);

        // Hospitaller - Healer
        CreateUnitClass("Hospitaller",
            strength: 6, dexterity: 8, endurance: 15, obedience: 15,
            reqStr: 0, reqDex: 0, reqEnd: 15, reqObe: 15,
            goldCost: 200, xpToUnlock: 100,
            canHeal: true);

        Debug.Log($"Created 6 unit class assets in {UNITS_PATH}");
    }

    [MenuItem("Tools/Cantus Crucis/Create Sample Melodies")]
    public static void CreateSampleMelodies()
    {
        EnsureDirectoryExists(MELODIES_PATH);

        // Attack Command - Fast aggressive pattern
        var attack = CreateMelody("Attack", bpm: 140, duration: 3f);
        attack.notes.Add(new MelodyData.NoteEntry(0.5f, NoteLane.Key5));
        attack.notes.Add(new MelodyData.NoteEntry(1.0f, NoteLane.KeyT));
        attack.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeyG));
        attack.notes.Add(new MelodyData.NoteEntry(2.0f, NoteLane.KeyB));
        attack.notes.Add(new MelodyData.NoteEntry(2.5f, NoteLane.KeySpace));
        EditorUtility.SetDirty(attack);

        // Defend Command - Steady rhythm
        var defend = CreateMelody("Defend", bpm: 100, duration: 4f);
        defend.notes.Add(new MelodyData.NoteEntry(0.5f, NoteLane.KeySpace));
        defend.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeySpace));
        defend.notes.Add(new MelodyData.NoteEntry(2.5f, NoteLane.KeySpace));
        defend.notes.Add(new MelodyData.NoteEntry(3.5f, NoteLane.KeySpace));
        EditorUtility.SetDirty(defend);

        // Rally Command - Rising pattern for morale
        var rally = CreateMelody("Rally", bpm: 120, duration: 4f);
        rally.notes.Add(new MelodyData.NoteEntry(0.5f, NoteLane.KeySpace));
        rally.notes.Add(new MelodyData.NoteEntry(1.0f, NoteLane.KeyB));
        rally.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeyG));
        rally.notes.Add(new MelodyData.NoteEntry(2.0f, NoteLane.KeyT));
        rally.notes.Add(new MelodyData.NoteEntry(2.5f, NoteLane.Key5));
        rally.notes.Add(new MelodyData.NoteEntry(3.0f, NoteLane.KeyT));
        rally.notes.Add(new MelodyData.NoteEntry(3.5f, NoteLane.Key5));
        EditorUtility.SetDirty(rally);

        Debug.Log($"Created 3 melody assets in {MELODIES_PATH}");
    }

    [MenuItem("Tools/Cantus Crucis/Create Sample Commands")]
    public static void CreateSampleCommands()
    {
        EnsureDirectoryExists(COMMANDS_PATH);

        // Load melodies to link
        var attackMelody = AssetDatabase.LoadAssetAtPath<MelodyData>($"{MELODIES_PATH}/Attack.asset");
        var defendMelody = AssetDatabase.LoadAssetAtPath<MelodyData>($"{MELODIES_PATH}/Defend.asset");
        var rallyMelody = AssetDatabase.LoadAssetAtPath<MelodyData>($"{MELODIES_PATH}/Rally.asset");

        // Attack Command
        CreateCommand("Attack",
            description: "Order your troops to aggressively engage the enemy. Increases damage dealt.",
            melody: attackMelody,
            effect: CommandEffect.Attack,
            target: CommandTarget.AllPlayerUnits,
            baseStrength: 1.2f,
            duration: 8f,
            moraleGain: 3,
            moraleLoss: 2);

        // Defend Command
        CreateCommand("Defend",
            description: "Order your troops to hold position and brace for attack. Increases defense.",
            melody: defendMelody,
            effect: CommandEffect.Defend,
            target: CommandTarget.AllPlayerUnits,
            baseStrength: 1.3f,
            duration: 10f,
            moraleGain: 2,
            moraleLoss: 1);

        // Rally Command
        CreateCommand("Rally",
            description: "Inspire your troops with a rallying cry. Restores morale and provides a small buff.",
            melody: rallyMelody,
            effect: CommandEffect.Rally,
            target: CommandTarget.AllPlayerUnits,
            baseStrength: 1.5f,
            duration: 6f,
            moraleGain: 10,
            moraleLoss: 3);

        // Charge Command (uses Attack melody with different effect)
        var chargeMelody = CreateMelody("Charge", bpm: 160, duration: 2.5f);
        if (chargeMelody.notes.Count == 0)
        {
            chargeMelody.notes.Add(new MelodyData.NoteEntry(0.3f, NoteLane.Key5));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(0.6f, NoteLane.Key5));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(0.9f, NoteLane.KeyT));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(1.2f, NoteLane.KeyT));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeyG));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(1.8f, NoteLane.KeyB));
            chargeMelody.notes.Add(new MelodyData.NoteEntry(2.1f, NoteLane.KeySpace));
            EditorUtility.SetDirty(chargeMelody);
        }

        CreateCommand("Charge",
            description: "Order a devastating charge! High damage and speed boost, but risky.",
            melody: chargeMelody,
            effect: CommandEffect.Charge,
            target: CommandTarget.AllPlayerUnits,
            baseStrength: 1.5f,
            duration: 5f,
            moraleGain: 5,
            moraleLoss: 5,
            minScore: 30,
            perfectScore: 85);

        // Hold Command (simple melody)
        var holdMelody = CreateMelody("Hold", bpm: 80, duration: 2f);
        if (holdMelody.notes.Count == 0)
        {
            holdMelody.notes.Add(new MelodyData.NoteEntry(0.5f, NoteLane.KeySpace));
            holdMelody.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeySpace));
            EditorUtility.SetDirty(holdMelody);
        }

        CreateCommand("Hold",
            description: "Order all units to stop and await further commands.",
            melody: holdMelody,
            effect: CommandEffect.Hold,
            target: CommandTarget.AllPlayerUnits,
            baseStrength: 1.0f,
            duration: 0f,  // Instant
            moraleGain: 1,
            moraleLoss: 1,
            minScore: 10,
            perfectScore: 50);

        Debug.Log($"Created 5 command assets in {COMMANDS_PATH}");
    }

    private static void CreateCommand(
        string name,
        string description,
        MelodyData melody,
        CommandEffect effect,
        CommandTarget target,
        float baseStrength,
        float duration,
        int moraleGain,
        int moraleLoss,
        int minScore = 20,
        int perfectScore = 80)
    {
        string path = $"{COMMANDS_PATH}/{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<CommandData>(path);
        if (existing != null)
        {
            Debug.Log($"Command {name} already exists, skipping.");
            return;
        }

        var command = ScriptableObject.CreateInstance<CommandData>();
        command.commandName = name;
        command.description = description;
        command.melody = melody;
        command.effectType = effect;
        command.targetType = target;
        command.baseStrength = baseStrength;
        command.duration = duration;
        command.moraleGainOnSuccess = moraleGain;
        command.moraleLossOnFail = moraleLoss;
        command.minimumScoreToExecute = minScore;
        command.perfectScoreThreshold = perfectScore;

        AssetDatabase.CreateAsset(command, path);
    }

    private static void CreateUnitClass(
        string name,
        int strength, int dexterity, int endurance, int obedience,
        int reqStr, int reqDex, int reqEnd, int reqObe,
        int goldCost, int xpToUnlock,
        float meleeBonus = 1.0f, float rangedBonus = 1.0f,
        bool canMount = false, bool hasAura = false, bool canHeal = false)
    {
        string path = $"{UNITS_PATH}/{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<UnitClassData>(path);
        if (existing != null)
        {
            Debug.Log($"Unit class {name} already exists, skipping.");
            return;
        }

        var unit = ScriptableObject.CreateInstance<UnitClassData>();
        unit.className = name;
        unit.strength = strength;
        unit.dexterity = dexterity;
        unit.endurance = endurance;
        unit.obedience = obedience;
        unit.requiredStrength = reqStr;
        unit.requiredDexterity = reqDex;
        unit.requiredEndurance = reqEnd;
        unit.requiredObedience = reqObe;
        unit.goldCost = goldCost;
        unit.xpToUnlock = xpToUnlock;
        unit.meleeBonus = meleeBonus;
        unit.rangedBonus = rangedBonus;
        unit.canMount = canMount;
        unit.hasAura = hasAura;
        unit.canHeal = canHeal;

        AssetDatabase.CreateAsset(unit, path);
    }

    private static MelodyData CreateMelody(string name, float bpm, float duration)
    {
        string path = $"{MELODIES_PATH}/{name}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<MelodyData>(path);
        if (existing != null)
        {
            Debug.Log($"Melody {name} already exists, skipping.");
            return existing;
        }

        var melody = ScriptableObject.CreateInstance<MelodyData>();
        melody.melodyName = name;
        melody.bpm = bpm;
        melody.duration = duration;

        AssetDatabase.CreateAsset(melody, path);
        return melody;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
