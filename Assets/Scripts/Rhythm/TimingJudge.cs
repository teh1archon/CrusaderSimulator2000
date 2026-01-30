using UnityEngine;

/// <summary>
/// Evaluates timing accuracy for rhythm note hits.
/// Based on GDD timing windows:
/// - Perfect: ±3 frames (~50ms) = +5 pts
/// - Good: ±4-6 frames (~100ms) = +2 pts
/// - Bad: ±7-9 frames (~150ms) = -2 pts
/// - Miss: beyond = -3 pts
/// </summary>
public static class TimingJudge
{
    // Timing windows in seconds (assuming 60 FPS as reference)
    public const float PERFECT_WINDOW = 0.050f;  // ±3 frames at 60fps
    public const float GOOD_WINDOW = 0.100f;     // ±6 frames at 60fps
    public const float BAD_WINDOW = 0.150f;      // ±9 frames at 60fps

    // Point values from GDD
    public const int PERFECT_POINTS = 5;
    public const int GOOD_POINTS = 2;
    public const int BAD_POINTS = -2;
    public const int MISS_POINTS = -3;

    /// <summary>
    /// Judge the timing accuracy of a hit
    /// </summary>
    /// <param name="timeDifference">Absolute time difference between hit and note target time</param>
    /// <returns>The judgement result</returns>
    public static HitJudgement Judge(float timeDifference)
    {
        float absTime = Mathf.Abs(timeDifference);

        if (absTime <= PERFECT_WINDOW)
            return HitJudgement.Perfect;
        if (absTime <= GOOD_WINDOW)
            return HitJudgement.Good;
        if (absTime <= BAD_WINDOW)
            return HitJudgement.Bad;

        return HitJudgement.Miss;
    }

    /// <summary>
    /// Get the point value for a judgement
    /// </summary>
    public static int GetPoints(HitJudgement judgement)
    {
        return judgement switch
        {
            HitJudgement.Perfect => PERFECT_POINTS,
            HitJudgement.Good => GOOD_POINTS,
            HitJudgement.Bad => BAD_POINTS,
            HitJudgement.Miss => MISS_POINTS,
            _ => 0
        };
    }

    /// <summary>
    /// Get a color for visual feedback
    /// </summary>
    public static Color GetJudgementColor(HitJudgement judgement)
    {
        return judgement switch
        {
            HitJudgement.Perfect => new Color(1f, 0.84f, 0f),    // Gold
            HitJudgement.Good => new Color(0.2f, 0.8f, 0.2f),    // Green
            HitJudgement.Bad => new Color(0.8f, 0.4f, 0.1f),     // Orange
            HitJudgement.Miss => new Color(0.8f, 0.2f, 0.2f),    // Red
            _ => Color.white
        };
    }
}

/// <summary>
/// Result of timing judgement
/// </summary>
public enum HitJudgement
{
    Perfect,
    Good,
    Bad,
    Miss
}
