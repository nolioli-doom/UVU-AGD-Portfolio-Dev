using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TextMeshProUGUI slot1CustomerName;
    public TextMeshProUGUI slot1Timer;
    public TextMeshProUGUI slot1Items;

    [Header("Order Slot 2")]
    public GameObject slot2Root;
    public TextMeshProUGUI slot2CustomerName;
    public TextMeshProUGUI slot2Timer;
    public TextMeshProUGUI slot2Items;

    [Header("Order Slot 3")]
    public GameObject slot3Root;
    public TextMeshProUGUI slot3CustomerName;
    public TextMeshProUGUI slot3Timer;
    public TextMeshProUGUI slot3Items;

    [Header("Refresh")]
    [Tooltip("How often to refresh display (0 = every frame)")]
    public float refreshInterval = 0.5f;

    [Header("Styling")]
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.gray;
    
    [Header("Text Colors")]
    public Color customerNameColor = Color.white;
    public Color timerColor = Color.white;
    public Color itemsColor = Color.white;
    
    [Header("Species Colors")]
    public Color dogColor = Color.red;
    public Color catColor = Color.green;
    public Color bunnyColor = Color.blue;
    
    [Header("Timer Colors (Urgency-Based)")]
    public Color timerColorNormal = Color.white;
    public Color timerColorWarning = Color.yellow;
    public Color timerColorUrgent = Color.red;
    
    [Tooltip("Time threshold for warning color (seconds)")]
    public float timerWarningThreshold = 60f;
    
    [Tooltip("Time threshold for urgent color (seconds)")]
    public float timerUrgentThreshold = 30f;

    [Header("Debug")]
    public bool logUpdates = false;

    private float refreshTimer = 0f;

    void Update()
    {
        if (orderManager == null) return;

        refreshTimer += Time.deltaTime;
        if (refreshTimer >= refreshInterval)
        {
            refreshTimer = 0f;
            RefreshDisplay();
        }
    }

    void OnEnable()
    {
        // Subscribe to order completion/expiration events for immediate refresh
        if (orderManager != null)
        {
            orderManager.OnOrderCompleted.AddListener(OnOrderCompleted);
            orderManager.OnOrderExpired.AddListener(OnOrderExpired);
        }
        RefreshDisplay();
    }

    void OnDisable()
    {
        // Unsubscribe from events
        if (orderManager != null)
        {
            orderManager.OnOrderCompleted.RemoveListener(OnOrderCompleted);
            orderManager.OnOrderExpired.RemoveListener(OnOrderExpired);
        }
    }

    /// <summary>
    /// Called when an order is completed - refresh immediately
    /// </summary>
    void OnOrderCompleted(CustomerOrder order)
    {
        if (logUpdates) Debug.Log($"[PinnedOrdersDisplay] Order completed, refreshing display");
        RefreshDisplay();
    }

    /// <summary>
    /// Called when an order expires - refresh immediately
    /// </summary>
    void OnOrderExpired(CustomerOrder order)
    {
        if (logUpdates) Debug.Log($"[PinnedOrdersDisplay] Order expired, refreshing display");
        RefreshDisplay();
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
        // Get pinned orders directly from slots
        var slot0Order = orderManager.pinnedSlot0;
        var slot1Order = orderManager.pinnedSlot1;
        var slot2Order = orderManager.pinnedSlot2;
 
        // Update slot 0
        UpdateSlot(0, slot0Order, slot1Root, slot1CustomerName, slot1Timer, slot1Items);
        
        // Update slot 1
        UpdateSlot(1, slot1Order, slot2Root, slot2CustomerName, slot2Timer, slot2Items);
        
        // Update slot 2
        UpdateSlot(2, slot2Order, slot3Root, slot3CustomerName, slot3Timer, slot3Items);
    }

    /// <summary>
    /// Update a single order slot
    /// </summary>
    void UpdateSlot(int slotIndex, CustomerOrder order, 
                    GameObject slotRoot, TextMeshProUGUI nameText, TextMeshProUGUI timerText, TextMeshProUGUI itemsText)
    {
        if (order != null)
        {
            // Order exists for this slot
            if (slotRoot != null) slotRoot.SetActive(true);

            // Update customer name
            if (nameText != null)
            {
                string archetypeName = (order.archetype != null && !string.IsNullOrEmpty(order.archetype.displayName))
                    ? order.archetype.displayName
                    : "—";
                nameText.text = $"{slotIndex + 1}. {archetypeName}";
                nameText.color = customerNameColor;
            }

            // Update timer
            if (timerText != null)
            {
                float timeRemaining = order.GetRemainingTime();
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timerText.text = $"Time: {minutes:00}:{seconds:00}";
                
                // Color code based on urgency
                if (timeRemaining < timerUrgentThreshold)
                    timerText.color = timerColorUrgent;
                else if (timeRemaining < timerWarningThreshold)
                    timerText.color = timerColorWarning;
                else
                    timerText.color = timerColorNormal;
            }

            // Update items with progress
            if (itemsText != null)
            {
                var sb = new StringBuilder();

                foreach (var item in order.items)
                {
                    // Skip fully completed items so their line disappears once satisfied
                    if (item.IsComplete)
                        continue;

                    // We no longer show quantity progress (0/1 etc.),
                    // just a simple bullet with the colored part name.
                    string checkmark = "•";
                    string partTypeDisplay = item.partType.ToString().Replace("_", " ");
                    
                    // Get species color
                    Color speciesColor = GetSpeciesColor(item.species);
                    string colorHex = ColorUtility.ToHtmlStringRGB(speciesColor);
                    
                    // Format: "• Hand" with Hand colored by species (red/green/blue)
                    string itemText = $"{checkmark} <color=#{colorHex}>{partTypeDisplay}</color>";
                    
                    sb.AppendLine(itemText);
                }
                
                itemsText.text = sb.ToString().TrimEnd();
                itemsText.color = itemsColor;
            }
        }
        else
        {
            // No order for this slot - completely hide
            if (slotRoot != null) slotRoot.SetActive(false);
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
    /// Force immediate refresh (call from button if needed)
    /// </summary>
    public void ResetTimers()
    {
        RefreshDisplay();
    }

    /// <summary>
    /// Get color for a species
    /// </summary>
    Color GetSpeciesColor(SpeciesType species)
    {
        switch (species)
        {
            case SpeciesType.Dog:
                return dogColor;
            case SpeciesType.Cat:
                return catColor;
            case SpeciesType.Bunny:
                return bunnyColor;
            default:
                return itemsColor;
        }
    }
}

