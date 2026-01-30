#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using NavMeshPlus.Components;

/// <summary>
/// Editor tools for creating and managing levels.
/// Access via: Tools > Cantus Crucis > Level Tools
/// </summary>
public class LevelEditorTools : Editor
{
    private const string LEVELS_PATH = "Assets/ScriptableObjects/Levels";
    private const string OBSTACLES_PATH = "Assets/Prefabs/Obstacles";
    private const string UNITS_PATH = "Assets/Prefabs/Units";
    private const string COMMANDS_PATH = "Assets/ScriptableObjects/Commands";

    [MenuItem("Tools/Cantus Crucis/Level Tools/Create Sample Level (TestBattlefield)")]
    public static void CreateSampleLevel()
    {
        EnsureDirectoryExists(LEVELS_PATH);
        EnsureDirectoryExists(OBSTACLES_PATH);

        // First create the obstacle prefab if it doesn't exist
        CreateBasicObstaclePrefab();

        // Load required assets
        var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OBSTACLES_PATH}/BasicObstacle.prefab");
        var squirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UNITS_PATH}/Squire.prefab");
        var knightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UNITS_PATH}/Knight.prefab");

        // Load commands
        var attackCmd = AssetDatabase.LoadAssetAtPath<CommandData>($"{COMMANDS_PATH}/Attack.asset");
        var defendCmd = AssetDatabase.LoadAssetAtPath<CommandData>($"{COMMANDS_PATH}/Defend.asset");
        var chargeCmd = AssetDatabase.LoadAssetAtPath<CommandData>($"{COMMANDS_PATH}/Charge.asset");

        // Create level asset
        string levelPath = $"{LEVELS_PATH}/TestBattlefield.asset";
        var existing = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
        if (existing != null)
        {
            Debug.Log("TestBattlefield already exists. Updating...");
            UpdateTestBattlefield(existing, obstaclePrefab, squirePrefab, knightPrefab, attackCmd, defendCmd, chargeCmd);
            EditorUtility.SetDirty(existing);
        }
        else
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            UpdateTestBattlefield(level, obstaclePrefab, squirePrefab, knightPrefab, attackCmd, defendCmd, chargeCmd);
            AssetDatabase.CreateAsset(level, levelPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created TestBattlefield level at {levelPath}");

        // Select the created asset
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
    }

    private static void UpdateTestBattlefield(
        LevelData level,
        GameObject obstaclePrefab,
        GameObject squirePrefab,
        GameObject knightPrefab,
        CommandData attackCmd,
        CommandData defendCmd,
        CommandData chargeCmd)
    {
        level.levelName = "Test Battlefield";
        level.backgroundSprite = null;  // Will use scene default
        level.battlefieldSize = new Vector2(20f, 15f);

        // Clear existing data
        level.obstacles.Clear();
        level.playerUnits.Clear();
        level.enemyUnits.Clear();
        level.availableCommands.Clear();

        // Add obstacles (3 barriers in the middle)
        if (obstaclePrefab != null)
        {
            level.obstacles.Add(new LevelData.ObstacleEntry(
                obstaclePrefab,
                new Vector3(-2f, 2f, 0f),
                Quaternion.identity,
                new Vector3(1f, 3f, 1f)
            ));

            level.obstacles.Add(new LevelData.ObstacleEntry(
                obstaclePrefab,
                new Vector3(2f, -1f, 0f),
                Quaternion.identity,
                new Vector3(1f, 2.5f, 1f)
            ));

            level.obstacles.Add(new LevelData.ObstacleEntry(
                obstaclePrefab,
                new Vector3(0f, -3f, 0f),
                Quaternion.Euler(0, 0, 45f),
                new Vector3(1f, 2f, 1f)
            ));
        }

        // Add player units (left side)
        var playerPrefab = squirePrefab ?? knightPrefab;
        if (playerPrefab != null)
        {
            level.playerUnits.Add(new LevelData.UnitSpawnEntry(playerPrefab, new Vector3(-7f, 2f, 0f)));
            level.playerUnits.Add(new LevelData.UnitSpawnEntry(playerPrefab, new Vector3(-7f, 0f, 0f)));
            level.playerUnits.Add(new LevelData.UnitSpawnEntry(playerPrefab, new Vector3(-7f, -2f, 0f)));
        }

        // Add enemy units (right side)
        var enemyPrefab = squirePrefab ?? knightPrefab;
        if (enemyPrefab != null)
        {
            level.enemyUnits.Add(new LevelData.UnitSpawnEntry(enemyPrefab, new Vector3(7f, 2f, 0f)));
            level.enemyUnits.Add(new LevelData.UnitSpawnEntry(enemyPrefab, new Vector3(7f, 0f, 0f)));
            level.enemyUnits.Add(new LevelData.UnitSpawnEntry(enemyPrefab, new Vector3(7f, -2f, 0f)));
        }

        // Add commands
        if (attackCmd != null) level.availableCommands.Add(attackCmd);
        if (defendCmd != null) level.availableCommands.Add(defendCmd);
        if (chargeCmd != null) level.availableCommands.Add(chargeCmd);
    }

    [MenuItem("Tools/Cantus Crucis/Level Tools/Create Basic Obstacle Prefab")]
    public static void CreateBasicObstaclePrefab()
    {
        EnsureDirectoryExists(OBSTACLES_PATH);

        string prefabPath = $"{OBSTACLES_PATH}/BasicObstacle.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.Log("BasicObstacle prefab already exists.");
            return;
        }

        // Create the obstacle GameObject
        GameObject obstacle = new GameObject("BasicObstacle");

        // Add SpriteRenderer with a simple square
        var spriteRenderer = obstacle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreatePlaceholderSprite();
        spriteRenderer.color = new Color(0.2f, 0.2f, 0.2f, 1f);  // Dark gray
        spriteRenderer.sortingOrder = 1;

        // Add BoxCollider2D for physics/NavMesh
        var collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        // Add NavMeshModifier to mark as Not Walkable
        // Note: NavMeshModifier is from NavMeshPlus package
        var modifier = obstacle.AddComponent<NavMeshModifier>();
        modifier.overrideArea = true;
        modifier.area = 1;  // 1 = Not Walkable (from NavMesh areas)

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obstacle, prefabPath);

        // Cleanup scene object
        DestroyImmediate(obstacle);

        AssetDatabase.SaveAssets();
        Debug.Log($"Created BasicObstacle prefab at {prefabPath}");

        Selection.activeObject = prefab;
    }

    private static Sprite CreatePlaceholderSprite()
    {
        // Try to find Unity's default white square sprite
        Sprite builtinSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtinSprite != null) return builtinSprite;

        // Fallback: create a simple white texture
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    [MenuItem("Tools/Cantus Crucis/Level Tools/Create Level From Scene Selection")]
    public static void CreateLevelFromSceneSelection()
    {
        EnsureDirectoryExists(LEVELS_PATH);

        // Create new level
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.levelName = "New Level";

        // Find selected objects and categorize them
        foreach (var obj in Selection.gameObjects)
        {
            // Check if it's a unit
            var unit = obj.GetComponent<Unit>();
            if (unit != null)
            {
                // Get the prefab source
                var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabSource != null)
                {
                    var entry = new LevelData.UnitSpawnEntry(prefabSource, obj.transform.position);
                    if (unit.IsPlayerUnit)
                        level.playerUnits.Add(entry);
                    else
                        level.enemyUnits.Add(entry);
                }
                continue;
            }

            // Check if it's an obstacle (has collider but no Unit)
            var collider = obj.GetComponent<Collider2D>();
            if (collider != null)
            {
                var prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabSource != null)
                {
                    level.obstacles.Add(new LevelData.ObstacleEntry(
                        prefabSource,
                        obj.transform.position,
                        obj.transform.rotation,
                        obj.transform.localScale
                    ));
                }
            }
        }

        // Save with unique name
        string path = AssetDatabase.GenerateUniqueAssetPath($"{LEVELS_PATH}/NewLevel.asset");
        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Created level from selection: {path}");
        Debug.Log($"  - {level.playerUnits.Count} player units");
        Debug.Log($"  - {level.enemyUnits.Count} enemy units");
        Debug.Log($"  - {level.obstacles.Count} obstacles");

        Selection.activeObject = level;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path);
            string folderName = Path.GetFileName(path);

            // Ensure parent exists
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = Path.GetDirectoryName(parent);
                string parentName = Path.GetFileName(parent);
                AssetDatabase.CreateFolder(grandParent, parentName);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
