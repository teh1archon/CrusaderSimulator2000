using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the rhythm game display.
/// Shows score, judgement feedback, and progress.
/// </summary>
public class RhythmUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmGameManager rhythmManager;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI judgementText;
    [SerializeField] private TextMeshProUGUI melodyNameText;
    [SerializeField] private Image progressBar;

    [Header("Lane Indicators")]
    [SerializeField] private Image[] laneIndicators;  // 5 lane background indicators

    [Header("Judgement Display")]
    [SerializeField] private float judgementDisplayTime = 0.5f;
    private float judgementTimer;

    // Key states for visual feedback
    private readonly KeyCode[] laneKeys = {
        KeyCode.Alpha5,
        KeyCode.T,
        KeyCode.G,
        KeyCode.B,
        KeyCode.Space
    };

    private Color[] laneDefaultColors;

    private void Start()
    {
        if (rhythmManager == null)
            rhythmManager = RhythmGameManager.Instance;

        if (rhythmManager != null)
        {
            rhythmManager.OnMelodyStarted += OnMelodyStarted;
            rhythmManager.OnMelodyCompleted += OnMelodyCompleted;
            rhythmManager.OnScoreChanged += OnScoreChanged;
            rhythmManager.OnNoteHit += OnNoteHit;
            rhythmManager.OnNoteMissed += OnNoteMissed;
        }

        // Store default lane colors
        if (laneIndicators != null && laneIndicators.Length > 0)
        {
            laneDefaultColors = new Color[laneIndicators.Length];
            for (int i = 0; i < laneIndicators.Length; i++)
            {
                if (laneIndicators[i] != null)
                    laneDefaultColors[i] = laneIndicators[i].color;
            }
        }

        UpdateScore(0);
        HideJudgement();
    }

    private void OnDestroy()
    {
        if (rhythmManager != null)
        {
            rhythmManager.OnMelodyStarted -= OnMelodyStarted;
            rhythmManager.OnMelodyCompleted -= OnMelodyCompleted;
            rhythmManager.OnScoreChanged -= OnScoreChanged;
            rhythmManager.OnNoteHit -= OnNoteHit;
            rhythmManager.OnNoteMissed -= OnNoteMissed;
        }
    }

    private void Update()
    {
        // Update progress bar
        if (rhythmManager != null && rhythmManager.IsPlaying && progressBar != null)
        {
            progressBar.fillAmount = rhythmManager.GetProgress();
        }

        // Update lane key highlights
        UpdateLaneHighlights();

        // Fade out judgement text
        if (judgementTimer > 0)
        {
            judgementTimer -= Time.deltaTime;
            if (judgementTimer <= 0)
            {
                HideJudgement();
            }
        }
    }

    private void UpdateLaneHighlights()
    {
        if (laneIndicators == null) return;

        for (int i = 0; i < laneIndicators.Length && i < laneKeys.Length; i++)
        {
            if (laneIndicators[i] == null) continue;

            if (Input.GetKey(laneKeys[i]))
            {
                // Brighten when pressed
                laneIndicators[i].color = Color.white;
            }
            else
            {
                // Return to default
                laneIndicators[i].color = laneDefaultColors[i];
            }
        }
    }

    private void OnMelodyStarted(MelodyData melody)
    {
        if (melodyNameText != null)
            melodyNameText.text = melody.melodyName;

        if (progressBar != null)
            progressBar.fillAmount = 0f;
    }

    private void OnMelodyCompleted(int finalScore)
    {
        ShowJudgement("Complete!", Color.cyan);

        if (progressBar != null)
            progressBar.fillAmount = 1f;
    }

    private void OnScoreChanged(int score)
    {
        UpdateScore(score);
    }

    private void OnNoteHit(NoteLane lane, HitJudgement judgement)
    {
        ShowJudgement(judgement.ToString(), TimingJudge.GetJudgementColor(judgement));
    }

    private void OnNoteMissed(NoteLane lane)
    {
        ShowJudgement("Miss", TimingJudge.GetJudgementColor(HitJudgement.Miss));
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    private void ShowJudgement(string text, Color color)
    {
        if (judgementText != null)
        {
            judgementText.text = text;
            judgementText.color = color;
            judgementText.gameObject.SetActive(true);
        }
        judgementTimer = judgementDisplayTime;
    }

    private void HideJudgement()
    {
        if (judgementText != null)
            judgementText.gameObject.SetActive(false);
    }
}
