using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the day timer that counts down from the difficulty's dayTimerDuration.
/// When timer hits zero, ends the day and fails all remaining orders.
/// </summary>
public class DayTimer : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("OrderGenerator to get current difficulty settings")]
    public OrderGenerator orderGenerator;
    
    [Tooltip("OrderManagerSO to fail remaining orders when timer expires")]
    public OrderManagerSO orderManager;
    
    [Tooltip("DayFinishedManager to trigger day end")]
    public DayFinishedManager dayFinishedManager;
    
    [Header("State (Read-Only)")]
    [Tooltip("Current remaining time in seconds")]
    public float remainingTime = 0f;
    
    [Tooltip("Whether the timer is currently running")]
    public bool isRunning = false;
    
    [Header("Events")]
    [Tooltip("Invoked when timer hits zero")]
    public UnityEvent OnTimerExpired;
    
    [Tooltip("Invoked every second with remaining time (for UI updates)")]
    public UnityEvent<float> OnTimerTick;
    
    [Header("Debug")]
    public bool logTimerEvents = true;
    
    private float dayTimerDuration = 300f;
    private bool dayEnded = false;
    
    void Start()
    {
        // Auto-find dependencies
        if (orderGenerator == null)
        {
            orderGenerator = FindObjectOfType<OrderGenerator>();
        }
        
        if (orderManager == null)
        {
            orderManager = Resources.FindObjectsOfTypeAll<OrderManagerSO>().FirstOrDefault();
        }
        
        if (dayFinishedManager == null)
        {
            dayFinishedManager = FindObjectOfType<DayFinishedManager>();
        }
        
        // Initialize timer from current difficulty
        InitializeTimer();
    }
    
    /// <summary>
    /// Initialize timer from current difficulty settings
    /// </summary>
    public void InitializeTimer()
    {
        if (orderGenerator == null)
        {
            Debug.LogError("[DayTimer] OrderGenerator not found! Cannot initialize timer.");
            return;
        }
        
        // Get current day index from DayIndexSO
        DayIndexSO dayIndexSO = Resources.FindObjectsOfTypeAll<DayIndexSO>().FirstOrDefault();
        if (dayIndexSO == null)
        {
            Debug.LogError("[DayTimer] DayIndexSO not found! Cannot get day index.");
            return;
        }
        
        int dayIndex = dayIndexSO.currentDayIndex;
        var difficulty = orderGenerator.GetDifficultyForDay(dayIndex);
        
        if (difficulty == null)
        {
            Debug.LogError($"[DayTimer] No difficulty found for day {dayIndex}!");
            dayTimerDuration = 300f; // Fallback
        }
        else
        {
            dayTimerDuration = difficulty.dayTimerDuration;
        }
        
        remainingTime = dayTimerDuration;
        isRunning = true;
        dayEnded = false;
        
        if (logTimerEvents)
        {
            Debug.Log($"[DayTimer] Initialized with {dayTimerDuration}s duration (Day {dayIndex})");
        }
    }
    
    void Update()
    {
        if (!isRunning || dayEnded) return;
        
        remainingTime -= Time.deltaTime;
        remainingTime = Mathf.Max(0f, remainingTime);
        
        // Invoke tick event every second
        if (Mathf.FloorToInt(remainingTime) != Mathf.FloorToInt(remainingTime + Time.deltaTime))
        {
            OnTimerTick?.Invoke(remainingTime);
        }
        
        // Check if timer expired
        if (remainingTime <= 0f && !dayEnded)
        {
            OnTimerExpired?.Invoke();
            EndDayDueToTimer();
        }
    }
    
    /// <summary>
    /// End the day because timer expired - fail all remaining orders
    /// </summary>
    private void EndDayDueToTimer()
    {
        dayEnded = true;
        isRunning = false;
        
        if (logTimerEvents)
        {
            Debug.Log("[DayTimer] Day timer expired! Failing all remaining orders.");
        }
        
        // Fail all pinned orders (reward 0)
        if (orderManager != null)
        {
            var pinnedOrders = orderManager.GetPinnedOrders();
            foreach (var order in pinnedOrders)
            {
                if (order != null && !order.IsExpired())
                {
                    // Expire the order (no reward)
                    orderManager.ExpireOrder(order);
                }
            }
            
            // Clear all waiting orders (they never got pinned, so no score)
            // This ensures the day ends immediately
            orderManager.ClearAllOrders();
        }
        
        // Notify DayFinishedManager
        if (dayFinishedManager != null)
        {
            dayFinishedManager.HandleDayEnded();
        }
    }
    
    /// <summary>
    /// Stop the timer (called when day ends normally)
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
        if (logTimerEvents)
        {
            Debug.Log("[DayTimer] Timer stopped.");
        }
    }
    
    /// <summary>
    /// Get formatted time string (MM:SS)
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}

