using Fusion;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
}

[RequireComponent(typeof(PlayerGrab))]
public class PlayerInteractor : NetworkBehaviour
{
    public Transform rayOrigin;
    public float interactDistance = 3f;
    public LayerMask interactLayer;

    public PlayerGrab Grab { get; private set; }
    private bool wasPressed;

    public override void Spawned()
    {
        Grab = GetComponent<PlayerGrab>();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
        {
            return;
        } 

        bool pressed = data.buttons.IsSet(InputButton.grab);
        if (pressed && !wasPressed && Runner.IsForward)
        {
            HandlePress();
        }
        wasPressed = pressed;
    }

    private void HandlePress()
    {
        if (!HasInputAuthority || rayOrigin == null)
        {
            return;
        } 

        if (Grab.IsHolding)
        {
            Grab.RequestDrop();
            return;
        }

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayer))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract(this))
            {
                interactable.Interact(this);
            }
        }
    }
}
