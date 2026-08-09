using Game.Scripts.ECS.Components.Input;
using Game.Scripts.ECS.Components.Movement;
using Game.Scripts.ECS.Components.Player;
using Game.Scripts.Services.Input;
using Leopotam.EcsLite;

namespace Game.Scripts.ECS.Systems
{
    public class PlayerInputSystem : IEcsInitSystem, IEcsRunSystem
    {
        private readonly IInputService _inputService;

        private EcsFilter _filter;
        private EcsPool<HorizontalMovement> _horizontalPool;
        private EcsPool<MouseInputDirection> _lookPool;
        private EcsPool<InteractInput> _interactPool;
        private bool _interactWasPressed;

        public PlayerInputSystem(IInputService inputService)
        {
            _inputService = inputService;
        }

        public void Init(IEcsSystems systems)
        {
            EcsWorld world = systems.GetWorld();
            _filter = world.Filter<PlayerTag>().Inc<HorizontalMovement>().Inc<MouseInputDirection>().Inc<InteractInput>().End();
            _horizontalPool = world.GetPool<HorizontalMovement>();
            _lookPool = world.GetPool<MouseInputDirection>();
            _interactPool = world.GetPool<InteractInput>();
        }

        public void Run(IEcsSystems systems)
        {
            bool isPressed = _inputService.Interact.CurrentValue;
            bool justPressed = isPressed && _interactWasPressed == false;
            _interactWasPressed = isPressed;
            
            foreach (int entity in _filter)
            {
                ref HorizontalMovement horizontalMovement = ref _horizontalPool.Get(entity);
                ref MouseInputDirection mouseInputDirection = ref _lookPool.Get(entity);

                mouseInputDirection.Direction = _inputService.Look.CurrentValue;
                horizontalMovement.Direction = _inputService.Move.CurrentValue;
                
                ref InteractInput interactInput = ref _interactPool.Get(entity);
                interactInput.IsInteractPressed = justPressed;
            }
        }
    }
}