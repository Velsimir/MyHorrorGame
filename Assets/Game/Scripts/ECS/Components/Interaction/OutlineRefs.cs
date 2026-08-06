using System;
using UnityEngine;

namespace Game.Scripts.ECS.Components.Interaction
{
    [Serializable]
    public struct OutlineRefs
    {
        public MeshRenderer MeshRenderer;
        public Material[] WithOutline;
        public Material[] Default;
    }
}