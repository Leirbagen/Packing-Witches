using System;
using Fusion;

using UnityEngine;

public class ReputationManager : NetworkBehaviour
{
    public static ReputationManager Instance { get; private set; }
    [Networked] public int TotalStars { get; set; }
    [Networked] public int RatingCount { get; set; }
    public static event Action<float> OnRatingChanged;

    private int lastCount = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public float Average
    {
        get
        {
            if (RatingCount == 0)
            {
                return 0f;
            }
            else
            {
                return (float)TotalStars / RatingCount;
            }
        }
    }

    public void AddRating(int stars)
    {
        if (!HasStateAuthority) return;
        TotalStars += Mathf.Clamp(stars, 1, 5);
        RatingCount++;
    }

    public override void Render()
    {
        if (RatingCount != lastCount)
        {
            lastCount = RatingCount;
            OnRatingChanged?.Invoke(Average);
        }
    }
}
