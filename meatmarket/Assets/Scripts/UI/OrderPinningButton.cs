using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Component for buttons that pin/unpin specific orders.
/// Attach to each order pin button in Customer Scene.
/// </summary>
public class OrderPinningButton : MonoBehaviour
{
    [Header("Configuration")]
    public OrderManagerSO orderManager;
    [Tooltip("Which order index this button controls (0-based)")]
    public int orderIndex;

    [Header("Display (Optional)")]
    public Text buttonLabel;
    public Image buttonBackground;
    public Color pinnedColor = Color.green;
    public Color unpinnedColor = Color.gray;
    public Color disabledColor = Color.red;

    [Header("Events")]
    public UnityEvent OnPinSuccess;
    public UnityEvent OnUnpin;
    public UnityEvent OnPinFailed;  // Max reached or invalid

    void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Toggle pin state (wire to button OnClick)
    /// </summary>
    public void TogglePin()
    {
        if (orderManager == null)
        {
            Debug.LogError("[OrderPinningButton] OrderManager not assigned!");
            return;
        }

        // Check if this order exists
        if (orderIndex < 0 || orderIndex >= orderManager.allOrders.Count)
        {
            Debug.LogWarning($"[OrderPinningButton] Invalid order index: {orderIndex}");
            OnPinFailed?.Invoke();
            return;
        }

        // Check if currently pinned
        if (orderManager.IsOrderPinned(orderIndex))
        {
            // Unpin
            bool success = orderManager.UnpinOrderAtIndex(orderIndex);
            if (success)
            {
                OnUnpin?.Invoke();
                UpdateVisuals();
            }
        }
        else
        {
            // Pin
            bool success = orderManager.PinOrderAtIndex(orderIndex);
            if (success)
            {
                OnPinSuccess?.Invoke();
                UpdateVisuals();
            }
            else
            {
                OnPinFailed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Update button visuals based on pin state
    /// </summary>
    public void UpdateVisuals()
    {
        if (orderManager == null) return;

        bool exists = orderIndex >= 0 && orderIndex < orderManager.allOrders.Count;
        bool isPinned = exists && orderManager.IsOrderPinned(orderIndex);

        // Update label
        if (buttonLabel != null)
        {
            if (exists)
            {
                var order = orderManager.GetOrder(orderIndex);
                string status = isPinned ? "[PINNED]" : "";
                buttonLabel.text = $"Order {orderIndex + 1}: {order.customerName} {status}";
            }
            else
            {
                buttonLabel.text = $"Order {orderIndex + 1}: (None)";
            }
        }

        // Update background color
        if (buttonBackground != null)
        {
            if (!exists)
            {
                buttonBackground.color = disabledColor;
            }
            else if (isPinned)
            {
                buttonBackground.color = pinnedColor;
            }
            else
            {
                buttonBackground.color = unpinnedColor;
            }
        }
    }
}

