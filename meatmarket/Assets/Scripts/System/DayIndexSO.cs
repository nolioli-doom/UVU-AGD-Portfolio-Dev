using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject that persists the current day index across scene loads.
/// Used by CustomerSpawner, DayTimer, DayFinishedScreen, etc. to track which day the player is on.
/// 
/// This class exposes a static singleton-style <see cref="Instance"/> property that
/// caches the asset reference in a static field. Holding this static reference
/// prevents Unity from unloading the asset between scene loads, which ensures that
/// runtime changes to <see cref="currentDayIndex"/> persist reliably.
/// </summary>
[CreateAssetMenu(menuName = "Butchery/Day Index")]
public class DayIndexSO : ScriptableObject
{
    private static DayIndexSO _instance;

    /// <summary>
    /// Global accessor for the current DayIndexSO asset.
    /// Uses a cached static reference so the asset is not unloaded between scenes.
    /// </summary>
    public static DayIndexSO Instance
    {
        get
        {
            if (_instance == null)
            {
                // Find the asset even if it's not in Resources folder
                _instance = Resources.FindObjectsOfTypeAll<DayIndexSO>().FirstOrDefault();
                if (_instance == null)
                {
                    Debug.LogError("[DayIndexSO] Instance not found! Please create a DayIndexSO asset (e.g., 'CurrentDay') and ensure it is referenced in at least one scene.");
                }
            }
            return _instance;
        }
    }

    [Header("Day Index")]
    [Tooltip("Current day index (0 = first day, 1 = second day, etc.)")]
    public int currentDayIndex = 0;

    /// <summary>
    /// Ensure static Instance is set when the asset is loaded/enabled.
    /// </summary>
    private void OnEnable()
    {
        _instance = this;
    }
    
    /// <summary>
    /// Increment to the next day.
    /// </summary>
    public void IncrementDay()
    {
        int oldIndex = currentDayIndex;
        currentDayIndex++;
        Debug.Log($"[DayIndexSO] IncrementDay called: {oldIndex} → {currentDayIndex}");
    }
    
    /// <summary>
    /// Reset to day 0 (first day).
    /// </summary>
    public void ResetToDayZero()
    {
        currentDayIndex = 0;
    }
    
    /// <summary>
    /// Get the display day number (1-indexed for UI).
    /// </summary>
    public int GetDisplayDayNumber()
    {
        return currentDayIndex + 1;
    }
}

