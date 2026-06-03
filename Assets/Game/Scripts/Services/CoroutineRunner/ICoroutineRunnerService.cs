using System.Collections;
using UnityEngine;

namespace Game.Scripts.Services.CoroutineRunner
{
    public interface ICoroutineRunnerService
    {
        Coroutine StartRoutine(IEnumerator routine);
        void StopRoutine(Coroutine coroutine);
        void StopAllRoutines();
    }
}