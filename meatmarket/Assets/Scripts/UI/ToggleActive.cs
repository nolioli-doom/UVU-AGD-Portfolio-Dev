using UnityEngine;

/// <summary>
/// Simple component that provides methods to toggle GameObject active state.
/// Can be called from Unity Events.
/// </summary>
public class ToggleActive : MonoBehaviour
{
    [Header("Target GameObject")]
    [Tooltip("GameObject to toggle (if null, toggles this GameObject)")]
    public GameObject targetObject;
    
    [Header("Debug")]
    public bool logToggles = false;
    
    void Awake()
    {
        // If no target specified, use this GameObject
        if (targetObject == null)
        {
            targetObject = gameObject;
        }
    }
    
    /// <summary>
    /// Toggle the GameObject's active state (call from Unity Events)
    /// </summary>
    public void Toggle()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[ToggleActive] Target GameObject is null!");
            return;
        }
        
        bool newState = !targetObject.activeSelf;
        targetObject.SetActive(newState);
        
        if (logToggles)
        {
            Debug.Log($"[ToggleActive] Toggled {targetObject.name} to {(newState ? "active" : "inactive")}");
        }
    }
    
    /// <summary>
    /// Set GameObject to active (call from Unity Events)
    /// </summary>
    public void SetActive()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[ToggleActive] Target GameObject is null!");
            return;
        }
        
        targetObject.SetActive(true);
        
        if (logToggles)
        {
            Debug.Log($"[ToggleActive] Set {targetObject.name} to active");
        }
    }
    
    /// <summary>
    /// Set GameObject to inactive (call from Unity Events)
    /// </summary>
    public void SetInactive()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[ToggleActive] Target GameObject is null!");
            return;
        }
        
        targetObject.SetActive(false);
        
        if (logToggles)
        {
            Debug.Log($"[ToggleActive] Set {targetObject.name} to inactive");
        }
    }
}

