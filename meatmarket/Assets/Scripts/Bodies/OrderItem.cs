using System;
using UnityEngine;

[Serializable]
public class OrderItem
{
    public SpeciesType species;
    public BodyPartType part;
    [Min(1)] public int quantity = 1;
    public QualityTier minQuality = QualityTier.Normal;
}