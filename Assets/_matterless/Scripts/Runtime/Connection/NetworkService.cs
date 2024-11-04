using System;
using Auki.ConjureKit;
using Matterless.Inject;
using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class NetworkService : INetworkService, ITickable
    {
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly Settings m_Settings;
        private readonly NetworkStatusView m_View;

        public event Action<ConnectionQuality> onNetworkQualityChanged;
        public event Action<ConnectionStatus> onNetworkConnectionChanged;
        public event Action onForceRefresh;

        private float m_MeasurementTimer = 0;
        private int m_MeasurementCountAfterRefresh = 0;
        private bool m_IsRecentlyRefreshed;
        private ConnectionQuality m_CurrentConnectionQuality;
        private ConnectionStatus m_CurrentConnectionStatus;
        private NetworkReachability m_CurrentNetworkReachability;
        
        public NetworkService(
            IAukiWrapper aukiWrapper,
            Settings settings)
        {
            m_AukiWrapper = aukiWrapper;
            m_Settings = settings;
            m_View = NetworkStatusView.Create("UIPrefabs/UIP_NetworkStatusView").Init();
        }
        
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            m_MeasurementTimer += deltaTime;
            if (m_MeasurementTimer >= m_Settings.networkMeasurementFrequency)
            { 
                // MeasureNetwork();
                m_MeasurementTimer = 0;
            }
        }

        // private void MeasureNetwork()
        // {
        //     if (CheckInternet())
        //     {
        //         m_AukiWrapper.MeasurePing(OnPingResponse, OnPingError);
        //     }
        // }

        private bool CheckInternet()
        {
            NetworkReachability networkReachability = Application.internetReachability;

            if (networkReachability != m_CurrentNetworkReachability)
            {
                m_CurrentNetworkReachability = networkReachability;
                m_CurrentConnectionStatus = m_CurrentNetworkReachability != NetworkReachability.NotReachable ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                
                onNetworkConnectionChanged?.Invoke(m_CurrentConnectionStatus);
                m_View.UpdateConnectionStatus(m_CurrentConnectionStatus);
            }

            return m_CurrentConnectionStatus == ConnectionStatus.Connected;
        }

        private void OnPingResponse(double ms)
        {
            NetworkQuality nq = m_AukiWrapper.GetNetworkQuality();
#if MATTERLESS_DEV || MATTERLESS_STG
            m_View.UpdatePing(System.Convert.ToInt32(nq.AverageRoundtripTimeInMillisecondsLastTen <= -1
                ? ms
                : nq.AverageRoundtripTimeInMillisecondsLastTen));
#endif
            ConnectionQuality connectionQuality = GetConnectionQuality(nq.AverageRoundtripTimeInMillisecondsLastTen <= -1
                ? ms
                : nq.AverageRoundtripTimeInMillisecondsLastTen);

            if (m_CurrentConnectionQuality != connectionQuality)
            {
                m_CurrentConnectionQuality = connectionQuality;
                onNetworkQualityChanged?.Invoke(m_CurrentConnectionQuality);
                m_View.UpdateConnectionQuality(m_CurrentConnectionQuality);
            }

            if (nq.AverageRoundtripTimeInMillisecondsLastTen >= m_Settings.refreshLatency)
            {
                // the game is very unplayable at these values, force user to refresh session maybe?
                if (!m_IsRecentlyRefreshed)
                {
                    onForceRefresh?.Invoke();
                    m_IsRecentlyRefreshed = true;
                }
            }

            if (ms >= m_Settings.disconnectLatency)
            {
                // latency is too big that we might want game to be disconnected instead?
            }
            
            // We need to give some time to network to see if it gets better without trying to refresh again
            m_MeasurementCountAfterRefresh++;
            if (m_MeasurementCountAfterRefresh >= 10)
            {
                m_MeasurementCountAfterRefresh = 0;
                m_IsRecentlyRefreshed = false;
            }
        }

        private ConnectionQuality GetConnectionQuality(double ms)
        {
            // temporary values to test
            if (ms <= m_Settings.goodLatency)
            {
                return ConnectionQuality.Good;
            }

            if (m_Settings.goodLatency < ms && ms <= m_Settings.mildLatency)
            {
                return ConnectionQuality.Mild;
            }

            return ConnectionQuality.Bad;
        }
        
        private void OnPingError(string error)
        {
            Debug.Log($"Ping failed error: {error}");
        }

        private void DebugNetworkQuality(NetworkQuality networkQuality)
        {
            Debug.Log($"HagallUri {networkQuality.HagallUri}");
            Debug.Log($"AverageRoundtripTimeInMilliseconds {networkQuality.AverageRoundtripTimeInMilliseconds}");
            Debug.Log($"LastRoundtripTimeInMilliseconds {networkQuality.LastRoundtripTimeInMilliseconds}");
            Debug.Log($"AverageRoundtripTimeInMillisecondsLastTen {networkQuality.AverageRoundtripTimeInMillisecondsLastTen}");
            Debug.Log($"LongestRoundtripTimeInMilliseconds {networkQuality.LongestRoundtripTimeInMilliseconds}");
            Debug.Log($"ShortestRoundtripTimeInMilliseconds {networkQuality.ShortestRoundtripTimeInMilliseconds}");
        }
        
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private float m_NetworkMeasurementFrequency;
            [SerializeField] private float m_DisconnectLatency;
            [SerializeField] private float m_RefreshLatency;
            [SerializeField] private float m_GoodLatency;
            [SerializeField] private float m_MildLatency;
            [SerializeField] private float m_BadLatency;
            
            public float networkMeasurementFrequency => m_NetworkMeasurementFrequency;
            public float disconnectLatency => m_DisconnectLatency;
            public float refreshLatency => m_RefreshLatency;
            public float goodLatency => m_GoodLatency;
            public float mildLatency => m_MildLatency;
            public float badLatency => m_BadLatency;
        }
    }
}
