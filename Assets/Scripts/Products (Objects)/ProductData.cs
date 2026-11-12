using UnityEngine;

[CreateAssetMenu(fileName = "New Product", menuName = "Product")]
public class ProductData : ScriptableObject
{
    public int id;
    public string productName;
    public Sprite icon;
    public double price;
    public Vector2Int footprint = new Vector2Int(1, 1);
}
