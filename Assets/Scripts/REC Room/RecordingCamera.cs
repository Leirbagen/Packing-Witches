using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordingCamera : NetworkBehaviour, IInteractable
{
    public ProductDisplayZone displayZone;
    public float recordingDuration = 5f;

    [Header("Cinematics")]
    public Camera tripodCamera;
    private Camera storedPlayerCamera;

    [Header("UI Settings")]
    public GameObject playerHUD;
    public GameObject recCanvas;
    public TextMeshProUGUI timerText;

    [Networked] public NetworkBool IsRecording { get; set; }
    [Networked] public TickTimer RecordingTimer { get; set; }
    [Networked] public PlayerRef CurrentCameraman { get; set; }

    public string GetInteractionPrompt()
    {
        if (IsRecording)
        {
            float time = RecordingTimer.RemainingTime(Runner) ?? 0f;
            return $"Recording... ({time:F0}s)";
        }

        PackableItem item = displayZone.GetDisplayedItem();

        if (item)
        {
            return $"Press [F] to record promo for {item.productData.productName}";
        }

        return "Place a product on the recording table first";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsRecording)
        {
            return;
        }

        PackableItem item = displayZone.GetDisplayedItem();

        if (item)
        {
            RPC_StartRecording(interactor);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartRecording(PlayerRef interactor)
    {
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
        if (IsRecording && recCanvas.activeSelf)
        {
            float time = RecordingTimer.RemainingTime(Runner) ?? 0f;
            timerText.text = $"REC 00:0{time:F0}";
        }
    }

    private void FinishRecording()
    {
        EconomyManager.Instance.AddRevenue(10);

        RPC_BroadcastRecordingState(false, CurrentCameraman);
        CurrentCameraman = PlayerRef.None;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_BroadcastRecordingState(bool isStarting, PlayerRef interactor)
    {
        if (isStarting)
        {
            Debug.Log("<color=magenta>[Recording] Camera is rolling </color>");
        }
        else
        {
            Debug.Log("<color=magenta>[Recording] Video uploaded </color>");
        }

        if (interactor == Runner.LocalPlayer)
        {
            if (isStarting)
            {
                storedPlayerCamera = Camera.main;
                storedPlayerCamera.enabled = false;
                tripodCamera.gameObject.SetActive(true);

                playerHUD.SetActive(false);
                recCanvas.SetActive(true);
            }
            else
            {
                tripodCamera.gameObject.SetActive(false);
                storedPlayerCamera.enabled = true;

                playerHUD.SetActive(true);
                recCanvas.SetActive(false);
            }
        }
    }
}