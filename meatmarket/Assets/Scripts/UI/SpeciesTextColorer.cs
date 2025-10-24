using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Handles coloring of TextMeshPro components based on species types.
/// Drag TextMeshPro components into inspector slots and set colors for each species.
/// </summary>
public class SpeciesTextColorer : MonoBehaviour
{
    [Header("TextMeshPro References")]
    [Tooltip("Drag TextMeshPro components that should be colored based on species")]
    public TextMeshProUGUI textComponent1;
    public TextMeshProUGUI textComponent2;
    public TextMeshProUGUI textComponent3;
    public TextMeshProUGUI textComponent4;
    public TextMeshProUGUI textComponent5;

    [Header("Species Colors")]
    [Tooltip("Color for Dog species parts")]
    public Color dogColor = new Color(0.6f, 0.4f, 0.2f, 1f); // Brown
    
    [Tooltip("Color for Cat species parts")]
    public Color catColor = new Color(1f, 0.5f, 0f, 1f); // Orange
    
    [Tooltip("Color for Bunny species parts")]
    public Color bunnyColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray

    [Header("Default Color")]
    [Tooltip("Color used for unknown species or fallback")]
    public Color defaultColor = Color.white;

    [Header("Debug")]
    public bool logColorChanges = false;

    /// <summary>
    /// Gets the color for a specific species type.
    /// </summary>
    public Color GetSpeciesColor(SpeciesType species)
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
                if (logColorChanges) Debug.LogWarning($"[SpeciesTextColorer] Unknown species: {species}, using default color");
                return defaultColor;
        }
    }

    /// <summary>
    /// Sets the color of all assigned TextMeshPro components to the specified species color.
    /// </summary>
    public void SetTextColor(SpeciesType species)
    {
        Color color = GetSpeciesColor(species);
        SetTextColor(color);
        
        if (logColorChanges) Debug.Log($"[SpeciesTextColorer] Set text color to {species} color: {color}");
    }

    /// <summary>
    /// Sets the color of all assigned TextMeshPro components to the specified color.
    /// </summary>
    public void SetTextColor(Color color)
    {
        if (textComponent1 != null) textComponent1.color = color;
        if (textComponent2 != null) textComponent2.color = color;
        if (textComponent3 != null) textComponent3.color = color;
        if (textComponent4 != null) textComponent4.color = color;
        if (textComponent5 != null) textComponent5.color = color;
    }

    /// <summary>
    /// Updates text content and applies species-based coloring.
    /// Useful for displaying parts with automatic color coding.
    /// </summary>
    public void UpdateTextWithSpecies(string text, SpeciesType species)
    {
        Color color = GetSpeciesColor(species);
        
        if (textComponent1 != null) { textComponent1.text = text; textComponent1.color = color; }
        if (textComponent2 != null) { textComponent2.text = text; textComponent2.color = color; }
        if (textComponent3 != null) { textComponent3.text = text; textComponent3.color = color; }
        if (textComponent4 != null) { textComponent4.text = text; textComponent4.color = color; }
        if (textComponent5 != null) { textComponent5.text = text; textComponent5.color = color; }
        
        if (logColorChanges) Debug.Log($"[SpeciesTextColorer] Updated text to '{text}' with {species} color: {color}");
    }

    /// <summary>
    /// Updates text content with multiple species, coloring each part appropriately.
    /// Format: "2× Dog Hand, 1× Cat Foot" - each species gets its own color.
    /// </summary>
    public void UpdateTextWithMultipleSpecies(List<(string text, SpeciesType species)> parts)
    {
        if (parts == null || parts.Count == 0)
        {
            SetTextColor(defaultColor);
            return;
        }

        StringBuilder sb = new StringBuilder();
        
        foreach (var (text, species) in parts)
        {
            Color color = GetSpeciesColor(species);
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            sb.Append($"<color=#{colorHex}>{text}</color>");
            
            // Add separator if not the last item
            if (parts.IndexOf((text, species)) < parts.Count - 1)
            {
                sb.Append(", ");
            }
        }
        
        string finalText = sb.ToString();
        
        if (textComponent1 != null) { textComponent1.text = finalText; textComponent1.color = Color.white; }
        if (textComponent2 != null) { textComponent2.text = finalText; textComponent2.color = Color.white; }
        if (textComponent3 != null) { textComponent3.text = finalText; textComponent3.color = Color.white; }
        if (textComponent4 != null) { textComponent4.text = finalText; textComponent4.color = Color.white; }
        if (textComponent5 != null) { textComponent5.text = finalText; textComponent5.color = Color.white; }
        
        if (logColorChanges) Debug.Log($"[SpeciesTextColorer] Updated text with {parts.Count} colored parts");
    }

    /// <summary>
    /// Resets all text components to default color.
    /// </summary>
    public void ResetToDefaultColor()
    {
        SetTextColor(defaultColor);
        if (logColorChanges) Debug.Log("[SpeciesTextColorer] Reset to default color");
    }

    /// <summary>
    /// Test method to cycle through species colors (useful for testing in inspector).
    /// </summary>
    [ContextMenu("Test Species Colors")]
    public void TestSpeciesColors()
    {
        Debug.Log("[SpeciesTextColorer] Testing species colors...");
        
        // Test each component individually
        Debug.Log($"Text Component 1: {(textComponent1 != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"Text Component 2: {(textComponent2 != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"Text Component 3: {(textComponent3 != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"Text Component 4: {(textComponent4 != null ? "ASSIGNED" : "NULL")}");
        Debug.Log($"Text Component 5: {(textComponent5 != null ? "ASSIGNED" : "NULL")}");
        
        if (textComponent1 != null)
        {
            Debug.Log($"Text Component 1 text: '{textComponent1.text}'");
            Debug.Log($"Text Component 1 color: {textComponent1.color}");
        }
        
        // Test setting color directly with more aggressive approach
        if (textComponent1 != null)
        {
            textComponent1.color = Color.red;
            textComponent1.text = "MANUAL RED TEST";
            Debug.Log("MANUALLY set to RED with text 'MANUAL RED TEST' - check if visible");
            
            // Force update the text component
            textComponent1.SetAllDirty();
            Debug.Log($"After SetAllDirty - Color: {textComponent1.color}, Text: '{textComponent1.text}'");
        }
        
        // Wait a moment, then test next
        StartCoroutine(TestColorSequence());
    }
    
    private System.Collections.IEnumerator TestColorSequence()
    {
        yield return new WaitForSeconds(1f);
        SetTextColor(Color.green);
        Debug.Log("Set to GREEN - check if text changed color");
        
        yield return new WaitForSeconds(1f);
        SetTextColor(Color.blue);
        Debug.Log("Set to BLUE - check if text changed color");
        
        yield return new WaitForSeconds(1f);
        SetTextColor(Color.white);
        Debug.Log("Reset to WHITE");
    }

    /// <summary>
    /// Validates that all assigned TextMeshPro components are valid.
    /// </summary>
    void OnValidate()
    {
        int validComponents = 0;
        if (textComponent1 != null) validComponents++;
        if (textComponent2 != null) validComponents++;
        if (textComponent3 != null) validComponents++;
        if (textComponent4 != null) validComponents++;
        if (textComponent5 != null) validComponents++;
        
        // Log validation results
        if (logColorChanges)
        {
            Debug.Log($"[SpeciesTextColorer] Validated {validComponents} TextMeshPro components");
        }
    }
}
