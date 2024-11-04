using System;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.Grund;
using Auki.ConjureKit.Hagall.Messages;
using Auki.ConjureKit.Manna;
using Auki.ConjureKit.Vikja;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Matterless.Floorcraft
{
    public class AukiWrapper : IAukiWrapper
    {
        private readonly ARSessionOrigin m_ArSessionOrigin;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly AukiSettings m_Settings;
        private readonly List<uint> m_MyEntities = new();

        private Camera m_ARCamera;
        private ARCameraManager m_ARCameraManager;
        private IConjureKit m_ConjureKit;
        private Vikja m_Vikja;
        private Grund m_Grund;
        private float m_JoinTimestamp;
        private NetworkQuality m_NetworkQuality;
        
        public event Action onInit;
        public event Action onLeft;
        public event Action<Session> onJoined;
        public event Action<EntityAction> onEntityAction;
        public event Action<ComponentAddBroadcast> onComponentAdd;
        public event Action<ComponentUpdateBroadcast> onComponentUpdate;
        public event Action<ComponentDeleteBroadcast> onComponentDelete;
        public event Action<CustomMessageBroadcast> onCustomMessageBroadcast;
        public event Action<uint> onParticipantLeft;
        public event Action<Participant> onParticipantJoined;
        public event Action<uint> onEntityDeleted;
        public event Action<Entity> onEntityAdded;
        public event Action<Entity> onEntityUpdatePose;
        public event Action<State> onStateChanged;
        /// <summary>
        /// Old host, new host respectively
        /// </summary>
        public event Action<uint, uint> onHostChanged;

        public Camera arCamera => m_ARCamera == null ? throw new Exception("AR Camera is null") : m_ARCamera;
        public ARRaycastManager arRaycastManager { get; private set; }
        public ARCameraManager arCameraManager => m_ARCameraManager == null ? throw new Exception("AR Camera Manager is null") : m_ARCameraManager;
        public bool ready { get; private set; }
        public bool isConnected { get; private set; }

        // TODO:: this can be optimised by not checking every frame but only when the session state changes
        public bool isHost
        {
            get
            {
                if(!isConnected) return false;

                var myId = m_ConjureKit.GetSession().ParticipantId;

                foreach (var participant in m_ConjureKit.GetSession().GetParticipants())
                {
                    if (myId > participant.Id)
                        return false;
                }

                return true;
            }
        }
        
        public float joinTimestamp => m_JoinTimestamp;
        private Session m_Session;
        private Action<Manna> m_OnMannaInstalled;

        public bool IsMine(uint entity) => m_MyEntities.Contains(entity);

        public void InstallManna(Action<Manna> onComplete)
        {
            if (m_ConjureKit == null)
            {
                m_OnMannaInstalled = onComplete;
                return;
            }

            onComplete?.Invoke(InstallManna());
        }
        
        private  Manna InstallManna()
        {
            return new Manna(m_ConjureKit, 10f)
            {
                VibrateOnCalibrate = true
            };
        }

        public void AddEntity(Pose pose, bool persistent, Action<Entity> onEntityAdded,
            Action<string> onError)
        {
            m_Session.AddEntity(pose, persistent,
                (entity) =>
                {
                    m_MyEntities.Add(entity.Id);
                    onEntityAdded(entity);
                },
                onError);
        }

        public void DeleteEntity(uint entityId, Action onComplete)
        {
            m_Session.DeleteEntity(entityId,
                () =>
                {
                    m_MyEntities.Remove(entityId);
                    onComplete();
                });
        }
        
        public Entity GetEntity(uint entityId) => m_Session.GetEntity(entityId);
        public bool HasEntity(uint entityId) => m_Session.GetEntity(entityId) != null;

        public void AddComponent(Session session, uint componentTypeId, uint entityId, byte[] data, Action onComplete,
            Action<string> onError = null)
            => session.AddComponent(componentTypeId, entityId, data, onComplete, onError);

        public void UpdateComponent(Session session, uint componentTypeId, uint entityId, byte[] data)
            => session.UpdateComponent(componentTypeId, entityId, data);

        public void DeleteComponent(Session session, uint componentTypeId, uint entityId, Action onComplete, Action<string> onError)
            => session.DeleteComponent(componentTypeId, entityId, onComplete, onError);

        public void AddComponentType(Session session, string name, Action<uint> onComplete, Action<string> onError = null)
            => session.AddComponentType(name, onComplete, onError);

        public void SubscribeToComponentType(Session session, uint id, Action onComplete, Action<string> onError = null)
            => session.SubscribeToComponentType(id, onComplete, onError);

        public Session GetSession() => m_ConjureKit.GetSession();
        public State GetState() => m_ConjureKit.GetState();

        public void GetComponents(Session session, uint id, Action<List<EntityComponent>> onComplete,
            Action<string> onError = null)
            => session.GetComponents(id, onComplete, onError);

        public Auki.Util.Protobuf.WellKnownTypes.Timestamp GetNowAsProtobufTimestamp()
            => m_ConjureKit.GetNowAsProtobufTimestamp();

        public bool SendCustomMessage(uint[] participantIds, byte[] data)
            => m_ConjureKit.SendCustomMessage(participantIds, data);

        public void RequestAction(uint entityId, string name, byte[] data, Action<EntityAction> onComplete,
            Action<string> onError)
            => m_Vikja.RequestAction(entityId, name, data, onComplete, onError);

        public void Leave()
        {
            Debug.Log("[domain] left");
            m_ConjureKit.Disconnect();
        }

        public void Join(Action onComplete = null, Action<string> onFail = null)
        {
            Debug.Log("new session");
            m_ConjureKit.Connect(session=>onComplete?.Invoke(), onFail);
        }

        public void Join(string sessionId, Action onComplete = null, Action<string> onFail = null)
        {
            Debug.Log("join session " + sessionId);
            m_ConjureKit.Connect(sessionId, session=>onComplete?.Invoke(), onFail);
        }

        public void BroadcastCustomMessage(byte[] data)
        {
            m_ConjureKit.SendCustomMessage(m_Session.GetParticipantsIds().ToArray(), data);
        }

        public void MeasurePing(Action<double> onComplete, Action<string> onError)
        {
            if (m_ConjureKit == null)
                return;

            m_ConjureKit.MeasurePing(onComplete, onError);
        }

        public NetworkQuality GetNetworkQuality()
        {
            if (m_NetworkQuality == null)
                m_NetworkQuality = m_ConjureKit.GetNetworkQuality();
            
            return m_NetworkQuality;
        }
        
        public void SendCustomMessageToParticipant(uint participantId, byte[] data)
        {
            m_ConjureKit.SendCustomMessage(new []{participantId}, data);
        }

        public AukiWrapper(
            IAnalyticsService analyticsService, 
            AukiSettings settings,
            ARSessionOrigin arSessionOrigin, 
            ARSession session

            )
        {
            m_AnalyticsService = analyticsService;
            m_Settings = settings;
            m_ArSessionOrigin = arSessionOrigin;
        }

        public void Install(Action onSuccess, Action onFail)
        {
            Debug.Log("Installing Auki...");

            // install Auki modules
            m_ConjureKit = new ConjureKit(
                m_ArSessionOrigin.camera.transform,
                m_Settings.appKey, m_Settings.appSecret, m_Settings.logLevel);
            
            FinalizeInstallation();
            onSuccess?.Invoke();

            // No need to call conjurekit configuration initalization anymore
            /*ConjureKitConfiguration config = new ConjureKitConfiguration();

            try
            {
                config = ConjureKitConfiguration.Get();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                onFail?.Invoke();
            }
            finally
            {
                m_ConjureKit.Init(config, 
                    () =>
                    {
                        Debug.Log("Successfully inited ConjureKit");
                        FinalizeInstallation();
                        onSuccess();
                    },
                    (string error) =>
                    {
                        Debug.LogError("Failed to init ConjureKit: " + error);
                        onFail?.Invoke();
                    });
            }*/
        }

        private void FinalizeInstallation()
        {
            m_Vikja = new Vikja(m_ConjureKit);
            m_Grund = new Grund(m_ConjureKit, m_Vikja);
            InitConjureKit(m_Settings);
            InitVikja();
            m_OnMannaInstalled?.Invoke(InstallManna());

            if (m_Settings.autoJoinOnStart)
            {
                if (m_Settings.useThisSessionIdInEditor && Application.isEditor)
                    m_ConjureKit.Connect(m_Settings.sessionId);
                else
                    m_ConjureKit.Connect();
            }

            onInit?.Invoke();
        }

        private void InitVikja()
        {
            m_Vikja.OnEntityAction += OnEntityAction;
        }

        private void InitConjureKit(AukiSettings settings)
        {
            m_ConjureKit.OnLeft += OnLeft;
            m_ConjureKit.OnJoined += OnJoined;
            m_ConjureKit.OnEntityAdded += OnEntityAdded;
            m_ConjureKit.OnComponentDelete += OnComponentDelete;
            m_ConjureKit.OnEntityDeleted += OnEntityDeleted;
            m_ConjureKit.OnParticipantJoined += OnParticipantJoined;
            m_ConjureKit.OnParticipantLeft += OnParticipantLeft;
            m_ConjureKit.OnCustomMessageBroadcast += OnCustomMessageBroadcast;
            m_ConjureKit.OnComponentAdd += OnComponentAdd;
            m_ConjureKit.OnComponentUpdate += OnComponentUpdate;
            m_ConjureKit.OnEntityUpdatePose += OnEntityUpdatePose;


            // cache XR and Unity components
            m_ARCamera = m_ArSessionOrigin.GetComponentInChildren<Camera>();
            m_ARCameraManager = m_ArSessionOrigin.GetComponentInChildren<ARCameraManager>();
            m_ARCamera.cullingMask = settings.cameraCullingMask.value;
            arRaycastManager = m_ArSessionOrigin.GetComponent<ARRaycastManager>();

            // set AR occlusion settings
            var arOcclusionManager =
                m_ArSessionOrigin.GetComponentInChildren<AROcclusionManager>();
            arOcclusionManager.requestedHumanDepthMode = settings.humanSegmentationDepthMode;
            arOcclusionManager.requestedHumanStencilMode = settings.humanSegmentationStencilMode;
            arOcclusionManager.requestedEnvironmentDepthMode = settings.environmentDepthMode;

            // set AR plane manager settings
            var arPlaneManager = m_ArSessionOrigin.GetComponent<ARPlaneManager>();
            arPlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;

            m_ConjureKit.OnStateChanged += OnAukiStateChanged;
            // use grund
            m_Grund.SetActive(settings.useGrund);
        }

        private void OnEntityUpdatePose(Entity obj) => onEntityUpdatePose?.Invoke(obj);
        private void OnComponentUpdate(ComponentUpdateBroadcast obj) => onComponentUpdate?.Invoke(obj);
        private void OnComponentDelete(ComponentDeleteBroadcast obj) => onComponentDelete?.Invoke(obj);
        private void OnEntityAction(EntityAction obj) => onEntityAction?.Invoke(obj);
        private void OnComponentAdd(ComponentAddBroadcast obj) => onComponentAdd?.Invoke(obj);
        private void OnCustomMessageBroadcast(CustomMessageBroadcast obj) => onCustomMessageBroadcast?.Invoke(obj);

        private void OnParticipantLeft(uint leftParticipantId)
        {
            onParticipantLeft?.Invoke(leftParticipantId);

            uint hostId = 999;
            List<Participant> participants = m_ConjureKit.GetSession().GetParticipants();

            if (participants.Count == 0)
                return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (hostId > participants[i].Id)
                {
                    hostId = participants[i].Id;
                }
            }

            if (hostId != leftParticipantId && hostId > leftParticipantId)
            {
                onHostChanged?.Invoke(leftParticipantId, hostId);
            }
        }

        private void OnParticipantJoined(Participant participant)
        {
            Debug.Log($"Participant with id {participant.Id} joined the session");
            onParticipantJoined?.Invoke(participant);
        } 
        private void OnEntityDeleted(uint obj)
        {
            Debug.Log($"Got on entity deleted from conjure kit for id {obj}");
            onEntityDeleted?.Invoke(obj);
        }
        
        private void OnEntityAdded(Entity obj) => onEntityAdded?.Invoke(obj);

        private void OnLeft(Session session)
        {
            isConnected = false;
            m_MyEntities.Clear();
            onLeft?.Invoke();
        }

        private void OnJoined(Session session)
        {
            Debug.Log($"Joined session {session.Id} with participant id {session.ParticipantId} ");
            isConnected = true;
            m_Session = session;
            onJoined?.Invoke(session);
            m_JoinTimestamp = Time.time;
            m_AnalyticsService.ArSessionEnter(session.Id , (uint)session.GetParticipants().Count);
        }

        private void OnAukiStateChanged(State state)
        {
            Debug.Log($"[AukiWrapper] OnAukiStateChanged {state}");
            onStateChanged?.Invoke(state);
            
            if (!ready)
            {
                ready = state == State.JoinedSession || state == State.Calibrated;
            }
        }
    }
}