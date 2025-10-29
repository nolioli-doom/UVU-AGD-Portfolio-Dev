using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Component for buttons that pin/unpin specific orders.
/// Attach to each order pin button in Customer Scene.
/// </summary>
public class OrderPinningButton : MonoBehaviour
{
    [Header("Configuration")]
    public OrderManagerSO orderManager;
    [Tooltip("Which order index this button controls (0-based)")]
    public int orderIndex;

    [Header("Display (Optional)")]
    public Text buttonLabel;
    public Image buttonBackground;
    public Color pinnedColor = Color.green;
    public Color unpinnedColor = Color.gray;
    public Color disabledColor = Color.red;

    [Header("Events")]
    public UnityEvent OnPinSuccess;
    public UnityEvent OnUnpin;
    public UnityEvent OnPinFailed;  // Max reached or invalid

    void Start()
    {
        UpdateVisuals();
    }

    /// <summary>
    /// Toggle pin state (wire to button OnClick)
    /// NOTE: This script is obsolete - use CustomerDetailUI instead
    /// </summary>
    public void TogglePin()
    {
        Debug.LogWarning("[OrderPinningButton] This script is obsolete. Use CustomerDetailUI instead.");
        // This component is no longer compatible with the new slot-based system
        // Orders must be pinned through the CustomerDetailUI interface
    }

    /// <summary>
    /// Update button visuals based on pin state
    /// NOTE: This script is obsolete
    /// </summary>
    public void UpdateVisuals()
    {
        // This component is no longer compatible with the new system
        if (buttonLabel != null)
        {
            buttonLabel.text = "OBSOLETE";
        }
        if (buttonBackground != null)
        {
            buttonBackground.color = disabledColor;
        }
    }
}

