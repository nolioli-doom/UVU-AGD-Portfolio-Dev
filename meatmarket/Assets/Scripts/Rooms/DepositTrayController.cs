using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the deposit tray where severed parts are collected.
/// Handles deposit allocation to pinned orders and scoring.
/// Attach to deposit tray GameObject in scene.
/// </summary>
public class DepositTrayController : MonoBehaviour
{
    [Header("Configuration")]
    public OrderManagerSO orderManager;
    public ScoreConfigSO scoreConfig;
    public ScoreManagerController scoreManager;

    [Header("Debug")]
    public bool logDeposits = true;

    [Header("Events")]
    public UnityEvent OnDeposit;
    public UnityEvent OnWaste;
    public UnityEvent<int> OnScoreEarned;

    // Current contents of the tray
    private List<SeveredPartData> contents = new List<SeveredPartData>();

    /// <summary>
    /// Add a severed part to the tray (called by BodyPartTree via event or direct)
    /// </summary>
    public void AddPart(SeveredPartData part)
    {
        if (part == null)
        {
            Debug.LogWarning("[DepositTray] Attempted to add null part");
            return;
        }

        contents.Add(part);
        if (logDeposits) Debug.Log($"[DepositTray] Added: {part}");
    }

    /// <summary>
    /// Deposit all parts in tray to pinned orders (wire to Deposit button)
    /// </summary>
    public void Deposit()
    {
        if (contents.Count == 0)
        {
            if (logDeposits) Debug.Log("[DepositTray] No parts to deposit");
            return;
        }

        if (orderManager == null)
        {
            Debug.LogError("[DepositTray] OrderManager not assigned!");
            return;
        }

        if (logDeposits) Debug.Log($"[DepositTray] Deposit clicked - Processing {contents.Count} parts");

        var pinnedOrders = orderManager.GetPinnedOrders();
        if (pinnedOrders.Count == 0)
        {
            if (logDeposits) Debug.Log("[DepositTray] No pinned orders to fulfill");
            Waste();  // Auto-waste if no orders
            return;
        }

        // Process each part in tray
        List<SeveredPartData> unmatched = new List<SeveredPartData>();

        foreach (var part in contents)
        {
            bool matched = TryMatchPartToOrders(part, pinnedOrders);
            if (!matched)
            {
                unmatched.Add(part);
            }
        }

        // Report unmatched parts
        if (unmatched.Count > 0 && logDeposits)
        {
            Debug.Log($"[DepositTray] {unmatched.Count} parts did not match any orders:");
            foreach (var part in unmatched)
            {
                Debug.Log($"[DepositTray]   - {part}");
            }
        }

        // Clear tray
        contents.Clear();
        if (logDeposits) Debug.Log("[DepositTray] Tray cleared");

        OnDeposit?.Invoke();
    }

    /// <summary>
    /// Try to match a part to pinned orders (sequential: Order 1 → 2 → 3)
    /// </summary>
    bool TryMatchPartToOrders(SeveredPartData part, List<CustomerOrder> orders)
    {
        // Try each order in sequence
        for (int orderIdx = 0; orderIdx < orders.Count; orderIdx++)
        {
            var order = orders[orderIdx];

            // Skip completed orders
            if (order.items.All(item => item.IsComplete))
            {
                continue;
            }

            // Try each item in this order
            for (int itemIdx = 0; itemIdx < order.items.Count; itemIdx++)
            {
                var item = order.items[itemIdx];

                // Skip completed items
                if (item.IsComplete)
                {
                    continue;
                }

                // Check if part matches this item
                if (DoesPartMatchItem(part, item))
                {
                    // Match found! Calculate score and fulfill
                    int score = CalculateScore(part, item, order.archetype);
                    item.IncrementProgress(1);
                    
                    if (scoreManager != null)
                    {
                        scoreManager.AddScore(score);
                    }

                    OnScoreEarned?.Invoke(score);

                    if (logDeposits)
                    {
                        Debug.Log($"[DepositTray] ✓ Matched {part.segmentType} to Order {orderIdx+1}, Item {itemIdx+1} | Score: +{score}");
                        Debug.Log($"[DepositTray]   Progress: {item.currentQuantity}/{item.quantity} ({item.Progress*100:0}%)");
                    }

                    // Check if order is now complete
                    if (order.items.All(i => i.IsComplete))
                    {
                        if (logDeposits) Debug.Log($"[DepositTray] ★ Order {orderIdx+1} COMPLETE: \"{order.customerName}\"");
                    }

                    return true;  // Successfully matched
                }
            }
        }

        // No match found
        if (logDeposits) Debug.Log($"[DepositTray] ✗ No match for {part.segmentType} ({part.species})");
        return false;
    }

    /// <summary>
    /// Check if a part matches an order item.
    /// Uses generic matching: L_Hand and R_Hand both match "Hand" orders.
    /// </summary>
    bool DoesPartMatchItem(SeveredPartData part, OrderItem item)
    {
        // Must match species
        if (part.species != item.species)
            return false;

        // Convert specific segment to generic order type and check match
        if (!PartTypeMapper.DoesSegmentMatchOrderType(part.segmentType, item.partType))
            return false;

        // Must meet minimum quality (allow lower quality but with penalty in scoring)
        // For now, we'll accept any quality and let scoring handle it
        return true;
    }

    /// <summary>
    /// Calculate score for a matched part
    /// </summary>
    int CalculateScore(SeveredPartData part, OrderItem item, CustomerArchetypeSO archetype)
    {
        if (scoreConfig == null)
        {
            Debug.LogWarning("[DepositTray] ScoreConfig not assigned, using default score");
            return 10;
        }

        return scoreConfig.CalculatePartScore(part, item, archetype);
    }

    /// <summary>
    /// Discard all parts in tray (wire to Waste button)
    /// </summary>
    public void Waste()
    {
        if (contents.Count > 0)
        {
            if (logDeposits) Debug.Log($"[DepositTray] Waste clicked - Discarded {contents.Count} parts");
        }
        
        contents.Clear();
        OnWaste?.Invoke();
    }

    /// <summary>
    /// Print tray contents to console (wire to debug button)
    /// </summary>
    public void PrintTrayContents()
    {
        Debug.Log($"[DepositTray] === Tray Contents ({contents.Count} parts) ===");
        for (int i = 0; i < contents.Count; i++)
        {
            Debug.Log($"[DepositTray]   {i+1}. {contents[i]}");
        }
    }

    /// <summary>
    /// Get current tray contents count
    /// </summary>
    public int GetContentsCount()
    {
        return contents.Count;
    }
}

