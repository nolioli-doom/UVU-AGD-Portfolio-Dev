using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Data-only deposit tray store (whitebox). Holds TrayPiece entries
/// added by the severing pipeline and exposes debug helpers.
/// Integrates with AllocationService to allocate pieces to pinned orders.
/// </summary>
public class DepositTrayController : MonoBehaviour
{
    [Header("State")]
    public List<TrayPiece> trayPieces = new List<TrayPiece>();

    [Header("Dependencies")]
    [Tooltip("Allocation service for matching pieces to orders (auto-found if not assigned)")]
    public AllocationService allocationService;

    [Header("Debug")]
    public bool logOnAdd = false;
    public bool logDepositSummary = true;

    /// <summary>
    /// Add a TrayPiece to the tray (called by BodyPartTree/CutRouter when a limb fully severs).
    /// </summary>
    public void AddPiece(TrayPiece piece)
    {
        if (piece == null) return;
        trayPieces.Add(piece);
        if (logOnAdd)
        {
            Debug.Log($"[DepositTray] Added: {piece}");
        }
    }

    /// <summary>
    /// Backwards-compatibility: accept legacy SeveredPartData and convert to TrayPiece.
    /// </summary>
    public void AddPart(SeveredPartData part)
    {
        if (part == null) return;
        var piece = new TrayPiece
        {
            species = part.species,
            partType = PartTypeMapper.SegmentToOrderType(part.segmentType),
            limbId = $"{part.species}.{part.segmentType}",
            isPerfect = (part.quality == QualityTier.Perfect)
        };
        AddPiece(piece);
    }

    /// <summary>
    /// Remove all tray contents (Trash/Clear button behavior for now).
    /// </summary>
    public void TrashAll()
    {
        trayPieces.Clear();
    }

    /// <summary>
    /// Auto-find allocation service if not assigned
    /// </summary>
    void Awake()
    {
        if (allocationService == null)
        {
            allocationService = FindObjectOfType<AllocationService>();
            if (allocationService == null)
            {
                Debug.LogWarning("[DepositTray] No AllocationService found in scene. Deposit action will not work.");
            }
        }
    }

    /// <summary>
    /// Deposit action: Attempt to allocate all tray pieces to pinned orders.
    /// Matched pieces are removed from the tray. Unmatched pieces remain.
    /// Prints allocation summary to console.
    /// Wire this to the Deposit button in the Inspector.
    /// </summary>
    public void Deposit()
    {
        if (allocationService == null)
        {
            Debug.LogError("[DepositTray] Cannot deposit: AllocationService not found!");
            return;
        }

        if (trayPieces.Count == 0)
        {
            if (logDepositSummary) Debug.Log("[DepositTray] Tray is empty - nothing to deposit.");
            return;
        }

        // Attempt allocation
        var summary = allocationService.AllocateAllPieces();

        // Print summary
        if (logDepositSummary)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[DepositTray] Deposit Summary:");
            sb.AppendLine($"  Total pieces processed: {summary.allocatedCount + summary.unmatchedPieces.Count}");
            sb.AppendLine($"  Allocated: {summary.allocatedCount}");
            sb.AppendLine($"  Unmatched (left on tray): {summary.unmatchedPieces.Count}");
            sb.AppendLine($"  Orders completed: {summary.completedOrders.Count}");
            
            if (summary.allocationDetails.Count > 0)
            {
                sb.AppendLine("  Allocation details:");
                foreach (var detail in summary.allocationDetails)
                {
                    sb.AppendLine($"    Slot {detail.slotIndex + 1}: {detail.piece.partType} → {detail.order.customerName}");
                }
            }
            
            if (summary.unmatchedPieces.Count > 0)
            {
                sb.AppendLine("  Unmatched pieces:");
                foreach (var piece in summary.unmatchedPieces)
                {
                    sb.AppendLine($"    - {piece.partType} ({piece.species})");
                }
            }

            if (summary.completedOrders.Count > 0)
            {
                sb.AppendLine("  Completed orders:");
                foreach (var order in summary.completedOrders)
                {
                    sb.AppendLine($"    - {order.customerName}");
                }
            }
            
            Debug.Log(sb.ToString());
        }
    }

    /// <summary>
    /// Print current tray contents to the console (for whitebox debugging).
    /// Wire this to a UI Button in the inspector if desired.
    /// </summary>
    public void DebugPrintTray()
    {
        if (trayPieces.Count == 0)
        {
            Debug.Log("[DepositTray] Tray is empty.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[DepositTray] Contents:");
        for (int i = 0; i < trayPieces.Count; i++)
        {
            var p = trayPieces[i];
            sb.AppendLine($"  {i+1}. {p}");
        }
        Debug.Log(sb.ToString());
    }
}


