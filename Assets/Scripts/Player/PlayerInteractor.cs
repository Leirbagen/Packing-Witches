using Fusion;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    public Transform cameraTransform;
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;

    private IInteractable currentInteractable;
    private PlayerGrab playerGrab;

    private void Awake()
    {
        playerGrab = GetComponentInParent<PlayerGrab>();
        if (playerGrab == null)
        {
            playerGrab = GetComponentInChildren<PlayerGrab>();
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || !HasInputAuthority)
        {
            return;
        }

        if (ShopTerminal.IsOpen || InputController.Instance == null || cameraTransform == null)
        {
            return;
        }

        bool grabPressed = InputController.Instance.GetButtonDown(InputController.Input.GRAB);
        bool packPressed = InputController.Instance.GetButtonDown(InputController.Input.PACK);
        bool restPressed = InputController.Instance.GetButtonDown(InputController.Input.REST);

        if (grabPressed && playerGrab != null && playerGrab.IsHolding)
        {
            playerGrab.RequestDrop();
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactableLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit validHit = default;
        bool hasValidHit = false;

        foreach (RaycastHit hit in hits)
        {
            if (playerGrab != null && playerGrab.IsHolding && playerGrab.HeldObject != null)
            {
                if (hit.collider.transform.IsChildOf(playerGrab.HeldObject.transform) || hit.collider.gameObject == playerGrab.HeldObject.gameObject)
                {
                    continue;
                }
            }

            if (hit.collider.transform.IsChildOf(transform.root))
            {
                continue;
            }

            validHit = hit;
            hasValidHit = true;
            break;
        }

        if (hasValidHit)
        {
            currentInteractable = validHit.collider.GetComponentInParent<IInteractable>();
            GrabbableObject grabbable = validHit.collider.GetComponentInParent<GrabbableObject>();

            if (currentInteractable != null)
            {
                string prompt = currentInteractable.GetInteractionPrompt();
                if (!string.IsNullOrEmpty(prompt))
                {
                    bool isRestZone = currentInteractable is RestZone;
                    bool usesPackKey = currentInteractable is BoxContainer
                        || currentInteractable is CraftingTable
                        || currentInteractable is CatController
                        || currentInteractable is CatBowl
                        || currentInteractable is RecordingCamera
                        || currentInteractable is StampPrinter;

                    if (isRestZone && restPressed)
                    {
                        currentInteractable.OnInteract(Runner.LocalPlayer);
                        return;
                    }
                    else if (usesPackKey && packPressed)
                    {
                        currentInteractable.OnInteract(Runner.LocalPlayer);
                        return;
                    }
                    else if (!isRestZone && !usesPackKey && grabPressed)
                    {
                        currentInteractable.OnInteract(Runner.LocalPlayer);
                        return;
                    }
                }
            }
            else
            {
                currentInteractable = null;
            }

            if (playerGrab != null && !playerGrab.IsHolding && grabbable != null && grabPressed)
            {
                playerGrab.RequestGrab(grabbable);
            }
        }
        else
        {
            currentInteractable = null;
        }
    }
}