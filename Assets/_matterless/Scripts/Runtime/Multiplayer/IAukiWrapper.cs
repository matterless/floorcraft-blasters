using System;
using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.Hagall.Messages;
using Auki.ConjureKit.Manna;
using Auki.ConjureKit.Vikja;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public interface IAukiWrapper
    {
        Camera arCamera { get; }
        ARCameraManager arCameraManager { get; }
        ARRaycastManager arRaycastManager { get; }
        bool ready { get; }
        float joinTimestamp { get; }
        bool isConnected { get; }
        bool isHost { get; }

        event Action onInit;
        event Action onLeft;
        event Action<Session> onJoined;
        event Action<EntityAction> onEntityAction;
        event Action<ComponentAddBroadcast> onComponentAdd;
        event Action<ComponentUpdateBroadcast> onComponentUpdate;
        event Action<ComponentDeleteBroadcast> onComponentDelete;
        event Action<CustomMessageBroadcast> onCustomMessageBroadcast;
        event Action<uint> onParticipantLeft;
        event Action<Participant> onParticipantJoined;
        event Action<uint> onEntityDeleted;
        event Action<Entity> onEntityAdded;
        event Action<Entity> onEntityUpdatePose;
        event Action<State> onStateChanged;
        event Action<uint, uint> onHostChanged;

        void Install(Action onSuccess, Action onFail);
        void InstallManna(Action<Manna> onComplete);
        void AddEntity(Pose pose, bool persistent, Action<Entity> onEntityAdded, Action<string> onError);
        void DeleteEntity(uint entityId, Action onComplete);
        void BroadcastCustomMessage(byte[] data);
        //void AddComponent(Session session, uint componentTypeId, uint entityId, byte[] data, Action onComplete, Action<string> onError = null);
        //void UpdateComponent(Session session, uint componentTypeId, uint entityId, byte[] data);
        //void DeleteComponent(Session session, uint componentTypeId, uint entityId, Action onComplete, Action<string> onError = null);
        //void AddComponentType(Session session, string name, Action<uint> onComplete, Action<string> onError = null);
        //void SubscribeToComponentType(Session session, uint id, Action onComplete, Action<string> onError = null);
        //void GetComponents(Session session, uint id, Action<List<EntityComponent>> onComplete, Action<string> onError = null);
        void Leave();
        void Join(Action onComplete = null, Action<string> onFail = null);
        void Join(string sessionId , Action onComplete = null, Action<string> onFail = null);
        Auki.Util.Protobuf.WellKnownTypes.Timestamp GetNowAsProtobufTimestamp();
        Session GetSession();
        Entity GetEntity(uint entityId);
        bool HasEntity(uint entityId);
        State GetState();
        bool IsMine(uint entityId);
        bool SendCustomMessage(uint[] participantIds, byte[] data);
        void MeasurePing(Action<double> onComplete, Action<string> onError);
        NetworkQuality GetNetworkQuality(); 
    }
}