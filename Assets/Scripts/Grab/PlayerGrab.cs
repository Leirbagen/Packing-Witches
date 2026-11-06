using Fusion;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    public Transform handPoint;
    [Networked] public NetworkBool IsHolding { get; set; }
    [Networked] public GrabbableObject HeldObject { get; set; }

    public void RequestGrab(GrabbableObject grabbable)
    {
        if (!IsHolding)
        {
            RPC_RequestGrab(grabbable.Object);
        }
    }

    public void RequestDrop()
    {
        if (HeldObject != null)
        {
            if (Object.HasStateAuthority)
            {
                DropItemInternal(HeldObject.Object);
            }
            else
            {
                RPC_RequestDrop(HeldObject.Object);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestGrab(NetworkObject obj)
    {
        if (IsHolding)
        {
            return;
        }

        if (obj.TryGetComponent(out DeliverablePackage package))
        {
            if (package.IsDelivered) return;
        }

        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null && !grabbable.IsGrabbed)
        {
            grabbable.IsGrabbed = true;
            grabbable.CurrentHolder = this.Object;

            HeldObject = grabbable;
            IsHolding = true;

            ToggleCollisions(grabbable, true);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDrop(NetworkObject obj)
    {
        DropItemInternal(obj);
    }

    private void DropItemInternal(NetworkObject obj)
    {
        if (obj == null) return;

        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null)
        {
            ToggleCollisions(grabbable, false);
            grabbable.IsGrabbed = false;
            grabbable.CurrentHolder = null;
        }

        HeldObject = null;
        IsHolding = false;
    }

    private void ToggleCollisions(GrabbableObject grabbable, bool ignore)
    {
        Collider playerCollider = GetComponentInChildren<Collider>();
        if (playerCollider == null)
        {
            playerCollider = GetComponentInParent<Collider>();
        }

        Collider itemCollider = grabbable.GetComponent<Collider>();

        if (playerCollider != null && itemCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, itemCollider, ignore);
        }
    }
}