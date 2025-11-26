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

    [Header("Impact VFX")]
    [Tooltip("Impact effect prefabs to spawn on first click. If not assigned, will use shared VFX from CutRouter. If multiple prefabs are provided, one will be randomly selected.")]
    public GameObject[] impactVFXPrefabs;

    [Tooltip("Parent transform for spawned VFX (optional). If null, will use shared VFX parent from CutRouter, or spawn at root level.")]
    public Transform vfxParent;

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
        // Check if interactions are locked (e.g., during day end screen)
        if (InteractionLockManager.IsLocked)
        {
            return; // Don't process cut zones when locked
        }

        // Spawn impact VFX on first click (before the cut is processed)
        if (!HasBeenCut)
        {
            SpawnImpactVFX(hit.point, hit.normal);
        }

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
        
        // Check if interactions are locked (e.g., during day end screen)
        if (InteractionLockManager.IsLocked)
        {
            return; // Don't process trigger cuts when locked
        }

        var p = transform.position; // best-effort hit point if none
        var normal = -transform.forward; // Use forward direction as normal approximation
        
        // Spawn impact VFX on first click (before the cut is processed)
        if (!HasBeenCut)
        {
            SpawnImpactVFX(p, normal);
        }

        var toolType = ToolManager.Instance != null ? ToolManager.Instance.CurrentTool : default(ToolManager.ToolType);
        var bodyType = GetBodyTypeFromRoot();
        var ctx = new CutContext(root, limb, section, precision, p, normal, other.gameObject, toolType, bodyType);
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

    /// <summary>
    /// Spawn impact VFX at the hit point with proper rotation
    /// </summary>
    private void SpawnImpactVFX(Vector3 position, Vector3 normal)
    {
        // Get VFX prefabs - check local first, then fall back to shared from CutRouter
        GameObject[] vfxPrefabsToUse = GetVFXPrefabs();
        
        if (vfxPrefabsToUse == null || vfxPrefabsToUse.Length == 0)
        {
            return; // No VFX to spawn
        }

        // Select a random prefab if multiple are provided
        GameObject prefabToSpawn = vfxPrefabsToUse[UnityEngine.Random.Range(0, vfxPrefabsToUse.Length)];
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[CutZone] Impact VFX prefab is null in array for {name}", this);
            return;
        }

        // Calculate rotation: align VFX forward with the surface normal
        // For impact effects, we typically want the effect to face outward from the surface
        Quaternion rotation = Quaternion.LookRotation(normal);

        // Get VFX parent - check local first, then fall back to shared from CutRouter
        Transform parentToUse = GetVFXParent();
        GameObject vfxInstance = Instantiate(prefabToSpawn, position, rotation, parentToUse);

        if (debugLog)
        {
            Debug.Log($"[CutZone] Spawned impact VFX '{prefabToSpawn.name}' at {position} for {limb}/{section}/{precision}", this);
        }
    }

    /// <summary>
    /// Get VFX prefabs - check local first, then fall back to shared from CutRouter
    /// </summary>
    private GameObject[] GetVFXPrefabs()
    {
        // First check if this CutZone has its own VFX prefabs assigned
        if (impactVFXPrefabs != null && impactVFXPrefabs.Length > 0)
        {
            return impactVFXPrefabs;
        }

        // Fall back to shared VFX from CutRouter
        if (root != null)
        {
            CutRouter cutRouter = root.GetComponent<CutRouter>();
            if (cutRouter != null && cutRouter.sharedImpactVFXPrefabs != null && cutRouter.sharedImpactVFXPrefabs.Length > 0)
            {
                return cutRouter.sharedImpactVFXPrefabs;
            }
        }

        return null;
    }

    /// <summary>
    /// Get VFX parent - check local first, then fall back to shared from CutRouter
    /// </summary>
    private Transform GetVFXParent()
    {
        // First check if this CutZone has its own VFX parent assigned
        if (vfxParent != null)
        {
            return vfxParent;
        }

        // Fall back to shared VFX parent from CutRouter
        if (root != null)
        {
            CutRouter cutRouter = root.GetComponent<CutRouter>();
            if (cutRouter != null && cutRouter.sharedVFXParent != null)
            {
                return cutRouter.sharedVFXParent;
            }
        }

        return null; // No parent - spawn at root level
    }
}