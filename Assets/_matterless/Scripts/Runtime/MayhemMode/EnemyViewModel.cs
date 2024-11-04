using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct EnemyViewModel
    {
        public uint entityId;
        public Vector3 groundPosition;
        public Quaternion orientation;
        public EnemyState enemyState;
        public float speed;

        public EnemyViewModel
        (uint entityId,
            Vector3 groundPosition,
            Quaternion orientation,
            EnemyState enemyState,
            float speed)
        {
            this.entityId = entityId;
            this.groundPosition = groundPosition;
            this.orientation = orientation;
            this.enemyState = enemyState;
            this.speed = speed;
        }
    }
}