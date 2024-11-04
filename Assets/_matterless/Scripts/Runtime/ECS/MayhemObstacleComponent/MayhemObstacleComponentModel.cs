using System;

namespace Matterless.Floorcraft
{
    public class MayhemObstacleComponentModel : EntityComponentModel
    {
        public MayhemObstacleModel model { get; set; }
        private int[] m_IntData;
        
        public static MayhemObstacleComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new MayhemObstacleComponentModel(typeId, entityId, isMine);

        public MayhemObstacleComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_INT * 3];
            m_IntData = new int[3];
            model = new MayhemObstacleModel();
        }
        
        public override void Deserialize(byte[] data)
        {
            Converters.ByteToInt(data, ref m_IntData);

            int state = m_IntData[0];
            int health = m_IntData[1];
            int waveNumber = m_IntData[2];

            model = new MayhemObstacleModel(state, health, waveNumber);
        }
        
        public override void Serialize()
        {
            Buffer.BlockCopy(BitConverter.GetBytes(model.stateId), 0, m_Data, SIZE_OF_INT * 0, SIZE_OF_INT);
            Buffer.BlockCopy(BitConverter.GetBytes(model.health), 0, m_Data, SIZE_OF_INT * 1, SIZE_OF_INT);
            Buffer.BlockCopy(BitConverter.GetBytes(model.waveNumber), 0, m_Data, SIZE_OF_INT * 2, SIZE_OF_INT);

        }
    }
}