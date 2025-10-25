using UnityEngine;

/// <summary>
/// Handles the visual representation of a customer.
/// Manages wandering behavior.
/// </summary>
public class CustomerVisual : MonoBehaviour
{
    
    [Header("Wandering Behavior")]
    [Tooltip("Speed of wandering movement")]
    public float wanderSpeed = 1f;
    
    [Tooltip("Time between direction changes")]
    public float directionChangeInterval = 2f;
    
    [Tooltip("Enable wandering movement")]
    public bool enableWandering = true;
    
    [Header("Waiting Area Boundaries")]
    [Tooltip("Center of the waiting area")]
    public Vector3 waitingAreaCenter = Vector3.zero;
    
    [Tooltip("Size of the waiting area (X, Y, Z)")]
    public Vector3 waitingAreaSize = new Vector3(10, 2, 8);
    
    [Header("Debug")]
    public bool logMovement = false;
    
    [Header("Customer State")]
    [Tooltip("Whether this customer's order is pinned")]
    public bool isPinned = false;
    
    private CustomerOrder customerOrder;
    private CustomerArchetypeSO archetype;
    private Vector3 spawnPosition;
    private Vector3 currentTarget;
    private float lastDirectionChange;
    private bool isMoving = false;
    
    void Start()
    {
        spawnPosition = transform.position;
        lastDirectionChange = Time.time;
        
        // Set initial target
        SetNewWanderTarget();
    }
    
    void Update()
    {
        if (enableWandering && isMoving)
        {
            MoveTowardsTarget();
            
            // Change direction periodically
            if (Time.time - lastDirectionChange >= directionChangeInterval)
            {
                SetNewWanderTarget();
                lastDirectionChange = Time.time;
            }
        }
    }
    
    /// <summary>
    /// Set up this customer with order and archetype data
    /// </summary>
    public void SetupCustomer(CustomerOrder order, CustomerArchetypeSO archetype)
    {
        customerOrder = order;
        this.archetype = archetype;
        
        if (logMovement) Debug.Log($"[CustomerVisual] Set up customer: {order.customerName} with {order.items?.Count ?? 0} items (Order ID: {order.GetHashCode()}) - GameObject: {gameObject.name}");
    }
    
    private void MoveTowardsTarget()
    {
        Vector3 direction = (currentTarget - transform.position).normalized;
        transform.position += direction * wanderSpeed * Time.deltaTime;
        
        // Check if we've reached the target
        if (Vector3.Distance(transform.position, currentTarget) < 0.1f)
        {
            SetNewWanderTarget();
        }
    }
    
    private void SetNewWanderTarget()
    {
        // Generate a new target within the waiting area boundaries
        Vector3 halfSize = waitingAreaSize * 0.5f;
        Vector3 randomOffset = new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            0,
            Random.Range(-halfSize.z, halfSize.z)
        );
        
        currentTarget = waitingAreaCenter + randomOffset;
        isMoving = true;
        
        if (logMovement) Debug.Log($"[CustomerVisual] New wander target: {currentTarget}");
    }
    
    /// <summary>
    /// Stop wandering and return to waiting area center
    /// </summary>
    public void ReturnToSpawn()
    {
        currentTarget = waitingAreaCenter;
        enableWandering = false;
        isMoving = true;
        
        if (logMovement) Debug.Log("[CustomerVisual] Returning to waiting area center");
    }
    
    /// <summary>
    /// Get the customer's order data
    /// </summary>
    public CustomerOrder GetOrder()
    {
        return customerOrder;
    }
    
    /// <summary>
    /// Get the customer's archetype
    /// </summary>
    public CustomerArchetypeSO GetArchetype()
    {
        return archetype;
    }
    
    /// <summary>
    /// Set whether this customer is pinned
    /// </summary>
    public void SetPinned(bool pinned)
    {
        isPinned = pinned;
        if (logMovement) Debug.Log($"[CustomerVisual] Customer pinned state: {pinned}");
    }
    
    /// <summary>
    /// Check if this customer is pinned
    /// </summary>
    public bool IsPinned()
    {
        return isPinned;
    }
    
    /// <summary>
    /// Set wandering enabled/disabled
    /// </summary>
    public void SetWanderingEnabled(bool enabled)
    {
        enableWandering = enabled;
        if (!enabled)
        {
            isMoving = false;
        }
        if (logMovement) Debug.Log($"[CustomerVisual] Wandering enabled: {enabled}");
    }
    
    void OnDrawGizmos()
    {
        // Draw waiting area boundaries
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(waitingAreaCenter, waitingAreaSize);
        
        // Draw waiting area center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(waitingAreaCenter, 0.3f);
        
        // Draw current target
        if (isMoving)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(currentTarget, 0.2f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }
}
