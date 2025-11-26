using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PurchaseUI : MonoBehaviour
{
    [Header("Core References (same tray prefab)")]
    [Tooltip("Tray handle that moves IN/OUT.")]
    public TrayHandle trayHandle;
    [Tooltip("Oven controller on this tray.")]
    public OvenController oven;
    [Tooltip("Shock controller on this tray (we don't block it while blank, except during respawn).")]
    public ShockController shock;
    [Tooltip("Morgue controller for mutex management.")]
    public MorgueController morgueController;

    [Header("Buy UI")]
    [Tooltip("Root transform for the physical buy buttons (child object).")]
    public Transform buyUiRoot;
    [Tooltip("Local position for hidden state (e.g., below the tray surface).")]
    public Vector3 buyUiHiddenLocalPos = new Vector3(0, -0.05f, 0);
    [Tooltip("Local position for visible state (e.g., centered on the tray surface).")]
    public Vector3 buyUiVisibleLocalPos = new Vector3(0, 0.02f, 0);
    [Tooltip("How fast to slide the UI between hidden/visible.")]
    public float buyUiSlideSpeed = 6f;

    [Header("Plushie Spawning")]
    [Tooltip("Parent slot where plushies are spawned for THIS tray.")]
    public Transform plushieSlot;
    [Tooltip("Prefabs available to buy (Cat, Dog, Bunny, etc.). Index maps to your buy buttons).")]
    public List<GameObject> plushiePrefabs = new List<GameObject>();

    [Header("Buttons to (de)activate")]
    [Tooltip("RaycastButton on the handle collider (optional, for disabling during respawn).")]
    public MonoBehaviour handleButton; // e.g., RaycastButton
    [Tooltip("RaycastButton on the oven toggle (WILL be disabled while blank & during respawn).")]
    public MonoBehaviour ovenButton;   // e.g., RaycastButton
    [Tooltip("RaycastButton on the shock button (only disabled during respawn).")]
    public MonoBehaviour shockButton;  // e.g., RaycastButton
    [Tooltip("All RaycastButtons that belong to this buy UI (enabled only when buy UI is visible).")]
    public List<MonoBehaviour> buyOptionButtons = new List<MonoBehaviour>();

    [Header("Respawn")]
    [Tooltip("How long the tray is locked after a buy selection; plushie is spawned during this period.")]
    public float respawnDuration = 1.5f;

    [Header("Events (for VFX/SFX wiring)")]
    public UnityEvent OnAutoPopOutStart;     // after successful incineration (when cooldown ends)
    public UnityEvent OnAutoPopOutComplete;
    public UnityEvent OnBuyUiShown;
    public UnityEvent OnBuyUiHidden;
    public UnityEvent OnBuySelected;         // when player picks an option
    public UnityEvent OnAutoPullInStart;     // when auto pull-in begins after buy
    public UnityEvent OnAutoPullInComplete;
    public UnityEvent OnRespawnStart;
    public UnityEvent<float> OnRespawnProgress; // 0..1
    public UnityEvent OnRespawnComplete;

    // ─────────────────────────────────────────────────────────────────────────────
    // Internal state
    // ─────────────────────────────────────────────────────────────────────────────
    private bool blankReadyActive = false;   // true once we enter blank state post-incineration
    private bool buyUiVisible = false;       // visual state (depends on tray IN/OUT)
    private bool isRespawning = false;

    private Coroutine uiSlideCo;
    private Coroutine respawnCo;

    void Awake()
    {
        // Ensure UI starts hidden
        if (buyUiRoot != null)
            buyUiRoot.localPosition = buyUiHiddenLocalPos;
        SetBuyOptionsInteractable(false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public hooks to wire in Inspector
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wire this to OvenController.OnCooldownEnd.
    /// We treat 'successful incineration' as: Burn finished + Cooldown ended → tray unlocks.
    /// At that moment: force oven OFF, auto-pop OUT, disable oven button, and activate persistent Buy UI.
    /// </summary>
    public void OnOvenCooldownEnded_SuccessfulIncineration()
    {
        if (isRespawning) return; // safety
        Debug.Log("[PurchaseUI] Burn+Cooldown complete → forcing oven OFF, entering BlankReady, auto pop out");
        
        // Notify morgue that this tray is now BlankReady (exempt from mutex)
        if (morgueController != null)
        {
            var ctx = GetTrayContext();
            if (ctx != null)
            {
                morgueController.NotifyBlankReadyEnter(ctx);
            }
        }
        
        // Force oven OFF before entering blank state (no oven while tray is blank)
        if (oven != null) oven.ForceInstantReset();
        
        EnterBlankReady();
        AutoPopOut();
    }

    /// <summary>
    /// Wire to TrayHandle.onTrayFullyOut — used to re-show persistent Buy UI when the player pulls it back out.
    /// </summary>
    public void OnTrayFullyOut()
    {
        if (!blankReadyActive || isRespawning) return;
        Debug.Log("[PurchaseUI] Tray out, showing Buy UI");
        ShowBuyUi();
    }

    /// <summary>
    /// Wire to TrayHandle.onTrayStartMovingIn or onTrayFullyIn — we hide while IN.
    /// </summary>
    public void OnTrayMovedInOrStartedIn()
    {
        if (!blankReadyActive || isRespawning) return;
        Debug.Log("[PurchaseUI] Tray in, hiding Buy UI (persistent)");
        HideBuyUi();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Buy selection entry points (hook these to your Buy buttons via UnityEvents)
    // Each button should call SelectBuyIndex(i) with its associated prefab index.
    // ─────────────────────────────────────────────────────────────────────────────

    public void SelectBuyIndex(int prefabIndex)
    {
        if (!blankReadyActive || isRespawning) return;
        if (prefabIndex < 0 || prefabIndex >= plushiePrefabs.Count) return;

        Debug.Log($"[PurchaseUI] Buy option {prefabIndex} selected");
        OnBuySelected?.Invoke();

        // Force oven reset before auto pull-in to ensure clean state
        if (oven != null) oven.ForceInstantReset();

        // Hide UI immediately and begin auto pull-in + respawn lock
        HideBuyUi();
        StartCoroutine(AutoPullIn_ThenRespawn(prefabIndex));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core blank-ready lifecycle
    // ─────────────────────────────────────────────────────────────────────────────

    private void EnterBlankReady()
    {
        blankReadyActive = true;

        // Disable oven button while blank (cannot turn oven on with empty tray)
        SetEnabled(ovenButton, false);

        // While blank-ready, handle & shock remain usable (only blocked during respawn)
        if (!isRespawning)
        {
            SetEnabled(handleButton, true);
            SetEnabled(shockButton, true);
        }
    }

    private void ExitBlankReady()
    {
        blankReadyActive = false;

        // Re-enable oven button once we have a plushie again (happens after respawn completes)
        SetEnabled(ovenButton, true);

        // Buy UI hidden
        HideBuyUi();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Auto OUT / IN sequences
    // ─────────────────────────────────────────────────────────────────────────────

    private void AutoPopOut()
    {
        // If already out, just ensure UI is shown.
        if (trayHandle != null && !trayHandle.IsInTray)
        {
            ShowBuyUi();
            return;
        }

        OnAutoPopOutStart?.Invoke();

        // If we're mid-move we could issue Toggle, but we have a clean snap with ForceSetTray(false).
        // If you prefer animation, swap to ToggleTray() and listen to onTrayFullyOut.
        if (trayHandle != null)
            trayHandle.ForceSetTray(false);

        OnAutoPopOutComplete?.Invoke();
        ShowBuyUi();
    }

    private IEnumerator AutoPullIn_ThenRespawn(int prefabIndex)
    {
        Debug.Log("[PurchaseUI] Auto pull-in started, respawn lock engaged");
        OnAutoPullInStart?.Invoke();

        // Notify morgue that respawn is starting
        if (morgueController != null)
        {
            var ctx = GetTrayContext();
            if (ctx != null)
            {
                morgueController.NotifyRespawnStart(ctx);
            }
        }

        // Block all inputs during respawn window
        BeginGlobalLock();

        // Pull IN (snap for reliability; swap to ToggleTray for animated movement if you prefer)
        if (trayHandle != null)
            trayHandle.ForceSetTray(true);

        OnAutoPullInComplete?.Invoke();

        // Start respawn
        OnRespawnStart?.Invoke();
        isRespawning = true;

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, respawnDuration);
        while (t < respawnDuration)
        {
            t += Time.deltaTime;
            OnRespawnProgress?.Invoke(Mathf.Clamp01(t * inv));
            yield return null;
        }

        // Spawn plushie during/at end of respawn (we choose end for simplicity)
        SpawnPlushie(prefabIndex);

        // Done: unlock, leave blank-ready, return to occupied
        isRespawning = false;
        EndGlobalLock();
        ExitBlankReady();
        
        // Notify morgue that respawn is complete and tray is now Occupied
        if (morgueController != null)
        {
            var ctx = GetTrayContext();
            if (ctx != null)
            {
                morgueController.NotifyRespawnComplete(ctx);
                morgueController.NotifyBlankReadyExit(ctx);
            }
        }
        
        Debug.Log("[PurchaseUI] Respawn complete, plushie spawned, returning to Occupied");
        OnRespawnComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Visual show/hide of Buy UI
    // ─────────────────────────────────────────────────────────────────────────────

    private void ShowBuyUi()
    {
        if (buyUiRoot == null) return;
        if (buyUiVisible) return;

        buyUiVisible = true;
        SlideUi(buyUiVisibleLocalPos);
        OnBuyUiShown?.Invoke();
        SetBuyOptionsInteractable(true);
    }

    private void HideBuyUi()
    {
        if (buyUiRoot == null) return;
        if (!buyUiVisible) return;

        buyUiVisible = false;
        SlideUi(buyUiHiddenLocalPos);
        OnBuyUiHidden?.Invoke();
        SetBuyOptionsInteractable(false);
    }

    private void SlideUi(Vector3 targetLocalPos)
    {
        if (uiSlideCo != null) StopCoroutine(uiSlideCo);
        uiSlideCo = StartCoroutine(SlideUiRoutine(targetLocalPos));
    }

    private IEnumerator SlideUiRoutine(Vector3 targetLocalPos)
    {
        Vector3 start = buyUiRoot.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * buyUiSlideSpeed;
            buyUiRoot.localPosition = Vector3.Lerp(start, targetLocalPos, t);
            yield return null;
        }
        buyUiRoot.localPosition = targetLocalPos;
    }

    private void SetBuyOptionsInteractable(bool enabled)
    {
        if (buyOptionButtons == null) return;
        foreach (var b in buyOptionButtons)
            SetEnabled(b, enabled);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Spawning
    // ─────────────────────────────────────────────────────────────────────────────

    private void SpawnPlushie(int prefabIndex)
    {
        if (plushieSlot == null || prefabIndex < 0 || prefabIndex >= plushiePrefabs.Count) return;

        // Safety: clear any lingering children
        for (int i = plushieSlot.childCount - 1; i >= 0; i--)
        {
            var child = plushieSlot.GetChild(i);
            Destroy(child.gameObject);
        }

        GameObject prefab = plushiePrefabs[prefabIndex];
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, plushieSlot);
        var plush = go.GetComponent<PlushieBehaviour>();
        if (plush != null)
        {
            plush.ResetState();
            // Let Oven/Shock auto-detect later if configured, but we can also set them immediately:
            if (oven != null) oven.SetCurrentPlushie(plush);
            if (shock != null) shock.SetCurrentPlushie(plush);
        }

        // Tray is now occupied again; Buy UI remains hidden until the next post-burn.
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Lock helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private void BeginGlobalLock()
    {
        // During respawn: ALL inputs disabled, including buy options, handle, oven, shock
        SetEnabled(handleButton, false);
        SetEnabled(ovenButton, false);
        SetEnabled(shockButton, false);
        SetBuyOptionsInteractable(false);
        
        // Lock the tray handle via OvenController to prevent pulling out the tray during plushie spawn
        if (oven != null)
        {
            oven.SetTrayLocked(true);
            Debug.Log("[PurchaseUI] Tray locked during respawn");
        }
    }

    private void EndGlobalLock()
    {
        // Back to normal occupied state; oven re-enabled in ExitBlankReady()
        SetEnabled(handleButton, true);
        SetEnabled(shockButton, true);
        // ovenButton gets re-enabled in ExitBlankReady()
        
        // Unlock the tray handle now that plushie has spawned
        if (oven != null)
        {
            oven.SetTrayLocked(false);
            Debug.Log("[PurchaseUI] Tray unlocked after respawn complete");
        }
    }

    private void SetEnabled(MonoBehaviour comp, bool enable)
    {
        if (comp != null) comp.enabled = enable;
    }

    /// <summary>
    /// Get this tray's context from the morgue controller.
    /// </summary>
    private MorgueController.TrayContext GetTrayContext()
    {
        if (morgueController == null) return null;
        
        foreach (var ctx in morgueController.trays)
        {
            if (ctx.purchaseUI == this)
            {
                return ctx;
            }
        }
        return null;
    }
}
