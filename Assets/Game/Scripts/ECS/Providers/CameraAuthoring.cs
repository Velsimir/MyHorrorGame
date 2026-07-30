using Game.Scripts.ECS.Components;
using Game.Scripts.ECS.Components.MonoBehaviourRefs;
using Game.Scripts.ECS.Components.Tags;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Game.Scripts.ECS.Providers
{
    public class CameraAuthoring : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [SerializeField] CinemachineBasicMultiChannelPerlin _cinemachineCameraPerlin;
        
        [Inject] IEcsWorldProvider _ecsWorldProvider;
        
        private void Start()
        {
            int cinemachine = _ecsWorldProvider.World.NewEntity();
            _ecsWorldProvider.World.GetPool<CinemachineTag>().Add(cinemachine);
            ref var cinemachineRefs = ref _ecsWorldProvider.World.GetPool<CinemachineRefs>().Add(cinemachine);
            
            cinemachineRefs.Camera = _cinemachineCamera;
            cinemachineRefs.Perlin = _cinemachineCameraPerlin;
        }
    }
}