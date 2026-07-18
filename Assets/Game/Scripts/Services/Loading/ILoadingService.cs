using Cysharp.Threading.Tasks;

namespace Game.Scripts.Services.Loading
{
    public interface ILoadingService
    {
        UniTask BeginLoadingAsync(ILoadingOperation[] operations);
    }
}