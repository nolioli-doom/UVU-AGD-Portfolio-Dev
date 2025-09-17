using UnityEngine;
using UnityEngine.Events;

public class CutRouter : MonoBehaviour
{
    [System.Serializable] public class CutEvent : UnityEvent<CutContext> { }

    [Header("Routed Events (wire these ONCE)")]
    public CutEvent OnAnyCutEnter;
    public CutEvent OnPerfectCutEnter;
    public CutEvent OnMissCutEnter;           // both MissNorth / MissSouth

    [Header("Debug")]
    public bool logCuts = false;

    void Awake()
    {
        var zones = GetComponentsInChildren<CutZone>(includeInactive: true);
        foreach (var z in zones)
        {
            z.OnCutEnter += HandleCutEnter;
            z.OnCutExit  += HandleCutExit; // available if you need it
        }
    }

    void OnDestroy()
    {
        var zones = GetComponentsInChildren<CutZone>(includeInactive: true);
        foreach (var z in zones)
        {
            z.OnCutEnter -= HandleCutEnter;
            z.OnCutExit  -= HandleCutExit;
        }
    }

    void HandleCutEnter(CutContext ctx)
    {
        if (logCuts)
            Debug.Log($"[{name}] CUT: {ctx.limb}/{ctx.section}/{ctx.precision} at {ctx.hitPoint}", this);

        OnAnyCutEnter?.Invoke(ctx);

        if (ctx.precision == CutPrecision.Perfect)
            OnPerfectCutEnter?.Invoke(ctx);
        else
            OnMissCutEnter?.Invoke(ctx);
    }

    void HandleCutExit(CutContext ctx)
    {
        // expose a CutExit event if you need it later
    }
}