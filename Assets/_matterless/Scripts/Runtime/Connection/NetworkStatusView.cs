using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class NetworkStatusView : UIView<NetworkStatusView>
    {
        #region Inspector

        [SerializeField] private GameObject m_DisconnectedIcon;
        [SerializeField] private RawImage m_GoodQualityNetworkIcon;
        [SerializeField] private RawImage m_MildQualityNetworkIcon;
        [SerializeField] private RawImage m_BadQualityNetworkIcon;
        [SerializeField] private TextMeshProUGUI m_PingText;
        #endregion

        public override NetworkStatusView Init()
        {
#if MATTERLESS_DEV || MATTERLESS_STG
            m_PingText.gameObject.SetActive(true);
#endif
            return this;
        }

        public void UpdateConnectionStatus(ConnectionStatus connectionStatus)
        {
            m_DisconnectedIcon.SetActive(connectionStatus == ConnectionStatus.Disconnected);
            m_GoodQualityNetworkIcon.gameObject.SetActive(connectionStatus == ConnectionStatus.Connected);
            m_MildQualityNetworkIcon.gameObject.SetActive(connectionStatus == ConnectionStatus.Connected);
            m_BadQualityNetworkIcon.gameObject.SetActive(connectionStatus == ConnectionStatus.Connected);
        }

        public void UpdateConnectionQuality(ConnectionQuality connectionQuality)
        {
            m_GoodQualityNetworkIcon.color = connectionQuality == ConnectionQuality.Good ? Color.white : Color.gray;
            m_MildQualityNetworkIcon.color = connectionQuality >= ConnectionQuality.Mild ? Color.white : Color.gray;
            m_BadQualityNetworkIcon.color = connectionQuality >= ConnectionQuality.Bad ? Color.white : Color.gray;
        }

        public void UpdatePing(int ping)
        {
            m_PingText.text = ping.ToString();
        }
    }
}