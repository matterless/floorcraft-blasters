using Matterless.Inject;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public class DebugInstaller : MonoInstaller
    {
        [SerializeField] private ARPlaneManager m_ARPlaneManager;
        
        protected override void InstallBindings()
        {
            // get arguments
            var appConfig = arguments[0] as AppConfigs;

            // settings
            container.BindInstance(appConfig.arPlaneOverlaySettings);
            container.BindInstance(m_ARPlaneManager);

            // services
            container.Bind<ARPlaneOverlayService>();
            container.Bind<MaterialDebugService>();
            container.Bind<DebugService>();
        }
    }
}