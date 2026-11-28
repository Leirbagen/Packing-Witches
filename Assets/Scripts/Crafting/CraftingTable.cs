using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class CraftingTable : NetworkBehaviour, IInteractable
{
    public CraftingRecipe[] availableRecipes;
    public string stationName = "Crafting Station";
    public Transform snapPoint;
    public WorldPromptUI worldPrompt;

    [Networked, Capacity(4)] public NetworkLinkedList<int> PlacedIngredientIDs => default;
    [Networked] public NetworkBool IsProcessing { get; set; }
    [Networked] public TickTimer ProcessTimer { get; set; }
    [Networked] private NetworkPrefabRef currentResultPrefab { get; set; }

    private MachineBreakdown breakdown;
    private MachineUpgrade upgrade;

    private void Awake()
    {
        breakdown = GetComponent<MachineBreakdown>();
        upgrade = GetComponent<MachineUpgrade>();

        if (worldPrompt == null)
        {
            worldPrompt = GetComponentInChildren<WorldPromptUI>();
        }
    }

    private bool IsMachineBroken()
    {
        return breakdown != null && breakdown.Object != null && breakdown.Object.IsValid && breakdown.IsBroken;
    }

    public string GetInteractionPrompt()
    {
        if (Object == null || !Object.IsValid)
        {
            return string.Empty;
        }

        if (IsMachineBroken())
        {
            return breakdown.GetBrokenPrompt();
        }

        string upgradeLine = string.Empty;
        if (upgrade != null)
        {
            string prompt = upgrade.GetUpgradePrompt();
            if (!string.IsNullOrEmpty(prompt))
            {
                upgradeLine = "\n" + prompt;
            }
        }

        if (IsProcessing)
        {
            float time = ProcessTimer.RemainingTime(Runner) ?? 0f;
            return $"{stationName} processing... {time:F1}s{upgradeLine}";
        }

        if (PlacedIngredientIDs.Count > 0)
        {
            List<CraftingRecipe> possible = GetPossibleRecipes();

            if (possible.Count == 0)
            {
                return $"<color=#FF4444>Invalid combination.</color>{upgradeLine}";
            }

            List<string> possibleNames = new List<string>();
            foreach (CraftingRecipe recipe in possible)
            {
                if (recipe.requiredIngredients.Length == PlacedIngredientIDs.Count)
                {
                    return $"Press [F] to activate {stationName}: <color=#FFFF00>{recipe.recipeName}</color>{upgradeLine}";
                }
                possibleNames.Add(recipe.recipeName);
            }

            string namesJoined = string.Join(", ", possibleNames);
            return $"{stationName} | Items: {PlacedIngredientIDs.Count} | Possible: <color=#00FFFF>{namesJoined}</color>{upgradeLine}";
        }

        return $"Press [F] to place ingredient in {stationName}{upgradeLine}";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (Object == null || !Object.IsValid || IsMachineBroken() || IsProcessing)
        {
            return;
        }

        RPC_InteractWithTable(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InteractWithTable(PlayerRef interactor)
    {
        if (IsMachineBroken() || IsProcessing)
        {
            return;
        }

        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null)
        {
            return;
        }

        if (playerGrab.IsHolding && playerGrab.HeldObject != null)
        {
            PackableItem heldItem = playerGrab.HeldObject.GetComponent<PackableItem>();
            if (heldItem != null && heldItem.productData != null)
            {
                if (!IsValidIngredient(heldItem.productData.id))
                {
                    RPC_BroadcastMessage("<color=#FFA500>[Crafting] This product does not have a recipe</color>");
                    return;
                }

                if (PlacedIngredientIDs.Count < PlacedIngredientIDs.Capacity)
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

    private bool IsValidIngredient(int productId)
    {
        foreach (CraftingRecipe recipe in availableRecipes)
        {
            foreach (ProductData reqIngredient in recipe.requiredIngredients)
            {
                if (reqIngredient.id == productId)
                {
                    return true;
                }
            }
        }

        return false;
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
                float finalTime = recipe.processTime;
                if (upgrade != null && upgrade.Object != null && upgrade.Object.IsValid)
                {
                    finalTime = recipe.processTime * upgrade.GetProcessTimeMultiplier();
                }

                IsProcessing = true;
                ProcessTimer = TickTimer.CreateFromSeconds(Runner, finalTime);
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

    private PlayerGrab FindPlayerGrab(PlayerRef playerRef)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null && playerObj.TryGetComponent(out PlayerGrab playerGrab))
        {
            return playerGrab;
        }

        foreach (PlayerGrab grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.InputAuthority == playerRef)
            {
                return grabber;
            }
        }

        return null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || IsMachineBroken())
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
        if (worldPrompt != null)
        {
            worldPrompt.ShowTemporaryMessage(msg);
        }
    }
}