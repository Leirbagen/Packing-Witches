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

            if (IsGrabbed) Debug.Log($"[Grab] 6. Following hand. holder={CurrentHolder}");
        }
    }
}