using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ShockController : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.05f)] public float chargeTime = 1.25f;
    [Min(0f)] public float paralyzeDuration = 2.0f;
    [Tooltip("Optional delay after firing before another charge can start. Set 0 to disable cooldown.")]
    [Min(0f)] public float postFireCooldown = 0f;

    [Header("Targeting")]
    [Tooltip("If true, we will re-scan the tray's PlushieSlot right before firing the shock.")]
    public bool autoRefreshPlushieOnFire = true;
    [Tooltip("Parent transform where this tray spawns/holds its plushie.")]
    public Transform plushieSlot;
    [Tooltip("Current plushie reference (can be set by your tray/buy logic).")]
    public PlushieBehaviour currentPlushie;

    [Header("Events")]
    public UnityEvent OnChargeStart;
    public UnityEvent<float> OnChargeProgress;   // 0..1
    public UnityEvent OnChargeCancelled;
    public UnityEvent OnShockFired;              // fires even if no plushie
    public UnityEvent OnParalyzeApplied;         // fires only if a plushie was paralyzed
    public UnityEvent OnShockCooldownStart;      // only if postFireCooldown > 0
    public UnityEvent<float> OnShockCooldownProgress; // 0..1
    public UnityEvent OnShockCooldownEnd;        // only if postFireCooldown > 0

    [Header("State (read-only)")]
    public bool IsCharging { get; private set; }
    public bool IsInPostCooldown { get; private set; }

    private Coroutine chargeCo;
    private Coroutine cooldownCo;

    // ─────────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Begin the charge sequence (if not already charging or cooling down).
    /// Bind your button to call this.
    /// </summary>
    public void StartCharge()
    {
        if (IsCharging || IsInPostCooldown) return;

        Debug.Log("[ShockController] Shock charging started");
        if (chargeCo != null) StopCoroutine(chargeCo);
        chargeCo = StartCoroutine(ChargeRoutine());
    }

    /// <summary>
    /// Cancels an active charge (does nothing if not charging). No shock is fired.
    /// </summary>
    public void CancelCharge()
    {
        if (!IsCharging) return;

        Debug.Log("[ShockController] Charge cancelled");
        if (chargeCo != null)
        {
            StopCoroutine(chargeCo);
            chargeCo = null;
        }
        IsCharging = false;
        OnChargeCancelled?.Invoke();
    }

    /// <summary>
    /// Optional helper for your buy/reset flow: clears everything immediately.
    /// </summary>
    public void ForceInstantReset()
    {
        CancelCharge();
        if (IsInPostCooldown) CancelCooldown();
    }

    /// <summary>
    /// Manually set the current plushie (optional if you rely on auto-refresh).
    /// </summary>
    public void SetCurrentPlushie(PlushieBehaviour plushie)
    {
        currentPlushie = plushie;
    }

    /// <summary>
    /// Auto-find the plushie under our slot (one level or deeper).
    /// </summary>
    public void AutoFindPlushieOnSlot()
    {
        currentPlushie = null;
        if (plushieSlot != null)
            currentPlushie = plushieSlot.GetComponentInChildren<PlushieBehaviour>(includeInactive: false);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Internals
    // ─────────────────────────────────────────────────────────────────────────────

    private IEnumerator ChargeRoutine()
    {
        IsCharging = true;
        OnChargeStart?.Invoke();

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, chargeTime);

        while (t < chargeTime)
        {
            t += Time.deltaTime;
            OnChargeProgress?.Invoke(Mathf.Clamp01(t * inv));
            yield return null;
        }

        // Done charging
        IsCharging = false;
        chargeCo = null;

        // Optionally refresh the target at the exact moment of fire
        if (autoRefreshPlushieOnFire)
            AutoFindPlushieOnSlot();

        // Fire shock VFX/SFX
        Debug.Log("[ShockController] Shock fired");
        OnShockFired?.Invoke();

        // Apply paralyze if we have a valid plushie
        if (currentPlushie != null && paralyzeDuration > 0f)
        {
            currentPlushie.Paralyze(paralyzeDuration);
            Debug.Log($"[ShockController] Plushie paralyzed for {paralyzeDuration} seconds");
            OnParalyzeApplied?.Invoke();
        }
        else
        {
            Debug.Log("[ShockController] No plushie present, shock wasted");
        }

        // Optional cooldown
        if (postFireCooldown > 0f)
            StartCooldown();
    }

    private void StartCooldown()
    {
        if (cooldownCo != null) StopCoroutine(cooldownCo);
        cooldownCo = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        IsInPostCooldown = true;
        Debug.Log("[ShockController] Post-fire cooldown start");
        OnShockCooldownStart?.Invoke();

        float t = 0f;
        float inv = 1f / Mathf.Max(0.0001f, postFireCooldown);

        while (t < postFireCooldown)
        {
            t += Time.deltaTime;
            OnShockCooldownProgress?.Invoke(Mathf.Clamp01(t * inv));
            yield return null;
        }

        IsInPostCooldown = false;
        Debug.Log("[ShockController] Post-fire cooldown end");
        OnShockCooldownEnd?.Invoke();
        cooldownCo = null;
    }

    private void CancelCooldown()
    {
        if (cooldownCo != null)
        {
            StopCoroutine(cooldownCo);
            cooldownCo = null;
        }
        IsInPostCooldown = false;
        OnShockCooldownEnd?.Invoke();
    }
}