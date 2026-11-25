using Fusion;
using UnityEngine;

public class StampRoll : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 20;
    [SerializeField] private Transform visual;
    [SerializeField] private float minSize = 0.2f;
    [Networked, OnChangedRender(nameof(OnCapacityChanged))] 
    public int Remaining {get; set;}
    [Networked] public NetworkBool IsSeated { get; set; }
    [Networked] public NetworkObject SeatedIn { get; set; }
    private Vector3 baseScale;
    private Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if(visual == null)
        {
            visual =transform;
        }
        baseScale = visual.localScale;
    }

    public override void Spawned()
    {
        if(HasStateAuthority)
        {
            Remaining = maxCapacity;
        }
        UpdateVisual();
    }

    public bool HasPaper()
    {
        return Remaining > 0;
    }

    public bool TryConsume()
    {
        if(!HasStateAuthority)
        {
            return false;
        }
        if(Remaining <= 0)
        {
            return false;
        }
        Remaining = Remaining - 1;
        return true;
    }

    private void OnCapacityChanged()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float percent = (float)Remaining / Mathf.Max(1, maxCapacity);
        float factor = Mathf.Lerp(minSize, 1f, percent);
        visual.localScale = new Vector3(baseScale.x * factor, baseScale.y, baseScale.z * factor );
    }

    public void Seat(NetworkObject printer, Transform slot)
    {
        if (!HasStateAuthority)
        {
            return;
        }
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(slot.position, slot.rotation);
        SeatedIn = printer;
        IsSeated = true;
    }

    public void Unseat()
    {
        if (!HasStateAuthority)
        {
            return;
        }
        IsSeated = false;
        SeatedIn = null;
    }

    public override void Render()
    {
        if (!IsSeated || SeatedIn == null)
        {
            return;
        }

        GrabbableObject grabbable = GetComponent<GrabbableObject>();
        if (grabbable != null && grabbable.IsGrabbed)
        {
            return;
        }

        StampPrinter printer = SeatedIn.GetComponent<StampPrinter>();
        if (printer != null && printer.rollSlot != null)
        {
            transform.SetPositionAndRotation(printer.rollSlot.position, printer.rollSlot.rotation);
        }
    }
    
}
