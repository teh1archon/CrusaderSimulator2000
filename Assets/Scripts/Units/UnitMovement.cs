using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Movement component for units. Handles NavMeshAgent pathfinding.
/// Does NOT handle input directly - Unit.cs controls autonomous movement.
/// Can be used by debug tools or future direct-control modes if needed.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class UnitMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float combatStoppingDistance = 1.5f;  // Distance to stop when engaging enemy

    [Header("Debug")]
    [SerializeField] private bool drawPath = true;

    public NavMeshAgent Agent => agent;
    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > stoppingDistance;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Critical for 2D: disable rotation on the Y axis (up in 3D, but we're on XY plane)
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    /// <summary>
    /// Configure agent speed based on unit stats
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (agent != null)
        {
            agent.speed = speed;
        }
    }

    /// <summary>
    /// Move to a destination point
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent == null || !agent.isActiveAndEnabled) return;

        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    /// <summary>
    /// Move toward a target (used for combat engagement)
    /// </summary>
    public void MoveToTarget(Transform target, float attackRange)
    {
        if (agent == null || !agent.isActiveAndEnabled || target == null) return;

        agent.stoppingDistance = Mathf.Max(attackRange - 0.5f, combatStoppingDistance);
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    /// <summary>
    /// Stop all movement
    /// </summary>
    public void Stop()
    {
        if (agent == null) return;

        agent.ResetPath();
        agent.isStopped = true;
    }

    /// <summary>
    /// Check if unit has reached its destination
    /// </summary>
    public bool HasReachedDestination()
    {
        if (agent == null || !agent.isActiveAndEnabled) return true;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
        return false;
    }

    /// <summary>
    /// Check if unit is within attack range of target
    /// </summary>
    public bool IsInRange(Transform target, float range)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= range;
    }

    /// <summary>
    /// Get distance to a target
    /// </summary>
    public float GetDistanceTo(Vector3 position)
    {
        return Vector3.Distance(transform.position, position);
    }

    /// <summary>
    /// Warp the unit to a position (bypasses pathfinding)
    /// </summary>
    public void WarpTo(Vector3 position)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }
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

    // Note: Sprite facing is handled by Unit.cs to avoid duplicate logic
}
