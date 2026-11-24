using Fusion;
using UnityEngine;

public class StampRoll : NetworkBehaviour
{
    [SerializeField] private int maxCapacity = 20;
    [SerializeField] private Transform visual;
    [SerializeField] private float minSize = 0.2f;
    [Networked, OnChangedRender(nameof(OnCapacityChanged))]
    public int Remaining {get; set;}
    private Vector3 baseScale;

    private void Awake()
    {
        if(visual == null)
        {
            visual =transform;
        }
        baseScale = visual.localScale;
    }

    public override void Spawned()
    {
        if(HasStateAuthority)
        {
            Remaining = maxCapacity;
        }
        UpdateVisual();
    }

    public bool HasPaper()
    {
        return Remaining > 0;
    }

    public bool TryConsume()
    {
        if(!HasStateAuthority)
        {
            return false;
        }
        if(Remaining <= 0)
        {
            return false;
        }
        Remaining = Remaining - 1;
        return true;
    }

    private void OnCapacityChanged()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float percent = (float)Remaining / Mathf.Max(1, maxCapacity);
        float factor = Mathf.Lerp(minSize, 1f, percent);
        visual.localScale = new Vector3(baseScale.x * factor, baseScale.y, baseScale.z * factor );
    }
}
