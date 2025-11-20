using UnityEngine;

public enum Limb { LeftArm, RightArm, LeftLeg, RightLeg, None }
public enum CutSection { ShoulderOrHip, ElbowOrKnee, WristOrAnkle, Neck, TorsoMiddle }
public enum CutPrecision { MissNorth, MissSouth, Perfect }

public struct CutContext
{
    public Transform root;          // the plushie root
    public Limb limb;
    public CutSection section;
    public CutPrecision precision;
    public Vector3 hitPoint;        // world position (optional)
    public Vector3 hitNormal;       // world normal (optional)
    public GameObject tool;         // who did the cutting (optional)
    public ToolManager.ToolType toolType; // specific tool type used
    public string bodyType;         // type of body being cut (e.g., "CAT", "DOG", etc.)

    public CutContext(Transform root, Limb limb, CutSection section, CutPrecision precision,
        Vector3 hitPoint, Vector3 hitNormal, GameObject tool, ToolManager.ToolType toolType, string bodyType)
    {
        this.root = root;
        this.limb = limb;
        this.section = section;
        this.precision = precision;
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
        this.tool = tool;
        this.toolType = toolType;
        this.bodyType = bodyType;
    }

    /// <summary>
    /// Returns the specific joint name based on limb and section metadata.
    /// For example: LeftArm + ShoulderOrHip = "Shoulder", LeftLeg + ElbowOrKnee = "Knee"
    /// </summary>
    public string GetSpecificJointName()
    {
        return GetSpecificJointName(limb, section);
    }

    /// <summary>
    /// Static helper method to get specific joint name from limb and section.
    /// </summary>
    public static string GetSpecificJointName(Limb limb, CutSection section)
    {
        switch (section)
        {
            case CutSection.Neck:
                return "Neck";
            case CutSection.TorsoMiddle:
                return "TorsoMiddle";
            case CutSection.ShoulderOrHip:
                return IsArm(limb) ? "Shoulder" : "Hip";
            case CutSection.ElbowOrKnee:
                return IsArm(limb) ? "Elbow" : "Knee";
            case CutSection.WristOrAnkle:
                return IsArm(limb) ? "Wrist" : "Ankle";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Helper method to determine if a limb is an arm.
    /// </summary>
    private static bool IsArm(Limb limb)
    {
        return limb == Limb.LeftArm || limb == Limb.RightArm;
    }
}