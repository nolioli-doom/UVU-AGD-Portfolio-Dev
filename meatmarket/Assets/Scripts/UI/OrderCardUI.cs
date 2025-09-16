using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class OrderCardUI : MonoBehaviour
{
    [Header("UI References")]
    public Text headerText;   // "Name - Archetype"
    public Text timerText;    // "Time: 00s"
    public Text itemsText;    // Multiline ordered items

    float timer;
    bool ticking;

    public void Bind(CustomerOrder order)
    {
        // Header: Name - Archetype
        string archetypeName = (order.archetype != null && !string.IsNullOrEmpty(order.archetype.displayName))
            ? order.archetype.displayName
            : "—";
        headerText.text = $"{order.customerName} - {archetypeName}";

        // Timer
        timer = Mathf.Max(0f, order.timeLimitSeconds);
        ticking = true;
        timerText.text = $"Time: {timer:0}s";

        // Ordered Items (multiline)
        var sb = new StringBuilder();
        foreach (var it in order.items)
            sb.AppendLine($"{it.quantity}× {it.species} {it.part} (≥{it.minQuality})");
        itemsText.text = sb.ToString().TrimEnd();
    }

    void Update()
    {
        if (!ticking) return;

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        timerText.text = $"Time: {timer:0}s";

        if (timer <= 0f) ticking = false;
    }
}