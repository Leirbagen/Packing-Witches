using Fusion;
using UnityEngine;

public class Stamp : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnContentChanged))]
    public NetworkString<_64> Content { get; set; }

    [Networked] public NetworkBool IsAttached { get; set; }

    [Networked, OnChangedRender(nameof(OnParentBoxChanged))]
    public NetworkObject ParentBox { get; set; }

    public void SetContent(string content)
    {
        if (!HasStateAuthority)
        {
            return;
        }
        Content = content;
    }

    private void OnContentChanged()
    {
        Debug.Log("[Stamp] Content: " + Content);
    }

    public void AttachToBox(Transform anchor, NetworkObject boxObject)
    {
        if (!HasStateAuthority || anchor == null || boxObject == null)
        {
            return;
        }
        IsAttached = true;
        ParentBox = boxObject;

        StickToAnchor(anchor);
    }

    private void OnParentBoxChanged()
    {
        if (ParentBox != null && !HasStateAuthority)
        {
            Transform anchor = ParentBox.transform;

            if (ParentBox.TryGetComponent(out BoxContainer box) &&
                box.stampAnchor != null)
            {
                anchor = box.stampAnchor;
            }
            StickToAnchor(anchor);
        }
    }

    private void StickToAnchor(Transform anchor)
    {
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.enabled = false;
        }

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        if (TryGetComponent(out GrabbableObject grabbable))
        {
            grabbable.enabled = false;
        }

        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}