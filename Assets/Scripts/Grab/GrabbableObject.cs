using Fusion;
using UnityEngine;

public class GrabbableObject : NetworkBehaviour, IInteractable
{
    private Rigidbody rb;
    [Networked] public NetworkBool IsGrabbed { get; set; }
    [Networked] public NetworkObject CurrentHolder { get; set; }

        public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsGrabbed;
    }
/*
    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab.RequestGrab(this);
    }
*/

    public string GetInteractionPrompt()
    {
        if (IsGrabbed)
        {
            return "";
        }
        else
        {
            return "Press F to grab";
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnInteract(PlayerRef interactor)
    {
        Debug.Log("[Grab] 1. OnInteract llamado. IsGrabbed=" + IsGrabbed);
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
            Debug.LogWarning("[Grab] 2. TryGetPlayerObject falló, busco por InputAuthority");
            foreach (PlayerGrab g in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
            {
                if (g.Object.InputAuthority == interactor)
                {
                    grab = g;
                    break;
                }
            }
        }

        if (grab == null)
        {
            Debug.LogError("[Grab] 3. No encontré el PlayerGrab del jugador " + interactor);
            return;
        }

        Debug.Log("[Grab] 4. Pidiendo agarrar " + name);
        grab.RequestGrab(this);
    }
    
    public override void Render()
    {
        if (IsGrabbed && CurrentHolder != null)
        {
            PlayerGrab grabber = CurrentHolder.GetComponent<PlayerGrab>();
            if (grabber != null && grabber.handPoint != null)
            {
                transform.position = grabber.handPoint.position;
                transform.rotation = grabber.handPoint.rotation;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            /*
            if (IsGrabbed && CurrentHolder != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            */
            bool held = IsGrabbed && CurrentHolder != null;
            rb.isKinematic = held;
            rb.useGravity = !held;
        }
    }
}