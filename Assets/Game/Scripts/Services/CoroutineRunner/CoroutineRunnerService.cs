using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Services.CoroutineRunner
{
    public class CoroutineRunnerService : MonoBehaviour, ICoroutineRunnerService
    {
        private readonly List<Coroutine> _activeCoroutines = new List<Coroutine>();
    
        public Coroutine StartRoutine(IEnumerator routine)
        {
            var coroutine = StartCoroutine(WrapRoutine(routine));
            _activeCoroutines.Add(coroutine);
            return coroutine;
        }
    
        public void StopRoutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                _activeCoroutines.Remove(coroutine);
            }
        }
    
        public void StopAllRoutines()
        {
            foreach (var coroutine in _activeCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _activeCoroutines.Clear();
        }
    
        private IEnumerator WrapRoutine(IEnumerator routine)
        {
            yield return routine;
        }
    }
}