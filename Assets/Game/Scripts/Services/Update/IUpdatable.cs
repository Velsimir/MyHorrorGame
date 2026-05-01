namespace Game.Scripts.Services.Update
{
    public interface IUpdatable
    {
        void FixedUpdate(float deltaTime);
        void Update(float deltaTime);
        void LateUpdate(float deltaTime);
    }
}