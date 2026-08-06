using Game.Scripts.Configs;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Dependencies
{
    [CreateAssetMenu(fileName = "ConfigsInstaller", menuName = "Dependencies/ConfigsInstaller")]
    public class ConfigsInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private GameConfigs _gameConfigs;
        
        public override void InstallBindings()
        {
            Container.Bind<GameConfigs>().FromInstance(_gameConfigs).AsSingle();
        }
    }
}