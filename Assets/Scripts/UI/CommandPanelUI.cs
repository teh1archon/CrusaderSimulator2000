using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single command panel in the command list.
/// Displays command info and handles selection highlighting.
/// </summary>
public class CommandPanelUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image[] attributeImages;  // 3 attribute icons

    [Header("State")]
    [SerializeField] private bool isSelected;
    [SerializeField] private bool isExecuting;

    // Colors set by parent
    private Color normalColor;
    private Color selectedColor;
    private Color executingColor;

    private CommandData commandData;
    private int commandIndex;

    /// <summary>
    /// Initialize the panel with command data
    /// </summary>
    public void Initialize(CommandData command, int index)
    {
        commandData = command;
        commandIndex = index;

        // Auto-find components if not assigned
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (nameText == null)
            nameText = GetComponentInChildren<TextMeshProUGUI>();

        // Find icon image (first child image that isn't background)
        if (iconImage == null)
        {
            var images = GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img != backgroundImage && img.name.Contains("icon"))
                {
                    iconImage = img;
                    break;
                }
            }
        }

        // Update display
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (commandData == null) return;

        // Set name
        if (nameText != null)
        {
            nameText.text = commandData.commandName;
        }

        // Set icon if available
        if (iconImage != null && commandData.icon != null)
        {
            iconImage.sprite = commandData.icon;
            iconImage.enabled = true;
        }
        else if (iconImage != null)
        {
            // No icon, could show placeholder or effect type indicator
            iconImage.enabled = commandData.icon != null;
        }

        // Attribute images could show command effect type, duration, etc.
        // For now, leave them as placeholders
    }

    /// <summary>
    /// Set the color scheme for this panel
    /// </summary>
    public void SetColors(Color normal, Color selected, Color executing)
    {
        normalColor = normal;
        selectedColor = selected;
        executingColor = executing;
        UpdateVisualState();
    }

    /// <summary>
    /// Set selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }

    /// <summary>
    /// Set executing state (when command is being played)
    /// </summary>
    public void SetExecuting(bool executing)
    {
        isExecuting = executing;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (backgroundImage == null) return;

        if (isExecuting)
        {
            backgroundImage.color = executingColor;
        }
        else if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }

        // Scale effect for selection
        transform.localScale = isSelected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
    }

    // Public accessors
    public CommandData Command => commandData;
    public int Index => commandIndex;
    public bool IsSelected => isSelected;
}
