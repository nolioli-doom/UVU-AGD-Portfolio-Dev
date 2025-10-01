using UnityEngine;

public class ScoringReceiver : MonoBehaviour
{
    public void HandleAnyCut(CutContext ctx)  
    { 
        string jointName = ctx.GetSpecificJointName();
        string limbInfo = ctx.limb != Limb.None ? $"{ctx.limb}/" : "";
        Debug.Log($"ANY CUT: {ctx.toolType} used on {ctx.bodyType} - {limbInfo}{jointName}/{ctx.precision} at {ctx.hitPoint}"); 
    }
    
    public void HandlePerfect(CutContext ctx) 
    { 
        string jointName = ctx.GetSpecificJointName();
        string limbInfo = ctx.limb != Limb.None ? $"{ctx.limb}/" : "";
        Debug.Log($"PERFECT CUT: {ctx.toolType} used on {ctx.bodyType} - {limbInfo}{jointName} - +score at {ctx.hitPoint}"); 
    }
    
    public void HandleMiss(CutContext ctx)    
    { 
        string jointName = ctx.GetSpecificJointName();
        string limbInfo = ctx.limb != Limb.None ? $"{ctx.limb}/" : "";
        Debug.Log($"MISS CUT: {ctx.toolType} used on {ctx.bodyType} - {limbInfo}{jointName}/{ctx.precision} - spark FX at {ctx.hitPoint}"); 
    }
}