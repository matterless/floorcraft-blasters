using System;
using Random = UnityEngine.Random;

namespace Matterless.Floorcraft
{
    public class EnemyWave
    {
        EnemyWaveModel m_Model;

        public EnemyWaveModel model => m_Model;

        private float m_SpawnTimer;
        private float m_SpawnFrequency;
        private int m_EnemiesSpawned;
        private int m_EnemiesKilled;
        private bool m_IsSpawning;

        public void UpdateModel(EnemyWaveModel model)
        {
            m_Model = model;
            m_SpawnFrequency = Random.Range(m_Model.spawnFrequencyMin, m_Model.spawnFrequencyMax);
            ResetWave();
        }

        public void Update(float deltaTime)
        {
            if (m_IsSpawning)
            {
                return;
            }

            if (m_EnemiesSpawned < m_Model.maxNumberOfEnemies)
            {
                m_SpawnTimer += deltaTime;
            }
            else
            {
                return;
            }

            if (m_SpawnTimer >= m_SpawnFrequency)
            {
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            m_IsSpawning = true;
            m_SpawnTimer = 0;
            m_SpawnFrequency = Random.Range(m_Model.spawnFrequencyMin, m_Model.spawnFrequencyMax);
            m_Model.spawnEnemy?.Invoke();
        }

        public void OnEnemySpawned()
        {
            m_EnemiesSpawned++;
            m_IsSpawning = false;
        }

        public void OnEnemyKilled()
        {
            m_EnemiesKilled++;

            try
            {
                if (m_EnemiesKilled >= m_Model.maxNumberOfEnemies)
                {
                    m_Model.onWaveCompleted?.Invoke();
                    ResetWave();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void ResetWave()
        {
            m_EnemiesKilled = 0;
            m_EnemiesSpawned = 0;
            m_SpawnTimer = 0;
            m_IsSpawning = false;
        }
    }
}