using UnityEngine;
using UnityEngine.Events;

public class CutRouter : MonoBehaviour
{
    [System.Serializable] public class CutEvent : UnityEvent<CutContext> { }

    [Header("Body Configuration")]
    [SerializeField] private string bodyType = "CAT";

    [Header("Impact VFX (Shared for all CutZones)")]
    [Tooltip("Impact effect prefabs to spawn on first click for all child CutZones. If not set on individual CutZones, they will use these. If multiple prefabs are provided, one will be randomly selected.")]
    public GameObject[] sharedImpactVFXPrefabs;

    [Tooltip("Parent transform for spawned VFX (optional). If null, VFX will be spawned at root level.")]
    public Transform sharedVFXParent;

    [Header("Routed Events (wire these ONCE)")]
    public CutEvent OnAnyCutEnter;
    public CutEvent OnPerfectCutEnter;
    public CutEvent OnMissCutEnter;           // both MissNorth / MissSouth

    [Header("Debug")]
    public bool logCuts = false;
    public bool logToolUsage = true;          // log which tool was used on which body part

    // Public property to access body type
    public string BodyType => bodyType;

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
        {
            string jointName = ctx.GetSpecificJointName();
            string limbInfo = ctx.limb != Limb.None ? $"{ctx.limb}/" : "";
            Debug.Log($"[{name}] CUT: {limbInfo}{jointName}/{ctx.precision} at {ctx.hitPoint}", this);
        }

        // Enhanced logging with tool and body type information
        if (logToolUsage)
        {
            string jointName = ctx.GetSpecificJointName();
            string limbInfo = ctx.limb != Limb.None ? $"{ctx.limb}/" : "";
            Debug.Log($"[CutRouter] {ctx.toolType} used on {ctx.bodyType} - {limbInfo}{jointName}/{ctx.precision} cut at {ctx.hitPoint}", this);
        }

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