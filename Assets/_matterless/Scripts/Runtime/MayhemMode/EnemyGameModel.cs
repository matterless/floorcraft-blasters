using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct EnemyGameModel
    {
        public EnemyState enemyState;
        public Vector3 position;
        public Quaternion rotation;
        public float speed;

        public EnemyGameModel(EnemyState enemyState, Vector3 position, Quaternion rotation, float speed)
        {
            this.enemyState = enemyState;
            this.position = position;
            this.rotation = rotation;
            this.speed = speed;
        }
    }
}