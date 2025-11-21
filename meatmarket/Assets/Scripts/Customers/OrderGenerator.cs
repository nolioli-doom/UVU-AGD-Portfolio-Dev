using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class OrderGenerator : MonoBehaviour
{
    [Header("Tuning Data")]
    public List<PlushieSpeciesSO> speciesDefs;
    public List<CustomerArchetypeSO> customerArchetypes;
    public List<OrderDifficultySO> difficultyByDay; // index clamped

    [Header("Order Management")]
    [Tooltip("Optional: Automatically populate this OrderManagerSO when generating rounds")]
    public OrderManagerSO orderManagerSO;

    [Header("Events")]
    public UnityEvent onRoundGenerated;  // fire when ready; consumers can read CurrentRound
    [Tooltip("Optional: send a copy to listeners after generation")]
    public OrdersRoundEvent onRoundGeneratedWithPayload;
    [Serializable] public class OrdersRoundEvent : UnityEvent<OrdersRound> { }

    [Header("Debug/State")]
    public OrdersRound CurrentRound;

    System.Random rng = new System.Random();

    /// <summary>
    /// Generate a test round with day index 0 (for quick testing via button)
    /// </summary>
    public void GenerateTestRound()
    {
        GenerateRound(0);
    }

    public void GenerateRound(int dayIndex)
    {
        var diff = difficultyByDay[Mathf.Clamp(dayIndex, 0, difficultyByDay.Count - 1)];

        // 1) Pick customer count
        int customers = UnityEngine.Random.Range(diff.customersRange.x, diff.customersRange.y + 1);

        // 2) Choose round “theme” species (1–2) to force overlap
        var weighted = BuildWeightedSpeciesList();
        var themeSpecies = PickThemeSpecies(weighted, count: rng.Next(1, 3)); // 1 or 2

        // 3) Create orders
        var orders = new List<CustomerOrder>();
        var roundPartPool = new List<(SpeciesType, OrderPartType)>(); // used to bias overlap across customers

        for (int i = 0; i < customers; i++)
        {
            var archetype = customerArchetypes[UnityEngine.Random.Range(0, customerArchetypes.Count)];
            var order = new CustomerOrder
            {
                archetype = archetype,
                tipMultiplier = archetype.tipMultiplier,
                wasteSensitivity = archetype.wasteSensitivity,
                customerName = string.IsNullOrEmpty(archetype.displayName) ? $"Customer {i + 1}" : archetype.displayName
            };

            // Time limit blended by patience/speed
            float tMin = diff.timeLimitSecondsRange.x;
            float tMax = diff.timeLimitSecondsRange.y;
            float baseTime = UnityEngine.Random.Range(tMin, tMax);
            baseTime /= archetype.speedBias;
            baseTime *= archetype.patienceMultiplier;
            order.timeLimitSeconds = Mathf.Clamp(baseTime, 20f, 300f);

            // Total pieces count (each piece is a separate item, even if same type)
            int totalPieces = UnityEngine.Random.Range(diff.itemsPerOrderRange.x, diff.itemsPerOrderRange.y + 1);

            // Force within-customer same species sometimes
            SpeciesType? lockedSpecies = null;
            if (UnityEngine.Random.value < diff.sameSpeciesBias)
                lockedSpecies = themeSpecies[rng.Next(themeSpecies.Count)];

            for (int k = 0; k < totalPieces; k++)
            {
                SpeciesType s;
                if (lockedSpecies.HasValue) s = lockedSpecies.Value;
                else s = (UnityEngine.Random.value < diff.roundThemeBias)
                      ? themeSpecies[rng.Next(themeSpecies.Count)]
                      : weighted[rng.Next(weighted.Count)].species;

                var def = speciesDefs.First(d => d.species == s);
                OrderPartType partType = PickPartType(def);

                // Each piece is quantity 1 (no more quantity system)
                int qty = 1;

                // Quality: bias via curve & archetype precision
                var q = RollQuality(diff.qualityCurve, archetype.precisionBias);

                order.items.Add(new OrderItem { species = s, partType = partType, quantity = qty, minQuality = q });

                // Store to encourage cross-customer reuse
                roundPartPool.Add((s, partType));
            }

            orders.Add(order);
        }

        // 4) Encourage overlap across customers by mutating some items to parts already requested
        if (roundPartPool.Count > 0 && diff.overlapAcrossCustomers > 0f)
        {
            int mutations = Mathf.CeilToInt(orders.Sum(o => o.items.Count) * diff.overlapAcrossCustomers * 0.35f);
            for (int m = 0; m < mutations; m++)
            {
                var sample = roundPartPool[rng.Next(roundPartPool.Count)];
                var targetOrder = orders[rng.Next(orders.Count)];
                if (targetOrder.items.Count == 0) continue;
                var targetItem = targetOrder.items[rng.Next(targetOrder.items.Count)];
                targetItem.species = sample.Item1;
                targetItem.partType = sample.Item2;
            }
        }

        // 5) Respect species supply caps loosely (swap some requests if wildly over)
        var impliedBodies = EstimateBodiesNeeded(orders);
        ApplySupplySoftCaps(orders, impliedBodies, diff);

        // 6) Finalize round
        CurrentRound = new OrdersRound { dayIndex = dayIndex, orders = orders, impliedBodiesNeeded = impliedBodies };
        
        // 7) Populate OrderManagerSO if assigned
        if (orderManagerSO != null)
        {
            orderManagerSO.SetOrders(orders);
        }
        
        onRoundGenerated?.Invoke();
        onRoundGeneratedWithPayload?.Invoke(CloneRound(CurrentRound)); // send a safe copy
    }

    // ===== Helpers =====

    /// <summary>
    /// Get the difficulty name for a given day index
    /// </summary>
    /// <summary>
    /// Get the difficulty SO for a given day index
    /// </summary>
    public OrderDifficultySO GetDifficultyForDay(int dayIndex)
    {
        if (difficultyByDay == null || difficultyByDay.Count == 0)
        {
            Debug.LogError("[OrderGenerator] No difficulties configured!");
            return null;
        }
        return difficultyByDay[Mathf.Clamp(dayIndex, 0, difficultyByDay.Count - 1)];
    }
    
    public string GetDifficultyName(int dayIndex)
    {
        if (difficultyByDay == null || difficultyByDay.Count == 0)
            return "Unknown";
        
        int clampedIndex = Mathf.Clamp(dayIndex, 0, difficultyByDay.Count - 1);
        return difficultyByDay[clampedIndex] != null ? difficultyByDay[clampedIndex].name : "Unknown";
    }

    private struct WeightedSpecies { public SpeciesType species; public float weight; public PlushieSpeciesSO def; }

    private List<WeightedSpecies> BuildWeightedSpeciesList()
    {
        var list = new List<WeightedSpecies>();
        foreach (var d in speciesDefs)
        {
            float w = Mathf.Max(0.001f, d.rarityWeight);
            list.Add(new WeightedSpecies { species = d.species, weight = w, def = d });
        }
        return list;
    }

    private List<SpeciesType> PickThemeSpecies(List<WeightedSpecies> weighted, int count)
    {
        var pool = weighted.ToList();
        var result = new List<SpeciesType>();
        count = Mathf.Clamp(count, 1, Mathf.Min(2, pool.Count));
        for (int i = 0; i < count; i++)
        {
            float total = pool.Sum(p => p.weight);
            float r = (float)rng.NextDouble() * total;
            float acc = 0f;
            foreach (var w in pool)
            {
                acc += w.weight;
                if (r <= acc)
                {
                    result.Add(w.species);
                    pool.Remove(w);
                    break;
                }
            }
        }
        return result;
    }

    private OrderPartType PickPartType(PlushieSpeciesSO def)
    {
        // Pick from all available part types for this species
        var valid = def.yields.Select(y => y.partType).ToList();
        if (valid.Count == 0) return OrderPartType.Scrap;
        
        // Mild bias toward limb parts to create overlap opportunities
        var limbParts = new[] { 
            OrderPartType.Upper_Arm,
            OrderPartType.Forearm,
            OrderPartType.Hand,
            OrderPartType.Upper_Leg,
            OrderPartType.Lower_Leg,
            OrderPartType.Foot
        };
        
        if (rng.NextDouble() < 0.55 && valid.Any(limbParts.Contains))
            return limbParts.First(p => valid.Contains(p));
        
        return valid[rng.Next(valid.Count)];
    }

    private QualityTier RollQuality(AnimationCurve curve, float precisionBias)
    {
        float r = Mathf.Clamp01((float)rng.NextDouble());
        // bias toward high quality by skewing r downward with precision
        r = Mathf.Pow(r, 1f / Mathf.Clamp(precisionBias, 0.1f, 4f));
        float t = curve.Evaluate(r); // 0..1
        if (t > 0.85f) return QualityTier.Perfect;
        if (t > 0.6f)  return QualityTier.High;
        if (t > 0.3f)  return QualityTier.Normal;
        return QualityTier.Low;
    }

    private Dictionary<SpeciesType, int> EstimateBodiesNeeded(List<CustomerOrder> orders)
    {
        // Simple estimate: count total pieces needed
        var map = new Dictionary<(SpeciesType, OrderPartType), int>();
        foreach (var o in orders)
        foreach (var it in o.items)
        {
            var key = (it.species, it.partType);
            map[key] = map.TryGetValue(key, out var v) ? v + 1 : 1;
        }

        var bodiesBySpecies = new Dictionary<SpeciesType, int>();
        foreach (var pair in map)
        {
            var def = speciesDefs.First(d => d.species == pair.Key.Item1);
            int perBody = Mathf.Max(1, def.GetYield(pair.Key.Item2)); // safety: min 1
            int bodies = Mathf.CeilToInt(pair.Value / (float)perBody);
            bodiesBySpecies[pair.Key.Item1] = bodiesBySpecies.TryGetValue(pair.Key.Item1, out var b) ? Mathf.Max(b, bodies) : bodies;
        }
        return bodiesBySpecies;
    }

    private void ApplySupplySoftCaps(List<CustomerOrder> orders, Dictionary<SpeciesType, int> impliedBodies, OrderDifficultySO diff)
    {
        foreach (var kv in impliedBodies.ToList())
        {
            var def = speciesDefs.First(d => d.species == kv.Key);
            int cap = def.dailySupplyCap + diff.softSpeciesCapBuffer;
            if (kv.Value <= cap) continue;

            // Swap some items to a more common species (heuristic)
            int over = kv.Value - cap;
            ReplaceSpeciesInSomeItems(orders, kv.Key, over);
        }
    }

    private void ReplaceSpeciesInSomeItems(List<CustomerOrder> orders, SpeciesType tooMany, int countToReduce)
    {
        // Find a replacement species with high rarityWeight
        var replacement = speciesDefs.OrderByDescending(s => s.rarityWeight).First().species;

        foreach (var order in orders)
        {
            foreach (var item in order.items)
            {
                if (countToReduce <= 0) return;
                if (item.species != tooMany) continue;

                // swap species; keep same part/qty
                item.species = replacement;
                countToReduce--;
            }
        }
    }

    private OrdersRound CloneRound(OrdersRound src)
    {
        var clone = new OrdersRound
        {
            dayIndex = src.dayIndex,
            orders = new List<CustomerOrder>(),
            impliedBodiesNeeded = new Dictionary<SpeciesType, int>(src.impliedBodiesNeeded)
        };
        foreach (var o in src.orders)
        {
            var c = new CustomerOrder
            {
                archetype = o.archetype,
                timeLimitSeconds = o.timeLimitSeconds,
                tipMultiplier = o.tipMultiplier,
                wasteSensitivity = o.wasteSensitivity,
                customerName = o.customerName,
                items = new List<OrderItem>()
            };
            foreach (var it in o.items)
                c.items.Add(new OrderItem { species = it.species, partType = it.partType, quantity = 1, minQuality = it.minQuality });
            clone.orders.Add(c);
        }
        return clone;
    }
}