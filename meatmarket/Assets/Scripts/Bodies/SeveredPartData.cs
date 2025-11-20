using UnityEngine;

/// <summary>
/// Data structure representing a severed body segment that has been deposited to the tray.
/// Contains all information needed for order matching and scoring.
/// </summary>
[System.Serializable]
public class SeveredPartData
{
    [Header("Identity")]
    public SpeciesType species;
    public BodySegmentType segmentType;

    [Header("Quality")]
    public QualityTier quality;
    public CutPrecision precision;

    [Header("Tool Used")]
    public ToolManager.ToolType toolUsed;

    [Header("Cut Location")]
    public CutSection cutSection;  // Which joint was cut to sever this

    public SeveredPartData(SpeciesType species, BodySegmentType segmentType, 
                          QualityTier quality, CutPrecision precision, 
                          ToolManager.ToolType toolUsed, CutSection cutSection)
    {
        this.species = species;
        this.segmentType = segmentType;
        this.quality = quality;
        this.precision = precision;
        this.toolUsed = toolUsed;
        this.cutSection = cutSection;
    }

    public override string ToString()
    {
        return $"{segmentType} ({species}, {quality}, {precision}, {toolUsed})";
    }
}

