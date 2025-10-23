using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Tracks and manages the player's score for the current day.
/// Attach to a GameObject in the scene (e.g., GameManager or ScoreDisplay).
/// </summary>
public class ScoreManagerController : MonoBehaviour
{
    [Header("Configuration")]
    public ScoreConfigSO scoreConfig;

    [Header("State (Read-Only)")]
    public int currentScore = 0;

    [Header("Events")]
    public UnityEvent<int> OnScoreChanged;  // Passes new total score
    public UnityEvent<int> OnDayComplete;   // Passes final score

    [Header("Debug")]
    public bool logScoreChanges = true;

    /// <summary>
    /// Add score points (called by DepositTray)
    /// </summary>
    public void AddScore(int amount)
    {
        if (amount <= 0) return;

        currentScore += amount;
        
        if (logScoreChanges)
        {
            Debug.Log($"[ScoreManager] +{amount} score | Total: {currentScore}");
        }

        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Reset score to zero (wire to Reset button or call at day start)
    /// </summary>
    public void ResetScore()
    {
        int previousScore = currentScore;
        currentScore = 0;
        
        if (logScoreChanges)
        {
            Debug.Log($"[ScoreManager] Score reset (was: {previousScore})");
        }

        OnScoreChanged?.Invoke(currentScore);
    }

    /// <summary>
    /// Get current score (for queries)
    /// </summary>
    public int GetCurrentScore()
    {
        return currentScore;
    }

    /// <summary>
    /// Finalize day and emit final score event
    /// </summary>
    public void FinishDay()
    {
        if (logScoreChanges)
        {
            Debug.Log($"[ScoreManager] Day Complete | Final Score: {currentScore}");
        }

        OnDayComplete?.Invoke(currentScore);
    }
}

