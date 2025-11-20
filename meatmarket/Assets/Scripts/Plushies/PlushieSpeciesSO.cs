using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Butchery/Plushie Species")]
public class PlushieSpeciesSO : ScriptableObject
{
    [Header("Identity")]
    public SpeciesType species;
    [Range(0f, 1f)] public float rarityWeight = 0.5f;    // higher = shows up more often
    [Min(0)] public int dailySupplyCap = 99;             // soft cap per day

    [Header("Yields per Body (Generic Counts)")]
    [Tooltip("Use generic types: Hand: 2 (not L_Hand: 1, R_Hand: 1)")]
    public List<PartYield> yields = new();

    [Serializable]
    public struct PartYield
    {
        [Tooltip("Part type (e.g., Upper_Arm becomes 'Upper Arm' in display)")]
        public OrderPartType partType;
        [Tooltip("Total count per body (e.g., Hand: 2 means 1 left + 1 right)")]
        [Min(0)] public int count;
        
        /// <summary>
        /// Get display name with underscores replaced by spaces
        /// </summary>
        public string GetDisplayName()
        {
            return partType.ToString().Replace("_", " ");
        }
    }

    // Helper lookup at runtime
    private Dictionary<OrderPartType, int> _yieldMap;
    public int GetYield(OrderPartType partType)
    {
        if (_yieldMap == null)
        {
            _yieldMap = new Dictionary<OrderPartType, int>();
            foreach (var y in yields)
                _yieldMap[y.partType] = y.count;
        }
        return _yieldMap.TryGetValue(partType, out var c) ? c : 0;
    }
}