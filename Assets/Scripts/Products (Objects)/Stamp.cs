using Fusion;
using UnityEngine;

public class Stamp : NetworkBehaviour
{
    public OrderDestiny destination;
    [Networked] public NetworkBool IsAttached { get; set; }

    [Networked, OnChangedRender(nameof(OnParentBoxChanged))]
    public NetworkObject ParentBox { get; set; }

    public void AttachToBox(Transform anchor, NetworkObject boxObject)
    {
        IsAttached = true;
        ParentBox = boxObject;

        ApplyPartnerLogic(anchor);
    }

    private void OnParentBoxChanged()
    {
        if (ParentBox != null && !HasStateAuthority)
        {
            Transform anchor = ParentBox.transform;
            if (ParentBox.TryGetComponent(out BoxContainer box) && box.stampAnchor != null)
            {
                anchor = box.stampAnchor;
            }
            ApplyPartnerLogic(anchor);
        }
    }

    private void ApplyPartnerLogic(Transform anchor)
    {
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