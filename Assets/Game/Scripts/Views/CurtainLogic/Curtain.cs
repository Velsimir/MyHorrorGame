using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Views.CurtainLogic
{
    public class Curtain : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeTime;
    
        public async UniTask ShowAsync()
        {
            _canvasGroup.alpha = 1;
            
            _canvasGroup.blocksRaycasts = true;
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
                
                UniTask.Yield();
            }
            
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
