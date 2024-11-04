namespace Matterless.Floorcraft
{
    public interface IPoolable
    {
        void OnPop();
        void OnPush();
    }
}