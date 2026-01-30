#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.IO;

/// <summary>
/// Editor tool to generate unit prefabs from UnitClassData ScriptableObjects.
/// Access via: Tools > Cantus Crucis > Generate Unit Prefabs
/// Or right-click a UnitClassData asset > Create Unit Prefab
/// </summary>
public class UnitPrefabGenerator : Editor
{
    private const string PREFABS_PATH = "Assets/Prefabs/Units";

    [MenuItem("Tools/Cantus Crucis/Generate Unit Prefabs From All UnitClassData")]
    public static void GenerateAllUnitPrefabs()
    {
        EnsureDirectoryExists(PREFABS_PATH);

        string[] guids = AssetDatabase.FindAssets("t:UnitClassData");
        int created = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitClassData classData = AssetDatabase.LoadAssetAtPath<UnitClassData>(path);

            if (classData != null)
            {
                if (CreateUnitPrefab(classData))
                    created++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {created} unit prefabs in {PREFABS_PATH}");
    }

    [MenuItem("Assets/Create Unit Prefab", false, 0)]
    private static void CreatePrefabFromSelected()
    {
        if (Selection.activeObject is UnitClassData classData)
        {
            EnsureDirectoryExists(PREFABS_PATH);

            if (CreateUnitPrefab(classData))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Created prefab for {classData.className}");
            }
        }
    }

    [MenuItem("Assets/Create Unit Prefab", true)]
    private static bool ValidateCreatePrefab()
    {
        return Selection.activeObject is UnitClassData;
    }

    /// <summary>
    /// Create a unit prefab from UnitClassData
    /// </summary>
    public static bool CreateUnitPrefab(UnitClassData classData)
    {
        if (classData == null)
        {
            Debug.LogError("Cannot create prefab: UnitClassData is null");
            return false;
        }

        string prefabPath = $"{PREFABS_PATH}/{classData.className}.prefab";

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            Debug.Log($"Prefab for {classData.className} already exists at {prefabPath}");
            return false;
        }

        // Create the unit GameObject
        GameObject unitObj = new GameObject(classData.className);

        try
        {
            // Add NavMeshAgent (configured for 2D)
            NavMeshAgent agent = unitObj.AddComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.radius = 0.3f;
            agent.height = 0.1f;  // Thin for 2D
            agent.baseOffset = 0f;
            agent.speed = 2f + (classData.dexterity * 0.05f);
            agent.angularSpeed = 0f;  // No rotation for 2D
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.1f;
            agent.autoBraking = true;

            // Add SpriteRenderer
            SpriteRenderer sr = unitObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            if (classData.unitSprite != null)
            {
                sr.sprite = classData.unitSprite;
            }
            else
            {
                // Create placeholder sprite
                sr.sprite = CreatePlaceholderSprite();
            }

            // Add Unit component and assign class data
            Unit unit = unitObj.AddComponent<Unit>();
            SerializedObject serializedUnit = new SerializedObject(unit);
            SerializedProperty classDataProp = serializedUnit.FindProperty("classData");
            classDataProp.objectReferenceValue = classData;
            serializedUnit.ApplyModifiedPropertiesWithoutUndo();

            // Add UnitVisuals
            unitObj.AddComponent<UnitVisuals>();

            // Add Collider for selection and combat
            CircleCollider2D collider = unitObj.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            // Set layer to "Units" if it exists
            int unitsLayer = LayerMask.NameToLayer("Units");
            if (unitsLayer != -1)
            {
                unitObj.layer = unitsLayer;
            }

            // Save as prefab
            EnsureDirectoryExists(PREFABS_PATH);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(unitObj, prefabPath);

            Debug.Log($"Created prefab: {prefabPath}");
            return true;
        }
        finally
        {
            // Clean up the temporary GameObject
            DestroyImmediate(unitObj);
        }
    }

    /// <summary>
    /// Update an existing prefab with new UnitClassData values
    /// </summary>
    [MenuItem("Assets/Update Unit Prefab From ClassData", false, 1)]
    private static void UpdatePrefabFromClassData()
    {
        if (Selection.activeObject is UnitClassData classData)
        {
            string prefabPath = $"{PREFABS_PATH}/{classData.className}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning($"No existing prefab found at {prefabPath}. Creating new one.");
                CreateUnitPrefab(classData);
                return;
            }

            // Load prefab for editing
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                GameObject prefabRoot = editingScope.prefabContentsRoot;

                // Update NavMeshAgent speed
                NavMeshAgent agent = prefabRoot.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = 2f + (classData.dexterity * 0.05f);
                }

                // Update sprite if set
                SpriteRenderer sr = prefabRoot.GetComponent<SpriteRenderer>();
                if (sr != null && classData.unitSprite != null)
                {
                    sr.sprite = classData.unitSprite;
                }

                // Verify Unit component has correct classData reference
                Unit unit = prefabRoot.GetComponent<Unit>();
                if (unit != null)
                {
                    SerializedObject serializedUnit = new SerializedObject(unit);
                    SerializedProperty classDataProp = serializedUnit.FindProperty("classData");
                    classDataProp.objectReferenceValue = classData;
                    serializedUnit.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Updated prefab: {prefabPath}");
        }
    }

    [MenuItem("Assets/Update Unit Prefab From ClassData", true)]
    private static bool ValidateUpdatePrefab()
    {
        return Selection.activeObject is UnitClassData;
    }

    private static Sprite CreatePlaceholderSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color fillColor = new Color(0.5f, 0.5f, 0.5f);

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

    private static void EnsureDirectoryExists(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            // Create parent directories if needed
            string[] parts = path.Split('/');
            string currentPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }
                currentPath = nextPath;
            }
        }
    }
}
#endif
