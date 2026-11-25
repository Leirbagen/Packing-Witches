using Fusion;
using UnityEngine;

public class StampPrinter : NetworkBehaviour, IInteractable
{
    [SerializeField] private NetworkPrefabRef stampPrefab;
    [SerializeField] private Transform printPoint;
    public Transform rollSlot;
    [SerializeField] private StampRoll initialRoll;
    [Networked] public NetworkObject CurrentRollObject {get; set;}
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
        PlayerGrab localGrab = FindLocalPlayerGrab();
        if (localGrab != null && localGrab.IsHolding && localGrab.HeldObject != null)
        {
            if (localGrab.HeldObject.TryGetComponent(out StampRoll roll))
            {
                return "Press [F] to install roll";
            }
        }
        return string.Empty;
    }

    public void OnInteract(PlayerRef interactor)
    {
        RPC_InstallRoll(interactor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InstallRoll(PlayerRef interactor)
    {
        PlayerGrab playerGrab = FindPlayerGrab(interactor);
        if (playerGrab == null || !playerGrab.IsHolding || playerGrab.HeldObject == null)
        {
            return;
        }
        if (rollSlot == null)
        {
            return;
        }
        if(CurrentRoll != null)
        {
            return;
        }
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
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }
        if (printPoint == null || !stampPrefab.IsValid)
        {
            return;
        }
        StampRoll roll = CurrentRoll;
        if(!roll.TryConsume())
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

        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
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
        foreach (var grabber in FindObjectsByType<PlayerGrab>(FindObjectsSortMode.None))
        {
            if (grabber.Object != null && grabber.Object.HasInputAuthority)
            {
                return grabber;
            }
        }
        return null;
    }
}
