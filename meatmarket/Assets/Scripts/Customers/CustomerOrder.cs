using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CustomerOrder
{
    public CustomerArchetypeSO archetype;
    public List<OrderItem> items = new();
    public float timeLimitSeconds = 60f;      // affected by archetype
    public float tipMultiplier = 1f;          // from archetype
    public float wasteSensitivity = 0.5f;     // from archetype
    public string customerName;               // from archetype/display
}