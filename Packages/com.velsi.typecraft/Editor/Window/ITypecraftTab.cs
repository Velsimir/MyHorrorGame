using System;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Editor.Window
{
    public interface ITypecraftTab : IDisposable
    {
        string Title { get; }
        int Order { get; }
        VisualElement Element { get; }
        void OnShow();
        void Tick(float deltaTime);
        void OnTypecraftChanged();
    }
}
