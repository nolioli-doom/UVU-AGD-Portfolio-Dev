using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for the day finished screen.
/// Displays day score and provides Next Day / Replay Day buttons.
/// </summary>
public class DayFinishedScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text displaying the day score")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Text displaying day number")]
    public TextMeshProUGUI dayNumberText;
    
    [Tooltip("Button to proceed to next day")]
    public Button nextDayButton;
    
    [Tooltip("Button to replay the current day")]
    public Button replayDayButton;
    
    [Header("System References")]
    [Tooltip("DayIndexSO that stores the current day index")]
    public DayIndexSO dayIndexSO;
    
    [Tooltip("DayFinishedManager to reset day state")]
    public DayFinishedManager dayFinishedManager;
    
    [Tooltip("ScoreManagerController to reset score")]
    public ScoreManagerController scoreManager;
    
    [Tooltip("OrderManagerSO to clear orders")]
    public OrderManagerSO orderManager;
    
    [Header("Text Format")]
    [Tooltip("Format string for score display (use {0} for score)")]
    public string scoreFormat = "Score: {0}";
    
    [Tooltip("Format string for day number (use {0} for day number)")]
    public string dayNumberFormat = "Day {0}";
    
    [Header("Debug")]
    public bool logActions = true;
    
    private int currentDayScore = 0;
    
    void Start()
    {
        // Auto-find dependencies
        if (dayIndexSO == null)
        {
            dayIndexSO = Resources.FindObjectsOfTypeAll<DayIndexSO>().FirstOrDefault();
        }
        
        if (dayFinishedManager == null)
        {
            dayFinishedManager = FindObjectOfType<DayFinishedManager>();
        }
        
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManagerController>();
        }
        
        if (orderManager == null)
        {
            orderManager = Resources.FindObjectsOfTypeAll<OrderManagerSO>().FirstOrDefault();
        }
        
        // Setup button listeners
        if (nextDayButton != null)
        {
            nextDayButton.onClick.AddListener(OnNextDayClicked);
        }
        
        if (replayDayButton != null)
        {
            replayDayButton.onClick.AddListener(OnReplayDayClicked);
        }
        
        // Hide screen initially
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Set the score to display for this day
    /// </summary>
    public void SetDayScore(int score)
    {
        currentDayScore = score;
        
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }
        
        // Update day number display
        if (dayNumberText != null && dayIndexSO != null)
        {
            int dayNumber = dayIndexSO.GetDisplayDayNumber(); // Display as 1-indexed
            dayNumberText.text = string.Format(dayNumberFormat, dayNumber);
        }
    }
    
    /// <summary>
    /// Handle Next Day button click
    /// </summary>
    private void OnNextDayClicked()
    {
        if (logActions)
        {
            Debug.Log("[DayFinishedScreen] Next Day button clicked");
        }
        
        // Increment day index
        if (dayIndexSO != null)
        {
            dayIndexSO.IncrementDay();
        }
        
        // Reload scene for fresh start
        ReloadScene();
    }
    
    /// <summary>
    /// Handle Replay Day button click
    /// </summary>
    private void OnReplayDayClicked()
    {
        if (logActions)
        {
            Debug.Log("[DayFinishedScreen] Replay Day button clicked");
        }
        
        // Don't increment day index (replay same day)
        
        // Reload scene for fresh start
        ReloadScene();
    }
    
    /// <summary>
    /// Reload the current scene to start fresh
    /// </summary>
    private void ReloadScene()
    {
        LoadingScreen.ReloadCurrentScene();
    }
}

