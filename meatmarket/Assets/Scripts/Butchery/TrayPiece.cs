using System;
using UnityEngine;

/// <summary>
/// Data-only representation of a severed limb/piece that can be held by the deposit tray
/// and allocated to pinned orders. Created at the moment a limb becomes fully severed.
/// </summary>
[Serializable]
public class TrayPiece
{
    [Header("Identity")]
    public SpeciesType species;
    public OrderPartType partType;

    [Tooltip("Canonical identifier for the limb, e.g., Dog.RightArm.Shoulder")] 
    public string limbId;

    [Header("Quality")] 
    [Tooltip("True if any required cut on this limb hit a Perfect zone")] 
    public bool isPerfect;

    [Header("Provenance (Optional)")]
    [Tooltip("World position where the final sever occurred (optional)")] 
    public Vector3 severWorldPosition;

    [Tooltip("Unix time (seconds) when sever occurred (optional)")] 
    public double severUnixTimeSeconds;

    public override string ToString()
    {
        var quality = isPerfect ? "Perfect" : "Miss";
        return $"TrayPiece[{species} {partType} | {quality} | limbId={limbId}]";
    }
}


