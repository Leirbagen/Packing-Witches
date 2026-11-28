using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController characterController;

    [SerializeField] private float velocity = 5f;
    [SerializeField] private float runVelocity = 9f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 20f;
    [SerializeField] private float staminaRegenPerSecond = 15f;
    [SerializeField] private float regenDelay = 1.5f;
    [SerializeField] private float exhaustedRecover = 25f;

    [Networked] public float Stamina { get; set; }
    [Networked] public NetworkBool IsExhausted { get; set; }
    [Networked] private TickTimer regenTimer { get; set; }
    [Networked] public NetworkBool IsSprinting { get; set; }
    [Networked] public float NetworkYaw { get; set; }

    public float StaminaNormalized => Stamina / Mathf.Max(1f, maxStamina);
    public AudioSource playerAudio;
    public AudioClip jumpSound;
    public GameObject cameraLook;

    private void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
        characterController.acceleration = 100f;
        characterController.braking = 100f;
    }

    public override void Spawned()
    {
        base.Spawned();

        if (cameraLook != null)
        {
            Camera camComponent = cameraLook.GetComponent<Camera>();
            AudioListener audioListener = cameraLook.GetComponent<AudioListener>();

            if (camComponent != null)
            {
                camComponent.enabled = HasInputAuthority;
            }

            if (audioListener != null)
            {
                audioListener.enabled = HasInputAuthority;
            }

            if (HasInputAuthority)
            {
                cameraLook.tag = "MainCamera";
            }
        }

        if (HasStateAuthority)
        {
            Stamina = maxStamina;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority && !HasInputAuthority)
        {
            return;
        }

        Vector3 moveDirection = Vector3.zero;
        bool jumpPressed = false;
        bool runPressed = false;

        if (GetInput(out NetworkInputData data))
        {
            NetworkYaw = data.look.x;
            Quaternion yawRotation = Quaternion.Euler(0f, NetworkYaw, 0f);
            Vector3 cameraForward = yawRotation * Vector3.forward;
            Vector3 cameraRight = yawRotation * Vector3.right;
            moveDirection = (cameraRight * data.move.x) + (cameraForward * data.move.y);
            moveDirection.Normalize();
            jumpPressed = data.buttons.IsSet(InputButton.jump);
            runPressed = data.buttons.IsSet(InputButton.run);
        }

        bool isRunning = runPressed && moveDirection.sqrMagnitude > 0.01f && !IsExhausted && Stamina > 0f;

        if (isRunning)
        {
            characterController.maxSpeed = runVelocity;
        }
        else
        {
            characterController.maxSpeed = velocity;
        }

        if (moveDirection.sqrMagnitude < 0.01f)
        {
            characterController.Velocity = new Vector3(0f, characterController.Velocity.y, 0f);
        }

        characterController.Move(moveDirection);

        if (!jumpPressed && characterController.Velocity.y > characterController.jumpImpulse)
        {
            characterController.Velocity = new Vector3(characterController.Velocity.x, 0f, characterController.Velocity.z);
        }

        UpdateStamina(isRunning);

        if (HasStateAuthority)
        {
            IsSprinting = isRunning;
        }

        if (jumpPressed && characterController.Grounded)
        {
            characterController.Jump();
            if (Runner.IsForward && playerAudio != null && jumpSound != null)
            {
                playerAudio.PlayOneShot(jumpSound);
            }
        }

        transform.rotation = Quaternion.Euler(0f, NetworkYaw, 0f);
    }

    private void UpdateStamina(bool isRunning)
    {
        if (isRunning)
        {
            Stamina = Mathf.Max(0f, Stamina - staminaDrainPerSecond * Runner.DeltaTime);
            regenTimer = TickTimer.CreateFromSeconds(Runner, regenDelay);

            if (Stamina <= 0f)
            {
                IsExhausted = true;
            }
        }
        else if (regenTimer.ExpiredOrNotRunning(Runner))
        {
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * Runner.DeltaTime);

            if (IsExhausted && Stamina >= exhaustedRecover)
            {
                IsExhausted = false;
            }
        }
    }
}