using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Services.Update
{
    public class UpdateService : MonoBehaviour, IUpdateService
    {
        private List<IUpdatable> _updatables = new List<IUpdatable>();

        public void AddNew(IUpdatable updatable)
        {
            if (_updatables.Contains(updatable) == false)
                _updatables.Add(updatable);
        }

        public void Remove(IUpdatable updatable)
        {
            if (_updatables.Contains(updatable))
                _updatables.Remove(updatable);
        }

        private void FixedUpdate()
        {
            foreach (var updatable in _updatables)
            {
                updatable.FixedUpdate(Time.deltaTime);
            }
        }

        private void Update()
        {
            foreach (var updatable in _updatables)
            {
                updatable.Update(Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            foreach (var updatable in _updatables)
            {
                updatable.LateUpdate(Time.deltaTime);
            }
        }
    }
}