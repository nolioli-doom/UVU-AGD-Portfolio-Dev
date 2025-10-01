using UnityEngine;

public class OrderUIBinderUGUI : MonoBehaviour
{
    public OrderGenerator source;
    public RectTransform listParent;     // assign to ScrollView/Viewport/Content
    public GameObject orderCardPrefab;   // assign OrderCard prefab

    void OnEnable()
    {
        if (source != null)
            source.onRoundGeneratedWithPayload.AddListener(RefreshWithPayload);
    }
    void OnDisable()
    {
        if (source != null)
            source.onRoundGeneratedWithPayload.RemoveListener(RefreshWithPayload);
    }

    public void RefreshWithPayload(OrdersRound round)
    {
        // clear
        for (int i = listParent.childCount - 1; i >= 0; i--)
            Destroy(listParent.GetChild(i).gameObject);

        // rebuild
        foreach (var order in round.orders)
        {
            var go = Instantiate(orderCardPrefab, listParent);
            var card = go.GetComponent<OrderCardUI>();
            card.Bind(order);
        }
    }
}