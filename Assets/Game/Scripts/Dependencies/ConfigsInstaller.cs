using Game.Scripts.Configs;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Dependencies
{
    [CreateAssetMenu(fileName = "ConfigsInstaller", menuName = "Dependencies/ConfigsInstaller")]
    public class ConfigsInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private PlayerConfig _playerConfig;
        
        public override void InstallBindings()
        {
            Container.Bind<PlayerConfig>().FromInstance(_playerConfig).AsSingle();
        }
    }
}