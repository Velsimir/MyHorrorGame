using Game.Scripts.Services.Input;
using Reflex.Attributes;
using UnityEngine;
using R3;

namespace Game.Scripts
{
    public class InputReaderTest : MonoBehaviour
    {
        [Inject] private InputService _inputService;

        private void Start()
        {
            _inputService.Move.Subscribe(ReadValuesMove);
            _inputService.Look.Subscribe(ReadValuesLook);
        }

        private void ReadValuesMove(Vector2 movingInput)
        {
            Debug.Log($"ReadValuesMove {movingInput}");
        }
        
        private void ReadValuesLook(Vector2 movingInput)
        {
            Debug.Log($"ReadValuesLook {movingInput}");
        }
    }
}