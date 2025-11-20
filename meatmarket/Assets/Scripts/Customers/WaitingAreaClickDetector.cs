using UnityEngine;

/// <summary>
/// Large collider for detecting clicks on the waiting area.
/// When clicked, arranges customers into a queue behind the counter.
/// </summary>
public class WaitingAreaClickDetector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Customer spawner to get spawned customers")]
    public CustomerSpawner customerSpawner;
    
    [Header("Click Detection")]
    [Tooltip("Layer mask for click detection")]
    public LayerMask clickableLayerMask = -1;
    
    [Tooltip("Enable click detection")]
    public bool enableClickDetection = true;
    
    [Header("Debug")]
    public bool logClicks = true;
    
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // Auto-find components if not assigned
        if (customerSpawner == null)
            customerSpawner = FindObjectOfType<CustomerSpawner>();
    }
    
    void Update()
    {
        if (!enableClickDetection) return;
        
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            HandleClick();
        }
    }
    
    private void HandleClick()
    {
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayerMask))
        {
            // Check if we hit this collider
            if (hit.collider == GetComponent<Collider>())
            {
                OnWaitingAreaClicked(hit.point);
            }
        }
    }
    
    private void OnWaitingAreaClicked(Vector3 clickPosition)
    {
        if (logClicks) Debug.Log($"[WaitingAreaClickDetector] Waiting area clicked at {clickPosition}");
        
        // Get all spawned customers
        if (customerSpawner != null)
        {
            var customers = customerSpawner.GetSpawnedCustomers();
            
            if (customers.Count > 0)
            {
                // TODO: Arrange customers in queue (Phase 2)
                if (logClicks) Debug.Log($"[WaitingAreaClickDetector] {customers.Count} customers ready for queue arrangement (Phase 2)");
            }
            else
            {
                if (logClicks) Debug.Log("[WaitingAreaClickDetector] No customers to arrange in queue");
            }
        }
        else
        {
            Debug.LogWarning("[WaitingAreaClickDetector] No CustomerSpawner found!");
        }
    }
    
    /// <summary>
    /// Enable or disable click detection
    /// </summary>
    public void SetClickDetectionEnabled(bool enabled)
    {
        enableClickDetection = enabled;
    }
    
    /// <summary>
    /// Manually trigger queue arrangement (for testing)
    /// </summary>
    [ContextMenu("Arrange Customers in Queue")]
    public void ManualArrangeQueue()
    {
        if (customerSpawner != null)
        {
            var customers = customerSpawner.GetSpawnedCustomers();
            if (logClicks) Debug.Log($"[WaitingAreaClickDetector] Manual queue arrangement: {customers.Count} customers ready (Phase 2)");
        }
    }
}
