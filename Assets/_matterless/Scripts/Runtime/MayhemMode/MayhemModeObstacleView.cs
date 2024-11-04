using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class MayhemModeObstacleView : ObstacleView
    {
        [SerializeField] private NPCEnemySpawnPoint[] m_PredefinedSpawnPoints;
        [SerializeField] private GameObject m_DeathEffect;
        NPCEnemySpawnPoint[] selectedPoints;
        private int m_MaxHealth;
        private int m_Health;
        private float m_PlayAreaRadius;

        public Action onTowerDestroyed;
        
        public void Init(int health, int maxHealth, float playAreaRadius)
        {
            m_MaxHealth = maxHealth;
            m_PlayAreaRadius = playAreaRadius;
            onScaleChanged += OnScaleChanged;
            SetHealth(health);
        }

        private void OnScaleChanged(float newScale)
        {
            SetPlayArea(m_PlayAreaRadius);
        }

        public void SetHealth(int health)
        {
            if (m_Health != health)
            {
                m_Health = health;
                float normalizedHealthValue = (1f / m_MaxHealth) * health;
                SetDamageVisuals(normalizedHealthValue);
            }
            
            if (health == 0 && m_DeathEffect != null)
            {
                onTowerDestroyed?.Invoke();
                m_DeathEffect.SetActive(true);
                m_DeathEffect.transform.parent = null;
                Destroy(m_DeathEffect, 5f);
            }
        }
        
        public void ActivateSpawnPoints(int[] spawnPointIndexes)
        {
            if (spawnPointIndexes != null)
            {
                selectedPoints = new NPCEnemySpawnPoint[spawnPointIndexes.Length];

                for (int i = 0; i < selectedPoints.Length; i++)
                {
                    selectedPoints[i] = m_PredefinedSpawnPoints[spawnPointIndexes[i]];
                    selectedPoints[i].Spawn();
                }
            }
            else
            {
                Debug.LogWarning("Array is null");
            }
        }

        public void DeactivateSpawnPoints()
        {
            if (selectedPoints != null)
            {
                foreach (var esp in selectedPoints)
                {
                    esp.DeSpawn();
                }
            }

            selectedPoints = null;
        }

        private void SetPlayArea(float radius)
        {
            foreach (var esp in m_PredefinedSpawnPoints)
            {
                Vector3 obstaclePosition = transform.position;
                Vector3 pointDirection = (esp.transform.position - obstaclePosition).normalized;
                esp.transform.position = obstaclePosition + pointDirection * radius * transform.localScale.x;
            }
        }

        public NPCEnemySpawnPoint[] GetSpawnPoints(int[] spawnPointIndexes)
        {
            selectedPoints = new NPCEnemySpawnPoint[spawnPointIndexes.Length];

            for (int i = 0; i < selectedPoints.Length; i++)
            {
                selectedPoints[i] = m_PredefinedSpawnPoints[spawnPointIndexes[i]];
                selectedPoints[i].Spawn();
            }

            return selectedPoints;
        }
    }
}
