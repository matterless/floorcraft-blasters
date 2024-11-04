using Matterless.Module.RemoteConfigs;
using UnityEngine;
using MatterlessRemoteConfigsSettings =  Matterless.Module.RemoteConfigs.RemoteConfigSettings;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Matterless/Environment Settings")]
    public class EnvironmentSettings : ScriptableObject
    {
        [Header("Dev/Stg/Prod")]
        [SerializeField] private MatterlessRemoteConfigsSettings m_DevRemoteConfigs;
        [SerializeField] private MatterlessRemoteConfigsSettings m_StgRemoteConfigs;
        [SerializeField] private MatterlessRemoteConfigsSettings m_ProdRemoteConfigs;

        
        public Module.RemoteConfigs.IRemoteConfigSettings remoteConfigSettings
        {
            get
            {
                // custom devs symbols check should be before MATTERLESS_DEV
                // because we want to allow to have e.g. MATTERLESS_ANDREAS & MATTERLESS_DEV
                // as MATTERLESS_DEV will unlock the Debug View
                
#if MATTERLESS_DEV || MATTERLESS_STG
                return m_StgRemoteConfigs;
#elif MATTERLESS_PROD
                return m_ProdRemoteConfigs;
#elif MATTERLESS_APPSTORE
                return m_ProdRemoteConfigs;
#else
                throw new System.Exception("You have not defined an environment: MATTERLESS_DEV, MATTERLESS_STG, MATTERLESS_PROD");
#endif
            }
        }

        // version
        public string version => $"v{Application.version}{this.remoteConfigSettings.postfix}";
    }
}
