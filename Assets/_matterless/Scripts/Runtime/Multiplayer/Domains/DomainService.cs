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
        private const string POST_DOMAIN_SESSION_ENDPOINT = "v2/shared_sessions";
        private const string PUT_DOMAIN_SESSION_ENDPOINT = "v2/shared_sessions/{0}";
        private const string PUT_REPLACE_DOMAIN_SESSION_ENDPOINT = "v2/shared_sessions/{0}/replace";
        private const string POST_QUERY_SESSIONS_ENDPOINT = "v2/shared_sessions/query";
        
        public const string APP_ID = "floorcraft";

        [System.Serializable]
        private class SessionPostPayload
        {
            [FormerlySerializedAs("session")] public string session_id;
            public string domain_id;
            public string app_id;
            public string threshold;

            public string CreatePayload(string sessionId, string domainId, string threshold = null)
            {
                app_id = APP_ID;
                session_id = sessionId;
                domain_id = domainId;
                this.threshold = threshold;
                return JsonUtility.ToJson(this);
            }
        }
        
        [System.Serializable]
        private class SessionPutPayload
        {
            public string[] data;
            public string[] tags;
            public string threshold;
            public long updated_at;

            /// <summary>
            /// Updated at is required, others are optional to update.
            /// </summary>
            /// <param name="updated_at">Unix timestamp in ms</param>
            /// <param name="threshold">To update the existing session threshold</param>
            /// <param name="data">Additional data to update</param>
            /// <param name="tags">Additional tags to update</param>
            /// <returns></returns>
            public string CreatePayload(long updatedAt, string threshold = null, string[] data = null, string[] tags = null)
            {
                this.data = data;
                this.tags = tags;
                this.threshold = threshold;
                this.updated_at = updatedAt;
                return JsonUtility.ToJson(this);
            }
        }
        
        [System.Serializable]
        private class ReplaceSessionPutPayload
        {
            public string session_id;
            public long updated_at;

            public string CreatePayload(string sessionId, long updatedAt)
            {
                session_id = sessionId;
                updated_at = updatedAt;
                return JsonUtility.ToJson(this);
            }
        }
        
        [System.Serializable]
        private class QuerySessionsPostPayload
        {
            public string _id;
            public bool exclude_expired;

            /// <summary>
            /// To query desired session data
            /// </summary>
            /// <param name="sessionId">Unique session id</param>
            /// <param name="excludeExpired">Excludes expired domains from the result array</param>
            /// <returns></returns>
            public string CreatePayload(string sessionId, bool excludeExpired)
            {
                _id = sessionId;
                exclude_expired = excludeExpired;
                return JsonUtility.ToJson(this);
            }
        }
        
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
        private readonly IMannaService m_MannaService;
        private readonly MannaService.Settings m_MannaSettings;

        //private readonly IConnectionService m_ConnectionService;
        private readonly IRestService m_RestService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly IInputDialogueService m_InputDialogueService;
        private readonly HeartbeatService.Settings m_HeartbeatSettings;
        private readonly DomainAssetService m_DomainAssetService;
        private readonly SessionPostPayload m_SessionPostPayload = new SessionPostPayload();
        private readonly SessionPutPayload m_SessionPutPayload = new SessionPutPayload();
        private readonly ReplaceSessionPutPayload m_ReplaceSessionPutPayload = new ReplaceSessionPutPayload();
        private readonly QuerySessionsPostPayload m_QuerySessionPostPayload = new QuerySessionsPostPayload();

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
                return;

            // cache domain
            m_DomainId = domainId;
            m_AnalyticsService.SeenDomain(domainId);
            // join to new session, to prevent same sessions on multiple domains
            m_AukiWrapper.Join(() => PostSessionIdToDomain(m_AukiWrapper.GetSession().Id));
        }

        /// <summary>
        /// Send session id to backend to bound it with the domain
        /// </summary>
        /// <param name="sessionId"></param>
        private void PostSessionIdToDomain(string sessionId)
        {
            Debug.Log($"DomainService.PostSessionIdToDomain {sessionId}");

            OnDomainStateChanged(new DomainStatusEvent()
            {
                state = DomainState.Entering,
                sessionId = sessionId
            });

            // post my session id
            m_RestService.UnsecurePostJson(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(POST_DOMAIN_SESSION_ENDPOINT),
                // payload
                m_SessionPostPayload.CreatePayload(sessionId, m_DomainId, GetThresholdAsString(m_HeartbeatSettings.threshold)),
                // response
                (response) => OnSetSessionIdResponse(sessionId, response),
                // error
                (x) => Debug.LogError(x.message));
        }
        
        /// <summary>
        /// Replace session id of the domain. This will be called if the domain session is expired and we are going to replace it.
        /// </summary>
        /// <param name="sessionId"></param>
        private void ReplaceSessionIdOfDomain(string sessionId)
        {
            Debug.Log($"DomainService.ReplaceSessionIdOfDomain {sessionId}");
            
            m_RestService.UnsecurePutJson(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(string.Format(PUT_REPLACE_DOMAIN_SESSION_ENDPOINT, m_CurrentSessionData._id)),
                // payload
                m_ReplaceSessionPutPayload.CreatePayload(sessionId, m_CurrentSessionData.updated_at),
                // response
                (response) => OnSetSessionIdResponse(sessionId, response),
                // error
                (x) => Debug.LogError(x.message));
        }

        private void OnSetSessionIdResponse(string currentSessionId, string response)
        {
            Debug.Log($"DomainService.OnSetSessionIdResponse cur:{currentSessionId}, res:{response}");

            SessionResponse sessionResponse = JsonConvert.DeserializeObject<SessionResponse>(response);
            m_CurrentSessionData = sessionResponse;

            // This means our new session has been put to the domain successfully
            if (currentSessionId == sessionResponse.session_id)
            {
                OnDomainSessionJoinedCompleted(currentSessionId, isMasterClient: true);
                return;
            }
            
            Debug.Log($"Domain has an existing session {sessionResponse.session_id}, joining to it.");
            // If we come to this point this means that domain already has an existing session, we need to switch to it
            m_AukiWrapper.Join(
                sessionResponse.session_id,
                // if ok
                () => OnDomainSessionJoinedCompleted(m_AukiWrapper.GetSession().Id, isMasterClient: false),
                // The session of the domain is most probably expired
                OnDomainSessionJoinFailed);
        }
        
        private void OnQueryAllSessionsResponse(string response)
        {
            SessionResponse[] sessionResponse = JsonConvert.DeserializeObject<SessionResponse[]>(response);

            foreach (var sr in sessionResponse)
            {
                
            }
        }

        /// <summary>
        /// There is no way to know if a session is alive or not on Hagall via Matterless API.
        /// This fail assumes the session is expired and we act regarding to that.
        /// </summary>
        /// <param name="error">Error message from server</param>
        private void OnDomainSessionJoinFailed(string error)
        {
            Debug.Log($"DomainService.OnDomainSpaceJoinFailed {error}");
            
            // We will override the expired session that was registered to domain with our new session
            m_AukiWrapper.Join(()=> ReplaceSessionIdOfDomain(m_AukiWrapper.GetSession().Id));
        }

        private void OnDomainSessionJoinedCompleted(string sessionId, bool isMasterClient)
        {
            Debug.Log($"DomainService.OnDomainSessionJoinedCompleted {sessionId}, isMaster:{isMasterClient}");

            m_AnalyticsService.EnterDomain(m_DomainId, isMasterClient ? DomainEnterType.Host : DomainEnterType.Join);


            OnDomainStateChanged(new DomainStatusEvent()
            {
                state = DomainState.Connected,
                uniqueSessionId = m_CurrentSessionData._id,
                threshold = m_CurrentSessionData.threshold,
                sessionId = sessionId
            });

            onLightHouseAssign?.Invoke();
            sessionIdDomain = true;

            GetAndCreateDomainAssets();
        }

        private void GetAndCreateDomainAssets()
        {
            Debug.Log($"DomainService.GetAndCreateDomainAssets");
            m_DomainAssetService.GetAndCreateDomainAssets(APP_ID, m_DomainId);
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
        const string _testDomainId = "2c7383f1-71fd-4042-a5fd-3da26beba60g";

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (Input.GetKeyDown(KeyCode.D) && _keyRecognition == 0)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.O) && _keyRecognition == 1)
                _keyRecognition++;
            else if (Input.GetKeyDown(KeyCode.M) && _keyRecognition == 2)
            {
                OnDomainQrCodeScanned(_testDomainId);
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
                m_RestService.GetLookingGlassProtocolFullUrl(POST_QUERY_SESSIONS_ENDPOINT),
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
                m_RestService.GetLookingGlassProtocolFullUrl(string.Format(PUT_DOMAIN_SESSION_ENDPOINT, sessionId)),
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