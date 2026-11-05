using TMPro;
using UnityEngine;

public class OrdersUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private OrderEntryUI orderPrefab;

    private void OnEnable()
    {
        OrderManager.OnOrdersChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        OrderManager.OnOrdersChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        foreach (int id in OrderManager.Instance.ReceivedOrders)
        {
            OrderData order = OrderManager.Instance.GetOrder(id);
            if (order == null) continue;

            OrderEntryUI entry = Instantiate(orderPrefab, container);
            entry.OrderConstructor(id, BuildText(order));
        }
    }

    private string BuildText(OrderData order)
    {
        string text = order.message + "\n";
        foreach (ProductQuantity line in order.products)
        {
            text += "- " + " x" + line.quantity + line.product.name + "\n";
        }
        text += "Pago: $" + order.payment;
        return text;
    }
}
