using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Views.CurtainLogic
{
    public class Curtain : MonoBehaviour, ICurtain
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeTime;
        [SerializeField] private Animator _animator;

        private UniTaskCompletionSource _loopEntered;
        private bool _isLogoScreenShowed;
        
        private readonly int _simpleLoadingScreenAnimationHash = Animator.StringToHash("SimpleLoading");

        private void Awake()
        {
            _loopEntered = new UniTaskCompletionSource();
        }
        

        public async UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            
            if (_isLogoScreenShowed)
                _animator.Play(_simpleLoadingScreenAnimationHash);
            
            _canvasGroup.alpha = 1;
            
            _canvasGroup.blocksRaycasts = true;

            await _loopEntered.Task;
        }


        public async UniTask HideAsync()
        {
            float elapsedTime = 0;
            float startAlpha = _canvasGroup.alpha;
        
            while (elapsedTime < _fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float time = elapsedTime / _fadeTime;
            
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0, time);
                
                await UniTask.Yield();
            }
            
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void LogoScreenShowed()
        {
            _isLogoScreenShowed = true;
            _loopEntered.TrySetResult();
        }
    }
}
