using Fusion;
using UnityEngine;

public class PackableItem : NetworkBehaviour
{
    [Header("Product Reference")]
    public ProductData productData;
    [Networked] public NetworkBool IsPacked { get; set; }
    [Networked] public NetworkObject PackedParent { get; set; }
    [Networked] public Vector3 PackedLocalPos { get; set; }
    [Networked] public Quaternion PackedLocalRot { get; set; }

    private bool physicsDisabled;

    public void PackIntoBox(NetworkObject box, Vector3 worldPosition, Quaternion worldRotation)
    {
        PackedParent = box;
        PackedLocalPos = box.transform.InverseTransformPoint(worldPosition);
        PackedLocalRot = Quaternion.Inverse(box.transform.rotation) * worldRotation;
        IsPacked = true;
    }

/*
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
    */
    private void DisablePhysics()
    {
        physicsDisabled = true;

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }

    public override void Render()
    {
        if (!IsPacked || PackedParent == null)
        {
            return;
        } 

        if (!physicsDisabled)
        {
            DisablePhysics();
        } 
        transform.position = PackedParent.transform.TransformPoint(PackedLocalPos);
        transform.rotation = PackedParent.transform.rotation * PackedLocalRot;
    }
}