using Fusion;
using UnityEngine;

public class Stamp : NetworkBehaviour
{
    public OrderDestiny destination;
    [Networked] public NetworkBool IsAttached { get; set; }

    public void AttachToBox(Transform anchor)
    {
        IsAttached = true;
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.enabled = false;
        }
        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
