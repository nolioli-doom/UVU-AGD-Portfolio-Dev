using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CutZone : MonoBehaviour
{
    [Header("Metadata")]
    public Limb limb = Limb.None;
    public CutSection section = CutSection.Neck;          // set appropriately
    public CutPrecision precision = CutPrecision.Perfect; // MissNorth/MissSouth/Perfect

    [Header("Trigger Mode")]
    public bool useTriggerCallbacks = false;   // enable if using OnTriggerEnter as the cut input
    public bool debugLog = true;

    [Header("Cut State (Runtime)")]
    public bool HasBeenCut { get; private set; } = false;

    // C# events (router subscribes)
    public event Action<CutContext> OnCutEnter;
    public event Action<CutContext> OnCutExit;

    Collider col; Transform root;

    void Awake()
    {
        col = GetComponent<Collider>();
        if (!col.isTrigger && useTriggerCallbacks)
            Debug.LogWarning($"{name}: Collider is not trigger but useTriggerCallbacks is true.", this);

        // assume plushie root is the topmost with a CutRouter or just top parent
        root = GetComponentInParent<CutRouter>() ? GetComponentInParent<CutRouter>().transform : transform.root;
    }

    // If you want to drive cuts from a raycast, call this from your tool when it hits the collider
    public void NotifyRaycastHit(RaycastHit hit, GameObject toolGO)
    {
        var toolType = ToolManager.Instance != null ? ToolManager.Instance.CurrentTool : default(ToolManager.ToolType);
        var bodyType = GetBodyTypeFromRoot();
        var ctx = new CutContext(root, limb, section, precision, hit.point, hit.normal, toolGO, toolType, bodyType);
        if (debugLog) Debug.Log($"CutZone hit: {limb}/{section}/{precision} at {hit.point}", this);
        OnCutEnter?.Invoke(ctx);
    }

    // Optional trigger-driven mode (e.g., blade enters trigger)
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerCallbacks) return;
        var p = transform.position; // best-effort hit point if none
        var toolType = ToolManager.Instance != null ? ToolManager.Instance.CurrentTool : default(ToolManager.ToolType);
        var bodyType = GetBodyTypeFromRoot();
        var ctx = new CutContext(root, limb, section, precision, p, Vector3.up, other.gameObject, toolType, bodyType);
        OnCutEnter?.Invoke(ctx);
    }

    void OnTriggerExit(Collider other)
    {
        if (!useTriggerCallbacks) return;
        var p = transform.position;
        var toolType = ToolManager.Instance != null ? ToolManager.Instance.CurrentTool : default(ToolManager.ToolType);
        var bodyType = GetBodyTypeFromRoot();
        var ctx = new CutContext(root, limb, section, precision, p, Vector3.up, other.gameObject, toolType, bodyType);
        OnCutExit?.Invoke(ctx);
    }

    // Helper method to get body type from the root object
    private string GetBodyTypeFromRoot()
    {
        // Try to find a DismemberableBody component first (for backward compatibility)
        var dismemberableBody = root.GetComponent<DismemberableBody>();
        if (dismemberableBody != null)
        {
            // Use reflection to get the bodyType field
            var field = typeof(DismemberableBody).GetField("bodyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(dismemberableBody)?.ToString() ?? "UNKNOWN";
            }
        }
        
        // Fallback: try to get body type from CutRouter if it has one
        var cutRouter = root.GetComponent<CutRouter>();
        if (cutRouter != null)
        {
            return cutRouter.BodyType;
        }
        
        // Final fallback
        return "UNKNOWN";
    }

    /// <summary>
    /// Mark this cut zone as having been cut (called by BodyPartTree)
    /// </summary>
    public void MarkAsCut()
    {
        if (HasBeenCut) return;  // Prevent double-cutting
        
        HasBeenCut = true;
        if (debugLog) Debug.Log($"[CutZone] {limb}/{section}/{precision} marked as CUT", this);
    }
}