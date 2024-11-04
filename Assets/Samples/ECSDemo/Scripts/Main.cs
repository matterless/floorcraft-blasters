using System.Collections.Generic;
using Auki.ConjureKit;
using UnityEngine;
using UnityEngine.UI;

namespace AukiSampleECSDemo
{
    /// <summary>
    /// This sample shows a basic ConjureKit ECS use case - a custom system that controls the scale of a game object.
    /// * The InputField in the upper right allows you to input a session id & join another device in a shared session.
    /// * The Spawn button in the upper right will instantiate a cube that will have a scale component controlled by ECSSampleScaleSystem.
    /// </summary>
    public class Main : MonoBehaviour
    {
        /// <summary>
        /// Transform of the camera GameObject. Required by ConjureKit.
        /// </summary>
        public Transform cameraTransform;

        /// <summary>
        /// When pressed will instantiate a cube and attach a scaling component to it.
        /// </summary>
        public Button spawnButton;
        
        /// <summary>
        /// Label that shows currently joined session id.
        /// </summary>
        public Text sessionInfo;
        
        /// <summary>
        /// Label that shows current ConjureKit state.
        /// </summary>
        public Text conjureKitStateInfo;
        
        /// <summary>
        /// When pressed will try to join session with id from customSessionInputField.
        /// </summary>
        public Button joinCustomSessionButton;
        
        /// <summary>
        /// Input field that allows you to specify session to join by id.
        /// If the session does not exist the participant will be redirected to a new one.
        /// </summary>
        public InputField customSessionInputField;
        private ConjureKit _conjureKit;
       
        private bool _joined;
        private string _lastJoinedSessionId;
    
        private readonly Dictionary<uint, GameObject> _entityMap = new Dictionary<uint, GameObject>();
        private Entity _localParticipantCubeEntity;
    
        private uint _testScaleComponentId;
        private ECSSampleScaleSystem _system;
        
        /// <summary>
        /// If there's no reference to BoxCollider GameObject.CreatePrimitive in Spawn() might fail.
        /// Read note here -- https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html.
        /// </summary>
        private BoxCollider _preventBoxColliderStripping = new BoxCollider();
        
        private void Start()
        {
            // Initialize the SDK.
            _conjureKit = new ConjureKit(
                cameraTransform,
                "insert_app_key_here",
                "insert_app_secret_here"
            );
            
            _conjureKit.OnJoined += session =>
            {
                sessionInfo.text = session.Id;
                _joined = true;
                _lastJoinedSessionId = session.Id;
                _system = new ECSSampleScaleSystem(OnNewEntityScaleReceived, session);
                
                // Systems must be registered with the session object to work correctly.
                session.RegisterSystem(_system, () => Debug.Log($"ECSSampleScaleSystem registered."));
            };
    
            // Remove cubes for deleted non-participant entities.
            _conjureKit.OnEntityDeleted += entityId =>
            {
                if (!_entityMap.ContainsKey(entityId)) return;
                var go = _entityMap[entityId];
                _entityMap.Remove(entityId);
                Destroy(go);
            };

            _conjureKit.OnLeft += session =>
            {
                sessionInfo.text = "";
                _joined = false;
                _localParticipantCubeEntity = null;
                _system = null;
                foreach (var (_, go) in _entityMap)
                {
                    Destroy(go);
                }
                _entityMap.Clear();
            };
    
            _conjureKit.OnStateChanged += state =>
            {
                ToggleControlsState(state == State.JoinedSession || state == State.Calibrated);
                conjureKitStateInfo.text = state.ToString();
            };
            
            _conjureKit.Connect();
        }
        
        private void OnNewEntityScaleReceived(uint entityId, float newScale)
        {
            if (!_entityMap.ContainsKey(entityId))
            {
                _entityMap[entityId] = CreateCube(DefaultCubePose());
            }

            var targetGameObject = _entityMap[entityId];
            targetGameObject.transform.localScale = Vector3.one * newScale;
        }

        private void ToggleControlsState(bool interactable)
        {
            spawnButton.interactable = interactable;
            joinCustomSessionButton.interactable = interactable;
        }

        private void Update()
        {
            if (!_joined) return;
            if (_localParticipantCubeEntity == null || !_entityMap.ContainsKey(_localParticipantCubeEntity.Id)) return;
            _system?.UpdateLocalEntityScale(_localParticipantCubeEntity);
        }
    
        /// <summary>
        /// Spawn a new cube object in front of the camera.
        /// The ECSSampleScaleSystem will be driving its scale component.
        /// </summary>
        public void Spawn()
        {
            if (!_joined) return;
            if (_localParticipantCubeEntity != null) return;
            
            var session = _conjureKit.GetSession();
            if (session == null) return;

            var cubePose = DefaultCubePose();

            // Create an entity for this cube.
            session.AddEntity(
                cubePose,
                entity =>
                {
                    _entityMap[entity.Id] = CreateCube(cubePose);
                    _system.AttachToEntity(
                        entity,
                        () => _localParticipantCubeEntity = entity
                    );
                },
                Debug.Log
            );
        }

        private Pose DefaultCubePose()
        {
            var position = cameraTransform.position + cameraTransform.forward * 0.5f;
            var cubePose = new Pose(position, Quaternion.identity);
            return cubePose;
        }

        private GameObject CreateCube(Pose cubePose)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetPositionAndRotation(cubePose.position, cubePose.rotation);
            go.transform.localScale = Vector3.one * 0.01f;
            return go;
        }

        /// <summary>
        /// Tries to join a session by the id in the custom session InputField in the top right.
        /// </summary>
        public void JoinCustomSessionButtonPressed()
        {
            var targetSessionId = customSessionInputField.text;
            if (string.IsNullOrEmpty(targetSessionId) || targetSessionId == _lastJoinedSessionId) return;
            Debug.Log($"Joining custom session: {targetSessionId}");
            _conjureKit.Disconnect();
            _conjureKit.Connect(
                targetSessionId,
                session => Debug.Log($"Joined session {session.Id}"),
                _ =>
                {
                    Debug.LogWarning($"Failed to join session {targetSessionId}, connecting to a new session instead.");
                    _conjureKit.Connect();
                }
            );
        }
    }
}
