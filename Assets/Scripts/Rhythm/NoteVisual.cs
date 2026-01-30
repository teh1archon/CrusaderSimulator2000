using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single note in the rhythm highway.
/// Handles display and hit/miss feedback animations.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class NoteVisual : MonoBehaviour
{
    private RectTransform rectTransform;
    private Image image;
    private TMPro.TextMeshProUGUI laneText;  // Cached reference for pooling

    [Header("Visual Settings")]
    [SerializeField] private float hitScalePunch = 1.5f;
    [SerializeField] private float fadeOutSpeed = 3f;

    // Note data
    public NoteLane Lane { get; private set; }
    public float TargetTime { get; private set; }

    // Animation state
    private bool isAnimating;
    private float animationTime;
    private Color baseColor;
    private Vector3 baseScale;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    private Image Image
    {
        get
        {
            if (image == null)
                image = GetComponent<Image>();
            return image;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        baseScale = Vector3.one;
    }

    private void Update()
    {
        if (!isAnimating) return;

        animationTime += Time.deltaTime * fadeOutSpeed;

        // Scale punch animation
        float scaleT = 1f - Mathf.Pow(1f - animationTime, 3f);
        transform.localScale = Vector3.Lerp(baseScale * hitScalePunch, baseScale, scaleT);

        // Fade out
        Color c = Image.color;
        c.a = Mathf.Lerp(1f, 0f, animationTime);
        Image.color = c;

        // Complete animation
        if (animationTime >= 1f)
        {
            isAnimating = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Initialize the note with lane and timing data
    /// </summary>
    public void Initialize(NoteLane lane, float targetTime, Color color)
    {
        Lane = lane;
        TargetTime = targetTime;
        baseColor = color;

        Image.color = color;
        transform.localScale = baseScale;
        isAnimating = false;
        animationTime = 0f;

        // Add lane indicator text
        UpdateLaneText(lane);
    }

    private void UpdateLaneText(NoteLane lane)
    {
        // Use cached reference if valid, otherwise find or create
        if (laneText == null)
        {
            // Try to find existing child first
            var existingText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (existingText != null)
            {
                laneText = existingText;
            }
            else
            {
                // Create new text object
                var textObj = new GameObject("LaneText");
                textObj.transform.SetParent(transform, false);

                var textRect = textObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                laneText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
                laneText.alignment = TMPro.TextAlignmentOptions.Center;
                laneText.fontSize = 24;
                laneText.fontStyle = TMPro.FontStyles.Bold;
            }
        }

        laneText.text = GetLaneLabel(lane);
        laneText.color = GetContrastColor(baseColor);
    }

    private string GetLaneLabel(NoteLane lane)
    {
        return lane switch
        {
            NoteLane.Key5 => "5",
            NoteLane.KeyT => "T",
            NoteLane.KeyG => "G",
            NoteLane.KeyB => "B",
            NoteLane.KeySpace => "▬",  // Bar symbol for spacebar
            _ => "?"
        };
    }

    private Color GetContrastColor(Color background)
    {
        // Calculate luminance and return black or white for contrast
        float luminance = 0.299f * background.r + 0.587f * background.g + 0.114f * background.b;
        return luminance > 0.5f ? Color.black : Color.white;
    }

    /// <summary>
    /// Play hit feedback animation
    /// </summary>
    public void PlayHitEffect(HitJudgement judgement)
    {
        baseColor = TimingJudge.GetJudgementColor(judgement);
        Image.color = baseColor;

        isAnimating = true;
        animationTime = 0f;
    }

    /// <summary>
    /// Play miss feedback animation
    /// </summary>
    public void PlayMissEffect()
    {
        baseColor = TimingJudge.GetJudgementColor(HitJudgement.Miss);
        Image.color = baseColor;

        isAnimating = true;
        animationTime = 0f;
    }
}
