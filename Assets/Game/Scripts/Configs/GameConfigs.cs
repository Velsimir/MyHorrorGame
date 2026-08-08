using Game.Scripts.ECS.Authoring;
using UnityEngine;

namespace Game.Scripts.Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/GameConfigs")]
    public class GameConfigs : ScriptableObject
    {
        [SerializeField] private PlayerConfig _playerConfig;
        [SerializeField] private WalkConfig _walkConfig;
        [SerializeField] private GravityConfig _gravityConfigs;

        public PlayerConfig PlayerConfig => _playerConfig;
        public WalkConfig WalkConfig => _walkConfig;
        public GravityConfig GravityConfig => _gravityConfigs;
    }
}