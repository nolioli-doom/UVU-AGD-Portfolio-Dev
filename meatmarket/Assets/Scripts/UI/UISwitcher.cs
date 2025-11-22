using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Handles switching between Customer and Butchery scenes.
/// Switches cameras AND UI panels in one call.
/// </summary>
public class UISwitcher : MonoBehaviour
{
    [Header("Cameras")]
    [Tooltip("Camera for Customer Scene (where orders are taken)")]
    public Camera customerSceneCamera;
    
    [Tooltip("Camera for Butchery Scene (where orders are fulfilled)")]
    public Camera butcherySceneCamera;
    
    [Header("Customer Scene UI")]
    [Tooltip("UI elements that should be active in Customer Scene")]
    public List<GameObject> customerUIElements = new List<GameObject>();
    
    [Header("Butchery Scene UI")]
    [Tooltip("UI elements that should be active in Butchery Scene")]
    public List<GameObject> butcheryUIElements = new List<GameObject>();
    
    [Header("Configuration")]
    [Tooltip("Which scene is active on start")]
    public bool startInCustomerScene = true;
    
    [Header("Debug")]
    public bool logSwitches = true;
    
    [Header("Button Text (Optional)")]
    [Tooltip("Button text component to update when switching scenes")]
    public TextMeshProUGUI buttonText;
    
    [Tooltip("Text to show when in Customer Scene")]
    public string customerSceneButtonText = "Go to Butchery";
    
    [Tooltip("Text to show when in Butchery Scene")]
    public string butcherySceneButtonText = "Go to Customer";
    
    private bool isInCustomerScene;
    
    void Awake()
    {
        // Initialize state in Awake to ensure it's set before any Start() methods
        isInCustomerScene = startInCustomerScene;
    }
    
    void Start()
    {
        // Ensure state is correct and update scene
        if (logSwitches) Debug.Log($"[UISwitcher] Start() - startInCustomerScene={startInCustomerScene}, isInCustomerScene={isInCustomerScene}");
        
        // Force correct initial state
        isInCustomerScene = startInCustomerScene;
        UpdateSceneState();
        
        if (logSwitches) Debug.Log($"[UISwitcher] Started in {(isInCustomerScene ? "Customer" : "Butchery")} scene");
    }
    
    /// <summary>
    /// Switch to the other scene (call from button OnClick)
    /// </summary>
    public void SwitchScene()
    {
        bool previousState = isInCustomerScene;
        
        if (logSwitches) Debug.Log($"[UISwitcher] SwitchScene() called - Current state: {(previousState ? "Customer" : "Butchery")}, startInCustomerScene={startInCustomerScene}");
        
        // Flip the state
        isInCustomerScene = !isInCustomerScene;
        
        if (logSwitches) Debug.Log($"[UISwitcher] SwitchScene() - Previous: {(previousState ? "Customer" : "Butchery")}, New: {(isInCustomerScene ? "Customer" : "Butchery")}");
        
        UpdateSceneState();
        
        // When switching to butchery, reset all customers to roaming
        if (!isInCustomerScene)
        {
            ResetAllCustomersToRoaming();
        }
        
        if (logSwitches) Debug.Log($"[UISwitcher] Switched to {(isInCustomerScene ? "Customer" : "Butchery")} scene");
    }
    
    /// <summary>
    /// Switch to Customer Scene specifically
    /// </summary>
    public void SwitchToCustomerScene()
    {
        isInCustomerScene = true;
        UpdateSceneState();
        
        if (logSwitches) Debug.Log("[UISwitcher] Switched to Customer scene");
    }
    
    /// <summary>
    /// Switch to Butchery Scene specifically
    /// </summary>
    public void SwitchToButcheryScene()
    {
        isInCustomerScene = false;
        UpdateSceneState();
        
        // Reset all customers to roaming when switching to butchery
        ResetAllCustomersToRoaming();
        
        if (logSwitches) Debug.Log("[UISwitcher] Switched to Butchery scene");
    }
    
    /// <summary>
    /// Public method to reset customers to roaming (call from button OnClick)
    /// </summary>
    public void ResetCustomersToRoaming()
    {
        ResetAllCustomersToRoaming();
    }
    
    /// <summary>
    /// Reset all customers to roaming state (called when switching to butchery)
    /// </summary>
    private void ResetAllCustomersToRoaming()
    {
        CustomerQueueManager queueManager = FindObjectOfType<CustomerQueueManager>();
        if (queueManager != null)
        {
            var queuedCustomers = queueManager.GetAllQueuedCustomers();
            foreach (var customer in queuedCustomers)
            {
                customer.ReturnToWaitingArea();
            }
            queueManager.ClearQueue();
        }
        
        // Also reset all customers in the scene (in case some aren't in queue)
        CustomerVisual[] allCustomers = FindObjectsOfType<CustomerVisual>();
        foreach (var customer in allCustomers)
        {
            customer.ReturnToWaitingArea();
        }
        
        if (logSwitches) Debug.Log("[UISwitcher] Reset all customers to roaming state");
    }
    
    /// <summary>
    /// Update camera and UI state based on current scene
    /// </summary>
    private void UpdateSceneState()
    {
        // Update cameras
        if (customerSceneCamera != null) customerSceneCamera.enabled = isInCustomerScene;
        if (butcherySceneCamera != null) butcherySceneCamera.enabled = !isInCustomerScene;
        
        // Update Customer UI elements
        foreach (GameObject uiElement in customerUIElements)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(isInCustomerScene);
            }
        }
        
        // Update Butchery UI elements
        foreach (GameObject uiElement in butcheryUIElements)
        {
            if (uiElement != null)
            {
                uiElement.SetActive(!isInCustomerScene);
            }
        }
        
        // Update button text
        UpdateButtonText();
    }
    
    /// <summary>
    /// Update the button text based on current scene
    /// </summary>
    public void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isInCustomerScene ? customerSceneButtonText : butcherySceneButtonText;
        }
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
    
    /// <summary>
    /// Add a UI element to Customer Scene list
    /// </summary>
    public void AddCustomerUIElement(GameObject uiElement)
    {
        if (uiElement != null && !customerUIElements.Contains(uiElement))
        {
            customerUIElements.Add(uiElement);
        }
    }
    
    /// <summary>
    /// Add a UI element to Butchery Scene list
    /// </summary>
    public void AddButcheryUIElement(GameObject uiElement)
    {
        if (uiElement != null && !butcheryUIElements.Contains(uiElement))
        {
            butcheryUIElements.Add(uiElement);
        }
    }
    
    /// <summary>
    /// Remove a UI element from Customer Scene list
    /// </summary>
    public void RemoveCustomerUIElement(GameObject uiElement)
    {
        customerUIElements.Remove(uiElement);
    }
    
    /// <summary>
    /// Remove a UI element from Butchery Scene list
    /// </summary>
    public void RemoveButcheryUIElement(GameObject uiElement)
    {
        butcheryUIElements.Remove(uiElement);
    }
}
