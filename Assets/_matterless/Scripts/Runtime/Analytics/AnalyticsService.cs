using System;
using System.Collections.Generic;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public enum CarExplodeCause
    {
        Obstacle,
        Player,
        Despawn,
        ProximityMine,
        WreckingBall,
        Laser
    }

    public class AnalyticsService : IAnalyticsService
    {
        private enum AnalyticsEvent
        {
            APP_LAUNCH,
            APP_BG,
            APP_FG,
            AR_SESSION_ENTER,
            AR_SESSION_EXIT,
            AR_INVITE_SHOW,
            AR_INVITE_HIDE,
            AR_CAR_SPAWN,
            AR_CAR_DIED,
            AR_CAR_KILL,
            AR_ITEM_SPAWN,
            AR_RECORD_START,
            AR_RECORD_FINISH,
            AR_PHOTO_CAPTURE,
            AR_DOMAIN_SEEN,
            AR_DOMAIN_ENTER,
            AR_POWERUP_USE
        }


        private readonly Amplitude m_Amplitude;
        private readonly Dictionary<string, object> m_SessionIdDictionary;
        private readonly Dictionary<string, object> m_SessionParticipantDictionary;
        private readonly Dictionary<string, object> m_ExitSessionDictionary;
        private readonly Dictionary<string, object> m_SpawnVehicleDictionary;
        private readonly Dictionary<string, object> m_VehicleExplodedDictionary;
        private readonly Dictionary<string, object> m_FinishRecordDictionary;
        private readonly Dictionary<string, object> m_DomainDictionary;
        private readonly Dictionary<string, object> m_DomainJoinTypeDictionary;
        private readonly Dictionary<string, object> m_PowerUpDictionary;
        private readonly Dictionary<string, object> m_ItemSpawnDictionary;

        private const string SESSION_ID_PARAM = "Session Id";
        private const string DOMAIN_ID_PARAM = "Domain Id";
        private const string DOMAIN_JOIN_TYPE_PARAM = "Domain Join Type";
        private const string SESSION_PARTICIPANT_PARAM = "Participant Id";
        private const string HORIZONTAL_DURATION_PARAM = "Horizontal Duration Id";
        private const string VERTICAL_DURATION_PARAM = "Vertical Duration Id";
        private const string RAYCAST_DISTANCE_PARAM = "Raycast Distance Id";
        private const string VEHICLE_TYPE_PARAM = "Vehicle Type Id";
        private const string EXPLODE_CAUSE_PARAM = "Explode Cause Id";
        private const string VIDEO_DURATION_PARAM = "Video Duration Id";
        private const string POWERUP_ITEM_PARAM = "item Id";
        private const string POWERUP_TYPE_PARAM = "item type";
        private const string POWERUP_QUANTITY_PARAM = "Quantity";

        public AnalyticsService(IUnityEventDispatcher unityEventDispatcher)
        {
            m_Amplitude = Amplitude.Instance;
            m_Amplitude.logging = true;
            m_Amplitude.init("YOUR_AMPLITUDE_ID");
            SendEvent(AnalyticsEvent.APP_LAUNCH);
            Debug.Log("Test");
            m_DomainDictionary = new Dictionary<string, object>()
            {
                {DOMAIN_ID_PARAM, string.Empty}
            };

            m_DomainJoinTypeDictionary = new Dictionary<string, object>()
            {
                {DOMAIN_ID_PARAM, string.Empty},
                {DOMAIN_JOIN_TYPE_PARAM, string.Empty}
            };
            // cache dictionaries
            m_SessionIdDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
            };
            m_SessionParticipantDictionary =new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {SESSION_PARTICIPANT_PARAM, 0}
            };
                
            m_ExitSessionDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {HORIZONTAL_DURATION_PARAM, 0f},
                {VERTICAL_DURATION_PARAM, 0f},
            };
            m_SpawnVehicleDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {RAYCAST_DISTANCE_PARAM, 0f},
                {VEHICLE_TYPE_PARAM, 0}
            };
            m_VehicleExplodedDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {EXPLODE_CAUSE_PARAM, new CarExplodeCause()},
            };
            m_FinishRecordDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {VIDEO_DURATION_PARAM, 0f},
            };
            m_ItemSpawnDictionary = new Dictionary<string, object>()
            {
                {SESSION_ID_PARAM, string.Empty},
                {POWERUP_ITEM_PARAM, string.Empty},
                {POWERUP_TYPE_PARAM, string.Empty},
                {SESSION_PARTICIPANT_PARAM, 0},
                {RAYCAST_DISTANCE_PARAM, 0f},
            };
            m_PowerUpDictionary = new Dictionary<string, object>()
            {
                {POWERUP_ITEM_PARAM, 0},
                {POWERUP_QUANTITY_PARAM,-1}
            };

            unityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
        }

        private void OnApplicationPause(bool paused)
        {
            SendEvent(paused ? AnalyticsEvent.APP_BG : AnalyticsEvent.APP_FG );
        }
        
        public void ArSessionEnter(string sessionId , uint participantCount)
        {
            m_SessionParticipantDictionary[SESSION_ID_PARAM] = sessionId;
            m_SessionParticipantDictionary[SESSION_PARTICIPANT_PARAM] = participantCount;
            SendEvent(AnalyticsEvent.AR_SESSION_ENTER, m_SessionParticipantDictionary);
        }

        public void ShowQRCode(string sessionId )
        {
            m_SessionIdDictionary[SESSION_ID_PARAM] = sessionId;
            SendEvent(AnalyticsEvent.AR_INVITE_SHOW, m_SessionIdDictionary);
        }

        public void HideQRCode(string sessionId )
        {
            m_SessionIdDictionary[SESSION_ID_PARAM] = sessionId;
            SendEvent(AnalyticsEvent.AR_INVITE_HIDE, m_SessionIdDictionary);
        }

        public void ExitSession( float horizontalDuration, float verticalDuration,string spaceId)
        {
            Debug.Log("exit analytics");
            m_ExitSessionDictionary[SESSION_ID_PARAM] = spaceId == string.Empty ? m_SessionParticipantDictionary[SESSION_ID_PARAM] : spaceId;
            m_ExitSessionDictionary[HORIZONTAL_DURATION_PARAM] = horizontalDuration;
            m_ExitSessionDictionary[VERTICAL_DURATION_PARAM] = verticalDuration;
            SendEvent(AnalyticsEvent.AR_SESSION_EXIT, m_ExitSessionDictionary);
        }

        public void SpawnVehicle(string sessionId, float raycastDistance, string vehicleType)
        {
            m_SpawnVehicleDictionary[SESSION_ID_PARAM] = sessionId;
            m_SpawnVehicleDictionary[RAYCAST_DISTANCE_PARAM] = raycastDistance;
            m_SpawnVehicleDictionary[VEHICLE_TYPE_PARAM] = vehicleType;
            SendEvent(AnalyticsEvent.AR_CAR_SPAWN, m_SpawnVehicleDictionary);
        }

        public void PlayerVehicleExploded(string sessionId, CarExplodeCause cause)
        {
            m_VehicleExplodedDictionary[SESSION_ID_PARAM] = sessionId;
            m_VehicleExplodedDictionary[EXPLODE_CAUSE_PARAM] = cause;
            SendEvent(AnalyticsEvent.AR_CAR_DIED, m_VehicleExplodedDictionary);
        }

        public void PlayerCauseAnotherPlayerExplode(string sessionId)
        {
            m_SessionIdDictionary[SESSION_ID_PARAM] = sessionId;
            SendEvent(AnalyticsEvent.AR_CAR_KILL, m_SessionIdDictionary);
        }

        public void PlaceObstacle(AssetType assetType,string assetId,float raycastDistance,string sessionId,int participantCount)
        {
            m_ItemSpawnDictionary[SESSION_ID_PARAM] = sessionId;
            m_ItemSpawnDictionary[POWERUP_ITEM_PARAM] = assetId;
            m_ItemSpawnDictionary[POWERUP_TYPE_PARAM] = assetType;
            m_ItemSpawnDictionary[SESSION_PARTICIPANT_PARAM] = participantCount;
            m_ItemSpawnDictionary[RAYCAST_DISTANCE_PARAM] = raycastDistance;
            SendEvent(AnalyticsEvent.AR_ITEM_SPAWN, m_ItemSpawnDictionary);
        }

        public void StartRecording(string sessionId = "")
        {
            m_SessionIdDictionary[SESSION_ID_PARAM] = sessionId == string.Empty ? (string) m_SessionIdDictionary[SESSION_ID_PARAM] : sessionId;
            SendEvent(AnalyticsEvent.AR_RECORD_START, m_SessionIdDictionary);
        }

        public void FinishRecording(float duration, string sessionId = "")
        {
            m_FinishRecordDictionary[SESSION_ID_PARAM] = sessionId == string.Empty ? (string) m_SessionIdDictionary[SESSION_ID_PARAM] : sessionId;
            m_FinishRecordDictionary[VIDEO_DURATION_PARAM] = duration;
            SendEvent(AnalyticsEvent.AR_RECORD_FINISH, m_FinishRecordDictionary);
        }

        public void TakePhoto(string sessionId = "")
        {
            m_SessionIdDictionary[SESSION_ID_PARAM] = sessionId == string.Empty ? (string) m_SessionIdDictionary[SESSION_ID_PARAM] : sessionId;
            SendEvent(AnalyticsEvent.AR_PHOTO_CAPTURE, m_SessionIdDictionary);
        }

        public void SeenDomain(string domainId)
        {
            m_DomainDictionary[DOMAIN_ID_PARAM] = domainId;
            SendEvent(AnalyticsEvent.AR_DOMAIN_SEEN, m_DomainDictionary);
        }
        public void EnterDomain(string domainId,DomainEnterType domainEnterType)
        {
            m_DomainJoinTypeDictionary[DOMAIN_ID_PARAM] = domainId;
            m_DomainJoinTypeDictionary[DOMAIN_JOIN_TYPE_PARAM] = domainEnterType.ToString();
            SendEvent(AnalyticsEvent.AR_DOMAIN_ENTER, m_DomainJoinTypeDictionary);
        }

        public void PowerUpUse(EquipmentState state, int quantity)
        {
            m_PowerUpDictionary[POWERUP_ITEM_PARAM] = state;
            m_PowerUpDictionary[POWERUP_QUANTITY_PARAM] = quantity;
            SendEvent(AnalyticsEvent.AR_POWERUP_USE, m_PowerUpDictionary);
        }
        private void SendEvent(AnalyticsEvent eventId, Dictionary<string, object> parameter = null)
        {
            // don't send analytics from unity editor
            if(Application.isEditor)
                return;
            
            m_Amplitude.logEvent(eventId.ToString(), parameter);
        }
        
    }

    public enum DomainEnterType
    {
        None,
        Assign,
        Join,
        Host
    }
}
