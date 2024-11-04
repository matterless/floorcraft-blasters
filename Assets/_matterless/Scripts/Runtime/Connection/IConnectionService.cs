using System;

namespace Matterless.Floorcraft
{
    public interface IConnectionService
    {
        event Action<ConnectionState> onConnectionStateChanged;

        bool isReady { get; }
        string connectedSessionId { get; }

        void NewSession(Action onComplete = null, Action<string> onFail = null);
        void JoinSession(string sessionId);
        void Reconnect(float delay);
        void LeaveSession();
        void LeaveSessionAndReconnect(float delay);

    }
}