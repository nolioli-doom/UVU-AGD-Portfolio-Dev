using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages day completion detection and triggers day finished screen.
/// Day ends when:
/// 1. All customer orders are cleared (completed or expired), OR
/// 2. Day timer hits zero
/// </summary>
public class DayFinishedManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("OrderManagerSO to check order states")]
    public OrderManagerSO orderManager;
    
    [Tooltip("DayTimer to check if timer expired")]
    public DayTimer dayTimer;
    
    [Tooltip("ScoreManagerController to get final score")]
    public ScoreManagerController scoreManager;
    
    [Header("Day Finished Screen")]
    [Tooltip("GameObject containing the day finished UI (should be inactive by default)")]
    public GameObject dayFinishedScreen;
    
    [Header("Events")]
    [Tooltip("Invoked when day ends (all orders cleared or timer expired)")]
    public UnityEvent OnDayEnded;
    
    [Header("Debug")]
    public bool logDayState = true;
    
    private bool dayHasEnded = false;
    
    void Start()
    {
        // Auto-find dependencies
        if (orderManager == null)
        {
            orderManager = Resources.FindObjectsOfTypeAll<OrderManagerSO>().FirstOrDefault();
        }
        
        if (dayTimer == null)
        {
            dayTimer = FindObjectOfType<DayTimer>();
        }
        
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManagerController>();
        }
        
        // Hide day finished screen initially
        if (dayFinishedScreen != null)
        {
            dayFinishedScreen.SetActive(false);
        }
    }
    
    void Update()
    {
        if (dayHasEnded) return;
        
        // Check if day should end
        if (AreAllOrdersCleared())
        {
            HandleDayEnded();
        }
    }
    
    /// <summary>
    /// Check if all customer orders are cleared (completed or expired)
    /// </summary>
    private bool AreAllOrdersCleared()
    {
        if (orderManager == null) return false;
        
        // Check if there are any waiting orders
        var waitingOrders = orderManager.GetWaitingOrders();
        if (waitingOrders.Count > 0)
        {
            return false; // Still have waiting orders
        }
        
        // Check if there are any pinned orders
        var pinnedOrders = orderManager.GetPinnedOrders();
        if (pinnedOrders.Count > 0)
        {
            return false; // Still have pinned orders
        }
        
        // All orders are cleared (either completed or expired)
        return true;
    }
    
    /// <summary>
    /// Called when day ends (either all cleared or timer expired)
    /// </summary>
    public void HandleDayEnded()
    {
        if (dayHasEnded) return;
        
        dayHasEnded = true;
        
        // Stop day timer
        if (dayTimer != null)
        {
            dayTimer.StopTimer();
        }
        
        if (logDayState)
        {
            int finalScore = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
            Debug.Log($"[DayFinishedManager] Day ended! Final score: {finalScore}");
        }
        
        // Show day finished screen
        if (dayFinishedScreen != null)
        {
            dayFinishedScreen.SetActive(true);
            
            // Update screen with current score
            DayFinishedScreen screenScript = dayFinishedScreen.GetComponent<DayFinishedScreen>();
            if (screenScript != null)
            {
                int finalScore = scoreManager != null ? scoreManager.GetCurrentScore() : 0;
                screenScript.SetDayScore(finalScore);
            }
        }
        
        // Invoke event
        OnDayEnded?.Invoke();
    }
    
    /// <summary>
    /// Reset for new day
    /// </summary>
    public void ResetForNewDay()
    {
        dayHasEnded = false;
        
        if (dayFinishedScreen != null)
        {
            dayFinishedScreen.SetActive(false);
        }
        
        if (dayTimer != null)
        {
            dayTimer.InitializeTimer();
        }
    }
}

