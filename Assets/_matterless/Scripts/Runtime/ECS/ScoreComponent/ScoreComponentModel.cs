using System;
namespace Matterless.Floorcraft
{
    public class ScoreComponentModel : EntityComponentModel
    {
        public ScoreModel model { get; set; }

        public static ScoreComponentModel Create(uint typeId, uint entityId, bool isMine) =>
            new ScoreComponentModel(typeId, entityId, isMine);
        public ScoreComponentModel(uint typeId, uint entityId, bool isMine) : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE_OF_INT];
            model = new ScoreModel();
        }

        public override void Serialize()
        {
            Buffer.BlockCopy(BitConverter.GetBytes(model.score), 0, m_Data, 0, SIZE_OF_INT);
        }

        public override void Deserialize(byte[] data)
        {
            model = new ScoreModel(BitConverter.ToInt32(data, 0));
        }
    }
}


