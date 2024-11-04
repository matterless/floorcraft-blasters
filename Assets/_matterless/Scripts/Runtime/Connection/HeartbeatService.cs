using System;
using System.Collections.Generic;
using Matterless.Inject;
using Matterless.Localisation;
using Matterless.Module.UI;
using Matterless.UTools;
using Newtonsoft.Json;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class HeartbeatService : IHeartbeatService, ITickable
    {
        private const string POST_HEARTBEAT_SESSION_ENDPOINT = "v2/shared_sessions/{0}/heartbeat";
        private const string DOMAIN_ERROR_TITLE = "DOMAIN_ERROR_TITLE";
        private const string DOMAIN_SESSION_EXPIRED_LABEL = "DOMAIN_SESSION_EXPIRED_LABEL";
        private const string LEAVE_BUTTON_LABEL = "LEAVE_BUTTON_LABEL";
        
        [System.Serializable]
        private class SessionResponse
        {
            public string _id;
            public string app_id;
            public string domain_id;
            public string session_id;
            public string data;
            public long created_at;
            public long updated_at;
            public string created_by;
            public string updated_by;
            public string[] tags;
            public string threshold;
            public long last_activated_at;
            public string match_phrase;
        }
        
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IConnectionService m_ConnectionService;
        private readonly IInputDialogueService m_InputDialogueService;
        private readonly IDomainService m_DomainService;
        private readonly IRestService m_RestService;
        private readonly Settings m_Settings;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        private readonly ILocalisationService m_LocalisationService;

        private float m_HeartbeatTimer;
        private float m_HeartbeatFrequency;
        private bool m_IsConnectedToDomain;
        private string m_CurrentUniqueSessionId = string.Empty;
        private string m_CurrentHearbeatUrl;
        private DomainState m_DomainState = DomainState.None;

        private DateTime m_PauseTime;
        private DateTime m_LastHeartbeatTime;
        private float m_CurrentDomainThreshold;
        private bool m_CanRespondHeartbeat;
        
        // Texts
        private string m_domainErrorTitleLabel;
        private string m_domainSessionExpiredErrorDescriptionLabel;
        private string m_leaveButtonLabel;

        public HeartbeatService(
            IAukiWrapper aukiWrapper,
            IConnectionService connectionService,
            IInputDialogueService inputDialogueService,
            IDomainService domainService,
            IRestService restService,
            Settings settings,
            IUnityEventDispatcher unityEventDispatcher,
            ILocalisationService localisationService)
        {
            m_AukiWrapper = aukiWrapper;
            m_ConnectionService = connectionService;
            m_InputDialogueService = inputDialogueService;
            m_DomainService = domainService;
            m_RestService = restService;
            m_Settings = settings;
            m_UnityEventDispatcher = unityEventDispatcher;
            m_LocalisationService = localisationService;
            m_DomainService.onDomainStateChanged += OnDomainStateChanged;
            m_UnityEventDispatcher.unityOnApplicationPause += OnPauseEvent;

            if (m_Settings.frequencyPercentage is < 0 or > 1)
                throw new Exception("Heartbeat frequency percentage cannot be out of (0..1");

            m_domainErrorTitleLabel = m_LocalisationService.Translate(DOMAIN_ERROR_TITLE);
            m_domainSessionExpiredErrorDescriptionLabel = m_LocalisationService.Translate(DOMAIN_SESSION_EXPIRED_LABEL);
            m_leaveButtonLabel = m_LocalisationService.Translate(LEAVE_BUTTON_LABEL);
        }

        private void OnPauseEvent(bool pauseStatus)
        {
            if (!m_IsConnectedToDomain)
                return;

            if (!pauseStatus)
            {
                TimeSpan ts = DateTime.Now - m_LastHeartbeatTime;
                if (ts.TotalMilliseconds < m_CurrentDomainThreshold)
                {
                    Debug.Log("Threshold valid, sending immediate heartbeat");
                    m_HeartbeatTimer = m_HeartbeatFrequency;
                }
                else
                {
                    // TODO: Remove this else check when the feature is completed
                    Debug.Log("Threshold passed");
                }

            }
        }

        private void OnDomainStateChanged(DomainStatusEvent evt)
        {
            m_DomainState = evt.state;
            m_IsConnectedToDomain = evt.state == DomainState.Connected;

            if (evt.state != DomainState.Connected)
            {
                m_CanRespondHeartbeat = false;
                m_HeartbeatFrequency = -1;
                return;
            }

            // cache domain session unique id
            m_CurrentUniqueSessionId = evt.uniqueSessionId;
            // cache end point url
            m_CurrentHearbeatUrl = m_RestService.GetLookingGlassProtocolFullUrl(string.Format(POST_HEARTBEAT_SESSION_ENDPOINT, m_CurrentUniqueSessionId));

            if (!string.IsNullOrEmpty(evt.threshold) 
                && TryGetMiliseconds(evt.threshold, out var domainThreshold))
            {
                m_HeartbeatFrequency = domainThreshold * 0.001f * m_Settings.frequencyPercentage;
                m_CurrentDomainThreshold = domainThreshold;
                Debug.Log($"Heartbeat frequency set to {m_HeartbeatFrequency}sec for threshold {domainThreshold}ms");
            }
            else
            {
                // if threshold is not set, we set it to -1 to disable heartbeat
                m_HeartbeatFrequency = -1;
                Debug.Log("Heartbeat has been disabled");
            }
        }

        private void PostHeartbeat(string sessionId)
        {
            Debug.Log($"HeartbeatService.Beat {sessionId}");

            m_CanRespondHeartbeat = true;
            // post session heartbeat
            m_RestService.UnsecurePostJson(
                // url
                m_CurrentHearbeatUrl,
                // payload
                String.Empty,
                // response
                OnBeatResponse,
                // error
                OnBeatFail);
        }

        private void OnBeatResponse(string response)
        {
            Debug.Log($"Beat success for {response}");
            
            SessionResponse sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(response);

            if (!m_AukiWrapper.isConnected || !m_CanRespondHeartbeat)
                return;

            // We check on beat success that if we are still connected to a session and if that session is the same with the domain session
            // It will *nearly* always be same while playing the game normally but it may change if we take the app to the background for a while
            if (sessionResponse.session_id != m_AukiWrapper.GetSession().Id)
            {
                Debug.Log("Domain session is the different with our session!");
                // Should we show to user the error and force them to connect new session?
                ShowDomainError(m_domainSessionExpiredErrorDescriptionLabel);

                // Or should we automatically join to domain's new session to keep the user in the domain
                // Is it the same that connecting to a domain with a session and connecting to session that is belongs to a domain in that moment?
                // m_AukiWrapper.Join(sessionResponse.session_id);
                
                // Stop heartbeat since we acknowledged we are not in this domain anymore
                m_HeartbeatFrequency = -1;
            }
            else
            {
                Debug.Log("Beat success, the game should work normally");
            }
        }
        
        private void OnBeatFail(RestService.ErrorResponse beatError)
        {
            Debug.LogWarning($"Beat fail, error {beatError.rawCode} / {beatError.code}, message: {beatError.message}");
            
            if (!m_AukiWrapper.isConnected || !m_CanRespondHeartbeat)
                return;

            // disable heartbeat
            m_HeartbeatFrequency = -1;

            // According to documentation: https://dsm.stg.lookingglassprotocol.com/swagger#/SharedSessions/heartbeatSharedSession
            if (beatError.rawCode == 404)
            {
                Debug.Log("Sessions does not exist or expired");
                ShowDomainError(m_domainSessionExpiredErrorDescriptionLabel);
            }
            else
            {
                ShowDomainError(beatError.message);
            }
        }

        private void ShowDomainError(string descriptionMessage)
        {
            m_InputDialogueService.Show(new DialogueModel(
                m_domainErrorTitleLabel,
                descriptionMessage,
                m_leaveButtonLabel, false, 
                () => m_ConnectionService.LeaveSessionAndReconnect(0.1f)));
        }

        private bool CanBeat()
        {
            if (m_DomainState == DomainState.Connected && 
                !string.IsNullOrEmpty(m_CurrentUniqueSessionId) && 
                !string.IsNullOrWhiteSpace(m_CurrentUniqueSessionId))
            {
                return !m_AukiWrapper.isHost || !m_Settings.onlyMasterClient;
            }

            return false;
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if(!m_IsConnectedToDomain || m_HeartbeatFrequency < 0)
                return;
            
            m_HeartbeatTimer += unscaledDeltaTime;

            if (m_HeartbeatTimer >= m_HeartbeatFrequency)
            {
                if (CanBeat())
                {
                    PostHeartbeat(m_CurrentUniqueSessionId);
                    m_LastHeartbeatTime = DateTime.Now;
                }

                m_HeartbeatTimer = 0;
            }
        }

        #region Threshold Parser
        // Valid time units are "ns", "us", "ms", "s", "m", "h".
        // Doc: https://dsm.stg.lookingglassprotocol.com/swagger#/SharedSessions/addSharedSession
        private static Dictionary<string, float> s_TimeUnitsToMiliseconds = new Dictionary<string, float>()
        {
            {"ns", 1000000.0f},
            {"us", 1000.0f},
            {"ms", 1.0f},
            {"s", 0.001f},
            {"m", 1.0f/60.0f},
            {"h", 1.0f/3600.0f}
        };

        private static bool TryGetMiliseconds(string threshold, out int result)
        {
            foreach (var unit in s_TimeUnitsToMiliseconds)
            {
                if (threshold != null && threshold.Contains(unit.Key))
                {
                    string[] splitted = threshold.Split(unit.Key);
                    int time = Convert.ToInt32(splitted[0]);
                    int miliseconds = Mathf.RoundToInt((float)time / unit.Value);

                    result = miliseconds;
                    return true;
                }
            }

            result = -1;
            return false;
        }
        #endregion

        #region Settings
        [System.Serializable]
        public class Settings
        {
            [Header("Domain lifetime in milliseconds (ms)")]
            [SerializeField] private int m_DomainThreshold = 5000;
            [Header("Heartbeat period in threshold percentage. (0..1).")]
            [SerializeField, Range(0f,1f)] private float m_HeartbeatFrequencyThresholdPercentage = 0.2f;
            [Header("Sets if the heartbeat will be sent by only the master client, or everyone.")]
            [SerializeField] private bool m_OnlyMasterClient = false;

            public int threshold => m_DomainThreshold;
            public float frequencyPercentage => m_HeartbeatFrequencyThresholdPercentage;
            public bool onlyMasterClient => m_OnlyMasterClient;
        }
        #endregion
    }
}