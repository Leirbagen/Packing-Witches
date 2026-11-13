using Fusion;
using UnityEngine;

public class BoxContainer : NetworkBehaviour, IInteractable
{
    [Header("Box Configuration")]
    public string boxName = "Small Box";

    [Networked, Capacity(20)] public NetworkLinkedList<int> PackedItemIDs => default;
    [Networked] public NetworkBool HasStamp { get; set; }
    [Networked] public int StampDestinationId { get; set; } = -1;

    [Header("References")]
    public BoxGrid grid;
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
                    return $"Press [F] to pack {item.productData.productName} into {boxName}";
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
        /*
        GrabbableObject held = playerGrab.HeldObject;
        if (held.TryGetComponent(out Stamp stamp))
        {
            TryAttachStamp(playerGrab, stamp);
        }
        else if (held.TryGetComponent(out PackableItem item))
        {
            TryPackItemAuto(playerGrab, item);
        }
        */
        if (playerGrab.HeldObject.TryGetComponent(out Stamp stamp))
        {
            TryAttachStamp(playerGrab, stamp);
        }
    }

    public void RequestPack(int x, int y, int rotation)
    {
        RPC_PackAt(Runner.LocalPlayer, x, y, rotation);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PackAt(PlayerRef interactor, int x, int y, int rotation)
    {
        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            return;
        } 
        if (playerGrab.HeldObject.TryGetComponent(out PackableItem item))
        {
            TryPackItem(playerGrab, item, x, y, rotation);
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

/*
    private void TryPackItemAuto(PlayerGrab playerGrab, PackableItem item)
    {
        if (item.IsPacked || item.productData == null) return;

        for (int rotation = 0; rotation < 2; rotation++)
        {
            Vector2Int size = grid.GetSize(item.productData.footprint, rotation);

            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    if (grid.CanPlace(size, x, y))
                    {
                        TryPackItem(playerGrab, item, x, y, rotation);
                        return;
                    }
                }
            }
        }
        Debug.Log("[Packing] No hay espacio en la caja");
    }
*/
    
    private void TryPackItem(PlayerGrab playerGrab, PackableItem item, int x, int y, int rotation)
    {
        if (item.IsPacked || item.productData == null)
        {
            return;
        } 

        Vector2Int size = grid.GetSize(item.productData.footprint, rotation);

        if (grid.Place(size, x, y) == -1)
        {
            return;
        } 

        playerGrab.RequestDrop();
        PackedItemIDs.Add(item.productData.id);

        Vector3 pos = grid.FootprintCenterWorld(size, x, y);
        Quaternion rot = grid.gridOrigin.rotation * Quaternion.Euler(0f, 90f * rotation, 0f);
        item.PackIntoBox(Object, pos, rot);
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