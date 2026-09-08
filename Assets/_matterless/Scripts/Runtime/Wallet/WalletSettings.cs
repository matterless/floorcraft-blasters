using UnityEngine;

namespace Matterless.Floorcraft
{   
    [System.Serializable]
    public class WalletSettings 
    {
        public string projectId => m_ProjectId;
        public string projectName => m_ProjectName;
        public string projectDescription => m_ProjectDescription;
        public string projectUrl => m_ProjectUrl;
        public string projectIconUrl => m_ProjectIconUrl;

        // WalletConnect is provided by Reown and is used to connect to the wallet.
        // The project id is not serialized: it comes from AppSecrets (see Docs/Secrets.md).
        // To get one, visit https://docs.reown.com/appkit/unity/core/installation
        [System.NonSerialized] private string m_ProjectId;

        [Header("Reown Settings")]
        [SerializeField] private string m_ProjectName = "YOUR_PROJECT_NAME";
        [SerializeField] private string m_ProjectDescription = "YOUR_PROJECT_DESCRIPTION";
        [SerializeField] private string m_ProjectUrl = "YOUR_PROJECT_URL";
        [SerializeField] private string m_ProjectIconUrl = "YOUR_PROJECT_ICON_URL";

        internal void SetSecrets(string projectId)
        {
            m_ProjectId = projectId;
        }
    }
}