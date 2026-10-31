using Fusion;
using UnityEngine;

public class PlayerInteractor : NetworkBehaviour
{
    public Transform cameraTransform;
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;
    private IInteractable currentInteractable;

    private void Update()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        if (ShopTerminal.IsOpen) return;
        bool grabPressed = InputController.Instance.GetButtonDown(InputController.Input.GRAB);
        if (grabPressed)
        {
            var grab = GetComponentInParent<PlayerGrab>();
            if (grab == null)
            {
                grab = GetComponentInChildren<PlayerGrab>();
            }

            if (grab != null && grab.IsHolding)
            {
                grab.RequestDrop();
                return;
            }
        }
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * interactionDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            currentInteractable = hit.collider.GetComponentInParent<IInteractable>();

            if (currentInteractable != null)
            {
                Debug.Log($"<color=green>Looking at : {hit.collider.name}!</color> - " + currentInteractable.GetInteractionPrompt());

                if (grabPressed)
                {
                    currentInteractable.OnInteract(Runner.LocalPlayer);
                }
            }
            else
            {
                Debug.LogWarning($"<color=yellow>Hit with {hit.collider.name}, doesnt have IInteractable.</color>");
            }
        }
        else
        {
            currentInteractable = null;
        }
    }
}