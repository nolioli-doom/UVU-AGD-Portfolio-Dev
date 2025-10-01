using UnityEngine;

public class RaycastClickManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject toolGO; // tool/cursor/blade GameObject
    [SerializeField] private float cutRayDistance = 100f;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

		// Default to this GameObject if no tool assigned
		if (toolGO == null)
		{
			toolGO = this.gameObject;
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

			// Also notify CutZones when clicked (specific layer)
			if (Physics.Raycast(ray, out RaycastHit cutHit, cutRayDistance, LayerMask.GetMask("CutZones")))
			{
				if (cutHit.collider.TryGetComponent(out CutZone zone))
				{
					zone.NotifyRaycastHit(cutHit, toolGO);
				}
			}
        }
    }
}