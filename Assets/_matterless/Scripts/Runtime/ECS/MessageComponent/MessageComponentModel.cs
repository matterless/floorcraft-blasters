using System;

namespace Matterless.Floorcraft
{
    public class MessageComponentModel : EntityComponentModel
    {
        public MessageModel model { get; set; }

        public static MessageComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new MessageComponentModel(typeId, entityId, isMine);

        public MessageComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_UINT + 1];
        }

        public override void Deserialize(byte[] data)
        {
            model = new MessageModel(
                data[0],
                BitConverter.ToUInt32(data, 1));
        }

        public override void Serialize()
        {
            // message
            m_Data[0] = model.messageId;
            // entity id
            Buffer.BlockCopy(BitConverter.GetBytes(model.activeByEntityId), 0, m_Data, 1, SIZE_OF_UINT);
        }
    }
}