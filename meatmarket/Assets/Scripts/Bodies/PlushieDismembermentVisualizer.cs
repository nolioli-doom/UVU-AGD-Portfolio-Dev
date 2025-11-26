using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages visual dismemberment by disabling SkinnedMeshRenderer components
/// when body segments become fully isolated.
/// Attach to the same GameObject as BodyPartTree (plushie root).
/// </summary>
public class PlushieDismembermentVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class SegmentMeshMapping
    {
        [Tooltip("Body segment type (e.g., Head, L_Upper_Arm, R_Hand)")]
        public BodySegmentType segmentType;
        
        [Tooltip("SkinnedMeshRenderer for this segment. Drag the GameObject with the renderer here.")]
        public SkinnedMeshRenderer meshRenderer;
        
        [Tooltip("Optional: GameObject reference (auto-finds SkinnedMeshRenderer if meshRenderer is null)")]
        public GameObject segmentGameObject;
        
        /// <summary>
        /// Get the SkinnedMeshRenderer, either directly or from the GameObject
        /// </summary>
        public SkinnedMeshRenderer GetRenderer()
        {
            if (meshRenderer != null)
                return meshRenderer;
            
            if (segmentGameObject != null)
            {
                meshRenderer = segmentGameObject.GetComponent<SkinnedMeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = segmentGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
                }
            }
            
            return meshRenderer;
        }
    }

    [Header("Segment Mappings")]
    [Tooltip("Map each BodySegmentType to its SkinnedMeshRenderer(s). You can add multiple entries for the same segment type (e.g., Head + Ears + Head Fur). Drag GameObjects here.")]
    public List<SegmentMeshMapping> segmentMappings = new List<SegmentMeshMapping>();

    [Header("Debug")]
    [Tooltip("Log when meshes are disabled")]
    public bool logDismemberment = true;

    // Runtime lookup dictionary for fast access: maps segment type to list of renderers
    // This allows multiple renderers per segment (e.g., Head + Ears + Head Fur)
    private Dictionary<BodySegmentType, List<SkinnedMeshRenderer>> rendererLookup = new Dictionary<BodySegmentType, List<SkinnedMeshRenderer>>();
    
    // Track which segments have been disabled (for safety)
    private HashSet<BodySegmentType> disabledSegments = new HashSet<BodySegmentType>();

    void Awake()
    {
        BuildRendererLookup();
    }

    /// <summary>
    /// Build the runtime lookup dictionary from Inspector mappings
    /// Supports multiple renderers per segment type (e.g., Head + Ears + Head Fur)
    /// </summary>
    void BuildRendererLookup()
    {
        rendererLookup.Clear();
        disabledSegments.Clear();

        foreach (var mapping in segmentMappings)
        {
            SkinnedMeshRenderer renderer = mapping.GetRenderer();
            
            if (renderer == null)
            {
                Debug.LogWarning($"[PlushieDismembermentVisualizer] No SkinnedMeshRenderer found for {mapping.segmentType}. " +
                               $"Make sure to assign either meshRenderer or segmentGameObject.", this);
                continue;
            }

            // Add to list (allows multiple renderers per segment type)
            if (!rendererLookup.ContainsKey(mapping.segmentType))
            {
                rendererLookup[mapping.segmentType] = new List<SkinnedMeshRenderer>();
            }
            
            rendererLookup[mapping.segmentType].Add(renderer);
        }

        if (logDismemberment && rendererLookup.Count > 0)
        {
            int totalRenderers = rendererLookup.Values.Sum(list => list.Count);
            Debug.Log($"[PlushieDismembermentVisualizer] Built renderer lookup with {rendererLookup.Count} segment types ({totalRenderers} total renderers)");
        }
    }

    /// <summary>
    /// Called by BodyPartTree when a segment becomes fully isolated.
    /// Disables all corresponding mesh renderers (supports multiple renderers per segment).
    /// </summary>
    public void OnSegmentIsolated(BodySegmentType segmentType)
    {
        // Safety check: don't disable twice
        if (disabledSegments.Contains(segmentType))
        {
            if (logDismemberment)
            {
                Debug.LogWarning($"[PlushieDismembermentVisualizer] Segment {segmentType} already disabled, skipping.");
            }
            return;
        }

        if (!rendererLookup.TryGetValue(segmentType, out List<SkinnedMeshRenderer> renderers))
        {
            if (logDismemberment)
            {
                Debug.LogWarning($"[PlushieDismembermentVisualizer] No mesh renderer mapped for {segmentType}. " +
                               $"Add it to the segmentMappings list in the Inspector.", this);
            }
            return;
        }

        if (renderers == null || renderers.Count == 0)
        {
            Debug.LogWarning($"[PlushieDismembermentVisualizer] Renderer list for {segmentType} is empty! Cannot disable.", this);
            return;
        }

        // Disable all renderers for this segment type
        int disabledCount = 0;
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.enabled)
            {
                renderer.enabled = false;
                disabledCount++;
            }
        }

        disabledSegments.Add(segmentType);

        if (logDismemberment)
        {
            string rendererNames = string.Join(", ", renderers.Where(r => r != null).Select(r => r.gameObject.name));
            Debug.Log($"[PlushieDismembermentVisualizer] Disabled {disabledCount} mesh renderer(s) for {segmentType}: {rendererNames}");
        }
    }

    /// <summary>
    /// Reset all disabled segments (re-enable all meshes).
    /// Called when plushie is reset/respawned (though prefabs are fresh, this is for safety).
    /// </summary>
    public void ResetAllSegments()
    {
        int reenabledCount = 0;
        foreach (var kvp in rendererLookup)
        {
            if (kvp.Value != null)
            {
                foreach (var renderer in kvp.Value)
                {
                    if (renderer != null && !renderer.enabled)
                    {
                        renderer.enabled = true;
                        reenabledCount++;
                    }
                }
            }
        }
        
        disabledSegments.Clear();
        
        if (logDismemberment)
        {
            Debug.Log($"[PlushieDismembermentVisualizer] Reset all segments (re-enabled {reenabledCount} mesh renderer(s))");
        }
    }

    /// <summary>
    /// Validate configuration in Editor
    /// </summary>
    void OnValidate()
    {
        // Rebuild lookup when mappings change in Inspector
        if (Application.isPlaying)
        {
            BuildRendererLookup();
        }
    }
}

