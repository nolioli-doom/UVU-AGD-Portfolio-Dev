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
        
        // Continuously update progress until ready to activate
        // Note: progress only goes 0-0.9 when allowSceneActivation is false
        while (!loadingOperation.isDone)
        {
            // Calculate progress percentage (0-90% range, then we'll show 100% when ready)
            float rawProgress = loadingOperation.progress;
            int progressPercent = Mathf.RoundToInt(rawProgress * 100f);
            
            // Update loading text with progress
            if (loadingText != null)
            {
                loadingText.text = $"{loadingMessage} {progressPercent}%";
            }
            
            // When progress reaches 0.9 (90%), the scene is ready but not activated
            // We'll show 90% and then wait for minimum time before activating
            if (rawProgress >= 0.9f)
            {
                // Show 90% while waiting
                if (loadingText != null)
                {
                    loadingText.text = $"{loadingMessage} 90%";
                }
                break;
            }
            
            yield return null;
        }
        
        // Ensure minimum load time has passed (prevents flash if load is too fast)
        float elapsedTime = Time.time - loadStartTime;
        if (elapsedTime < minimumLoadTime)
        {
            // Show progress while waiting for minimum time
            float waitTime = minimumLoadTime - elapsedTime;
            float waitStart = Time.time;
            while (Time.time - waitStart < waitTime)
            {
                // Interpolate from 90% to 99% during wait
                float waitProgress = (Time.time - waitStart) / waitTime;
                int displayPercent = Mathf.RoundToInt(90f + (waitProgress * 9f));
                if (loadingText != null)
                {
                    loadingText.text = $"{loadingMessage} {displayPercent}%";
                }
                yield return null;
            }
        }
        
        // Show 100% before activating
        if (loadingText != null)
        {
            loadingText.text = $"{loadingMessage} 100%";
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


