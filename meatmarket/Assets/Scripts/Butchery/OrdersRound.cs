using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OrdersRound
{
    public int dayIndex;
    public List<CustomerOrder> orders = new();
    // Derived analytics (optional)
    public Dictionary<SpeciesType, int> impliedBodiesNeeded = new();
}