using System;
using System.Collections.Generic;
using System.Text;
using Auki.ConjureKit.Hagall.Messages;
using Matterless.Audio;
using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public static class UnityGameObjectTag
    {
        public const string Speeder = "Speeder";
        public const string Obstacle = "Obstacle";
        public const string Laser = "Laser";
        public const string Crate = "Crate";
        public const string Enemy = "Enemy";
    }

    public class SpeederService : ITickable
    {
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IRaycastService m_RaycastService;
        private readonly MarkerTypeService m_MarkerTypeService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly AudioUiService m_AudioUiService;
        private readonly SpeederHUDService m_SpeederHUDService;
        private readonly IHapticService m_HapticService;

        private readonly Dictionary<uint, SpeederView> m_SpeederViews = new();
        private readonly List<ISpeederSimulation> m_SimulationList = new();
        private readonly Dictionary<uint, ISpeederSimulation> m_Simulations = new();
        private readonly Dictionary<uint, Vehicle> m_Vehicles = new();

        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly PropertiesComponentService m_PropertiesComponentService;
        private readonly SpeederStateComponentService m_SpeederStateComponentService;
        private readonly TransformComponentService m_TransformComponentService;
        private readonly ScoreComponentService m_ScoreComponentService;
        private readonly NameComponentService m_NameComponentService;
        private readonly EquipmentService m_EquipmentService;
        private readonly CooldownService m_CooldownService;
        private readonly CrownService m_CrownService;
        private readonly NameTagService m_NameTagService;

        private readonly NotificationService m_NotificationService;
        private readonly LaserBeamService m_LaserService;

        private readonly VehicleSelectorService.Settings m_Setting;
        private readonly PropertiesECSService.Settings m_AssetSettings;
        private readonly SpeederSimulation.Settings m_SimulationSettings;
        private readonly DashSettings m_DashSettings;
        private readonly AudioService m_AudioService;
        private readonly WorldScaleService m_WorldScaleService;
        private readonly FlameThrowerService.Settings m_FlamethrowerSettings;
        private uint m_MySpeederEntity;
        private uint m_EmptySpeederEntity = 99999;
        private bool m_CollisionFlag;

        private readonly List<SpeederState> setStatesToCheck = new()
        {
            SpeederState.LaserCharge, SpeederState.LaserFire, SpeederState.Loading, SpeederState.Boosting, SpeederState.Braking,
            SpeederState.OverHeat, SpeederState.Totaled
        };

        private readonly CrownService.Settings m_CrownSettings;
        private readonly WreckingBallMagnetService.Settings m_WreckingBallMagnetSettings;
        private readonly LaserService.Settings m_LaserSettings;
        private ISpeederSimulation mySimulation => m_Simulations[m_MySpeederEntity];

        public ISpeederSimulation GetSimulation(uint entityId)
        {
            if (m_Simulations.ContainsKey(entityId))
            {
                return m_Simulations[entityId];
            }
            else
            {
                return null;
            }
        }

        public Dictionary<uint, SpeederView> speederViews => m_SpeederViews;
        public uint serverSpeederEntity => m_MySpeederEntity;
        public ISpeederSimulation serverSpeeder => mySimulation;
        public Vehicle GetVehicle(uint entityId) => m_Vehicles[entityId];
        public Action onRespawn;
        public Action onSpawn;

        // When your speeder is killed
        public Action onKill;
        private Vehicle m_SelectedVehicle;

        public SpeederService(IAukiWrapper aukiWrapper,
            IRaycastService raycastService,
            MarkerTypeService markerTypeService,
            IAnalyticsService analyticsService,
            ICoroutineRunner coroutineRunner,
            AudioUiService audioUiService,
            SpeederHUDService speederHUDService,
            PropertiesComponentService propertiesEcsService,
            SpeederStateComponentService speederStateComponentService,
            TransformComponentService transformEcsService,
            ScoreComponentService scoreComponentService,
            NameComponentService nameComponentService,
            EquipmentService equipmentService,
            CooldownService cooldownService,
            CrownService crownService,
            IHapticService hapticService,
            NotificationService notificationService,
            LaserBeamService laserService,
            VehicleSelectorService.Settings setting,
            PropertiesECSService.Settings assetSettings,
            SpeederSimulation.Settings simulationSettings,
            CrownService.Settings crownSettings,
            WreckingBallMagnetService.Settings wreckingBallMagnetSettings,
            LaserService.Settings laserSettings,
            FlameThrowerService.Settings flamethrowerSettings,
            DashSettings dashSettings,
            AudioService audioService,
            WorldScaleService worldScaleService
        )
        {
            m_Setting = setting;
            m_AssetSettings = assetSettings;
            m_SimulationSettings = simulationSettings;
            m_DashSettings = dashSettings;
            m_AudioService = audioService;
            m_WorldScaleService = worldScaleService;
            m_AukiWrapper = aukiWrapper;
            m_RaycastService = raycastService;
            m_MarkerTypeService = markerTypeService;
            m_AnalyticsService = analyticsService;
            m_AudioUiService = audioUiService;
            m_SpeederHUDService = speederHUDService;
            m_NotificationService = notificationService;
            m_LaserService = laserService;
            m_CoroutineRunner = coroutineRunner;
            //m_SpeederHUDService.onRespawnButtonClicked += Respawn;
            m_PropertiesComponentService = propertiesEcsService;
            m_SpeederStateComponentService = speederStateComponentService;
            m_TransformComponentService = transformEcsService;
            m_ScoreComponentService = scoreComponentService;
            m_NameComponentService = nameComponentService;
            m_EquipmentService = equipmentService;
            m_CooldownService = cooldownService;
            m_CrownService = crownService;
            m_HapticService = hapticService;
            m_FlamethrowerSettings = flamethrowerSettings;
            m_LaserSettings = laserSettings;
            m_WreckingBallMagnetSettings = wreckingBallMagnetSettings;
            m_PropertiesComponentService.onComponentAdded += OnPropertyComponentAdded;
            m_TransformComponentService.onComponentAdded += OnTransformComponentAdded;
            m_TransformComponentService.onComponentUpdated += OnTransformComponentUpdated;
            m_PropertiesComponentService.onComponentUpdated += OnPropertyComponentUpdated;
            m_AukiWrapper.onCustomMessageBroadcast += OnCustomMessageBroadcast;
           
            m_CrownSettings = crownSettings;

            m_AukiWrapper.onEntityDeleted += OnSpeederDelete;
        }

        private bool IsPlayer(uint entityId) =>
            entityId == m_MySpeederEntity;

        public void CreateSpeeder(Vehicle vehicle)
        {
            Pose spawnPose = m_RaycastService.hitPose;

            m_AukiWrapper.AddEntity(spawnPose, false, entity =>
                {
                    // cache my entity id
                    m_MySpeederEntity = entity.Id;
                    m_SpeederHUDService.serverEntityId = entity.Id;
                    // add components
                    m_SpeederStateComponentService.AddComponent(entity.Id, new SpeederStateModel(SpeederState.Loading));
                    m_PropertiesComponentService.AddComponent(entity.Id, new PropertiesModel(vehicle.id));
                    m_TransformComponentService.AddComponent(entity.Id,
                        new TransformModel(spawnPose.position, spawnPose.rotation, 0));
                    m_ScoreComponentService.AddComponent(entity.Id, new ScoreModel(0));
                    m_EquipmentService.AddComponent(entity.Id, new EquipmentStateModel(EquipmentState.None));
                    m_EquipmentService.SetEquipAfterAbilityCommand(entity.Id, EquipmentState.Laser);
                    m_NameComponentService.AddComponent(entity.Id,
                        new NameModel(m_NameComponentService.GetRandomNameTagIndex()));
                    //m_CooldownService.SetCooldownSettings(m_LaserSettings);
                    // set market to arrow
                    
                    m_AnalyticsService.SpawnVehicle(m_AukiWrapper.GetSession().Id, m_RaycastService.distance,
                        ((AssetId) Enum.ToObject(typeof(AssetId), vehicle.id)).ToString());
                    m_CollisionFlag = false;
                    onSpawn?.Invoke();
                },
                error => { Debug.LogError("Error!!"); });
        }

        public void RemoveSpeeder()
        {
            RemoveSpeeder(m_MySpeederEntity);
        }

        public void RemoveSpeeder(uint speederEntityId)
        {
            // remove components
            m_SpeederStateComponentService.DeleteComponent(speederEntityId);
            m_PropertiesComponentService.DeleteComponent(speederEntityId);
            m_TransformComponentService.DeleteComponent(speederEntityId);
            m_ScoreComponentService.DeleteComponent(speederEntityId);
            m_EquipmentService.DeleteComponent(speederEntityId);
            // deleting the entity didn't invoke the delete property events
            // TODO:: ask AUKI about it

            Debug.Log("speeder attempt delete " + speederEntityId);
            m_AukiWrapper.DeleteEntity(speederEntityId,
                () => Debug.Log($"Obstacle entity deleted {speederEntityId}"));
            OnSpeederDelete(speederEntityId);
        }

        private void OnPropertyComponentAdded(PropertiesComponentModel model)
        {
            Debug.Log($"****** OnPropertyComponentAdded {model.entityId} isMine:{model.isMine}");

            if (m_AssetSettings.GetAsset(model.model.id).assetType != AssetType.Vehicle)
                return;

            // get and cache vehicle asset
            var vehicleAsset = m_Setting.GetAsset(model.model.id);
            m_Vehicles.Add(model.entityId, vehicleAsset);

            // subscribe to collision detection
            if (model.isMine)
            {
                //Debug.Log($"<color=green>onCollisionEntered subscribed</color>");
                m_PropertiesComponentService.GetView<SpeederView>(model.entityId).onCollisionEntered +=
                    OnCollisionEntered;
                
                m_PropertiesComponentService.GetView<SpeederView>(model.entityId).onTriggerEntered +=
                    OnTriggerEntered;
            }

            // if we add them here we are going to invoke them both on AukiWrapper::OnComponentAdd && AukiWrapper:OnComponentAdd
            // add transform component
            //Pose p = m_RaycastService.hitPose;
            //m_TransformEcsService.AddComponent(model.entityId, new TransformModel(p.position, p.rotation, 0));

            //// add message component
            //m_MessageComponentService.AddComponent(model.entityId, new MessageModel(MessageModel.Message.None, model.entityId));
        }


        private void OnTransformComponentAdded(TransformComponentModel model)
        {
            Debug.Log($"****** OnTransformComponentAdded {model.entityId}");

            // this is checking if this is a speeder
            if (!m_Vehicles.ContainsKey(model.entityId))
                return;

            var vehicle = m_Vehicles[model.entityId];

            // get speeder state
            // var speederStateModel = m_SpeederStateComponentService.GetComponentModel(model.entityId);

            // play spawn sfx
            m_AudioUiService.PlayCarSpawnSound();

            Pose initPose;
            // add the floor raycast to set the correct position first
            if (m_RaycastService.FloorRaycast(model.model.position, out Vector3 position, out Vector3 _))
            {
                initPose = new Pose(position + Vector3.up * 0.002f, model.model.rotation);
            }
            else
            {
                initPose = new Pose(model.model.position, model.model.rotation);
            }

            // new simulation 
            var simulation = new SpeederSimulation(
                model.entityId,
                m_SimulationSettings,
                m_DashSettings,
                vehicle,
                model.isMine
            );

            Debug.Log($"Add simulation {model.entityId}");
            m_Simulations.Add(model.entityId, simulation);
            m_SimulationList.Add(simulation);

            if (model.isMine)
            {
                if (m_SpeederStateComponentService.GetComponentModel(simulation.entityId).model.state ==
                    SpeederState.Clone)
                {
                    uint cloneId = m_PropertiesComponentService.GetGameObject(model.entityId)
                        .GetComponent<ShadowCloneGameObject>().originEntityId;
                    var inputModel =
                        SpeederMapper.ToInputModel(model.model.position, model.model.rotation,
                            m_Simulations[cloneId].speed);
                    simulation.Init(inputModel, 5f);
                }
                else
                {
                    // init player simulation
                    var inputModel = SpeederMapper.ToInputModel(m_RaycastService.hitPose);
                    simulation.Init(inputModel);
                    m_SpeederStateComponentService.UnsetState(simulation.entityId, SpeederState.Loading);
                }
            }
            else
            {
                // init other simulation
                var inputModel =
                    SpeederMapper.ToInputModel(model.model.position, model.model.rotation, model.model.speed);
                simulation.Init(inputModel, 0f);
            }

            var viewModel = SpeederMapper.ToViewModel(simulation, EquipmentState.None);
            // get speeder view
            var speederView = m_PropertiesComponentService.GetGameObject(model.entityId)
                .GetComponent<SpeederView>()
                .Init
                (
                    m_AukiWrapper.arCamera,
                    vehicle,
                    m_SimulationSettings,
                    m_CrownSettings,
                    m_WreckingBallMagnetSettings,
                    m_FlamethrowerSettings,
                    m_LaserService,
                    m_LaserSettings,
                    viewModel,
                    m_AudioService,
                    m_SpeederStateComponentService,
                    m_WorldScaleService
                );

            m_SpeederViews.Add(model.entityId, speederView);
            m_SpeederViews[model.entityId].transform
                .SetPositionAndRotation(initPose.position, initPose.rotation);

            // init HUD
            if (model.isMine)
            {
                m_SpeederHUDService.Respawn();
                m_SpeederHUDService.Show();
            }
        }

        // other
        private void OnTransformComponentUpdated(TransformComponentModel model)
        {
            if (model.isMine)
                return;

            //Debug.Log($"****** UpdateTransformEcsCallback {model.entityId}");
#if UNITY_EDITOR
            Debug.DrawLine(model.model.position, model.model.position + Vector3.up, Color.green, 5f);
#endif
            var position = model.model.position;
            var rotation = model.model.rotation;
            var speed = model.model.speed;

            if (m_RaycastService.FloorRaycast(position, out Vector3 snappedGroundPosition, out _))
            {
                position = snappedGroundPosition;
            }

            var correctedModel = new TransformModel(position, rotation, speed);
            model.model = correctedModel;
        }

        private void OnTriggerEntered(SpeederView view, Collider collider)
        {
            GameObjectView otherView = collider.gameObject.GetComponent<GameObjectView>();
            if (otherView == null)
            {
                //Debug.LogWarning("collision don't have a view");
                return;
            }

            if (otherView.CompareTag(UnityGameObjectTag.Obstacle))
            {
                CollideWithObstacle(view.entityId, otherView.entityId);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Obstacle);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Laser))
            {
                if (otherView.entityId == this.m_MySpeederEntity)
                    return;

                CollideWithLaser(view.entityId, otherView.entityId);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Laser);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Enemy))
            {
                CollideWithEnemy(view.entityId, otherView.entityId, otherView);
            }
        }

        private void OnPropertyComponentUpdated(PropertiesComponentModel model)
        {
            Debug.Log($"****** OnPropertyComponentUpdated {model.entityId} isMine:{model.isMine}");

            if (m_AssetSettings.GetAsset(model.model.id).assetType != AssetType.Vehicle)
                return;

            // get and cache vehicle asset
            var vehicleAsset = m_Setting.GetAsset(model.model.id);
            m_Vehicles[model.entityId] = vehicleAsset;
            
            if (m_Vehicles.ContainsKey(model.entityId))
            {
                speederViews[model.entityId].SetVehicle(m_Vehicles[model.entityId]);
            }
        }

        private void OnCollisionEntered(SpeederView view, Collision collision)
        {
            GameObjectView otherView = collision.gameObject.GetComponent<GameObjectView>();

            if (otherView == null)
            {
                //Debug.LogWarning("collision don't have a view");
                return;
            }

            Debug.Log($"<color=green>OnCollisionEntered {view.entityId} -> {otherView.entityId}</color>");

            if (otherView.CompareTag(UnityGameObjectTag.Obstacle))
            {
                // only obstacle having multiple collider
                if (m_CollisionFlag)
                    return;

                m_CollisionFlag = true;
                CollideWithObstacle(view.entityId, otherView.entityId);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Obstacle);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Laser))
            {
                CollideWithLaser(view.entityId, otherView.entityId);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Laser);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Speeder))
            {
                CollideWithOtherVehicle(view, (SpeederView)otherView, collision);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Crate))
            {
                CollideWithCrate(view, otherView, collision);
            }
            else if (otherView.CompareTag(UnityGameObjectTag.Enemy))
            {
                CollideWithEnemy(view.entityId, otherView.entityId, otherView);
            }
        }

        private void CollideWithEnemy(uint hitEntityId, uint hitByEntityId, GameObjectView otherView)
        {
            NPCEnemyView enemyView = otherView.gameObject.GetComponent<NPCEnemyView>();

            if (enemyView == null)
            {
                return;
            }

            if (enemyView.isShieldOn)
            {
                SendSpeederEventMessage(MessageModel.Message.Kill, m_MySpeederEntity, hitByEntityId);
                KillMineLocalSpeeder();
            }
        }

        private void DeductPointByCollide(uint entityId, uint obstacleEntityId)
        {
            uint obstacleOwnerParticipantId = m_AukiWrapper.GetEntity(obstacleEntityId).ParticipantId;
            uint speederEntityId = GetOwnedSpeederEntityId(obstacleOwnerParticipantId);

            if (speederEntityId != m_MySpeederEntity && speederEntityId != 0)
            {
                int otherScore = m_ScoreComponentService.GetScoreComponentModel(speederEntityId).model.score +
                                 m_ScoreComponentService.settings.obstacleKill;
                m_ScoreComponentService.UpdateComponent(speederEntityId, new ScoreModel(otherScore));
            }

            int myScore = Mathf.FloorToInt(m_ScoreComponentService.GetScoreComponentModel(entityId).model.score *
                                           (1 - m_ScoreComponentService.settings.killedDeductRate));
            m_ScoreComponentService.UpdateComponent(entityId, new ScoreModel(myScore));
            m_CollisionFlag = false;
        }
        private void DeductPointByLaser(uint entityId, uint obstacleEntityId)
        {
            uint obstacleOwnerParticipantId = m_AukiWrapper.GetSession().GetEntity(obstacleEntityId).ParticipantId;
            uint speederEntityId = GetOwnedSpeederEntityId(obstacleOwnerParticipantId);

            if (speederEntityId != m_MySpeederEntity)
            {
                int otherScore = m_ScoreComponentService.GetScoreComponentModel(speederEntityId).model.score +
                                 m_ScoreComponentService.settings.laserKill;
                m_ScoreComponentService.UpdateComponent(speederEntityId, new ScoreModel(otherScore));
            }

            int myScore = Mathf.FloorToInt(m_ScoreComponentService.GetScoreComponentModel(entityId).model.score *
                                           (1 - m_ScoreComponentService.settings.killedDeductRate));
            m_ScoreComponentService.UpdateComponent(entityId, new ScoreModel(myScore));
        }

        private void DeductPointBySpeeder(uint entityId, uint otherEntityId)
        {
            int otherScore = m_ScoreComponentService.GetScoreComponentModel(otherEntityId).model.score +
                             m_ScoreComponentService.settings.speederKill;
            m_ScoreComponentService.UpdateComponent(otherEntityId, new ScoreModel(otherScore));

            int myScore = Mathf.FloorToInt(m_ScoreComponentService.GetScoreComponentModel(entityId).model.score *
                                           (1 - m_ScoreComponentService.settings.killedDeductRate));
            m_ScoreComponentService.UpdateComponent(entityId, new ScoreModel(myScore));
        }

        private void DeductPointByDespawn(uint entityId)
        {
            int myScore = Mathf.FloorToInt(m_ScoreComponentService.GetScoreComponentModel(entityId).model.score *
                                           (1 - m_ScoreComponentService.settings.killedDeductRate));
            m_ScoreComponentService.UpdateComponent(entityId, new ScoreModel(myScore));
        }

        private uint GetOwnedSpeederEntityId(uint pid)
        {
            foreach (var view in m_SpeederViews)
            {
                uint participantId = m_AukiWrapper.GetEntity(view.Key).ParticipantId;
                if (pid == participantId)
                {
                    return view.Key;
                }
            }

            // This might happen because other player haven't (never) spawned yet but their obstacle spawned, so we don't have their speeder entity id yet in the pool
            Debug.LogError("Can't find EntityId by ParticipantId " + pid);
            return 0;
        }

        private void CollideWithCrate(SpeederView myView, GameObjectView otherView, Collision collision)
        {
            otherView.gameObject.SetActive(false);
            m_CooldownService.ReduceCooldown(2f);
        }

        private void CollideWithOtherVehicle(SpeederView myView, SpeederView otherView, Collision collision)
        {
            var myEntityId = myView.entityId;
            var otherEntityId = otherView.entityId;

            var otherSimulation = m_Simulations[otherEntityId];

            if (mySimulation.age < 1 || otherSimulation.age < 1)
                return;

            if (otherSimulation.state.HasFlag(SpeederState.Totaled))
                return;

            if (m_SpeederStateComponentService.GetComponentModel(myView.entityId).model.state == SpeederState.Clone)
                return;

            if (m_SpeederStateComponentService.GetComponentModel(otherView.entityId).model.state == SpeederState.Clone)
            {
                uint otherCloneFrom = otherView.GetComponent<ShadowCloneGameObject>().originEntityId;
                if (otherCloneFrom == myEntityId)
                {
                    return;
                }

                SendSpeederEventMessage(MessageModel.Message.Kill, m_MySpeederEntity, otherCloneFrom);
                DeductPointBySpeeder(myEntityId, otherCloneFrom);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Player);
                KillMineLocalSpeeder();
                return;
            }


            var attackVector = Vector3.ProjectOnPlane(mySimulation.groundPosition - otherSimulation.groundPosition,
                mySimulation.floorNormal).normalized;
            var angleOfAttack = Vector3.Angle(otherView.transform.forward, attackVector);
            var hitDirection = Vector3.ProjectOnPlane(otherSimulation.groundPosition - mySimulation.groundPosition,
                mySimulation.floorNormal).normalized;
            var hitAngle = Vector3.Angle(myView.transform.forward, hitDirection);

            Debug.Log(
                $"CollideWithOtherVehicle hitAngle:{hitAngle}, my speed:{mySimulation.speed}, other speed:{otherSimulation.speed}");
            Debug.Log(
                $"CollideWithOtherVehicle hitAngle:{hitAngle}, my speed:{mySimulation.boosting}, other speed:{otherSimulation.boosting}");

            // other speeder late boost then me, and other is boosting
            // if local boosting is less that the one hitting me, and the one hitting me is boosting
            // Then I die.
            if (mySimulation.state.HasFlag(SpeederState.Boosting) &&
                otherSimulation.state.HasFlag(SpeederState.Boosting))
            {
                if (mySimulation.boosting < otherSimulation.boosting) //Both boosting and my boost is lower, I die
                {
                    SendSpeederEventMessage(MessageModel.Message.Kill, myEntityId, otherEntityId);
                    DeductPointBySpeeder(myEntityId, otherEntityId);
                    m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Player);
                    m_AnalyticsService.PlayerCauseAnotherPlayerExplode(m_AukiWrapper.GetSession().Id);
                    KillMineLocalSpeeder();
                    return;
                }
            }

            // I am not boosting, they are boosting and there was collision, I die.
            if (!mySimulation.state.HasFlag(SpeederState.Boosting) &&
                otherSimulation.state.HasFlag(SpeederState.Boosting))
            {
                SendSpeederEventMessage(MessageModel.Message.Kill, myEntityId, otherEntityId);
                DeductPointBySpeeder(myEntityId, otherEntityId);
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Player);
                KillMineLocalSpeeder();
                return;
            }

            if (hitAngle < 40 && angleOfAttack < 40 &&
                otherSimulation.speed > 0) //Frontal collision and they are moving, I die
            {
                // send destroy message
                SendSpeederEventMessage(MessageModel.Message.Kill, myEntityId, otherEntityId);
                // send score 
                DeductPointBySpeeder(myEntityId, otherEntityId);
                // analytics
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Player);
                m_AnalyticsService.PlayerCauseAnotherPlayerExplode(m_AukiWrapper.GetSession().Id);
                KillMineLocalSpeeder();
                return;
            }

            if (hitAngle > 40 && angleOfAttack < 40) // My hit angle is more than 40, I am getting hit, I die
            {
                // send destroy message
                SendSpeederEventMessage(MessageModel.Message.Kill, myEntityId, otherEntityId);
                // send score 
                DeductPointBySpeeder(myEntityId, otherEntityId);
                // analytics
                m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Player);
                m_AnalyticsService.PlayerCauseAnotherPlayerExplode(m_AukiWrapper.GetSession().Id);
                KillMineLocalSpeeder();
                return;
            }

            // TODO:: add this analytic message
            // m_AnalyticsService.PlayerCauseAnotherPlayerExplode(m_AukiWrapper.GetSession().Id);
        }

        private void CollideWithObstacle(uint hitEntityId, uint hitByEntityId)
        {
            if (m_Simulations[hitEntityId].boosting > 0)
            {
                return;
            }

            // TODO : move to service
            // if (m_Simulations[hitEntityId].state == SpeederState.Clone && m_MySpeederEntity == m_ShadowCloneService.IsCloneFrom(hitEntityId))
            // {
            //     m_MessageComponentService.SendMessage(hitEntityId, MessageModel.Message.Kill, hitByEntityId);
            //     return;
            // }
            // analytics
            // send destroy message
            SendSpeederEventMessage(MessageModel.Message.Kill, m_MySpeederEntity, hitByEntityId);

            // check if obstacle is a static object without an auki entity and an owner
            if (!m_AukiWrapper.HasEntity(hitByEntityId))
                return;

            // send point data
            DeductPointByCollide(hitEntityId, hitByEntityId);
            KillMineLocalSpeeder();
        }
        private void CollideWithLaser(uint hitEntityId, uint hitByEntityId)
        {
            if (m_Simulations[hitEntityId].boosting > 0)
            {
                return;
            }

            // TODO : move to service
            // if (m_Simulations[hitEntityId].state == SpeederState.Clone && m_MySpeederEntity == m_ShadowCloneService.IsCloneFrom(hitEntityId))
            // {
            //     m_MessageComponentService.SendMessage(hitEntityId, MessageModel.Message.Kill, hitByEntityId);
            //     return;
            // }
            // analytics
            // send destroy message
            SendSpeederEventMessage(MessageModel.Message.Kill, m_MySpeederEntity, hitByEntityId);

            // send point data
            DeductPointByLaser(hitEntityId, hitByEntityId);
            KillMineLocalSpeeder();
        }

        private void KillMineLocalSpeeder()
        {
            m_SpeederHUDService.Hide();
            m_SpeederHUDService.Totaled();
            m_HapticService.PlayHeavyHapticsTwice();
            onKill?.Invoke();
            m_NotificationService.Destroyed();
            m_MarkerTypeService.SetType(MarkerService.MarkerType.Tip);
            m_SpeederViews[m_MySpeederEntity].Kill();
            m_SpeederStateComponentService.SetState(m_MySpeederEntity, SpeederState.Totaled);
            //RemoveSpeeder(m_MySpeederEntity);
        }

        private void DespawnLocalSpeeder()
        {
            m_SpeederHUDService.Totaled();
            m_SpeederStateComponentService.SetState(m_MySpeederEntity, SpeederState.Totaled);
            DeductPointByDespawn(m_MySpeederEntity);
            m_SpeederViews[m_MySpeederEntity].Kill();
            //RemoveSpeeder(m_MySpeederEntity);
        }

        private void RespawnLocalSpeeder(uint entityId)
        {
            var transform = m_TransformComponentService.GetComponentModel(entityId).model;
            var prop = m_PropertiesComponentService.GetComponentModel(entityId).model;
            var inputModel = SpeederMapper.ToInputModel(transform.position, transform.rotation);
            m_Simulations[entityId].Init(inputModel);
            m_SpeederViews[entityId].transform
                .SetPositionAndRotation(inputModel.position, inputModel.rotation);
            // unset state
            m_SpeederStateComponentService.UnsetState(entityId, SpeederState.Totaled);
            // play sfx
            m_AudioUiService.PlayCarSpawnSound();
            m_CrownService.OnCrownKeeperRespawn(entityId);
            m_CollisionFlag = false;
            m_AnalyticsService.SpawnVehicle(m_AukiWrapper.GetSession().Id, m_RaycastService.distance,
                ((AssetId)Enum.ToObject(typeof(AssetId), prop.id)).ToString());
        }

        private void OnCustomMessageBroadcast(CustomMessageBroadcast message)
        {
            byte[] messageBody = message.Body;

            if ((CustomMessageId)messageBody[0] == CustomMessageId.SpeederEvent)
            {
                SpeederEventMessage model = new SpeederEventMessage(messageBody);
                switch (model.messageType)
                {
                    case MessageModel.Message.None:
                        // do nothing
                        return;
                    case MessageModel.Message.Kill:

                        if (!m_SpeederViews.ContainsKey(model.entityId)) // Not a speeder killed
                            return;


                        m_SpeederViews[model.entityId].Kill();
                        m_SpeederStateComponentService.SetState(model.entityId, SpeederState.Totaled);
                        if (IsPlayer(model.activeByEntityId))
                        {
                            m_NotificationService.Destroy();
                        }

                        m_CrownService.OnCrownKeeperDestroy(model.entityId);

                        //onKill?.Invoke();
                        break;
                    case MessageModel.Message.Respawn:
                        // init other simulation
                        var transform = m_TransformComponentService.GetComponentModel(model.entityId).model;
                        var prop = m_PropertiesComponentService.GetComponentModel(model.entityId).model;
                        var inputModel = SpeederMapper.ToInputModel(transform.position, transform.rotation);
                        m_Simulations[model.entityId].Init(inputModel);
                        m_SpeederViews[model.entityId].transform
                            .SetPositionAndRotation(inputModel.position, inputModel.rotation);
                        // unset state
                        m_SpeederStateComponentService.UnsetState(model.entityId, SpeederState.Totaled);
                        // play sfx
                        m_AudioUiService.PlayCarSpawnSound();
                        m_CrownService.OnCrownKeeperRespawn(model.entityId);
                        m_CollisionFlag = false;
                        m_AnalyticsService.SpawnVehicle(m_AukiWrapper.GetSession().Id, m_RaycastService.distance,
                            ((AssetId)Enum.ToObject(typeof(AssetId), prop.id)).ToString());
                        break;
                    case MessageModel.Message.Despawn:
                        m_SpeederViews[model.entityId].Kill();
                        m_SpeederStateComponentService.SetState(model.entityId, SpeederState.Totaled);

                        DeductPointByDespawn(model.entityId);

                        break;
                }
            }
        }

        private void SendSpeederEventMessage(MessageModel.Message message, uint myEntityID, uint otherEntityId)
        {
            uint[] participants = m_AukiWrapper.GetSession().GetParticipantsIds().ToArray();
            SpeederEventMessage eventMessage = new SpeederEventMessage(CustomMessageId.SpeederEvent, myEntityID, otherEntityId, message);
            m_AukiWrapper.SendCustomMessage(participants, eventMessage.GetBytes());
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            for (int i = 0; i < m_SimulationList.Count; i++)
            {
                //Get inputs
                var simulation = m_SimulationList[i];

                if (m_SpeederStateComponentService.TryGetComponentModel(simulation.entityId,
                        out var speederStateComponent))
                {
                    if (speederStateComponent.model.state == SpeederState.Clone)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

                var entityId = simulation.entityId;

                var equipmentState = EquipmentState.None;

                if (!m_EquipmentService.TryGetComponentModel(entityId, out var equipmentComponent))
                    return; // speeder not finished yet due to race condition.

                equipmentState = equipmentComponent.model.state;

                if (!m_TransformComponentService.TryGetComponentModel(simulation.entityId, out var transformComponent))
                    return; // speeder not finished yet due to race condition.

                var transform = transformComponent;
                var speederState = SpeederState.None;
                if (m_SpeederStateComponentService.TryGetComponentModel(simulation.entityId,
                    out var speederStatecomponent))
                {
                    speederState = speederStatecomponent.model.state;
                    if (simulation.isPlayer && 
                        simulation.age >= 1f && m_MarkerTypeService.GetType() != MarkerService.MarkerType.Arrow)
                    {
                        m_MarkerTypeService.SetType(MarkerService.MarkerType.Arrow);
                    }
                    if (simulation.isPlayer && simulation.state.HasFlag(SpeederState.Totaled) &&
                        m_MarkerTypeService.GetType() != MarkerService.MarkerType.Tip)
                    {
                        m_MarkerTypeService.SetType(MarkerService.MarkerType.Tip);
                    }
                }

                // TODO : move to service
                var target = SpeederHelper.GetTarget(m_RaycastService);
                var isPlayer = simulation.entityId == m_MySpeederEntity;

                var floorSnapTarget = isPlayer ? target : null;
                var (floorNormal, position) = SpeederHelper.GetFloorNormalAndPosition(simulation, m_RaycastService, floorSnapTarget);

                //var crownKeeper = m_CrownService.crownKeeper == entityId;
                var crownKeeper = false;
                var brakeInput = (isPlayer) ? m_SpeederHUDService.brakeInput : false;
                
                // NOTE: (Marko) Under glow Highlight control
                m_SpeederViews[entityId].SetUnderGlowHighlight(isPlayer);
                

                var input = (isPlayer && equipmentState == EquipmentState.Dash && !m_CooldownService.inCooldown)
                    ? m_SpeederHUDService.tapScreenInput
                    : false;
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
                    equipmentState,
                    crownKeeper,
                    m_WorldScaleService.worldScale
                );

                // Do update
                simulation.Update(deltaTime, inputModel);
                if (inputModel.input)
                {
                    m_CooldownService.ActivateCooldown();
                    m_EquipmentService.Use(entityId);
                }


                // Get outputs
                var viewState = SpeederMapper.ToViewModel(simulation, equipmentState);
                var gameState = SpeederMapper.ToGameModel(simulation, equipmentState);

                // Apply outputs
                m_SpeederViews[entityId].UpdateView(viewState);

                if (isPlayer)
                {
                    // copy this condition from LaserService
                    if (speederState.HasFlag(SpeederState.LaserCharge) && !speederState.HasFlag(SpeederState.LaserFire) && !m_SpeederHUDService.holdScreenInput)
                    {
                        m_AnalyticsService.PowerUpUse(equipmentState, m_EquipmentService.GetUses(entityId));
                    }

                    if (ShouldBroadcastTransform(deltaTime))
                    {
                        m_TransformComponentService.UpdateComponent(transform,
                            new TransformModel(gameState.position, gameState.rotation, gameState.speed));
                    }


                    foreach (var setState in setStatesToCheck) // todo I dont like this too much
                    {
                        if (gameState.speederState.HasFlag(setState) &&
                            !simulation.lastSpeederGameModel.speederState.HasFlag(setState))
                        {
                            m_SpeederStateComponentService.SetState(entityId, setState);
                        }

                        if (!gameState.speederState.HasFlag(setState) &&
                            simulation.lastSpeederGameModel.speederState.HasFlag(setState))
                        {
                            m_SpeederStateComponentService.UnsetState(entityId, setState);
                        }
                    }
                }

                simulation.lastSpeederGameModel = gameState;
            }
        }

        private const float BROADCAST_FREQUENCY = 0.0334f;
        private float m_BroadcastTimer;

        private bool ShouldBroadcastTransform(float deltaTime) 
        {
            //TODO:: check delta position & rotation & speed
            //m_BroadcastTimer += deltaTime;

            //if (m_BroadcastTimer < BROADCAST_FREQUENCY)
            //    return false;

            //m_BroadcastTimer = 0;
            return true;
        }

        private void OnSpeederDelete(uint entityId)
        {
            if (!m_SpeederViews.ContainsKey(entityId))
                return;

            var simulation = m_Simulations[entityId];

            m_Vehicles.Remove(entityId);
            m_SimulationList.Remove(simulation);
            m_Simulations.Remove(entityId);
            m_SpeederViews.Remove(entityId);

            if (IsPlayer(entityId))
                m_SpeederHUDService.Hide();

            if (IsPlayer(entityId))
            {
                m_MySpeederEntity = m_EmptySpeederEntity;
            }
        }

        public void Respawn()
        {
            m_MarkerTypeService.SetType(MarkerService.MarkerType.Arrow);
            // init simulation
            var inputModel = SpeederMapper.ToInputModel(m_RaycastService.hitPose);
            mySimulation.Init(inputModel);
            m_PropertiesComponentService.UpdateComponent(m_MySpeederEntity, new PropertiesModel(m_SelectedVehicle.id));

            // respawn HUD
            m_SpeederHUDService.Respawn();

            // update transform component
            m_TransformComponentService.UpdateComponent(m_MySpeederEntity,
                new TransformModel(m_RaycastService.hitPose.position, m_RaycastService.hitPose.rotation, 0f));
            //m_TransformComponentService.ResetFrequency();
            // update message component - broadcast message
            /*m_MessageComponentService.UpdateComponent(
                m_MySpeederEntity,
                new MessageModel(MessageModel.Message.Respawn, m_MySpeederEntity));*/
            SendSpeederEventMessage(MessageModel.Message.Respawn, m_MySpeederEntity, m_MySpeederEntity);
            RespawnLocalSpeeder(m_MySpeederEntity);
            // play sfx
            m_AudioUiService.PlayCarSpawnSound();
            
            onRespawn?.Invoke();
        }

        // public function here?
        public void Despawn(bool back = false)
        {
            m_SpeederHUDService.Hide();
            if (m_MySpeederEntity != m_EmptySpeederEntity)
            {
                SendSpeederEventMessage(MessageModel.Message.Despawn, m_MySpeederEntity, m_MySpeederEntity);
                DespawnLocalSpeeder();
                if (back)
                {
                    m_AnalyticsService.PlayerVehicleExploded(m_AukiWrapper.GetSession().Id, CarExplodeCause.Despawn);
                }
            }
        }

        public bool TryGetSpeederView(uint entity, out SpeederView speederView)
        {
            if (!m_SpeederViews.ContainsKey(entity))
            {
                speederView = default;
                return false;
            }

            speederView = m_SpeederViews[entity];
            return true;
        }

        public bool TryGetSpeeder(uint entityId, out ISpeederSimulation speederSimulation)
        {
            if (m_Simulations.ContainsKey(entityId))
            {
                speederSimulation = m_Simulations[entityId];
                return true;
            }

            speederSimulation = null;
            return false;
        }

        public List<ISpeederSimulation> GetSimulations()
        {
            return m_SimulationList;
        }

        public void SetServerVehicle(Vehicle vehicle)
        {
            m_SelectedVehicle = vehicle;
        }

        public void SpawnVehicle()
        {
            if(m_SpeederViews.ContainsKey(m_MySpeederEntity))
            {
                Respawn();
            }
            else
            {
                CreateSpeeder(m_SelectedVehicle);
            }
        }
    }
}
