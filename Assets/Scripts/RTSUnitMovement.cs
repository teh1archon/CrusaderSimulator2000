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
}