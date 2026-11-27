using Fusion;
using UnityEngine;

public class MachineBreakdown : NetworkBehaviour
{
    public string machineName = "Machine";
    public float repairDuration = 3f;
    public float repairDistance = 3.5f;
    public KeyCode repairKey = KeyCode.E;

    public ParticleSystem brokenParticles;
    public WorldPromptUI worldPrompt;

    [Networked, OnChangedRender(nameof(OnBrokenStateChanged))]
    public NetworkBool IsBroken { get; set; }

    private float localRepairTimer = 0f;

    private void Awake()
    {
        if (worldPrompt == null)
        {
            worldPrompt = GetComponentInChildren<WorldPromptUI>();
        }
    }

    public override void Spawned()
    {
        UpdateParticles();
    }

    public void BreakMachine()
    {
        if (!HasStateAuthority || IsBroken)
        {
            return;
        }

        IsBroken = true;
    }

    private void OnBrokenStateChanged()
    {
        localRepairTimer = 0f;
        UpdateParticles();

        if (worldPrompt != null)
        {
            if (IsBroken)
            {
                worldPrompt.ShowTemporaryMessage($"<color=#FF4444>{machineName} BROKE DOWN!</color>", 3f);
            }
            else
            {
                worldPrompt.ShowTemporaryMessage($"<color=#44FF44>{machineName} repaired!</color>", 2.5f);
            }
        }
    }

    private void UpdateParticles()
    {
        if (brokenParticles == null)
        {
            return;
        }

        if (IsBroken)
        {
            if (!brokenParticles.isPlaying)
            {
                brokenParticles.Play();
            }
        }
        else
        {
            if (brokenParticles.isPlaying)
            {
                brokenParticles.Stop();
            }
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
        {
            return;
        }

        if (!IsBroken)
        {
            localRepairTimer = 0f;
            return;
        }

        if (IsLocalPlayerLookingAtMachine() && Input.GetKey(repairKey))
        {
            localRepairTimer += Time.deltaTime;

            if (localRepairTimer >= repairDuration)
            {
                localRepairTimer = 0f;
                RPC_RequestRepair();
            }
        }
        else
        {
            localRepairTimer = 0f;
        }
    }

    private bool IsLocalPlayerLookingAtMachine()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, repairDistance))
        {
            MachineBreakdown hitMachine = hit.collider.GetComponentInParent<MachineBreakdown>();
            if (hitMachine == this)
            {
                return true;
            }
        }

        return false;
    }

    public string GetBrokenPrompt()
    {
        if (localRepairTimer > 0f)
        {
            int percent = Mathf.Clamp(Mathf.RoundToInt((localRepairTimer / repairDuration) * 100f), 0, 100);
            return $"<color=#FF4444>BROKEN!</color>\n<color=#FFFF00>Repairing... {percent}%</color>";
        }

        return $"<color=#FF4444>{machineName} IS BROKEN!</color>\nHold [{repairKey}] to repair";
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRepair()
    {
        if (!IsBroken)
        {
            return;
        }

        IsBroken = false;
    }
}