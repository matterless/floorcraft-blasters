using System;

namespace Matterless.Floorcraft
{
    [Flags]
    public enum MayhemObstacleState : int
    {
        None = 0,
        Spawning = 1,
        Despawning = 2
    }
    
    public struct MayhemObstacleModel
    {
        
        public MayhemObstacleModel(MayhemObstacleState state, int health, int waveNumber)
        {
            m_StateId = (byte)state;
            m_Health = health;
            m_WaveNumber = waveNumber;
        }
        
        public MayhemObstacleModel(MayhemObstacleState state, int waveNumber)
        {
            m_StateId = (byte)state;
            m_Health = 1;
            m_WaveNumber = waveNumber;
        }

        public MayhemObstacleModel(int state, int health, int waveNumber)
        {
            m_StateId = state;
            m_Health = health;
            m_WaveNumber = waveNumber;
        }

        private int m_StateId;
        private int m_Health;
        private int m_WaveNumber;
        public MayhemObstacleState state => (MayhemObstacleState)m_StateId;
        public int health => m_Health;
        public int waveNumber => m_WaveNumber;
        public int stateId => m_StateId;
    }
}