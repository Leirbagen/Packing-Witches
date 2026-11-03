using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : NetworkBehaviour
{
    [Networked] public NetworkBool IsGrabbed { get; set; }

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Render()
    {
        if (rb != null)
        {
            rb.isKinematic = IsGrabbed;
            rb.useGravity = !IsGrabbed;
        }
    }

    public void Grab(Transform handPoint)
    {
        Object.RequestStateAuthority();
        IsGrabbed = true;
        transform.SetParent(handPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        IsGrabbed = false;
        transform.SetParent(null);
    }
}