using Fusion;
using UnityEngine;
using System;

public class EconomyManager : NetworkBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Networked, OnChangedRender(nameof(OnRevenueChanged))]
    public int TotalRevenue { get; set; }

    public static event Action<int> OnRevenueUpdated;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddRevenue(int amount)
    {
        if (!HasStateAuthority) return;
        TotalRevenue += amount;
    }

    private void OnRevenueChanged()
    {
        OnRevenueUpdated?.Invoke(TotalRevenue);
    }

    public bool TrySpend(int amount)
    {
        if (!HasStateAuthority)
        {
            return false;
        } 
        if (TotalRevenue < amount)
        {
            return false;
        } 
        TotalRevenue -= amount;
        return true;
    }
}