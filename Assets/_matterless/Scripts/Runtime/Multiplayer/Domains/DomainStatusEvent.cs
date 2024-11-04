namespace Matterless.Floorcraft
{
    public struct DomainStatusEvent
    {
        public DomainState state;
        public string uniqueSessionId;
        public string sessionId;
        public string threshold;
    }
}