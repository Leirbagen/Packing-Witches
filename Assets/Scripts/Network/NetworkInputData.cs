using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 move;
    public Vector2 look;
    public NetworkButtons buttons;
}

public enum InputButton
{
    jump,
    run,
    grab
}