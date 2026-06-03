using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Scripts.Services.CoroutineRunner
{
    public class CoroutineRunnerService : MonoBehaviour, ICoroutineRunnerService
    {
        private readonly List<Coroutine> _activeCoroutines = new();

        public Coroutine StartRoutine(IEnumerator routine)
        {
            if (routine == null) return null;

            var coroutine = StartCoroutine(WrapAndTrack(routine));
            _activeCoroutines.Add(coroutine);
            return coroutine;
        }

        private IEnumerator WrapAndTrack(IEnumerator routine)
        {
            var currentCoroutine = StartCoroutine(routine);
            try
            {
                yield return currentCoroutine;
            }
            finally
            {
                _activeCoroutines.Remove(currentCoroutine);
            }
        }

        public void StopRoutine(Coroutine coroutine)
        {
            if (coroutine == null) return;
        
            StopCoroutine(coroutine);
            _activeCoroutines.Remove(coroutine);
        }

        public void StopAllRoutines()
        {
            foreach (var coroutine in _activeCoroutines.ToList())
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
        
            _activeCoroutines.Clear();
        }

        private void OnDestroy()
        {
            StopAllRoutines();
        }
    }
}