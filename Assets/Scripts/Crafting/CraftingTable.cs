using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CraftingTable : NetworkBehaviour, IInteractable
{
    [Header("Database")]
    public CraftingRecipe[] availableRecipes; 

    [Header("Crafting Settings")]
    public Transform snapPoint; 

    
    [Networked, Capacity(4)] public NetworkLinkedList<int> PlacedIngredientIDs => default;

    [Networked] public NetworkBool IsProcessing { get; set; }
    [Networked] public TickTimer ProcessTimer { get; set; }

    [Networked] private NetworkPrefabRef currentResultPrefab { get; set; }

    public string GetInteractionPrompt()
    {
        if (IsProcessing)
        {
            float time = ProcessTimer.RemainingTime(Runner) ?? 0f;
            return $"Processing..{time:F1}s";
        }

        if (PlacedIngredientIDs.Count > 0)
        {
            return $"Press [F] to place item OR craft ({PlacedIngredientIDs.Count} items on table)";
        }

        return "Press [F] to place ingredient";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsProcessing) return;
        RPC_InteractWithTable(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InteractWithTable(PlayerRef interactor)
    {
        if (IsProcessing) return;

        PlayerGrab playerGrab = FindObjectsOfType<PlayerGrab>()
            .FirstOrDefault(p => p.Object.InputAuthority == interactor);

        if (playerGrab == null) return;

        if (playerGrab.IsHolding && playerGrab.HeldObject != null)
        {
            PackableItem heldItem = playerGrab.HeldObject.GetComponent<PackableItem>();

            if (heldItem != null && heldItem.productData != null)
            {
                bool isIngredientValid = false;
                foreach (CraftingRecipe recipe in availableRecipes)
                {
                    foreach (ProductData reqIngredient in recipe.requiredIngredients)
                    {
                        if (reqIngredient.id == heldItem.productData.id)
                        {
                            isIngredientValid = true;
                            break; 
                        }
                    }
                    if (isIngredientValid) break;
                }

                if (!isIngredientValid)
                {
                    RPC_BroadcastMessage($"<color=orange>[Crafting] this product does not have a recipe</color>");
                    return; 
                }

                if (PlacedIngredientIDs.Count < 4)
                {
                    PlacedIngredientIDs.Add(heldItem.productData.id);

                    GrabbableObject itemToDestroy = playerGrab.HeldObject;
                    playerGrab.RequestDrop();
                    Runner.Despawn(itemToDestroy.Object);

                    Debug.Log($"<color=white>[Crafting] Ingredient added .</color>");
                }
            }
        }
        else if (!playerGrab.IsHolding)
        {
            TryCraft();
        }
    }

    private void TryCraft()
    {
        if (PlacedIngredientIDs.Count == 0) return;

        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (MatchRecipe(recipe))
            {

                IsProcessing = true;
                ProcessTimer = TickTimer.CreateFromSeconds(Runner, recipe.processTime);
                currentResultPrefab = recipe.resultPrefab;

                RPC_BroadcastMessage($"<color=cyan>[Crafting] Started crafting: {recipe.recipeName}</color>");
                return;
            }
        }

        RPC_BroadcastMessage("<color=red>[Crafting] Invalid combination</color>");
    }

    private bool MatchRecipe(CraftingRecipe recipe)
    {
        if (recipe.requiredIngredients.Length != PlacedIngredientIDs.Count) return false;

        List<int> tableItems = new List<int>();
        foreach (int id in PlacedIngredientIDs) tableItems.Add(id);

        foreach (ProductData reqIngredient in recipe.requiredIngredients)
        {
            if (tableItems.Contains(reqIngredient.id))
            {
                tableItems.Remove(reqIngredient.id); 
            }
            else
            {
                return false; 
            }
        }
        return true; 
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsProcessing && ProcessTimer.Expired(Runner))
        {
            IsProcessing = false;
            PlacedIngredientIDs.Clear(); 

            if (currentResultPrefab.IsValid)
            {
                Runner.Spawn(currentResultPrefab, snapPoint.position, snapPoint.rotation);
                RPC_BroadcastMessage("<color=green>[Crafting] Product ready!</color>");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastMessage(string msg)
    {
        Debug.Log(msg);
    }
}