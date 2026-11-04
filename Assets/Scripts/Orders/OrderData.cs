using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewOrder", menuName = "Order")]
public class OrderData : ScriptableObject
{
    public int id;
    public string message;
    public List<ProductQuantity> products = new List<ProductQuantity>();
    public double payment;
}

public class ProductQuantity
{
    public ProductData product;
    public int quantity;
}
