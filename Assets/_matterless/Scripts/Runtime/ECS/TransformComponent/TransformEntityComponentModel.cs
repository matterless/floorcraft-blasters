using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class TransformEntityComponentModel : EntityComponentModel
    {
        int SIZE_OF_FLOAT = sizeof(float);

        private float[] m_FloatData;

        public Vector3 position { get; private set; }
        public Quaternion rotation {get; private set; }
        public float speed { get; private set; }

        public TransformEntityComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[8 * SIZE_OF_FLOAT];
            m_FloatData = new float[8];
        }

        public override void Serialize()
        {
            // vector3
            // x
            Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, m_Data, SIZE_OF_FLOAT * 0, SIZE_OF_FLOAT);
            // y
            Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, m_Data, SIZE_OF_FLOAT * 1, SIZE_OF_FLOAT);
            // z
            Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, m_Data, SIZE_OF_FLOAT * 2, SIZE_OF_FLOAT);
            // quaternion
            // x
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, m_Data, SIZE_OF_FLOAT * 3, SIZE_OF_FLOAT);
            // y
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, m_Data, SIZE_OF_FLOAT * 4, SIZE_OF_FLOAT);
            // z
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, m_Data, SIZE_OF_FLOAT * 5, SIZE_OF_FLOAT);
            // w
            Buffer.BlockCopy(BitConverter.GetBytes(rotation.w), 0, m_Data, SIZE_OF_FLOAT * 6, SIZE_OF_FLOAT);
            // speed
            Buffer.BlockCopy(BitConverter.GetBytes(speed), 0, m_Data, SIZE_OF_FLOAT * 7, SIZE_OF_FLOAT);
        }

        public override void Deserialize(byte[] data)
        {
            Converters.ByteToFloat(data, ref m_FloatData);
            
            position = new Vector3(m_FloatData[0], m_FloatData[1], m_FloatData[2]);
            rotation = new Quaternion(m_FloatData[3], m_FloatData[4], m_FloatData[5], m_FloatData[6]);
            speed = m_FloatData[7];
        }

        public void Update(Vector3 pos, Quaternion rot, float speed)
        {
            position = pos;
            rotation = rot;
            this.speed = speed;
            Serialize();
        }    
    }
}