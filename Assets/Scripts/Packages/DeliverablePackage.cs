
using Fusion;
using UnityEngine;

[RequireComponent(typeof(GrabbableObject))]
public class DeliverablePackage : NetworkBehaviour
{
    public int rewardValue = 15;

    [Networked, OnChangedRender(nameof(OnDeliveredChanged))]
    public NetworkBool IsDelivered { get; set; }

    public GrabbableObject Grabbable { get; private set; }
    private Rigidbody rb;

    private void Awake()
    {
        Grabbable = GetComponent<GrabbableObject>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnDeliveredChanged()
    {
        if (IsDelivered)
        {
            WorldPromptUI[] prompts = GetComponentsInChildren<WorldPromptUI>(true);
            foreach (WorldPromptUI prompt in prompts)
            {
                prompt.gameObject.SetActive(false);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsDelivered)
        {
            return;
        }
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
