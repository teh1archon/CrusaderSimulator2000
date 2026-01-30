using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a melody/command track that the player performs during battle.
/// Create assets via: Right-click > Create > Cantus > Melody
/// </summary>
[CreateAssetMenu(fileName = "NewMelody", menuName = "Cantus/Melody")]
public class MelodyData : ScriptableObject
{
    [Tooltip("Display name for this melody/command")]
    public string melodyName;

    [Tooltip("Tempo in beats per minute - affects note spacing visualization")]
    [Range(60, 200)]
    public float bpm = 120f;

    [Tooltip("Total duration of the melody in seconds")]
    public float duration = 4f;

    [Tooltip("Sorted list of notes - add entries with time (seconds) and lane (key)")]
    public List<NoteEntry> notes = new List<NoteEntry>();

    /// <summary>
    /// A single note in the melody sequence
    /// </summary>
    [System.Serializable]
    public struct NoteEntry
    {
        [Tooltip("Time in seconds from melody start when this note should be hit")]
        public float time;

        [Tooltip("Which key/lane this note appears on")]
        public NoteLane lane;

        public NoteEntry(float time, NoteLane lane)
        {
            this.time = time;
            this.lane = lane;
        }
    }

    /// <summary>
    /// Validates and sorts notes by time. Call from editor or at runtime load.
    /// </summary>
    public void SortNotes()
    {
        notes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    private void OnValidate()
    {
        // Auto-sort notes in editor when values change
        SortNotes();
    }
}

/// <summary>
/// The five input lanes/keys for the rhythm system
/// Maps to keyboard keys: 5, T, G, B, Space
/// </summary>
public enum NoteLane
{
    Key5,       // Top row - pinky
    KeyT,       // Top row - index
    KeyG,       // Middle row - index
    KeyB,       // Bottom row - index
    KeySpace    // Spacebar - thumb
}
