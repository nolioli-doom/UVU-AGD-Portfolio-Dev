using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlushieBehaviour : MonoBehaviour
{
    [Header("Status (read-only at runtime)")]
    public bool IsParalyzed { get; private set; }
    public bool IsBurned { get; private set; }

    [Header("Paralyze Settings")]
    [Tooltip("Default paralyze duration if none is provided by the caller.")]
    public float defaultParalyzeDuration = 2.0f;

    [Header("Burn Settings")]
    [Tooltip("If > 0, wait this long after Burn() before destroying the plushie.")]
    public float burnDestroyDelay = 0.25f;

    [Header("Optional Component Control")]
    [Tooltip("Components to disable while paralyzed (e.g., movement scripts, AI, NavMeshAgent).")]
    public Behaviour[] disableWhileParalyzed;
    [Tooltip("Colliders to disable after Burn() (prevents further interaction).")]
    public Collider[] collidersToDisableOnBurn;
    [Tooltip("Renderers you might want to toggle/replace on Burn() (optional).")]
    public Renderer[] renderersToggleOnBurn;

    [Header("Events")]
    public UnityEvent OnParalyzeStart;
    public UnityEvent OnParalyzeEnd;
    public UnityEvent OnBurned;       // fired when Burn() is called
    public UnityEvent OnDestroyed;    // fired right before destroy (or when deactivating)

    // internal
    private Coroutine paralyzeCo;

    /// <summary>
    /// Paralyzes the plushie for a given duration. If already paralyzed, timer is refreshed.
    /// </summary>
    public void Paralyze() => Paralyze(defaultParalyzeDuration);

    /// <summary>
    /// Paralyzes the plushie for 'duration' seconds.
    /// </summary>
    public void Paralyze(float duration)
    {
        if (IsBurned) return; // already burned/removed
        if (duration <= 0f) return;

        Debug.Log($"[PlushieBehaviour] Paralyzed for {duration} seconds");
        // refresh or start
        if (paralyzeCo != null) StopCoroutine(paralyzeCo);
        paralyzeCo = StartCoroutine(ParalyzeRoutine(duration));
    }

    private IEnumerator ParalyzeRoutine(float duration)
    {
        // enter
        IsParalyzed = true;
        SetParalyzedComponents(true);
        OnParalyzeStart?.Invoke();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // exit
        IsParalyzed = false;
        SetParalyzedComponents(false);
        Debug.Log("[PlushieBehaviour] Paralyze ended");
        OnParalyzeEnd?.Invoke();
        paralyzeCo = null;
    }

    /// <summary>
    /// Burns the plushie. Disables interaction, fires events, and destroys the object (after optional delay).
    /// Expected to be called by OvenController on burn completion.
    /// </summary>
    public void Burn()
    {
        if (IsBurned) return;

        Debug.Log("[PlushieBehaviour] Burned");
        // lock further interaction
        IsBurned = true;

        // stop any paralyze effect
        if (paralyzeCo != null)
        {
            StopCoroutine(paralyzeCo);
            paralyzeCo = null;
        }
        if (IsParalyzed)
        {
            IsParalyzed = false;
            SetParalyzedComponents(false);
            // We do not fire OnParalyzeEnd here to avoid double-VFX spamming; comment in if desired.
            // OnParalyzeEnd?.Invoke();
        }

        // disable colliders / interaction
        if (collidersToDisableOnBurn != null)
        {
            foreach (var c in collidersToDisableOnBurn)
                if (c) c.enabled = false;
        }

        // optional: toggle visuals (e.g., hide mesh before spawning ash via event)
        if (renderersToggleOnBurn != null)
        {
            foreach (var r in renderersToggleOnBurn)
                if (r) r.enabled = false;
        }

        OnBurned?.Invoke();

        if (burnDestroyDelay > 0f)
            StartCoroutine(DestroyAfterDelay(burnDestroyDelay));
        else
            DestroyNow();
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.deltaTime;
            yield return null;
        }
        DestroyNow();
    }

    private void DestroyNow()
    {
        Debug.Log("[PlushieBehaviour] Destroyed");
        OnDestroyed?.Invoke();
        // If you prefer pooling, replace with gameObject.SetActive(false) and clear state externally.
        Destroy(gameObject);
    }

    /// <summary>
    /// Resets all runtime flags and re-enables components/visuals.
    /// Call this right after instantiating or when reusing from a pool.
    /// </summary>
    public void ResetState()
    {
        Debug.Log("[PlushieBehaviour] Reset state");
        // stop any running paralyze
        if (paralyzeCo != null)
        {
            StopCoroutine(paralyzeCo);
            paralyzeCo = null;
        }

        IsParalyzed = false;
        IsBurned = false;

        SetParalyzedComponents(false);

        if (collidersToDisableOnBurn != null)
        {
            foreach (var c in collidersToDisableOnBurn)
                if (c) c.enabled = true;
        }

        if (renderersToggleOnBurn != null)
        {
            foreach (var r in renderersToggleOnBurn)
                if (r) r.enabled = true;
        }
    }

    private void SetParalyzedComponents(bool disable)
    {
        if (disableWhileParalyzed == null) return;
        foreach (var b in disableWhileParalyzed)
            if (b) b.enabled = !disable;
    }
}