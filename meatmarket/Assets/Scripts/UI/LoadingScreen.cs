using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Helper class to trigger loading scene transitions.
/// Call LoadScene() or ReloadCurrentScene() to transition through the loading scene.
/// The actual loading logic is handled by LoadingSceneController in the loading scene.
/// </summary>
public static class LoadingScreen
{
    /// <summary>
    /// Name of the loading scene (must match the scene file name in Build Settings)
    /// </summary>
    public const string LOADING_SCENE_NAME = "LoadingScene";
    
    /// <summary>
    /// Static variable to store the target scene name for the loading scene to load
    /// </summary>
    public static string TargetSceneName { get; private set; } = "";
    
    /// <summary>
    /// Reload the current scene by going through the loading scene
    /// </summary>
    public static void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        LoadScene(currentSceneName);
    }
    
    /// <summary>
    /// Load a specific scene by going through the loading scene
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        TargetSceneName = sceneName;
        SceneManager.LoadScene(LOADING_SCENE_NAME);
    }
}

