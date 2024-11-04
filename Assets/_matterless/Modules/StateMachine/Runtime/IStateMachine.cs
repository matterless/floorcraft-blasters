namespace Matterless.StateMachine
{
    public interface IStateMachine
    {
        void AddState(IState state);
        void Update(float dt, float udt);
        void Start(int stateId);
        void Stop();
        void SwitchState(int stateId);
    }
}