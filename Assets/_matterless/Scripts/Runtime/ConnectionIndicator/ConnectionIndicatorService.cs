using Matterless.Localisation;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ConnectionIndicatorService
    {
        private readonly HeaderUiService m_HeaderUiService;
        private readonly IConnectionService m_ConnectionService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly Settings m_Settings;
        private readonly INetworkService m_NetworkService;

        private ConnectionState m_State = ConnectionState.Disconnected;
        private bool m_ConnectedToDomain = false;


        public ConnectionIndicatorService(
            IConnectionService connectionService,
            IDomainService domainService,
            ILocalisationService localisationService,
            Settings settings,
            HeaderUiService headerUiService,
            INetworkService networkService)
        {
            m_HeaderUiService = headerUiService;
            m_ConnectionService = connectionService;
            m_LocalisationService = localisationService;
            m_Settings = settings;
            m_NetworkService = networkService;
            m_ConnectionService.onConnectionStateChanged += OnConnectionStateChanged;
            m_NetworkService.onNetworkConnectionChanged += OnConnectionStatusChanged;
            domainService.onDomainStateChanged += OnDomainStateChanged;

            // init
            OnConnectionStateChanged(ConnectionState.Disconnected);

            m_HeaderUiService.SetVersion($"v{Application.version}");
            m_LocalisationService.onLanguageChanged += UpdateLabel;
        }

        private void OnDomainStateChanged(DomainStatusEvent evt)
        {
            m_ConnectedToDomain = evt.state == DomainState.Connected;
            UpdateLabel();
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            m_State = state;
            UpdateLabel();
        }
        
        private void OnConnectionStatusChanged(ConnectionStatus connectionStatus)
        {
            // Handling disconnect status only since Connected state handled by auki wrapper
            if (connectionStatus == ConnectionStatus.Disconnected && m_State != ConnectionState.Disconnected)
            {
                m_State = ConnectionState.Disconnected;
                UpdateLabel();
            }
        }

        private void UpdateLabel()
        {
            switch(m_State)
            {
                case ConnectionState.Disconnected:
                    m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.disconnectColor, 
                        m_LocalisationService.Translate("DISCONNECTED_LABEL"));
                    break;
                case ConnectionState.Connecting:
                    m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.connectingColor,
                        m_LocalisationService.Translate("CONNECTING_LABEL"));
                    break;
                case ConnectionState.SessionInitialising:
                    m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.connectingColor,
                        m_LocalisationService.Translate("SESSION_INIT_LABEL"));
                    break;
                case ConnectionState.Connected:
                    m_HeaderUiService.UpdateConnectionIndicatorUI(
                        m_Settings.connectColor,
                        m_ConnectionService.connectedSessionId + (m_ConnectedToDomain ? " D" : string.Empty) );
                    break;

                    //case State.EnteringDomain:
                    //    m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.connectingColor,
                    //        m_LocalisationService.Translate("ENTERING_DOMAIN_LABEL"));
                    //    break;
            }
        }


        //private Coroutine m_ConnectingTimerCoroutine;
        //private float m_Timer;

        //private IEnumerator ConnectivityTimer()
        //{
        //    m_Timer = 0;

        //    Debug.Log("@@@@@@@@ Start Connectivity Timer");

        //    while (m_State == State.Connecting)
        //    {
        //        Debug.Log($"@@@@@@@@ Try to connect... {m_Timer}");

        //        m_Timer += Time.unscaledDeltaTime;

        //        if(m_Timer > 2)
        //        {
        //            m_State = State.Failed;
        //            m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.disconnectColor, m_State.ToString());
        //            Debug.LogWarning("@@@@@@@@ There is a connectivity error");
        //            yield break;
        //        }

        //        yield return null;
        //    }
        //}


        //private void OnAukiStateChanged(Auki.ConjureKit.State state)
        //{
        //    Debug.Log($"@@@@@@@@ {state}");

        //    switch (state)
        //    {
        //        case Auki.ConjureKit.State.Disconnected:
        //            m_State = State.Disconnected;
        //            m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.disconnectColor, $"{state.ToString()}");
        //            break;
        //        case Auki.ConjureKit.State.Connecting:
        //            Debug.Log("@@@@@@@@ Start Connectivity Timer");
        //            m_State = State.Connecting;
        //            m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.connectingColor, $"{state.ToString()}");


        //            if (m_ConnectingTimerCoroutine != null)
        //                m_CoroutineRunner.StopUnityCoroutine(m_ConnectingTimerCoroutine);

        //            Debug.Log("@@@@@@@@ Start Connectivity Timer");
        //            m_ConnectingTimerCoroutine = m_CoroutineRunner.StartUnityCoroutine(ConnectivityTimer());

        //            break;
        //        //case Auki.ConjureKit.State.JoinedSession:
        //        //    m_HeaderUiService.UpdateConnectionIndicatorUI(m_Settings.connectColor, $"{m_AukiWrapper.GetSession().Id}");
        //        //    break;
        //    }
        //}

        [System.Serializable]
        public class Settings
        {
            [SerializeField] private Color m_DisconnectColor;
            [SerializeField] private Color m_ConnectingColor;
            [SerializeField] private Color m_ConnectColor;

            public Color disconnectColor => m_DisconnectColor;
            public Color connectingColor => m_ConnectingColor;
            public Color connectColor => m_ConnectColor;
        }
    }
}
