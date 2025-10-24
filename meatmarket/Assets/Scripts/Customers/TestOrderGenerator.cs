using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple test order generator for the Customer Scene.
/// Creates test orders that can be pinned and taken to the Butchery Scene.
/// </summary>
public class TestOrderGenerator : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("OrderManagerSO to populate with test orders")]
    public OrderManagerSO orderManager;
    
    [Header("Test Configuration")]
    [Tooltip("Number of test orders to generate")]
    [Range(1, 10)]
    public int numberOfTestOrders = 5;
    
    [Header("Debug")]
    public bool logGeneration = true;
    
    /// <summary>
    /// Generate test orders and populate the OrderManagerSO
    /// Call this from a button in Customer Scene
    /// </summary>
    public void GenerateTestOrders()
    {
        if (orderManager == null)
        {
            Debug.LogError("[TestOrderGenerator] OrderManagerSO is not assigned!");
            return;
        }
        
        var testOrders = new List<CustomerOrder>();
        
        // Generate test orders
        for (int i = 0; i < numberOfTestOrders; i++)
        {
            var order = CreateTestOrder(i + 1);
            testOrders.Add(order);
        }
        
        // Set orders in the manager
        orderManager.SetOrders(testOrders);
        
        if (logGeneration) Debug.Log($"[TestOrderGenerator] Generated {testOrders.Count} test orders");
    }
    
    /// <summary>
    /// Create a single test order
    /// </summary>
    private CustomerOrder CreateTestOrder(int orderNumber)
    {
        var order = new CustomerOrder
        {
            customerName = $"Test Customer {orderNumber}",
            items = new List<OrderItem>()
        };
        
        // Add 1-3 random items to each order
        int itemCount = Random.Range(1, 4);
        for (int i = 0; i < itemCount; i++)
        {
            var item = new OrderItem
            {
                species = GetRandomSpecies(),
                partType = GetRandomPartType(),
                quantity = Random.Range(1, 3) // 1 or 2 of each part
            };
            order.items.Add(item);
        }
        
        return order;
    }
    
    /// <summary>
    /// Get a random species
    /// </summary>
    private SpeciesType GetRandomSpecies()
    {
        var species = new[] { SpeciesType.Dog, SpeciesType.Cat, SpeciesType.Bunny };
        return species[Random.Range(0, species.Length)];
    }
    
    /// <summary>
    /// Get a random part type
    /// </summary>
    private OrderPartType GetRandomPartType()
    {
        var parts = new[] { 
            OrderPartType.Hand, 
            OrderPartType.Foot, 
            OrderPartType.Head, 
            OrderPartType.Upper_Arm, 
            OrderPartType.Forearm,
            OrderPartType.Upper_Leg,
            OrderPartType.Lower_Leg,
            OrderPartType.Upper_Torso,
            OrderPartType.Lower_Torso
        };
        return parts[Random.Range(0, parts.Length)];
    }
    
    /// <summary>
    /// Clear all orders (useful for testing)
    /// </summary>
    public void ClearAllOrders()
    {
        if (orderManager != null)
        {
            orderManager.ClearAllOrders();
            if (logGeneration) Debug.Log("[TestOrderGenerator] Cleared all orders");
        }
    }
}
