/// <summary>
/// Utility class for mapping between specific BodySegmentType (L/R) and generic OrderPartType.
/// </summary>
public static class PartTypeMapper
{
    /// <summary>
    /// Convert a specific BodySegmentType to its generic OrderPartType.
    /// Example: L_Hand → Hand, R_Hand → Hand
    /// </summary>
    public static OrderPartType SegmentToOrderType(BodySegmentType segment)
    {
        switch (segment)
        {
            case BodySegmentType.Head:
                return OrderPartType.Head;
            
            case BodySegmentType.Upper_Torso:
                return OrderPartType.Upper_Torso;
            
            case BodySegmentType.Lower_Torso:
                return OrderPartType.Lower_Torso;
            
            case BodySegmentType.L_Upper_Arm:
            case BodySegmentType.R_Upper_Arm:
                return OrderPartType.Upper_Arm;
            
            case BodySegmentType.L_Forearm:
            case BodySegmentType.R_Forearm:
                return OrderPartType.Forearm;
            
            case BodySegmentType.L_Hand:
            case BodySegmentType.R_Hand:
                return OrderPartType.Hand;
            
            case BodySegmentType.L_Upper_Leg:
            case BodySegmentType.R_Upper_Leg:
                return OrderPartType.Upper_Leg;
            
            case BodySegmentType.L_Lower_Leg:
            case BodySegmentType.R_Lower_Leg:
                return OrderPartType.Lower_Leg;
            
            case BodySegmentType.L_Foot:
            case BodySegmentType.R_Foot:
                return OrderPartType.Foot;
            
            case BodySegmentType.Scrap:
            default:
                return OrderPartType.Scrap;
        }
    }

    /// <summary>
    /// Check if a specific segment matches a generic order type.
    /// Example: L_Hand matches Hand, R_Forearm matches Forearm
    /// </summary>
    public static bool DoesSegmentMatchOrderType(BodySegmentType segment, OrderPartType orderType)
    {
        return SegmentToOrderType(segment) == orderType;
    }
}

