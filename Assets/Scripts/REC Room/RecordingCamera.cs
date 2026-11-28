using Fusion;
using TMPro;
using UnityEngine;

public class RecordingCamera : NetworkBehaviour, IInteractable
{
    public ProductDisplayZone displayZone;
    public float recordingDuration = 5f;

    public Camera tripodCamera;
    public GameObject playerHUD;
    public GameObject recCanvas;
    public TextMeshProUGUI timerText;
    public WorldPromptUI worldPrompt;

    [Networked] public NetworkBool IsRecording { get; set; }
    [Networked] public TickTimer RecordingTimer { get; set; }
    [Networked] public PlayerRef CurrentCameraman { get; set; }

    private Camera storedPlayerCamera;

    private void Awake()
    {
        if (worldPrompt == null)
        {
            worldPrompt = GetComponentInChildren<WorldPromptUI>();
        }
    }

    public string GetInteractionPrompt()
    {
        if (Object == null || !Object.IsValid)
        {
            return string.Empty;
        }

        if (IsRecording)
        {
            float time = RecordingTimer.RemainingTime(Runner) ?? 0f;
            return $"Recording... ({time:F0}s)";
        }

        if (displayZone != null)
        {
            PackableItem item = displayZone.GetDisplayedItem();
            if (item != null && item.productData != null)
            {
                return $"Press [F] to record promo for {item.productData.productName}";
            }
        }

        return "Place a product on the recording table first";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (Object == null || !Object.IsValid || IsRecording || displayZone == null)
        {
            return;
        }

        PackableItem item = displayZone.GetDisplayedItem();
        if (item != null)
        {
            RPC_StartRecording(interactor);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartRecording(PlayerRef interactor)
    {
        if (IsRecording)
        {
            return;
        }

        IsRecording = true;
        CurrentCameraman = interactor;
        RecordingTimer = TickTimer.CreateFromSeconds(Runner, recordingDuration);

        RPC_BroadcastRecordingState(true, interactor);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        if (IsRecording && RecordingTimer.Expired(Runner))
        {
            IsRecording = false;
            FinishRecording();
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
        {
            return;
        }

        if (IsRecording && recCanvas != null && recCanvas.activeSelf && timerText != null)
        {
            int seconds = Mathf.CeilToInt(RecordingTimer.RemainingTime(Runner) ?? 0f);
            timerText.text = $"REC 00:{seconds:00}";
        }
    }

    private void FinishRecording()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.AddRevenue(10);
        }

        RPC_BroadcastRecordingState(false, CurrentCameraman);
        CurrentCameraman = PlayerRef.None;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastRecordingState(NetworkBool isStarting, PlayerRef interactor)
    {
        if (worldPrompt != null)
        {
            if (isStarting)
            {
                worldPrompt.ShowTemporaryMessage("<color=#FF00FF>Camera is rolling!</color>");
            }
            else
            {
                worldPrompt.ShowTemporaryMessage("<color=#44FF44>Video uploaded! +$10</color>");
            }
        }

        if (interactor == Runner.LocalPlayer)
        {
            if (isStarting)
            {
                storedPlayerCamera = Camera.main;
                if (storedPlayerCamera != null)
                {
                    storedPlayerCamera.enabled = false;
                }

                if (tripodCamera != null)
                {
                    tripodCamera.gameObject.SetActive(true);
                }

                if (playerHUD != null)
                {
                    playerHUD.SetActive(false);
                }

                if (recCanvas != null)
                {
                    recCanvas.SetActive(true);
                }

                if (worldPrompt != null)
                {
                    worldPrompt.gameObject.SetActive(false);
                }
            }
            else
            {
                if (tripodCamera != null)
                {
                    tripodCamera.gameObject.SetActive(false);
                }

                if (storedPlayerCamera != null)
                {
                    storedPlayerCamera.enabled = true;
                }

                if (playerHUD != null)
                {
                    playerHUD.SetActive(true);
                }

                if (recCanvas != null)
                {
                    recCanvas.SetActive(false);
                }

                if (worldPrompt != null)
                {
                    worldPrompt.gameObject.SetActive(true);
                }
            }
        }
    }
}