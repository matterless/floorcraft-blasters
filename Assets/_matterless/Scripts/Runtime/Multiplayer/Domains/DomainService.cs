using Auki.ConjureKit.Manna;
using Matterless.Inject;
using Matterless.Localisation;
using Matterless.Module.UI;
using System;
using Newtonsoft.Json;
using System.Linq;
using Auki.ConjureKit;
using UnityEngine;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{
    public enum DomainState
    {
        None, Entering, Connected
    }

    public class DomainService : IDomainService
#if UNITY_EDITOR
        ,ITickable
#endif
    {
        private const string POST_DOMAIN_SESSION_ENDPOINT = "api/v1/shared-sessions";
        
        public const string APP_ID = "floorcraft";

        [System.Serializable]
        private class SessionPostPayload
        {
            public string app_id;
            public string domain_id;
            public bool join_session;
            public bool only_same_app;
            public string session_guid;
            public int threshold;
            public bool with_domain;

            public string CreatePayload(string sessionId, string domainId, int thresholdMs, string appKey)
            {
                app_id = appKey;
                domain_id = domainId;
                join_session = true;
                only_same_app = true;
                session_guid = $"{sessionId}:{domainId}";
                threshold = thresholdMs / 1000; // Convert milliseconds to seconds
                with_domain = false;
                return JsonUtility.ToJson(this);
            }
        }
        
        [System.Serializable]
        private class SessionResponse
        {
            public string id; // DDS uses this
            public string _id; // Keep for backward compatibility
            public string app_id;
            public string domain_id;
            public string session_id; // Still keep for backward compatibility
            public string session_guid; // DDS uses this instead
            public string data;
            public string created_at; // Changed to string (DDS returns ISO 8601)
            public string updated_at; // Changed to string (DDS returns ISO 8601)
            public string created_by;
            public string updated_by;
            public string[] tags;
            public int threshold; // Changed from string to int
            public string last_activated_at; // Keep for backward compatibility
            public string last_active_at; // DDS uses this
            public string match_phrase;
            
            // Helper method to extract session ID from session_guid
            public string GetSessionId()
            {
                if (string.IsNullOrEmpty(session_guid))
                    return session_id; // Fallback to old field if session_guid not present
                    
                var colonIndex = session_guid.IndexOf(':');
                if (colonIndex > 0)
                    return session_guid.Substring(0, colonIndex);
                    
                return session_id; // Fallback
            }
        }

        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IMannaService m_MannaService;
        private readonly MannaService.Settings m_MannaSettings;
        private readonly AukiSettings m_AukiSettings;

        //private readonly IConnectionService m_ConnectionService;
        private readonly IRestService m_RestService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly IInputDialogueService m_InputDialogueService;
        private readonly HeartbeatService.Settings m_HeartbeatSettings;
        private readonly DomainAssetService m_DomainAssetService;
        private readonly SessionPostPayload m_SessionPostPayload = new SessionPostPayload();

        private string m_DomainId;
        private SessionResponse m_CurrentSessionData;
        //this is a workaround be cause we need to cancel the poseselector to restart scanning
        private readonly LighthousePose m_NullLighthouse = new();
        private bool m_ExpectingBadLighthouse;
        private LighthousePose m_LatestLighthousePose;

        public event Action onLightHouseScanFail;
        
        /// <summary>
        /// Domain state updates with couple of optional data. 
        /// The session id (eg. 19b2x1)
        /// The unique session id (eg. 64a53f63975a499050dec200)
        /// The session threshold (eg. 3000ms)
        /// The domain state (eg. connected, entering)
        /// </summary>
        public event Action<DomainStatusEvent> onDomainStateChanged;
        public event Action onLightHouseAssign;

        public bool sessionIdDomain { get; private set; } = false;
        public string currentDomainId => m_DomainId;

        public DomainService(
            IAukiWrapper aukiWrapper,
            IMannaService mannaService,
            MannaService.Settings mannaSettings,
            AukiSettings aukiSettings,
            //IConnectionService connectionService,
            IRestService restService,
            IAnalyticsService analyticsService,
            ILocalisationService localisationService,
            IInputDialogueService inputDialogueService,
            PropertiesComponentService propertiesComponentService,
            TransformComponentService transformComponentService,
            PropertiesECSService.Settings propECSSettings,
            DomainSettings domainSettings,
            HeartbeatService.Settings heartbeatSettings)
        {
            m_AukiWrapper = aukiWrapper;
            m_MannaService = mannaService;
            m_MannaSettings = mannaSettings;
            m_AukiSettings = aukiSettings;
            //m_ConnectionService = connectionService;
            m_RestService = restService;
            m_AnalyticsService = analyticsService;
            m_LocalisationService = localisationService;
            m_InputDialogueService = inputDialogueService;
            m_HeartbeatSettings = heartbeatSettings;
            m_DomainAssetService = new DomainAssetService(
                aukiWrapper, restService,
                propertiesComponentService, 
                transformComponentService,
                propECSSettings);

            // reset session in domain flag on session left
            m_AukiWrapper.onLeft += ResetValuesOnSessionLeft;
            mannaService.onCalibrationFail += OnCalibrationFail;
            mannaService.onPoseSelect += PoseSelector;
        }

        private void ResetValuesOnSessionLeft()
        {
            sessionIdDomain = false;
            m_DomainAssetService.ResetValues();

            OnDomainStateChanged(new DomainStatusEvent()
            {
                state = DomainState.None
            });
        }

        private void OnDomainStateChanged(DomainStatusEvent evt)
        {
            onDomainStateChanged?.Invoke(evt);

            if (m_MannaSettings.scanningType == MannaService.ScanningType.InGame)
            {
                switch (evt.state)
                {
                    case DomainState.Connected:
                        m_MannaService.SetScanningFrequency(MannaService.FrequencyType.Mid);
                        m_MannaService.StartScanning();
                        break;
                    default:
                        m_MannaService.StopScanning();
                        break;
                }
            }
        }

        private void PoseSelector(LighthousePose[] poses, Action<LighthousePose> action)
        {
            Debug.Log($"{poses.Length} domains found.");

            m_LatestLighthousePose = null; // Reset first, then set again below with valid pose (if it has any)
            LighthousePose selectedPose = null;

            //no lighthouses with calibration
            var poseList = poses.Where(p => !p.IsEqualToIdentityPose()).ToList();
            if (poseList.Count == 0)
            {
                action.Invoke(m_NullLighthouse);
                m_ExpectingBadLighthouse = false;
                return;
            }

            Debug.Log($"{poseList.Count} valid domains found.");

            //there is only one domain, use it
            if (poseList.Count == 1)
            {
                Debug.Log($"Only 1 pose found, setting selected pose to {poseList[0].domainId}");
                LighthousePose pose = poseList[0];
                selectedPose = pose;

                //ignore it if we are in a domain and its not this one
                /*if (sessionIdDomain && pose.domainId != m_DomainId)
                {
                    m_ExpectingBadLighthouse = true;
                }
                else
                {
                    //go (happy path)
                    selectedPose = pose;
                }*/
            }
            else if (sessionIdDomain)
            {
                //we are in a domain and there are multiple options.
                LighthousePose sameDomain = null;
                foreach (LighthousePose lighthousePose in poseList)
                {
                    // we are already in one of the domain options.
                    if (lighthousePose.domainId == m_DomainId)
                    {
                        Debug.Log($"Lighthouse has the same domain {lighthousePose.domainId}");
                        sameDomain = lighthousePose;
                        selectedPose = lighthousePose;
                    }
                }

                // we are in a domain which is not belong to newly scanned lighthouse, join to the new domain
                if (sameDomain == null)
                {
                    Debug.Log($"We are in a domain and many poses found, setting selected pose to {poseList[0].domainId}");
                    selectedPose = poseList[0];
                }

                if (selectedPose == null)
                {
                    m_ExpectingBadLighthouse = true;
                }
            }
            else
            {
                // Multiple options and we're not in a domain. Pick first. (ideally we'd show a popup here but let's keep it simple for now)
                Debug.Log($"Multiple options and we're not in a domain. Pick first pose to {poseList[0].domainId}");

                poseList = poses.OrderBy(pose => pose.addedToDomainAt).ToList();
                
                selectedPose = poseList[0];
            }

            if (selectedPose == null)
            {
                action.Invoke(m_NullLighthouse);
                return;
            }
            
            m_LatestLighthousePose = selectedPose;
            Debug.Log("[domain] scanning into domain with ID " + selectedPose.domainId);
            OnDomainQrCodeScanned(selectedPose.domainId);
            action?.Invoke(selectedPose);
        }

        private void OnCalibrationFail(CalibrationFailureData failureData)
        {
            if (failureData.Reason == CalibrationFailureData.CalibrationFailureReason.LighthouseNotPlaced)
                onLightHouseScanFail?.Invoke();
        }

        private void OnDomainQrCodeScanned(string domainId)
        {
            
            // if I'm already in this domain -> do nothing
            if (domainId == m_DomainId && sessionIdDomain)
            {
                return;
            }

            // cache domain
            m_DomainId = domainId;
            m_AnalyticsService.SeenDomain(domainId);
            
            // Disconnect from current session first (if any)
            m_AukiWrapper.Leave();
            
            // join to new session, to prevent same sessions on multiple domains
            m_AukiWrapper.Join(
                onComplete: () => {
                    try
                    {
                        string newSessionId = m_AukiWrapper.GetSession().Id;
                        PostSessionIdToDomain(newSessionId);
                    }
                    catch (Exception ex)
                    {
                    }
                },
                onFail: (error) => {
                });
        }

        /// <summary>
        /// Send session id to backend to bound it with the domain
        /// </summary>
        /// <param name="sessionId"></param>
        private void PostSessionIdToDomain(string sessionId)
        {

            OnDomainStateChanged(new DomainStatusEvent()
            {
                state = DomainState.Entering,
                sessionId = sessionId
            });

            string payload = m_SessionPostPayload.CreatePayload(
                sessionId, 
                m_DomainId, 
                (int)m_HeartbeatSettings.threshold,
                m_AukiSettings.appKey
            );
            
            // post my session id to DDS (with authentication)
            m_RestService.SecurePostJson(
                // url
                m_RestService.GetDdsUrl(POST_DOMAIN_SESSION_ENDPOINT),
                // payload - DDS format
                payload,
                // response
                (response) => OnSetSessionIdResponse(sessionId, response),
                // error
                (x) => {
                    Debug.LogError(x.message);
                });
        }
        

        private void OnSetSessionIdResponse(string currentSessionId, string response)
        {

            try
            {
                SessionResponse sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(response);
                
                m_CurrentSessionData = sessionResponse;

                // Extract the actual session ID from DDS session_guid (format: "sessionId:domainId")
                string returnedSessionId = sessionResponse.GetSessionId();

                // This means our new session has been put to the domain successfully (we're hosting)
                if (currentSessionId == returnedSessionId)
                {
                    OnDomainSessionJoinedCompleted(currentSessionId, isMasterClient: true);
                    return;
                }
                
                // If we come to this point this means that domain already has an existing session, we need to switch to it
                m_AukiWrapper.Join(
                    returnedSessionId,
                    // if ok
                    () => {
                        OnDomainSessionJoinedCompleted(m_AukiWrapper.GetSession().Id, isMasterClient: false);
                    },
                    // The session of the domain is most probably expired
                    OnDomainSessionJoinFailed);
            }
            catch (Exception ex)
            {
            }
        }
        
        private void OnQueryAllSessionsResponse(string response)
        {
            SessionResponse[] sessionResponse = JsonConvert.DeserializeObject<SessionResponse[]>(response);

            foreach (var sr in sessionResponse)
            {
                
            }
        }

        /// <summary>
        /// Called when joining an existing domain session fails (session expired).
        /// With DDS, we just POST again and it will automatically replace the expired session.
        /// </summary>
        /// <param name="error">Error message from server</param>
        private void OnDomainSessionJoinFailed(string error)
        {
            
            // DDS POST will automatically replace expired session
            m_AukiWrapper.Join(() => {
                PostSessionIdToDomain(m_AukiWrapper.GetSession().Id);
            });
        }

        private void OnDomainSessionJoinedCompleted(string sessionId, bool isMasterClient)
        {
            Debug.Log($"DomainService.OnDomainSessionJoinedCompleted {sessionId}, isMaster:{isMasterClient}");

            m_AnalyticsService.EnterDomain(m_DomainId, isMasterClient ? DomainEnterType.Host : DomainEnterType.Join);


            OnDomainStateChanged(new DomainStatusEvent()
            {
                state = DomainState.Connected,
                uniqueSessionId = !string.IsNullOrEmpty(m_CurrentSessionData.id) ? m_CurrentSessionData.id : m_CurrentSessionData._id, // DDS uses 'id', fallback to '_id'
                threshold = m_CurrentSessionData.threshold.ToString() + "000ms", // Convert seconds back to ms format
                sessionId = sessionId
            });

            onLightHouseAssign?.Invoke();
            sessionIdDomain = true;

            GetAndCreateDomainAssets();
        }

        private void GetAndCreateDomainAssets()
        {
            Debug.Log($"DomainService.GetAndCreateDomainAssets");
            // TODO: DDS does not support arbitrary data storage like Looking Glass Protocol did.
            // Domain assets feature is disabled. Consider using ConjureKit state sync or custom messages instead.
            // m_DomainAssetService.GetAndCreateDomainAssets(APP_ID, m_DomainId);
        }

        private string GetThresholdAsString(float threshold)
        {
            return $"{threshold}{"ms"}";
        }

        #region Domain Assets
        public void CreateAsset(AssetId assetId, Pose pose)
            => m_DomainAssetService.CreateAsset(m_DomainId, assetId, pose);
        

        public void DeleteDomainAssets() => m_DomainAssetService.DeleteDomainAssets();
        #endregion

#if UNITY_EDITOR
        int _keyRecognition = 0;
        const string _testDomainId = "2c7383f1-71fd-4042-a5fd-3da26beba60f"; // Fixed: was 60g (invalid), now 60f (valid UUID)

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (Input.GetKeyDown(KeyCode.D) && _keyRecognition == 0)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.O) && _keyRecognition == 1)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.M) && _keyRecognition == 2)
            {
                // Use the configured domain ID from AukiSettings if available, otherwise use test domain ID
                string domainId = !string.IsNullOrEmpty(m_AukiSettings.appDomainId) 
                    ? m_AukiSettings.appDomainId 
                    : _testDomainId;
                
                OnDomainQrCodeScanned(domainId);
                _keyRecognition = 0;
            }
        }
#endif

        /*
         *     UNUSED CODE

        /        //private void PostQuerySessions(string uniqueSessionId, bool excludeExpiredSessions)
        {
            Debug.Log($"DomainService.PostQuerySessions {uniqueSessionId}");

            m_RestService.UnsecurePostJson(
                // url
                m_RestService.GetDdsUrl(POST_QUERY_SESSIONS_ENDPOINT),
                // payload
                m_QuerySessionPostPayload.CreatePayload(uniqueSessionId, excludeExpiredSessions),
                // response
                excludeExpiredSessions ? OnQueryAliveSessionsResponse : OnQueryAllSessionsResponse,
                // error
                (x) => Debug.LogError(x.message));
        }

        / <summary>
        / Update existing and alive session of the domain with new properties by overriding them.
        / </summary>
        / <param name="sessionId">Session id of the session going to be updated</param>
        / <param name="updated_at">Unix timestamp (in ms) of the current time</param>
        / <param name="threshold">New expiration threshold for the session</param>
        / <param name="data"></param>
        / <param name="tags"></param>
        private void UpdateExistingSessionInDomain(string sessionId, long updated_at, string threshold, string[] data = null, string[] tags = null)
        {
            // Update existing session in domain
            m_RestService.UnsecurePutJson(
                // url
                m_RestService.GetDdsUrl(string.Format(PUT_DOMAIN_SESSION_ENDPOINT, sessionId)),
                // payload
                m_SessionPutPayload.CreatePayload(updated_at, threshold, data, tags),
                // response
                (responseSessionId) => OnSetSessionIdResponse(sessionId, responseSessionId.Trim()),
                // error
                (x) => Debug.LogError(x.message));
        }
        private void OnQueryAliveSessionsResponse(string response)
        {
            SessionResponse[] sessionResponse = JsonConvert.DeserializeObject<SessionResponse[]>(response);

            foreach (var sr in sessionResponse)
            {
                if (sr._id == m_CurrentSessionData._id)
                {
                    // Session is alive, try to rejoin it
                    Debug.Log("Session is alive, try to rejoin it");
                    m_AukiWrapper.Join(m_CurrentSessionData.session_id,() => PostSessionIdToDomain(m_AukiWrapper.GetSession().Id));
                    return;
                }
            }

            // Domain session is expired, get a new session from Auki and replace the domain session with it
            m_AukiWrapper.Join(() => ReplaceSessionIdOfDomain(m_AukiWrapper.GetSession().Id));
        }
        */
    }
}