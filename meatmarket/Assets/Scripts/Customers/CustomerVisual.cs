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
    
    [Tooltip("Time to wait before moving to new target (in seconds)")]
    public float waitTimeBetweenMovements = 1.5f;
    
    [Tooltip("Enable wandering movement")]
    public bool enableWandering = true;
    
    [Tooltip("Y offset for customer height (adjusts spawn height above/below ground)")]
    public float heightOffset = 0f;
    
    [Header("Waiting Area Boundaries")]
    [Tooltip("Center of the waiting area (set by CustomerSpawner)")]
    private Vector3 waitingAreaCenter = Vector3.zero;
    
    [Tooltip("Size of the waiting area (set by CustomerSpawner)")]
    private Vector3 waitingAreaSize = new Vector3(10, 2, 8);
    
    /// <summary>
    /// Set the waiting area boundaries (called by CustomerSpawner)
    /// </summary>
    public void SetWaitingArea(Vector3 center, Vector3 size)
    {
        waitingAreaCenter = center;
        waitingAreaSize = size;
    }
    
    [Header("Debug")]
    public bool logMovement = false;
    
    [Header("Customer State")]
    [Tooltip("Whether this customer's order is pinned")]
    public bool isPinned = false;
    
    [Tooltip("Whether this customer's order is completed")]
    public bool isCompleted = false;
    
    private CustomerOrder customerOrder;
    private CustomerArchetypeSO archetype;
    private Vector3 spawnPosition;
    private Vector3 currentTarget;
    private bool isMoving = false;
    private bool isWaiting = false;
    
    void Start()
    {
        spawnPosition = transform.position;
        
        // Start wandering immediately
        StartNewWanderCycle();
    }
    
    void Update()
    {
        if (!enableWandering) return;
        
        if (isMoving)
        {
            // Only process movement while moving
            MoveTowardsTarget();
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
        float distance = Vector3.Distance(transform.position, currentTarget);
        
        // Check if we're close enough to the target
        if (distance < 0.1f)
        {
            // Smoothly lerp to exact position
            float lerpSpeed = wanderSpeed * 2f; // Faster lerp when close
            transform.position = Vector3.Lerp(transform.position, currentTarget, lerpSpeed * Time.deltaTime);
            
            // Check if we're close enough to consider "reached"
            if (distance < 0.05f)
            {
                StartWaiting();
            }
        }
        else
        {
            // Normal movement with smooth lerping
            float t = Time.deltaTime * wanderSpeed / distance;
            transform.position = Vector3.Lerp(transform.position, currentTarget, t);
        }
    }
    
    private void StartWaiting()
    {
        isMoving = false;
        isWaiting = true;
        
        // Start coroutine to wait and then resume wandering
        StartCoroutine(WaitAndResumeWandering());
        
        if (logMovement) Debug.Log($"[CustomerVisual] Reached target, waiting {waitTimeBetweenMovements}s before next movement");
    }
    
    private System.Collections.IEnumerator WaitAndResumeWandering()
    {
        yield return new WaitForSeconds(waitTimeBetweenMovements);
        
        if (enableWandering && isWaiting)
        {
            StartNewWanderCycle();
        }
    }
    
    private void StartNewWanderCycle()
    {
        isWaiting = false;
        isMoving = true;
        
        // Generate a new target within the waiting area boundaries
        Vector3 halfSize = waitingAreaSize * 0.5f;
        Vector3 randomOffset = new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            heightOffset,
            Random.Range(-halfSize.z, halfSize.z)
        );
        
        currentTarget = waitingAreaCenter + randomOffset;
        
        if (logMovement) Debug.Log($"[CustomerVisual] New wander target: {currentTarget}");
    }
    
    /// <summary>
    /// Stop wandering and return to waiting area center
    /// </summary>
    public void ReturnToSpawn()
    {
        Vector3 returnPosition = waitingAreaCenter;
        returnPosition.y += heightOffset;
        currentTarget = returnPosition;
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
        if (pinned)
        {
            isCompleted = false; // Can't be both pinned and completed
        }
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
    /// Set whether this customer's order is completed
    /// </summary>
    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
        if (completed)
        {
            isPinned = false; // Can't be both pinned and completed
            enableWandering = false; // Stop wandering when completed
            isMoving = false;
            isWaiting = false;
        }
        if (logMovement) Debug.Log($"[CustomerVisual] Customer completed state: {completed}");
    }
    
    /// <summary>
    /// Check if this customer's order is completed
    /// </summary>
    public bool IsCompleted()
    {
        return isCompleted;
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
            isWaiting = false;
        }
        if (logMovement) Debug.Log($"[CustomerVisual] Wandering enabled: {enabled}");
    }
    
    /// <summary>
    /// Return customer to waiting area and resume roaming
    /// </summary>
    public void ReturnToWaitingArea()
    {
        SetWanderingEnabled(true);
        StartNewWanderCycle();
        if (logMovement) Debug.Log($"[CustomerVisual] Returning to waiting area and resuming roaming");
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

