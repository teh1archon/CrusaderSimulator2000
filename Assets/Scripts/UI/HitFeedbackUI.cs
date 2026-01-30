using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying hit judgement feedback.
/// Shows Perfect/Good/Bad/Miss with visual effects.
/// Placed where the bard character is in the concept art.
/// </summary>
public class HitFeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmGameManager rhythmManager;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI judgementText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private Image flashImage;  // Full panel flash effect
    [SerializeField] private ParticleSystem hitParticles;  // Optional particle effect

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 0.5f;
    [SerializeField] private float fadeSpeed = 3f;
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float scaleMultiplier = 1.3f;

    [Header("Colors")]
    [SerializeField] private Color perfectColor = new Color(1f, 0.84f, 0f);    // Gold
    [SerializeField] private Color goodColor = new Color(0.2f, 0.8f, 0.2f);    // Green
    [SerializeField] private Color badColor = new Color(0.8f, 0.5f, 0.1f);     // Orange
    [SerializeField] private Color missColor = new Color(0.8f, 0.2f, 0.2f);    // Red

    [Header("Text")]
    [SerializeField] private string perfectText = "PERFECT!";
    [SerializeField] private string goodText = "Good!";
    [SerializeField] private string badText = "Bad";
    [SerializeField] private string missText = "Miss";

    private float displayTimer;
    private int currentCombo;
    private Vector3 originalScale;
    private Color originalFlashColor;

    private void Start()
    {
        if (rhythmManager == null)
            rhythmManager = RhythmGameManager.Instance;

        if (rhythmManager != null)
        {
            rhythmManager.OnNoteHit += OnNoteHit;
            rhythmManager.OnNoteMissed += OnNoteMissed;
            rhythmManager.OnMelodyStarted += OnMelodyStarted;
            rhythmManager.OnMelodyCompleted += OnMelodyCompleted;
        }

        // Store original values
        if (judgementText != null)
        {
            originalScale = judgementText.transform.localScale;
            judgementText.gameObject.SetActive(false);
        }

        if (flashImage != null)
        {
            originalFlashColor = flashImage.color;
            flashImage.gameObject.SetActive(false);
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }

        currentCombo = 0;
    }

    private void OnDestroy()
    {
        if (rhythmManager != null)
        {
            rhythmManager.OnNoteHit -= OnNoteHit;
            rhythmManager.OnNoteMissed -= OnNoteMissed;
            rhythmManager.OnMelodyStarted -= OnMelodyStarted;
            rhythmManager.OnMelodyCompleted -= OnMelodyCompleted;
        }
    }

    private void Update()
    {
        // Fade out judgement text
        if (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;

            // Fade alpha
            if (judgementText != null && judgementText.gameObject.activeSelf)
            {
                float alpha = Mathf.Clamp01(displayTimer / displayDuration);
                Color c = judgementText.color;
                c.a = alpha;
                judgementText.color = c;

                // Scale animation
                if (useScaleAnimation)
                {
                    float scaleT = 1f - alpha;
                    judgementText.transform.localScale = Vector3.Lerp(
                        originalScale * scaleMultiplier,
                        originalScale,
                        scaleT
                    );
                }
            }

            // Fade flash
            if (flashImage != null && flashImage.gameObject.activeSelf)
            {
                float flashAlpha = Mathf.Clamp01(displayTimer / displayDuration) * 0.3f;
                Color c = flashImage.color;
                c.a = flashAlpha;
                flashImage.color = c;
            }

            if (displayTimer <= 0)
            {
                HideFeedback();
            }
        }
    }

    private void OnNoteHit(NoteLane lane, HitJudgement judgement)
    {
        Color color;
        string text;

        switch (judgement)
        {
            case HitJudgement.Perfect:
                color = perfectColor;
                text = perfectText;
                currentCombo++;
                break;
            case HitJudgement.Good:
                color = goodColor;
                text = goodText;
                currentCombo++;
                break;
            case HitJudgement.Bad:
                color = badColor;
                text = badText;
                currentCombo = 0;  // Bad breaks combo
                break;
            default:
                color = missColor;
                text = missText;
                currentCombo = 0;
                break;
        }

        ShowFeedback(text, color);
        UpdateCombo();

        // Play particles for perfect/good hits
        if (hitParticles != null && (judgement == HitJudgement.Perfect || judgement == HitJudgement.Good))
        {
            var main = hitParticles.main;
            main.startColor = color;
            hitParticles.Play();
        }
    }

    private void OnNoteMissed(NoteLane lane)
    {
        currentCombo = 0;
        ShowFeedback(missText, missColor);
        UpdateCombo();
    }

    private void OnMelodyStarted(MelodyData melody)
    {
        currentCombo = 0;
        UpdateCombo();
    }

    private void OnMelodyCompleted(int score)
    {
        HideFeedback();
    }

    private void ShowFeedback(string text, Color color)
    {
        if (judgementText != null)
        {
            judgementText.text = text;
            judgementText.color = color;
            judgementText.gameObject.SetActive(true);

            if (useScaleAnimation)
            {
                judgementText.transform.localScale = originalScale * scaleMultiplier;
            }
        }

        if (flashImage != null)
        {
            Color flashColor = color;
            flashColor.a = 0.3f;
            flashImage.color = flashColor;
            flashImage.gameObject.SetActive(true);
        }

        displayTimer = displayDuration;
    }

    private void HideFeedback()
    {
        if (judgementText != null)
        {
            judgementText.gameObject.SetActive(false);
            judgementText.transform.localScale = originalScale;
        }

        if (flashImage != null)
        {
            flashImage.gameObject.SetActive(false);
        }
    }

    private void UpdateCombo()
    {
        if (comboText == null) return;

        if (currentCombo > 1)
        {
            comboText.text = $"{currentCombo}x";
            comboText.gameObject.SetActive(true);
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Manually trigger feedback (for testing)
    /// </summary>
    [ContextMenu("Test Perfect")]
    public void TestPerfect() => ShowFeedback(perfectText, perfectColor);

    [ContextMenu("Test Good")]
    public void TestGood() => ShowFeedback(goodText, goodColor);

    [ContextMenu("Test Bad")]
    public void TestBad() => ShowFeedback(badText, badColor);

    [ContextMenu("Test Miss")]
    public void TestMiss() => ShowFeedback(missText, missColor);
}
