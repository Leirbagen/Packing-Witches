using UnityEngine;
using Fusion;

public class PlayerController : NetworkBehaviour
{
    private NetworkCharacterController characterController;
    [SerializeField] private float velocity = 5;
    [SerializeField] private float runVelocity = 40; 
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 20f;
    [SerializeField] private float staminaRegenPerSecond = 15f;
    [SerializeField] private float regenDelay = 1.5f;
    [SerializeField] private float exhaustedRecover = 25f;
    [Networked] private bool wasGrounded { get; set; }
    [Networked] public float Stamina { get; set; }
    [Networked] public NetworkBool IsExhausted { get; set; }
    [Networked] private TickTimer regenTimer { get; set; }
    [Networked] public NetworkBool IsSprinting { get; set; }
    public float StaminaNormalized => Stamina / maxStamina;
    public AudioSource playerAudio;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public GameObject cameraLook;
    [Networked] public float NetworkYaw { get; set; }
    private float pendingJumpForce = 0f;
    private void Awake()
    {
        characterController = GetComponent<NetworkCharacterController>();
    }
    public override void Spawned()
    {
        base.Spawned();

        Camera camComponent = cameraLook.GetComponent<Camera>();
        AudioListener audioListener = cameraLook.GetComponent<AudioListener>();

        if (HasInputAuthority)
        {
            if (camComponent != null)
            {
                camComponent.enabled = true;
            } 
            if (audioListener != null)
            {
                audioListener.enabled = true;
            } 
            cameraLook.tag = "MainCamera";
        }
        else
        {
            if (camComponent != null)
            {
                camComponent.enabled = false;
            } 
            if (audioListener != null)
            {
                audioListener.enabled = false;
            } 
        }
        if (Object.HasStateAuthority)
        {
            wasGrounded = true; // could be useful
            Stamina = maxStamina;
        }
        if (HasInputAuthority)
        {
            var staminaUI = FindFirstObjectByType<StaminaUI>();
            if (staminaUI != null)
            {
                staminaUI.Bind(this);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!Object.HasStateAuthority && !Object.HasInputAuthority)
        {
            return;
        }
        
        Vector3 moveDirection = Vector3.zero;
        bool jumpPressed = false;
        bool runPressed = false;
        bool isRunning = false;

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
        if (pendingJumpForce > 0f)
        {
            if (characterController.Grounded)
            {
                characterController.Jump();
            }
            characterController.Velocity = new Vector3(characterController.Velocity.x, pendingJumpForce, characterController.Velocity.z);
            pendingJumpForce = 0f;
        }
        
        bool isInAir = !characterController.Grounded;
        
        bool wantsToRun = false;
        if (runPressed == true)
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                wantsToRun = true;
            }
        }
        isRunning = false;
        if (wantsToRun == true)
        {
            if (IsExhausted == false)
            {
                if (Stamina > 0f)
                {
                    isRunning = true;
                }
            }
        }
        if(isRunning == true)
        {
            characterController.maxSpeed = runVelocity;
        }
        else
        {
            characterController.maxSpeed = velocity;
        }
        characterController.Move(moveDirection);
        
        
        UpdateStamina(isRunning);
        if (HasStateAuthority)
        {
            IsSprinting = isRunning;
        }

        if (jumpPressed && characterController.Grounded)
        {
            characterController.Jump();
            if (Runner.IsForward)
            {
                if (playerAudio != null && jumpSound != null)
                {
                    playerAudio.PlayOneShot(jumpSound);
                }
            }
            
        }
        

        transform.rotation = Quaternion.Euler(0f, NetworkYaw, 0f);
        /*
        if (!characterController.Grounded)
        {
            if (transform.position.y > highestYPosition)
            {
                highestYPosition = transform.position.y;
            }
        }
        else
        {
            if (!wasGrounded)
            {
                if (Runner.IsForward)
                {
                    if (playerAudio != null && landSound != null)
                    {
                        playerAudio.PlayOneShot(landSound);
                    }
                }
                float fallDistance = highestYPosition - transform.position.y;
            }
            highestYPosition = transform.position.y;
        }
        */
        wasGrounded = characterController.Grounded;
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
