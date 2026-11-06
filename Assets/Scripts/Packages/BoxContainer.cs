using Fusion;
using UnityEngine;

public class BoxContainer : NetworkBehaviour, IInteractable
{
    [Header("Box Configuration")]
    public string boxName = "Small Box";
    public int maxCapacity = 2;
    [Networked] public int CurrentLoad { get; set; } = 0;

    [Header("References")]
    public Transform contentParent;

    public string GetInteractionPrompt()
    {
        PlayerGrab localGrab = FindLocalPlayerGrab();
        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            PackableItem item = localGrab.HeldObject.GetComponent<PackableItem>();
            if (item != null && !item.IsPacked)
            {
                return $"Press [F] to pack item into {boxName} ({CurrentLoad}/{maxCapacity})";
            }
        }

        return string.Empty;
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_TryPackItem(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TryPackItem(PlayerRef interactor)
    {
        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            Debug.Log("[Packing] Not holding any object to pack.");
            return;
        }

        PackableItem item = playerGrab.HeldObject.GetComponent<PackableItem>();
        if (item == null || item.IsPacked)
        {
            Debug.Log("[Packing] Held object is not supported to be packed.");
            return;
        }

        if (CurrentLoad + item.itemSizeCost <= maxCapacity)
        {
            GrabbableObject heldObj = playerGrab.HeldObject;

            playerGrab.RequestDrop();
            CurrentLoad += item.itemSizeCost;
            item.PackIntoBox(contentParent, CurrentLoad);

            Debug.Log($"[Packing] Object packed successfully in {boxName}. Actual box capacity: {CurrentLoad}/{maxCapacity}");
        }
        else
        {
            Debug.Log("[Packing] Box is full.");
        }
    }

    private PlayerGrab FindPlayerGrab(PlayerRef playerRef)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null && playerObj.TryGetComponent(out PlayerGrab playerGrab))
        {
            return playerGrab;
        }

        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.InputAuthority == playerRef)
            {
                return grabber;
            }
        }

        return null;
    }

    private PlayerGrab FindLocalPlayerGrab()
    {
        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.HasInputAuthority)
            {
                return grabber;
            }
        }
        return null;
    }
}