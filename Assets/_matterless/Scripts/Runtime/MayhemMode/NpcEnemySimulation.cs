using System.Collections;
using System.Collections.Generic;
using Matterless.Floorcraft;
using UnityEngine;

namespace Matterless
{
    public class NpcEnemySimulation : INPCEnemySimulation
    {
        private Vector3 m_GroundPosition;
        private Vector3 m_FloorNormal;
        private Vector3 m_Target;
        private Quaternion m_Orientation;
        private readonly bool m_IsHost;
        private Vector3 m_Velocity;
        private float m_Speed;
        private EnemyState m_State;
        private uint m_EntityId;
        private NpcEnemyInputModel m_LastNpcEnemyGameModel;

        // properties
        public EnemyState state => m_State;
        public Vector3 groundPosition => m_GroundPosition;
        public Vector3 floorNormal => m_FloorNormal;
        public float speed => m_Speed;
        public uint entityId => m_EntityId;
        public Quaternion rotation => m_Orientation;

        public NpcEnemySimulation(
            uint entityId,
            bool isHost)
        {
            m_EntityId = entityId;
            m_IsHost = isHost;
        }

        public void Init(NpcEnemyInputModel inputModel, Vector3 target)
        {
            m_GroundPosition = inputModel.position;
            m_Orientation = inputModel.rotation;
            m_Target = target;
            m_Speed = inputModel.speed;
            UnSetState(EnemyState.Spawning);

            var up = Vector3.up;
            var vector = Vector3.ProjectOnPlane(m_Target - m_GroundPosition, up);
            var dir = vector.normalized;
            
            m_Orientation = Quaternion.LookRotation(dir, up);
            m_Velocity = m_Orientation * Vector3.forward * m_Speed;

            Debug.Log($"Init - speed: {m_Speed}");
        }

        public NpcEnemyInputModel Update(float deltaTime, NpcEnemyInputModel inputModel)
        {
            m_State = inputModel.enemyState;

            if (m_IsHost)
            {
                return ServerUpdate(deltaTime);
            }

            return ClientUpdate(deltaTime);
        }

        private NpcEnemyInputModel ServerUpdate(float dt)
        {
            m_GroundPosition += m_Velocity * dt;

            return new NpcEnemyInputModel()
            {
                position = m_GroundPosition,
                rotation = m_Orientation,
                speed = m_Speed,
                enemyState = m_State,
            };
        }

        private NpcEnemyInputModel ClientUpdate(float dt)
        {
            m_GroundPosition = Vector3.MoveTowards(m_GroundPosition, m_Target, dt * m_Speed);

            return new NpcEnemyInputModel()
            {
                position = m_GroundPosition,
                rotation = m_Orientation,
                speed = m_Speed,
                enemyState = m_State
            };
        }

        private void UnSetState(EnemyState stateToUnset) => m_State &= ~stateToUnset;
    }
}