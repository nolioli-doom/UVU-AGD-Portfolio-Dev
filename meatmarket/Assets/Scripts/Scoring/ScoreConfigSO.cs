using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject containing all scoring configuration and tuning values
/// </summary>
[CreateAssetMenu(menuName = "Butchery/Score Config")]
public class ScoreConfigSO : ScriptableObject
{
    [Header("Base Values")]
    [Tooltip("Base score awarded per segment deposited")]
    public int baseScorePerSegment = 10;

    [Header("Quality Multipliers")]
    [Range(0.1f, 3f)] public float perfectQualityMultiplier = 1.5f;
    [Range(0.1f, 3f)] public float highQualityMultiplier = 1.2f;
    [Range(0.1f, 3f)] public float normalQualityMultiplier = 1.0f;
    [Range(0.1f, 3f)] public float lowQualityMultiplier = 0.7f;

    [Header("Precision Bonus")]
    [Tooltip("Bonus percentage for Perfect precision cuts (0.25 = 25% bonus)")]
    [Range(0f, 1f)] public float precisionBonusPercent = 0.25f;

    [Header("Tool Bonuses")]
    [Tooltip("Bonus percentage when using the correct tool (0.15 = 15% bonus)")]
    [Range(0f, 1f)] public float correctToolBonusPercent = 0.15f;

    [Header("Tool-Joint Mappings")]
    [Tooltip("Define which tools are optimal for which cut sections")]
    public List<ToolJointMapping> toolMappings = new List<ToolJointMapping>()
    {
        new ToolJointMapping { tool = ToolManager.ToolType.Cleaver, preferredSections = new List<CutSection> { CutSection.ElbowOrKnee } },
        new ToolJointMapping { tool = ToolManager.ToolType.Shears, preferredSections = new List<CutSection> { CutSection.WristOrAnkle } },
        new ToolJointMapping { tool = ToolManager.ToolType.BoneSaw, preferredSections = new List<CutSection> { CutSection.Neck, CutSection.TorsoMiddle, CutSection.ShoulderOrHip } }
    };

    [Serializable]
    public class ToolJointMapping
    {
        public ToolManager.ToolType tool;
        public List<CutSection> preferredSections;
    }

    /// <summary>
    /// Get quality multiplier based on quality tier
    /// </summary>
    public float GetQualityMultiplier(QualityTier quality)
    {
        switch (quality)
        {
            case QualityTier.Perfect: return perfectQualityMultiplier;
            case QualityTier.High: return highQualityMultiplier;
            case QualityTier.Normal: return normalQualityMultiplier;
            case QualityTier.Low: return lowQualityMultiplier;
            default: return normalQualityMultiplier;
        }
    }

    /// <summary>
    /// Check if tool is correct for the given cut section
    /// </summary>
    public bool IsCorrectTool(ToolManager.ToolType tool, CutSection section)
    {
        foreach (var mapping in toolMappings)
        {
            if (mapping.tool == tool && mapping.preferredSections.Contains(section))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Calculate total score for a severed part
    /// </summary>
    public int CalculatePartScore(SeveredPartData part, OrderItem orderItem, CustomerArchetypeSO archetype)
    {
        // Base score
        float score = baseScorePerSegment;

        // Apply quality multiplier
        float qualityMult = GetQualityMultiplier(part.quality);
        score *= qualityMult;

        // Apply precision bonus (Perfect cuts get extra)
        if (part.precision == CutPrecision.Perfect)
        {
            score += baseScorePerSegment * precisionBonusPercent;
        }

        // Apply tool bonus (correct tool gets extra)
        if (IsCorrectTool(part.toolUsed, part.cutSection))
        {
            score += baseScorePerSegment * correctToolBonusPercent;
        }

        // Apply archetype multipliers
        float archetypeMult = archetype.tipMultiplier;

        // If quality meets or exceeds order requirement, apply precision bias bonus
        if (part.quality >= orderItem.minQuality)
        {
            archetypeMult *= archetype.precisionBias;
        }
        else
        {
            // Penalty for below-quality delivery
            archetypeMult *= 0.7f;
        }

        score *= archetypeMult;

        return Mathf.RoundToInt(score);
    }
}

