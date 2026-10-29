using Fusion;
using UnityEngine;

[RequireComponent(typeof(GrabbableObject))]
public class DeliverablePackage : NetworkBehaviour
{
    public int rewardValue = 15;
    [Networked] public NetworkBool IsDelivered { get; set; }

    public GrabbableObject Grabbable { get; private set; }

    private void Awake()
    {
        Grabbable = GetComponent<GrabbableObject>();
    }
}