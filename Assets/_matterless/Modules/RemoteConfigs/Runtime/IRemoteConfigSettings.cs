namespace Matterless.Module.RemoteConfigs
{
    public interface IRemoteConfigSettings
    {
        string apiKey { get; }
        string postfix { get; }
    }
}