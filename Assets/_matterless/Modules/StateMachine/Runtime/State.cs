using System;


namespace Matterless.StateMachine
{
    public class State : IState
    {
        private readonly int m_Id;
        private readonly Action m_OnEnter;
        private readonly Action m_OnExit;
        private readonly Action<float, float> m_OnUpdate;

        public State(Enum stateId, Action onEnter, Action onExit, Action<float, float> onUpdate = null)
        {
            m_Id = Convert.ToInt32(stateId);
            m_OnEnter = onEnter;
            m_OnExit = onExit;
            m_OnUpdate = onUpdate;
        }

        public int id => m_Id;
        public void Enter() => m_OnEnter?.Invoke();
        public void Exit() => m_OnExit?.Invoke();
        public void Update(float dt, float udt) => m_OnUpdate?.Invoke(dt, udt);
    }
}
