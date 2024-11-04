using System;
using System.Collections.Generic;


namespace Matterless.StateMachine
{
    public class StateMachine : IStateMachine
    {
        private readonly Dictionary<int, IState> m_States;
        private IState m_CurrentState;

        public StateMachine(params IState[] states)
        {
            m_States = new Dictionary<int, IState>();

            if (states == null)
                return;

            foreach (var state in states)
                AddState(state);
        }

        public IState currentState => m_CurrentState;

        public void AddState(IState state)
        {
            if (m_States.ContainsKey(state.id))
                throw new Exception($"Duplicate state with id:{state.id} in this state machine");

            m_States.Add(state.id, state);
        }

        public void Update(float dt, float udt) => m_CurrentState?.Update(dt, udt);

        public void Start(int stateId)
        {
            SetCurrentState(stateId);
        }

        public void Stop()
        {
            if (m_CurrentState != null)
                m_CurrentState.Exit();

            m_CurrentState = null;
        }

        public void SwitchState(int stateId)
        {
            if (m_CurrentState != null)
                m_CurrentState.Exit();

            SetCurrentState(stateId);
        }

        private void SetCurrentState(int stateId)
        {
            var state = GetState(stateId);
            state.Enter();
            m_CurrentState = state;
        }

        private IState GetState(int stateId)
        {
            if (!m_States.ContainsKey(stateId))
                throw new KeyNotFoundException($"No state with this id:{stateId} exists in this state machine");

            return m_States[stateId];
        }
    }
}
