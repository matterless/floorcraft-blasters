using System;
namespace Matterless.Floorcraft
{
    public class NameComponentModel : EntityComponentModel
    {
        public NameModel model { get; set; }
        public static NameComponentModel Create(uint typeId, uint entityId, bool isMine) =>
            new NameComponentModel(typeId, entityId, isMine);
        public NameComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_INT];
            model = new NameModel(0);
        }

        public override void Serialize()
        {
            Buffer.BlockCopy(BitConverter.GetBytes(model.name), 0, m_Data, 0, SIZE_OF_INT);
        }

        public override void Deserialize(byte[] data)
        {
            model = new NameModel(BitConverter.ToInt32(data, 0));
        }
    }
}

