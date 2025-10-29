using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the circular queue of customers behind the counter.
/// Handles positioning, cycling through customers, and order pinning.
/// </summary>
public class CustomerQueueManager : MonoBehaviour
{
    [Header("Queue Configuration")]
    [Tooltip("Maximum number of customers that can be in the queue (0 = dynamic based on waiting customers)")]
    public int maxQueueSize = 0;
    
    [Tooltip("Radius of the circular queue arrangement on X axis")]
    public float queueRadiusX = 2f;
    
    [Tooltip("Radius of the circular queue arrangement on Z axis")]
    public float queueRadiusZ = 2f;
    
    [Tooltip("Height offset for customers in queue")]
    public float queueHeight = 0f;
    
    [Tooltip("Center point of the circular queue")]
    public Transform queueCenter;
    
    [Header("Queue Behavior")]
    [Tooltip("Speed of customer movement when entering/leaving queue")]
    public float moveSpeed = 3f;
    
    [Tooltip("Time to wait before removing completed customers")]
    public float removalDelay = 2f;
    
    [Header("Debug")]
    public bool logQueueActions = true;
    public bool showQueueGizmos = true;
    
    private List<CustomerVisual> queuedCustomers = new List<CustomerVisual>();
    private int currentIndex = 0; // Index of currently selected customer
    
    void Start()
    {
        // Auto-find queue center if not assigned
        if (queueCenter == null)
        {
            queueCenter = transform;
        }
        
        if (logQueueActions) Debug.Log("[CustomerQueueManager] Initialized with max queue size: " + maxQueueSize);
    }
    
    /// <summary>
    /// Add a customer to the queue
    /// </summary>
    public bool AddCustomerToQueue(CustomerVisual customer)
    {
        if (customer == null || customer.IsPinned()) return false;
        
        if (queuedCustomers.Contains(customer)) return false;
        
        // Use dynamic queue size if maxQueueSize is 0
        int effectiveMaxSize = maxQueueSize > 0 ? maxQueueSize : queuedCustomers.Count + 1;
        
        if (queuedCustomers.Count >= effectiveMaxSize)
        {
            if (logQueueActions) Debug.Log("[CustomerQueueManager] Queue is full, cannot add customer");
            return false;
        }
        
        // Stop customer wandering and add to queue
        customer.SetWanderingEnabled(false);
        queuedCustomers.Add(customer);
        
        // Position customer in queue
        PositionCustomerInQueue(customer, queuedCustomers.Count - 1);
        
        // Set as current selection if it's the first customer
        if (queuedCustomers.Count == 1)
        {
            currentIndex = 0;
        }
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] Added customer to queue. Queue size: {queuedCustomers.Count}");
        return true;
    }
    
    /// <summary>
    /// Remove a customer from the queue
    /// </summary>
    public void RemoveCustomerFromQueue(CustomerVisual customer)
    {
        if (!queuedCustomers.Contains(customer))
        {
            if (logQueueActions) Debug.Log("[CustomerQueueManager] Customer not in queue");
            return;
        }
        
        int index = queuedCustomers.IndexOf(customer);
        queuedCustomers.RemoveAt(index);
        
        // Adjust current index if necessary
        if (currentIndex >= queuedCustomers.Count && queuedCustomers.Count > 0)
        {
            currentIndex = queuedCustomers.Count - 1;
        }
        else if (queuedCustomers.Count == 0)
        {
            currentIndex = 0;
        }
        
        // Reposition remaining customers
        RepositionAllCustomers();
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] Removed customer from queue. Queue size: {queuedCustomers.Count}");
    }
    
    /// <summary>
    /// Cycle to the next customer in the queue
    /// </summary>
    public void NextCustomer()
    {
        if (queuedCustomers.Count <= 1) 
        {
            if (logQueueActions) Debug.Log("[CustomerQueueManager] NextCustomer: Not enough customers to cycle");
            return;
        }
        
        int oldIndex = currentIndex;
        currentIndex = (currentIndex + 1) % queuedCustomers.Count;
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] NextCustomer: Changed from index {oldIndex} to {currentIndex}");
        
        // Reposition all customers to show current selection at front
        RepositionAllCustomers();
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] Selected customer {currentIndex + 1} of {queuedCustomers.Count}");
    }
    
    /// <summary>
    /// Cycle to the previous customer in the queue
    /// </summary>
    public void PreviousCustomer()
    {
        if (queuedCustomers.Count <= 1) 
        {
            if (logQueueActions) Debug.Log("[CustomerQueueManager] PreviousCustomer: Not enough customers to cycle");
            return;
        }
        
        int oldIndex = currentIndex;
        currentIndex = (currentIndex - 1 + queuedCustomers.Count) % queuedCustomers.Count;
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] PreviousCustomer: Changed from index {oldIndex} to {currentIndex}");
        
        // Reposition all customers to show current selection at front
        RepositionAllCustomers();
        
        if (logQueueActions) Debug.Log($"[CustomerQueueManager] Selected customer {currentIndex + 1} of {queuedCustomers.Count}");
    }
    
    /// <summary>
    /// Rotate all customers clockwise (each moves to next position)
    /// </summary>
    private void RotateQueueClockwise()
    {
        if (queuedCustomers.Count <= 1) return;
        
        // Store the last customer
        CustomerVisual lastCustomer = queuedCustomers[queuedCustomers.Count - 1];
        
        // Shift all customers one position clockwise
        for (int i = queuedCustomers.Count - 1; i > 0; i--)
        {
            queuedCustomers[i] = queuedCustomers[i - 1];
        }
        
        // Put the last customer at the front
        queuedCustomers[0] = lastCustomer;
        
        // Reposition all customers
        RepositionAllCustomers();
    }
    
    /// <summary>
    /// Rotate all customers counterclockwise (each moves to previous position)
    /// </summary>
    private void RotateQueueCounterclockwise()
    {
        if (queuedCustomers.Count <= 1) return;
        
        // Store the first customer
        CustomerVisual firstCustomer = queuedCustomers[0];
        
        // Shift all customers one position counterclockwise
        for (int i = 0; i < queuedCustomers.Count - 1; i++)
        {
            queuedCustomers[i] = queuedCustomers[i + 1];
        }
        
        // Put the first customer at the end
        queuedCustomers[queuedCustomers.Count - 1] = firstCustomer;
        
        // Reposition all customers
        RepositionAllCustomers();
    }
    
    /// <summary>
    /// Get the currently selected customer
    /// </summary>
    public CustomerVisual GetCurrentCustomer()
    {
        if (queuedCustomers.Count == 0) return null;
        
        if (currentIndex >= queuedCustomers.Count)
        {
            if (logQueueActions) Debug.LogWarning($"[CustomerQueueManager] GetCurrentCustomer: currentIndex {currentIndex} out of range (queue size: {queuedCustomers.Count})");
            currentIndex = 0;
        }
        
        return queuedCustomers[currentIndex];
    }
    
    
    /// <summary>
    /// Get all customers currently in the queue
    /// </summary>
    public List<CustomerVisual> GetAllQueuedCustomers()
    {
        return new List<CustomerVisual>(queuedCustomers);
    }
    
    /// <summary>
    /// Check if the queue is full
    /// </summary>
    public bool IsQueueFull()
    {
        int effectiveMaxSize = maxQueueSize > 0 ? maxQueueSize : queuedCustomers.Count;
        return queuedCustomers.Count >= effectiveMaxSize;
    }
    
    /// <summary>
    /// Check if the queue is empty
    /// </summary>
    public bool IsQueueEmpty()
    {
        return queuedCustomers.Count == 0;
    }
    
    /// <summary>
    /// Clear all customers from the queue and return them to roaming
    /// </summary>
    public void ClearQueue()
    {
        foreach (var customer in queuedCustomers)
        {
            customer.SetWanderingEnabled(true);
        }
        
        queuedCustomers.Clear();
        currentIndex = 0;
        
        if (logQueueActions) Debug.Log("[CustomerQueueManager] Cleared all customers from queue");
    }
    
    private void PositionCustomerInQueue(CustomerVisual customer, int index)
    {
        if (queueCenter == null) return;
        
        // Calculate position in elliptical arrangement
        // First position (index 0) should be at -Z (closest to counter)
        float angle = (360f / queuedCustomers.Count) * index - 90f; // -90 degrees to start at bottom
        Vector3 offset = new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * queueRadiusX,
            queueHeight,
            Mathf.Sin(angle * Mathf.Deg2Rad) * queueRadiusZ
        );
        
        Vector3 targetPosition = queueCenter.position + offset;
        
        // Move customer to position
        StartCoroutine(MoveCustomerToPosition(customer, targetPosition));
    }
    
    private void RepositionAllCustomers()
    {
        for (int i = 0; i < queuedCustomers.Count; i++)
        {
            // Calculate position relative to current selection
            // Current customer (currentIndex) goes to position 0 (front)
            // Others are positioned around the circle
            int relativePosition = (i - currentIndex + queuedCustomers.Count) % queuedCustomers.Count;
            PositionCustomerInQueue(queuedCustomers[i], relativePosition);
        }
    }
    
    private System.Collections.IEnumerator MoveCustomerToPosition(CustomerVisual customer, Vector3 targetPosition)
    {
        Vector3 startPosition = customer.transform.position;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / moveSpeed;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            customer.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        customer.transform.position = targetPosition;
    }
    
    void OnDrawGizmos()
    {
        if (!showQueueGizmos) return;
        
        Vector3 center = queueCenter != null ? queueCenter.position : transform.position;
        
        // Draw elliptical queue positions
        Gizmos.color = Color.green;
        int queueSize = queuedCustomers.Count > 0 ? queuedCustomers.Count : (maxQueueSize > 0 ? maxQueueSize : 6);
        for (int i = 0; i < queueSize; i++)
        {
            float angle = (360f / queueSize) * i - 90f; // -90 degrees to start at bottom
            Vector3 offset = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * queueRadiusX,
                queueHeight,
                Mathf.Sin(angle * Mathf.Deg2Rad) * queueRadiusZ
            );
            
            Gizmos.DrawWireSphere(center + offset, 0.3f);
        }
        
        // Draw center point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.2f);
        
        // Draw elliptical outline
        Gizmos.color = Color.cyan;
        DrawEllipse(center, queueRadiusX, queueRadiusZ, queueHeight);
        
        // Draw current selection (shows which customer is at slot 0/front)
        if (queuedCustomers.Count > 0 && currentIndex < queuedCustomers.Count)
        {
            Gizmos.color = Color.red;
            // Get the actual customer at the front (slot 0)
            CustomerVisual frontCustomer = queuedCustomers[currentIndex];
            if (frontCustomer != null)
            {
                // Draw at the customer's actual position
                Gizmos.DrawWireSphere(frontCustomer.transform.position, 0.4f);
            }
        }
    }
    
    private void DrawEllipse(Vector3 center, float radiusX, float radiusZ, float height)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(
                Mathf.Cos(angle1) * radiusX,
                height,
                Mathf.Sin(angle1) * radiusZ
            );
            
            Vector3 point2 = center + new Vector3(
                Mathf.Cos(angle2) * radiusX,
                height,
                Mathf.Sin(angle2) * radiusZ
            );
            
            Gizmos.DrawLine(point1, point2);
        }
    }
}
