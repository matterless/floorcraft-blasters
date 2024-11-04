using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Matterless/Enemy")]
    public class Enemy : Asset
    {
        //[SerializeField] private string m_Name;
            
        [Header("Enemy Settings")]
        [SerializeField] private float m_EnemyStartingSpeed;
        [SerializeField] private float m_EnemySpeedIncrease;
        [SerializeField] private int m_EnemyStartingHealth;
        [SerializeField] private int m_EnemyHealthIncrease;
        [SerializeField] private ushort m_GroundClearance = 3;

        [Header("Simulation Settings")] [SerializeField]
        private float m_BrakeDistance;

        [SerializeField] private float m_MaxSpeed;
        [SerializeField] private float m_BrakePower;
        [SerializeField] private float m_Acceleration;
        [SerializeField] private float m_MaxTurningRadius;
            
        //public string name => m_Name;
        public float enemyStartingSpeed => m_EnemyStartingSpeed;
        public float enemySpeedIncrease => m_EnemySpeedIncrease;
        public int enemyStartingHealth => m_EnemyStartingHealth;
        public int enemyHealthIncrease => m_EnemyHealthIncrease;
            
        public float brakeDistance => m_BrakeDistance;
        public float maxSpeed => m_MaxSpeed;
        public float brakePower => m_BrakePower;
        public float acceleration => m_Acceleration;
        public float maxTurningRadius => m_MaxTurningRadius;
        public float groundClearance => (float)m_GroundClearance / 1000f;
    }
}