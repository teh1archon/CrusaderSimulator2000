using UnityEngine;

/// <summary>
/// Test component for the rhythm system.
/// Allows starting melodies via inspector or keyboard for debugging.
/// </summary>
public class RhythmTestRunner : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private MelodyData testMelody;
    [SerializeField] private KeyCode startKey = KeyCode.Return;
    [SerializeField] private KeyCode stopKey = KeyCode.Escape;

    [Header("Auto-Start")]
    [SerializeField] private bool autoStartOnPlay = false;
    [SerializeField] private float autoStartDelay = 1f;

    private RhythmGameManager rhythmManager;

    private void Start()
    {
        rhythmManager = RhythmGameManager.Instance;

        if (rhythmManager == null)
        {
            Debug.LogError("RhythmTestRunner: No RhythmGameManager found! Add one to the scene.");
            return;
        }

        // Subscribe to events for logging
        rhythmManager.OnMelodyStarted += m => Debug.Log($"[Test] Melody started: {m.melodyName}");
        rhythmManager.OnMelodyCompleted += score => Debug.Log($"[Test] Melody completed! Score: {score}");
        rhythmManager.OnMelodyInterrupted += () => Debug.Log("[Test] Melody interrupted!");

        if (autoStartOnPlay && testMelody != null)
        {
            Invoke(nameof(StartTestMelody), autoStartDelay);
        }
    }

    private void Update()
    {
        if (rhythmManager == null) return;

        // Start melody with Enter key
        if (Input.GetKeyDown(startKey))
        {
            StartTestMelody();
        }

        // Stop melody with Escape key
        if (Input.GetKeyDown(stopKey))
        {
            StopMelody();
        }
    }

    [ContextMenu("Start Test Melody")]
    public void StartTestMelody()
    {
        if (testMelody == null)
        {
            Debug.LogWarning("RhythmTestRunner: No test melody assigned!");
            return;
        }

        if (rhythmManager == null)
        {
            rhythmManager = RhythmGameManager.Instance;
            if (rhythmManager == null)
            {
                Debug.LogError("RhythmTestRunner: No RhythmGameManager found!");
                return;
            }
        }

        Debug.Log($"Starting test melody: {testMelody.melodyName}");
        rhythmManager.StartMelody(testMelody);
    }

    [ContextMenu("Stop Melody")]
    public void StopMelody()
    {
        if (rhythmManager != null && rhythmManager.IsPlaying)
        {
            rhythmManager.InterruptMelody();
        }
    }

    [ContextMenu("Create Test Melody")]
    public void CreateTestMelodyInMemory()
    {
        // Create a simple test melody at runtime for quick testing
        testMelody = ScriptableObject.CreateInstance<MelodyData>();
        testMelody.melodyName = "Quick Test";
        testMelody.bpm = 120;
        testMelody.duration = 5f;

        // Simple pattern: one note per second, cycling through lanes
        testMelody.notes.Add(new MelodyData.NoteEntry(1.0f, NoteLane.Key5));
        testMelody.notes.Add(new MelodyData.NoteEntry(1.5f, NoteLane.KeyT));
        testMelody.notes.Add(new MelodyData.NoteEntry(2.0f, NoteLane.KeyG));
        testMelody.notes.Add(new MelodyData.NoteEntry(2.5f, NoteLane.KeyB));
        testMelody.notes.Add(new MelodyData.NoteEntry(3.0f, NoteLane.KeySpace));
        testMelody.notes.Add(new MelodyData.NoteEntry(3.5f, NoteLane.Key5));
        testMelody.notes.Add(new MelodyData.NoteEntry(4.0f, NoteLane.KeyT));
        testMelody.notes.Add(new MelodyData.NoteEntry(4.5f, NoteLane.KeyG));

        Debug.Log("Created test melody with 8 notes");
    }

    private void OnGUI()
    {
        // Debug overlay
        GUILayout.BeginArea(new Rect(10, 10, 300, 150));

        GUILayout.Label("=== Rhythm Test Runner ===");
        GUILayout.Label($"Press [{startKey}] to start melody");
        GUILayout.Label($"Press [{stopKey}] to stop");
        GUILayout.Label("");
        GUILayout.Label("Keys: 5, T, G, B, Space");

        if (rhythmManager != null && rhythmManager.IsPlaying)
        {
            GUILayout.Label($"Playing: {rhythmManager.GetProgress():P0}");
            GUILayout.Label($"Score: {rhythmManager.CurrentScore}");
        }
        else
        {
            GUILayout.Label("Status: Idle");
        }

        GUILayout.EndArea();
    }
}
