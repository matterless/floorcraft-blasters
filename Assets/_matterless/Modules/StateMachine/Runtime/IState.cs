namespace Matterless.StateMachine
{
    public interface IState
    {
        int id { get; }

        void Enter();
        void Exit();
        void Update(float dt, float udt);
    }
}
