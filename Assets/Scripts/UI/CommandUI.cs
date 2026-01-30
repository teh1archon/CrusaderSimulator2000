using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for command selection and feedback.
/// Displays available commands and execution results.
/// </summary>
public class CommandUI : MonoBehaviour
{
    [Header("Command List")]
    [SerializeField] private Transform commandListContainer;
    [SerializeField] private GameObject commandButtonPrefab;
    [SerializeField] private int maxVisibleCommands = 6;

    [Header("Morale Display")]
    [SerializeField] private Slider moraleSlider;
    [SerializeField] private Image moraleFill;
    [SerializeField] private TextMeshProUGUI moraleText;

    [Header("Feedback Display")]
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Colors")]
    [SerializeField] private Color highMoraleColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color normalMoraleColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color lowMoraleColor = new Color(0.8f, 0.2f, 0.2f);

    private CommandExecutor commandExecutor;
    private MoraleManager moraleManager;
    private List<GameObject> commandButtons = new List<GameObject>();
    private float feedbackTimer;

    private void Start()
    {
        commandExecutor = CommandExecutor.Instance;
        moraleManager = MoraleManager.Instance;

        if (commandExecutor != null)
        {
            commandExecutor.OnAvailableCommandsChanged += RefreshCommandList;
            commandExecutor.OnCommandCompleted += ShowCommandFeedback;
            commandExecutor.OnCommandInterrupted += ShowInterruptFeedback;
        }

        if (moraleManager != null)
        {
            moraleManager.OnMoraleChanged += UpdateMoraleDisplay;
            UpdateMoraleDisplay(moraleManager.CurrentMorale, moraleManager.MaxMorale);
        }

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (commandExecutor != null)
        {
            commandExecutor.OnAvailableCommandsChanged -= RefreshCommandList;
            commandExecutor.OnCommandCompleted -= ShowCommandFeedback;
            commandExecutor.OnCommandInterrupted -= ShowInterruptFeedback;
        }

        if (moraleManager != null)
        {
            moraleManager.OnMoraleChanged -= UpdateMoraleDisplay;
        }
    }

    private void Update()
    {
        // Number key shortcuts (1-6 for commands)
        for (int i = 0; i < Mathf.Min(maxVisibleCommands, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectCommand(i);
            }
        }

        // Fade feedback text
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Refresh the command button list
    /// </summary>
    private void RefreshCommandList(List<CommandData> commands)
    {
        // Clear existing buttons
        foreach (var button in commandButtons)
        {
            Destroy(button);
        }
        commandButtons.Clear();

        if (commandListContainer == null) return;

        // Create new buttons
        for (int i = 0; i < Mathf.Min(commands.Count, maxVisibleCommands); i++)
        {
            var command = commands[i];
            int index = i; // Capture for lambda

            GameObject buttonObj;
            if (commandButtonPrefab != null)
            {
                buttonObj = Instantiate(commandButtonPrefab, commandListContainer);
            }
            else
            {
                buttonObj = CreateDefaultButton(commandListContainer);
            }

            // Set up button text
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"{i + 1}. {command.commandName}";
            }
            else
            {
                var legacyText = buttonObj.GetComponentInChildren<Text>();
                if (legacyText != null)
                    legacyText.text = $"{i + 1}. {command.commandName}";
            }

            // Set up button click
            var button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectCommand(index));
            }

            commandButtons.Add(buttonObj);
        }
    }

    /// <summary>
    /// Select and start a command
    /// </summary>
    public void SelectCommand(int index)
    {
        if (commandExecutor == null) return;

        if (commandExecutor.IsExecutingCommand)
        {
            Debug.Log("Already executing a command - will interrupt");
        }

        commandExecutor.StartCommand(index);
    }

    /// <summary>
    /// Update the morale bar display
    /// </summary>
    private void UpdateMoraleDisplay(int current, int max)
    {
        if (moraleSlider != null)
        {
            moraleSlider.maxValue = max;
            moraleSlider.value = current;
        }

        if (moraleText != null)
        {
            moraleText.text = $"{current}/{max}";
        }

        if (moraleFill != null)
        {
            float percent = (float)current / max;
            if (percent <= 0.3f)
                moraleFill.color = lowMoraleColor;
            else if (percent >= 0.8f)
                moraleFill.color = highMoraleColor;
            else
                moraleFill.color = normalMoraleColor;
        }
    }

    /// <summary>
    /// Show feedback for completed command
    /// </summary>
    private void ShowCommandFeedback(CommandData command, float score, CommandResult result)
    {
        if (feedbackText == null) return;

        string resultText = result switch
        {
            CommandResult.Perfect => "<color=yellow>PERFECT!</color>",
            CommandResult.Good => "<color=green>Good!</color>",
            CommandResult.Weak => "<color=orange>Weak...</color>",
            CommandResult.Failed => "<color=red>FAILED!</color>",
            _ => ""
        };

        feedbackText.text = $"{command.commandName}: {resultText} ({score:F0}%)";
        feedbackText.gameObject.SetActive(true);
        feedbackTimer = feedbackDuration;
    }

    /// <summary>
    /// Show feedback for interrupted command
    /// </summary>
    private void ShowInterruptFeedback(CommandData command)
    {
        if (feedbackText == null) return;

        feedbackText.text = $"<color=red>{command.commandName} INTERRUPTED!</color>";
        feedbackText.gameObject.SetActive(true);
        feedbackTimer = feedbackDuration;
    }

    /// <summary>
    /// Create a default button if no prefab assigned
    /// </summary>
    private GameObject CreateDefaultButton(Transform parent)
    {
        var buttonObj = new GameObject("CommandButton");
        buttonObj.transform.SetParent(parent, false);

        var rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 40);

        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 16;
        tmp.color = Color.white;

        return buttonObj;
    }
}
