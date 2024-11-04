using System;

namespace Matterless.Floorcraft
{
    public struct MayhemEnemiesStatusModel
    {
        public MayhemEnemiesStatusModel(int enemyCount, MayhemEnemyStatusModel[] enemyModels)
        {
            m_EnemyCount = enemyCount;
            m_EnemyModels = enemyModels;
        }

        private int m_EnemyCount;
        private MayhemEnemyStatusModel[] m_EnemyModels;

        public int enemyCount => m_EnemyCount;
        public MayhemEnemyStatusModel[] enemyModels => m_EnemyModels;
    }
}