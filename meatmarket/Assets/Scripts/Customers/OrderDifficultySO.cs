using UnityEngine;

[CreateAssetMenu(menuName = "Butchery/Order Difficulty Tier")]
public class OrderDifficultySO : ScriptableObject
{
    [Header("Customer Count per Round")]
    public Vector2Int customersRange = new(2, 4);

    [Header("Items per Order")]
    public Vector2Int itemsPerOrderRange = new(2, 4);

    [Header("Overlap / Theme")]
    [Range(0f, 1f)] public float roundThemeBias = 0.65f;   // chance items prefer theme species
    [Range(0f, 1f)] public float sameSpeciesBias = 0.55f;  // within-customer: stick to one species
    [Range(0f, 1f)] public float overlapAcrossCustomers = 0.6f; // re-use parts across customers

    [Header("Quality Targets")]
    public AnimationCurve qualityCurve = AnimationCurve.Linear(0, 0.4f, 1, 0.8f); // maps 0..1 -> High/Perfect likelihood
    public Vector2 timeLimitSecondsRange = new(45f, 120f);

    [Header("Rarity & Caps")]
    [Range(0f, 1f)] public float rareSpeciesFavor = 0.25f; // how often to pull rarer species
    public int softSpeciesCapBuffer = 2;                    // allow exceeding cap by small buffer (then adjust)
    
    [Header("Day Timer")]
    [Tooltip("Total time limit for the day in seconds. Day ends when this timer hits zero.")]
    public float dayTimerDuration = 300f; // 5 minutes default
}