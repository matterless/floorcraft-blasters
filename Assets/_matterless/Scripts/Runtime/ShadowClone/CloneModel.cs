namespace Matterless.Floorcraft
{
    public class CloneModel
    {
        public CloneModel(uint entityId)
        {
            m_OriginEntityId = entityId;
        }
        public uint originEntityId => m_OriginEntityId;
        private uint m_OriginEntityId;
    }
}

