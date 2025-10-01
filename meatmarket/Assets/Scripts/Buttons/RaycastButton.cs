using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class RaycastButton : MonoBehaviour
{
    [Tooltip("Called when this button is clicked with a raycast")]
    public UnityEvent onClick;

    // Optional: name or ID for debugging or grouping
    public string buttonID;
    
    // Called by the manager when this button is hit
    public void TriggerClick()
    {
        onClick?.Invoke();
    }
}