using UnityEngine;

namespace Matterless.Floorcraft
{
    public class FlameThrowerView : GameObjectView
    {
        private Rigidbody m_RigidBody;
        public Rigidbody rigidBody => m_RigidBody;

        void Awake()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        public void Init(uint id)
        {
            entityId = id;
        }
    }
}