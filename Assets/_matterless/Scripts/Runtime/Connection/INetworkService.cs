using System;

namespace Matterless.Floorcraft
{
    public interface INetworkService
    {
        event Action<ConnectionQuality> onNetworkQualityChanged;
        event Action<ConnectionStatus> onNetworkConnectionChanged;
        event Action onForceRefresh;
    }
}