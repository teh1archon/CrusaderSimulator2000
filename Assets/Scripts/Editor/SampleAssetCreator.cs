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

    [MenuItem("Tools/Cantus Crucis/Create Sample Assets")]
    public static void CreateAllSampleAssets()
    {
        CreateSampleUnitClasses();
        CreateSampleMelodies();
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
