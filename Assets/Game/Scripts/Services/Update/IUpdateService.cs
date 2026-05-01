namespace Game.Scripts.Services.Update
{
    public interface IUpdateService
    {
        void AddNew(IUpdatable updatable);
        void Remove(IUpdatable updatable);
    }
}