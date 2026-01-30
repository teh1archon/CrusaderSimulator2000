using System;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages level loading, obstacle placement, and NavMesh baking.
/// Coordinates with UnitSpawner and CommandExecutor to set up battles.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Scene References")]
    [Tooltip("SpriteRenderer for the level background")]
    [SerializeField] private SpriteRenderer backgroundRenderer;

    [Tooltip("Parent transform for spawned obstacles")]
    [SerializeField] private Transform obstacleParent;

    [Tooltip("NavMeshSurface for runtime baking")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Current Level")]
    [SerializeField] private LevelData currentLevel;
    [SerializeField] private bool isLevelLoaded;

    [Header("Debug")]
    [SerializeField] private bool autoLoadOnStart;
    [SerializeField] private LevelData debugLevelToLoad;

    // Track spawned objects for cleanup
    private List<GameObject> spawnedObstacles = new List<GameObject>();

    // Events
    public event Action<LevelData> OnLevelLoaded;
    public event Action OnLevelUnloaded;
    public event Action OnNavMeshBaked;

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
        if (autoLoadOnStart && debugLevelToLoad != null)
        {
            LoadLevel(debugLevelToLoad);
        }
    }

    /// <summary>
    /// Load a level with its default unit configuration
    /// </summary>
    public void LoadLevel(LevelData level)
    {
        LoadLevel(level, null);
    }

    /// <summary>
    /// Load a level with campaign unit overrides
    /// </summary>
    /// <param name="level">Level data to load</param>
    /// <param name="campaignPlayerUnits">Override player units (null = use level defaults)</param>
    public void LoadLevel(LevelData level, List<GameObject> campaignPlayerUnits)
    {
        if (level == null)
        {
            Debug.LogError("[LevelManager] Cannot load null level!");
            return;
        }

        // Unload existing level first
        if (isLevelLoaded)
        {
            UnloadLevel();
        }

        currentLevel = level;
        Debug.Log($"[LevelManager] Loading level: {level.levelName}");

        // 1. Set background
        SetBackground(level.backgroundSprite);

        // 2. Spawn obstacles
        SpawnObstacles(level.obstacles);

        // 3. Bake NavMesh
        BakeNavMesh();

        // 4. Set available commands
        SetCommands(level.availableCommands);

        // 5. Spawn units
        SpawnUnits(level, campaignPlayerUnits);

        isLevelLoaded = true;
        OnLevelLoaded?.Invoke(level);

        Debug.Log($"[LevelManager] Level '{level.levelName}' loaded successfully!");
    }

    /// <summary>
    /// Unload the current level
    /// </summary>
    public void UnloadLevel()
    {
        if (!isLevelLoaded) return;

        Debug.Log($"[LevelManager] Unloading level: {currentLevel?.levelName}");

        // Clear obstacles
        foreach (var obstacle in spawnedObstacles)
        {
            if (obstacle != null)
                Destroy(obstacle);
        }
        spawnedObstacles.Clear();

        // Clear units via UnitSpawner
        if (UnitSpawner.Instance != null)
        {
            UnitSpawner.Instance.DespawnAllUnits();
        }

        // Clear background
        if (backgroundRenderer != null)
        {
            backgroundRenderer.sprite = null;
        }

        currentLevel = null;
        isLevelLoaded = false;

        OnLevelUnloaded?.Invoke();
    }

    private void SetBackground(Sprite sprite)
    {
        if (backgroundRenderer == null)
        {
            Debug.LogWarning("[LevelManager] No background renderer assigned!");
            return;
        }

        backgroundRenderer.sprite = sprite;

        if (sprite != null)
        {
            Debug.Log($"[LevelManager] Background set: {sprite.name}");
        }
    }

    private void SpawnObstacles(List<LevelData.ObstacleEntry> obstacles)
    {
        if (obstacles == null || obstacles.Count == 0)
        {
            Debug.Log("[LevelManager] No obstacles to spawn");
            return;
        }

        // Create parent if not assigned
        if (obstacleParent == null)
        {
            var parentObj = new GameObject("Obstacles");
            obstacleParent = parentObj.transform;
        }

        foreach (var entry in obstacles)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning("[LevelManager] Obstacle entry has null prefab, skipping");
                continue;
            }

            // Instantiate under parent first, then apply local transform values
            GameObject obstacle = Instantiate(entry.prefab, obstacleParent);

            // Apply stored values as LOCAL transforms (not world)
            obstacle.transform.localPosition = entry.position;
            obstacle.transform.localRotation = entry.rotation;
            obstacle.transform.localScale = entry.scale;

            spawnedObstacles.Add(obstacle);
        }

        Debug.Log($"[LevelManager] Spawned {spawnedObstacles.Count} obstacles");
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogWarning("[LevelManager] No NavMeshSurface assigned - skipping bake");
            return;
        }

        Debug.Log("[LevelManager] Baking NavMesh...");
        navMeshSurface.BuildNavMesh();
        OnNavMeshBaked?.Invoke();
        Debug.Log("[LevelManager] NavMesh baked successfully");
    }

    private void SetCommands(List<CommandData> commands)
    {
        if (CommandExecutor.Instance == null)
        {
            Debug.LogWarning("[LevelManager] CommandExecutor not found!");
            return;
        }

        if (commands != null && commands.Count > 0)
        {
            CommandExecutor.Instance.SetAvailableCommands(commands);
            Debug.Log($"[LevelManager] Set {commands.Count} available commands");
        }
    }

    private void SpawnUnits(LevelData level, List<GameObject> campaignPlayerUnits)
    {
        if (UnitSpawner.Instance == null)
        {
            Debug.LogError("[LevelManager] UnitSpawner not found - cannot spawn units!");
            return;
        }

        var spawner = UnitSpawner.Instance;

        // Spawn enemy units (always from level data)
        foreach (var entry in level.enemyUnits)
        {
            if (entry.unitPrefab == null) continue;
            spawner.SpawnUnit(entry.unitPrefab, entry.spawnPosition, false);
        }
        Debug.Log($"[LevelManager] Spawned {level.enemyUnits.Count} enemy units");

        // Spawn player units (from campaign override or level data)
        if (campaignPlayerUnits != null && campaignPlayerUnits.Count > 0)
        {
            // Use campaign units at level-defined positions
            int spawnCount = Mathf.Min(campaignPlayerUnits.Count, level.playerUnits.Count);
            for (int i = 0; i < spawnCount; i++)
            {
                var prefab = campaignPlayerUnits[i];
                var position = level.playerUnits[i].spawnPosition;
                if (prefab != null)
                {
                    spawner.SpawnUnit(prefab, position, true);
                }
            }
            Debug.Log($"[LevelManager] Spawned {spawnCount} campaign player units");
        }
        else
        {
            // Use level default units
            foreach (var entry in level.playerUnits)
            {
                if (entry.unitPrefab == null) continue;
                spawner.SpawnUnit(entry.unitPrefab, entry.spawnPosition, true);
            }
            Debug.Log($"[LevelManager] Spawned {level.playerUnits.Count} default player units");
        }
    }

    // Public accessors
    public LevelData CurrentLevel => currentLevel;
    public bool IsLevelLoaded => isLevelLoaded;
}
