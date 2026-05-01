using Game.Scripts.Services;
using Game.Scripts.Services.Input;
using Game.Scripts.Services.Update;
using Reflex.Attributes;
using UnityEngine;
using R3;

namespace Game.Scripts
{
    public class InputReaderTest : MonoBehaviour
    {
        [Inject] private IInputService _inputService;
        [Inject] private IUpdateService _updatableService;
        [Inject] private CoroutineRunnerService _coroutineRunnerService;
        
        private void Start()
        {
            _inputService.Move.Subscribe(ReadValuesMove);
            _inputService.Look.Subscribe(ReadValuesLook);

            if (_updatableService == null)
            {
                Debug.Log($"_updatableService is null!");
            }
            else
            {
                Debug.Log($"_updatableService is NOT null!");
            }

            if (_coroutineRunnerService == null)
            {
                Debug.Log($"_coroutineRunnerService is null!");
            }
            else
            {
                Debug.Log($"_coroutineRunnerService is NOT null!");
            }
        }

        private void ReadValuesMove(Vector2 movingInput)
        {
        }
        
        private void ReadValuesLook(Vector2 movingInput)
        {
        }
    }
}