
using Fusion;
using UnityEngine;

public class PackableItem : NetworkBehaviour
{
    [Header("Product Reference")]
    public ProductData productData;

    [Networked] public NetworkObject CarrierBox { get; set; }
    [Networked] public Vector3 CarriedLocalPos { get; set; }
    [Networked] public Quaternion CarriedLocalRot { get; set; }

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void StartCarry(NetworkObject box)
    {
        if (!HasStateAuthority || box == null || rb == null)
        {
            return;
        }
            
        if (CarrierBox == box)
        {
            return;
        }

        if (CarrierBox != null)
        {
            return;
        }

        CarriedLocalPos = box.transform.InverseTransformPoint(transform.position);

        CarriedLocalRot = Quaternion.Inverse(box.transform.rotation) * transform.rotation;

        CarrierBox = box;

        rb.isKinematic = true;
        rb.useGravity = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void StopCarry()
    {
        if (!HasStateAuthority || rb == null)
        {
            return;
        }

        CarrierBox = null;

        GrabbableObject grabbable = GetComponent<GrabbableObject>();

        if (grabbable != null && grabbable.IsGrabbed)
        {
            return;
        }
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || rb == null)
        {
            return;
        }

        GrabbableObject grabbable = GetComponent<GrabbableObject>();

        bool held = grabbable != null && grabbable.IsGrabbed;

        if (CarrierBox != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else if (!held)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    public override void Render()
    {
        if (CarrierBox == null)
        {
            return;
        }
        transform.SetPositionAndRotation(CarrierBox.transform.TransformPoint(CarriedLocalPos), CarrierBox.transform.rotation * CarriedLocalRot);
    }
}
