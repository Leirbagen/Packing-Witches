using UnityEngine;

[CreateAssetMenu(fileName = "New Product", menuName = "Product")]
public class ProductData : ScriptableObject
{
    public int id;
    public string name;
    public Sprite icon;
    public double price;
}
