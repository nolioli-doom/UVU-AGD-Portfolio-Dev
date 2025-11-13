using UnityEngine;

/// <summary>
/// Provides programmatic control over Unity's ambient lighting settings.
/// Allows you to disable ambient light completely or set custom values.
/// </summary>
public class LightingController : MonoBehaviour
{
    [Header("Ambient Lighting Control")]
    [Tooltip("If true, ambient lighting will be disabled (set to black)")]
    public bool disableAmbientLight = false;
    
    [Tooltip("Custom ambient color (only used if disableAmbientLight is false)")]
    public Color ambientColor = Color.black;
    
    [Tooltip("Ambient intensity multiplier (0 = no ambient, 1 = full)")]
    [Range(0f, 1f)]
    public float ambientIntensity = 0f;
    
    [Header("Ambient Mode")]
    [Tooltip("0 = Skybox, 1 = Gradient, 2 = Color, 3 = Trilight")]
    public AmbientMode ambientMode = AmbientMode.Color;
    
    private void Start()
    {
        UpdateAmbientLighting();
    }
    
    private void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying)
        {
            UpdateAmbientLighting();
        }
    }
    
    /// <summary>
    /// Update Unity's RenderSettings ambient lighting based on current values
    /// </summary>
    public void UpdateAmbientLighting()
    {
        if (disableAmbientLight)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
        }
        else
        {
            RenderSettings.ambientMode = (UnityEngine.Rendering.AmbientMode)ambientMode;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = ambientIntensity;
        }
    }
    
    /// <summary>
    /// Completely disable ambient lighting (convenience method)
    /// </summary>
    public void DisableAmbientLight()
    {
        disableAmbientLight = true;
        UpdateAmbientLighting();
    }
    
    /// <summary>
    /// Enable ambient lighting with custom settings
    /// </summary>
    public void EnableAmbientLight(Color color, float intensity)
    {
        disableAmbientLight = false;
        ambientColor = color;
        ambientIntensity = intensity;
        UpdateAmbientLighting();
    }
}

/// <summary>
/// Enum matching Unity's AmbientMode for inspector dropdown
/// </summary>
public enum AmbientMode
{
    Skybox = 0,
    Gradient = 1,
    Color = 2,
    Trilight = 3
}

