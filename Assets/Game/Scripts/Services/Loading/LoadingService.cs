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
            //TODO включить экран загрузки, когда будет нужен, для тестов отключил
            //await _curtain.ShowAsync();

            foreach (ILoadingOperation operation in operations)
                await operation.ExecuteAsync();
            
            //await _curtain.HideAsync();
        }
    }
}