using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Service responsible for allocating tray pieces to pinned orders.
/// Processes pieces in slot priority order (Slot 0 > Slot 1 > Slot 2).
/// Tracks allocation state and triggers events for UI/VFX integration.
/// </summary>
public class AllocationService : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Order manager to access pinned orders")]
    public OrderManagerSO orderManager;
    
    [Tooltip("Deposit tray containing pieces to allocate")]
    public DepositTrayController depositTray;

    [System.Serializable] public class PieceAllocatedEvent : UnityEvent<CustomerOrder, OrderItem, TrayPiece> { }
    [System.Serializable] public class PieceUnmatchedEvent : UnityEvent<TrayPiece> { }
    [System.Serializable] public class OrderCompletedEvent : UnityEvent<CustomerOrder> { }

    [Header("Events")]
    [Tooltip("Invoked when a piece is successfully allocated to an order item")]
    public PieceAllocatedEvent OnPieceAllocated;
    
    [Tooltip("Invoked when a piece cannot be matched to any pinned order")]
    public PieceUnmatchedEvent OnPieceUnmatched;
    
    [Tooltip("Invoked when an order becomes fully complete (all items fulfilled)")]
    public OrderCompletedEvent OnOrderCompleted;

    [Header("Debug")]
    public bool logAllocations = true;

    /// <summary>
    /// Auto-find dependencies if not assigned
    /// </summary>
    void Awake()
    {
        if (orderManager == null)
        {
            orderManager = Resources.FindObjectsOfTypeAll<OrderManagerSO>().FirstOrDefault();
            if (orderManager == null)
            {
                Debug.LogWarning("[AllocationService] OrderManagerSO not found! Allocation will not work.");
            }
        }

        if (depositTray == null)
        {
            depositTray = FindObjectOfType<DepositTrayController>();
            if (depositTray == null)
            {
                Debug.LogWarning("[AllocationService] DepositTrayController not found! Allocation will not work.");
            }
        }
    }

    /// <summary>
    /// Attempt to allocate all tray pieces to pinned orders.
    /// Processes in slot priority order and leaves unmatched pieces on the tray.
    /// Returns allocation summary for logging.
    /// </summary>
    public AllocationSummary AllocateAllPieces()
    {
        var summary = new AllocationSummary();
        
        if (depositTray == null || orderManager == null)
        {
            Debug.LogError("[AllocationService] Missing dependencies!");
            return summary;
        }

        // Get pinned orders in slot order (0, 1, 2)
        var pinnedOrders = orderManager.GetPinnedOrders();
        if (pinnedOrders.Count == 0)
        {
            if (logAllocations) Debug.Log("[AllocationService] No pinned orders to allocate to");
            // Mark all pieces as unmatched
            foreach (var piece in depositTray.trayPieces)
            {
                summary.unmatchedPieces.Add(piece);
                OnPieceUnmatched?.Invoke(piece);
            }
            return summary;
        }

        // Process each piece and try to allocate it
        var piecesToRemove = new List<TrayPiece>();
        
        foreach (var piece in depositTray.trayPieces)
        {
            bool allocated = false;
            
            // Try to allocate to orders in slot priority (0, 1, 2)
            foreach (var order in pinnedOrders)
            {
                if (order == null || order.IsExpired()) continue;
                
                // Try to find a matching incomplete item in this order
                OrderItem matchingItem = FindMatchingIncompleteItem(order, piece);
                if (matchingItem != null)
                {
                    // Allocate piece to this item
                    matchingItem.IncrementProgress(1);
                    summary.allocatedCount++;
                    summary.allocationDetails.Add(new AllocationDetail
                    {
                        piece = piece,
                        order = order,
                        item = matchingItem,
                        slotIndex = orderManager.GetOrderSlotIndex(order)
                    });
                    
                    OnPieceAllocated?.Invoke(order, matchingItem, piece);
                    
                    // Check if this allocation completed the order
                    if (orderManager.IsOrderComplete(order))
                    {
                        summary.completedOrders.Add(order);
                        
                        // Complete the order (clears slot and moves to completed list)
                        orderManager.CompleteOrder(order);
                        
                        // Trigger event for VFX/SFX
                        OnOrderCompleted?.Invoke(order);
                        
                        if (logAllocations)
                        {
                            Debug.Log($"[AllocationService] Order \"{order.customerName}\" COMPLETED and cleared from slot!");
                        }
                    }
                    
                    allocated = true;
                    piecesToRemove.Add(piece);
                    break; // Move to next piece
                }
            }
            
            if (!allocated)
            {
                // No match found - leave on tray and trigger event
                summary.unmatchedPieces.Add(piece);
                OnPieceUnmatched?.Invoke(piece);
            }
        }
        
        // Remove allocated pieces from tray
        foreach (var piece in piecesToRemove)
        {
            depositTray.trayPieces.Remove(piece);
        }
        
        if (logAllocations)
        {
            Debug.Log($"[AllocationService] Allocated {summary.allocatedCount} pieces, {summary.unmatchedPieces.Count} unmatched, {summary.completedOrders.Count} orders completed");
        }
        
        return summary;
    }

    /// <summary>
    /// Find the first incomplete OrderItem in an order that matches the given piece.
    /// Matches on species and partType.
    /// </summary>
    private OrderItem FindMatchingIncompleteItem(CustomerOrder order, TrayPiece piece)
    {
        if (order == null || order.items == null) return null;
        
        foreach (var item in order.items)
        {
            if (item.IsComplete) continue; // Skip already-complete items
            
            // Match species and partType
            if (item.species == piece.species && item.partType == piece.partType)
            {
                return item;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Allocation result summary for logging/debugging
    /// </summary>
    [System.Serializable]
    public class AllocationSummary
    {
        public int allocatedCount;
        public List<TrayPiece> unmatchedPieces = new List<TrayPiece>();
        public List<CustomerOrder> completedOrders = new List<CustomerOrder>();
        public List<AllocationDetail> allocationDetails = new List<AllocationDetail>();

        public override string ToString()
        {
            return $"Allocated: {allocatedCount} | Unmatched: {unmatchedPieces.Count} | Completed: {completedOrders.Count}";
        }
    }

    /// <summary>
    /// Detail record of a single allocation
    /// </summary>
    [System.Serializable]
    public class AllocationDetail
    {
        public TrayPiece piece;
        public CustomerOrder order;
        public OrderItem item;
        public int slotIndex;
    }
}
