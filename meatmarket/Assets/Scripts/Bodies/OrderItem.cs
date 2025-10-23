using System;
using UnityEngine;

[Serializable]
public class OrderItem
{
    public SpeciesType species;
    public OrderPartType partType;  // Generic type (Hand, not L_Hand/R_Hand)
    [Min(1)] public int quantity = 1;
    public QualityTier minQuality = QualityTier.Normal;

    [Header("Progress (Runtime)")]
    public int currentQuantity = 0;  // How many have been delivered

    public bool IsComplete => currentQuantity >= quantity;
    public float Progress => Mathf.Clamp01((float)currentQuantity / quantity);

    public void IncrementProgress(int amount = 1)
    {
        currentQuantity += amount;
    }

    public void ResetProgress()
    {
        currentQuantity = 0;
    }
}