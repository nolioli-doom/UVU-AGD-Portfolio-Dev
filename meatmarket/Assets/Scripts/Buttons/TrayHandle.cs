using UnityEngine;
using UnityEngine.Events;

public class TrayHandle : MonoBehaviour
{
    [Header("Tray Settings")]
    public Transform trayTransform;        // the tray we are moving
    public Vector3 inPosition;             // local position when tray is inside oven
    public Vector3 outPosition;            // local position when tray is pulled out
    public float moveSpeed = 5f;           // speed of movement

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
    }

    // call this from a button click or collider trigger
    public void ToggleTray()
    {
        Debug.Log($"[TrayHandle] ToggleTray called (currently {(IsInTray ? "IN" : "OUT")})");
        if (!isMoving)
        {
            if (IsInTray)
                StartCoroutine(MoveTray(outPosition, false));
            else
                StartCoroutine(MoveTray(inPosition, true));
        }
    }

    /// <summary>
    /// Force the tray to a specific state instantly (used by PurchaseUI for auto-pop-out/in).
    /// No movement animation, just snap to position.
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
        
        if (trayTransform != null)
        {
            trayTransform.localPosition = targetPos;
        }
        
        IsInTray = inState;

        // Fire appropriate events if state actually changed
        if (wasInTray != IsInTray)
        {
            if (IsInTray)
            {
                onTrayFullyIn?.Invoke();
            }
            else
            {
                onTrayFullyOut?.Invoke();
            }
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

        isMoving = false;
    }
}

