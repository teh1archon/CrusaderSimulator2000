using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core manager for the rhythm game system.
/// Handles melody playback, input detection, and scoring.
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private float noteScrollTime = 2f;  // Time for note to travel across screen
    [SerializeField] private float missWindow = 0.2f;    // Time after note passes before auto-miss

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;    // For melody music tracks
    [SerializeField] private AudioSource sfxSource;      // For hit/miss sound effects
    [SerializeField] private AudioClip missSound;        // Sound played when note is missed
    [SerializeField] private AudioClip hitSound;         // Optional: sound played on hit
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.5f;

    [Header("Current State")]
    [SerializeField] private MelodyData currentMelody;
    [SerializeField] private bool isPlaying;
    [SerializeField] private bool isCountingDown;
    [SerializeField] private float melodyTime;
    [SerializeField] private float countdownTime;
    [SerializeField] private int currentScore;
    private int lastCountdownSecond = -1;

    // Input mapping
    private readonly Dictionary<NoteLane, KeyCode> laneKeys = new Dictionary<NoteLane, KeyCode>
    {
        { NoteLane.Key5, KeyCode.Alpha5 },
        { NoteLane.KeyT, KeyCode.T },
        { NoteLane.KeyG, KeyCode.G },
        { NoteLane.KeyB, KeyCode.B },
        { NoteLane.KeySpace, KeyCode.Space }
    };

    // Active notes tracking
    private List<ActiveNote> activeNotes = new List<ActiveNote>();
    private int nextNoteIndex;

    // Events for UI and other systems
    public event Action<MelodyData> OnMelodyStarted;
    public event Action<int> OnMelodyCompleted;  // Final score
    public event Action OnMelodyInterrupted;
    public event Action<NoteLane, float> OnNoteSpawned;  // Lane, target time
    public event Action<NoteLane, HitJudgement> OnNoteHit;
    public event Action<NoteLane> OnNoteMissed;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnCountdownTick;  // Seconds remaining (3, 2, 1, 0=GO!)
    public event Action OnCountdownComplete;

    private struct ActiveNote
    {
        public float targetTime;
        public NoteLane lane;
        public bool wasHit;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-create audio sources if not assigned (for testing)
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            Debug.Log("[RhythmGameManager] Created music AudioSource");
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            Debug.Log("[RhythmGameManager] Created SFX AudioSource");
        }
    }

    private void Update()
    {
        if (currentMelody == null) return;

        // Handle countdown phase
        if (isCountingDown)
        {
            countdownTime -= Time.deltaTime;

            // Fire countdown tick events (3, 2, 1...)
            int currentSecond = Mathf.CeilToInt(countdownTime);
            if (currentSecond != lastCountdownSecond && currentSecond >= 0)
            {
                lastCountdownSecond = currentSecond;
                OnCountdownTick?.Invoke(currentSecond);
            }

            // Countdown complete
            if (countdownTime <= 0f)
            {
                isCountingDown = false;
                isPlaying = true;
                melodyTime = 0f;
                StartMelodyMusic();
                OnCountdownComplete?.Invoke();
                Debug.Log("Countdown complete - melody starting!");
            }
            return;
        }

        if (!isPlaying) return;

        melodyTime += Time.deltaTime;

        // Spawn upcoming notes
        SpawnUpcomingNotes();

        // Check for input
        CheckInput();

        // Check for missed notes
        CheckMissedNotes();

        // Check if melody is complete (account for note offset)
        float effectiveDuration = currentMelody.duration + currentMelody.noteTimeOffset;
        if (melodyTime >= effectiveDuration + missWindow)
        {
            CompleteMelody();
        }
    }

    /// <summary>
    /// Start playing a melody
    /// </summary>
    public void StartMelody(MelodyData melody)
    {
        if (melody == null || melody.notes.Count == 0)
        {
            Debug.LogWarning("Cannot start null or empty melody");
            return;
        }

        // If already playing, interrupt current melody
        if (isPlaying || isCountingDown)
        {
            InterruptMelody();
        }

        currentMelody = melody;
        melodyTime = 0f;
        currentScore = 0;
        nextNoteIndex = 0;
        activeNotes.Clear();
        lastCountdownSecond = -1;

        // Start countdown if delay is configured
        if (melody.startDelay > 0f)
        {
            countdownTime = melody.startDelay;
            isCountingDown = true;
            isPlaying = false;
            OnCountdownTick?.Invoke(Mathf.CeilToInt(countdownTime));
            Debug.Log($"Starting countdown ({melody.startDelay}s) for melody: {melody.melodyName}");
        }
        else
        {
            isCountingDown = false;
            isPlaying = true;
            StartMelodyMusic();
        }

        OnMelodyStarted?.Invoke(melody);
        OnScoreChanged?.Invoke(currentScore);

        Debug.Log($"Started melody: {melody.melodyName} ({melody.notes.Count} notes)");
    }

    /// <summary>
    /// Stop the current melody without completing it (causes morale penalty per GDD)
    /// </summary>
    public void InterruptMelody()
    {
        if (!isPlaying && !isCountingDown) return;

        isPlaying = false;
        isCountingDown = false;
        StopMelodyMusic();
        OnMelodyInterrupted?.Invoke();
        Debug.Log("Melody interrupted!");
    }

    private void SpawnUpcomingNotes()
    {
        // Spawn notes that are within scroll time of being hit
        while (nextNoteIndex < currentMelody.notes.Count)
        {
            var note = currentMelody.notes[nextNoteIndex];

            // Apply the global offset to note time
            float adjustedTime = note.time + currentMelody.noteTimeOffset;

            // Spawn note when it's within scroll time
            if (adjustedTime <= melodyTime + noteScrollTime)
            {
                activeNotes.Add(new ActiveNote
                {
                    targetTime = adjustedTime,
                    lane = note.lane,
                    wasHit = false
                });

                OnNoteSpawned?.Invoke(note.lane, adjustedTime);
                nextNoteIndex++;
            }
            else
            {
                break;
            }
        }
    }

    private void CheckInput()
    {
        foreach (var kvp in laneKeys)
        {
            if (Input.GetKeyDown(kvp.Value))
            {
                ProcessLaneInput(kvp.Key);
            }
        }
    }

    private void ProcessLaneInput(NoteLane lane)
    {
        // Find the closest unhit note in this lane
        int closestIndex = -1;
        float closestDiff = float.MaxValue;

        for (int i = 0; i < activeNotes.Count; i++)
        {
            var note = activeNotes[i];
            if (note.wasHit || note.lane != lane) continue;

            float diff = Mathf.Abs(melodyTime - note.targetTime);
            if (diff < closestDiff && diff <= TimingJudge.BAD_WINDOW)
            {
                closestDiff = diff;
                closestIndex = i;
            }
        }

        if (closestIndex >= 0)
        {
            // Hit the note
            var note = activeNotes[closestIndex];
            float timeDiff = melodyTime - note.targetTime;

            HitJudgement judgement = TimingJudge.Judge(timeDiff);
            int points = TimingJudge.GetPoints(judgement);

            currentScore += points;

            // Mark as hit
            var hitNote = activeNotes[closestIndex];
            hitNote.wasHit = true;
            activeNotes[closestIndex] = hitNote;

            OnNoteHit?.Invoke(lane, judgement);
            OnScoreChanged?.Invoke(currentScore);

            Debug.Log($"Hit! Lane: {lane}, Judgement: {judgement}, Points: {points}, Total: {currentScore}");
        }
        else
        {
            // Wrong key or no note to hit - could penalize here
            Debug.Log($"Empty hit on lane {lane}");
        }
    }

    private void CheckMissedNotes()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (note.wasHit) continue;

            // Note is missed if we're past the bad window
            if (melodyTime > note.targetTime + TimingJudge.BAD_WINDOW)
            {
                currentScore += TimingJudge.MISS_POINTS;

                // Mark as hit (missed)
                var missedNote = activeNotes[i];
                missedNote.wasHit = true;
                activeNotes[i] = missedNote;

                OnNoteMissed?.Invoke(note.lane);
                OnScoreChanged?.Invoke(currentScore);
                PlayMissSound();

                Debug.Log($"Missed! Lane: {note.lane}, Points: {TimingJudge.MISS_POINTS}, Total: {currentScore}");
            }
        }
    }

    private void CompleteMelody()
    {
        isPlaying = false;
        StopMelodyMusic();
        OnMelodyCompleted?.Invoke(currentScore);
        Debug.Log($"Melody completed! Final score: {currentScore}");
    }

    #region Audio

    private void StartMelodyMusic()
    {
        if (musicSource == null || currentMelody == null) return;
        if (currentMelody.musicTrack == null) return;

        musicSource.clip = currentMelody.musicTrack;
        musicSource.volume = currentMelody.musicVolume;
        musicSource.Play();
        Debug.Log($"Playing music: {currentMelody.musicTrack.name}");
    }

    private void StopMelodyMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    private void PlayMissSound()
    {
        if (sfxSource == null || missSound == null) return;
        sfxSource.PlayOneShot(missSound, sfxVolume);
    }

    private void PlayHitSound()
    {
        if (sfxSource == null || hitSound == null) return;
        sfxSource.PlayOneShot(hitSound, sfxVolume);
    }

    #endregion

    // Public accessors
    public bool IsPlaying => isPlaying;
    public bool IsCountingDown => isCountingDown;
    public float CountdownTime => countdownTime;
    public float MelodyTime => melodyTime;
    public float MelodyDuration => currentMelody?.duration ?? 0f;
    public int CurrentScore => currentScore;
    public float NoteScrollTime => noteScrollTime;

    /// <summary>
    /// Get normalized progress (0-1) through current melody
    /// </summary>
    public float GetProgress()
    {
        if (currentMelody == null || currentMelody.duration <= 0) return 0f;
        return Mathf.Clamp01(melodyTime / currentMelody.duration);
    }
}
