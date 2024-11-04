using System;

namespace Matterless.Module.RemoteConfigs
{
    public interface IRemoteConfigService
    {
        void GetRemoteCatalogue(Action callback);
        void GetRemoteConfig(string config, Action<string> callback = null);
        void GetRemoteConfigs(Action onComplete, Action onError, params string[] configs );
        void RegisterConfig(string config, Action<string> callback);
    }
}