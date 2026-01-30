using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns units from prefabs. Each unit type should have its own prefab with:
/// - Unit component (with UnitClassData reference)
/// - UnitVisuals component
/// - NavMeshAgent (configured for 2D with appropriate radius)
/// - SpriteRenderer
/// - Collider2D
///
/// Prefabs are pooled per-type for performance.
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    public static UnitSpawner Instance { get; private set; }

    [Header("Spawn Configuration")]
    [SerializeField] private Transform[] playerSpawnPoints;
    [SerializeField] private Transform[] enemySpawnPoints;
    [SerializeField] private float spawnRadius = 2f;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSizePerType = 10;
    [SerializeField] private Transform poolParent;

    [Header("Fallback (for testing without prefabs)")]
    [Tooltip("If true, creates basic units dynamically when no prefab provided")]
    [SerializeField] private bool allowDynamicCreation = true;

    // Unit tracking
    private List<Unit> allUnits = new List<Unit>();
    private List<Unit> playerUnits = new List<Unit>();
    private List<Unit> enemyUnits = new List<Unit>();

    // Per-prefab pooling
    private Dictionary<GameObject, Queue<GameObject>> prefabPools = new Dictionary<GameObject, Queue<GameObject>>();

    // Team color tinting
    [Header("Team Colors")]
    [SerializeField] private Color playerTint = new Color(0.7f, 0.9f, 1f, 1f);  // Light blue tint
    [SerializeField] private Color enemyTint = new Color(1f, 0.7f, 0.7f, 1f);   // Light red tint
    [SerializeField] private bool applyTeamTint = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Create pool parent if not set
        if (poolParent == null)
        {
            var poolObj = new GameObject("UnitPool");
            poolObj.transform.SetParent(transform);
            poolParent = poolObj.transform;
        }
    }

    #region Prefab-Based Spawning (Primary API)

    /// <summary>
    /// Spawn a unit from a prefab at specified position.
    /// The prefab should have Unit component with UnitClassData already assigned.
    /// </summary>
    public Unit SpawnUnit(GameObject prefab, Vector3 position, bool isPlayerUnit)
    {
        if (prefab == null)
        {
            Debug.LogError("UnitSpawner: Cannot spawn unit with null prefab");
            return null;
        }

        // Get from pool or instantiate
        GameObject unitObj = GetFromPool(prefab);
        unitObj.transform.position = position;
        unitObj.transform.SetParent(null); // Remove from pool parent

        // Get unit component
        Unit unit = unitObj.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"UnitSpawner: Prefab '{prefab.name}' missing Unit component!");
            ReturnToPool(prefab, unitObj);
            return null;
        }

        // Initialize unit (uses classData from prefab)
        unit.Initialize(isPlayerUnit);
        unitObj.name = $"{unit.ClassName}_{(isPlayerUnit ? "Player" : "Enemy")}";

        // Apply team tint
        if (applyTeamTint)
        {
            var sr = unitObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = isPlayerUnit ? playerTint : enemyTint;
            }
        }

        // Ensure NavMeshAgent is properly placed
        var agent = unitObj.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            unitObj.SetActive(true);
            agent.enabled = true;
            agent.Warp(position);
        }
        else
        {
            unitObj.SetActive(true);
        }

        // Track the unit
        RegisterUnit(unit, isPlayerUnit);

        Debug.Log($"Spawned {unit.ClassName} at {position} (Player: {isPlayerUnit})");
        return unit;
    }

    /// <summary>
    /// Spawn a unit at a random spawn point
    /// </summary>
    public Unit SpawnUnitAtSpawnPoint(GameObject prefab, bool isPlayerUnit)
    {
        Vector3 position = GetRandomSpawnPosition(isPlayerUnit);
        return SpawnUnit(prefab, position, isPlayerUnit);
    }

    /// <summary>
    /// Spawn multiple units of the same type
    /// </summary>
    public List<Unit> SpawnUnits(GameObject prefab, int count, bool isPlayerUnit)
    {
        List<Unit> spawned = new List<Unit>();
        for (int i = 0; i < count; i++)
        {
            Unit unit = SpawnUnitAtSpawnPoint(prefab, isPlayerUnit);
            if (unit != null)
                spawned.Add(unit);
        }
        return spawned;
    }

    /// <summary>
    /// Pre-warm the pool for a specific prefab
    /// </summary>
    public void PrewarmPool(GameObject prefab, int count)
    {
        if (prefab == null) return;

        if (!prefabPools.ContainsKey(prefab))
        {
            prefabPools[prefab] = new Queue<GameObject>();
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(poolParent);
            prefabPools[prefab].Enqueue(obj);
        }
    }

    #endregion

    #region Legacy API (UnitClassData-based, for testing)

    /// <summary>
    /// [LEGACY] Spawn a unit from UnitClassData. Creates a basic unit dynamically.
    /// Prefer using prefab-based SpawnUnit() for production.
    /// </summary>
    public Unit SpawnUnitFromData(UnitClassData classData, Vector3 position, bool isPlayerUnit)
    {
        if (classData == null)
        {
            Debug.LogError("UnitSpawner: Cannot spawn unit with null UnitClassData");
            return null;
        }

        if (!allowDynamicCreation)
        {
            Debug.LogError("UnitSpawner: Dynamic creation disabled. Use prefab-based spawning.");
            return null;
        }

        GameObject unitObj = CreateDynamicUnit(classData);
        unitObj.transform.position = position;
        unitObj.name = $"{classData.className}_{(isPlayerUnit ? "Player" : "Enemy")}";

        Unit unit = unitObj.GetComponent<Unit>();
        unit.Initialize(classData, isPlayerUnit);

        // Set sprite
        var sr = unitObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (classData.unitSprite != null)
            {
                sr.sprite = classData.unitSprite;
            }
            else
            {
                sr.sprite = CreatePlaceholderSprite(isPlayerUnit);
            }

            if (applyTeamTint)
            {
                sr.color = isPlayerUnit ? playerTint : enemyTint;
            }
        }

        // Ensure NavMeshAgent is on navmesh
        var agent = unitObj.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
            unitObj.SetActive(true);
            agent.enabled = true;
            agent.Warp(position);
        }
        else
        {
            unitObj.SetActive(true);
        }

        RegisterUnit(unit, isPlayerUnit);

        Debug.Log($"[Legacy] Spawned {classData.className} at {position} (Player: {isPlayerUnit})");
        return unit;
    }

    private GameObject CreateDynamicUnit(UnitClassData classData)
    {
        var unitObj = new GameObject("Unit");

        // Add NavMeshAgent configured for 2D
        var agent = unitObj.AddComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.radius = 0.3f;
        agent.height = 0.5f;

        // Add sprite renderer
        var sr = unitObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        // Add unit component
        unitObj.AddComponent<Unit>();

        // Add visuals component
        unitObj.AddComponent<UnitVisuals>();

        // Add collider for selection/combat
        var collider = unitObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;

        // Set layer
        unitObj.layer = LayerMask.NameToLayer("Units");
        if (unitObj.layer == -1) unitObj.layer = 0;

        return unitObj;
    }

    #endregion

    #region Unit Tracking

    private void RegisterUnit(Unit unit, bool isPlayerUnit)
    {
        allUnits.Add(unit);
        if (isPlayerUnit)
            playerUnits.Add(unit);
        else
            enemyUnits.Add(unit);

        unit.OnDeath += OnUnitDeath;
    }

    private void UnregisterUnit(Unit unit)
    {
        unit.OnDeath -= OnUnitDeath;
        allUnits.Remove(unit);
        playerUnits.Remove(unit);
        enemyUnits.Remove(unit);
    }

    public List<Unit> GetAllUnits() => new List<Unit>(allUnits);
    public List<Unit> GetPlayerUnits() => new List<Unit>(playerUnits);
    public List<Unit> GetEnemyUnits() => new List<Unit>(enemyUnits);

    #endregion

    #region Despawning

    /// <summary>
    /// Despawn a unit and return it to pool
    /// </summary>
    public void DespawnUnit(Unit unit)
    {
        if (unit == null) return;

        UnregisterUnit(unit);

        // Try to return to appropriate pool
        // For now, just deactivate (proper pooling requires tracking source prefab)
        unit.gameObject.SetActive(false);
        unit.transform.SetParent(poolParent);
    }

    /// <summary>
    /// Despawn all units
    /// </summary>
    public void DespawnAllUnits()
    {
        var unitsCopy = new List<Unit>(allUnits);
        foreach (var unit in unitsCopy)
        {
            DespawnUnit(unit);
        }
    }

    private void OnUnitDeath(Unit deadUnit)
    {
        allUnits.Remove(deadUnit);
        playerUnits.Remove(deadUnit);
        enemyUnits.Remove(deadUnit);

        // Delay cleanup to allow death animation
        StartCoroutine(DelayedCleanup(deadUnit, 2f));
    }

    private System.Collections.IEnumerator DelayedCleanup(Unit unit, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (unit != null)
        {
            unit.OnDeath -= OnUnitDeath;
            unit.gameObject.SetActive(false);
            unit.transform.SetParent(poolParent);
        }
    }

    #endregion

    #region Pooling

    private GameObject GetFromPool(GameObject prefab)
    {
        if (prefabPools.TryGetValue(prefab, out var pool) && pool.Count > 0)
        {
            var obj = pool.Dequeue();
            // Reset unit state
            var unit = obj.GetComponent<Unit>();
            if (unit != null)
            {
                unit.ResetForPooling();
            }
            return obj;
        }

        return Instantiate(prefab);
    }

    private void ReturnToPool(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        instance.transform.SetParent(poolParent);

        if (!prefabPools.ContainsKey(prefab))
        {
            prefabPools[prefab] = new Queue<GameObject>();
        }
        prefabPools[prefab].Enqueue(instance);
    }

    #endregion

    #region Helpers

    private Vector3 GetRandomSpawnPosition(bool isPlayerUnit)
    {
        Transform[] spawnPoints = isPlayerUnit ? playerSpawnPoints : enemySpawnPoints;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"No spawn points configured for {(isPlayerUnit ? "player" : "enemy")} units");
            return transform.position;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 offset = Random.insideUnitCircle * spawnRadius;
        return spawnPoint.position + new Vector3(offset.x, offset.y, 0);
    }

    private Sprite CreatePlaceholderSprite(bool isPlayerUnit)
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color fillColor = isPlayerUnit ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.3f, 0.3f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = x < 2 || x >= size - 2 || y < 2 || y >= size - 2;
                texture.SetPixel(x, y, isBorder ? Color.white : fillColor);
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (playerSpawnPoints != null)
        {
            foreach (var point in playerSpawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, spawnRadius);
            }
        }

        Gizmos.color = Color.red;
        if (enemySpawnPoints != null)
        {
            foreach (var point in enemySpawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, spawnRadius);
            }
        }
    }
#endif
}
