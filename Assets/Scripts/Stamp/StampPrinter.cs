using Fusion;
using UnityEngine;

public class StampPrinter : NetworkBehaviour, IInteractable
{
    [SerializeField] private NetworkPrefabRef stampPrefab;
    [SerializeField] private Transform printPoint;
    public Transform rollSlot;
    [SerializeField] private StampRoll initialRoll;

    [Networked] public NetworkObject CurrentRollObject { get; set; }

    private MachineBreakdown breakdown;

    public StampRoll CurrentRoll
    {
        get
        {
            if (CurrentRollObject == null)
            {
                return null;
            }
            return CurrentRollObject.GetComponent<StampRoll>();
        }
    }

    public bool HasPaper
    {
        get
        {
            StampRoll roll = CurrentRoll;
            if (roll == null)
            {
                return false;
            }
            return roll.HasPaper();
        }
    }

    private void Awake()
    {
        breakdown = GetComponent<MachineBreakdown>();
    }

    private bool IsMachineBroken()
    {
        return breakdown != null && breakdown.Object != null && breakdown.Object.IsValid && breakdown.IsBroken;
    }

    public override void Spawned()
    {
        if (HasStateAuthority && initialRoll != null && CurrentRollObject == null)
        {
            initialRoll.Seat(Object, rollSlot);
            CurrentRollObject = initialRoll.Object;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
        {
            return;
        }

        StampRoll current = CurrentRoll;
        if (current == null)
        {
            return;
        }

        GrabbableObject grabbable = current.GetComponent<GrabbableObject>();
        if (grabbable != null && grabbable.IsGrabbed)
        {
            current.Unseat();
            CurrentRollObject = null;
        }
    }

    public string GetInteractionPrompt()
    {
        if (IsMachineBroken())
        {
            return breakdown.GetBrokenPrompt();
        }

        PlayerGrab localGrab = FindLocalPlayerGrab();
        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            if (localGrab.HeldObject.TryGetComponent(out StampRoll roll))
            {
                return "Stamp Printer\n<color=#FFFF00>Press [F] to install roll</color>";
            }
        }

        if (!HasPaper)
        {
            return "Stamp Printer\n<color=#FFA500>Out of paper! Install a stamp roll</color>";
        }

        return "Stamp Printer\n<color=#44FF44>Ready to print labels</color>";
    }

    public void OnInteract(PlayerRef interactor)
    {
        if (IsMachineBroken())
        {
            return;
        }

        RPC_InstallRoll(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InstallRoll(PlayerRef interactor)
    {
        if (IsMachineBroken())
        {
            return;
        }

        if (rollSlot == null || CurrentRoll != null)
        {
            return;
        }

        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            return;
        }

        if (!playerGrab.HeldObject.TryGetComponent(out StampRoll newRoll))
        {
            return;
        }

        playerGrab.RequestDrop();
        newRoll.Seat(Object, rollSlot);
        CurrentRollObject = newRoll.Object;
    }

    public void RequestPrint(string content)
    {
        if (IsMachineBroken())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            Debug.LogWarning("[Printer] Empty stamp content.");
            return;
        }

        RPC_RequestPrint(content.Trim());
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestPrint(string content)
    {
        if (IsMachineBroken())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        if (printPoint == null || !stampPrefab.IsValid)
        {
            return;
        }

        StampRoll roll = CurrentRoll;
        if (roll == null || !roll.TryConsume())
        {
            return;
        }

        NetworkObject spawnedObject = Runner.Spawn(stampPrefab, printPoint.position, printPoint.rotation);
        if (spawnedObject == null)
        {
            return;
        }

        Stamp stamp = spawnedObject.GetComponent<Stamp>();
        if (stamp != null)
        {
            stamp.SetContent(content);
        }
    }

    private PlayerGrab FindPlayerGrab(PlayerRef playerRef)
    {
        NetworkObject playerObj = Runner.GetPlayerObject(playerRef);
        if (playerObj != null && playerObj.TryGetComponent(out PlayerGrab playerGrab))
        {
            return playerGrab;
        }

        foreach (PlayerGrab grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.InputAuthority == playerRef)
            {
                return grabber;
            }
        }

        return null;
    }

    private PlayerGrab FindLocalPlayerGrab()
    {
        foreach (PlayerGrab grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.HasInputAuthority)
            {
                return grabber;
            }
        }

        return null;
    }
}