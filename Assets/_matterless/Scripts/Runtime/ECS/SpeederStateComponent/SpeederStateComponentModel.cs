namespace Matterless.Floorcraft
{
    public class SpeederStateComponentModel : EntityComponentModel
    {
        public SpeederStateModel model { get; set; }

        public static SpeederStateComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new SpeederStateComponentModel(typeId, entityId, isMine);

        public SpeederStateComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[1];
        }

        public override void Deserialize(byte[] data)
        {
            model = new SpeederStateModel(data[0]);
        }

        public override void Serialize()
        {
            m_Data[0] = model.stateId;
        }
    }
}