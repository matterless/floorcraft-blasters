using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.Hagall.Messages;
using Auki.ConjureKit.Manna;
using Auki.ConjureKit.Vikja;


using Auki.Util.Protobuf.WellKnownTypes;

using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public class AukiWrapperMock : IAukiWrapper, ITickable
    {
        public Camera arCamera {get; private set;}
        public ARCameraManager arCameraManager { get; }
        public ARRaycastManager arRaycastManager { get; private set; }
        public IConjureKit conjureKit { get; private set; }
        public bool ready { get; private set; }
        public float joinTimestamp { get; }
        public bool isConnected { get; private set; }
        public bool isHost => true;

        public event Action onInit;
        public event Action onLeft;
        public event Action<Session> onJoined;
        public event Action<EntityAction> onEntityAction;
        public event Action<ComponentAddBroadcast> onComponentAdd;
        public event Action<ComponentUpdateBroadcast> onComponentUpdate;
        public event Action<CustomMessageBroadcast> onCustomMessageBroadcast;
        public event Action<uint> onParticipantLeft;
        public event Action<Participant> onParticipantJoined;
        public event Action<uint> onEntityDeleted;
        public event Action<Auki.ConjureKit.Entity> onEntityAdded;
        public event Action<Auki.ConjureKit.Entity> onEntityUpdatePose;
        public event Action<LighthousePose[], Action<LighthousePose>> onPoseSelect;
        public event Action<CalibrationFailureData> onCalibrationFail;
        public event Action<ComponentDeleteBroadcast> onComponentDelete;
        public event Action<State> onStateChanged;
        public event Action onJoiningNewSession;
        public event Action<uint, uint> onHostChanged;

        public AukiWrapperMock(ICoroutineRunner coroutineRunner)
        {
            var aukiMockMono = new GameObject("AukiWrapperMock");
            var arCameraObject = new GameObject("AR Camera");
            arCamera = arCameraObject.AddComponent<Camera>();
            arCameraObject.transform.SetParent(aukiMockMono.transform);
            arRaycastManager = arCameraObject.AddComponent<ARRaycastManager>();
            m_CoroutineRunner = coroutineRunner;
        }

        public void Install(Action onSuccess, Action onFail)
        {
            ready = true;

            onInit?.Invoke();

            m_CoroutineRunner.StartUnityCoroutine(JoinCoroutine());
        }

        public void InstallManna(Action<Manna> onComplete)
        {
            throw new NotImplementedException();
        }

        private IEnumerator JoinCoroutine()
        {
            yield return new WaitForSeconds(3);
            Join();
        }

        public Entity GetEntity(uint entityId)
        {
            throw new NotImplementedException();
        }

        public State GetState() => State.Calibrated;
        
        public void SetLighthouseVisible(bool show)
        {
            Debug.Log("Show LightHouse");
        }

        public void AddEntity(Pose pose, bool persistent, Action<Auki.ConjureKit.Entity> onEntityAdded, Action<string> onError)
        {
            Debug.Log("AukiMock:: AddEntity");
        }

        public void AddComponent(uint componentTypeId, uint entityId, byte[] data, Action onComplete, Action<string> onError = null)
        {
            Debug.Log("AukiMock:: AddComponent");
        }

        public void AddComponentType(string name, Action<uint> onComplete, Action<string> onError = null)
        {
            Debug.Log("AukiMock:: AddComponentType");
        }

        public void SubscribeToComponentType(uint id, Action onComplete, Action<string> onError = null)
        {
            Debug.Log("AukiMock:: SubscribeToComponentType");
        }

        public void GetComponents(uint id, Action<List<Auki.ConjureKit.EntityComponent>> onComplete, Action<string> onError = null)
        {
            Debug.Log("AukiMock:: GetComponents");
        }

        public bool SendCustomMessage(uint[] participantIds, byte[] data)
        {
            Debug.Log("AukiMock:: SendCustomMessage");
            return true;
        }

        public void Join(string sessionId, Action onComplete = null, Action<string> onFail = null)
        {
            isConnected = true;
            //m_Session.Id = sessionId;
            onJoined?.Invoke(null);
            onComplete?.Invoke();
            ready = true;
        }

        public Timestamp GetNowAsProtobufTimestamp()
        {
            Debug.Log("AukiMock:: GetNowAsProtobufTimestamp");
            return new Timestamp();
        }

        // Session m_Session = new Session(
        //     "mock",
        //     (uint)0,
        //     new Dictionary<uint, Participant>() { { 0, new Participant(0) } },
        //     new Dictionary<uint, Auki.ConjureKit.Entity>() { { 0, new Auki.ConjureKit.Entity(0,0, new Pose()) } },
        //     new Dictionary<uint, Dictionary<uint, Auki.ConjureKit.EntityComponent>>()
        //     );
        private readonly ICoroutineRunner m_CoroutineRunner;

        public Session GetSession()
        {
            Debug.Log("AukiMock:: GetSession");
            return null;
        }

        public void RequestAction(uint entityId, string name, byte[] data, Action<EntityAction> onComplete, Action<string> onError)
        {
            Debug.Log("AukiMock:: RequestAction");
        }

        public void UpdateEntityPose(uint entityId, Pose pose)
        {
            Debug.Log("AukiMock:: UpdateEntityPose");
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            if (Input.GetKeyDown(KeyCode.J))
                Join();

            if (Input.GetKeyUp(KeyCode.Escape))
                Leave();
        }
        

        public void Leave()
        {
            isConnected = false;
            onLeft?.Invoke();
            ready = false;
        }

        public void Join(Action onComplete = null, Action<string> onFail = null)
        {
            isConnected = true;
            onJoined?.Invoke(null);
            onComplete?.Invoke();
            ready = true;
        }

        public void UpdateComponent(uint componentTypeId, uint entityId, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void DeleteComponent(uint componentTypeId, uint entityId, Action onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }

        public void DeleteEntity(uint entityId, Action onComplete)
        {
            throw new NotImplementedException();
        }

        public bool IsMine(uint entityId)
        {
            throw new NotImplementedException();
        }

        public void MeasurePing(Action<double> onComplete, Action<string> onError)
        {
            throw new NotImplementedException();
        }

        public NetworkQuality GetNetworkQuality()
        {
            throw new NotImplementedException();
        }

        public void Join(string sessionId,Action<string> onFail)
        {
            throw new NotImplementedException();
        }

        public bool HasEntity(uint entityId)
        {
            throw new NotImplementedException();
        }

        public void BroadcastCustomMessage(byte[] data)
        {
            throw new NotImplementedException();
        }

        public void AddComponent(Session session, uint componentTypeId, uint entityId, byte[] data, Action onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }

        public void UpdateComponent(Session session, uint componentTypeId, uint entityId, byte[] data)
        {
            throw new NotImplementedException();
        }

        public void DeleteComponent(Session session, uint componentTypeId, uint entityId, Action onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }

        public void AddComponentType(Session session, string name, Action<uint> onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }

        public void SubscribeToComponentType(Session session, uint id, Action onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }

        public void GetComponents(Session session, uint id, Action<List<EntityComponent>> onComplete, Action<string> onError = null)
        {
            throw new NotImplementedException();
        }
    }
}