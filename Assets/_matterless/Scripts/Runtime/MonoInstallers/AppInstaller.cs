using Matterless.Inject;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Matterless.Floorcraft
{
    public class AppInstaller : MonoInstaller
    {
        [SerializeField] private NotificationSettings m_NotificationSettings;
        protected override void InstallBindings()
        {
            // get arguments
            var appConfig = arguments[0] as AppConfigs;
          
            container.BindInstance(appConfig.domainSettings);
            container.BindInstance(appConfig.heartbeatSettings);
            container.BindInstance(appConfig.raycastSettings);
            container.BindInstance(appConfig.vehicleSelectorSettings);
            container.BindInstance(appConfig.placeableSelectorSettings);
            container.BindInstance(appConfig.obstacleSettings);
            container.BindInstance(appConfig.crownSettings);
            container.BindInstance(appConfig.proximityMineSettings);
            container.BindInstance(appConfig.scoreSetting);
            container.BindInstance(appConfig.speederSimluationSettings);
            container.BindInstance(appConfig.dashSettings);
            container.BindInstance(appConfig.honkSettings);
            container.BindInstance(appConfig.wreckingBallMagnetSettings);
            container.BindInstance(appConfig.flamethrowerSettings);
            container.BindInstance(appConfig.laserServiceSettings);
            container.BindInstance(appConfig.equipmentSettings);
            container.BindInstance(appConfig.respawnSettings);
            container.BindInstance(appConfig.shadowCloneSettings);
            container.BindInstance(appConfig.powerUpSpawnPointSettings);
            container.BindInstance(appConfig.menuSettings);
            container.BindInstance(appConfig.leaderboardSettings);
            container.BindInstance(appConfig.nameTagSettings);
            container.BindInstance(appConfig.npcEnemyServiceSettings);
            container.BindInstance(appConfig.mayhemModeSettings);
            container.BindInstance(appConfig.worldScaleSettings);
            
            container.Bind<WorldScaleService>();
            container.Bind<PropertiesECSService.Settings>();
            container.Bind<IConnectionService, ConnectionService>();
            container.Bind<IRaycastService, RaycastService>();
            container.Bind<SpeederHUDService>();
            container.Bind<SpeederService>();
            container.Bind<WreckingBallMagnetService>();
            container.Bind<FlameThrowerService>();
            container.Bind<LaserService>();
            container.Bind<LaserBeamService>();
            container.Bind<ObstacleService>();
            container.Bind<SettingMenuService>();
            //container.Bind<SpawnLocations>();
            
            container.Bind<ProximityMineService>();
            container.Bind<MarkerTypeService>();
            container.Bind<IMarkerService, MarkerService>();
            // ECS
            container.Bind<IComponentModelFactory, ComponentModelFactory>();
            container.Bind<IECSController, ECSController>();
            // Components
            container.Bind<PropertiesComponentService>();
            container.Bind<SpeederStateComponentService>();
            container.Bind<TransformComponentService>();
            container.Bind<MessageComponentService>();
            container.Bind<ScoreComponentService>();
            container.Bind<NameComponentService>();
            container.Bind<NameTagService>();
            container.Bind<EquipmentService>();
            container.Bind<PowerUpSpawnPointService>();
            container.Bind<CrownService>();
            container.Bind<CooldownService>();
            container.Bind<RespawnService>();
            container.Bind<INotificationService, NotificationService>(m_NotificationSettings);
            container.Bind<HonkService>();
            container.Bind<ShadowCloneService>();
            container.Bind<CloneComponentService>();
            container.Bind<LeaderboardService>();

            
            container.Bind<IHapticService, HapticService>();
            
            container.Bind<InAppPurchaseService>();
            container.Bind<IStoreService, StoreService>();


            // Mayhem Mode
            container.Bind<EnemyStateComponentService>();
            container.Bind<MayhemUiService>();
            container.Bind<MayhemModeService>();
            container.Bind<NPCEnemyService>();
            container.Bind<MayhemObstacleComponentService>();
            container.Bind<SpawnLocationsComponentService>();
            container.Bind<SpawnLocationsService>();
            container.Bind<MayhemEnemiesStatusComponentService>();
            container.Bind<IGameOverUiService, GameOverUiService>();

            // Domain Services
            container.Bind<IDomainService, DomainService>();
            container.Bind<IHeartbeatService, HeartbeatService>();
            container.Bind<DomainAssetPlacementService>();
        }
    }
}