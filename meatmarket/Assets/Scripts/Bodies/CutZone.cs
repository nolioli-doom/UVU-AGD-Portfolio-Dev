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
        var ctx = new CutContext(root, limb, section, precision, hit.point, hit.normal, toolGO);
        if (debugLog) Debug.Log($"CutZone hit: {limb}/{section}/{precision} at {hit.point}", this);
        OnCutEnter?.Invoke(ctx);
    }

    // Optional trigger-driven mode (e.g., blade enters trigger)
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerCallbacks) return;
        var p = transform.position; // best-effort hit point if none
        var ctx = new CutContext(root, limb, section, precision, p, Vector3.up, other.gameObject);
        OnCutEnter?.Invoke(ctx);
    }

    void OnTriggerExit(Collider other)
    {
        if (!useTriggerCallbacks) return;
        var p = transform.position;
        var ctx = new CutContext(root, limb, section, precision, p, Vector3.up, other.gameObject);
        OnCutExit?.Invoke(ctx);
    }
}