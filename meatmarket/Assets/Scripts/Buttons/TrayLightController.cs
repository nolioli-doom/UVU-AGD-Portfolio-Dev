using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the color of a light above a tray based on the plushie species currently in the tray.
/// Listens to tray movement events and updates the light color when the tray is pulled out.
/// </summary>
public class TrayLightController : MonoBehaviour
{
    [Header("Light Reference")]
    [Tooltip("The Light component to control (auto-found if not assigned)")]
    public Light trayLight;

    [Header("Tray References")]
    [Tooltip("OvenController for this tray (to get current plushie)")]
    public OvenController ovenController;
    
    [Tooltip("TrayHandle for this tray (to listen to movement events)")]
    public TrayHandle trayHandle;

    [Header("Species Color Mapping")]
    [Tooltip("Light color when tray contains a Dog plushie")]
    public Color dogColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown
    
    [Tooltip("Light color when tray contains a Cat plushie")]
    public Color catColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    
    [Tooltip("Light color when tray contains a Bunny plushie")]
    public Color bunnyColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray
    
    [Tooltip("Default light color when tray is empty or species is unknown")]
    public Color defaultColor = Color.white;

    [Header("Animation Settings")]
    [Tooltip("If true, smoothly animate color changes. If false, change instantly.")]
    public bool animateColorChange = true;
    
    [Tooltip("Duration of color animation in seconds")]
    public float colorChangeDuration = 0.5f;

    [Header("Debug")]
    [Tooltip("Log color changes for debugging")]
    public bool logColorChanges = false;

    // Internal state
    private Color targetColor;
    private Color currentColor;
    private bool isAnimating = false;

    void Awake()
    {
        // Auto-find light if not assigned
        if (trayLight == null)
        {
            trayLight = GetComponent<Light>();
            if (trayLight == null)
            {
                trayLight = GetComponentInChildren<Light>();
            }
            if (trayLight == null && logColorChanges)
            {
                Debug.LogWarning("[TrayLightController] No Light component found! Please assign one in the Inspector.", this);
            }
        }

        // Auto-find OvenController if not assigned
        if (ovenController == null)
        {
            ovenController = GetComponentInParent<OvenController>();
            if (ovenController == null)
            {
                ovenController = FindObjectOfType<OvenController>();
            }
            if (ovenController == null && logColorChanges)
            {
                Debug.LogWarning("[TrayLightController] No OvenController found! Cannot detect plushie species.", this);
            }
        }

        // Auto-find TrayHandle if not assigned
        if (trayHandle == null)
        {
            trayHandle = GetComponentInParent<TrayHandle>();
            if (trayHandle == null)
            {
                trayHandle = FindObjectOfType<TrayHandle>();
            }
        }

        // Initialize color
        if (trayLight != null)
        {
            currentColor = trayLight.color;
            targetColor = currentColor;
        }
    }

    void Start()
    {
        // Subscribe to tray events
        if (trayHandle != null)
        {
            trayHandle.onTrayFullyOut.AddListener(OnTrayPulledOut);
            trayHandle.onTrayFullyIn.AddListener(OnTrayPushedIn);
        }
        else if (logColorChanges)
        {
            Debug.LogWarning("[TrayLightController] No TrayHandle found! Light color will not update automatically.", this);
        }

        // Set initial color based on current plushie (if tray is already out)
        if (trayHandle != null && !trayHandle.IsInTray)
        {
            UpdateLightColorForCurrentPlushie();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (trayHandle != null)
        {
            trayHandle.onTrayFullyOut.RemoveListener(OnTrayPulledOut);
            trayHandle.onTrayFullyIn.RemoveListener(OnTrayPushedIn);
        }
    }

    void Update()
    {
        // Animate color change if needed
        if (isAnimating && trayLight != null)
        {
            currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime / colorChangeDuration);
            trayLight.color = currentColor;

            // Check if animation is complete
            if (ColorDistance(currentColor, targetColor) < 0.01f)
            {
                currentColor = targetColor;
                trayLight.color = targetColor;
                isAnimating = false;
            }
        }
    }

    /// <summary>
    /// Called when the tray is fully pulled out.
    /// Updates the light color based on the current plushie species.
    /// </summary>
    private void OnTrayPulledOut()
    {
        UpdateLightColorForCurrentPlushie();
    }

    /// <summary>
    /// Called when the tray is fully pushed in.
    /// Optionally turn off light or set to default color.
    /// </summary>
    private void OnTrayPushedIn()
    {
        // You can optionally change the light when tray is in, or leave it as-is
        // For now, we'll leave it showing the last plushie color
    }

    /// <summary>
    /// Updates the light color based on the plushie currently in the tray.
    /// </summary>
    public void UpdateLightColorForCurrentPlushie()
    {
        if (trayLight == null) return;

        // Check if tray is empty (no plushie)
        if (ovenController == null || ovenController.currentPlushie == null)
        {
            SetLightColor(defaultColor);
            if (logColorChanges)
            {
                Debug.Log("[TrayLightController] Tray is empty, using default color");
            }
            return;
        }

        SpeciesType species = GetCurrentPlushieSpecies();
        Color newColor = GetColorForSpecies(species);

        SetLightColor(newColor);

        if (logColorChanges)
        {
            Debug.Log($"[TrayLightController] Updated light color to {species} color: {newColor}");
        }
    }

    /// <summary>
    /// Gets the species of the plushie currently in the tray.
    /// Assumes plushie is not null (caller should check first).
    /// Returns SpeciesType.Cat as fallback if species cannot be determined.
    /// </summary>
    private SpeciesType GetCurrentPlushieSpecies()
    {
        if (ovenController == null) return SpeciesType.Cat; // Fallback

        PlushieBehaviour plushie = ovenController.currentPlushie;
        if (plushie == null) return SpeciesType.Cat; // Fallback (shouldn't happen if called correctly)

        // Get BodyPartTree component from the plushie GameObject
        BodyPartTree bodyPartTree = plushie.GetComponent<BodyPartTree>();
        if (bodyPartTree == null)
        {
            // Try getting from children
            bodyPartTree = plushie.GetComponentInChildren<BodyPartTree>();
        }

        if (bodyPartTree != null)
        {
            return bodyPartTree.species;
        }

        if (logColorChanges)
        {
            Debug.LogWarning($"[TrayLightController] Plushie found but no BodyPartTree component! Using default color.", plushie);
        }

        return SpeciesType.Cat; // Fallback
    }

    /// <summary>
    /// Gets the color for a specific species type.
    /// </summary>
    private Color GetColorForSpecies(SpeciesType species)
    {
        switch (species)
        {
            case SpeciesType.Dog:
                return dogColor;
            case SpeciesType.Cat:
                return catColor;
            case SpeciesType.Bunny:
                return bunnyColor;
            default:
                if (logColorChanges)
                {
                    Debug.LogWarning($"[TrayLightController] Unknown species: {species}, using default color");
                }
                return defaultColor;
        }
    }

    /// <summary>
    /// Sets the light color (with optional animation).
    /// </summary>
    public void SetLightColor(Color color)
    {
        if (trayLight == null) return;

        targetColor = color;

        if (animateColorChange)
        {
            isAnimating = true;
        }
        else
        {
            currentColor = color;
            trayLight.color = color;
            isAnimating = false;
        }
    }

    /// <summary>
    /// Manually set the light color for a specific species.
    /// Useful for testing or external control.
    /// </summary>
    public void SetLightColorForSpecies(SpeciesType species)
    {
        Color color = GetColorForSpecies(species);
        SetLightColor(color);
    }

    /// <summary>
    /// Calculate color distance for animation completion check.
    /// </summary>
    private float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a);
    }

    /// <summary>
    /// Force immediate color update (useful when plushie is spawned/removed).
    /// Call this from PurchaseUI or other scripts when plushie changes.
    /// </summary>
    public void ForceUpdateColor()
    {
        UpdateLightColorForCurrentPlushie();
    }
}

