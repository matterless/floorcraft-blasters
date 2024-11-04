using System;

namespace Matterless.Floorcraft
{
    public class MayhemEventMessage : CustomMessage
    {
        public MessageModel.Message messageType;

        public MayhemEventMessage(CustomMessageId id, MessageModel.Message messageType) : base((byte)id)
        {
            this.messageType = messageType;
        }
        
        public override byte[] GetBytes()
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)id;
            bytes[1] = (byte)messageType;
            return bytes;
        }
        
        public MayhemEventMessage (byte[] bytes) : base(bytes[0])
        {
            messageType = (MessageModel.Message)bytes[1];
        }
    }
}