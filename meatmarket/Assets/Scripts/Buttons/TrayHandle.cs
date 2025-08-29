using UnityEngine;
using UnityEngine.Events;

public class TrayHandle : MonoBehaviour
{
    [Header("Tray Settings")]
    public Transform trayTransform;        // the tray we are moving
    public Vector3 inPosition;             // local position when tray is inside oven
    public Vector3 outPosition;            // local position when tray is pulled out
    public float moveSpeed = 5f;           // speed of movement

    [Header("Oven Controller Reference")]
    [Tooltip("Reference to OvenController to notify of tray position changes.")]
    public OvenController ovenController;

    [Header("Morgue Reference")]
    [Tooltip("Reference to MorgueController for mutex management.")]
    public MorgueController morgueController;

    [Header("Handle Button")]
    [Tooltip("Reference to the handle button component (RaycastButton) for enabling/disabling.")]
    public MonoBehaviour handleButton;

    [Header("Forced Movement")]
    [Tooltip("Speed for forced tray movements (auto-pop-out/in). Set to 0 for instant movement.")]
    public float forcedMoveSpeed = 8f;    // speed for forced movements (faster than manual)

    [Header("Events")]
    public UnityEvent onTrayStartMovingIn;
    public UnityEvent onTrayStartMovingOut;
    public UnityEvent onTrayFullyIn;
    public UnityEvent onTrayFullyOut;

    private bool isMoving = false;

    // 🔎 public getter for tray state
    public bool IsInTray { get; private set; } = true; // assume starts in

    void Start()
    {
        // Ensure tray starts in the correct position and state
        if (trayTransform != null)
        {
            trayTransform.localPosition = inPosition;
            IsInTray = true;
        }
        Debug.Log($"[TrayHandle] Tray initialized, starting position: {(IsInTray ? "IN" : "OUT")}");
        
        // Notify oven controller of initial tray position
        if (ovenController != null)
        {
            ovenController.OnTrayFullyIn();
        }
    }

    // call this from a button click or collider trigger
    public void ToggleTray()
    {
        Debug.Log($"[TrayHandle] ToggleTray called (currently {(IsInTray ? "IN" : "OUT")})");
        if (!isMoving)
        {
            if (IsInTray)
            {
                // Check mutex only if this is an occupied tray going OUT
                if (ShouldCheckMutexForOut())
                {
                    if (!RequestMutexPermission())
                    {
                        Debug.Log("[TrayHandle] Mutex denied OUT permission, ignoring click");
                        return;
                    }
                }
                StartCoroutine(MoveTray(outPosition, false));
            }
            else
            {
                StartCoroutine(MoveTray(inPosition, true));
            }
        }
    }

    /// <summary>
    /// Force the tray to a specific state with smooth movement (used by PurchaseUI for auto-pop-out/in).
    /// Uses forcedMoveSpeed for consistent animation.
    /// </summary>
    public void ForceSetTray(bool inState)
    {
        Debug.Log($"[TrayHandle] Force set to {(inState ? "IN" : "OUT")}");
        if (isMoving)
        {
            StopAllCoroutines();
            isMoving = false;
        }

        Vector3 targetPos = inState ? inPosition : outPosition;
        bool wasInTray = IsInTray;
        
        // If forcedMoveSpeed is 0, snap instantly (for debugging or special cases)
        if (forcedMoveSpeed <= 0f)
        {
            if (trayTransform != null)
            {
                trayTransform.localPosition = targetPos;
            }
            IsInTray = inState;
            FireTrayStateEvents(wasInTray, IsInTray);
        }
        else
        {
            // Use smooth movement with forcedMoveSpeed
            StartCoroutine(ForceMoveTray(targetPos, inState, wasInTray));
        }
    }

    private System.Collections.IEnumerator MoveTray(Vector3 target, bool goingIn)
    {
        isMoving = true;
        Debug.Log($"[TrayHandle] Moving {(goingIn ? "IN" : "OUT")}");

        // fire start events
        if (goingIn)
            onTrayStartMovingIn?.Invoke();
        else
            onTrayStartMovingOut?.Invoke();

        // Notify morgue of movement start (only for occupied trays)
        NotifyMorgueOfMovementStart(goingIn);

        Vector3 start = trayTransform.localPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            trayTransform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        trayTransform.localPosition = target;
        IsInTray = goingIn;

        Debug.Log($"[TrayHandle] Fully {(IsInTray ? "IN" : "OUT")}");

        // fire end events
        if (IsInTray)
            onTrayFullyIn?.Invoke();
        else
            onTrayFullyOut?.Invoke();

        // Notify morgue of movement completion (only for occupied trays)
        NotifyMorgueOfMovement(goingIn);

        isMoving = false;
    }

    /// <summary>
    /// Smooth movement for forced tray positioning.
    /// </summary>
    private System.Collections.IEnumerator ForceMoveTray(Vector3 target, bool goingIn, bool wasInTray)
    {
        isMoving = true;
        Debug.Log($"[TrayHandle] Force moving {(goingIn ? "IN" : "OUT")}");

        // Fire start events
        if (goingIn)
            onTrayStartMovingIn?.Invoke();
        else
            onTrayStartMovingOut?.Invoke();

        Vector3 start = trayTransform.localPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * forcedMoveSpeed;
            if (trayTransform != null)
            {
                trayTransform.localPosition = Vector3.Lerp(start, target, Mathf.Clamp01(t));
            }
            yield return null;
        }

        // Ensure final position is exact
        if (trayTransform != null)
        {
            trayTransform.localPosition = target;
        }
        
        IsInTray = goingIn;
        Debug.Log($"[TrayHandle] Force move complete, now {(IsInTray ? "IN" : "OUT")}");

        // Fire end events
        FireTrayStateEvents(wasInTray, IsInTray);

        // Notify morgue of movement completion (only for occupied trays)
        NotifyMorgueOfMovement(goingIn);

        isMoving = false;
    }

    /// <summary>
    /// Helper method to fire tray state change events and notify oven controller.
    /// </summary>
    private void FireTrayStateEvents(bool wasInTray, bool isNowInTray)
    {
        if (wasInTray != isNowInTray)
        {
            if (isNowInTray)
            {
                onTrayFullyIn?.Invoke();
                // Also notify oven controller directly
                if (ovenController != null)
                {
                    ovenController.OnTrayFullyIn();
                }
            }
            else
            {
                onTrayFullyOut?.Invoke();
                // Also notify oven controller directly
                if (ovenController != null)
                {
                    ovenController.OnTrayFullyOut();
                }
            }
        }
    }

    /// <summary>
    /// Notify morgue of movement start events (only for occupied trays).
    /// </summary>
    private void NotifyMorgueOfMovementStart(bool goingIn)
    {
        if (morgueController == null) return;

        var ctx = GetTrayContext();
        if (ctx == null || !ctx.IsOccupied) return;

        if (goingIn)
        {
            morgueController.NotifyStartMovingIn(ctx);
        }
        else
        {
            morgueController.NotifyStartMovingOut(ctx);
        }
    }

    /// <summary>
    /// Notify morgue of movement completion events (only for occupied trays).
    /// </summary>
    private void NotifyMorgueOfMovement(bool goingIn)
    {
        if (morgueController == null) return;

        var ctx = GetTrayContext();
        if (ctx == null || !ctx.IsOccupied) return;

        if (goingIn)
        {
            morgueController.NotifyFullyIn(ctx);
        }
        else
        {
            morgueController.NotifyFullyOut(ctx);
        }
    }

    /// <summary>
    /// Check if this tray should consult the mutex for OUT movement.
    /// Only occupied trays need mutex permission.
    /// </summary>
    private bool ShouldCheckMutexForOut()
    {
        if (morgueController == null) return false;
        
        // Find this tray's context in the morgue
        foreach (var ctx in morgueController.trays)
        {
            if (ctx.handle == this)
            {
                return ctx.IsOccupied;
            }
        }
        return false; // Default to checking if we can't determine state
    }

    /// <summary>
    /// Request mutex permission for OUT movement.
    /// </summary>
    private bool RequestMutexPermission()
    {
        if (morgueController == null) return true; // Allow if no morgue reference
        
        // Find this tray's context in the morgue
        foreach (var ctx in morgueController.trays)
        {
            if (ctx.handle == this)
            {
                return morgueController.RequestOccupiedOut(ctx);
            }
        }
        return true; // Default to allowing if we can't find context
    }

    /// <summary>
    /// Get this tray's context from the morgue.
    /// </summary>
    private MorgueController.TrayContext GetTrayContext()
    {
        if (morgueController == null) return null;
        
        foreach (var ctx in morgueController.trays)
        {
            if (ctx.handle == this)
            {
                return ctx;
            }
        }
        return null;
    }
}

