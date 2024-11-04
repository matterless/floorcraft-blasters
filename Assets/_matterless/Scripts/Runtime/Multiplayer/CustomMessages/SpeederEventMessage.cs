using System;

namespace Matterless.Floorcraft
{
    public class SpeederEventMessage : CustomMessage
    {
        public MessageModel.Message messageType;
        public uint activeByEntityId;
        public uint entityId;

        public SpeederEventMessage(CustomMessageId id, uint entityId, uint activeByEntityId, MessageModel.Message messageType) : base((byte)id)
        {
            this.messageType = messageType;
            this.entityId = entityId;
            this.activeByEntityId = activeByEntityId;
        }
        
        public override byte[] GetBytes()
        {
            byte[] bytes = new byte[2 + 2 * SIZE_OF_UINT];
            bytes[0] = (byte)id;
            bytes[1] = (byte)messageType;
            Buffer.BlockCopy(BitConverter.GetBytes(entityId), 0, bytes, 2, SIZE_OF_UINT);
            Buffer.BlockCopy(BitConverter.GetBytes(activeByEntityId), 0, bytes, 2 + SIZE_OF_UINT, SIZE_OF_UINT);
            return bytes;
        }
        
        public SpeederEventMessage (byte[] bytes) : base(bytes[0])
        {
            messageType = (MessageModel.Message)bytes[1];
            entityId = BitConverter.ToUInt32(bytes, 2);
            activeByEntityId = BitConverter.ToUInt32(bytes, 2 + SIZE_OF_UINT);
        }
    }
}