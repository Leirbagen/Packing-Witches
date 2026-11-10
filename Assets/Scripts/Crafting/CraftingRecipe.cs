using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Settings")]
    public string recipeName;

    [Tooltip("Ingredients.. ")]
    public ProductData[] requiredIngredients;

    [Tooltip("Finished product")]
    public NetworkPrefabRef resultPrefab;

    public float processTime = 3f;
}