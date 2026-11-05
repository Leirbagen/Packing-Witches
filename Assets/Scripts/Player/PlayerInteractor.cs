using Fusion;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Interaction Settings")]
    public Transform cameraTransform;
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;
    private IInteractable currentInteractable;

    private void Update()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        if (ShopTerminal.IsOpen) return;

        bool grabPressed = InputController.Instance.GetButtonDown(InputController.Input.GRAB);
        bool packPressed = InputController.Instance.GetButtonDown(InputController.Input.PACK);

        PlayerGrab playerGrab = GetComponentInParent<PlayerGrab>();
        if (playerGrab == null)
        {
            playerGrab = GetComponentInChildren<PlayerGrab>();
        }

        if (grabPressed && playerGrab != null && playerGrab.IsHolding)
        {
            playerGrab.RequestDrop();
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactionDistance, Color.red);

        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactableLayer);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        RaycastHit validHit = default;
        bool hasValidHit = false;

        foreach (var hit in hits)
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
                    Debug.Log($"<color=green>Looking at : {validHit.collider.name}!</color> - " + prompt);

                    bool isBoxContainer = currentInteractable is BoxContainer;

                    if (isBoxContainer && packPressed)
                    {
                        currentInteractable.OnInteract(Runner.LocalPlayer);
                        return;
                    }
                    else if (!isBoxContainer && grabPressed)
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

            if (playerGrab != null && !playerGrab.IsHolding && grabbable != null)
            {
                if (grabPressed)
                {
                    playerGrab.RequestGrab(grabbable);
                }
            }
        }
        else
        {
            currentInteractable = null;
        }
    }
}