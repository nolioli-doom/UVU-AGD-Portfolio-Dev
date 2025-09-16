using UnityEngine;

[CreateAssetMenu(menuName = "Butchery/Customer Archetype")]
public class CustomerArchetypeSO : ScriptableObject
{
    [Header("Behavior & Scoring Modifiers")]
    [Range(0.5f, 2f)] public float patienceMultiplier = 1f;   // affects time limit
    [Range(0.5f, 2f)] public float precisionBias = 1f;        // weights toward higher quality
    [Range(0.5f, 2f)] public float speedBias = 1f;            // weights toward shorter timers
    [Range(0f, 1f)] public float wasteSensitivity = 0.5f;     // penalize scraps/waste
    [Range(0.5f, 2f)] public float tipMultiplier = 1f;

    [Header("Flavor")]
    public string displayName;
    public string traitNote; // e.g., "Hates scraps", "Loves fresh cuts"
}