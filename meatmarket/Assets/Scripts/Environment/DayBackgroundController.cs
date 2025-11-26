using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the background color of the Front Desk Camera based on the current day index.
/// Also updates a list of lights to match the background color.
/// The camera's clear flags should be set to "Solid Color" for this to work.
/// </summary>
public class DayBackgroundController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Front Desk Camera to change background color. If not assigned, will search by name.")]
    public Camera frontDeskCamera;
    
    [Tooltip("Name of the camera to find if not assigned (default: 'Front Desk Camera')")]
    public string cameraName = "Front Desk Camera";

    [Header("Day Index Reference")]
    [Tooltip("DayIndexSO that stores the current day index. If not assigned, will try to find it automatically.")]
    public DayIndexSO dayIndexSO;

    [Header("Day Colors")]
    [Tooltip("Array of background colors for each day index. Day 0 = index 0, Day 1 = index 1, etc.")]
    public Color[] dayColors = new Color[]
    {
        new Color(0.5f, 0.7f, 1f, 1f),      // Day 0: Light blue (morning)
        new Color(1f, 0.8f, 0.6f, 1f),      // Day 1: Warm orange (afternoon)
        new Color(0.2f, 0.2f, 0.4f, 1f),    // Day 2: Dark blue (evening)
        new Color(0.1f, 0.1f, 0.2f, 1f),    // Day 3: Very dark (night)
    };

    [Tooltip("Default color to use if day index is out of range")]
    public Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Lights")]
    [Tooltip("List of lights that will change color to match the background color for each day")]
    public Light[] lightsToUpdate;

    [Header("Settings")]
    [Tooltip("Update color on Start (when scene loads)")]
    public bool updateOnStart = true;

    [Tooltip("Update color when day index changes (requires checking in Update)")]
    public bool updateOnDayChange = false;

    [Header("Debug")]
    public bool logColorChanges = true;

    private int lastDayIndex = -1;

    void Awake()
    {
        // Auto-find camera if not assigned
        if (frontDeskCamera == null)
        {
            // Try to find by name first
            GameObject cameraObj = GameObject.Find(cameraName);
            if (cameraObj != null)
            {
                frontDeskCamera = cameraObj.GetComponent<Camera>();
            }
            
            // If still not found, try to find any camera with "Front Desk" in the name
            if (frontDeskCamera == null)
            {
                Camera[] allCameras = FindObjectsOfType<Camera>();
                frontDeskCamera = allCameras.FirstOrDefault(cam => cam.name.Contains("Front Desk"));
            }
            
            if (frontDeskCamera == null)
            {
                Debug.LogError($"[DayBackgroundController] Front Desk Camera not found! Please assign it in the Inspector or ensure a camera named '{cameraName}' exists.");
            }
        }

        // Auto-find DayIndexSO if not assigned
        if (dayIndexSO == null)
        {
            dayIndexSO = Resources.FindObjectsOfTypeAll<DayIndexSO>().FirstOrDefault();
            if (dayIndexSO == null)
            {
                Debug.LogWarning("[DayBackgroundController] DayIndexSO not found! Background color will not update. Please create one and assign it in the Inspector.");
            }
        }
    }

    void Start()
    {
        if (updateOnStart)
        {
            UpdateBackgroundColor();
        }
    }

    void Update()
    {
        if (updateOnDayChange && dayIndexSO != null)
        {
            int currentDayIndex = dayIndexSO.currentDayIndex;
            if (currentDayIndex != lastDayIndex)
            {
                UpdateBackgroundColor();
                lastDayIndex = currentDayIndex;
            }
        }
    }

    /// <summary>
    /// Update the camera's background color based on the current day index
    /// </summary>
    public void UpdateBackgroundColor()
    {
        if (frontDeskCamera == null)
        {
            if (logColorChanges)
            {
                Debug.LogWarning("[DayBackgroundController] Cannot update background color: Front Desk Camera is null.");
            }
            return;
        }

        if (dayIndexSO == null)
        {
            if (logColorChanges)
            {
                Debug.LogWarning("[DayBackgroundController] Cannot update background color: DayIndexSO is null.");
            }
            return;
        }

        int dayIndex = dayIndexSO.currentDayIndex;
        Color targetColor = GetColorForDay(dayIndex);

        // Set camera clear flags to solid color if not already set
        if (frontDeskCamera.clearFlags != CameraClearFlags.SolidColor)
        {
            if (logColorChanges)
            {
                Debug.Log($"[DayBackgroundController] Setting camera clear flags to SolidColor (was: {frontDeskCamera.clearFlags})");
            }
            frontDeskCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        // Update background color
        frontDeskCamera.backgroundColor = targetColor;

        // Update lights to match background color
        UpdateLightsColor(targetColor);

        if (logColorChanges)
        {
            Debug.Log($"[DayBackgroundController] Updated background color for Day {dayIndex} to {targetColor}");
        }
    }

    /// <summary>
    /// Update all lights in the list to match the target color
    /// </summary>
    private void UpdateLightsColor(Color targetColor)
    {
        if (lightsToUpdate == null || lightsToUpdate.Length == 0)
        {
            return;
        }

        int updatedCount = 0;
        foreach (Light light in lightsToUpdate)
        {
            if (light != null)
            {
                light.color = targetColor;
                updatedCount++;
            }
        }

        if (logColorChanges && updatedCount > 0)
        {
            Debug.Log($"[DayBackgroundController] Updated {updatedCount} light(s) to color {targetColor}");
        }
    }

    /// <summary>
    /// Get the color for a specific day index
    /// </summary>
    private Color GetColorForDay(int dayIndex)
    {
        if (dayIndex >= 0 && dayIndex < dayColors.Length)
        {
            return dayColors[dayIndex];
        }
        
        if (logColorChanges)
        {
            Debug.LogWarning($"[DayBackgroundController] Day index {dayIndex} is out of range (0-{dayColors.Length - 1}). Using default color.");
        }
        
        return defaultColor;
    }

    /// <summary>
    /// Manually set a color for a specific day index (useful for runtime customization)
    /// </summary>
    public void SetColorForDay(int dayIndex, Color color)
    {
        if (dayIndex >= 0)
        {
            // Expand array if needed
            if (dayIndex >= dayColors.Length)
            {
                System.Array.Resize(ref dayColors, dayIndex + 1);
            }
            
            dayColors[dayIndex] = color;
            
            // Update immediately if this is the current day
            if (dayIndexSO != null && dayIndexSO.currentDayIndex == dayIndex)
            {
                UpdateBackgroundColor();
            }
            
            if (logColorChanges)
            {
                Debug.Log($"[DayBackgroundController] Set color for Day {dayIndex} to {color}");
            }
        }
    }
}

