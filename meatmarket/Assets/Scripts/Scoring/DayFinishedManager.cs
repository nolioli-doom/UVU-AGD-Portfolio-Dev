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
    
    [Tooltip("CustomerSpawner to stop spawning when day ends")]
    public CustomerSpawner customerSpawner;
    
    [Tooltip("DayFinishedScreen to display end-of-day stats (assigned in Inspector - works even when disabled)")]
    public DayFinishedScreen dayFinishedScreen;
    
    [Header("Events")]
    [Tooltip("Invoked when day ends (all orders cleared or timer expired)")]
    public UnityEvent OnDayEnded;
    
    [Header("Debug")]
    public bool logDayState = true;
    
    private bool dayHasEnded = false;
    private int lastWaitingCount = -1;
    private int lastPinnedCount = -1;
    
    /// <summary>
    /// Final score for the day (stored when day ends, read by DayFinishedScreen)
    /// </summary>
    public static int FinalDayScore { get; private set; } = 0;
    
    void Start()
    {
        if (logDayState)
        {
            Debug.Log($"[DayFinishedManager] Start() called on GameObject: {gameObject.name} (active: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy})");
        }
        
        // Reset interaction lock on new day start (in case static variable persisted across scene load)
        InteractionLockManager.UnlockInteractions();
        
        // Reset day ended flag
        dayHasEnded = false;
        
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
        
        if (customerSpawner == null)
        {
            customerSpawner = FindObjectOfType<CustomerSpawner>();
        }
        
        // Cache DayFinishedScreen reference if not assigned (find it once, works even if disabled)
        if (dayFinishedScreen == null)
        {
            dayFinishedScreen = Resources.FindObjectsOfTypeAll<DayFinishedScreen>().FirstOrDefault();
        }
        
        if (logDayState)
        {
            Debug.Log($"[DayFinishedManager] Dependencies - orderManager: {(orderManager != null ? "found" : "NULL")}, dayTimer: {(dayTimer != null ? "found" : "NULL")}, scoreManager: {(scoreManager != null ? "found" : "NULL")}, customerSpawner: {(customerSpawner != null ? "found" : "NULL")}, dayFinishedScreen: {(dayFinishedScreen != null ? "found" : "NULL")}");
        }
    }
    
    void Update()
    {
        if (dayHasEnded) return;
        
        // Only log when order counts change (to track state without spamming)
        if (orderManager != null && logDayState)
        {
            int waitingCount = orderManager.GetWaitingOrders().Count;
            int pinnedCount = orderManager.GetPinnedOrders().Count;
            
            if (waitingCount != lastWaitingCount || pinnedCount != lastPinnedCount)
            {
                Debug.Log($"[DayFinishedManager] Order state changed - Waiting: {waitingCount}, Pinned: {pinnedCount}");
                lastWaitingCount = waitingCount;
                lastPinnedCount = pinnedCount;
            }
        }
        
        // Check if day should end
        if (AreAllOrdersCleared())
        {
            if (logDayState)
            {
                Debug.Log("[DayFinishedManager] Update() - All orders cleared, calling HandleDayEnded()");
            }
            HandleDayEnded();
        }
    }
    
    /// <summary>
    /// Check if all customer orders are cleared (completed or expired)
    /// Day ends when ALL orders are cleared: both pinned = 0 AND waiting = 0
    /// OR when the timer expires (which clears everything)
    /// </summary>
    private bool AreAllOrdersCleared()
    {
        if (orderManager == null)
        {
            return false;
        }
        
        // Check if there are any pinned orders
        var pinnedOrders = orderManager.GetPinnedOrders();
        if (pinnedOrders != null && pinnedOrders.Count > 0)
        {
            return false; // Still have pinned orders
        }
        
        // Check if there are any waiting orders
        var waitingOrders = orderManager.GetWaitingOrders();
        if (waitingOrders != null && waitingOrders.Count > 0)
        {
            return false; // Still have waiting orders - day should continue
        }
        
        // All orders are cleared (either completed or expired)
        // Only log when we actually detect all cleared (this is the important moment)
        if (logDayState)
        {
            Debug.Log("[DayFinishedManager] AreAllOrdersCleared() - All orders cleared! Day should end.");
        }
        return true;
    }
    
    /// <summary>
    /// Called when day ends (either all cleared or timer expired)
    /// </summary>
    public void HandleDayEnded()
    {
        if (dayHasEnded) return;
        
        if (logDayState)
        {
            Debug.Log($"[DayFinishedManager] HandleDayEnded called on GameObject: {gameObject.name} (active: {gameObject.activeSelf}, activeInHierarchy: {gameObject.activeInHierarchy})");
        }
        
        dayHasEnded = true;
        
        // Stop day timer
        if (dayTimer != null)
        {
            dayTimer.StopTimer();
        }
        
        // Stop customer spawning
        if (customerSpawner != null)
        {
            customerSpawner.StopSpawning();
        }
        
        // Lock all interactions (raycast buttons and cut zones disabled during day end screen)
        InteractionLockManager.LockInteractions();
        
        // Get final score BEFORE invoking events (in case score gets reset)
        int finalScore = 0;
        if (scoreManager != null)
        {
            finalScore = scoreManager.GetCurrentScore();
        }
        
        // Store final score in static property as backup (DayFinishedScreen can read it in OnEnable)
        FinalDayScore = finalScore;
        
        if (logDayState)
        {
            if (scoreManager != null)
            {
                Debug.Log($"[DayFinishedManager] Day ended! Final score: {finalScore} (ScoreManager found at: {scoreManager.gameObject.name})");
            }
            else
            {
                Debug.LogWarning("[DayFinishedManager] Day ended! ScoreManagerController is NULL - cannot get final score");
            }
        }
        
        // Set score directly on DayFinishedScreen if we have a reference (best practice: serialized reference)
        // This works even if the GameObject is disabled
        if (dayFinishedScreen != null)
        {
            dayFinishedScreen.SetDayScore(finalScore);
            if (logDayState)
            {
                Debug.Log($"[DayFinishedManager] Set final score {finalScore} on DayFinishedScreen (using cached reference)");
            }
        }
        else
        {
            if (logDayState)
            {
                Debug.LogWarning("[DayFinishedManager] DayFinishedScreen reference not found - DayFinishedScreen.OnEnable() will read score from FinalDayScore static property");
            }
        }
        
        // Invoke event (wire up day finished screen activation in Unity Inspector)
        if (OnDayEnded != null)
        {
            int listenerCount = OnDayEnded.GetPersistentEventCount();
            Debug.Log($"[DayFinishedManager] Invoking OnDayEnded event with {listenerCount} listener(s)");
            OnDayEnded.Invoke();
            Debug.Log($"[DayFinishedManager] OnDayEnded event invoked");
        }
        else
        {
            Debug.LogWarning("[DayFinishedManager] OnDayEnded event is null! No listeners will be called.");
        }
    }
    
    /// <summary>
    /// Check if the day has ended (public for other scripts to check)
    /// </summary>
    public bool IsDayEnded()
    {
        return dayHasEnded;
    }
    
    /// <summary>
    /// Reset for new day
    /// </summary>
    public void ResetForNewDay()
    {
        dayHasEnded = false;
        lastWaitingCount = -1;
        lastPinnedCount = -1;
        
        if (dayTimer != null)
        {
            dayTimer.InitializeTimer();
        }
    }
}

