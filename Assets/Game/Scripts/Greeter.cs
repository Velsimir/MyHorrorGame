using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;

namespace Game.Scripts
{
    public class Greeter : MonoBehaviour
    {
        [Inject] private IEnumerable<string> _word;

        private void Awake()
        {
            Debug.Log(string.Join(" ", _word));
        }
    }
}