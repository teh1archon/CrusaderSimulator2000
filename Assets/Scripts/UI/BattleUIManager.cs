using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main UI manager for the battle phase.
/// Handles pause, command selection, and coordinates other UI elements.
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pauseOverlay;  // Optional: visual overlay when paused

    [Header("Morale")]
    [SerializeField] private MoraleBarUI moraleBar;

    [Header("Commands")]
    [SerializeField] private CommandListUI commandList;

    [Header("Hit Feedback")]
    [SerializeField] private HitFeedbackUI hitFeedback;

    [Header("State")]
    [SerializeField] private bool isPaused;

    // Events
    public event Action<bool> OnPauseToggled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Hook up pause button
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        // Initialize pause state
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
    }

    private void Update()
    {
        // ESC to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle pause state
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            Debug.Log("[BattleUI] Game PAUSED");
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("[BattleUI] Game RESUMED");
        }

        if (pauseOverlay != null)
            pauseOverlay.SetActive(isPaused);

        OnPauseToggled?.Invoke(isPaused);
    }

    /// <summary>
    /// Set pause state directly
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        TogglePause();
    }

    // Public accessors
    public bool IsPaused => isPaused;
}
