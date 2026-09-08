using Matterless.Inject;

namespace Matterless.Floorcraft
{

    public class UiInstaller : MonoInstaller
    {
        protected override void InstallBindings()
        {
            // get arguments
            var appConfig = arguments[0] as AppConfigs;

            // settings
            container.BindInstance(appConfig.vehicleSelectorSettings);
            container.BindInstance(appConfig.placeableSelectorSettings);
            container.BindInstance(appConfig.obstacleSettings);
            container.BindInstance(appConfig.connectionIndicationSettings);
            container.BindInstance(appConfig.networkServiceSettings);
            container.BindInstance(appConfig.audioUiSettings);
            container.BindInstance(appConfig.rendererSettings);
            container.BindInstance(appConfig.recordingSettings);

            // sevices
            container.Bind<HeaderUiService>();
            container.Bind<SidebarUiService>();
            container.Bind<UiFlowService>();
            container.Bind<IRendererService, RendererService>();
            container.Bind<IntroUiService>();
            container.Bind<IVehicleSelectorService, VehicleSelectorService>();
            container.Bind<PlaceableSelectorService>();
            container.Bind<SpawningService>();
            container.Bind<ConnectionIndicatorService>();
            container.Bind<QrCodeUiService>();
            container.Bind<ObstaclesUiService>();
#if AVPRO_MOVIECAPTURE
            // AVPro Movie Capture is a paid plugin that is not part of the open-source repository.
            // The define comes from Assets/_matterless/Scripts/Runtime/csc.rsp, written by tools/paid-assets.sh.
            container.Bind<IRecordingService, RecordingService>();
#else
            container.Bind<IRecordingService, DummyRecordingService>();
#endif

            // this is a mock implementation of IScreenService
            // that locks screen orientation changes
            container.Bind<IScreenService, ScreenServiceLocked>();

            container.Bind<WalletUiService>();
        }
    }
}