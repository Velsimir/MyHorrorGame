using Cysharp.Threading.Tasks;

namespace Game.Scripts.Services.Loading
{
    public interface ILoadingOperation
    {
        UniTask ExecuteAsync();
    }
}