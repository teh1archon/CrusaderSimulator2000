using UnityEngine;

/// <summary>
/// Test runner for the Unit system.
/// Add to a GameObject in the scene to test unit spawning and autonomous combat.
///
/// Supports two modes:
/// 1. Prefab mode (recommended): Assign unit prefabs directly
/// 2. Legacy mode: Assign UnitClassData and units are created dynamically
/// </summary>
public class UnitTestRunner : MonoBehaviour
{
    [Header("Prefab Mode (Recommended)")]
    [Tooltip("Unit prefab with Unit component and UnitClassData already assigned")]
    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private GameObject enemyUnitPrefab;

    [Header("Legacy Mode (Fallback)")]
    [Tooltip("If prefabs not assigned, use UnitClassData to create units dynamically")]
    [SerializeField] private UnitClassData playerUnitClass;
    [SerializeField] private UnitClassData enemyUnitClass;

    [Header("Spawn Settings")]
    [SerializeField] private int playerUnitsToSpawn = 3;
    [SerializeField] private int enemyUnitsToSpawn = 3;
    [SerializeField] private Vector2 playerSpawnArea = new Vector2(-5, 0);
    [SerializeField] private Vector2 enemySpawnArea = new Vector2(5, 0);
    [SerializeField] private float spawnSpread = 2f;

    [Header("Auto-Spawn")]
    [Tooltip("Automatically spawn units on Start")]
    [SerializeField] private bool autoSpawnOnStart = false;

    private UnitSpawner spawner;
    private bool usingPrefabMode;

    private void Start()
    {
        spawner = UnitSpawner.Instance;
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<UnitSpawner>();
        }

        if (spawner == null)
        {
            var spawnerObj = new GameObject("UnitSpawner");
            spawner = spawnerObj.AddComponent<UnitSpawner>();
            Debug.Log("UnitTestRunner: Created UnitSpawner");
        }

        // Determine mode
        usingPrefabMode = playerUnitPrefab != null || enemyUnitPrefab != null;

        if (autoSpawnOnStart)
        {
            SpawnTestUnits();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SpawnTestUnits();
        if (Input.GetKeyDown(KeyCode.F2)) SpawnSinglePlayerUnit();
        if (Input.GetKeyDown(KeyCode.F3)) SpawnSingleEnemyUnit();
        if (Input.GetKeyDown(KeyCode.F4)) DespawnAllUnits();
        if (Input.GetKeyDown(KeyCode.F5)) PrintUnitStats();
    }

    public void SpawnTestUnits()
    {
        if (spawner == null)
        {
            Debug.LogError("UnitTestRunner: No UnitSpawner available!");
            return;
        }

        // Spawn player units
        for (int i = 0; i < playerUnitsToSpawn; i++)
        {
            Vector3 pos = GetRandomPosition(playerSpawnArea);
            SpawnPlayerUnit(pos);
        }

        // Spawn enemy units
        for (int i = 0; i < enemyUnitsToSpawn; i++)
        {
            Vector3 pos = GetRandomPosition(enemySpawnArea);
            SpawnEnemyUnit(pos);
        }

        string mode = usingPrefabMode ? "Prefab" : "Legacy";
        Debug.Log($"UnitTestRunner [{mode}]: Spawned {playerUnitsToSpawn} player units and {enemyUnitsToSpawn} enemy units");
    }

    public void SpawnSinglePlayerUnit()
    {
        Vector3 pos = GetRandomPosition(playerSpawnArea);
        SpawnPlayerUnit(pos);
        Debug.Log("Spawned player unit");
    }

    public void SpawnSingleEnemyUnit()
    {
        Vector3 pos = GetRandomPosition(enemySpawnArea);
        SpawnEnemyUnit(pos);
        Debug.Log("Spawned enemy unit");
    }

    private void SpawnPlayerUnit(Vector3 position)
    {
        if (spawner == null) return;

        if (playerUnitPrefab != null)
        {
            // Prefab mode
            spawner.SpawnUnit(playerUnitPrefab, position, true);
        }
        else
        {
            // Legacy mode
            var classData = playerUnitClass ?? CreateDefaultClassData("Test Squire");
            spawner.SpawnUnitFromData(classData, position, true);
        }
    }

    private void SpawnEnemyUnit(Vector3 position)
    {
        if (spawner == null) return;

        if (enemyUnitPrefab != null)
        {
            // Prefab mode
            spawner.SpawnUnit(enemyUnitPrefab, position, false);
        }
        else
        {
            // Legacy mode
            var classData = enemyUnitClass ?? CreateDefaultClassData("Test Enemy");
            spawner.SpawnUnitFromData(classData, position, false);
        }
    }

    public void DespawnAllUnits()
    {
        if (spawner != null)
        {
            spawner.DespawnAllUnits();
            Debug.Log("Despawned all units");
        }
    }

    public void PrintUnitStats()
    {
        if (spawner == null) return;

        var playerUnits = spawner.GetPlayerUnits();
        var enemyUnits = spawner.GetEnemyUnits();

        Debug.Log($"=== Unit Stats ===");
        Debug.Log($"Player units: {playerUnits.Count}");
        foreach (var unit in playerUnits)
        {
            if (unit != null)
                Debug.Log($"  {unit.ClassName}: {unit.CurrentHealth}/{unit.MaxHealth} HP - {unit.CurrentState}");
        }

        Debug.Log($"Enemy units: {enemyUnits.Count}");
        foreach (var unit in enemyUnits)
        {
            if (unit != null)
                Debug.Log($"  {unit.ClassName}: {unit.CurrentHealth}/{unit.MaxHealth} HP - {unit.CurrentState}");
        }
    }

    private Vector3 GetRandomPosition(Vector2 center)
    {
        return new Vector3(
            center.x + Random.Range(-spawnSpread, spawnSpread),
            center.y + Random.Range(-spawnSpread, spawnSpread),
            0
        );
    }

    private UnitClassData CreateDefaultClassData(string name)
    {
        var data = ScriptableObject.CreateInstance<UnitClassData>();
        data.className = name;
        data.strength = 10;
        data.dexterity = 10;
        data.endurance = 10;
        data.obedience = 50;
        return data;
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 220));
        GUILayout.Box("Unit Test Controls");

        string mode = usingPrefabMode ? "[Prefab Mode]" : "[Legacy Mode]";
        GUILayout.Label(mode);

        GUILayout.Label("F1: Spawn test battle");
        GUILayout.Label("F2: Spawn player unit");
        GUILayout.Label("F3: Spawn enemy unit");
        GUILayout.Label("F4: Despawn all");
        GUILayout.Label("F5: Print stats to console");

        if (spawner != null)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Player: {spawner.GetPlayerUnits().Count}");
            GUILayout.Label($"Enemy: {spawner.GetEnemyUnits().Count}");
        }

        GUILayout.EndArea();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(playerSpawnArea.x, playerSpawnArea.y, 0), Vector3.one * spawnSpread * 2);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(enemySpawnArea.x, enemySpawnArea.y, 0), Vector3.one * spawnSpread * 2);
    }
}
