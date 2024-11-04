namespace Matterless.Floorcraft
{
    public abstract class CustomMessage
    {
        protected const int SIZE_OF_INT = sizeof(int);
        protected const int SIZE_OF_UINT = sizeof(uint);
        public CustomMessageId id;

        public CustomMessage(byte id)
        {
            this.id = (CustomMessageId)id;
        }
        
        public abstract byte[] GetBytes();
    }
}