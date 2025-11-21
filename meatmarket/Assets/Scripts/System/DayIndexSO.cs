using UnityEngine;

/// <summary>
/// ScriptableObject that persists the current day index across scene loads.
/// Used by CustomerSpawner, DayTimer, and DayFinishedScreen to track which day the player is on.
/// </summary>
[CreateAssetMenu(menuName = "Butchery/Day Index")]
public class DayIndexSO : ScriptableObject
{
    [Header("Day Index")]
    [Tooltip("Current day index (0 = first day, 1 = second day, etc.)")]
    public int currentDayIndex = 0;
    
    /// <summary>
    /// Increment to the next day
    /// </summary>
    public void IncrementDay()
    {
        currentDayIndex++;
    }
    
    /// <summary>
    /// Reset to day 0 (first day)
    /// </summary>
    public void ResetToDayZero()
    {
        currentDayIndex = 0;
    }
    
    /// <summary>
    /// Get the display day number (1-indexed for UI)
    /// </summary>
    public int GetDisplayDayNumber()
    {
        return currentDayIndex + 1;
    }
}

