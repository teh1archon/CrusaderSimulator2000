using UnityEngine;

/// <summary>
/// Temporary debug script to diagnose animator issues.
/// Add to a unit and check the console for animation info.
/// Remove after debugging.
/// </summary>
public class AnimatorDebug : MonoBehaviour
{
    private Animator animator;
    private float lastNormalizedTime;
    private string lastStateName;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"[AnimatorDebug] {name} has no Animator!");
            return;
        }

        // Log animator info
        Debug.Log($"[AnimatorDebug] {name} - Animator found");
        Debug.Log($"[AnimatorDebug] Speed: {animator.speed}");
        Debug.Log($"[AnimatorDebug] UpdateMode: {animator.updateMode}");
        Debug.Log($"[AnimatorDebug] CullingMode: {animator.cullingMode}");

        // Log all parameters
        Debug.Log($"[AnimatorDebug] Parameters ({animator.parameterCount}):");
        foreach (var param in animator.parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
        }
    }

    private void Update()
    {
        if (animator == null) return;

        // Get current state info
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        string stateName = GetStateName(stateInfo);

        // Log if state changed
        if (stateName != lastStateName)
        {
            Debug.Log($"[AnimatorDebug] {name} - State changed to: {stateName}");
            lastStateName = stateName;
        }

        // Check if animation is progressing
        float normalizedTime = stateInfo.normalizedTime % 1f; // 0-1 progress through clip
        if (Mathf.Abs(normalizedTime - lastNormalizedTime) > 0.01f)
        {
            // Animation IS progressing
            lastNormalizedTime = normalizedTime;
        }
        else if (Time.frameCount % 60 == 0) // Log every 60 frames if stuck
        {
            Debug.LogWarning($"[AnimatorDebug] {name} - Animation may be stuck at {normalizedTime:F2} (state: {stateName}, length: {stateInfo.length:F2}s, speed: {stateInfo.speed})");
        }
    }

    private string GetStateName(AnimatorStateInfo info)
    {
        // Try to identify common states by hash
        if (info.IsName("Idle")) return "Idle";
        if (info.IsName("Run")) return "Run";
        if (info.IsName("Attack")) return "Attack";
        if (info.IsName("Death")) return "Death";
        if (info.IsName("Standing")) return "Standing";
        if (info.IsName("Idle_Ready")) return "Idle_Ready";
        if (info.IsName("Attack_idle")) return "Attack_idle";
        if (info.IsName("hurt")) return "hurt";
        return $"Unknown ({info.shortNameHash})";
    }
}
