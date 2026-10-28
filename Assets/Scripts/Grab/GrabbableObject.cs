using Fusion;
using UnityEngine;

public class GrabbableObject : NetworkBehaviour
{
    private Rigidbody rb;
    [Networked] public NetworkBool IsGrabbed { get; set; }
    [Networked] public NetworkObject CurrentHolder { get; set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (IsGrabbed && CurrentHolder != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                PlayerGrab grabber = CurrentHolder.GetComponent<PlayerGrab>();
                if (grabber != null && grabber.handPoint != null)
                {
                    rb.MovePosition(grabber.handPoint.position);
                    rb.MoveRotation(grabber.handPoint.rotation);
                }
            }
            else
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }
    }
}