using UnityEngine;

public class RoundBootstrap : MonoBehaviour
{
    public OrderGenerator generator;
    [Min(0)] public int dayIndex;

    [ContextMenu("Generate Round Now")]
    public void GenerateNow()
    {
        generator.GenerateRound(dayIndex);
    }
}