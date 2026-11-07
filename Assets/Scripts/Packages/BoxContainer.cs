using Fusion;
using UnityEngine;

public class BoxContainer : NetworkBehaviour, IInteractable
{
    [Header("Box Configuration")]
    public string boxName = "Small Box";
    public int maxCapacity = 4;
    [Networked] public int CurrentLoad { get; set; } = 0;

    [Networked, Capacity(20)] public NetworkLinkedList<int> PackedItemIDs => default;
    [Networked] public NetworkBool HasStamp { get; set; }
    [Networked] public int StampDestinationId { get; set; } = -1;

    [Header("References")]
    public Transform contentParent;
    public Transform stampAnchor;

    public string GetInteractionPrompt()
    {
        PlayerGrab localGrab = FindLocalPlayerGrab();
        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            GrabbableObject held = localGrab.HeldObject;
            if (held.TryGetComponent(out Stamp stamp))
            {
                if (!HasStamp && !stamp.IsAttached && stamp.destination != null)
                {
                    return $"Press [F] to put label ({stamp.destination.destiny}) on {boxName}";
                }
                return string.Empty;
            }

            if (held.TryGetComponent(out PackableItem item))
            {
                if (!item.IsPacked && item.productData != null)
                {
                    return $"Press [F] to pack {item.productData.productName} into {boxName} ({CurrentLoad}/{maxCapacity})";
                }
            }
        }
        return string.Empty;
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_TryUseHeldItem(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TryUseHeldItem(PlayerRef interactor)
    {
        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            return;
        } 
        GrabbableObject held = playerGrab.HeldObject;
        if (held.TryGetComponent(out Stamp stamp))
        {
            TryAttachStamp(playerGrab, stamp);
        }
        else if (held.TryGetComponent(out PackableItem item))
        {
            TryPackItem(playerGrab, item);
        }
    }

    private void TryAttachStamp(PlayerGrab playerGrab, Stamp stamp)
    {
        if (HasStamp || stamp.IsAttached || stamp.destination == null)
        {
            return;
        } 
        playerGrab.RequestDrop();
        HasStamp = true;
        StampDestinationId = stamp.destination.id;
        stamp.AttachToBox(stampAnchor != null ? stampAnchor : transform);
    }

    private void TryPackItem(PlayerGrab playerGrab, PackableItem item)
    {
        if (item.IsPacked || item.productData == null) return;

        if (CurrentLoad + item.itemSizeCost <= maxCapacity)
        {
            playerGrab.RequestDrop();
            CurrentLoad += item.itemSizeCost;
            PackedItemIDs.Add(item.productData.id);
            item.PackIntoBox(contentParent, CurrentLoad);
        }
    }
/*
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
*/
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