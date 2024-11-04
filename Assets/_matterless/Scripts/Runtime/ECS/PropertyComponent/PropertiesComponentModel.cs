using System;

namespace Matterless.Floorcraft
{
    public struct PropertiesModel
    {
        public PropertiesModel(uint id)
        {
            this.id = id;
        }

        public uint id { get; set; }
    }

    public class PropertiesComponentModel : EntityComponentModel
    {
        public PropertiesModel model { get; set; }

        public PropertiesComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_UINT];
            model = new PropertiesModel();
        }

        public static PropertiesComponentModel Create(uint typeId, uint entityId, bool isMine) 
            => new(typeId, entityId, isMine);

        public override void Deserialize(byte[] data)
        {
            model = new PropertiesModel(BitConverter.ToUInt32(data));
        }

        public override void Serialize()
        {
            Buffer.BlockCopy(BitConverter.GetBytes(model.id), 0, m_Data, 0, SIZE_OF_UINT);
        }
    }
}