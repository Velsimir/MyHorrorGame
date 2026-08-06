using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Services.Input
{
    public class InputService : IInputService
    {
        private readonly InputActions _inputActions;

        public ReactiveProperty<Vector2> Move { get; private set; }
        public ReactiveProperty<Vector2> Look { get; private set; }
        public ReactiveProperty<bool> Interact { get; private set; }

        public InputService()
        {
            _inputActions = new InputActions();
            _inputActions.Enable();

            Move = BindMovementInput(_inputActions.Player.Move);
            Look = BindMovementInput(_inputActions.Player.Look);
            Interact = BindInteractInput(_inputActions.Player.Interact);
        }

        private ReactiveProperty<Vector2> BindMovementInput(InputAction playerMove)
        {
            ReactiveProperty<Vector2> vector = new ReactiveProperty<Vector2>(Vector2.zero);

            playerMove.started += ctx => vector.Value = ctx.ReadValue<Vector2>();
            playerMove.performed += ctx => vector.Value = ctx.ReadValue<Vector2>();
            playerMove.canceled  += ctx => vector.Value = Vector2.zero;
            
            return vector;
        }

        private ReactiveProperty<bool> BindInteractInput(InputAction playerInteract)
        {
            ReactiveProperty<bool> interact = new ReactiveProperty<bool>(false);

            playerInteract.started += ctx => interact.Value = true;
            playerInteract.canceled += ctx => interact.Value = false;
            
            return interact;
        }
    }
}