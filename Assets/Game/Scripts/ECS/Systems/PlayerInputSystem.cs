using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Game.Scripts.Services.Input;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerInputSystem : IEcsRunSystem
    {
        private readonly IInputService _inputService;

        private bool _interactWasPressed;

        public PlayerInputSystem(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            EcsFilter filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<MouseInputDirection>().Inc<InteractInput>().End();
            EcsPool<HorizontalMovement> horizontalPool = world.GetPool<HorizontalMovement>();
            EcsPool<MouseInputDirection> lookPool = world.GetPool<MouseInputDirection>();
            EcsPool<InteractInput> interactPool = world.GetPool<InteractInput>();
            
            bool isPressed = _inputService.Interact.CurrentValue;
            bool justPressed = isPressed && _interactWasPressed == false;
            _interactWasPressed = isPressed;
            
            foreach (int entity in filter)
            {
                ref HorizontalMovement horizontalMovement = ref horizontalPool.Get(entity);
                ref MouseInputDirection mouseInputDirection = ref lookPool.Get(entity);

                mouseInputDirection.Direction = _inputService.Look.CurrentValue;
                horizontalMovement.Direction = _inputService.Move.CurrentValue;
                
                ref InteractInput interactInput = ref interactPool.Get(entity);
                interactInput.IsInteractPressed = justPressed;
            }
        }
    }
}