using Auki.Util;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Matterless.Floorcraft
{
    

    [System.Serializable]
    public class AukiSettings
    {
        [Header("Auki")]
        [SerializeField] private string m_AppKey;
        [SerializeField] private string m_AppSecret;
        [SerializeField] private AukiDebug.LogLevel m_LogLevel;
        [SerializeField] private bool m_AutoJoinOnStart = true;
        [SerializeField] private bool m_UseGrund = true;
        [SerializeField] private string m_AppDomainId;

        [Header("Manna")]
        [SerializeField] private MannaService.Settings m_MannaSettings;

        [Header("AR")]
        [SerializeField] private LayerMask m_CameraCullingMask;
        [SerializeField] private HumanSegmentationDepthMode m_HumanSegmentationDepthMode;
        [SerializeField] private HumanSegmentationStencilMode m_HumanSegmentationStencilMode;
        [SerializeField] private EnvironmentDepthMode m_EnvironmentDepthMode;

        [Header("Debug")][SerializeField] private bool m_UseThisSessionIdInEditor = false;
        [SerializeField] private string m_SessionId;

        public MannaService.Settings mannaSettings => m_MannaSettings;
        public string appKey => m_AppKey;
        public string appSecret => m_AppSecret;
        public string appDomainId => m_AppDomainId;
        public AukiDebug.LogLevel logLevel => m_LogLevel;
        public bool autoJoinOnStart => m_AutoJoinOnStart;
        public bool useGrund => m_UseGrund;
        public LayerMask cameraCullingMask => m_CameraCullingMask;
        public HumanSegmentationDepthMode humanSegmentationDepthMode => m_HumanSegmentationDepthMode;
        public HumanSegmentationStencilMode humanSegmentationStencilMode => m_HumanSegmentationStencilMode;
        public EnvironmentDepthMode environmentDepthMode => m_EnvironmentDepthMode;
        public bool useThisSessionIdInEditor => m_UseThisSessionIdInEditor;
        public string sessionId => m_SessionId;
    }
}