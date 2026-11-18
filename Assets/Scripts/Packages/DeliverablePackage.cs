
using Fusion;
using UnityEngine;

[RequireComponent(typeof(GrabbableObject))]
public class DeliverablePackage : NetworkBehaviour
{
    public int rewardValue = 15;

    [Networked]
    public NetworkBool IsDelivered { get; set; }
    public GrabbableObject Grabbable { get; private set; }
    private Rigidbody rb;

    private void Awake()
    {
        Grabbable = GetComponent<GrabbableObject>();
        rb = GetComponent<Rigidbody>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsDelivered)
        {
            return;
        }
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
