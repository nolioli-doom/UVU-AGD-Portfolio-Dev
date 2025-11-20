using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CustomerOrder
{
    public CustomerArchetypeSO archetype;
    public List<OrderItem> items = new();
    public float timeLimitSeconds = 60f;      // affected by archetype
    public float tipMultiplier = 1f;          // from archetype
    public float wasteSensitivity = 0.5f;     // from archetype
    public string customerName;               // from archetype/display
    
    // Timer tracking
    private float remainingTime;
    private bool timerInitialized = false;
    
    /// <summary>
    /// Get or initialize remaining time
    /// </summary>
    public float GetRemainingTime()
    {
        if (!timerInitialized)
        {
            remainingTime = timeLimitSeconds;
            timerInitialized = true;
        }
        return remainingTime;
    }
    
    /// <summary>
    /// Tick down the timer (called every frame)
    /// </summary>
    public void TickTimer(float deltaTime)
    {
        if (!timerInitialized)
        {
            remainingTime = timeLimitSeconds;
            timerInitialized = true;
        }
        remainingTime -= deltaTime;
        remainingTime = Mathf.Max(0f, remainingTime);
    }
    
    /// <summary>
    /// Check if order is expired
    /// </summary>
    public bool IsExpired()
    {
        return remainingTime <= 0f;
    }
}