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

    [Header("Yields per Body")]
    public List<PartYield> yields = new();

    [Serializable]
    public struct PartYield
    {
        public BodyPartType part;
        [Min(0)] public int count;
    }

    // Helper lookup at runtime
    private Dictionary<BodyPartType, int> _yieldMap;
    public int GetYield(BodyPartType part)
    {
        if (_yieldMap == null)
        {
            _yieldMap = new Dictionary<BodyPartType, int>();
            foreach (var y in yields)
                _yieldMap[y.part] = y.count;
        }
        return _yieldMap.TryGetValue(part, out var c) ? c : 0;
    }
}