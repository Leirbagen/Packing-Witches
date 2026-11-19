using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class BoxContainer : NetworkBehaviour, IInteractable
{
    [Header("Box Configuration")]
    public string boxName = "Small Box";

    [Networked, Capacity(20)]
    public NetworkLinkedList<int> PackedItemIDs => default;
    [Networked] public NetworkBool HasStamp { get; set; }
    [Networked] public int StampDestinationId { get; set; } = -1;
    [Networked] public NetworkBool ContentsLocked { get; set; }

    [Header("Content Detection")]
    public BoxCollider contentZone;
    public LayerMask itemLayer;
    public int checkEveryTicks = 10;
    public Transform stampAnchor;

    private List<PackableItem> carried = new List<PackableItem>();

    public string GetInteractionPrompt()
    {
        PlayerGrab localGrab = FindLocalPlayerGrab();

        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            if (localGrab.HeldObject.TryGetComponent(out Stamp stamp))
            {
                if (!HasStamp && !stamp.IsAttached && stamp.destination != null)
                {
                    return $"Press [PACK] to put label ({stamp.destination.destiny}) on {boxName}";
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
        if (stampAnchor != null)
        {
            anchor = stampAnchor;
        }
        else
        {
            anchor = transform;
        }
        stamp.AttachToBox(anchor, Object);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || ContentsLocked)
        {
            return;
        }
        if (contentZone == null)
        {
            return;
        }
        if (Runner.Tick % Mathf.Max(1, checkEveryTicks) != 0)
        {
            return;
        }

        RefreshContents();
    }

    public void RefreshContents()
    {
        if (!HasStateAuthority || contentZone == null || ContentsLocked)
        {
            return;
        }

        Vector3 center = contentZone.transform.TransformPoint(contentZone.center);
        Vector3 halfExtents = Vector3.Scale(contentZone.size, contentZone.transform.lossyScale) * 0.5f;
        Collider[] hits = Physics.OverlapBox(center, halfExtents, contentZone.transform.rotation, itemLayer, QueryTriggerInteraction.Collide);

        HashSet<PackableItem> found = new HashSet<PackableItem>();
        List<int> ids = new List<int>();

        foreach (Collider hit in hits)
        {
            PackableItem item = hit.GetComponentInParent<PackableItem>();
            if (item == null || item.productData == null)
            {
                continue;
            }
            if (found.Contains(item))
            {
                continue;
            }

            GrabbableObject grabbable = item.GetComponent<GrabbableObject>();
            if (grabbable != null && grabbable.IsGrabbed)
            {
                continue;
            }

            found.Add(item);
            ids.Add(item.productData.id);
        }

        ids.Sort();

        carried.Clear();
        foreach (PackableItem item in found)
        {
            carried.Add(item);
        }

        if (SameContents(ids))
        {
            return;
        }

        PackedItemIDs.Clear();
        foreach (int id in ids)
        {
            if (PackedItemIDs.Count >= PackedItemIDs.Capacity)
            {
                break;
            }
            PackedItemIDs.Add(id);
        }
    }

    public void PrepareForDelivery()
    {
        if (!HasStateAuthority || ContentsLocked)
        {
            return;
        }

        RefreshContents();

        foreach (PackableItem item in carried)
        {
            if (item != null)
            {
                item.StartCarry(Object);
            }
        }
        ContentsLocked = true;
    }

    private bool SameContents(List<int> ids)
    {
        if (ids.Count != PackedItemIDs.Count)
        {
            return false;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            if (PackedItemIDs[i] != ids[i])
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

    public void BeginTransport()
    {
        if (!HasStateAuthority || ContentsLocked)
        {
            return;
        }
        RefreshContents();

        foreach (PackableItem item in carried)
        {
            if (item == null)
            {
                continue;
            }
            item.StartTransport(Object);
        }
    }

    public void EndTransport()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        foreach (PackableItem item in carried)
        {
            if (item == null)
            {
                continue;
            }
            item.StopTransport();
        }
    }

}