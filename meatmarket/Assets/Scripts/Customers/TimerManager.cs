using UnityEngine;

/// <summary>
/// Global singleton that updates timers for all pinned orders every frame.
/// This ensures timers count down regardless of which UI is visible.
/// </summary>
public class TimerManager : MonoBehaviour
{
    private static TimerManager instance;
    private OrderManagerSO orderManager;
    
    public static TimerManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Try to find existing instance
                instance = FindObjectOfType<TimerManager>();
                
                // If not found, create new
                if (instance == null)
                {
                    GameObject go = new GameObject("TimerManager");
                    instance = go.AddComponent<TimerManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        // Find OrderManagerSO
        orderManager = Resources.FindObjectsOfTypeAll<OrderManagerSO>()[0];
    }
    
    void Update()
    {
        if (orderManager != null)
        {
            orderManager.UpdateTimers();
        }
    }
}

