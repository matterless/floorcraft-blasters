namespace Matterless.Floorcraft
{
    public struct MessageModel
    {
        public enum Message : byte // byte [0..255]
        {
            None = 0,
            Respawn = 1,
            Kill = 2,
            Despawn = 3,
            Honk = 4,
            EnemyKill = 5,
            ObstacleTotaled = 6,
            WaveStart = 7,
            WaveComplete = 8,
            MusicSyncAsk = 9,
            MusicSyncAnswer = 10,
        }

        public MessageModel (Message message, uint entityId)
        {
            this.messageId = (byte)message;
            activeByEntityId = entityId;
        }

        public MessageModel(byte message, uint entityId)
        {
            this.messageId = message;
            activeByEntityId = entityId;
        }

        public uint activeByEntityId { get; private set; }
        public Message message => (Message)messageId;
        public byte messageId { get; private set; }
    }
}