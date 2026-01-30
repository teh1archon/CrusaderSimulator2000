using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the visual note highway - the scrolling track where notes appear.
/// Notes scroll from right to left per GDD specification.
/// </summary>
public class NoteHighway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RhythmGameManager rhythmManager;
    [SerializeField] private RectTransform highwayPanel;
    [SerializeField] private RectTransform hitZone;

    [Header("Note Prefab")]
    [SerializeField] private GameObject notePrefab;

    [Header("Timing")]
    [Tooltip("How far before the hit zone (in seconds) notes spawn. 0 = use RhythmGameManager's value")]
    [SerializeField] private float noteLeadTime = 0f;

    [Header("Lane Configuration")]
    [SerializeField] private float laneSpacing = 60f;
    [SerializeField] private Color[] laneColors = new Color[5]
    {
        new Color(1f, 0.3f, 0.3f),   // Key5 - Red
        new Color(1f, 0.6f, 0.2f),   // KeyT - Orange
        new Color(1f, 1f, 0.3f),     // KeyG - Yellow
        new Color(0.3f, 1f, 0.3f),   // KeyB - Green
        new Color(0.3f, 0.6f, 1f)    // Space - Blue
    };

    // Active note visuals
    private List<NoteVisual> activeNotes = new List<NoteVisual>();
    private Queue<NoteVisual> notePool = new Queue<NoteVisual>();

    // Layout calculations
    private float highwayWidth;
    private float hitZoneX;
    private bool layoutCalculated = false;

    private void Start()
    {
        // Auto-assign highway panel to self if not set
        if (highwayPanel == null)
            highwayPanel = GetComponent<RectTransform>();

        if (rhythmManager == null)
            rhythmManager = RhythmGameManager.Instance;

        if (rhythmManager != null)
        {
            rhythmManager.OnNoteSpawned += SpawnNote;
            rhythmManager.OnNoteHit += OnNoteHit;
            rhythmManager.OnNoteMissed += OnNoteMissed;
            rhythmManager.OnMelodyCompleted += OnMelodyCompleted;
            rhythmManager.OnMelodyInterrupted += OnMelodyInterrupted;
        }
        else
        {
            Debug.LogError("NoteHighway: No RhythmGameManager found! Add one to the scene.");
        }

        CalculateLayout();
        CreateNotePrefabIfNeeded();
    }

    private void OnDestroy()
    {
        if (rhythmManager != null)
        {
            rhythmManager.OnNoteSpawned -= SpawnNote;
            rhythmManager.OnNoteHit -= OnNoteHit;
            rhythmManager.OnNoteMissed -= OnNoteMissed;
            rhythmManager.OnMelodyCompleted -= OnMelodyCompleted;
            rhythmManager.OnMelodyInterrupted -= OnMelodyInterrupted;
        }
    }

    private void Update()
    {
        // Recalculate layout once UI has had time to layout (first frame may have 0 width)
        if (!layoutCalculated && highwayPanel != null && highwayPanel.rect.width > 0)
        {
            CalculateLayout();
            layoutCalculated = true;
        }

        if (rhythmManager == null || !rhythmManager.IsPlaying) return;

        UpdateNotePositions();
    }

    private void CalculateLayout()
    {
        if (highwayPanel != null)
        {
            highwayWidth = highwayPanel.rect.width;
            // If width is 0 (not yet laid out), use a default
            if (highwayWidth <= 0)
                highwayWidth = 1000f;
        }
        else
        {
            highwayWidth = 1000f;  // Default fallback
        }

        if (hitZone != null && highwayPanel != null)
        {
            // Convert hit zone world position to highway panel local position
            Vector3 hitZoneWorldPos = hitZone.position;
            Vector3 localPos = highwayPanel.InverseTransformPoint(hitZoneWorldPos);
            hitZoneX = localPos.x;
            Debug.Log($"NoteHighway: Hit zone local X = {hitZoneX}, Highway width = {highwayWidth}");
        }
        else
        {
            // Default: hit zone at 35% from left edge (center of highway is 0)
            hitZoneX = -highwayWidth * 0.35f;
            Debug.Log($"NoteHighway: Using default hit zone X = {hitZoneX}");
        }
    }

    private void CreateNotePrefabIfNeeded()
    {
        if (notePrefab != null) return;

        // Create a simple note prefab dynamically
        notePrefab = new GameObject("NotePrefab_Template");
        notePrefab.SetActive(false);

        var rectTransform = notePrefab.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50, 50);

        var image = notePrefab.AddComponent<Image>();
        image.color = Color.white;

        notePrefab.AddComponent<NoteVisual>();

        // Keep prefab as child but hidden - it's just a template for instantiation
        notePrefab.transform.SetParent(transform, false);

        Debug.Log("NoteHighway: Created dynamic note prefab");
    }

    private void SpawnNote(NoteLane lane, float targetTime)
    {
        NoteVisual note = GetNoteFromPool();

        float yPos = GetLaneY(lane);
        // Start from right edge of highway
        float startX = highwayWidth / 2f;

        note.Initialize(lane, targetTime, laneColors[(int)lane]);
        note.RectTransform.anchoredPosition = new Vector2(startX, yPos);
        note.gameObject.SetActive(true);

        activeNotes.Add(note);
    }

    private void UpdateNotePositions()
    {
        float currentTime = rhythmManager.MelodyTime;
        float scrollTime = noteLeadTime > 0 ? noteLeadTime : rhythmManager.NoteScrollTime;

        // Calculate spawn and despawn X positions
        float spawnX = highwayWidth / 2f;   // Right edge
        float despawnX = hitZoneX - 100f;    // Past the hit zone (allow notes to continue past)

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (!note.gameObject.activeSelf)
            {
                ReturnNoteToPool(note);
                activeNotes.RemoveAt(i);
                continue;
            }

            // Calculate position based on time until note should be hit
            float timeUntilHit = note.TargetTime - currentTime;
            float normalizedTime = Mathf.Clamp01(timeUntilHit / scrollTime);

            // Position: hitZoneX when normalizedTime = 0, spawnX when normalizedTime = 1
            // Notes continue past hit zone (negative normalizedTime)
            float xPos = Mathf.LerpUnclamped(hitZoneX, spawnX, normalizedTime);

            note.RectTransform.anchoredPosition = new Vector2(xPos, note.RectTransform.anchoredPosition.y);

            // Remove if too far past hit zone (allow some overshoot for visual feedback)
            if (xPos < despawnX)
            {
                ReturnNoteToPool(note);
                activeNotes.RemoveAt(i);
            }
        }
    }

    private float GetLaneY(NoteLane lane)
    {
        // Center the 5 lanes vertically
        int laneIndex = (int)lane;
        float centerOffset = (4 * laneSpacing) / 2f;  // 5 lanes, 4 gaps
        return centerOffset - (laneIndex * laneSpacing);
    }

    private void OnNoteHit(NoteLane lane, HitJudgement judgement)
    {
        // Find and animate the hit note
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (note.Lane == lane && note.gameObject.activeSelf)
            {
                note.PlayHitEffect(judgement);
                break;
            }
        }
    }

    private void OnNoteMissed(NoteLane lane)
    {
        // Find and animate the missed note
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var note = activeNotes[i];
            if (note.Lane == lane && note.gameObject.activeSelf)
            {
                note.PlayMissEffect();
                break;
            }
        }
    }

    private void OnMelodyCompleted(int score)
    {
        ClearAllNotes();
    }

    private void OnMelodyInterrupted()
    {
        ClearAllNotes();
    }

    private void ClearAllNotes()
    {
        foreach (var note in activeNotes)
        {
            ReturnNoteToPool(note);
        }
        activeNotes.Clear();
    }

    private NoteVisual GetNoteFromPool()
    {
        if (notePool.Count > 0)
        {
            return notePool.Dequeue();
        }

        // Instantiate as child of highway panel (or self if not set)
        Transform parent = highwayPanel != null ? highwayPanel : transform;
        var newNote = Instantiate(notePrefab, parent).GetComponent<NoteVisual>();
        return newNote;
    }

    private void ReturnNoteToPool(NoteVisual note)
    {
        note.gameObject.SetActive(false);
        notePool.Enqueue(note);
    }
}
