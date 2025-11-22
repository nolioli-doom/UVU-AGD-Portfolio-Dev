using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual representation system for the deposit tray.
/// Spawns sprite GameObjects when limbs are added to the tray,
/// and removes them when limbs are allocated or trashed.
/// Each TrayPiece gets its own visual instance.
/// </summary>
public class DepositTrayVisualizer : MonoBehaviour
{
    [Header("Sprite Mappings")]
    [Tooltip("Map each species+partType combination to a sprite prefab. Drag prefabs here.")]
    public List<LimbSpriteMapping> spriteMappings = new List<LimbSpriteMapping>();

    [Header("Spawn Area")]
    [Tooltip("Transform reference for the center of the deposit area (world space)")]
    public Transform spawnAreaCenter;
    
    [Tooltip("Size of the spawn area (width, height, depth)")]
    public Vector3 spawnAreaSize = new Vector3(5f, 2f, 5f);
    
    [Tooltip("Y position offset for sprites (relative to spawn area center)")]
    public float spriteYOffset = 0f;

    [Header("Randomization")]
    [Tooltip("Random rotation enabled (0-360 degrees)")]
    public bool useRandomRotation = true;
    
    [Tooltip("Minimum random rotation in degrees (if random rotation enabled)")]
    public float minRotation = 0f;
    
    [Tooltip("Maximum random rotation in degrees (if random rotation enabled)")]
    public float maxRotation = 360f;

    [Header("Dependencies")]
    [Tooltip("Deposit tray controller to track (auto-found if not assigned)")]
    public DepositTrayController depositTray;

    [Header("Debug")]
    [Tooltip("Log when sprites are spawned/removed")]
    public bool logVisualChanges = true;

    // Runtime lookup dictionary: (SpeciesType, OrderPartType) -> GameObject prefab
    private Dictionary<(SpeciesType, OrderPartType), GameObject> spriteLookup = new Dictionary<(SpeciesType, OrderPartType), GameObject>();
    
    // Track spawned visual instances: TrayPiece -> GameObject
    private Dictionary<TrayPiece, GameObject> pieceToVisualMap = new Dictionary<TrayPiece, GameObject>();
    
    // Track all current visual GameObjects for cleanup
    private List<GameObject> allVisuals = new List<GameObject>();
    
    // Cache previous tray state to detect additions/removals
    private HashSet<TrayPiece> previousTrayPieces = new HashSet<TrayPiece>();

    void Awake()
    {
        // Auto-find deposit tray if not assigned
        if (depositTray == null)
        {
            depositTray = FindObjectOfType<DepositTrayController>();
            if (depositTray == null)
            {
                Debug.LogError("[DepositTrayVisualizer] No DepositTrayController found! Visuals will not work.", this);
            }
        }

        // Build sprite lookup dictionary from Inspector mappings
        BuildSpriteLookup();
        
        // Validate configuration
        ValidateConfiguration();
    }

    void Start()
    {
        // Initialize previous state snapshot
        if (depositTray != null)
        {
            foreach (var piece in depositTray.trayPieces)
            {
                previousTrayPieces.Add(piece);
            }
        }
    }

    void Update()
    {
        if (depositTray == null) return;

        // Sync visuals with current tray state
        SyncVisualsWithTray();
    }

    /// <summary>
    /// Build the runtime lookup dictionary from Inspector-assigned mappings
    /// </summary>
    void BuildSpriteLookup()
    {
        spriteLookup.Clear();
        
        foreach (var mapping in spriteMappings)
        {
            if (mapping.spritePrefab == null)
            {
                if (logVisualChanges)
                {
                    Debug.LogWarning($"[DepositTrayVisualizer] Sprite mapping for {mapping.species}.{mapping.partType} has no prefab assigned! Skipping.");
                }
                continue;
            }

            var key = (mapping.species, mapping.partType);
            if (spriteLookup.ContainsKey(key))
            {
                Debug.LogWarning($"[DepositTrayVisualizer] Duplicate mapping for {mapping.species}.{mapping.partType}! Using first assignment.");
                continue;
            }

            spriteLookup[key] = mapping.spritePrefab;
        }

        if (logVisualChanges && spriteLookup.Count > 0)
        {
            Debug.Log($"[DepositTrayVisualizer] Built sprite lookup with {spriteLookup.Count} mappings");
        }
    }

    /// <summary>
    /// Validate configuration and warn about missing setups
    /// </summary>
    void ValidateConfiguration()
    {
        if (spawnAreaCenter == null)
        {
            Debug.LogWarning("[DepositTrayVisualizer] Spawn Area Center not assigned! Sprites will spawn at origin (0,0,0).", this);
        }

        if (spriteMappings.Count == 0)
        {
            Debug.LogWarning("[DepositTrayVisualizer] No sprite mappings assigned! No visuals will appear.", this);
        }
    }

    /// <summary>
    /// Sync visual GameObjects with current tray contents
    /// Detects additions and removals, spawns/removes visuals accordingly
    /// </summary>
    void SyncVisualsWithTray()
    {
        if (depositTray == null) return;

        var currentPieces = new HashSet<TrayPiece>(depositTray.trayPieces);

        // Find pieces that were added (in current, not in previous)
        foreach (var piece in depositTray.trayPieces)
        {
            if (piece == null) continue;

            if (!previousTrayPieces.Contains(piece) && !pieceToVisualMap.ContainsKey(piece))
            {
                // New piece added - spawn visual
                SpawnVisualForPiece(piece);
            }
        }

        // Find pieces that were removed (in previous, not in current)
        var removedPieces = new List<TrayPiece>();
        foreach (var piece in previousTrayPieces)
        {
            if (piece == null || !currentPieces.Contains(piece))
            {
                removedPieces.Add(piece);
            }
        }

        // Remove visuals for pieces no longer in tray
        foreach (var removedPiece in removedPieces)
        {
            RemoveVisualForPiece(removedPiece);
        }

        // Update previous state snapshot
        previousTrayPieces.Clear();
        foreach (var piece in depositTray.trayPieces)
        {
            if (piece != null)
            {
                previousTrayPieces.Add(piece);
            }
        }
    }

    /// <summary>
    /// Spawn a visual GameObject for a TrayPiece
    /// </summary>
    void SpawnVisualForPiece(TrayPiece piece)
    {
        if (piece == null)
        {
            Debug.LogWarning("[DepositTrayVisualizer] Cannot spawn visual for null piece");
            return;
        }

        // Look up sprite prefab
        var key = (piece.species, piece.partType);
        if (!spriteLookup.TryGetValue(key, out GameObject prefab))
        {
            if (logVisualChanges)
            {
                Debug.LogWarning($"[DepositTrayVisualizer] No sprite mapping found for {piece.species}.{piece.partType} - visual will not appear");
            }
            return;
        }

        // Calculate spawn position (random within spawn area)
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // Calculate spawn rotation (random if enabled)
        Quaternion spawnRotation = GetRandomRotation();

        // Instantiate visual GameObject
        GameObject visualInstance = Instantiate(prefab, spawnPosition, spawnRotation, transform);
        
        // Track the mapping
        pieceToVisualMap[piece] = visualInstance;
        allVisuals.Add(visualInstance);

        if (logVisualChanges)
        {
            Debug.Log($"[DepositTrayVisualizer] Spawned visual for {piece.species}.{piece.partType} at {spawnPosition}");
        }
    }

    /// <summary>
    /// Remove visual GameObject for a TrayPiece
    /// </summary>
    void RemoveVisualForPiece(TrayPiece piece)
    {
        if (piece == null) return;

        if (pieceToVisualMap.TryGetValue(piece, out GameObject visualInstance))
        {
            // Remove from tracking
            pieceToVisualMap.Remove(piece);
            allVisuals.Remove(visualInstance);

            // Destroy the GameObject
            if (visualInstance != null)
            {
                Destroy(visualInstance);
            }

            if (logVisualChanges)
            {
                Debug.Log($"[DepositTrayVisualizer] Removed visual for {piece.species}.{piece.partType}");
            }
        }
    }

    /// <summary>
    /// Get a random position within the spawn area bounds
    /// </summary>
    Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : Vector3.zero;
        
        // Random offset within spawn area size (centered)
        float x = center.x + Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float y = center.y + spriteYOffset + Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        float z = center.z + Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Get a random rotation (if enabled) or default rotation.
    /// All limbs spawn with 90° X rotation (laying flat), plus optional random Y rotation.
    /// </summary>
    Quaternion GetRandomRotation()
    {
        float xRotation = 90f; // Always 90° on X axis (laying flat)
        float yRotation = 0f;
        float zRotation = 0f;

        if (useRandomRotation)
        {
            yRotation = Random.Range(minRotation, maxRotation);
        }

        return Quaternion.Euler(xRotation, yRotation, zRotation);
    }

    /// <summary>
    /// Cleanup all visuals (called on disable/destroy)
    /// </summary>
    void OnDisable()
    {
        ClearAllVisuals();
    }

    void OnDestroy()
    {
        ClearAllVisuals();
    }

    /// <summary>
    /// Remove all visual GameObjects
    /// </summary>
    void ClearAllVisuals()
    {
        foreach (var visual in allVisuals)
        {
            if (visual != null)
            {
                Destroy(visual);
            }
        }

        pieceToVisualMap.Clear();
        allVisuals.Clear();
        previousTrayPieces.Clear();
    }

    /// <summary>
    /// Draw gizmos in Scene view to visualize spawn area
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (spawnAreaCenter == null) return;

        Gizmos.color = Color.yellow;
        Vector3 center = spawnAreaCenter.position;
        center.y += spriteYOffset;
        
        // Draw wireframe box for spawn area
        Gizmos.DrawWireCube(center, spawnAreaSize);
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.1f);
    }
}

/// <summary>
/// Serializable mapping structure for Inspector assignment.
/// Maps a species+partType combination to a sprite prefab.
/// </summary>
[System.Serializable]
public class LimbSpriteMapping
{
    [Tooltip("Species type (Cat, Dog, Bunny)")]
    public SpeciesType species;
    
    [Tooltip("Part type (Hand, Foot, Head, etc.)")]
    public OrderPartType partType;
    
    [Tooltip("Prefab to instantiate for this limb type. Can be a sprite renderer, 3D model, or any GameObject.")]
    public GameObject spritePrefab;

    public override string ToString()
    {
        return $"{species}.{partType} → {(spritePrefab != null ? spritePrefab.name : "NULL")}";
    }
}

