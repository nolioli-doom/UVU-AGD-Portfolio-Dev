using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for displaying customer order details in the swipe menu.
/// Handles order display, pinning, and navigation between customers.
/// </summary>
public class CustomerDetailUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component showing customer name")]
    public TextMeshProUGUI customerNameText;
    
    [Tooltip("Text component showing order items")]
    public TextMeshProUGUI orderItemsText;
    
    [Tooltip("Text component showing order timer")]
    public TextMeshProUGUI timerText;
    
    [Tooltip("Button to pin/unpin the current order")]
    public Button pinButton;
    
    [Tooltip("Text component for the pin button")]
    public TextMeshProUGUI pinButtonText;
    
    [Tooltip("Button to go to next customer")]
    public Button nextButton;
    
    [Tooltip("Button to go to previous customer")]
    public Button previousButton;
    
    [Header("System References")]
    [Tooltip("OrderManagerSO asset for order management")]
    public OrderManagerSO orderManager;
    
    [Header("UI Text")]
    [Tooltip("Text to show when no customer is selected")]
    public string noCustomerText = "No Customer Selected";
    
    [Tooltip("Text for pin button when order is not pinned")]
    public string pinButtonTextUnpinned = "Pin Order";
    
    [Tooltip("Text for pin button when order is pinned")]
    public string pinButtonTextPinned = "Unpin Order";
    
    [Tooltip("Text to show when order is completed")]
    public string orderCompletedText = "Order Completed!";
    
    [Header("Timer Settings")]
    [Tooltip("Format string for timer display (e.g., 'Time: {0}s')")]
    public string timerFormat = "Time: {0}s";
    
    [Tooltip("Color for normal timer")]
    public Color normalTimerColor = Color.white;
    
    [Tooltip("Color for urgent timer (when time is running low)")]
    public Color urgentTimerColor = Color.red;
    
    [Tooltip("Time threshold for urgent timer color")]
    public float urgentTimeThreshold = 10f;
    
    [Header("Debug")]
    public bool logUIUpdates = false;
    
    private CustomerQueueManager queueManager;
    private CustomerVisual currentCustomer;
    private bool isOrderPinned = false;
    
    void Start()
    {
        // Find required components
        queueManager = FindObjectOfType<CustomerQueueManager>();
        
        if (queueManager == null)
        {
            Debug.LogError("[CustomerDetailUI] CustomerQueueManager not found in scene");
            return;
        }
        
        if (orderManager == null)
        {
            Debug.LogError("[CustomerDetailUI] OrderManagerSO not assigned in inspector");
            return;
        }
        
        // Set up button listeners
        SetupButtonListeners();
        
        // Initial UI update
        UpdateUI();
        
        if (logUIUpdates) Debug.Log("[CustomerDetailUI] Initialized");
    }
    
    void Update()
    {
        // Update timer display
        UpdateTimerDisplay();
        
        // Check if current customer has changed
        CustomerVisual newCustomer = queueManager.GetCurrentCustomer();
        if (newCustomer != currentCustomer)
        {
            if (logUIUpdates) Debug.Log($"[CustomerDetailUI] Customer changed from {(currentCustomer != null ? currentCustomer.name : "null")} to {(newCustomer != null ? newCustomer.name : "null")} (Order ID: {(newCustomer != null && newCustomer.GetOrder() != null ? newCustomer.GetOrder().GetHashCode() : "null")})");
            currentCustomer = newCustomer;
            UpdateUI();
        }
    }
    
    private void SetupButtonListeners()
    {
        if (pinButton != null)
        {
            pinButton.onClick.AddListener(OnPinButtonClicked);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(OnPreviousButtonClicked);
        }
    }
    
    private void OnPinButtonClicked()
    {
        if (currentCustomer == null || orderManager == null) return;
        
        CustomerOrder order = currentCustomer.GetOrder();
        if (order == null) return;
        
        // Check if already pinned
        bool alreadyPinned = orderManager.IsOrderPinned(order);
        
        if (alreadyPinned)
        {
            // Order is already pinned - disable button
            if (logUIUpdates) Debug.Log("[CustomerDetailUI] Order is already pinned");
            UpdatePinButtonState(order);
            return;
        }
        
        // Check if slots are available
        int availableSlots = orderManager.GetAvailablePinSlots();
        if (availableSlots <= 0)
        {
            if (logUIUpdates) Debug.Log("[CustomerDetailUI] Cannot pin: all slots full (max 3)");
            return;
        }
        
        // Pin the order (returns slot index or -1)
        int slotIndex = orderManager.PinOrder(order);
        if (slotIndex >= 0)
        {
            // Successfully pinned
            isOrderPinned = true;
            currentCustomer.SetPinned(true);
            
            // Remove customer from queue via queue manager
            queueManager?.RemoveCustomerFromQueue(currentCustomer);
            
            UpdatePinButtonText();
            
            // Immediately refresh the pinned orders display (if it exists)
            PinnedOrdersDisplay pinnedDisplay = FindObjectOfType<PinnedOrdersDisplay>();
            if (pinnedDisplay != null)
            {
                pinnedDisplay.RefreshDisplay();
                if (logUIUpdates) Debug.Log("[CustomerDetailUI] Refreshed PinnedOrdersDisplay");
            }
            
            if (logUIUpdates) Debug.Log($"[CustomerDetailUI] Pinned order to slot {slotIndex + 1}");
        }
        else
        {
            if (logUIUpdates) Debug.Log("[CustomerDetailUI] Failed to pin order");
        }
    }
    
    private void OnNextButtonClicked()
    {
        queueManager.NextCustomer();
        UpdateUI();
        if (logUIUpdates) Debug.Log("[CustomerDetailUI] Moved to next customer");
    }
    
    private void OnPreviousButtonClicked()
    {
        queueManager.PreviousCustomer();
        UpdateUI();
        if (logUIUpdates) Debug.Log("[CustomerDetailUI] Moved to previous customer");
    }
    
    private void UpdateUI()
    {
        if (currentCustomer == null)
        {
            ShowNoCustomerState();
            return;
        }
        
        CustomerOrder order = currentCustomer.GetOrder();
        if (order == null)
        {
            ShowNoCustomerState();
            return;
        }
        
        if (logUIUpdates) Debug.Log($"[CustomerDetailUI] Updating UI for customer: {currentCustomer.name}, order: {order.customerName}, pieces: {order.items?.Count ?? 0} (Order ID: {order.GetHashCode()})");
        
        // Update customer name
        if (customerNameText != null)
        {
            customerNameText.text = order.customerName;
        }
        
        // Update order items
        UpdateOrderItemsDisplay(order);
        
        // Update pin button state
        UpdatePinButtonState(order);
        
        // Update navigation buttons
        UpdateNavigationButtons();
    }
    
    private void ShowNoCustomerState()
    {
        if (customerNameText != null)
        {
            customerNameText.text = noCustomerText;
        }
        
        if (orderItemsText != null)
        {
            orderItemsText.text = "";
        }
        
        if (timerText != null)
        {
            timerText.text = "";
        }
        
        if (pinButton != null)
        {
            pinButton.interactable = false;
        }
        
        if (nextButton != null)
        {
            nextButton.interactable = false;
        }
        
        if (previousButton != null)
        {
            previousButton.interactable = false;
        }
        
    }
    
    private void UpdateOrderItemsDisplay(CustomerOrder order)
    {
        if (orderItemsText == null) return;
        
        if (order.items == null || order.items.Count == 0)
        {
            orderItemsText.text = "No items";
            return;
        }
        
        // Simple display - just show total item count
        orderItemsText.text = $"{order.items.Count} pieces";
    }
    
    private void UpdatePinButtonState(CustomerOrder order)
    {
        if (pinButton == null || orderManager == null) return;
        
        // Check if this order is pinned
        isOrderPinned = orderManager.IsOrderPinned(order);
        
        // Check if slots are available
        int availableSlots = orderManager.GetAvailablePinSlots();
        
        // Disable button if already pinned OR if no slots available
        pinButton.interactable = !isOrderPinned && availableSlots > 0;
        
        UpdatePinButtonText();
    }
    
    private void UpdatePinButtonText()
    {
        if (pinButtonText == null) return;
        
        // Show different text based on state
        if (isOrderPinned)
        {
            pinButtonText.text = "Already Pinned";
        }
        else
        {
            int availableSlots = orderManager != null ? orderManager.GetAvailablePinSlots() : 0;
            if (availableSlots <= 0)
            {
                pinButtonText.text = "Slots Full";
            }
            else
            {
                pinButtonText.text = pinButtonTextUnpinned;
            }
        }
    }
    
    private void UpdateNavigationButtons()
    {
        bool hasCustomers = !queueManager.IsQueueEmpty();
        bool hasMultipleCustomers = queueManager.GetAllQueuedCustomers().Count > 1;
        
        if (nextButton != null)
        {
            nextButton.interactable = hasMultipleCustomers;
        }
        
        if (previousButton != null)
        {
            previousButton.interactable = hasMultipleCustomers;
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (timerText == null || currentCustomer == null) return;
        
        CustomerOrder order = currentCustomer.GetOrder();
        if (order == null)
        {
            timerText.text = "";
            return;
        }
        
        // Calculate remaining time (this would need to be implemented in OrderManagerSO)
        float remainingTime = GetOrderRemainingTime(order);
        
        if (remainingTime <= 0)
        {
            timerText.text = orderCompletedText;
            timerText.color = Color.green;
        }
        else
        {
            int timeInt = Mathf.CeilToInt(remainingTime);
            timerText.text = "Time: " + timeInt.ToString() + "s";
            timerText.color = remainingTime <= urgentTimeThreshold ? urgentTimerColor : normalTimerColor;
        }
    }
    
    private float GetOrderRemainingTime(CustomerOrder order)
    {
        if (order == null) return 0f;
        
        // Return the order's time limit (this is a placeholder until we implement actual timer tracking)
        return order.timeLimitSeconds;
    }
    
    /// <summary>
    /// Force refresh the UI display
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// Show/hide the entire UI panel
    /// </summary>
    public void SetUIVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
