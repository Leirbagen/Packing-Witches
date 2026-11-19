using Fusion;
using UnityEngine;

public class GrabbableObject : NetworkBehaviour, IInteractable
{
    private Rigidbody rb;

    [Networked] public NetworkBool IsGrabbed { get; set; }
    [Networked] public NetworkObject CurrentHolder { get; set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsGrabbed;
    }

    public string GetInteractionPrompt()
    {
        if (IsGrabbed)
        {
            return "";
        }
        else
        {
            return "Press E to grab";
        }
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsGrabbed)
        {
            return;
        }
        PlayerGrab grab = null;

        if (Runner.TryGetPlayerObject(interactor, out NetworkObject playerObj))
        {
            grab = playerObj.GetComponent<PlayerGrab>();
        }

        if (grab == null)
        {
            foreach (PlayerGrab g in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
            {
                if (g.Object != null && g.Object.InputAuthority == interactor)
                {
                    grab = g;
                    break;
                }
            }
        }

        if (grab == null)
        {
            Debug.LogError("[Grab] PlayerGrab not found: " + interactor);
            return;
        }

        grab.RequestGrab(this);
    }

    public override void Render()
    {
        if (!IsGrabbed || CurrentHolder == null)
        {
            return;
        }
        PlayerGrab grabber = CurrentHolder.GetComponent<PlayerGrab>();

        if (grabber != null && grabber.handPoint != null)
        {
            transform.SetPositionAndRotation(grabber.handPoint.position, grabber.handPoint.rotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || rb == null)
        {
            return;
        }
        bool held = IsGrabbed && CurrentHolder != null;

        rb.isKinematic = held;
        rb.useGravity = !held;
    }
}