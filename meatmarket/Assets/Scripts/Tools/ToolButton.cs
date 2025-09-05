using UnityEngine;

public class ToolButton : MonoBehaviour
{
    [Header("Tool Configuration")]
    [SerializeField] private ToolManager.ToolType toolToEquip;

    public void Equip()
    {
        if (ToolManager.Instance != null)
        {
            ToolManager.Instance.SetTool(toolToEquip);
        }
        else
        {
            Debug.LogWarning($"[ToolButton] ToolManager.Instance is null. Cannot equip {toolToEquip}");
        }
    }
}

