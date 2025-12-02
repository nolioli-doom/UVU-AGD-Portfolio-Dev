using UnityEngine;
using System.Linq;

/// <summary>
/// Controller for the Main Menu scene.
/// Handles difficulty selection and starting the game with the selected difficulty.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Configuration")]
    [Tooltip("Name of the gameplay scene to load (must match scene file name)")]
    public string gameplaySceneName = "Gameplay";
    
    [Header("Difficulty Selection Menu")]
    [Tooltip("GameObject containing the difficulty selection menu (will be shown/hidden)")]
    public GameObject difficultyMenuPanel;
    
    [Header("Day Index SO")]
    [Tooltip("DayIndexSO that stores the current day index. If not assigned, will try to find it automatically.")]
    public DayIndexSO dayIndexSO;
    
    [Header("Difficulty Day Mapping")]
    [Tooltip("Day index for Tutorial difficulty")]
    public int tutorialDayIndex = 0;
    
    [Tooltip("Day index for Easy difficulty")]
    public int easyDayIndex = 1;
    
    [Tooltip("Day index for Normal difficulty")]
    public int normalDayIndex = 2;
    
    [Tooltip("Day index for Hard difficulty")]
    public int hardDayIndex = 3;
    
    [Header("Debug")]
    [Tooltip("Log actions for debugging")]
    public bool logActions = true;
    
    void Start()
    {
        // Auto-find DayIndexSO if not assigned (via singleton Instance to keep it alive across scenes)
        if (dayIndexSO == null)
        {
            dayIndexSO = DayIndexSO.Instance;
            if (dayIndexSO == null && logActions)
            {
                Debug.LogWarning("[MainMenuController] DayIndexSO not found! Please create one and assign it in the Inspector.");
            }
        }
        
        // Hide difficulty menu on start
        if (difficultyMenuPanel != null)
        {
            difficultyMenuPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show the difficulty selection menu.
    /// Call this from the Play button's OnClick event.
    /// </summary>
    public void ShowDifficultyMenu()
    {
        if (difficultyMenuPanel != null)
        {
            difficultyMenuPanel.SetActive(true);
            if (logActions)
            {
                Debug.Log("[MainMenuController] Showing difficulty selection menu");
            }
        }
        else if (logActions)
        {
            Debug.LogWarning("[MainMenuController] Difficulty menu panel not assigned!");
        }
    }
    
    /// <summary>
    /// Hide the difficulty selection menu.
    /// </summary>
    public void HideDifficultyMenu()
    {
        if (difficultyMenuPanel != null)
        {
            difficultyMenuPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Start game with Tutorial difficulty (day index 0).
    /// Call this from the Tutorial button's OnClick event.
    /// </summary>
    public void StartTutorial()
    {
        StartGameWithDifficulty(tutorialDayIndex, "Tutorial");
    }
    
    /// <summary>
    /// Start game with Easy difficulty (day index 1).
    /// Call this from the Easy button's OnClick event.
    /// </summary>
    public void StartEasy()
    {
        StartGameWithDifficulty(easyDayIndex, "Easy");
    }
    
    /// <summary>
    /// Start game with Normal difficulty (day index 2).
    /// Call this from the Normal button's OnClick event.
    /// </summary>
    public void StartNormal()
    {
        StartGameWithDifficulty(normalDayIndex, "Normal");
    }
    
    /// <summary>
    /// Start game with Hard difficulty (day index 3).
    /// Call this from the Hard button's OnClick event.
    /// </summary>
    public void StartHard()
    {
        StartGameWithDifficulty(hardDayIndex, "Hard");
    }
    
    /// <summary>
    /// Internal method to set day index and load the gameplay scene.
    /// </summary>
    private void StartGameWithDifficulty(int dayIndex, string difficultyName)
    {
        // Set the day index
        if (dayIndexSO != null)
        {
            dayIndexSO.currentDayIndex = dayIndex;
            if (logActions)
            {
                Debug.Log($"[MainMenuController] Set day index to {dayIndex} ({difficultyName})");
            }
        }
        else
        {
            Debug.LogError("[MainMenuController] DayIndexSO is not assigned! Cannot set day index.");
            return;
        }
        
        // Load the gameplay scene through loading screen
        if (logActions)
        {
            Debug.Log($"[MainMenuController] Loading gameplay scene: {gameplaySceneName}");
        }
        
        LoadingScreen.LoadScene(gameplaySceneName);
    }
}

