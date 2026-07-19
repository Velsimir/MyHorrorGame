using R3;
using UnityEngine;

namespace Game.Scripts.Services.Input
{
    public interface IInputService
    {
        ReactiveProperty<Vector2> Move { get; }
        ReactiveProperty<Vector2> Look { get; }
    }
}