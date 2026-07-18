using Cysharp.Threading.Tasks;

namespace Game.Scripts.Views.CurtainLogic
{
    public interface ICurtain
    {
        UniTask ShowAsync();
        UniTask HideAsync();
    }
}