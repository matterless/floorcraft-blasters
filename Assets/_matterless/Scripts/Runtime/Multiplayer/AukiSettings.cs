using Auki.Util;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Matterless.Floorcraft
{
    

    [System.Serializable]
    public class AukiSettings
    {
        // App key, app secret, the posemesh domain id and the editor test session id are not
        // serialized: they are deployment-specific and come from AppSecrets (see Docs/Secrets.md),
        // set once at bootstrap through SetSecrets.
        [System.NonSerialized] private string m_AppKey;
        [System.NonSerialized] private string m_AppSecret;
        [System.NonSerialized] private string m_AppDomainId;
        [System.NonSerialized] private string m_SessionId;

        [Header("Auki")]
        [SerializeField] private AukiDebug.LogLevel m_LogLevel;
        [SerializeField] private bool m_AutoJoinOnStart = true;
        [SerializeField] private bool m_UseGrund = true;

        [Header("Manna")]
        [SerializeField] private MannaService.Settings m_MannaSettings;

        [Header("AR")]
        [SerializeField] private LayerMask m_CameraCullingMask;
        [SerializeField] private HumanSegmentationDepthMode m_HumanSegmentationDepthMode;
        [SerializeField] private HumanSegmentationStencilMode m_HumanSegmentationStencilMode;
        [SerializeField] private EnvironmentDepthMode m_EnvironmentDepthMode;

        [Header("Debug")]
        [Tooltip("Editor only: connect to the session id given by AUKI_EDITOR_SESSION_ID in .env instead of creating one")]
        [SerializeField] private bool m_UseThisSessionIdInEditor = false;

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

        internal void SetSecrets(string appKey, string appSecret, string appDomainId, string editorSessionId)
        {
            m_AppKey = appKey;
            m_AppSecret = appSecret;
            m_AppDomainId = appDomainId;
            m_SessionId = editorSessionId;
        }
    }
}