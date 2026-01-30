using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying the morale bar.
/// Handles fill amount, color changes, and glow effect when over 100%.
/// </summary>
public class MoraleBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MoraleManager moraleManager;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image glowImage;  // Optional glow overlay
    [SerializeField] private TextMeshProUGUI moraleText;  // Optional text display

    [Header("Colors")]
    [SerializeField] private Color lowMoraleColor = new Color(0.8f, 0.2f, 0.2f);    // Red
    [SerializeField] private Color normalMoraleColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Color highMoraleColor = new Color(0.2f, 0.8f, 0.2f);   // Green
    [SerializeField] private Color overchargedColor = new Color(1f, 0.84f, 0f);     // Gold

    [Header("Thresholds")]
    [SerializeField] private float lowThreshold = 0.3f;
    [SerializeField] private float highThreshold = 0.7f;

    [Header("Glow Effect")]
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMinAlpha = 0.3f;
    [SerializeField] private float glowMaxAlpha = 0.8f;
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.5f);

    private bool isOvercharged;
    private float glowTimer;

    private void Start()
    {
        if (moraleManager == null)
            moraleManager = MoraleManager.Instance;

        if (moraleManager != null)
        {
            moraleManager.OnMoraleChanged += UpdateMoraleDisplay;
            moraleManager.OnMoraleStateChanged += OnMoraleStateChanged;

            // Initialize display
            UpdateMoraleDisplay(moraleManager.CurrentMorale, moraleManager.MaxMorale);
        }

        // Hide glow initially
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (moraleManager != null)
        {
            moraleManager.OnMoraleChanged -= UpdateMoraleDisplay;
            moraleManager.OnMoraleStateChanged -= OnMoraleStateChanged;
        }
    }

    private void Update()
    {
        // Animate glow when overcharged
        if (isOvercharged && glowImage != null && glowImage.gameObject.activeSelf)
        {
            glowTimer += Time.deltaTime * glowPulseSpeed;
            float alpha = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, (Mathf.Sin(glowTimer) + 1f) * 0.5f);
            Color c = glowColor;
            c.a = alpha;
            glowImage.color = c;
        }
    }

    private void UpdateMoraleDisplay(int current, int max)
    {
        if (max <= 0) return;

        float percent = (float)current / max;
        isOvercharged = percent > 1f;

        // Update fill amount (clamped to 1 for display)
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(percent);

            // Update fill color based on morale level
            if (isOvercharged)
            {
                fillImage.color = overchargedColor;
            }
            else if (percent <= lowThreshold)
            {
                fillImage.color = lowMoraleColor;
            }
            else if (percent >= highThreshold)
            {
                fillImage.color = highMoraleColor;
            }
            else
            {
                // Lerp between low and normal for mid-range
                float t = (percent - lowThreshold) / (highThreshold - lowThreshold);
                fillImage.color = Color.Lerp(lowMoraleColor, normalMoraleColor, t);
            }
        }

        // Update glow visibility
        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(isOvercharged);
            if (isOvercharged)
            {
                glowTimer = 0f;  // Reset pulse
            }
        }

        // Update text
        if (moraleText != null)
        {
            moraleText.text = $"{current}/{max}";
        }
    }

    private void OnMoraleStateChanged(MoraleState state)
    {
        // Could add additional visual feedback for state changes
        // e.g., screen flash, border color change
        Debug.Log($"[MoraleBarUI] State changed to: {state}");
    }

    /// <summary>
    /// Create a simple glow image programmatically if not assigned.
    /// Call this from editor script or manually.
    /// </summary>
    [ContextMenu("Create Glow Image")]
    public void CreateGlowImage()
    {
        if (glowImage != null) return;
        if (fillImage == null) return;

        // Create glow as sibling behind fill
        GameObject glowObj = new GameObject("MoraleGlow");
        glowObj.transform.SetParent(fillImage.transform.parent, false);
        glowObj.transform.SetSiblingIndex(fillImage.transform.GetSiblingIndex());

        // Copy RectTransform from fill
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        RectTransform fillRect = fillImage.rectTransform;
        glowRect.anchorMin = fillRect.anchorMin;
        glowRect.anchorMax = fillRect.anchorMax;
        glowRect.offsetMin = fillRect.offsetMin - new Vector2(5, 5);  // Slightly larger
        glowRect.offsetMax = fillRect.offsetMax + new Vector2(5, 5);
        glowRect.pivot = fillRect.pivot;

        glowImage = glowObj.AddComponent<Image>();
        glowImage.color = glowColor;
        glowImage.raycastTarget = false;

        glowObj.SetActive(false);

        Debug.Log("Created glow image. For better effect, assign a soft glow sprite.");
    }
}
