using System;
using Fusion;
using UnityEngine;

public class OrderManager : NetworkBehaviour
{
    public static OrderManager Instance { get; private set; }
    [SerializeField] private OrderCatalog catalog;
    [SerializeField] private float minSeconds = 20f;
    [SerializeField] private float maxSeconds = 40f;
    [Networked, Capacity(10)]
    public NetworkLinkedList<int> ReceivedOrders => default;
    [Networked] private TickTimer nextOrderTimer { get; set; }
    public static event Action OnOrdersChanged;
    private int lastCount = -1; // it's -1 because we need that the first check will be always a change

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void Spawned()
    {
        if(HasStateAuthority)
        {
            nextOrderTimer = TickTimer.CreateFromSeconds(Runner, UnityEngine.Random.Range(minSeconds, maxSeconds));
        }
    }

    public override void FixedUpdateNetwork()
    {
        if(HasStateAuthority)
        {
            if(nextOrderTimer.Expired(Runner))
            {
                CreateRandomOrder();
                nextOrderTimer = TickTimer.CreateFromSeconds(Runner, UnityEngine.Random.Range(minSeconds, maxSeconds));
            }
        }
    }

    private void CreateRandomOrder()
    {
        if(catalog.orders.Count == 0)
        {
            return;
        }
        if(ReceivedOrders.Count < ReceivedOrders.Capacity)
        {
            OrderData order = catalog.orders[UnityEngine.Random.Range(0,catalog.orders.Count)];
            ReceivedOrders.Add(order.id);
        }
    }

    public override void Render()
    {
        if (ReceivedOrders.Count != lastCount)
        {
            lastCount = ReceivedOrders.Count;
            OnOrdersChanged?.Invoke();
        }
    }

    public OrderData GetOrder(int id)
    {
        return catalog.GetById(id);
    }
}
