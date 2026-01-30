# NavMeshPlus 2D Setup Guide for RTS

## 1. Installation

### Option A: Git URL (Recommended)
In Unity, go to **Window → Package Manager → + → Add package from git URL**:
```
https://github.com/h8man/NavMeshPlus.git
```

### Option B: Manual
Clone/download from `https://github.com/h8man/NavMeshPlus` and copy into your `Assets/` folder.

> **Note:** Also ensure Unity's AI Navigation package is installed (Window → Package Manager → Unity Registry → AI Navigation)

---

## 2. Scene Setup

### Create the Navigation Surface

1. Create an empty GameObject in the scene root, name it `NavMesh2D`
2. Add component: **Navigation Surface** (from AI Navigation)
3. Add component: **NavMeshCollectSources2d** (from NavMeshPlus)
4. On the **Navigation Surface** component, click **"Rotate Surface to XY"** button
   - This orients the NavMesh for standard 2D camera setup

### Configure the Surface

On the **Navigation Surface** component:
- **Agent Type**: Humanoid (or create custom in Window → AI → Navigation)
- **Collect Objects**: Volume (recommended) or Children
- **Include Layers**: Set to layers containing your obstacles
- **Use Geometry**: Physics Colliders (works with Collider2D)

On **NavMeshCollectSources2d**:
- **Overide by Grid**: Enable if using Tilemaps
- **Compound Collider Sources**: Enable for cleaner mesh from multiple small colliders

---

## 3. Mark Obstacles

For any GameObject that should block movement:

1. Add a **Collider2D** (BoxCollider2D, PolygonCollider2D, etc.)
2. Add component: **Navigation Modifier**
3. Set **Area Type** to "Not Walkable"

For different terrain costs (e.g., roads vs rough terrain):
- Create custom areas in **Window → AI → Navigation → Areas**
- Assign area type in Navigation Modifier

---

## 4. Bake the NavMesh

1. Select your `NavMesh2D` GameObject
2. In the **Navigation Surface** inspector, click **Bake**
3. You should see a blue overlay showing walkable areas in Scene view

### Runtime Baking (for dynamic obstacles)

```csharp
using UnityEngine;
using UnityEngine.AI;

public class NavMeshRuntimeBaker : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;
    
    // Call this when obstacles change
    public void RebakeNavMesh()
    {
        surface.BuildNavMesh();
    }
    
    // Async version for large maps
    public void RebakeNavMeshAsync()
    {
        surface.UpdateNavMesh(surface.navMeshData);
    }
}
```

---

## 5. Unit Setup

### Create a Unit Prefab

1. Create a new GameObject (e.g., Sprite or your unit visuals)
2. Add component: **NavMesh Agent**
3. Configure the agent:

```
Agent Type: Humanoid (match your surface)
Base Offset: 0
Speed: 3.5
Angular Speed: 120
Acceleration: 8
Stopping Distance: 0.1
Auto Braking: true
Radius: 0.5
Height: 1
```

> **Critical:** The NavMeshAgent works in 3D internally. For 2D, your unit's transform.position.z should be 0, and the NavMesh surface handles the XY→XZ conversion.

---

## 6. Basic Movement Controller

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RTSUnitMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Camera mainCamera;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private bool drawPath = true;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        
        // Critical for 2D: disable rotation on the Y axis (up in 3D, but we're on XY plane)
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        // Right-click to move (RTS standard)
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = 0f; // Ensure we stay on the 2D plane
            
            MoveTo(worldPos);
        }
    }

    public void MoveTo(Vector3 destination)
    {
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(destination);
    }

    public void Stop()
    {
        agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (!drawPath || agent == null || !agent.hasPath) return;

        Gizmos.color = Color.green;
        var path = agent.path;
        
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
        }
    }
}
```

---

## 7. RTS Selection & Group Movement

For commanding multiple units (RTS-style):

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RTSUnitCommander : MonoBehaviour
{
    [SerializeField] private LayerMask unitLayer;
    [SerializeField] private LayerMask groundLayer;
    
    private List<RTSUnitMovement> selectedUnits = new List<RTSUnitMovement>();
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Left-click to select
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectUnit();
        }
        
        // Right-click to command movement
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            CommandMovement();
        }
    }

    private void TrySelectUnit()
    {
        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, unitLayer);
        
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            selectedUnits.Clear();
        }
        
        if (hit.collider != null)
        {
            var unit = hit.collider.GetComponent<RTSUnitMovement>();
            if (unit != null && !selectedUnits.Contains(unit))
            {
                selectedUnits.Add(unit);
            }
        }
    }

    private void CommandMovement()
    {
        Vector3 targetPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        targetPos.z = 0f;
        
        if (selectedUnits.Count == 1)
        {
            selectedUnits[0].MoveTo(targetPos);
        }
        else
        {
            // Formation movement - spread units around target point
            MoveInFormation(targetPos);
        }
    }

    private void MoveInFormation(Vector3 center)
    {
        float spacing = 1.2f; // Adjust based on unit size
        int unitsPerRow = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
        
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int row = i / unitsPerRow;
            int col = i % unitsPerRow;
            
            // Center the formation
            float offsetX = (col - (unitsPerRow - 1) / 2f) * spacing;
            float offsetY = (row - (selectedUnits.Count / unitsPerRow - 1) / 2f) * spacing;
            
            Vector3 formationPos = center + new Vector3(offsetX, offsetY, 0f);
            selectedUnits[i].MoveTo(formationPos);
        }
    }
}
```

---

## 8. Handling Sprite Facing Direction

NavMeshAgent doesn't auto-rotate for 2D sprites. Handle it manually:

```csharp
// Add to RTSUnitMovement class

[Header("Sprite Facing")]
[SerializeField] private bool flipSprite = true;
[SerializeField] private SpriteRenderer spriteRenderer;

private void LateUpdate()
{
    if (!flipSprite || spriteRenderer == null) return;
    
    if (agent.velocity.sqrMagnitude > 0.01f)
    {
        // Flip sprite based on movement direction
        spriteRenderer.flipX = agent.velocity.x < 0;
    }
}
```

Or for 8-directional sprites:

```csharp
[Header("8-Direction Animation")]
[SerializeField] private Animator animator;

private void LateUpdate()
{
    if (agent.velocity.sqrMagnitude > 0.01f)
    {
        Vector2 dir = agent.velocity.normalized;
        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
    }
    else
    {
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 0);
    }
}
```

---

## 9. Common Issues & Fixes

### Unit not moving / path not found
- Check that the NavMesh is baked (blue overlay visible)
- Verify unit's position.z is 0
- Ensure agent type matches surface agent type
- Check that destination is on the NavMesh: `NavMesh.SamplePosition()`

### Unit slides or drifts
- Set `agent.updateRotation = false`
- Set `agent.updateUpAxis = false`

### Unit teleports to wrong position
- The NavMeshAgent internally uses XZ plane. NavMeshPlus handles conversion, but if you're manually setting positions, ensure you're consistent.

### Obstacle not blocking
- Obstacle needs a Collider2D
- Obstacle needs Navigation Modifier with "Not Walkable" area
- Re-bake the NavMesh after adding obstacles

---

## 10. Project Structure Recommendation

```
Assets/
├── Scripts/
│   ├── Navigation/
│   │   ├── RTSUnitMovement.cs
│   │   ├── RTSUnitCommander.cs
│   │   └── NavMeshRuntimeBaker.cs
│   └── ...
├── Prefabs/
│   ├── Units/
│   │   └── BasicUnit.prefab  (has NavMeshAgent + RTSUnitMovement)
│   └── ...
└── Scenes/
    └── ...
```

---

## Next Steps

Once basic movement works:

1. **Add local avoidance** (N:ORCA) if units stack badly in groups
2. **Implement attack-move** with target following
3. **Add NavMeshObstacle** for dynamic blocking objects (e.g., buildings being constructed)
4. **Optimize for mobile** with async baking and path caching

---

## License Notes

- **NavMeshPlus**: MIT License ✓ (can redistribute in open source)
- **Unity AI Navigation**: Part of Unity, follows Unity's licensing
- Your derivative code: Can be any license compatible with MIT

