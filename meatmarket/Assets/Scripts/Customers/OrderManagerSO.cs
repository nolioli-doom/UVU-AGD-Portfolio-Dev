using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ScriptableObject that manages all orders for the current day.
/// Persists across scenes and tracks pinning state.
/// Uses slot-based pinning system (max 3 slots).
/// Automatically clears on game start to prevent persistent test data.
/// </summary>
[CreateAssetMenu(menuName = "Butchery/Order Manager")]
public class OrderManagerSO : ScriptableObject
{
    [Header("Configuration")]
    [Min(1)] public int maxPinnedOrders = 3;

    [Header("Order States (Read-Only Inspector View)")]
    [Tooltip("All unpinned orders that can be pinned")]
    [SerializeField] private List<CustomerOrder> waitingOrders = new List<CustomerOrder>();
    
    [Tooltip("Slot 0: First pinned order (or null if empty)")]
    public CustomerOrder pinnedSlot0;
    
    [Tooltip("Slot 1: Second pinned order (or null if empty)")]
    public CustomerOrder pinnedSlot1;
    
    [Tooltip("Slot 2: Third pinned order (or null if empty)")]
    public CustomerOrder pinnedSlot2;
    
    [Tooltip("Orders that have been completed")]
    [SerializeField] private List<CustomerOrder> completedOrders = new List<CustomerOrder>();

    [System.Serializable] public class OrderCompletedEvent : UnityEvent<CustomerOrder> { }
    [System.Serializable] public class OrderExpiredEvent : UnityEvent<CustomerOrder> { }

    [Header("Events")]
    [Tooltip("Invoked when an order is completed (all items fulfilled)")]
    public OrderCompletedEvent OnOrderCompleted;
    
    [Tooltip("Invoked when an order expires (timer runs out)")]
    public OrderExpiredEvent OnOrderExpired;

    [Header("Debug")]
    public bool logActions = false;

    // Internal tracking
    private CustomerOrder[] pinnedOrdersSlots;
    private Dictionary<CustomerOrder, int> orderToSlotMap = new Dictionary<CustomerOrder, int>();

    /// <summary>
    /// Initialize arrays and clear data on startup
    /// </summary>
    void OnEnable()
    {
        // Initialize slot array
        if (pinnedOrdersSlots == null)
        {
            pinnedOrdersSlots = new CustomerOrder[3];
        }
        
        // Sync public inspector fields with internal array
        SyncInspectorFields();
        
        // Clear all data for fresh start
        waitingOrders.Clear();
        pinnedOrdersSlots[0] = null;
        pinnedOrdersSlots[1] = null;
        pinnedOrdersSlots[2] = null;
        completedOrders.Clear();
        orderToSlotMap.Clear();
        
        if (logActions) Debug.Log("[OrderManagerSO] Auto-cleared on enable - fresh start guaranteed");
    }

    /// <summary>
    /// Set all orders for the day (called by OrderGenerator)
    /// All orders start in waiting state
    /// </summary>
    public void SetOrders(List<CustomerOrder> orders)
    {
        waitingOrders = new List<CustomerOrder>(orders);
        
        // Clear all pinned and completed orders
        for (int i = 0; i < 3; i++)
        {
            pinnedOrdersSlots[i] = null;
        }
        completedOrders.Clear();
        orderToSlotMap.Clear();
        
        // Reset all order progress
        foreach (var order in waitingOrders)
        {
            if (order != null && order.items != null)
            {
                foreach (var item in order.items)
                {
                    item.ResetProgress();
                }
            }
        }
        
        SyncInspectorFields();
        
        if (logActions) Debug.Log($"[OrderManagerSO] Set {orders.Count} orders for the day (all in waiting state)");
    }
    
    /// <summary>
    /// Sync internal slot array with inspector-visible fields
    /// </summary>
    private void SyncInspectorFields()
    {
        pinnedSlot0 = pinnedOrdersSlots[0];
        pinnedSlot1 = pinnedOrdersSlots[1];
        pinnedSlot2 = pinnedOrdersSlots[2];
    }

    /// <summary>
    /// Pin an order to the first available slot (returns slot index or -1 if failed)
    /// </summary>
    public int PinOrder(CustomerOrder order)
    {
        if (order == null)
        {
            if (logActions) Debug.LogWarning("[OrderManagerSO] Cannot pin null order");
            return -1;
        }

        // Check if already pinned
        if (orderToSlotMap.ContainsKey(order))
        {
            int slotIndex = orderToSlotMap[order];
            if (logActions) Debug.Log($"[OrderManagerSO] Order \"{order.customerName}\" already pinned in slot {slotIndex}");
            return slotIndex;
        }

        // Find first available slot
        int availableSlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (pinnedOrdersSlots[i] == null)
            {
                availableSlot = i;
                break;
            }
        }

        if (availableSlot == -1)
        {
            if (logActions) Debug.Log($"[OrderManagerSO] Cannot pin order \"{order.customerName}\": all slots full");
            return -1;
        }

        // Pin to slot
        pinnedOrdersSlots[availableSlot] = order;
        orderToSlotMap[order] = availableSlot;

        // Remove from waiting orders
        waitingOrders.Remove(order);

        SyncInspectorFields();

        if (logActions) Debug.Log($"[OrderManagerSO] Pinned order \"{order.customerName}\" to slot {availableSlot}");
        
        return availableSlot;
    }

    /// <summary>
    /// Get all currently pinned orders in slot order
    /// </summary>
    public List<CustomerOrder> GetPinnedOrders()
    {
        List<CustomerOrder> pinned = new List<CustomerOrder>();
        for (int i = 0; i < 3; i++)
        {
            if (pinnedOrdersSlots[i] != null)
            {
                pinned.Add(pinnedOrdersSlots[i]);
            }
        }
        return pinned;
    }

    /// <summary>
    /// Check if an order is pinned
    /// </summary>
    public bool IsOrderPinned(CustomerOrder order)
    {
        if (order == null) return false;
        return orderToSlotMap.ContainsKey(order);
    }

    /// <summary>
    /// Get the slot index for a pinned order (returns -1 if not pinned)
    /// </summary>
    public int GetOrderSlotIndex(CustomerOrder order)
    {
        if (order == null) return -1;
        if (orderToSlotMap.ContainsKey(order))
        {
            return orderToSlotMap[order];
        }
        return -1;
    }

    /// <summary>
    /// Mark an order as complete and clear its slot
    /// Called by the deposit system when an order is fulfilled
    /// </summary>
    public void CompleteOrder(CustomerOrder order)
    {
        if (order == null) return;

        if (!orderToSlotMap.ContainsKey(order))
        {
            if (logActions) Debug.LogWarning($"[OrderManagerSO] Cannot complete order \"{order.customerName}\": not pinned");
            return;
        }

        int slotIndex = orderToSlotMap[order];

        // Remove from pinned slot
        pinnedOrdersSlots[slotIndex] = null;
        orderToSlotMap.Remove(order);

        // Add to completed orders
        completedOrders.Add(order);

        SyncInspectorFields();

        // Trigger completion event for UI/VFX
        OnOrderCompleted?.Invoke(order);

        if (logActions) Debug.Log($"[OrderManagerSO] Completed order \"{order.customerName}\" (cleared slot {slotIndex})");
    }

    /// <summary>
    /// Mark an order as expired and clear its slot (no reward)
    /// Called automatically when timer runs out
    /// </summary>
    public void ExpireOrder(CustomerOrder order)
    {
        if (order == null) return;

        if (!orderToSlotMap.ContainsKey(order))
        {
            if (logActions) Debug.LogWarning($"[OrderManagerSO] Cannot expire order \"{order.customerName}\": not pinned");
            return;
        }

        int slotIndex = orderToSlotMap[order];

        // Remove from pinned slot
        pinnedOrdersSlots[slotIndex] = null;
        orderToSlotMap.Remove(order);

        // Don't add to completed orders (expired orders don't count as completed)

        SyncInspectorFields();

        // Trigger expiration event for VFX/SFX
        OnOrderExpired?.Invoke(order);

        if (logActions) Debug.Log($"[OrderManagerSO] Expired order \"{order.customerName}\" (cleared slot {slotIndex}, no reward)");
    }

    /// <summary>
    /// Check if an order is complete (all items fulfilled)
    /// </summary>
    public bool IsOrderComplete(CustomerOrder order)
    {
        if (order == null || order.items == null) return false;
        return order.items.All(item => item.IsComplete);
    }

    /// <summary>
    /// Clear all orders and pins (for new day or reset)
    /// </summary>
    public void ClearAllOrders()
    {
        waitingOrders.Clear();
        for (int i = 0; i < 3; i++)
        {
            pinnedOrdersSlots[i] = null;
        }
        completedOrders.Clear();
        orderToSlotMap.Clear();
        SyncInspectorFields();
        if (logActions) Debug.Log("[OrderManagerSO] Cleared all orders");
    }

    /// <summary>
    /// Force clear all data (useful for debugging)
    /// </summary>
    [ContextMenu("Force Clear All Data")]
    public void ForceClearAllData()
    {
        waitingOrders.Clear();
        for (int i = 0; i < 3; i++)
        {
            pinnedOrdersSlots[i] = null;
        }
        completedOrders.Clear();
        orderToSlotMap.Clear();
        SyncInspectorFields();
        Debug.Log("[OrderManagerSO] FORCE CLEARED all data");
    }

    /// <summary>
    /// Get count of available pin slots (0, 1, 2, or 3)
    /// </summary>
    public int GetAvailablePinSlots()
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            if (pinnedOrdersSlots[i] == null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Get all waiting orders
    /// </summary>
    public List<CustomerOrder> GetWaitingOrders()
    {
        return new List<CustomerOrder>(waitingOrders);
    }

    /// <summary>
    /// Get all completed orders
    /// </summary>
    public List<CustomerOrder> GetCompletedOrders()
    {
        return new List<CustomerOrder>(completedOrders);
    }
    
    /// <summary>
    /// Update timers for all pinned orders (call from singleton every frame)
    /// Also checks for expired orders and clears them automatically
    /// </summary>
    public void UpdateTimers()
    {
        List<CustomerOrder> expiredOrders = new List<CustomerOrder>();
        
        for (int i = 0; i < 3; i++)
        {
            if (pinnedOrdersSlots[i] != null)
            {
                pinnedOrdersSlots[i].TickTimer(Time.deltaTime);
                
                // Check if order expired
                if (pinnedOrdersSlots[i].IsExpired())
                {
                    expiredOrders.Add(pinnedOrdersSlots[i]);
                }
            }
        }
        
        // Expire all expired orders (do this after iteration to avoid modifying collection)
        foreach (var expiredOrder in expiredOrders)
        {
            ExpireOrder(expiredOrder);
        }
    }
}

