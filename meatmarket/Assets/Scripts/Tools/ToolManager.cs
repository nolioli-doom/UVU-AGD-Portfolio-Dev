using UnityEngine;
using UnityEngine.Events;

public class ToolManager : MonoBehaviour
{
    public enum ToolType
    {
        Cleaver,
        Shears,
        BoneSaw
    }

    [Header("Tool State")]
    [SerializeField] private ToolType currentTool = ToolType.Cleaver;

    [Header("Events")]
    public UnityEvent<ToolType> OnToolChanged;
    public UnityEvent OnCleaverSelected;
    public UnityEvent OnShearsSelected;
    public UnityEvent OnBoneSawSelected;

    // Singleton instance
    public static ToolManager Instance { get; private set; }

    public ToolType CurrentTool => currentTool;

    void Awake()
    {
        // Singleton pattern - keep first instance, destroy duplicates
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"[ToolManager] Multiple ToolManager instances found. Destroying duplicate on {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Fire initial event so UI can sync to default state
        OnToolChanged?.Invoke(currentTool);
        FireConvenienceEvent(currentTool);
    }

    public void SetTool(ToolType tool)
    {
        // If tool is already selected, do nothing
        if (tool == currentTool)
        {
            return;
        }

        // Set new tool and log
        currentTool = tool;
        Debug.Log($"[ToolManager] Tool selected: {tool}");

        // Fire events
        OnToolChanged?.Invoke(currentTool);
        FireConvenienceEvent(currentTool);
    }

    private void FireConvenienceEvent(ToolType tool)
    {
        switch (tool)
        {
            case ToolType.Cleaver:
                OnCleaverSelected?.Invoke();
                break;
            case ToolType.Shears:
                OnShearsSelected?.Invoke();
                break;
            case ToolType.BoneSaw:
                OnBoneSawSelected?.Invoke();
                break;
        }
    }
}

