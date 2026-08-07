using Game.Scripts.ECS.Components.Doors;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Scripts.ECS.Authoring
{
    public class DoorAuthoring : InteractionObjectAuthoring
    {
        [SerializeField] private Transform _pivot;
        [SerializeField] private float _openingAngle;
        [SerializeField] private float _closingAngle;
        [SerializeField] private float _speed;

        protected override void AddSpecificTag(EcsWorld world, int entity)
        {
            ref var door = ref world.GetPool<Door>().Add(entity);
            
            door.Pivot = _pivot;
            door.InitialRotation = _pivot.localRotation;
            door.OpenAngle = _openingAngle;
            door.CloseAngle = _closingAngle;
            door.Speed = _speed;
        }
    }
}