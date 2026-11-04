using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OrderCatalog", menuName = "Catalog")]
public class OrderCatalog : ScriptableObject
{
    public List<OrderData> orders;

    public OrderData GetById(int id)
    {
        foreach(OrderData order in orders)
        {
            if(order.id == id)
            {
                return order;
            }
        }
        return null;
    }
}
