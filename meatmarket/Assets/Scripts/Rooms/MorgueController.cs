using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class MorgueController : MonoBehaviour
{
    [System.Serializable]
    public class TrayContext
    {
        [Header("Identity & Required Refs")]
        public string trayID;
        public TrayHandle handle;
        public OvenController oven;
        public ShockController shock;
        public PurchaseUI purchaseUI;
        public Transform plushieSlot;

        [Header("Startup Behaviour")]
        [Tooltip("If true, tray is forced IN at start. If false, it is forced OUT.")]
        public bool startTrayIn = true;

        [Tooltip("If false, any children under PlushieSlot will be destroyed at start before spawning.")]
        public bool respectExistingChildren = true;

        [Header("Starting Plushie (choose one)")]
        [Tooltip("Direct prefab to spawn at start (overrides index if set).")]
        public GameObject startingPrefab;

        [Tooltip("If no direct prefab is set, use this index into PurchaseUI.plushiePrefabs (0-based). -1 = none.")]
        public int startingPrefabIndex = -1;

        // ─────────────────────────────────────────────────────────────────────────────
        // Mutex State (runtime only)
        // ─────────────────────────────────────────────────────────────────────────────
        [System.NonSerialized] public TrayOccupancyState occupancyState = TrayOccupancyState.Occupied;
        
        /// <summary>
        /// True when tray has a plushie and is subject to the mutex.
        /// </summary>
        public bool IsOccupied => occupancyState == TrayOccupancyState.Occupied;
    }

    /// <summary>
    /// Tray occupancy states for mutex management.
    /// </summary>
    public enum TrayOccupancyState
    {
        Occupied,       // Has plushie, subject to mutex
        BlankReady,     // No plushie, Buy UI active, exempt from mutex
        Respawning      // No plushie, respawn in progress, exempt from mutex
    }

    /// <summary>
    /// Current state of the active occupied tray.
    /// </summary>
    public enum ActiveState
    {
        None,           // No occupied tray is active
        MovingOut,      // Occupied tray is moving OUT
        Out,            // Occupied tray is fully OUT
        MovingIn        // Occupied tray is moving back IN
    }

    [Header("Trays in this Morgue (order matters only for your own reference)")]
    public List<TrayContext> trays = new List<TrayContext>();

    [Header("Logging")]
    [Tooltip("Prints helpful logs during initialization and button actions.")]
    public bool verboseLogging = false;

    // ─────────────────────────────────────────────────────────────────────────────
    // Mutex State
    // ─────────────────────────────────────────────────────────────────────────────
    [Header("Mutex State (read-only)")]
    [SerializeField] private TrayContext activeOccupiedTray = null;
    
    // Debug property to track all changes to activeOccupiedTray
    private TrayContext _activeOccupiedTray;
    private TrayContext ActiveOccupiedTray
    {
        get => _activeOccupiedTray;
        set
        {
            if (verboseLogging && _activeOccupiedTray != value)
            {
                Debug.Log($"[MorgueController] activeOccupiedTray changed: {(value == null ? "null" : $"NOT NULL (trayID: '{value.trayID}')")} ← {(value == null ? "null" : $"NOT NULL (trayID: '{value.trayID}')")}");
            }
            _activeOccupiedTray = value;
        }
    }
    [SerializeField] private ActiveState activeState = ActiveState.None;

    // ─────────────────────────────────────────────────────────────────────────────
    // Runtime entry
    // ─────────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        InitializeAllAtRuntime();
    }

    public void InitializeAllAtRuntime()
    {
        // Force reset corrupted mutex state to ensure clean startup
        ActiveOccupiedTray = null;
        activeState = ActiveState.None;
        
        if (verboseLogging) Debug.Log($"[MorgueController] Mutex state reset - activeOccupiedTray: {(ActiveOccupiedTray == null ? "null" : $"NOT NULL (trayID: '{ActiveOccupiedTray.trayID}')")}");
        
        foreach (var ctx in trays)
        {
            if (!Validate(ctx)) continue;

            // Ensure oven is off and not in any locked state
            ctx.oven.ForceInstantReset();

            // Respect or clear existing
            if (!ctx.respectExistingChildren)
                ClearSlotChildren(ctx.plushieSlot);

            // Decide starting prefab
            var prefab = ResolveStartingPrefab(ctx);
            if (prefab != null)
            {
                var plush = SpawnPlushie(prefab, ctx.plushieSlot, ctx.trayID);
                if (plush != null)
                {
                    // Immediately register with oven/shock so they know we're occupied
                    ctx.oven.SetCurrentPlushie(plush);
                    ctx.shock.SetCurrentPlushie(plush);
                    ctx.occupancyState = TrayOccupancyState.Occupied;
                }
            }
            else
            {
                // Start blank
                ctx.occupancyState = TrayOccupancyState.BlankReady;
            }

            if (verboseLogging) Debug.Log($"[MorgueController] Init complete for {ctx.trayID}, state: {ctx.occupancyState}, startTrayIn: {ctx.startTrayIn}");

            // Check if this tray starts OUT and is Occupied (affects mutex)
            if (!ctx.startTrayIn && ctx.IsOccupied)
            {
                if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} starts OUT and Occupied - checking mutex");
                if (ActiveOccupiedTray == null)
                {
                    ActiveOccupiedTray = ctx;
                    activeState = ActiveState.Out;
                    UpdateHandleButtonStates();
                    if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} starts OUT and becomes active occupied tray");
                }
                else
                {
                    Debug.LogWarning($"[MorgueController] {ctx.trayID} starts OUT and Occupied, but {ActiveOccupiedTray.trayID} is already active. This tray will be blocked until the active one returns.");
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Mutex API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Request permission for an occupied tray to go OUT.
    /// Returns true if granted, false if denied.
    /// </summary>
    public bool RequestOccupiedOut(TrayContext ctx)
    {
        if (ctx == null || !ctx.IsOccupied)
        {
            Debug.LogWarning($"[MorgueController] RequestOccupiedOut called on null or non-occupied tray");
            return false;
        }

        if (verboseLogging) Debug.Log($"[MorgueController] RequestOccupiedOut for {ctx.trayID} - activeOccupiedTray: {(ActiveOccupiedTray == null ? "null" : $"NOT NULL (trayID: '{ActiveOccupiedTray.trayID}')")}");

        if (ActiveOccupiedTray == null)
        {
            // Grant immediately
            ActiveOccupiedTray = ctx;
            activeState = ActiveState.None; // Will be set to MovingOut when movement starts
            UpdateHandleButtonStates();
            if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} granted OUT permission immediately");
            return true;
        }
        else
        {
            // Deny - player must manually put active tray back first
            if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} denied OUT permission. Active: {ActiveOccupiedTray.trayID}. Put active tray back first.");
            return false;
        }
    }

    /// <summary>
    /// Called when a tray becomes blank and Buy UI is activated.
    /// </summary>
    public void NotifyBlankReadyEnter(TrayContext ctx)
    {
        if (ctx == null) return;

        var oldState = ctx.occupancyState;
        ctx.occupancyState = TrayOccupancyState.BlankReady;
        
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} state: {oldState} → BlankReady (exempt from mutex)");

        // If this tray was the active occupied tray, clear it
        if (ctx == ActiveOccupiedTray)
        {
            ActiveOccupiedTray = null;
            activeState = ActiveState.None;
            UpdateHandleButtonStates();
        }
    }

    /// <summary>
    /// Called when a tray leaves blank state (respawn complete).
    /// </summary>
    public void NotifyBlankReadyExit(TrayContext ctx)
    {
        if (ctx == null) return;

        var oldState = ctx.occupancyState;
        ctx.occupancyState = TrayOccupancyState.Occupied;
        
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} state: {oldState} → Occupied (now subject to mutex)");

        // This tray is now subject to the mutex again
        UpdateHandleButtonStates();
    }

    /// <summary>
    /// Called when respawn starts.
    /// </summary>
    public void NotifyRespawnStart(TrayContext ctx)
    {
        if (ctx == null) return;

        var oldState = ctx.occupancyState;
        ctx.occupancyState = TrayOccupancyState.Respawning;
        
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} state: {oldState} → Respawning (exempt from mutex)");

        // If this tray was the active occupied tray, clear it
        if (ctx == ActiveOccupiedTray)
        {
            ActiveOccupiedTray = null;
            activeState = ActiveState.None;
            UpdateHandleButtonStates();
        }
    }

    /// <summary>
    /// Called when respawn completes.
    /// </summary>
    public void NotifyRespawnComplete(TrayContext ctx)
    {
        if (ctx == null) return;

        var oldState = ctx.occupancyState;
        ctx.occupancyState = TrayOccupancyState.Occupied;
        
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} state: {oldState} → Occupied (now subject to mutex)");

        // This tray is now subject to the mutex again
        UpdateHandleButtonStates();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Movement Notifications
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when an occupied tray starts moving OUT.
    /// </summary>
    public void NotifyStartMovingOut(TrayContext ctx)
    {
        if (ctx == null || !ctx.IsOccupied || ctx != ActiveOccupiedTray) return;

        activeState = ActiveState.MovingOut;
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} started moving OUT");
    }

    /// <summary>
    /// Called when an occupied tray is fully OUT.
    /// </summary>
    public void NotifyFullyOut(TrayContext ctx)
    {
        if (ctx == null || !ctx.IsOccupied || ctx != ActiveOccupiedTray) return;

        activeState = ActiveState.Out;
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} is fully OUT");
    }

    /// <summary>
    /// Called when an occupied tray starts moving IN.
    /// </summary>
    public void NotifyStartMovingIn(TrayContext ctx)
    {
        if (ctx == null || !ctx.IsOccupied || ctx != ActiveOccupiedTray) return;

        activeState = ActiveState.MovingIn;
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} started moving IN");
    }

    /// <summary>
    /// Called when an occupied tray is fully IN.
    /// </summary>
    public void NotifyFullyIn(TrayContext ctx)
    {
        if (ctx == null || !ctx.IsOccupied || ctx != ActiveOccupiedTray) return;

        // Clear active occupied tray
        ActiveOccupiedTray = null;
        activeState = ActiveState.None;
        
        if (verboseLogging) Debug.Log($"[MorgueController] {ctx.trayID} returned IN, mutex cleared");

        // Re-enable handle buttons on other occupied trays
        UpdateHandleButtonStates();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Private Mutex Logic
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Update handle button states based on current mutex state.
    /// </summary>
    private void UpdateHandleButtonStates()
    {
        foreach (var ctx in trays)
        {
            if (ctx.handle == null) continue;

            bool shouldEnable = true;

            // If there's an active occupied tray, disable handles on other occupied trays
            if (ActiveOccupiedTray != null && ctx.IsOccupied && ctx != ActiveOccupiedTray)
            {
                shouldEnable = false;
            }

            // Enable/disable the handle button
            if (ctx.handle.handleButton != null)
            {
                ctx.handle.handleButton.enabled = shouldEnable;
            }

            if (verboseLogging && ctx.IsOccupied)
            {
                Debug.Log($"[MorgueController] {ctx.trayID} handle button: {(shouldEnable ? "ENABLED" : "DISABLED")}");
            }
        }
    }



    // ─────────────────────────────────────────────────────────────────────────────
    // Public bulk actions
    // ─────────────────────────────────────────────────────────────────────────────
    public void RestockAll()
    {
        foreach (var ctx in trays)
        {
            if (!Validate(ctx)) continue;

            ctx.oven.ForceInstantReset();
            ClearSlotChildren(ctx.plushieSlot);

            var prefab = ResolveStartingPrefab(ctx);
            if (prefab != null)
            {
                var plush = SpawnPlushie(prefab, ctx.plushieSlot, ctx.trayID);
                if (plush != null)
                {
                    ctx.oven.SetCurrentPlushie(plush);
                    ctx.shock.SetCurrentPlushie(plush);
                    ctx.occupancyState = TrayOccupancyState.Occupied;
                }
            }
            else
            {
                ctx.occupancyState = TrayOccupancyState.BlankReady;
            }

            if (verboseLogging) Debug.Log($"[MorgueController] Restocked {ctx.trayID}, state: {ctx.occupancyState}");
        }
    }

    public void ResetAll()
    {
        foreach (var ctx in trays)
        {
            if (!Validate(ctx)) continue;

            // Hard reset ovens/locks; leave current plushies as-is
            ctx.oven.ForceInstantReset();

            // Respect current tray pose; use this if you want a uniform pose instead:
            // ctx.handle.ForceSetTray(true); // force IN all trays

            if (verboseLogging) Debug.Log($"[MorgueController] Reset {ctx.trayID}");
        }
    }

    public void ClearAllSlots()
    {
        foreach (var ctx in trays)
        {
            if (!Validate(ctx)) continue;
            ClearSlotChildren(ctx.plushieSlot);
            // Clear oven/shock awareness since tray is now blank
            ctx.oven.SetCurrentPlushie(null);
            ctx.shock.SetCurrentPlushie(null);
            ctx.occupancyState = TrayOccupancyState.BlankReady; // Ensure it's blank ready

            if (verboseLogging) Debug.Log($"[MorgueController] Cleared slot for {ctx.trayID}, state: {ctx.occupancyState}");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────
    private bool Validate(TrayContext ctx)
    {
        bool ok = true;
        if (ctx.handle == null) { Debug.LogWarning($"[MorgueController] {ctx.trayID}: Missing TrayHandle"); ok = false; }
        if (ctx.oven == null)   { Debug.LogWarning($"[MorgueController] {ctx.trayID}: Missing OvenController"); ok = false; }
        if (ctx.shock == null)  { Debug.LogWarning($"[MorgueController] {ctx.trayID}: Missing ShockController"); ok = false; }
        if (ctx.plushieSlot == null) { Debug.LogWarning($"[MorgueController] {ctx.trayID}: Missing PlushieSlot"); ok = false; }
        return ok;
    }

    private GameObject ResolveStartingPrefab(TrayContext ctx)
    {
        if (ctx.startingPrefab != null) return ctx.startingPrefab;

        if (ctx.startingPrefabIndex >= 0 && ctx.purchaseUI != null)
        {
            var list = ctx.purchaseUI.plushiePrefabs;
            if (list != null && ctx.startingPrefabIndex < list.Count)
                return list[ctx.startingPrefabIndex];
        }
        return null; // start blank
    }

    private PlushieBehaviour SpawnPlushie(GameObject prefab, Transform slot, string trayID)
    {
        if (prefab == null || slot == null) return null;

        var go = Instantiate(prefab, slot);

        // reset local transform but preserve prefab's original rotation
        var t = go.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = prefab.transform.rotation; // preserve prefab's rotation
        t.localScale = Vector3.one;

        var plush = go.GetComponent<PlushieBehaviour>();
        if (plush != null)
        {
            plush.ResetState();
            if (verboseLogging) Debug.Log($"[MorgueController] Spawned plushie '{prefab.name}' on {trayID}");
        }
        else
        {
            Debug.LogWarning($"[MorgueController] Prefab '{prefab.name}' has no PlushieBehaviour. ({trayID})");
        }
        return plush;
    }

    private void ClearSlotChildren(Transform slot)
    {
        if (slot == null) return;
        for (int i = slot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(slot.GetChild(i).gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Editor convenience (ContextMenu calls also appear in the component menu)
    // ─────────────────────────────────────────────────────────────────────────────
    [ContextMenu("Initialize All Now")]
    private void _Ctx_InitializeAllNow() => InitializeAllAtRuntime();

    [ContextMenu("Restock All (Clear & Spawn Starting)")]
    private void _Ctx_RestockAll() => RestockAll();

    [ContextMenu("Reset All (Force Off & Unlock)")]
    private void _Ctx_ResetAll() => ResetAll();

    [ContextMenu("Clear All Slots (No Spawn)")]
    private void _Ctx_ClearAllSlots() => ClearAllSlots();
}

#if UNITY_EDITOR
[CustomEditor(typeof(MorgueController))]
public class MorgueControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var mc = (MorgueController)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Morgue Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Initialize All Now"))
                mc.InitializeAllAtRuntime();

            if (GUILayout.Button("Restock All"))
                mc.RestockAll();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset All"))
                mc.ResetAll();

            if (GUILayout.Button("Clear All Slots"))
                mc.ClearAllSlots();
        }
    }
}
#endif