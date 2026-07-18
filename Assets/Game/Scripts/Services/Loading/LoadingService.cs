using Cysharp.Threading.Tasks;
using Game.Scripts.Views.CurtainLogic;

namespace Game.Scripts.Services.Loading
{
    public class LoadingService : ILoadingService
    {
        private ICurtain _curtain;

        public LoadingService(ICurtain curtain)
        {
            _curtain = curtain;
        }

        public async UniTask BeginLoadingAsync(ILoadingOperation[] operations)
        {
            await _curtain.ShowAsync();

            foreach (ILoadingOperation operation in operations)
                await operation.ExecuteAsync();
            
            await _curtain.HideAsync();
        }
    }
}