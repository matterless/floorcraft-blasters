using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ProximityMineServer
    {
        private const float m_DistanceFromVehiclePivotToVehicleColliderOuterBounds = 0.05f;
        
        private readonly MessageComponentService m_MessageComponentService;
        private readonly SpeederHUDService m_SpeederHUDService;
        private readonly SpeederService m_SpeederService;
        private readonly ProximityMineService.Settings m_Settings;
        private readonly ProximityMineService m_ProximityMineService;
        private readonly ProximityMineView m_View;
        private readonly IAnalyticsService m_AnalyticsService;
        public ProximityMineView view => m_View; 
        public uint entityId => m_EntityId;
        private uint m_EntityId;
        private bool m_IsLocal;
        private string m_SessionId;
        
        public ProximityMineServer(
            uint entityId, 
            string sessionId,
            ProximityMineView view, 
            MessageComponentService messageComponentService,
            SpeederService speederService,
            ProximityMineService proximityMineService,
            ProximityMineService.Settings settings,
            IAnalyticsService analyticsService,
            bool isLocal
            )
        {
            m_View = view;
            m_EntityId = entityId;
            m_SessionId = sessionId;
            m_SpeederService = speederService;
            m_ProximityMineService = proximityMineService;
            m_Settings = settings;
            m_MessageComponentService = messageComponentService;
            m_IsLocal = isLocal;
            m_View.SetRadius(settings.radius);
            m_View.SetOutline(isLocal);
            m_View.collisionEvent.AddListener(OnCollisionEnter);
            m_View.gameObject.SetActive(true);
            m_AnalyticsService = analyticsService;
            UpdateCollisionIgnores();
        }
        public void Update()
        {
            UpdateCollisionIgnores();
        }
        public void Dispose()
        {
            m_View.ActivateExplosionSFX();
            m_View.collisionEvent.RemoveListener(OnCollisionEnter);
            Object.Destroy(m_View);
        }
        public void Explode()
        {   
            if (m_SpeederService.TryGetSpeeder(m_SpeederService.serverSpeederEntity, out ISpeederSimulation serverSpeeder) &&
                !serverSpeeder.state.HasFlag(SpeederState.Totaled) &&
                IsInExplosionRange(serverSpeeder.groundPosition, m_DistanceFromVehiclePivotToVehicleColliderOuterBounds))
            {
                // Explode my speeder since I am server and it is in range of explosion
                m_MessageComponentService.SendMessage(m_SpeederService.serverSpeederEntity, MessageModel.Message.Kill,m_EntityId);
            }
            var localIds = m_ProximityMineService.GetLocalIds();
            foreach (var localId in localIds)
            {
                if (m_ProximityMineService.TryGetProximityMine(localId, out ProximityMineServer proximityMine) && 
                    IsInExplosionRange(proximityMine.view.transform.position, m_Settings.radius))
                {
                    // Explode my mine since I am server and it is in range of explosion
                    m_MessageComponentService.SendMessage(proximityMine.entityId, MessageModel.Message.Kill,m_EntityId);
                }
            }
            Dispose();
        }
        void OnCollisionEnter(Collision collision)
        {
            GameObjectView view = collision.gameObject.GetComponent<GameObjectView>();
            m_MessageComponentService.SendMessage(view.entityId, MessageModel.Message.Kill,m_EntityId);
            m_MessageComponentService.SendMessage(m_EntityId, MessageModel.Message.Kill,view.entityId);
            m_AnalyticsService.PlayerVehicleExploded(m_SessionId,CarExplodeCause.ProximityMine);
        }
        
        // If we created the proximity mine (m_IsServer)
        // We don't want to collide since we cannot trigger it ourselves.
        //
        // or if is not a server speeder then we are not server and don't want to collide since
        // we will get kill message from the real server (remote speeder)
        // 
        // So, we only have the collision left that is your current speeder and enemy mines.
        void UpdateCollisionIgnores()
        {
            foreach (var kvp in m_SpeederService.speederViews)
            {
                if (m_IsLocal || kvp.Value.entityId != m_SpeederService.serverSpeederEntity)
                {
                    Physics.IgnoreCollision(m_View.sphereCollider, kvp.Value.boxCollider);
                }
            }
        }
        
        private bool IsInExplosionRange(Vector3 position, float extraRange) => 
            Vector3.Distance(m_View.transform.position,position) <= m_Settings.radius + extraRange;
    }
}