using System;

namespace Matterless.Floorcraft
{
    public class CloneComponentModel : EntityComponentModel
    {
        public CloneModel model { get; set; }
        public CloneComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_UINT];
        }

        public override void Serialize()
        {
            Buffer.BlockCopy(BitConverter.GetBytes(model.originEntityId), 0, m_Data, 0, SIZE_OF_UINT);
        }

        public override void Deserialize(byte[] data)
        {
            model = new CloneModel(BitConverter.ToUInt32(data, 0));
        }
    }
}

