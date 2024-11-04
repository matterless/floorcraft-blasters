using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft.TestECS
{

    public class TestECSInstaller : MonoInstaller
    {
        [SerializeField] private TestECSAppConfigs m_AppConfigs;
        [SerializeField] private TestECSAppView m_AppView;
        [SerializeField] ARSessionOrigin m_ArSessionOrigin;
        [SerializeField] ARSession m_ArSession;

        protected override void InstallBindings()
        {
            // get arguments
            var appConfig = m_AppConfigs;//arguments[0] as AppConfigs;

            // settings
            container.BindInstance(m_AppView);
            container.BindInstance(appConfig.aukiSettings);
            container.BindInstance(appConfig.appSettings);
            container.BindInstance(m_ArSessionOrigin);
            container.BindInstance(m_ArSession);

            container.Bind<ICoroutineRunner, CoroutineRunner>(this);
            container.Bind<IAnalyticsService, TestECSAnalyticsSerice>();
            container.Bind<IAukiWrapper, AukiWrapper>();

            //ECS
            container.Bind<IComponentModelFactory, TestECSComponentModelFactory>();
            container.Bind<IECSController, ECSController>();
            container.Bind<TestECSTestComponentService>();

            // APP
            container.Bind<TestECSApp>();
        }
    }
}