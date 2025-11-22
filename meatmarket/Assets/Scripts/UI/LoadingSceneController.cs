using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controller for the LoadingScene.
/// Handles the actual scene loading and displays loading progress.
/// This script should be attached to a GameObject in the LoadingScene.
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to display loading message")]
    public TextMeshProUGUI loadingText;
    
    [Header("Configuration")]
    [Tooltip("Text to display while loading")]
    public string loadingMessage = "Loading...";
    
    [Tooltip("Minimum time to show loading screen (seconds) - prevents flash if load is too fast")]
    public float minimumLoadTime = 0.5f;
    
    [Header("Debug")]
    public bool logLoading = true;
    
    private AsyncOperation loadingOperation;
    private float loadStartTime;
    
    void Start()
    {
        loadStartTime = Time.time;
        
        // Get target scene name from LoadingScreen static variable
        string targetScene = LoadingScreen.TargetSceneName;
        
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[LoadingSceneController] No target scene specified! Cannot load.");
            return;
        }
        
        if (logLoading)
        {
            Debug.Log($"[LoadingSceneController] Loading scene: {targetScene}");
        }
        
        // Update loading text
        if (loadingText != null)
        {
            loadingText.text = loadingMessage;
        }
        
        // Start loading the target scene
        StartCoroutine(LoadTargetScene(targetScene));
    }
    
    private System.Collections.IEnumerator LoadTargetScene(string sceneName)
    {
        // Load scene asynchronously
        loadingOperation = SceneManager.LoadSceneAsync(sceneName);
        loadingOperation.allowSceneActivation = false; // We'll activate manually
        
        // Wait until scene is loaded (90% complete)
        while (loadingOperation.progress < 0.9f)
        {
            // Update loading text with progress if desired
            if (loadingText != null)
            {
                int progress = Mathf.RoundToInt(loadingOperation.progress * 100f);
                loadingText.text = $"{loadingMessage} {progress}%";
            }
            yield return null;
        }
        
        // Ensure minimum load time has passed (prevents flash if load is too fast)
        float elapsedTime = Time.time - loadStartTime;
        if (elapsedTime < minimumLoadTime)
        {
            yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
        }
        
        // Small additional delay for smooth transition
        yield return new WaitForSeconds(0.2f);
        
        // Activate the scene
        loadingOperation.allowSceneActivation = true;
        
        if (logLoading)
        {
            Debug.Log($"[LoadingSceneController] Scene {sceneName} loaded and activated");
        }
    }
}


