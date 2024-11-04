using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Auki.ConjureKit.Hagall.Messages;
using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace Matterless.Floorcraft
{
    public class ShadowCloneService : ITickable
    {
        [System.Serializable]
        public class Settings : IEquipmentSetting
        {
            public Material cloneMaterial => m_CloneMaterial;
            public ParticleSystem cloneVFX => m_CloneVFX;
            public float cooldown => m_CooldownDuration;
            public float duration => m_CloneDuration;
            public int quantity => m_Quantity;
            public bool infinite => m_Infinite;
            
            [SerializeField] private float m_CooldownDuration;
            [SerializeField] private float m_CloneDuration;
            [SerializeField] private int m_Quantity = 3;
            [SerializeField] private Material m_CloneMaterial;
            [SerializeField] private ParticleSystem m_CloneVFX;
            [SerializeField] private bool m_Infinite = false;
        }

        private readonly AukiWrapper m_AukiWrapper;
        private readonly SpeederService m_SpeederService;
        private readonly PropertiesComponentService m_PropertiesComponentService;
        private readonly TransformComponentService m_TransformComponentService;
        private readonly MessageComponentService m_MessageComponentService;
        private readonly CloneComponentService m_CloneComponentService;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly CooldownService m_CooldownService;
        private readonly SpeederHUDService m_SpeederHUDService;
        private readonly IRaycastService m_RaycastService;


        private readonly SpeederStateComponentService m_SpeederStateComponentService;

        private readonly SpeederSimulation.Settings m_SimulationSettings;
        private readonly EquipmentService m_EquipmentComponentService;

        private readonly Settings m_Settings;
        private readonly WorldScaleService m_WorldScaleService;
        private uint m_CloneEntityId;
        private Vector3 m_Direction;
        private Vector3 m_StartPos;
        private bool m_CoolDown;
        private List<uint> m_CloneEntityIdList = new List<uint>();

        public ShadowCloneService(AukiWrapper aukiWrapper,
            SpeederService speederService,
            SpeederHUDService speederHUDService,
            PropertiesComponentService propertiesComponentService,
            TransformComponentService transformComponentService,
            SpeederStateComponentService speederStateComponentService,
            MessageComponentService messageComponentService,
            CloneComponentService cloneComponentService,
            EquipmentService equipmentComponentService,
            IRaycastService raycastService,
            CoroutineRunner coroutineRunner,
            CooldownService cooldownService,
            Settings settings,
            WorldScaleService worldScaleService
        )
        {
            m_AukiWrapper = aukiWrapper;
            m_SpeederService = speederService;
            m_PropertiesComponentService = propertiesComponentService;
            m_TransformComponentService = transformComponentService;
            m_SpeederStateComponentService = speederStateComponentService;
            m_MessageComponentService = messageComponentService;
            m_SpeederHUDService = speederHUDService;
            m_EquipmentComponentService = equipmentComponentService;
            m_RaycastService = raycastService;
            m_CloneComponentService = cloneComponentService;
            m_Settings = settings;
            m_WorldScaleService = worldScaleService;
            m_CoroutineRunner = coroutineRunner;
            m_CooldownService = cooldownService;
            m_PropertiesComponentService.onComponentAdded += OnPropertyComponentAdded;
            m_AukiWrapper.onEntityDeleted += OnCloneEntityDeleted;
            m_AukiWrapper.onCustomMessageBroadcast += OnCustomMessageBroadcast;
            m_SpeederStateComponentService.onComponentAdded += OnSpeederStateAdded;
            m_TransformComponentService.onComponentAdded += OnTransformComponentAdded;
            m_MessageComponentService.onComponentUpdated += OnMessageUpdate;
        }

        private void CreateShadowClone()
        {
            m_CooldownService.ActivateCooldown();
            uint entityId = m_SpeederService.serverSpeederEntity;
            Pose pose = new Pose(m_SpeederService.speederViews[entityId].gameObject.transform.position,
                m_SpeederService.speederViews[entityId].gameObject.transform.rotation);
            float speed = m_SpeederService.serverSpeeder.speed;
            Vehicle vehicle = m_SpeederService.GetVehicle(entityId);
            m_AukiWrapper.AddEntity(pose, false,
                entity =>
                {
                    Debug.Log("start clone position " + pose.position);
                    m_CloneEntityId = entity.Id;
                    m_CloneComponentService.AddComponent(entity.Id, new CloneModel(entityId));
                    m_SpeederStateComponentService.AddComponent(entity.Id, new SpeederStateModel(SpeederState.Clone));
                    m_PropertiesComponentService.AddComponent(entity.Id, new PropertiesModel(vehicle.id));
                    m_TransformComponentService.AddComponent(entity.Id,
                        new TransformModel(pose.position, pose.rotation, speed));
                    m_Direction = pose.rotation * Vector3.forward;
                    m_StartPos = pose.position;
                    m_MessageComponentService.AddComponent(entity.Id,
                        new MessageModel(MessageModel.Message.None, entity.Id));
                    m_CoroutineRunner.StartUnityCoroutine(WaitCloneDie(m_CloneEntityId));
                    
                }, _ => { Debug.LogError("Error : Create Shadow Clone. "); });

            m_TransformComponentService.SetFrequencyToEveryFrame();
            m_CoolDown = true;
        }

        private void OnPropertyComponentAdded(PropertiesComponentModel model)
        {
            if (!IsClone(model.entityId))
            {
                return;
            }
            // TODO : it should be every one
            if (!m_CloneComponentService.TryGetComponentModel(model.entityId, out CloneComponentModel cloneComponentModel))
            {
                return;
            }
            uint originEntityId = cloneComponentModel.model.originEntityId;
            ShadowCloneGameObject shadowCloneGameObject = m_PropertiesComponentService
                .GetView<SpeederView>(model.entityId).gameObject
                .AddComponent<ShadowCloneGameObject>();
            shadowCloneGameObject.originEntityId = originEntityId;
            // try to make it as illusion only
            m_PropertiesComponentService
                .GetView<SpeederView>(model.entityId).GetComponent<Collider>().enabled = false;
            
        }

        private void OnCloneEntityDeleted(uint entityId)
        {
            if (IsClone(entityId))
            {
                m_CloneEntityIdList.Remove(entityId);
            }
        }

        private void OnTransformComponentAdded(TransformComponentModel model)
        {
            if (IsClone(model.entityId))
            {
                if (model.isMine)
                {
                    // TODO : is there any way to get all the mesh instead of using GetComponentsInChildren
                    foreach (var meshRenderer in m_SpeederService.speederViews[m_CloneEntityId].meshPivot.GetComponentsInChildren<MeshRenderer>())
                    {
                        meshRenderer.material = m_Settings.cloneMaterial;
                    }
                }
                m_PropertiesComponentService.GetView<SpeederView>(model.entityId).gameObject.SetActive(true);
                // TODO : destroy the vfx
                ParticleSystem vfx = Object.Instantiate(m_Settings.cloneVFX);
                vfx.gameObject.transform.position = m_TransformComponentService.GetComponentModel(model.entityId).model.position;
                vfx.Play();
            }
        }

        private void OnSpeederStateAdded(SpeederStateComponentModel model)
        {
            if (model.model.state == SpeederState.Clone)
            {
                m_CloneEntityIdList.Add(model.entityId);
            }
        }

        private IEnumerator WaitCloneDie(uint entityId)
        {
            yield return new WaitForSeconds(m_Settings.duration);
            if (m_CoolDown)
            {
                KillClone(entityId);
            }
        }

        private void KillClone(uint entityId)
        {
            m_SpeederService.RemoveSpeeder(entityId);
            m_CloneComponentService.DeleteComponent(entityId);
            m_CloneEntityIdList.Remove(entityId);
            m_CoolDown = false;
        }

        private void OnMessageUpdate(MessageComponentModel model)
        {
            if (!m_CloneComponentService.TryGetComponentModel(m_CloneEntityId, out CloneComponentModel cloneModel))
                return;
            if (model.isMine && cloneModel.model.originEntityId == model.entityId && model.model.message == MessageModel.Message.Kill && m_CoolDown)
            {
                KillClone(cloneModel.entityId);
            }
        }

        private void OnCustomMessageBroadcast(CustomMessageBroadcast message)
        {
            bool isMine = message.ParticipantId == m_AukiWrapper.GetSession().ParticipantId;
            byte[] messageBody = message.Body;

            if ((CustomMessageId)messageBody[0] == CustomMessageId.SpeederEvent)
            {
                SpeederEventMessage model = new SpeederEventMessage(messageBody);
                if (!m_CloneComponentService.TryGetComponentModel(m_CloneEntityId, out CloneComponentModel cloneModel))
                    return;
                if (isMine && cloneModel.model.originEntityId == model.entityId && model.messageType == MessageModel.Message.Kill && m_CoolDown)
                {
                    KillClone(cloneModel.entityId);
                }
            }
        }

        public Vector3? GetCloneTarget()
        {
            // straight line
            return m_Direction * 20f;
        }

        public Vector3 GetCloneTarget(Vector3 position)
        {
            // mirror 
            float angle = Quaternion.FromToRotation(m_Direction, position - m_StartPos).eulerAngles.y;
            float distance = Vector3.Distance(m_StartPos, position);
            Vector3 targetRot = new Vector3(0, -angle, 0);
            Vector3 normalDir = Quaternion.Euler(targetRot) * m_Direction.normalized;
            return normalDir * distance + m_StartPos;
        }

        private bool IsClone(uint entityId)
        {
            return m_CloneEntityIdList.Contains(entityId);
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            MoveMyClone(deltaTime);
            
            if (!m_EquipmentComponentService.TryGetComponentModel(m_SpeederService.serverSpeederEntity,
                out EquipmentStateComponentModel equipmentState))
                return; // we are not in game or not inited


            if (equipmentState.model.state == EquipmentState.Clone && !m_CoolDown && m_SpeederHUDService.tapScreenInput)
            {
                CreateShadowClone();
            }
        }

        private void MoveMyClone(float deltaTime)
        {
            foreach (var cloneSimEntityId in m_CloneEntityIdList)
            {
                ISpeederSimulation cloneSimulation = m_SpeederService.GetSimulation(cloneSimEntityId);
                if (cloneSimulation == null)
                {
                    return;
                }

                if (!m_TransformComponentService.TryGetComponentModel(cloneSimEntityId, out var transform))
                    return; // speeder not finished yet due to race condition.

                var target = GetCloneTarget(m_RaycastService.hitPose.position);
                var isPlayer = cloneSimEntityId == m_CloneEntityId;
                
                Vector3? floorSnapTarget = isPlayer ? target : null;
                var (floorNormal, position) = SpeederHelper.GetFloorNormalAndPosition(cloneSimulation,
                    m_RaycastService, floorSnapTarget);

                if (!m_CloneComponentService.TryGetComponentModel(cloneSimEntityId,out CloneComponentModel cloneModel))
                {
                    return;
                }
                var crownKeeper = m_SpeederService.GetSimulation(cloneModel.model.originEntityId).crownKeeper;
                var brakeInput = false;

                var speederState = SpeederState.Clone;
                var input = false;

                var inputModel = SpeederMapper.ToInputModel
                (
                    transform.model,
                    target,
                    floorNormal,
                    position,
                    isPlayer,
                    brakeInput,
                    input,
                    speederState,
                    EquipmentState.Clone,
                    crownKeeper,
                    m_WorldScaleService.worldScale
                );
                cloneSimulation.Update(deltaTime, inputModel);
                var viewState = SpeederMapper.ToViewModel(cloneSimulation, EquipmentState.Clone);
                m_SpeederService.speederViews[cloneSimEntityId].UpdateView(viewState);

                if (cloneSimEntityId == m_CloneEntityId)
                {
                    m_TransformComponentService.UpdateComponent(transform,
                        new TransformModel(m_SpeederService.speederViews[m_CloneEntityId].transform.position,
                            m_SpeederService.speederViews[m_CloneEntityId].transform.rotation, cloneSimulation.speed));
                }
            }
        }
    }
}