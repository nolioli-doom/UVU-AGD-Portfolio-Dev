using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class OvenController : MonoBehaviour
{
    [Header("State (read-only in play mode)")]
    [SerializeField] private bool isOn = false;
    public bool IsOn => isOn;
    public bool IsBurning { get; private set; }
    public bool IsCooling { get; private set; }
    [SerializeField] private bool isTrayIn = false;  // Track if tray is in position
    public bool IsTrayIn => isTrayIn;

    [Header("Timing (seconds)")]
    [Min(0.05f)] public float burnDuration = 2.0f;
    [Min(0.05f)] public float cooldownDuration = 3.0f;

    [Header("Auto-Detect Plushie")]
    [Tooltip("If true, whenever the tray finishes going IN, the oven will try to find a PlushieBehaviour under 'plushieSlot'.")]
    public bool autoRefreshPlushieOnTrayIn = true;
    [Tooltip("Where the plushie instance lives (parent transform). Used by auto-detect.")]
    public Transform plushieSlot;

    [Header("Handle Locking (optional)")]
    [Tooltip("Disable this collider while burning/cooling to lock the handle.")]
    public Collider handleColliderToDisable;
    [Tooltip("Disable this component (e.g., RaycastButton) while burning/cooling to lock the handle.")]
    public MonoBehaviour handleRaycastButton;

    [Header("Plushie (set/cleared automatically or by tray code)")]
    public PlushieBehaviour currentPlushie;

    [Header("Events: power")]
    public UnityEvent OnOvenTurnedOn;
    public UnityEvent OnOvenTurnedOff;

    [Header("Events: burn")]
    public UnityEvent OnBurnStart;
    public UnityEvent OnBurnCancelled;
    public UnityEvent OnBurnComplete;
    public UnityEvent<float> OnBurnProgress;       // 0..1

    [Header("Events: cooldown")]
    public UnityEvent OnCooldownStart;
    public UnityEvent<float> OnCooldownProgress;   // 0..1
    public UnityEvent OnCooldownEnd;

    [Header("Events: tray lock state (true=locked)")]
    public UnityEvent<bool> OnTrayLockStateChanged;

    private Coroutine burnCo;
    private Coroutine cooldownCo;

    void Start()
    {
        // Initialize tray state - assume tray starts IN by default
        // This will be updated when TrayHandle fires its events
        isTrayIn = true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────────

    public void ToggleOven()
    {
        if (isOn) TurnOff();
        else TurnOn();
    }

    public void TurnOn()
    {
        if (isOn) return;
        isOn = true;
        Debug.Log("[OvenController] Oven turned ON");
        OnOvenTurnedOn?.Invoke();
        // Only try to start burn if tray is in and conditions are met
        if (isTrayIn) TryStartBurnIfPossible();
    }

    public void TurnOff()
    {
        if (!isOn) return;
        isOn = false;
        Debug.Log("[OvenController] Oven turned OFF");
        OnOvenTurnedOff?.Invoke();

        // Turning off cancels any active burn and does NOT start cooldown.
        if (IsBurning) CancelBurn();
        // If cooling: we keep cooling by default. Uncomment to cancel cooldown when turning off:
        // if (IsCooling) CancelCooldown();
    }

    /// <summary>
    /// Wire TrayHandle.onTrayFullyIn → this.
    /// If autoRefresh is on, re-scan the slot for a plushie, then try to burn.
    /// </summary>
    public void OnTrayFullyIn()
    {
        isTrayIn = true;
        if (autoRefreshPlushieOnTrayIn)
            AutoFindPlushieOnSlot();

        TryStartBurnIfPossible();
    }

    /// <summary>
    /// Wire TrayHandle.onTrayFullyOut → this.
    /// Updates tray position state.
    /// </summary>
    public void OnTrayFullyOut()
    {
        isTrayIn = false;
    }

    /// <summary>
    /// Force a full reset used by the Buy flow:
    /// - Turn Off, cancel burn/cooldown, unlock handle immediately.
    /// </summary>
    public void ForceInstantReset()
    {
        Debug.Log("[OvenController] Force reset");
        isOn = false;
        if (IsBurning) CancelBurn();
        if (IsCooling) CancelCooldown();
        SetTrayLocked(false);
        OnOvenTurnedOff?.Invoke();
    }

    /// <summary>
    /// Manually set current plushie (optional if you're using auto-detect).
    /// </summary>
    public void SetCurrentPlushie(PlushieBehaviour plushie)
    {
        currentPlushie = plushie;
    }

    /// <summary>
    /// Finds a PlushieBehaviour under 'plushieSlot' (one level or deeper).
    /// </summary>
    public void AutoFindPlushieOnSlot()
    {
        currentPlushie = null;
        if (plushieSlot == null) return;

        // Look for first active PlushieBehaviour in children
        currentPlushie = plushieSlot.GetComponentInChildren<PlushieBehaviour>(includeInactive: false);
    }

    /// <summary>
    /// Manually set tray position state (used for initialization or debugging).
    /// </summary>
    public void SetTrayPosition(bool inPosition)
    {
        isTrayIn = inPosition;
        Debug.Log($"[OvenController] Tray position manually set to {(inPosition ? "IN" : "OUT")}");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core logic
    // ─────────────────────────────────────────────────────────────────────────────

    private void TryStartBurnIfPossible()
    {
        if (!isOn) return;
        if (!isTrayIn) return;  // Check if tray is in position
        if (IsBurning || IsCooling) return;
        if (currentPlushie == null) return;

        StartBurn();
    }

    private void StartBurn()
    {
        if (burnCo != null) StopCoroutine(burnCo);
        burnCo = StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        IsBurning = true;
        SetTrayLocked(true);
        Debug.Log("[OvenController] Burn started");
        OnBurnStart?.Invoke();

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, burnDuration);

        while (t < burnDuration)
        {
            t += Time.deltaTime;
            OnBurnProgress?.Invoke(Mathf.Clamp01(t * inv));
            yield return null;
        }

        // Complete burn
        if (currentPlushie != null)
        {
            currentPlushie.Burn(); // implement visuals/self-destroy on the plushie
            currentPlushie = null;
        }

        Debug.Log("[OvenController] Burn complete, plushie destroyed");
        OnBurnComplete?.Invoke();
        IsBurning = false;

        StartCooldown();
    }

    private void CancelBurn()
    {
        if (burnCo != null)
        {
            StopCoroutine(burnCo);
            burnCo = null;
        }
        IsBurning = false;
        OnBurnCancelled?.Invoke();
        SetTrayLocked(false);
    }

    private void StartCooldown()
    {
        if (cooldownCo != null) StopCoroutine(cooldownCo);
        cooldownCo = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        IsCooling = true;
        Debug.Log("[OvenController] Cooldown started");
        OnCooldownStart?.Invoke();

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, cooldownDuration);

        while (t < cooldownDuration)
        {
            t += Time.deltaTime;
            OnCooldownProgress?.Invoke(Mathf.Clamp01(t * inv));
            yield return null;
        }

        IsCooling = false;
        Debug.Log("[OvenController] Cooldown ended");
        OnCooldownEnd?.Invoke();
        SetTrayLocked(false);
    }

    private void CancelCooldown()
    {
        if (cooldownCo != null)
        {
            StopCoroutine(cooldownCo);
            cooldownCo = null;
        }
        IsCooling = false;
        OnCooldownEnd?.Invoke();
        SetTrayLocked(false);
    }

    private void SetTrayLocked(bool locked)
    {
        if (handleColliderToDisable != null) handleColliderToDisable.enabled = !locked;
        if (handleRaycastButton != null) handleRaycastButton.enabled = !locked;
        OnTrayLockStateChanged?.Invoke(locked);
    }
}