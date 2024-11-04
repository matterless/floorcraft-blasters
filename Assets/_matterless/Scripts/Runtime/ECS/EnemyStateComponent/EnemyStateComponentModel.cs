using System;

namespace Matterless.Floorcraft
{
    public class EnemyStateComponentModel : EntityComponentModel
    {
        public EnemyStateModel model { get; set; }
        
        public static EnemyStateComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new EnemyStateComponentModel(typeId, entityId, isMine);

        public EnemyStateComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_INT + 1];
        }
        
        public override void Deserialize(byte[] data)
        {
            byte state = data[0];
            int health = BitConverter.ToInt32(data, 1);

            model = new EnemyStateModel(state, health);
        }
        
        public override void Serialize()
        {
            m_Data[0] = model.stateId;
            Buffer.BlockCopy(BitConverter.GetBytes(model.health), 0, m_Data, 1, SIZE_OF_INT);
        }
    }
}