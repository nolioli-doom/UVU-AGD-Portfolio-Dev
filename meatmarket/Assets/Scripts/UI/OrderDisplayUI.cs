using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays pinned orders in a UI list.
/// Attach to order display panel in Butchery Scene (or Customer Scene).
/// </summary>
public class OrderDisplayUI : MonoBehaviour
{
    [Header("Configuration")]
    public OrderManagerSO orderManager;

    [Header("UI References")]
    public Transform orderListParent;     // Content transform of ScrollView
    public GameObject orderCardPrefab;     // OrderCardUI prefab

    [Header("Refresh Settings")]
    [Tooltip("How often to refresh the display (0 = manual only)")]
    public float refreshInterval = 1f;

    [Header("Debug")]
    public bool logRefresh = false;

    private float refreshTimer = 0f;
    private List<GameObject> spawnedCards = new List<GameObject>();

    void Update()
    {
        if (refreshInterval > 0)
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Refresh the order display (call manually or auto via interval)
    /// </summary>
    public void RefreshDisplay()
    {
        if (orderManager == null)
        {
            Debug.LogWarning("[OrderDisplayUI] OrderManager not assigned");
            return;
        }

        if (orderListParent == null || orderCardPrefab == null)
        {
            Debug.LogWarning("[OrderDisplayUI] UI references not assigned");
            return;
        }

        // Clear existing cards
        ClearDisplay();

        // Get pinned orders
        var pinnedOrders = orderManager.GetPinnedOrders();

        if (logRefresh)
        {
            Debug.Log($"[OrderDisplayUI] Refreshing display with {pinnedOrders.Count} pinned orders");
        }

        // Create card for each pinned order
        foreach (var order in pinnedOrders)
        {
            var cardGO = Instantiate(orderCardPrefab, orderListParent);
            var card = cardGO.GetComponent<OrderCardUI>();
            
            if (card != null)
            {
                card.Bind(order);
            }
            else
            {
                Debug.LogWarning("[OrderDisplayUI] OrderCard prefab missing OrderCardUI component");
            }

            spawnedCards.Add(cardGO);
        }
    }

    /// <summary>
    /// Clear all spawned order cards
    /// </summary>
    void ClearDisplay()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        spawnedCards.Clear();
    }

    /// <summary>
    /// Manual refresh method (wire to button if needed)
    /// </summary>
    public void ManualRefresh()
    {
        RefreshDisplay();
    }

    void OnEnable()
    {
        // Refresh when panel becomes active
        RefreshDisplay();
    }

    void OnDisable()
    {
        // Clear when panel is hidden
        ClearDisplay();
    }
}

