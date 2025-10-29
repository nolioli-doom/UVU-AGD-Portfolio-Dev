using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the virtual body segment hierarchy for a plushie.
/// Scans CutZones at startup, tracks cuts, and detects when segments become fully isolated.
/// Attach to plushie root GameObject.
/// </summary>
public class BodyPartTree : MonoBehaviour
{
    [Header("Configuration")]
    public SpeciesType species;

    [Header("Connections (Auto-Found if not assigned)")]
    [Tooltip("Leave empty to auto-find in scene at runtime")]
    public DepositTrayController depositTray;

    [Header("Debug")]
    public bool logSegmentStates = true;

    // Internal virtual hierarchy
    private Dictionary<BodySegmentType, VirtualSegment> segments = new Dictionary<BodySegmentType, VirtualSegment>();
    private List<CutZone> allCutZones = new List<CutZone>();

    void Awake()
    {
        // Auto-find deposit tray if not assigned (for runtime-spawned prefabs)
        if (depositTray == null)
        {
            depositTray = FindObjectOfType<DepositTrayController>();
            if (depositTray == null)
            {
                Debug.LogError("[BodyPartTree] No DepositTrayController found in scene! Parts cannot be deposited.", this);
            }
            else if (logSegmentStates)
            {
                Debug.Log("[BodyPartTree] Auto-found DepositTrayController in scene");
            }
        }

        BuildVirtualHierarchy();
    }

    /// <summary>
    /// Scan all CutZones and build the virtual segment hierarchy
    /// </summary>
    void BuildVirtualHierarchy()
    {
        // Find all cut zones
        allCutZones = GetComponentsInChildren<CutZone>(includeInactive: false).ToList();
        
        if (allCutZones.Count == 0)
        {
            Debug.LogWarning("[BodyPartTree] No CutZones found on plushie!", this);
            return;
        }

        // Debug.Log($"[BodyPartTree] Found {allCutZones.Count} CutZones, building hierarchy...");

        // Create all virtual segments
        CreateAllSegments();

        // Map cut zones to segments
        MapCutZonesToSegments();

        // if (logSegmentStates)
        // {
        //     Debug.Log($"[BodyPartTree] Hierarchy built with {segments.Count} segments");
        //     foreach (var seg in segments.Values)
        //     {
        //         Debug.Log($"[BodyPartTree]   {seg}");
        //     }
        // }
    }

    /// <summary>
    /// Create VirtualSegment objects for all 15 body segments
    /// </summary>
    void CreateAllSegments()
    {
        // Create root
        AddSegment(BodySegmentType.Upper_Torso, isRoot: true);

        // Create all other segments
        AddSegment(BodySegmentType.Head);
        AddSegment(BodySegmentType.Lower_Torso);
        AddSegment(BodySegmentType.L_Upper_Arm);
        AddSegment(BodySegmentType.L_Forearm);
        AddSegment(BodySegmentType.L_Hand);
        AddSegment(BodySegmentType.R_Upper_Arm);
        AddSegment(BodySegmentType.R_Forearm);
        AddSegment(BodySegmentType.R_Hand);
        AddSegment(BodySegmentType.L_Upper_Leg);
        AddSegment(BodySegmentType.L_Lower_Leg);
        AddSegment(BodySegmentType.L_Foot);
        AddSegment(BodySegmentType.R_Upper_Leg);
        AddSegment(BodySegmentType.R_Lower_Leg);
        AddSegment(BodySegmentType.R_Foot);
    }

    void AddSegment(BodySegmentType type, bool isRoot = false)
    {
        segments[type] = new VirtualSegment
        {
            segmentType = type,
            species = species,
            isRoot = isRoot
        };
    }

    /// <summary>
    /// Map CutZones to their corresponding parent/child segments
    /// </summary>
    void MapCutZonesToSegments()
    {
        foreach (var zone in allCutZones)
        {
            // Map based on (Limb, CutSection) → segment relationships
            MapCutZoneToSegments(zone);
        }
    }

    /// <summary>
    /// Assign a CutZone as parent or child cut for appropriate segments
    /// </summary>
    void MapCutZoneToSegments(CutZone zone)
    {
        Limb limb = zone.limb;
        CutSection section = zone.section;

        // HEAD: Neck cut
        if (limb == Limb.None && section == CutSection.Neck)
        {
            segments[BodySegmentType.Head].parentCut = zone;
            segments[BodySegmentType.Upper_Torso].childCuts.Add(zone);
        }
        // UPPER_TORSO → LOWER_TORSO: Torso middle cut
        else if (limb == Limb.None && section == CutSection.TorsoMiddle)
        {
            segments[BodySegmentType.Lower_Torso].parentCut = zone;
            segments[BodySegmentType.Upper_Torso].childCuts.Add(zone);
        }
        // LEFT ARM: Shoulder → Elbow → Wrist
        else if (limb == Limb.LeftArm && section == CutSection.ShoulderOrHip)
        {
            segments[BodySegmentType.L_Upper_Arm].parentCut = zone;
            segments[BodySegmentType.Upper_Torso].childCuts.Add(zone);
        }
        else if (limb == Limb.LeftArm && section == CutSection.ElbowOrKnee)
        {
            segments[BodySegmentType.L_Forearm].parentCut = zone;
            segments[BodySegmentType.L_Upper_Arm].childCuts.Add(zone);
        }
        else if (limb == Limb.LeftArm && section == CutSection.WristOrAnkle)
        {
            segments[BodySegmentType.L_Hand].parentCut = zone;
            segments[BodySegmentType.L_Forearm].childCuts.Add(zone);
        }
        // RIGHT ARM: Shoulder → Elbow → Wrist
        else if (limb == Limb.RightArm && section == CutSection.ShoulderOrHip)
        {
            segments[BodySegmentType.R_Upper_Arm].parentCut = zone;
            segments[BodySegmentType.Upper_Torso].childCuts.Add(zone);
        }
        else if (limb == Limb.RightArm && section == CutSection.ElbowOrKnee)
        {
            segments[BodySegmentType.R_Forearm].parentCut = zone;
            segments[BodySegmentType.R_Upper_Arm].childCuts.Add(zone);
        }
        else if (limb == Limb.RightArm && section == CutSection.WristOrAnkle)
        {
            segments[BodySegmentType.R_Hand].parentCut = zone;
            segments[BodySegmentType.R_Forearm].childCuts.Add(zone);
        }
        // LEFT LEG: Hip → Knee → Ankle
        else if (limb == Limb.LeftLeg && section == CutSection.ShoulderOrHip)
        {
            segments[BodySegmentType.L_Upper_Leg].parentCut = zone;
            segments[BodySegmentType.Lower_Torso].childCuts.Add(zone);
        }
        else if (limb == Limb.LeftLeg && section == CutSection.ElbowOrKnee)
        {
            segments[BodySegmentType.L_Lower_Leg].parentCut = zone;
            segments[BodySegmentType.L_Upper_Leg].childCuts.Add(zone);
        }
        else if (limb == Limb.LeftLeg && section == CutSection.WristOrAnkle)
        {
            segments[BodySegmentType.L_Foot].parentCut = zone;
            segments[BodySegmentType.L_Lower_Leg].childCuts.Add(zone);
        }
        // RIGHT LEG: Hip → Knee → Ankle
        else if (limb == Limb.RightLeg && section == CutSection.ShoulderOrHip)
        {
            segments[BodySegmentType.R_Upper_Leg].parentCut = zone;
            segments[BodySegmentType.Lower_Torso].childCuts.Add(zone);
        }
        else if (limb == Limb.RightLeg && section == CutSection.ElbowOrKnee)
        {
            segments[BodySegmentType.R_Lower_Leg].parentCut = zone;
            segments[BodySegmentType.R_Upper_Leg].childCuts.Add(zone);
        }
        else if (limb == Limb.RightLeg && section == CutSection.WristOrAnkle)
        {
            segments[BodySegmentType.R_Foot].parentCut = zone;
            segments[BodySegmentType.R_Lower_Leg].childCuts.Add(zone);
        }
    }

    /// <summary>
    /// Handle a cut event from CutRouter. Wire this to CutRouter.OnAnyCutEnter in inspector.
    /// </summary>
    public void HandleCut(CutContext ctx)
    {
        // Find the matching CutZone
        CutZone matchingZone = allCutZones.FirstOrDefault(z => z.limb == ctx.limb && z.section == ctx.section && z.precision == ctx.precision);

        if (matchingZone == null)
        {
            Debug.LogWarning($"[BodyPartTree] No matching CutZone for {ctx.limb}/{ctx.section}/{ctx.precision}");
            return;
        }

        // Mark zone as cut
        matchingZone.MarkAsCut();

        // Find which segment(s) this cut affects and update their cached cut data
        UpdateSegmentCutData(matchingZone, ctx);

        // Check all segments for isolation
        CheckAllSegmentsForIsolation();
    }

    /// <summary>
    /// Update cut data for segments affected by this cut
    /// </summary>
    void UpdateSegmentCutData(CutZone zone, CutContext ctx)
    {
        // Find segment(s) where this zone is the parent cut
        foreach (var seg in segments.Values)
        {
            if (seg.parentCut == zone)
            {
                seg.lastPrecision = ctx.precision;
                seg.lastToolUsed = ctx.toolType;
                seg.lastCutSection = ctx.section;
            }
        }
    }

    /// <summary>
    /// Check all segments to see if any have become fully isolated
    /// </summary>
    void CheckAllSegmentsForIsolation()
    {
        foreach (var seg in segments.Values)
        {
            if (seg.IsFullyIsolated())
            {
                OnSegmentFullyIsolated(seg);
            }
        }
    }

    /// <summary>
    /// Called when a segment becomes fully isolated
    /// </summary>
    void OnSegmentFullyIsolated(VirtualSegment segment)
    {
        if (logSegmentStates)
        {
            Debug.Log($"[BodyPartTree] {segment.segmentType} is FULLY ISOLATED → Depositing");
        }

        // Generate severed part data
        SeveredPartData partData = segment.GeneratePartData();

        // Send to deposit tray
        if (depositTray != null)
        {
            depositTray.AddPart(partData);
        }
        else
        {
            Debug.LogWarning("[BodyPartTree] No DepositTray assigned, cannot deposit part!");
        }

        // Mark segment as already deposited (prevent double-deposit)
        // We do this by making it non-root (so IsFullyIsolated returns false next check)
        // A cleaner approach would be to add a "hasBeenDeposited" flag
        segment.isRoot = true;  // Hack: make it think it's root so it won't isolate again
    }
}

