using Fusion;
using UnityEngine;

public class MachineUpgrade : NetworkBehaviour
{
    public int level2Cost = 50;
    public int level3Cost = 120;

    public float level1TimeMultiplier = 1f;
    public float level2TimeMultiplier = 0.65f;
    public float level3TimeMultiplier = 0.4f;

    [Range(0f, 1f)] public float level1FailureChance = 1f;
    [Range(0f, 1f)] public float level2FailureChance = 0.35f;
    [Range(0f, 1f)] public float level3FailureChance = 0f;

    public KeyCode upgradeKey = KeyCode.E;
    public float holdDuration = 1.5f;
    public float upgradeDistance = 3.5f;
    public WorldPromptUI worldPrompt;

    [Networked, OnChangedRender(nameof(OnLevelChanged))]
    public int CurrentLevel { get; set; }

    private MachineBreakdown breakdown;
    private float localUpgradeTimer = 0f;

    public bool IsMaxLevel
    {
        get
        {
            return CurrentLevel >= 3;
        }
    }

    private void Awake()
    {
        breakdown = GetComponent<MachineBreakdown>();

        if (worldPrompt == null)
        {
            worldPrompt = GetComponentInChildren<WorldPromptUI>();
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority && CurrentLevel < 1)
        {
            CurrentLevel = 1;
        }
    }

    private bool IsMachineBroken()
    {
        return breakdown != null && breakdown.Object != null && breakdown.Object.IsValid && breakdown.IsBroken;
    }

    public int GetNextUpgradeCost()
    {
        if (CurrentLevel == 1)
        {
            return level2Cost;
        }

        if (CurrentLevel == 2)
        {
            return level3Cost;
        }

        return 0;
    }

    public float GetProcessTimeMultiplier()
    {
        if (CurrentLevel == 2)
        {
            return level2TimeMultiplier;
        }

        if (CurrentLevel >= 3)
        {
            return level3TimeMultiplier;
        }

        return level1TimeMultiplier;
    }

    public float GetFailureChance()
    {
        if (CurrentLevel == 2)
        {
            return level2FailureChance;
        }

        if (CurrentLevel >= 3)
        {
            return level3FailureChance;
        }

        return level1FailureChance;
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid)
        {
            return;
        }

        if (IsMaxLevel || IsMachineBroken())
        {
            localUpgradeTimer = 0f;
            return;
        }

        if (IsLocalPlayerLookingAtMachine() && Input.GetKey(upgradeKey))
        {
            localUpgradeTimer += Time.deltaTime;

            if (localUpgradeTimer >= holdDuration)
            {
                localUpgradeTimer = 0f;
                RPC_RequestUpgrade();
            }
        }
        else
        {
            localUpgradeTimer = 0f;
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
        if (Physics.Raycast(ray, out RaycastHit hit, upgradeDistance))
        {
            MachineUpgrade hitMachine = hit.collider.GetComponentInParent<MachineUpgrade>();
            if (hitMachine == this)
            {
                return true;
            }
        }

        return false;
    }

    public string GetUpgradePrompt()
    {
        if (Object == null || !Object.IsValid)
        {
            return string.Empty;
        }

        if (IsMaxLevel)
        {
            return "<color=#00FFFF>Lv.3 (MAX)</color>";
        }

        int cost = GetNextUpgradeCost();

        if (localUpgradeTimer > 0f)
        {
            int percent = Mathf.Clamp(Mathf.RoundToInt((localUpgradeTimer / holdDuration) * 100f), 0, 100);
            return $"<color=#FFFF00>Upgrading to Lv.{CurrentLevel + 1}... {percent}%</color>";
        }

        return $"<color=#AAAAAA>Lv.{CurrentLevel}</color> | Hold [{upgradeKey}] to Upgrade (<color=#44FF44>${cost}</color>)";
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestUpgrade()
    {
        if (IsMaxLevel || IsMachineBroken())
        {
            return;
        }

        int cost = GetNextUpgradeCost();

        if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpend(cost))
        {
            CurrentLevel++;
        }
        else
        {
            RPC_ShowNotEnoughMoney(cost);
        }
    }

    private void OnLevelChanged()
    {
        if (worldPrompt != null && CurrentLevel > 1)
        {
            worldPrompt.ShowTemporaryMessage($"<color=#44FF44>Upgraded to Level {CurrentLevel}!</color>", 2.5f);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowNotEnoughMoney(int requiredCost)
    {
        if (worldPrompt != null)
        {
            worldPrompt.ShowTemporaryMessage($"<color=#FF4444>Not enough money! Need ${requiredCost}</color>", 2.5f);
        }
    }
}