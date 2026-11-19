
using Fusion;
using UnityEngine;

public class PackableItem : NetworkBehaviour
{
    [Header("Product Reference")]
    public ProductData productData;
    [Networked] public NetworkObject CarrierBox { get; set; }
    [Networked] public Vector3 CarriedLocalPos { get; set; }
    [Networked] public Quaternion CarriedLocalRot { get; set; }
    [Networked] public NetworkObject TransportBox { get; set; }
    [Networked] public Vector3 TransportLocalPos { get; set; }
    [Networked] public Quaternion TransportLocalRot { get; set; }

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        if (CarrierBox != null)
        {
            Vector3 targetPosition =
                CarrierBox.transform.TransformPoint(CarriedLocalPos);

            Quaternion targetRotation =
                CarrierBox.transform.rotation * CarriedLocalRot;

            if (rb != null)
            {
                rb.position = targetPosition;
                rb.rotation = targetRotation;
            }

            return;
        }

        if (TransportBox != null)
        {
            Vector3 targetPosition =
                TransportBox.transform.TransformPoint(TransportLocalPos);

            Quaternion targetRotation =
                TransportBox.transform.rotation * TransportLocalRot;

            if (rb != null)
            {
                rb.position = targetPosition;
                rb.rotation = targetRotation;
            }
        }
    }

    public void StartCarry(NetworkObject box)
    {
        if (!HasStateAuthority || box == null || rb == null)
        {
            return;
        }
        if (CarrierBox != null)
        {
            return;
        }
        TransportBox = null;

        CarriedLocalPos = box.transform.InverseTransformPoint(transform.position);
        CarriedLocalRot = Quaternion.Inverse(box.transform.rotation) * transform.rotation;

        CarrierBox = box;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.isKinematic = true;
        rb.useGravity = false;
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

    public void StartTransport(NetworkObject box)
    {
        if (!HasStateAuthority || box == null || rb == null)
        {
            return;
        }

        if (CarrierBox != null || TransportBox != null)
        {
            return;
        }
        TransportLocalPos = box.transform.InverseTransformPoint(transform.position);
        TransportLocalRot = Quaternion.Inverse(box.transform.rotation) * transform.rotation;
        TransportBox = box;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public void StopTransport()
    {
        if (!HasStateAuthority || rb == null)
        {
            return;
        }
        TransportBox = null;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

/*
    public override void Render()
    {
        if (CarrierBox != null)
        {
            transform.SetPositionAndRotation(CarrierBox.transform.TransformPoint(CarriedLocalPos), CarrierBox.transform.rotation * CarriedLocalRot);
            return;
        }
        if (TransportBox != null)
        {
            Vector3 targetPosition = TransportBox.transform.TransformPoint(TransportLocalPos);
            Quaternion targetRotation = TransportBox.transform.rotation * TransportLocalRot;

            if (rb != null)
            {
                rb.position = targetPosition;
                rb.rotation = targetRotation;
            }
            else
            {
                transform.SetPositionAndRotation(targetPosition, targetRotation);
            }
        }
    }
    */
}
