using Fusion;
using UnityEngine;

public class PlayerGrab : NetworkBehaviour
{
    public Transform rayOrigin;
    public float grabDistance = 3f;
    public Transform handPoint;      
    
    public LayerMask grabbableLayer; 
    [Networked] public NetworkBool IsHolding { get; set; }
    [Networked] public GrabbableObject HeldObject { get; set; }
    private bool wasGrabPressed;

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            bool isGrabPressed = data.buttons.IsSet(InputButton.grab);
            if (isGrabPressed && !wasGrabPressed && Runner.IsForward)
            {
                if (!IsHolding)
                {
                    TryGrab();
                }
                else
                {
                    TryDrop();
                }
            }
            wasGrabPressed = isGrabPressed;
        }
    }

    private void TryGrab()
    {
        if(!HasInputAuthority || rayOrigin == null)
        {
            return;
        }
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if(Physics.Raycast(ray, out RaycastHit hit, grabDistance, grabbableLayer))
        {
            var grabbable = hit.collider.GetComponent<GrabbableObject>();
            if(grabbable != null && !grabbable.IsGrabbed)
            {
                RPC_RequestGrab(grabbable.Object);
            }
        }
        /*
        Collider[] hits = Physics.OverlapSphere(handPoint.position, grabRadius, grabbableLayer);
        Debug.Log($"[Grab] 2. Colliders encontrados: {hits.Length}");
        foreach (var hit in hits)
        {
            bool ok = hit.TryGetComponent(out GrabbableObject g);
            Debug.Log($"[Grab] 3. {hit.name} tiene GrabbableObject: {ok}");
            if (hit.TryGetComponent(out GrabbableObject grabbable))
            {
                if (!grabbable.IsGrabbed)
                {
                    RPC_RequestGrab(grabbable.Object);
                    break;
                }
            }
        }
        */
    }

    private void TryDrop()
    {
        if(!HasStateAuthority)
        {
            return;
        }
        if (HeldObject != null)
        {
            RPC_RequestDrop(HeldObject.Object);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestGrab(NetworkObject obj)
    {
        if (IsHolding)
        {
            return;
        }

        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null && !grabbable.IsGrabbed)
        {
            grabbable.IsGrabbed = true;
            grabbable.CurrentHolder = this.Object;

            HeldObject = grabbable;
            IsHolding = true;

        }
        /*
        Debug.Log($"[Grab] 4. RPC llegó. obj={obj}, IsHolding={IsHolding}");
        if (IsHolding) return;

        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        if (grabbable != null && !grabbable.IsGrabbed)
        {
            grabbable.IsGrabbed = true;
            grabbable.CurrentHolder = this.Object;

            HeldObject = grabbable;
            IsHolding = true;
            Debug.Log($"[Grab] 5. IsGrabbed={grabbable.IsGrabbed}, " +
          $"objStateAuth={grabbable.Object.HasStateAuthority}, " +
          $"IsHolding={IsHolding}");
        }
        */
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