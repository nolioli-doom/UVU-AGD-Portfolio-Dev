using UnityEngine;

/// <summary>
/// Global interaction lock manager.
/// Used to disable all raycast button and cut zone interactions during day end screen.
/// Static singleton pattern - accessible from anywhere without references.
/// </summary>
public static class InteractionLockManager
{
    /// <summary>
    /// Is interaction currently locked? (true = interactions disabled, false = interactions enabled)
    /// Defaults to false (unlocked) on scene load.
    /// </summary>
    public static bool IsLocked { get; private set; } = false;

    /// <summary>
    /// Lock all interactions (disable raycast buttons and cut zones).
    /// Called when day ends.
    /// </summary>
    public static void LockInteractions()
    {
        IsLocked = true;
        Debug.Log("[InteractionLockManager] Interactions LOCKED (raycast buttons and cut zones disabled)");
    }

    /// <summary>
    /// Unlock all interactions (enable raycast buttons and cut zones).
    /// Called on scene load/reset (typically not needed since scene reloads reset everything).
    /// </summary>
    public static void UnlockInteractions()
    {
        IsLocked = false;
        Debug.Log("[InteractionLockManager] Interactions UNLOCKED (raycast buttons and cut zones enabled)");
    }
}

