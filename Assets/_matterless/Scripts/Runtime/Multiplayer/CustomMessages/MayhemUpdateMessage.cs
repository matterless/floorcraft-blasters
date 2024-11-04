using System;

namespace Matterless.Floorcraft
{
    public class MayhemUpdateMessage : CustomMessage
    {
        public int towerHealth;
        public int waveNumber;

        public MayhemUpdateMessage(CustomMessageId id, int towerHealth, int waveNumber) : base((byte)id)
        {
            this.towerHealth = towerHealth;
            this.waveNumber = waveNumber;
        }

        public override byte[] GetBytes()
        {
            byte[] bytes = new byte[1 + SIZE_OF_INT * 2];
            bytes[0] = (byte)id;
            Buffer.BlockCopy(BitConverter.GetBytes(towerHealth), 0, bytes, 1, SIZE_OF_INT);
            Buffer.BlockCopy(BitConverter.GetBytes(waveNumber), 0, bytes, 1 + SIZE_OF_INT * 1, SIZE_OF_INT);
            return bytes;
        }
        
        public MayhemUpdateMessage(byte[] bytes) : base(bytes[0])
        {
            towerHealth = BitConverter.ToInt32(bytes, 1);
            waveNumber = BitConverter.ToInt32(bytes, 1 + SIZE_OF_INT * 1);
        }
    }
}