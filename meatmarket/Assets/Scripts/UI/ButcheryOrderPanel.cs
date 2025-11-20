using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Displays a single pinned order in the butchery scene with species-colored text.
/// Attach to each pinned order panel (3 total).
/// </summary>
public class ButcheryOrderPanel : MonoBehaviour
{
    [Header("Data Sources")]
    public OrderManagerSO orderManager;
    public DepositTrayController depositTray;
    
    [Header("UI Components")]
    public SpeciesTextColorer speciesColorer;
    public TextMeshProUGUI timerText; // Optional: separate timer display
    
    [Header("Configuration")]
    public int slotIndex = 0; // Which pinned order slot (0, 1, 2)
    
    [Header("Debug")]
    public bool logUpdates = false;
    
    private CustomerOrder currentOrder;
    
    void OnEnable()
    {
        // No events available, we'll update manually in Update()
    }
    
    void OnDisable()
    {
        // No events to unsubscribe from
    }
    
    void Update()
    {
        // Only update when orders change, not every frame
        if (orderManager == null) return;
        
        var pinnedOrders = orderManager.GetPinnedOrders();
        bool hasOrderForThisSlot = slotIndex < pinnedOrders.Count;
        
        // Only refresh if state has changed
        if (hasOrderForThisSlot && currentOrder == null)
        {
            // We now have an order for this slot
            RefreshDisplay();
        }
        else if (!hasOrderForThisSlot && currentOrder != null)
        {
            // We no longer have an order for this slot
            ShowEmpty();
        }
    }
    
    public void RefreshDisplay()
    {
        if (orderManager == null) return;
        
        List<CustomerOrder> pinned = orderManager.GetPinnedOrders();
        
        // Check if slot has an order
        if (slotIndex >= pinned.Count)
        {
            ShowEmpty();
            return;
        }
        
        currentOrder = pinned[slotIndex];
        
        // Update parts display with species colors
        UpdatePartsDisplay();
        UpdateTimer();
        
        if (logUpdates) Debug.Log($"[ButcheryOrderPanel] Refreshed slot {slotIndex}");
    }
    
    void UpdatePartsDisplay()
    {
        if (currentOrder == null || speciesColorer == null) return;
        
        // Create parts list with species
        var parts = currentOrder.items.Select(item => 
            ($"{item.quantity}× {item.partType}", item.species)).ToList();
        
        // Update text with mixed species colors
        speciesColorer.UpdateTextWithMultipleSpecies(parts);
        
        if (logUpdates) Debug.Log($"[ButcheryOrderPanel] Updated parts display for {currentOrder.customerName}");
    }
    
    void UpdateTimer()
    {
        if (currentOrder == null || timerText == null) return;
        
        // Since OrderManagerSO doesn't have timer tracking, we'll show a placeholder
        // You'll need to implement timer tracking separately if needed
        timerText.text = "Timer: --:--";
        timerText.color = Color.gray;
    }
    
    void ShowEmpty()
    {
        if (speciesColorer != null)
        {
            speciesColorer.UpdateTextWithSpecies("No order pinned", SpeciesType.Cat); // Use any species for default
        }
        
        if (timerText != null)
        {
            timerText.text = "—";
            timerText.color = Color.gray;
        }
        
        currentOrder = null;
    }
    
    
    private System.Collections.IEnumerator TestColorSequence()
    {
        yield return new WaitForSeconds(1f);
        speciesColorer.UpdateTextWithSpecies("TEST CAT", SpeciesType.Cat);
        Debug.Log("Set to Cat color - should be orange");
        
        yield return new WaitForSeconds(1f);
        speciesColorer.UpdateTextWithSpecies("TEST BUNNY", SpeciesType.Bunny);
        Debug.Log("Set to Bunny color - should be gray");
        
        yield return new WaitForSeconds(1f);
        speciesColorer.UpdateTextWithSpecies("No order pinned", SpeciesType.Cat);
        Debug.Log("Reset to default");
    }
}
