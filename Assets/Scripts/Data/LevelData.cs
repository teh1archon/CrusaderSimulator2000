using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a battle level with obstacles, units, and available commands.
/// Create assets via: Right-click > Create > Cantus > Level
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "Cantus/Level")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    [Tooltip("Display name for this level")]
    public string levelName;

    [Tooltip("Background sprite for the battlefield")]
    public Sprite backgroundSprite;

    [Header("Battlefield Size")]
    [Tooltip("Size of the playable area")]
    public Vector2 battlefieldSize = new Vector2(20f, 15f);

    [Header("Obstacles")]
    [Tooltip("Static obstacles that block movement")]
    public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();

    [Header("Player Units")]
    [Tooltip("Player units and their spawn positions (can be overridden by campaign)")]
    public List<UnitSpawnEntry> playerUnits = new List<UnitSpawnEntry>();

    [Header("Enemy Units")]
    [Tooltip("Enemy units and their spawn positions")]
    public List<UnitSpawnEntry> enemyUnits = new List<UnitSpawnEntry>();

    [Header("Available Commands")]
    [Tooltip("Commands the player can use in this level")]
    public List<CommandData> availableCommands = new List<CommandData>();

    /// <summary>
    /// An obstacle placed in the level
    /// </summary>
    [System.Serializable]
    public struct ObstacleEntry
    {
        [Tooltip("Obstacle prefab to instantiate")]
        public GameObject prefab;

        [Tooltip("World position")]
        public Vector3 position;

        [Tooltip("Rotation")]
        public Quaternion rotation;

        [Tooltip("Scale")]
        public Vector3 scale;

        public ObstacleEntry(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.prefab = prefab;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public ObstacleEntry(GameObject prefab, Vector3 position)
        {
            this.prefab = prefab;
            this.position = position;
            this.rotation = Quaternion.identity;
            this.scale = Vector3.one;
        }
    }

    /// <summary>
    /// A unit spawn point
    /// </summary>
    [System.Serializable]
    public struct UnitSpawnEntry
    {
        [Tooltip("Unit prefab to spawn")]
        public GameObject unitPrefab;

        [Tooltip("World position to spawn at")]
        public Vector3 spawnPosition;

        public UnitSpawnEntry(GameObject prefab, Vector3 position)
        {
            this.unitPrefab = prefab;
            this.spawnPosition = position;
        }
    }

    /// <summary>
    /// Get total unit count for a team
    /// </summary>
    public int GetPlayerUnitCount() => playerUnits.Count;
    public int GetEnemyUnitCount() => enemyUnits.Count;

    /// <summary>
    /// Validate level data
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(levelName))
        {
            Debug.LogWarning($"Level {name} has no name assigned");
            return false;
        }

        if (playerUnits.Count == 0)
        {
            Debug.LogWarning($"Level {levelName} has no player units");
            return false;
        }

        if (enemyUnits.Count == 0)
        {
            Debug.LogWarning($"Level {levelName} has no enemy units");
            return false;
        }

        return true;
    }
}
