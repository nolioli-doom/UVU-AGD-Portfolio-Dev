using UnityEngine;

public class RaycastClickManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left mouse click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RaycastButton button = hit.collider.GetComponent<RaycastButton>();
                if (button != null)
                {
                    button.TriggerClick();
                }
            }
        }
    }
}