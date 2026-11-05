using Fusion;
using UnityEngine;

public class PackableItem : NetworkBehaviour
{
    [Header("Item Configuration")]
    public int itemSizeCost = 1;
    [Networked] public NetworkBool IsPacked { get; set; }

    public void PackIntoBox(Transform contentParent, int loadIndex)
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
        transform.localPosition = new Vector3(0, 0.05f * loadIndex, 0);
        transform.localRotation = Quaternion.identity;
    }
}