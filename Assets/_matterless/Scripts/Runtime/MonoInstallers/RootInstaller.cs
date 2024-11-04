using UnityEngine;
using Matterless.Inject;
using Matterless.UTools;
using Matterless.Audio;
using Matterless.Localisation;
using UnityEngine.XR.ARFoundation;
using Matterless.Module.UI;
using Matterless.Module.RemoteConfigs;

namespace Matterless.Floorcraft
{
    public class RootInstaller : MonoInstaller
    {
        [SerializeField] private EnvironmentSettings m_EnvironmentSettings;
        [SerializeField] private AppConfigs m_AppConfigs;
        [SerializeField] private AudioDatabase m_AudioDatabase;
        [SerializeField] private ObjectContext m_AppContext;
        [SerializeField] private ObjectContext m_UiContext;
        [SerializeField] private ObjectContext m_DebugContext;

        [Header("Auki")]
        [SerializeField] bool m_UseAukiWrapperMock = false;
        [SerializeField] ARSessionOrigin m_arSessionOrigin;
        [SerializeField] ARSession m_arSession;

        private void SetupRendering(RenderingSettings settings)
        {
            Application.targetFrameRate = settings.targetFrameRate;
            m_arSession.matchFrameRateRequested = settings.arMatchFrameRateRequested;

            Debug.Log($"Rendering settings, set target frame rate: {settings.targetFrameRate}, ar match frame rate requested: {settings.arMatchFrameRateRequested}");
        }

        protected override void InstallBindings()
        {
            // app rendering setting init
            SetupRendering(m_AppConfigs.renderingSettings);

            // settings
            container.BindInstance(m_AppConfigs.aukiSettings);
            container.BindInstance(m_AppConfigs.aukiSettings.mannaSettings);
            container.BindInstance(m_AppConfigs.networkServiceSettings);

            // bind instances
            container.BindInstance(m_arSessionOrigin);
            container.BindInstance(m_arSession);
            
            // bind services
            if (Application.isEditor && m_UseAukiWrapperMock)
                container.Bind<IAukiWrapper, AukiWrapperMock>();
            else
                container.Bind<IAukiWrapper, AukiWrapper>();
            container.Bind<IMannaService, MannaService>();

            container.Bind<IPoolingService, PoolingService>();
            container.Bind<BacktraceService>();
            container.Bind<IRemoteConfigService, RemoteConfigService>(m_EnvironmentSettings.remoteConfigSettings);
            container.Bind<IAnalyticsService, AnalyticsService>();

            container.Bind<IPlayerPrefsService,PlayerPrefsService>();
            container.Bind<ICoroutineRunner, CoroutineRunner>(this);
            container.Bind<IUnityEventDispatcher, UnityEventDispatcher>(this.gameObject);
            container.Bind<IAudioService, AudioService>(m_AudioDatabase, false);
            container.Bind<AudioUiService>(m_AppConfigs.audioUiSettings);
            container.Bind<ILocalisationService, LocalisationService>();
            container.Bind<IRestService, RestService>();
            container.Bind<IInputDialogueService, InputDialogueService>();

            container.Bind<SplashScreenService>(m_AppConfigs.splashScreenSettings, m_EnvironmentSettings.version);
            container.Bind<Bootstrap>(m_AppContext, m_UiContext, m_DebugContext, m_AppConfigs);
            container.Bind<INetworkService, NetworkService>();
        }
    }
}