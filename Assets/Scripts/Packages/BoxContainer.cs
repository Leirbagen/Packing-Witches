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
                    return $"Press [PACK] to put label ({stamp.destination.destiny}) on {boxName}";
                }
                return string.Empty;
            }

            if (held.TryGetComponent(out PackableItem item))
            {
                if (!item.IsPacked && item.productData != null)
                {
                    return $"Press [F] to place {item.productData.productName} (R to rotate)";
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

        if (playerGrab.HeldObject.TryGetComponent(out Stamp stamp))
        {
            TryAttachStamp(playerGrab, stamp);
        }
    }

    public void RequestPack(int x, int y, int rotation, Vector2Int size)
    {
        RPC_PackAt(Runner.LocalPlayer, x, y, rotation, size.x, size.y);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PackAt(PlayerRef interactor, int x, int y, int rotation, int sizeX, int sizeY)
    {
        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            return;
        }

        if (playerGrab.HeldObject.TryGetComponent(out PackableItem item))
        {
            TryPackItem(playerGrab, item, x, y, rotation, new Vector2Int(sizeX, sizeY));
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
        Transform anchor;
        if(stampAnchor != null)
        {
            anchor = stampAnchor;
        }
        else
        {
            anchor = transform;
        }
        stamp.AttachToBox(anchor, Object);
    }

    private void TryPackItem(PlayerGrab playerGrab, PackableItem item, int x, int y, int rotation, Vector2Int size)
    {
        if (item.IsPacked || item.productData == null)
        {
            return;
        }
        Vector2Int fp = item.productData.footprint;

        bool normal = false;
        if (size.x == fp.x && size.y == fp.y)
        {
            normal = true;
        }

        bool swapped = false;
        if (size.x == fp.y && size.y == fp.x)
        {
            swapped = true;
        }

        if (!normal && !swapped)
        {
            return;
        }

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