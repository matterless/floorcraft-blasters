namespace Matterless.Floorcraft
{
    public struct SpawnLocationsModel
    {
        public SpawnLocationsModel(byte[] data)
        {
            m_Data = data;
        }

        private byte[] m_Data;
        public byte[] data => m_Data;
    }
}