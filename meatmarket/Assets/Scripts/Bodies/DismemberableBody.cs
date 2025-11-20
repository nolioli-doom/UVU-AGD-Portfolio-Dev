using UnityEngine;

public class DismemberableBody : MonoBehaviour
{
    [Header("Body Configuration")]
    [SerializeField] private string bodyType = "CAT";

    public void OnClicked()
    {
        var tool = ToolManager.Instance != null ? ToolManager.Instance.CurrentTool : default(ToolManager.ToolType);
        Debug.Log($"[DismemberableBody] {tool} used on {bodyType}");
    }
}

