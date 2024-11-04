using UnityEngine;

namespace Matterless.Floorcraft.TestECS
{
    public class TestECSTestComponentModel : EntityComponentModel
    {
        private const int SIZE = 3;

        public Color color { get; set; }

        public TestECSTestComponentModel(uint typeId, uint entityId, bool isMine) 
            : base(typeId, entityId, isMine)
        {
            m_Data = new byte[SIZE];
        }

        public static IEntityComponentModel Create(uint typeId, uint entityId, bool isMine)
            => new TestECSTestComponentModel(typeId, entityId, isMine);
        
        public override void Deserialize(byte[] data)
        {
            // deserialize
            color = new Color(
                ((float)data[0]) / 255f,
                ((float)data[1]) / 255f,
                ((float)data[2]) / 255f);
        }

        public override void Serialize()
        {
            data[0] = (byte)Mathf.FloorToInt(color.r * 255);
            data[1] = (byte)Mathf.FloorToInt(color.g * 255);
            data[2] = (byte)Mathf.FloorToInt(color.b * 255);
        }
    }
}