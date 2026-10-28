using Fusion;

using UnityEngine;

public class CameraController : NetworkBehaviour
{
    [SerializeField] private float sensibility = 200f;
    public Transform playerBody;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Euler(0f, data.look.x, 0f);
            }
            transform.localRotation = Quaternion.Euler(data.look.y, 0f, 0f);
        }
    }
}
