using System;

namespace Matterless.Floorcraft
{
    [Flags]
    public enum EnemyState : byte
    {
        None = 0,
        Spawning = 1,
        Attacking = 2,
        Despawning = 4
    }
    
    public struct EnemyStateModel
    {
        
        public EnemyStateModel(EnemyState state, int health)
        {
            m_StateId = (byte)state;
            m_Health = health;
        }

        public EnemyStateModel(byte state, int health)
        {
            m_StateId = state;
            m_Health = health;
        }

        private byte m_StateId;
        private int m_Health;
        public EnemyState state => (EnemyState)m_StateId;
        public int health => m_Health;
        public byte stateId => m_StateId;
    }
}