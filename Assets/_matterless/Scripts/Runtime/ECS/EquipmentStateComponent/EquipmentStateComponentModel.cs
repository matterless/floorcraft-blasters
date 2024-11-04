namespace Matterless.Floorcraft
{
    public class EquipmentStateComponentModel : EntityComponentModel
    {
        public EquipmentStateModel model { get; set; }

        public static EquipmentStateComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new EquipmentStateComponentModel(typeId, entityId, isMine);

        public EquipmentStateComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[1];
        }

        public override void Deserialize(byte[] data)
        {
            model = new EquipmentStateModel((EquipmentState)data[0]);
        }

        public override void Serialize()
        {
            m_Data[0] = (byte)model.state;
        }
    }
}