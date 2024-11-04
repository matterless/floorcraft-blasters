using System.Collections.Generic;
using Auki.ConjureKit;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ProximityMineService : ITickable
    {
        [System.Serializable]
        public class Settings : IEquipmentSetting
        {
            public IAsset proximityMine => m_ProximityMine;
            public float radius => m_Radius;
            public float cooldown => m_Cooldown;
            public float duration => m_Duration;
            public int quantity => m_Quantity;
            public bool infinite => m_Infinite;

            [SerializeField] private Asset m_ProximityMine;
            [SerializeField] private float m_Radius = 0.075f;
            [SerializeField] private float m_Cooldown = 3f;
            [SerializeField] private int m_Quantity = 10;
            [SerializeField] private float m_Duration = 0.1f;
            [SerializeField] private bool m_Infinite = false;
        }

        private readonly SpeederHUDService m_SpeederHUDService;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly SpeederService m_SpeederService;
        private readonly PropertiesComponentService m_PropertiesEcsService;
        private readonly TransformComponentService m_TransformService;
        private readonly MessageComponentService m_MessageComponentService;
        private readonly EquipmentService m_EquipmentService;
        private readonly Settings m_Settings;
        private readonly PropertiesECSService.Settings m_AssetSettings;
        private readonly List<ProximityMineServer> m_ProximityMines = new();
        private readonly IRaycastService m_RaycastService;
        private readonly List<uint> m_LocalEntityIds = new();
        private readonly IAnalyticsService m_AnalyticsService;

        public ProximityMineService(IAukiWrapper aukiWrapper,
            SpeederHUDService speederHUDService,
            PropertiesComponentService propertiesEcsService,
            TransformComponentService transformEcsService,
            MessageComponentService messageComponentService,
            EquipmentService equipmentService,
            SpeederService speederService,
            Settings settings,
            IRaycastService raycastService,
            PropertiesECSService.Settings assetSettings,
            IAnalyticsService analyticsService
        )
        {
            m_AnalyticsService = analyticsService;
            m_AssetSettings = assetSettings;
            m_SpeederService = speederService;
            m_SpeederHUDService = speederHUDService;
            m_RaycastService = raycastService;
            m_AukiWrapper = aukiWrapper;
            m_PropertiesEcsService = propertiesEcsService;
            m_TransformService = transformEcsService;
            m_MessageComponentService = messageComponentService;
            m_EquipmentService = equipmentService;
            m_Settings = settings;
            m_MessageComponentService.onComponentUpdated += OnMessageComponentUpdated;
            m_PropertiesEcsService.onComponentAdded += OnPropertiesComponentAdded;
            m_TransformService.onComponentAdded += OnTransformComponentAdded;
            m_AukiWrapper.onEntityDeleted += OnAukiEntityDeleted;
            m_AukiWrapper.onLeft += OnAukiLeft;
        }
        private void OnAukiLeft()
        {
            m_LocalEntityIds.Clear();
        }
        

        void OnMessageComponentUpdated(MessageComponentModel model)
        {
            if (model.model.message != MessageModel.Message.Kill)
                return;
            
            if (!TryGetProximityMine(model.entityId, out ProximityMineServer proximityMine))
                return;
            
            var isServer = m_LocalEntityIds.Contains(proximityMine.entityId);
            m_ProximityMines.Remove(proximityMine);
            var key = proximityMine.entityId;
            proximityMine.Explode();
            if (isServer)
            {
               
                m_PropertiesEcsService.DeleteComponent(key);
                m_AukiWrapper.DeleteEntity(proximityMine.entityId, () =>
                {
                    m_LocalEntityIds.Remove(key);
                });
            }
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var proximityMine in m_ProximityMines)
            {
                proximityMine.Update();
            }
            
            if (!m_EquipmentService.TryGetComponentModel(m_SpeederHUDService.serverEntityId, out var equipmentComponent))
                return;

            if (equipmentComponent.model.state == EquipmentState.ProximityMines &&
                m_SpeederHUDService.tapScreenInput
            )
            {
                CreateProximityMine();
            }
        }

        void CreateProximityMine()
        {
            if (m_LocalEntityIds.Count > 2)
            {  
                //Too many mines deployed, blow up the oldest one.
                m_MessageComponentService.SendMessage(m_LocalEntityIds[0], MessageModel.Message.Kill,m_LocalEntityIds[0]);
            } 
            var position = m_SpeederService.serverSpeeder.groundPosition;
            var rotation = m_SpeederService.serverSpeeder.rotation;
            m_AukiWrapper.AddEntity(new Pose(position, rotation), false, AddMineSuccessCallback, AddMineFailCallback);
        }
        private void AddMineFailCallback(string obj)
        {
            Debug.LogError("Error creating proximity mine"); 
        }
        private void AddMineSuccessCallback(Entity entity)
        {
            var position = m_SpeederService.serverSpeeder.groundPosition;
            var rotation = m_SpeederService.serverSpeeder.rotation;
            var offset = rotation * Vector3.forward * -0.1f;
            position += offset;
            m_LocalEntityIds.Add(entity.Id);
            m_PropertiesEcsService.AddComponent(entity.Id, new PropertiesModel(m_Settings.proximityMine.id));
            m_TransformService.AddComponent(entity.Id, new TransformModel(position, rotation, 0f));
            m_MessageComponentService.AddComponent(entity.Id,new MessageModel(MessageModel.Message.None, entity.Id));
        }

        private void OnPropertiesComponentAdded(PropertiesComponentModel model)
        {
            var asset = m_AssetSettings.GetAsset(model.model.id);

            if (asset.assetType != AssetType.ProximityMine)
                return;

            var proximityMineView = m_PropertiesEcsService
                .GetGameObject(model.entityId)
                .GetComponent<ProximityMineView>();
            
            var proximityMine = new ProximityMineServer(
                model.entityId, 
                m_AukiWrapper.GetSession().Id,
                proximityMineView, 
                m_MessageComponentService, 
                m_SpeederService,
                this,
                m_Settings,
                m_AnalyticsService,
                m_LocalEntityIds.Contains(model.entityId));
            m_ProximityMines.Add(proximityMine);
        }

        private void OnTransformComponentAdded(TransformComponentModel model)
        {
            if (!TryGetProximityMine(model.entityId, out ProximityMineServer proximityMine))
                return;

            // add the floor raycast to set the correct position
            if (m_RaycastService.FloorRaycast(model.model.position, out Vector3 position, out _))
            {
                proximityMine.view.transform.SetPositionAndRotation(position + Vector3.up * 0.002f,
                    model.model.rotation);
            }
            else
            {
                proximityMine.view.transform.SetPositionAndRotation(model.model.position, model.model.rotation);
            }
        }

        // Only happens on remote clients
        private void OnAukiEntityDeleted(uint entityId)
        {
            if (!TryGetProximityMine(entityId, out ProximityMineServer proximityMine))
                return;

            m_ProximityMines.Remove(proximityMine);
            proximityMine.Dispose();
        }

        public bool TryGetProximityMine(uint entityId, out ProximityMineServer returnValue)
        {
            foreach (var proximityMine in m_ProximityMines)
            {
                if (proximityMine.entityId == entityId)
                {
                    returnValue = proximityMine;
                    return true;
                }
            }

            returnValue = null;
            return false;
        }
        public List<uint> GetLocalIds()
        {
            return m_LocalEntityIds;
        }
    }
}