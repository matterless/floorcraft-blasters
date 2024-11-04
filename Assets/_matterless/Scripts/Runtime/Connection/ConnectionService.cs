using System;
using System.Collections;
using Auki.ConjureKit;
using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public enum ConnectionState
    {
        Disconnected, Connecting, SessionInitialising, Connected
    }

    public class ConnectionService : IConnectionService, ITickable
    {
        public event Action<ConnectionState> onConnectionStateChanged;
        public bool isReady { get; private set; } = false;
        public string connectedSessionId { get; private set; }

        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IECSController m_ECSController;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly INetworkService m_NetworkService;
        private AutoConnectionConfigs m_MyAutoConfig;
        private Coroutine m_WaitCoroutine;

        private State m_CurrentState;
        private bool m_IsConnecting;
        private float m_ConnectingTimer;

        public ConnectionService(
            IAukiWrapper aukiWrapper,
            IECSController ecsController,
            ICoroutineRunner coroutineRunner,
            IUnityEventDispatcher unityEventDispatcher,
            INetworkService networkService)
        {
            m_AukiWrapper = aukiWrapper;
            m_ECSController = ecsController;
            m_NetworkService = networkService;
            m_AukiWrapper.onJoined += OnSessionJoined;
            m_AukiWrapper.onStateChanged += OnStateChanged;
            m_CoroutineRunner = coroutineRunner;
            
            unityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
            m_NetworkService.onNetworkConnectionChanged += OnConnectionStatusChanged;
            m_NetworkService.onForceRefresh += OnForceRefresh;

            Connect();
        }

        private void OnStateChanged(State state)
        {
            Debug.Log($"Connection state changed {state.ToString()}");
            m_CurrentState = state;
            m_IsConnecting = false;
            m_ConnectingTimer = 0;
            switch(state)
            {
                case State.Disconnected:
                    isReady = false;
                    m_ECSController.Clear();
                    onConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
                    Reconnect(2);
                    break;
                case State.Connecting:
                    m_IsConnecting = true;
                    onConnectionStateChanged?.Invoke(ConnectionState.Connecting);
                    break;
                case State.JoinedSession:
                    // see OnSessionJoined method
                    break;
                case State.Calibrated:
                    break;
                case State.Initializing:
                    break;
            }
        }
        
        private void OnConnectionStatusChanged(ConnectionStatus connectionStatus)
        {
            if (connectionStatus == ConnectionStatus.Connected &&
                (m_CurrentState == State.Disconnected || m_CurrentState == State.Connecting))
            {
                Connect();
            }

            if (connectionStatus == ConnectionStatus.Disconnected)
            {
                m_CurrentState = State.Disconnected;
                m_AukiWrapper.Leave();
            }
        }

        private void OnForceRefresh()
        {
            LeaveSessionAndReconnect(2);
        }

        private void OnSessionJoined(Session session)
        {
            // cache session id
            connectedSessionId = session.Id;
            // invoke event
            onConnectionStateChanged?.Invoke(ConnectionState.SessionInitialising);
            // start ECS initialisation
            m_ECSController.Initialise(session, OnECSInitalisedSuccessfully, OnECSErrorInitialisation);
        }

        private void OnECSInitalisedSuccessfully()
        {
            isReady = true;
            // invoke event
            onConnectionStateChanged?.Invoke(ConnectionState.Connected);
        }

        private void OnECSErrorInitialisation()
        {

        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if(pauseStatus)
                return;

            Debug.Log("Trying to reconnect");
            // we need to wait 2 seconds in order to Auki has correct isConnected value
            // we tried with 1 sec and it didn't work
            Reconnect(2);
        }

        private void Connect()
        {
            // if auto connect file does not exist, just connect
            if(!AutoConnectionConfigs.hasConfig)
            {
                m_AukiWrapper.Join();
                return;
            }

            // if we have an auto connect file
            m_AukiWrapper.onJoined -= OnAukiJoined;
            m_AukiWrapper.onJoined += OnAukiJoined;

            m_MyAutoConfig = AutoConnectionConfigs.GetMyConfig();

            if (m_MyAutoConfig.isHost)
            {
                m_AukiWrapper.Join();
            }
            else
            {
                var hostConfig = AutoConnectionConfigs.GetConfig(m_MyAutoConfig.hostPathConfig);
                m_AukiWrapper.Join(hostConfig.currentSessionId  ,null);
            }
        }

        public void NewSession(Action onComplete = null, Action<string> onFail = null)
        { 
            m_AukiWrapper.Join(onComplete, onFail);
        }

        public void JoinSession(string sessionId)
        {
            m_AukiWrapper.Join(sessionId);
        }

        public void Reconnect(float delay)
        {
            // stop coroutine if exist
            if(m_WaitCoroutine != null)
                m_CoroutineRunner.StopUnityCoroutine(m_WaitCoroutine);
            // start coroutine
            Debug.Log("start reconnect coroutine");
            m_WaitCoroutine = m_CoroutineRunner.StartUnityCoroutine(ReconnectRoutine(delay));
        }

        public void LeaveSession()
        {
            m_AukiWrapper.Leave();
        }

        public void LeaveSessionAndReconnect(float delay)
        {
            m_AukiWrapper.Leave();
            Reconnect(delay);
        }

        private IEnumerator ReconnectRoutine(float delay)
        {
            Debug.Log("Checking connection...");
            Debug.Log($"Waiting for {delay} seconds...");
            yield return new WaitForSeconds(delay);
            
            Debug.Log($"Is connected: {m_AukiWrapper.isConnected}");

            if (m_AukiWrapper.isConnected)
            {
                Debug.Log("Already connected, breaking flow");
                yield break;
            }

            Debug.Log("Connecting...");
            Connect();
        }

        private void OnAukiJoined(Session session)
        {
            m_AukiWrapper.onJoined -= OnAukiJoined;
            
            if (m_MyAutoConfig.isHost)
            {
                m_MyAutoConfig.Save(session.Id);
            }
        }

        public bool GetConnectionStatus()
        {
            return m_AukiWrapper.isConnected;
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            /*if (m_IsConnecting)
            {
                m_ConnectingTimer += deltaTime;
                if (m_ConnectingTimer >= 8)
                {
                    LeaveSessionAndReconnect(2);
                    m_ConnectingTimer = 0;
                    m_IsConnecting = false;
                }
            }
            else if (m_ConnectingTimer > 0)
            {
                m_ConnectingTimer = 0;
            }*/
        }
    }
}