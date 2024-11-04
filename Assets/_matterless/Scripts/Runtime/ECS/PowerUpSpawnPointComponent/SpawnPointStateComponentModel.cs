using System;
namespace Matterless.Floorcraft
{
    public class SpawnPointStateComponentModel : EntityComponentModel
    {
        public SpawnPointCooldownStateModel model { get; set; }

        public static SpawnPointStateComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new SpawnPointStateComponentModel(typeId, entityId, isMine);

        public SpawnPointStateComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[1];
        }

        public override void Deserialize(byte[] data)
        {
            model = new SpawnPointCooldownStateModel(data[0] == 1);
        }

        public override void Serialize()
        {
            m_Data[0] = Convert.ToByte(model.inCooldown);
        }
    }
}
