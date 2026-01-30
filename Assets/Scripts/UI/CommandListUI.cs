using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI manager for the command selection list.
/// Handles arrow key navigation and visual highlighting.
/// </summary>
public class CommandListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CommandExecutor commandExecutor;
    [SerializeField] private Transform commandContainer;  // Parent for command panels (Content of ScrollView)
    [SerializeField] private GameObject commandPanelPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Selection")]
    [SerializeField] private int selectedIndex = 0;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.7f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.84f, 0f, 1f);  // Gold highlight
    [SerializeField] private Color executingColor = new Color(0.2f, 0.8f, 0.2f, 1f);  // Green when active

    [Header("Input")]
    [SerializeField] private bool useArrowKeys = true;
    [SerializeField] private KeyCode selectKey = KeyCode.Return;  // Enter to start command

    private List<CommandPanelUI> commandPanels = new List<CommandPanelUI>();
    private List<CommandData> commands = new List<CommandData>();
    private bool isExecuting;

    private void Start()
    {
        if (commandExecutor == null)
            commandExecutor = CommandExecutor.Instance;

        if (commandExecutor != null)
        {
            commandExecutor.OnAvailableCommandsChanged += RefreshCommandList;
            commandExecutor.OnCommandStarted += OnCommandStarted;
            commandExecutor.OnCommandCompleted += OnCommandCompleted;
            commandExecutor.OnCommandInterrupted += OnCommandInterrupted;

            // Initialize with current commands
            if (commandExecutor.AvailableCommands.Count > 0)
            {
                RefreshCommandList(commandExecutor.AvailableCommands);
            }
        }
    }

    private void OnDestroy()
    {
        if (commandExecutor != null)
        {
            commandExecutor.OnAvailableCommandsChanged -= RefreshCommandList;
            commandExecutor.OnCommandStarted -= OnCommandStarted;
            commandExecutor.OnCommandCompleted -= OnCommandCompleted;
            commandExecutor.OnCommandInterrupted -= OnCommandInterrupted;
        }
    }

    private void Update()
    {
        if (!useArrowKeys || commands.Count == 0) return;

        // Don't allow navigation while executing
        if (isExecuting) return;

        // Arrow key navigation
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SelectPrevious();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SelectNext();
        }

        // Enter to execute selected command
        if (Input.GetKeyDown(selectKey))
        {
            ExecuteSelected();
        }
    }

    /// <summary>
    /// Refresh the command list display
    /// </summary>
    private void RefreshCommandList(List<CommandData> newCommands)
    {
        commands = new List<CommandData>(newCommands);

        // Clear existing panels
        foreach (var panel in commandPanels)
        {
            if (panel != null)
                Destroy(panel.gameObject);
        }
        commandPanels.Clear();

        if (commandContainer == null || commandPanelPrefab == null) return;

        // Create new panels
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            GameObject panelObj = Instantiate(commandPanelPrefab, commandContainer);

            CommandPanelUI panel = panelObj.GetComponent<CommandPanelUI>();
            if (panel == null)
            {
                panel = panelObj.AddComponent<CommandPanelUI>();
            }

            panel.Initialize(command, i);
            panel.SetColors(normalColor, selectedColor, executingColor);
            commandPanels.Add(panel);
        }

        // Reset selection
        selectedIndex = 0;
        UpdateSelection();

        Debug.Log($"[CommandListUI] Refreshed with {commands.Count} commands");
    }

    /// <summary>
    /// Select previous command (up)
    /// </summary>
    public void SelectPrevious()
    {
        if (commands.Count == 0) return;

        selectedIndex--;
        if (selectedIndex < 0)
            selectedIndex = commands.Count - 1;  // Wrap around

        UpdateSelection();
        ScrollToSelected();
    }

    /// <summary>
    /// Select next command (down)
    /// </summary>
    public void SelectNext()
    {
        if (commands.Count == 0) return;

        selectedIndex++;
        if (selectedIndex >= commands.Count)
            selectedIndex = 0;  // Wrap around

        UpdateSelection();
        ScrollToSelected();
    }

    /// <summary>
    /// Execute the currently selected command
    /// </summary>
    public void ExecuteSelected()
    {
        if (commands.Count == 0 || commandExecutor == null) return;
        if (selectedIndex < 0 || selectedIndex >= commands.Count) return;

        commandExecutor.StartCommand(selectedIndex);
    }

    /// <summary>
    /// Select a specific command by index
    /// </summary>
    public void SelectCommand(int index)
    {
        if (index < 0 || index >= commands.Count) return;

        selectedIndex = index;
        UpdateSelection();
        ScrollToSelected();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < commandPanels.Count; i++)
        {
            if (commandPanels[i] != null)
            {
                commandPanels[i].SetSelected(i == selectedIndex);
            }
        }
    }

    private void ScrollToSelected()
    {
        if (scrollRect == null || commandPanels.Count == 0) return;
        if (selectedIndex < 0 || selectedIndex >= commandPanels.Count) return;

        // Calculate normalized position to scroll to
        float normalizedPos = 1f - ((float)selectedIndex / (commandPanels.Count - 1));
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
    }

    private void OnCommandStarted(CommandData command)
    {
        isExecuting = true;

        // Highlight the executing command
        for (int i = 0; i < commandPanels.Count; i++)
        {
            if (commands[i] == command)
            {
                commandPanels[i].SetExecuting(true);
            }
        }
    }

    private void OnCommandCompleted(CommandData command, float score, CommandResult result)
    {
        isExecuting = false;

        // Reset executing state
        foreach (var panel in commandPanels)
        {
            panel.SetExecuting(false);
        }
        UpdateSelection();
    }

    private void OnCommandInterrupted(CommandData command)
    {
        isExecuting = false;

        foreach (var panel in commandPanels)
        {
            panel.SetExecuting(false);
        }
        UpdateSelection();
    }

    // Public accessors
    public int SelectedIndex => selectedIndex;
    public CommandData SelectedCommand => (selectedIndex >= 0 && selectedIndex < commands.Count) ? commands[selectedIndex] : null;
}
