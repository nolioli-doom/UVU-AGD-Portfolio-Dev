using UnityEngine;

public enum SpeciesType { Cat, Dog, Bunny }

// Legacy enum - kept for backwards compatibility but deprecated
public enum BodyPartType { Head, Torso, Arm, Leg, Tail, Scrap }

// New granular body segment enum for precise cutting system (specific L/R tracking)
public enum BodySegmentType 
{ 
    Head,
    Upper_Torso,
    Lower_Torso,
    L_Upper_Arm,
    L_Forearm,
    L_Hand,
    R_Upper_Arm,
    R_Forearm,
    R_Hand,
    L_Upper_Leg,
    L_Lower_Leg,
    L_Foot,
    R_Upper_Leg,
    R_Lower_Leg,
    R_Foot,
    Scrap
}

// Generic part type for orders and yields (L/R are treated as equivalent)
public enum OrderPartType
{
    Head,
    Upper_Torso,
    Lower_Torso,
    Upper_Arm,      // Matches either L_Upper_Arm or R_Upper_Arm
    Forearm,        // Matches either L_Forearm or R_Forearm
    Hand,           // Matches either L_Hand or R_Hand
    Upper_Leg,      // Matches either L_Upper_Leg or R_Upper_Leg
    Lower_Leg,      // Matches either L_Lower_Leg or R_Lower_Leg
    Foot,           // Matches either L_Foot or R_Foot
    Scrap
}

public enum QualityTier { Low, Normal, High, Perfect }