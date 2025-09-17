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

    public CutContext(Transform root, Limb limb, CutSection section, CutPrecision precision,
        Vector3 hitPoint, Vector3 hitNormal, GameObject tool)
    {
        this.root = root;
        this.limb = limb;
        this.section = section;
        this.precision = precision;
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
        this.tool = tool;
    }
}