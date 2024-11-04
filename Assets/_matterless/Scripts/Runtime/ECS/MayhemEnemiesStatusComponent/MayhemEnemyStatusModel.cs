using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public struct MayhemEnemyStatusModel
    {
        public MayhemEnemyStatusModel(uint entityId, Vector3 position, float speed)
        {
            m_EntityId = entityId;
            m_Position = position;
            m_Speed = speed;
        }

        private uint m_EntityId;
        private Vector3 m_Position;
        private float m_Speed;

        public uint entityId => m_EntityId;
        public Vector3 position => m_Position;
        public float speed => m_Speed;
    }
}