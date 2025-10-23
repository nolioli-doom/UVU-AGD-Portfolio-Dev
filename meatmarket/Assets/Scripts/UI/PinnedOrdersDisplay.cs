using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple fixed display for 3 pinned orders in the Butchery Scene.
/// Shows order info, timer, and item progress in whitebox UI.
/// </summary>
public class PinnedOrdersDisplay : MonoBehaviour
{
    [Header("Configuration")]
    public OrderManagerSO orderManager;

    [Header("Order Slot 1")]
    public GameObject slot1Root;
    public Text slot1CustomerName;
    public Text slot1Timer;
    public Text slot1Items;

    [Header("Order Slot 2")]
    public GameObject slot2Root;
    public Text slot2CustomerName;
    public Text slot2Timer;
    public Text slot2Items;

    [Header("Order Slot 3")]
    public GameObject slot3Root;
    public Text slot3CustomerName;
    public Text slot3Timer;
    public Text slot3Items;

    [Header("Refresh")]
    [Tooltip("How often to refresh display (0 = every frame)")]
    public float refreshInterval = 0.5f;

    [Header("Styling")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;

    [Header("Debug")]
    public bool logUpdates = false;

    private float refreshTimer = 0f;
    private float[] orderTimers = new float[3]; // Track remaining time for each order

    void Update()
    {
        if (orderManager == null) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshDisplay();
        }

        // Tick down timers for active orders
        UpdateTimers();
    }

    /// <summary>
    /// Refresh all order slots
    /// </summary>
    public void RefreshDisplay()
    {
        if (orderManager == null)
        {
            Debug.LogWarning("[PinnedOrdersDisplay] OrderManager not assigned");
            return;
        }

        var pinnedOrders = orderManager.GetPinnedOrders();

        if (logUpdates)
        {
            Debug.Log($"[PinnedOrdersDisplay] Refreshing with {pinnedOrders.Count} pinned orders");
        }

        // Update slot 1
        UpdateSlot(0, pinnedOrders, slot1Root, slot1CustomerName, slot1Timer, slot1Items);
        
        // Update slot 2
        UpdateSlot(1, pinnedOrders, slot2Root, slot2CustomerName, slot2Timer, slot2Items);
        
        // Update slot 3
        UpdateSlot(2, pinnedOrders, slot3Root, slot3CustomerName, slot3Timer, slot3Items);
    }

    /// <summary>
    /// Update a single order slot
    /// </summary>
    void UpdateSlot(int slotIndex, System.Collections.Generic.List<CustomerOrder> orders, 
                    GameObject slotRoot, Text nameText, Text timerText, Text itemsText)
    {
        if (slotIndex < orders.Count)
        {
            // Order exists for this slot
            var order = orders[slotIndex];
            
            if (slotRoot != null) slotRoot.SetActive(true);

            // Update customer name
            if (nameText != null)
            {
                string archetypeName = (order.archetype != null && !string.IsNullOrEmpty(order.archetype.displayName))
                    ? order.archetype.displayName
                    : "—";
                nameText.text = $"[{slotIndex + 1}] {order.customerName}\n({archetypeName})";
                nameText.color = activeColor;
            }

            // Update timer
            if (timerText != null)
            {
                float timeRemaining = orderTimers[slotIndex];
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = $"Time: {minutes:00}:{seconds:00}";
                
                // Color code based on urgency
                if (timeRemaining < 30f)
                    timerText.color = Color.red;
                else if (timeRemaining < 60f)
                    timerText.color = Color.yellow;
                else
                    timerText.color = activeColor;
            }

            // Update items with progress
            if (itemsText != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Items:");
                
                foreach (var item in order.items)
                {
                    string checkmark = item.IsComplete ? "✓" : "○";
                    string progress = $"{item.currentQuantity}/{item.quantity}";
                    sb.AppendLine($"{checkmark} {progress}× {item.species} {item.partType} (≥{item.minQuality})");
                }
                
                itemsText.text = sb.ToString().TrimEnd();
                itemsText.color = activeColor;
            }
        }
        else
        {
            // No order for this slot - hide or show empty state
            if (slotRoot != null) slotRoot.SetActive(false);
            
            if (nameText != null)
            {
                nameText.text = $"[{slotIndex + 1}] (Empty Slot)";
                nameText.color = inactiveColor;
            }
            
            if (timerText != null)
            {
                timerText.text = "—";
                timerText.color = inactiveColor;
            }
            
            if (itemsText != null)
            {
                itemsText.text = "No order pinned";
                itemsText.color = inactiveColor;
            }
        }
    }

    /// <summary>
    /// Update timers for active orders
    /// </summary>
    void UpdateTimers()
    {
        if (orderManager == null) return;

        var pinnedOrders = orderManager.GetPinnedOrders();
        
        for (int i = 0; i < 3; i++)
        {
            if (i < pinnedOrders.Count)
            {
                // Initialize timer if needed
                if (orderTimers[i] <= 0f)
                {
                    orderTimers[i] = pinnedOrders[i].timeLimitSeconds;
                }
                
                // Tick down (only for non-complete orders)
                if (!IsOrderComplete(pinnedOrders[i]))
                {
                    orderTimers[i] -= Time.deltaTime;
                    orderTimers[i] = Mathf.Max(0f, orderTimers[i]);
                }
            }
            else
            {
                orderTimers[i] = 0f;
            }
        }
    }

    /// <summary>
    /// Check if all items in an order are complete
    /// </summary>
    bool IsOrderComplete(CustomerOrder order)
    {
        foreach (var item in order.items)
        {
            if (!item.IsComplete) return false;
        }
        return true;
    }

    /// <summary>
    /// Force immediate refresh (call from button if needed)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Reset timers (call when new orders are generated)
    /// </summary>
    public void ResetTimers()
    {
        for (int i = 0; i < 3; i++)
        {
            orderTimers[i] = 0f;
        }
        RefreshDisplay();
    }

    void OnEnable()
    {
        ResetTimers();
    }
}

