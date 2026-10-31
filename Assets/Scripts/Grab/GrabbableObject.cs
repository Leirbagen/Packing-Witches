using Fusion;
using UnityEngine;

public class GrabbableObject : NetworkBehaviour
{
    private Rigidbody rb;
    [Networked] public NetworkBool IsGrabbed { get; set; }
    [Networked] public NetworkObject CurrentHolder { get; set; }

        public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsGrabbed;
    }

    public void Interact(PlayerInteractor interactor)
    {
        //interactor.Grab.RequestGrab(this);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    public override void Render()
    {
        if (IsGrabbed && CurrentHolder != null)
        {
            PlayerGrab grabber = CurrentHolder.GetComponent<PlayerGrab>();
            if (grabber != null && grabber.handPoint != null)
            {
                transform.position = grabber.handPoint.position;
                transform.rotation = grabber.handPoint.rotation;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (IsGrabbed && CurrentHolder != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}