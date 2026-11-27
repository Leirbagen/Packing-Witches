using Fusion;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CraftingTable : NetworkBehaviour, IInteractable
{
    [Header("Database")]
    public CraftingRecipe[] availableRecipes;

    [Header("Crafting Settings")]
    public string stationName = "Crafting Station";
    public Transform snapPoint;

    public WorldPromptUI worldPrompt;
    [Networked, Capacity(4)] public NetworkLinkedList<int> PlacedIngredientIDs => default;

    [Networked] public NetworkBool IsProcessing { get; set; }
    [Networked] public TickTimer ProcessTimer { get; set; }

    [Networked] private NetworkPrefabRef currentResultPrefab { get; set; }

    private MachineBreakdown breakdown;

    private void Awake()
    {
        breakdown = GetComponent<MachineBreakdown>();
    }

    public string GetInteractionPrompt()
    {
        if (breakdown != null && breakdown.IsBroken)
        {
            return breakdown.GetBrokenPrompt();
        }

        if (IsProcessing)
        {
            float time = ProcessTimer.RemainingTime(Runner) ?? 0f;
            return $"{stationName} processing... {time:F1}s";
        }

        if (PlacedIngredientIDs.Count > 0)
        {
            List<CraftingRecipe> possible = GetPossibleRecipes();

            if (possible.Count == 0)
            {
                return "<color=#FF4444>Invalid combination.</color>";
            }

            foreach (CraftingRecipe p in possible)
            {
                if (p.requiredIngredients.Length == PlacedIngredientIDs.Count)
                {
                    return $"Press [F] to activate {stationName}: <color=#FFFF00>{p.recipeName}</color>";
                }
            }

            string possibleNames = string.Join(", ", possible.Select(r => r.recipeName));
            return $"{stationName} | Items: {PlacedIngredientIDs.Count} | Possible: <color=#00FFFF>{possibleNames}</color>";
        }

        return $"Press [F] to place ingredient in {stationName}";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (breakdown != null && breakdown.IsBroken)
        {
            return;
        }

        if (IsProcessing)
        {
            return;
        }

        RPC_InteractWithTable(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InteractWithTable(PlayerRef interactor)
    {
        if (breakdown != null && breakdown.IsBroken)
        {
            return;
        }

        if (IsProcessing)
        {
            return;
        }

        PlayerGrab playerGrab = FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.Object.InputAuthority == interactor);

        if (playerGrab == null)
        {
            return;
        }

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

                    if (isIngredientValid)
                    {
                        break;
                    }
                }

                if (!isIngredientValid)
                {
                    RPC_BroadcastMessage("<color=#FFA500>[Crafting] This product does not have a recipe</color>");
                    return;
                }

                if (PlacedIngredientIDs.Count < 4)
                {
                    PlacedIngredientIDs.Add(heldItem.productData.id);

                    GrabbableObject itemToDestroy = playerGrab.HeldObject;
                    playerGrab.RequestDrop();
                    Runner.Despawn(itemToDestroy.Object);
                }
            }
        }
        else if (!playerGrab.IsHolding)
        {
            TryCraft();
        }
    }

    private List<CraftingRecipe> GetPossibleRecipes()
    {
        List<CraftingRecipe> possibleRecipes = new List<CraftingRecipe>();
        if (PlacedIngredientIDs.Count == 0)
        {
            return possibleRecipes;
        }

        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (CanFormRecipe(recipe))
            {
                possibleRecipes.Add(recipe);
            }
        }
        return possibleRecipes;
    }

    private bool CanFormRecipe(CraftingRecipe recipe)
    {
        if (PlacedIngredientIDs.Count > recipe.requiredIngredients.Length)
        {
            return false;
        }

        List<int> requirements = new List<int>();
        foreach (ProductData req in recipe.requiredIngredients)
        {
            requirements.Add(req.id);
        }

        foreach (int tableItemID in PlacedIngredientIDs)
        {
            if (requirements.Contains(tableItemID))
            {
                requirements.Remove(tableItemID);
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private void TryCraft()
    {
        if (PlacedIngredientIDs.Count == 0)
        {
            return;
        }

        foreach (CraftingRecipe recipe in availableRecipes)
        {
            if (MatchRecipe(recipe))
            {
                IsProcessing = true;
                ProcessTimer = TickTimer.CreateFromSeconds(Runner, recipe.processTime);
                currentResultPrefab = recipe.resultPrefab;

                RPC_BroadcastMessage($"<color=#00FFFF>[Crafting] Started crafting: {recipe.recipeName}</color>");
                return;
            }
        }

        RPC_BroadcastMessage("<color=#FF4444>[Crafting] Invalid combination</color>");
    }

    private bool MatchRecipe(CraftingRecipe recipe)
    {
        if (recipe.requiredIngredients.Length != PlacedIngredientIDs.Count)
        {
            return false;
        }

        List<int> tableItems = new List<int>();
        foreach (int id in PlacedIngredientIDs)
        {
            tableItems.Add(id);
        }

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
        if (!HasStateAuthority)
        {
            return;
        }

        if (breakdown != null && breakdown.IsBroken)
        {
            return;
        }

        if (IsProcessing && ProcessTimer.Expired(Runner))
        {
            IsProcessing = false;
            PlacedIngredientIDs.Clear();

            if (currentResultPrefab.IsValid)
            {
                Runner.Spawn(currentResultPrefab, snapPoint.position, snapPoint.rotation);
                RPC_BroadcastMessage("<color=#44FF44>[Crafting] Product ready!</color>");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastMessage(string msg)
    {
        worldPrompt.ShowTemporaryMessage(msg);
    }
}