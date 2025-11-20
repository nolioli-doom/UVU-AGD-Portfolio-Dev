using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Represents a virtual body segment in the hierarchy.
/// Tracks parent/child cut states to determine when segment is fully isolated.
/// </summary>
public class VirtualSegment
{
    public BodySegmentType segmentType;
    public SpeciesType species;
    public bool isRoot;  // True for Upper_Torso
    public bool hasBeenDeposited;  // Prevent double-depositing

    // The cut zone that separates this segment from its parent
    public CutZone parentCut;

    // Cut zones that separate children from this segment
    public List<CutZone> childCuts = new List<CutZone>();

    // Cached cut data (from most recent parent cut)
    public CutPrecision lastPrecision = CutPrecision.Perfect;
    public ToolManager.ToolType lastToolUsed = ToolManager.ToolType.Cleaver;
    public CutSection lastCutSection = CutSection.Neck;

    /// <summary>
    /// Check if parent cut has been made
    /// </summary>
    public bool IsParentCutMade => isRoot || (parentCut != null && parentCut.HasBeenCut);

    /// <summary>
    /// Check if all child cuts have been made
    /// </summary>
    public bool AreAllChildCutsMade => childCuts.Count == 0 || childCuts.All(c => c.HasBeenCut);

    /// <summary>
    /// Check if this segment is fully isolated (ready for deposit)
    /// </summary>
    public bool IsFullyIsolated()
    {
        // Already deposited? Don't deposit again
        if (hasBeenDeposited) return false;
        
        // Root segment (Upper_Torso) can be isolated when all child cuts are made
        // Non-root segments need both parent AND child cuts made
        if (isRoot)
        {
            return AreAllChildCutsMade;
        }
        return IsParentCutMade && AreAllChildCutsMade;
    }

    /// <summary>
    /// Generate severed part data for this segment
    /// </summary>
    public SeveredPartData GeneratePartData()
    {
        // Determine quality from precision
        QualityTier quality = lastPrecision switch
        {
            CutPrecision.Perfect => QualityTier.Perfect,
            CutPrecision.MissNorth => QualityTier.Normal,
            CutPrecision.MissSouth => QualityTier.Normal,
            _ => QualityTier.Normal
        };

        return new SeveredPartData(
            species,
            segmentType,
            quality,
            lastPrecision,
            lastToolUsed,
            lastCutSection
        );
    }

    public override string ToString()
    {
        return $"{segmentType} (Parent: {(IsParentCutMade ? "CUT" : "INTACT")}, Children: {childCuts.Count(c => c.HasBeenCut)}/{childCuts.Count} cut)";
    }
}

