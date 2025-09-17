using UnityEngine;

public class ScoringReceiver : MonoBehaviour
{
    public void HandleAnyCut(CutContext ctx)  { Debug.Log($"ANY: {ctx.limb}/{ctx.section}/{ctx.precision}"); }
    public void HandlePerfect(CutContext ctx) { Debug.Log($"PERFECT: +score at {ctx.hitPoint}"); }
    public void HandleMiss(CutContext ctx)    { Debug.Log($"MISS: spark FX at {ctx.hitPoint}"); }
}