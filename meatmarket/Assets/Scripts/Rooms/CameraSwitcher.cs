using UnityEngine;

/// <summary>
/// Simple camera switching system for testing between Customer and Butchery scenes.
/// Attach to a GameObject with a Button component.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("Camera for Customer Scene (where orders are taken)")]
    public Camera customerSceneCamera;
    
    [Tooltip("Camera for Butchery Scene (where orders are fulfilled)")]
    public Camera butcherySceneCamera;
    
    [Header("Configuration")]
    [Tooltip("Which camera is active on start")]
    public bool startInCustomerScene = true;
    
    [Header("Debug")]
    public bool logSwitches = true;
    
    private bool isInCustomerScene;
    
    void Start()
    {
        // Set initial camera state
        isInCustomerScene = startInCustomerScene;
        UpdateCameraState();
        
        if (logSwitches) Debug.Log($"[CameraSwitcher] Started in {(isInCustomerScene ? "Customer" : "Butchery")} scene");
    }
    
    /// <summary>
    /// Switch to the other scene (call from button OnClick)
    /// </summary>
    public void SwitchScene()
    {
        isInCustomerScene = !isInCustomerScene;
        UpdateCameraState();
        
        if (logSwitches) Debug.Log($"[CameraSwitcher] Switched to {(isInCustomerScene ? "Customer" : "Butchery")} scene");
    }
    
    /// <summary>
    /// Switch to Customer Scene specifically
    /// </summary>
    public void SwitchToCustomerScene()
    {
        isInCustomerScene = true;
        UpdateCameraState();
        
        if (logSwitches) Debug.Log("[CameraSwitcher] Switched to Customer scene");
    }
    
    /// <summary>
    /// Switch to Butchery Scene specifically
    /// </summary>
    public void SwitchToButcheryScene()
    {
        isInCustomerScene = false;
        UpdateCameraState();
        
        if (logSwitches) Debug.Log("[CameraSwitcher] Switched to Butchery scene");
    }
    
    /// <summary>
    /// Update which camera is active
    /// </summary>
    private void UpdateCameraState()
    {
        if (customerSceneCamera != null) customerSceneCamera.enabled = isInCustomerScene;
        if (butcherySceneCamera != null) butcherySceneCamera.enabled = !isInCustomerScene;
    }
    
    /// <summary>
    /// Get current scene name
    /// </summary>
    public string GetCurrentSceneName()
    {
        return isInCustomerScene ? "Customer" : "Butchery";
    }
    
    /// <summary>
    /// Check if currently in customer scene
    /// </summary>
    public bool IsInCustomerScene()
    {
        return isInCustomerScene;
    }
}
