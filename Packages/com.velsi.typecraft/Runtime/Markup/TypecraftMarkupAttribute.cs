using System;
using UnityEngine;

namespace Velsi.Typecraft
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TypecraftMarkupAttribute : PropertyAttribute
    {
        public TypecraftMarkupAttribute(bool preview = true)
        {
            Preview = preview;
        }

        public bool Preview { get; }
    }
}
