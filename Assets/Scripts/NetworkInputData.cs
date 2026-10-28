using Fusion;
using UnityEngine;

public class NetworkInputData : INetworkInput
{
    public Vector2 move;
    public Vector2 look;
    public NetworkButtons buttons;
}

public enum InputButton
{
    jump,
    run
}