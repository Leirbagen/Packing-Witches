using Fusion;
using UnityEngine;

public class PackableItem : NetworkBehaviour
{
    [Header("Product Reference")]
    public ProductData productData;

    [Networked] public NetworkBool IsPacked { get; set; }

    public void PackIntoBox(Transform contentParent, Vector3 worldPosition, Quaternion worldRotation)
    {
        IsPacked = true;

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

        transform.SetParent(contentParent);
        transform.position = worldPosition;
        transform.rotation = worldRotation;
    }
}