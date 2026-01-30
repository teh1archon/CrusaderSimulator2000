using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test runner for the Command system.
/// Sets up all required managers and allows testing command execution.
/// </summary>
public class CommandTestRunner : MonoBehaviour
{
    [Header("Commands to Test")]
    [SerializeField] private List<CommandData> testCommands = new List<CommandData>();

    [Header("Auto-Setup")]
    [Tooltip("Automatically create required managers if missing")]
    [SerializeField] private bool autoCreateManagers = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = true;

    private CommandExecutor commandExecutor;
    private MoraleManager moraleManager;
    private RhythmGameManager rhythmManager;

    private void Start()
    {
        SetupManagers();
        SetupCommands();
    }

    private void SetupManagers()
    {
        // Find or create RhythmGameManager
        rhythmManager = RhythmGameManager.Instance;
        if (rhythmManager == null)
        {
            rhythmManager = FindFirstObjectByType<RhythmGameManager>();
        }
        if (rhythmManager == null && autoCreateManagers)
        {
            var obj = new GameObject("RhythmGameManager");
            rhythmManager = obj.AddComponent<RhythmGameManager>();
            Debug.Log("CommandTestRunner: Created RhythmGameManager");
        }

        // Find or create MoraleManager
        moraleManager = MoraleManager.Instance;
        if (moraleManager == null)
        {
            moraleManager = FindFirstObjectByType<MoraleManager>();
        }
        if (moraleManager == null && autoCreateManagers)
        {
            var obj = new GameObject("MoraleManager");
            moraleManager = obj.AddComponent<MoraleManager>();
            Debug.Log("CommandTestRunner: Created MoraleManager");
        }

        // Find or create CommandExecutor
        commandExecutor = CommandExecutor.Instance;
        if (commandExecutor == null)
        {
            commandExecutor = FindFirstObjectByType<CommandExecutor>();
        }
        if (commandExecutor == null && autoCreateManagers)
        {
            var obj = new GameObject("CommandExecutor");
            commandExecutor = obj.AddComponent<CommandExecutor>();
            Debug.Log("CommandTestRunner: Created CommandExecutor");
        }

        // Subscribe to events for debug logging
        if (commandExecutor != null)
        {
            commandExecutor.OnCommandStarted += cmd => Debug.Log($"[TEST] Command Started: {cmd.commandName}");
            commandExecutor.OnCommandCompleted += (cmd, score, result) =>
                Debug.Log($"[TEST] Command Completed: {cmd.commandName} | Score: {score:F1}% | Result: {result}");
            commandExecutor.OnCommandInterrupted += cmd =>
                Debug.Log($"[TEST] Command Interrupted: {cmd.commandName}");
        }

        if (moraleManager != null)
        {
            moraleManager.OnMoraleChanged += (current, max) =>
                Debug.Log($"[TEST] Morale: {current}/{max}");
            moraleManager.OnMoraleStateChanged += state =>
                Debug.Log($"[TEST] Morale State: {state}");
        }
    }

    private void SetupCommands()
    {
        if (commandExecutor == null) return;

        if (testCommands.Count > 0)
        {
            commandExecutor.SetAvailableCommands(testCommands);
            Debug.Log($"CommandTestRunner: Loaded {testCommands.Count} commands");
        }
        else
        {
            Debug.LogWarning("CommandTestRunner: No test commands assigned. Assign commands in Inspector or run Tools > Cantus Crucis > Create Sample Assets");
        }
    }

    private void Update()
    {
        // Number keys 1-9 to start commands
        for (int i = 0; i < Mathf.Min(testCommands.Count, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                StartCommand(i);
            }
        }

        // Escape to interrupt
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            InterruptCommand();
        }

        // R to reset morale
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetMorale();
        }

        // +/- to adjust morale manually
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus))
        {
            AdjustMorale(10);
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            AdjustMorale(-10);
        }
    }

    public void StartCommand(int index)
    {
        if (commandExecutor == null)
        {
            Debug.LogError("No CommandExecutor available!");
            return;
        }

        if (index < 0 || index >= testCommands.Count)
        {
            Debug.LogWarning($"Invalid command index: {index}");
            return;
        }

        commandExecutor.StartCommand(testCommands[index]);
    }

    public void InterruptCommand()
    {
        if (commandExecutor != null && commandExecutor.IsExecutingCommand)
        {
            commandExecutor.InterruptCommand();
        }
    }

    public void ResetMorale()
    {
        if (moraleManager != null)
        {
            moraleManager.ResetMorale();
            Debug.Log("Morale reset!");
        }
    }

    public void AdjustMorale(int amount)
    {
        if (moraleManager != null)
        {
            moraleManager.ChangeMorale(amount);
        }
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 400));
        GUILayout.Box("Command Test Runner");

        // Show morale
        if (moraleManager != null)
        {
            GUILayout.Label($"Morale: {moraleManager.CurrentMorale}/{moraleManager.MaxMorale} ({moraleManager.CurrentState})");
            GUILayout.Label("R: Reset | +/-: Adjust Morale");
        }

        GUILayout.Space(10);

        // Show available commands
        GUILayout.Label("Commands (press 1-9 to execute):");
        for (int i = 0; i < testCommands.Count && i < 9; i++)
        {
            var cmd = testCommands[i];
            if (cmd != null)
            {
                string status = "";
                if (commandExecutor != null && commandExecutor.ActiveCommand == cmd)
                    status = " [ACTIVE]";
                GUILayout.Label($"  {i + 1}. {cmd.commandName}{status}");
            }
        }

        GUILayout.Space(10);

        // Show countdown state
        if (rhythmManager != null && rhythmManager.IsCountingDown)
        {
            int countdown = Mathf.CeilToInt(rhythmManager.CountdownTime);
            GUILayout.Box($"GET READY: {countdown}");
            GUILayout.Label("ESC: Interrupt");
        }
        // Show rhythm state
        else if (rhythmManager != null && rhythmManager.IsPlaying)
        {
            GUILayout.Label($"Playing: {rhythmManager.GetProgress() * 100:F0}%");
            GUILayout.Label($"Score: {rhythmManager.CurrentScore}");
            GUILayout.Label("ESC: Interrupt");
        }

        GUILayout.Space(10);
        GUILayout.Label("Rhythm Keys: 5, T, G, B, Space");

        GUILayout.EndArea();
    }
}
