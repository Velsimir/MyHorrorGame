using Game.Scripts.ECS.Components.Camera;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Authoring
{
    public class CameraAuthoring : MonoBehaviour
    {
        [SerializeField] private CinemachineMixingCamera _mixingCamera;
        
        [Inject] IEcsWorldProvider _ecsWorldProvider;
        
        private void Start()
        {
            int camera = _ecsWorldProvider.World.NewEntity();
            _ecsWorldProvider.World.GetPool<CameraTag>().Add(camera);
            
            ref var cinemachineRefs = ref _ecsWorldProvider.World.GetPool<CinemachineRefs>().Add(camera);
            cinemachineRefs.CameraMixing = _mixingCamera;
        }
    }
}