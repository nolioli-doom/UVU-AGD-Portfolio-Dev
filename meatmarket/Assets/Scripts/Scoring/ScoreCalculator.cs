using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Calculates order scores using the original formula: i * (x + (t * s))
/// where:
/// i = tip multiplier
/// x = sum of limb values (base 10, Perfect = 10 * precisionBias)
/// t = time remaining
/// s = customer speed bias
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// Calculate score for a completed order using the original formula.
    /// </summary>
    /// <param name="order">The completed order</param>
    /// <param name="allocatedPieces">List of TrayPieces that were allocated to this order</param>
    /// <returns>Final score for the order</returns>
    public static int CalculateOrderScore(CustomerOrder order, List<TrayPiece> allocatedPieces)
    {
        if (order == null || order.archetype == null)
        {
            Debug.LogWarning("[ScoreCalculator] Cannot calculate score: order or archetype is null");
            return 0;
        }
        
        // i = tip multiplier
        float i = order.tipMultiplier;
        
        // x = sum of limb values
        // Base value: 10 per limb
        // Perfect limb: 10 * precisionBias
        float x = 0f;
        foreach (var piece in allocatedPieces)
        {
            float limbValue = 10f; // Base value
            
            if (piece.isPerfect)
            {
                // Perfect limb: 10 * precisionBias
                limbValue = 10f * order.archetype.precisionBias;
            }
            
            x += limbValue;
        }
        
        // t = time remaining (in seconds)
        float t = order.GetRemainingTime();
        
        // s = customer speed bias
        float s = order.archetype.speedBias;
        
        // Final score: i * (x + (t * s))
        float finalScore = i * (x + (t * s));
        
        return Mathf.RoundToInt(finalScore);
    }
}

