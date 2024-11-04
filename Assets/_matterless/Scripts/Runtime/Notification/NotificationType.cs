namespace  Matterless.Floorcraft
{
    public enum NotificationType
    {
        /// <summary>
        /// Someone join your session
        /// </summary>
        OnParticipantJoined,

        /// <summary>
        /// You join other session
        /// </summary>
        OnJoinedRoom,

        /// <summary>
        /// You make other destroy
        /// </summary>
        Destroy,

        /// <summary>
        /// You got destroyed
        /// </summary>
        Destroyed,

        /// <summary>
        /// You left the session
        /// </summary>
        Left,

        /// <summary>
        /// Someone leave the session
        /// </summary>
        OnParticipantLeft,
        StaticLighthouseAssign,
        StaticLighthouseFail
    }
}