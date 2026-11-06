using Fusion;
using UnityEngine;

public class BoxContainer : NetworkBehaviour, IInteractable
{
    [Header("Box Configuration")]
    public string boxName = "Small Box";
    public int maxCapacity = 4;
    [Networked] public int CurrentLoad { get; set; } = 0;

    [Networked, Capacity(20)] public NetworkLinkedList<int> PackedItemIDs => default;

    [Header("References")]
    public Transform contentParent;

    public string GetInteractionPrompt()
    {
        PlayerGrab localGrab = FindLocalPlayerGrab();
        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            PackableItem item = localGrab.HeldObject.GetComponent<PackableItem>();
            if (item != null && !item.IsPacked && item.productData != null)
            {
                return $"Press [F] to pack {item.productData.productName} into {boxName} ({CurrentLoad}/{maxCapacity})";
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
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null) return;

        PackableItem item = playerGrab.HeldObject.GetComponent<PackableItem>();
        if (item == null || item.IsPacked || item.productData == null) return;

        if (CurrentLoad + item.itemSizeCost <= maxCapacity)
        {
            playerGrab.RequestDrop();
            CurrentLoad += item.itemSizeCost;
            PackedItemIDs.Add(item.productData.id);
            item.PackIntoBox(contentParent, CurrentLoad);
        }
    }

    private PlayerGrab FindPlayerGrab(PlayerRef playerRef)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null && playerObj.TryGetComponent(out PlayerGrab playerGrab)) return playerGrab;
        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.InputAuthority == playerRef) return grabber;
        }
        return null;
    }

    private PlayerGrab FindLocalPlayerGrab()
    {
        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.HasInputAuthority) return grabber;
        }
        return null;
    }
}