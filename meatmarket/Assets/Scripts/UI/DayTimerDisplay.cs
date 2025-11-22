using UnityEngine;
using TMPro;

/// <summary>
/// UI component to display the day timer countdown.
/// Updates text every frame to show remaining time.
/// </summary>
public class DayTimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to display the timer")]
    public TextMeshProUGUI timerText;
    
    [Header("Dependencies")]
    [Tooltip("DayTimer to get remaining time")]
    public DayTimer dayTimer;
    
    [Header("Text Format")]
    [Tooltip("Format string for timer display (use {0} for formatted time)")]
    public string timerFormat = "Time: {0}";
    
    [Header("Color Settings")]
    [Tooltip("Normal color for timer")]
    public Color normalColor = Color.white;
    
    [Tooltip("Warning color when time is running low")]
    public Color warningColor = Color.yellow;
    
    [Tooltip("Urgent color when time is very low")]
    public Color urgentColor = Color.red;
    
    [Tooltip("Time threshold for warning color (seconds)")]
    public float warningThreshold = 60f;
    
    [Tooltip("Time threshold for urgent color (seconds)")]
    public float urgentThreshold = 30f;
    
    void Start()
    {
        // Auto-find DayTimer if not assigned
        if (dayTimer == null)
        {
            dayTimer = FindObjectOfType<DayTimer>();
        }
    }
    
    void Update()
    {
        if (dayTimer == null || timerText == null) return;
        
        // Get formatted time string
        string timeString = dayTimer.GetFormattedTime();
        timerText.text = string.Format(timerFormat, timeString);
        
        // Update color based on remaining time
        float remainingTime = dayTimer.remainingTime;
        if (remainingTime <= urgentThreshold)
        {
            timerText.color = urgentColor;
        }
        else if (remainingTime <= warningThreshold)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }
}

