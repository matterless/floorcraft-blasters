using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Component Model
    /// </summary>
    public class TransformComponentModel : EntityComponentModel
    {
        public static TransformComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new TransformComponentModel(typeId, entityId, isMine);
        
        public TransformModel model { get; set; }

        private float[] m_FloatData;

        public TransformComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[8 * SIZE_OF_FLOAT];
            m_FloatData = new float[8];
            model = new TransformModel();
        }

        //public override void Serialize()
        //{
        //    // position
        //    NonAllocationConverter.GetBytes(model.position.x, ref m_Data, SIZE_OF_FLOAT * 0);
        //    NonAllocationConverter.GetBytes(model.position.y, ref m_Data, SIZE_OF_FLOAT * 1);
        //    NonAllocationConverter.GetBytes(model.position.z, ref m_Data, SIZE_OF_FLOAT * 2);
        //    // rotation
        //    NonAllocationConverter.GetBytes(model.rotation.x, ref m_Data, SIZE_OF_FLOAT * 3);
        //    NonAllocationConverter.GetBytes(model.rotation.y, ref m_Data, SIZE_OF_FLOAT * 4);
        //    NonAllocationConverter.GetBytes(model.rotation.z, ref m_Data, SIZE_OF_FLOAT * 5);
        //    NonAllocationConverter.GetBytes(model.rotation.w, ref m_Data, SIZE_OF_FLOAT * 6);
        //    // speed
        //    NonAllocationConverter.GetBytes(model.speed, ref m_Data, SIZE_OF_FLOAT * 7);
        //}

        //public override void Deserialize(byte[] data)
        //{
        //    model = new(
        //        new Vector3(
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 0),
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 1),
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 2)),
        //        new Quaternion(
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 3),
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 4),
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 5),
        //        NonAllocationConverter.ToSingle(data, SIZE_OF_FLOAT * 6)),
        //        // speed
        //        NonAllocationConverter.ToSingle(data,   7));
        //}

        public override void Serialize()
        {
            // vector3
            // x
            Buffer.BlockCopy(BitConverter.GetBytes(model.position.x), 0, m_Data, SIZE_OF_FLOAT * 0, SIZE_OF_FLOAT);
            // y
            Buffer.BlockCopy(BitConverter.GetBytes(model.position.y), 0, m_Data, SIZE_OF_FLOAT * 1, SIZE_OF_FLOAT);
            // z
            Buffer.BlockCopy(BitConverter.GetBytes(model.position.z), 0, m_Data, SIZE_OF_FLOAT * 2, SIZE_OF_FLOAT);
            // quaternion
            // x
            Buffer.BlockCopy(BitConverter.GetBytes(model.rotation.x), 0, m_Data, SIZE_OF_FLOAT * 3, SIZE_OF_FLOAT);
            // y
            Buffer.BlockCopy(BitConverter.GetBytes(model.rotation.y), 0, m_Data, SIZE_OF_FLOAT * 4, SIZE_OF_FLOAT);
            // z
            Buffer.BlockCopy(BitConverter.GetBytes(model.rotation.z), 0, m_Data, SIZE_OF_FLOAT * 5, SIZE_OF_FLOAT);
            // w
            Buffer.BlockCopy(BitConverter.GetBytes(model.rotation.w), 0, m_Data, SIZE_OF_FLOAT * 6, SIZE_OF_FLOAT);
            // speed
            Buffer.BlockCopy(BitConverter.GetBytes(model.speed), 0, m_Data, SIZE_OF_FLOAT * 7, SIZE_OF_FLOAT);
        }

        public override void Deserialize(byte[] data)
        {
            Converters.ByteToFloat(data, ref m_FloatData);

            model = new TransformModel(
                new Vector3(m_FloatData[0], m_FloatData[1], m_FloatData[2]),
                new Quaternion(m_FloatData[3], m_FloatData[4], m_FloatData[5], m_FloatData[6]),
                m_FloatData[7]);
        }
    }
}