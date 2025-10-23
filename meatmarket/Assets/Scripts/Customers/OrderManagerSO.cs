using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject that manages all orders for the current day.
/// Persists across scenes and tracks pinning state.
/// </summary>
[CreateAssetMenu(menuName = "Butchery/Order Manager")]
public class OrderManagerSO : ScriptableObject
{
    [Header("Configuration")]
    [Min(1)] public int maxPinnedOrders = 3;

    [Header("Current State")]
    public List<CustomerOrder> allOrders = new List<CustomerOrder>();
    public List<int> pinnedOrderIndices = new List<int>();  // Indices of pinned orders

    [Header("Debug")]
    public bool logActions = true;

    /// <summary>
    /// Set all orders for the day (called by OrderGenerator)
    /// </summary>
    public void SetOrders(List<CustomerOrder> orders)
    {
        allOrders = orders;
        pinnedOrderIndices.Clear();
        
        // Reset all order progress
        foreach (var order in allOrders)
        {
            foreach (var item in order.items)
            {
                item.ResetProgress();
            }
        }
        
        if (logActions) Debug.Log($"[OrderManagerSO] Set {orders.Count} orders for the day");
    }

    /// <summary>
    /// Pin an order by index. Returns true if successful.
    /// </summary>
    public bool PinOrderAtIndex(int index)
    {
        if (index < 0 || index >= allOrders.Count)
        {
            Debug.LogWarning($"[OrderManagerSO] Invalid order index: {index}");
            return false;
        }

        if (pinnedOrderIndices.Contains(index))
        {
            if (logActions) Debug.Log($"[OrderManagerSO] Order {index} is already pinned");
            return false;
        }

        if (pinnedOrderIndices.Count >= maxPinnedOrders)
        {
            if (logActions) Debug.Log($"[OrderManagerSO] Cannot pin order {index}: max ({maxPinnedOrders}) reached");
            return false;
        }

        pinnedOrderIndices.Add(index);
        if (logActions) Debug.Log($"[OrderManagerSO] Pinned order {index}: \"{allOrders[index].customerName}\"");
        return true;
    }

    /// <summary>
    /// Unpin an order by index. Returns true if successful.
    /// </summary>
    public bool UnpinOrderAtIndex(int index)
    {
        if (pinnedOrderIndices.Remove(index))
        {
            if (logActions) Debug.Log($"[OrderManagerSO] Unpinned order {index}");
            return true;
        }
        
        if (logActions) Debug.Log($"[OrderManagerSO] Order {index} was not pinned");
        return false;
    }

    /// <summary>
    /// Check if an order is pinned
    /// </summary>
    public bool IsOrderPinned(int index)
    {
        return pinnedOrderIndices.Contains(index);
    }

    /// <summary>
    /// Get all currently pinned orders (in pinning order)
    /// </summary>
    public List<CustomerOrder> GetPinnedOrders()
    {
        return pinnedOrderIndices
            .Where(i => i >= 0 && i < allOrders.Count)
            .Select(i => allOrders[i])
            .ToList();
    }

    /// <summary>
    /// Get a specific order by index
    /// </summary>
    public CustomerOrder GetOrder(int index)
    {
        if (index >= 0 && index < allOrders.Count)
            return allOrders[index];
        return null;
    }

    /// <summary>
    /// Check if an order is complete (all items fulfilled)
    /// </summary>
    public bool IsOrderComplete(int index)
    {
        var order = GetOrder(index);
        if (order == null) return false;
        return order.items.All(item => item.IsComplete);
    }

    /// <summary>
    /// Clear all orders and pins (for new day or reset)
    /// </summary>
    public void ClearAllOrders()
    {
        allOrders.Clear();
        pinnedOrderIndices.Clear();
        if (logActions) Debug.Log("[OrderManagerSO] Cleared all orders");
    }

    /// <summary>
    /// Get count of available pin slots
    /// </summary>
    public int GetAvailablePinSlots()
    {
        return maxPinnedOrders - pinnedOrderIndices.Count;
    }
}

