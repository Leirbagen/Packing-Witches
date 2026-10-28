using Fusion;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    public Transform handPoint;      
    public float grabRadius = 2f;   
    public LayerMask grabbableLayer; 
    [Networked] public NetworkBool IsHolding { get; set; }
    [Networked] public GrabbableObject HeldObject { get; set; }
    private bool wasGrabPressed;

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            bool isGrabPressed = data.buttons.IsSet(InputButton.FIRE_1);
            if (isGrabPressed && !wasGrabPressed)
            {
                if (!IsHolding) TryGrab();
                else TryDrop();
            }
            wasGrabPressed = isGrabPressed;
        }
    }

    private void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(handPoint.position, grabRadius, grabbableLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out GrabbableObject grabbable))
            {
                if (!grabbable.IsGrabbed)
                {
                    RPC_RequestGrab(grabbable.Object);
                    break;
                }
            }
        }
    }

    private void TryDrop()
    {
        if (HeldObject != null)
        {
            RPC_RequestDrop(HeldObject.Object);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestGrab(NetworkObject obj)
    {
        if (IsHolding) return;

        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null && !grabbable.IsGrabbed)
        {
            grabbable.IsGrabbed = true;
            grabbable.CurrentHolder = this.Object;

            HeldObject = grabbable;
            IsHolding = true;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestDrop(NetworkObject obj)
    {
        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null)
        {
            grabbable.IsGrabbed = false;
            grabbable.CurrentHolder = null;
        }

        HeldObject = null;
        IsHolding = false;
    }
}