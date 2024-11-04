using System;
using System.Collections.Generic;
using System.Linq;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class ObstacleService : ITickable
    {
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IRaycastService m_RaycastService;
        private readonly PropertiesComponentService m_PropertiesEcsService;
        private readonly TransformComponentService m_TransformService;
        private readonly PowerUpSpawnPointService m_PowerUpSpawnPointService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly PropertiesECSService.Settings m_AssetSettings;
        private readonly ScoreComponentService m_ScoreService;
        private readonly MayhemObstacleComponentService m_MayhemObstacleComponentService;
        private readonly MayhemModeService.Settings m_MayhemModeSettings;
        private readonly PlaceableSelectorService.Settings m_PlaceableSettings;

        public Dictionary<Placeable, int> counter { get; } = new Dictionary<Placeable, int>();
        public int ObstaclesCount => counter[selectedPlaceable];
        
        public bool HasSpawnedObstacles => selectedPlaceable != null && counter.ContainsKey(selectedPlaceable) && counter[selectedPlaceable] > 0;
        public bool HasSpawnedMayhemObstacle => m_IsSpawnedMayhemObstacle;
        public int RemainingObstacles => m_Settings.maxObstacles - counter[selectedPlaceable];

        private readonly Dictionary<uint, ObstacleView> m_ObstacleViews = new();
        private readonly AudioUiService m_AudioUiService;

        public Placeable selectedPlaceable { get; private set; }
        public int maxObstacles
        {
            get { return m_Settings.maxObstacles; }
            private set { }
        }

        public Action onPlaceableChanged;
        public Action<uint> onObstacleSpawned;
        public Action<uint> onMayhemObstacleSpawned;
        public Action<uint> onObstacleRemoved;
        public Action<uint> onMayhemObstacleRemoved;
        private Settings m_Settings;
        private readonly WorldScaleService m_WorldScaleService;
        
        private TowerPlacementView m_TowerPlacementArea;
        private bool m_IsPreviewing;
        public bool isPreviewing => m_IsPreviewing;
        private bool m_InSpawningScreen;
        private bool m_IsSpawnedMayhemObstacle;
        private uint m_MayhemObstacleId;
        public bool isInHayhemMode { get; set; }

        public ObstacleService(
            IAukiWrapper aukiWrapper,
            IRaycastService raycastService,
            PropertiesComponentService propertiesComponentService,
            IAnalyticsService analyticsService,
            TransformComponentService transformService,
            PowerUpSpawnPointService powerUpSpawnPointService,
            PropertiesECSService.Settings assetSettings,
            ScoreComponentService scoreService,
            AudioUiService audioUiService,
            Settings settings,
            WorldScaleService worldScaleService,
            MayhemObstacleComponentService mayhemObstacleComponentService,
            MayhemModeService.Settings mayhemModeSettings,
            PlaceableSelectorService.Settings placeableSettings)
        {
            m_AssetSettings = assetSettings;
            m_ScoreService = scoreService;
            m_AudioUiService = audioUiService;
            m_Settings = settings;
            m_WorldScaleService = worldScaleService;
            m_AukiWrapper = aukiWrapper;
            m_RaycastService = raycastService;
            m_PropertiesEcsService = propertiesComponentService;
            m_TransformService = transformService;
            m_PowerUpSpawnPointService = powerUpSpawnPointService;
            m_AnalyticsService = analyticsService;
            m_MayhemObstacleComponentService = mayhemObstacleComponentService;
            m_MayhemModeSettings = mayhemModeSettings;
            m_PlaceableSettings = placeableSettings;

            m_PropertiesEcsService.onComponentAdded += OnPropertiesComponentAdded;
            m_TransformService.onComponentAdded += OnTransformComponentAdded;
            m_AukiWrapper.onEntityDeleted += OnAukiEntityDeleted;
            //m_CrownService.onCrownKeeperChanged += OnCrownKeeperChanged;

            m_AukiWrapper.onLeft += OnResetObstaclesButtonClicked;
        }


        private void CreateObstacle(IAsset asset)
        {
            var pose = m_RaycastService.hitPose;
            m_AukiWrapper.AddEntity(pose, asset.persistent, entity =>
            {
                m_PropertiesEcsService.AddComponent(entity.Id, new PropertiesModel(asset.id));
                m_TransformService.AddComponent(entity.Id, new TransformModel(pose.position, pose.rotation, 0f));

                if (asset.assetType == AssetType.Obstacle && asset.assetId == AssetId.MayhemPillar)
                {
                    m_MayhemObstacleComponentService.AddComponent(entity.Id,
                        new MayhemObstacleModel(MayhemObstacleState.Spawning, m_MayhemModeSettings.targetMaxHealth, 0));
                }

                m_AnalyticsService.PlaceObstacle(asset.assetType, ((AssetId)asset.id).ToString(), m_RaycastService.distance, m_AukiWrapper.GetSession().Id, m_AukiWrapper.GetSession().GetParticipants().Count);

                if (asset.assetType == AssetType.PowerUpSpawnPoint)
                {
                    m_PowerUpSpawnPointService.AddComponent(entity.Id, new SpawnPointCooldownStateModel(false));
                    m_PowerUpSpawnPointService.AddLocalId(entity.Id);
                }
                
                onObstacleSpawned?.Invoke(entity.Id);
            }, error => { Debug.LogError("Error!!"); });
        }

        public void RemoveAllObstacles()
        {
            var views = m_ObstacleViews.Keys.ToArray();

            for (int i = 0; i < views.Length; i++)
            {
                var entityId = views[i];
                if (m_PropertiesEcsService.GetComponentModel(entityId).isMine)
                {
                    RemoveObstacle(entityId);
                }
            }
        }

        public void RemoveObstacle(uint entityId)
        {
            if (m_PropertiesEcsService.GetComponentModel(entityId).isMine)
            {
                // remove components
                m_TransformService.DeleteComponent(entityId);
                m_PropertiesEcsService.DeleteComponent(entityId);

                if (m_MayhemObstacleComponentService.TryGetComponentModel(entityId, out var mayhemObstacleModel))
                {
                    m_MayhemObstacleComponentService.DeleteComponent(entityId);
                    m_IsSpawnedMayhemObstacle = false;
                }
                // deleting the entity didn't invoke the delete property events
                // TODO:: ask AUKI about it
                m_AukiWrapper.DeleteEntity(entityId, () =>
                {
                    Debug.Log($"Obstacle entity deleted {entityId}");
                });
                
                counter[selectedPlaceable]--;
            }
            
            onObstacleRemoved?.Invoke(entityId);
            m_ObstacleViews.Remove(entityId);
        }

        private void OnPropertiesComponentAdded(PropertiesComponentModel model)
        {
            var asset = m_AssetSettings.GetAsset(model.model.id);

            if (asset.assetType != AssetType.Obstacle)
                return;

            if (asset.assetId == AssetId.MayhemPillar)
            {
                m_IsSpawnedMayhemObstacle = true;
                m_MayhemObstacleId = model.entityId;
                onMayhemObstacleSpawned?.Invoke(model.entityId);

                if (!model.isMine)
                {
                    // We allow only one tower for mayhem mode, need to remove all other towers
                    if (m_ObstacleViews.Count > 0)
                    {
                        RemoveAllObstacles();
                    }
                }
            }

            var obstacleView = m_PropertiesEcsService.GetGameObject(model.entityId)
                .GetComponent<ObstacleView>()
                .Init(model.entityId, model.model.id, asset.scale, m_WorldScaleService.worldScale);

            m_ObstacleViews.Add(model.entityId, obstacleView);
        }

        private void OnTransformComponentAdded(TransformComponentModel model)
        {
            if (!m_ObstacleViews.ContainsKey(model.entityId))
                return;

            // add the floor raycast to set the correct position
            Vector3 position;
            Vector3 normal;
            if (m_RaycastService.FloorRaycast(model.model.position, out position, out normal))
            {
                m_ObstacleViews[model.entityId].transform.SetPositionAndRotation(position + Vector3.up * 0.002f, model.model.rotation);
            }
            else
            {
                m_ObstacleViews[model.entityId].transform.SetPositionAndRotation(model.model.position, model.model.rotation);
            }

        }

        // TODO:: doing this every frame is not a good solution
        // check the floor raycast to set the correct position every frame
        private void CheckObstacleOnPlane()
        {
            foreach (var obstacle in m_ObstacleViews)
            {
                if (m_AssetSettings.GetAsset(obstacle.Value.obstacleType).id != (uint)AssetId.Plane ||
                    m_AssetSettings.GetAsset(obstacle.Value.obstacleType).id != (uint)AssetId.PlaneMatterless)
                    return;
                Vector3 position;
                Vector3 normal;
                if (m_RaycastService.FloorRaycast(obstacle.Value.transform.position, out position, out normal))
                {
                    // add a bit on top of the plane , 0.002f is from old code
                    obstacle.Value.transform.SetPositionAndRotation(position + Vector3.up * 0.002f, obstacle.Value.transform.rotation);
                }
            }
        }

        private void OnAukiEntityDeleted(uint entityId)
        {
            Debug.Log("Entity deleted: " + entityId);
            m_ObstacleViews.Remove(entityId);
            if (entityId == m_MayhemObstacleId)
            {
                m_IsSpawnedMayhemObstacle = false;
                onMayhemObstacleRemoved?.Invoke(entityId);
            }
        }
        
        public void SetPlaceable(Placeable placeable)
        {
            Debug.Log($"Set pleacable{placeable.name}");
            selectedPlaceable = placeable;
            if (!counter.ContainsKey(selectedPlaceable)) counter[selectedPlaceable] = 0;
            
            //UpdateRemainingLabel();
            onPlaceableChanged?.Invoke();
        }

        public void OnObstaclesCreateButtonClicked()
        {
            // this is obsolete, as we hide the button when obstacles have reached the max number
            if(counter[selectedPlaceable] >= m_Settings.maxObstacles)
                return;
            
            m_AudioUiService.PlaySelectSound();
            // TODO:: replace this with ECS
            //m_EntityManager.SpawnPlayer(m_Settings.obstacle);
            
            //m_obstacleService.CreateObstacle(m_Settings.obstacles[0]);
            CreateObstacle(selectedPlaceable);
            counter[selectedPlaceable]++;
            //UpdateRemainingLabel();
            
            if (selectedPlaceable.assetId == AssetId.MayhemPillar)
            {
                if (m_TowerPlacementArea != null)
                {
                    m_TowerPlacementArea.OnTowerPlaced(() => { m_TowerPlacementArea.gameObject.SetActive(false); });
                }
                
                m_IsSpawnedMayhemObstacle = true;
            }

            onPlaceableChanged?.Invoke();
        }
        
        public void OnResetObstaclesButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
            if (selectedPlaceable.assetType == AssetType.Obstacle)
            {
                RemoveAllObstacles();    
            }
            else
            {
                m_PowerUpSpawnPointService.RemoveAllLocalOfType(selectedPlaceable);
            }
            counter[selectedPlaceable] = 0;
            //UpdateRemainingLabel();
            onPlaceableChanged?.Invoke();
        }

        public void SetInSpawningScreen(bool inSpawningScreen)
        {
            m_InSpawningScreen = inSpawningScreen;
        }

        public void StartPreviewing()
        {
            if (m_TowerPlacementArea == null)
            {
                m_TowerPlacementArea = GameObject.Instantiate(m_Settings.mayhemTowerPlacementArea);
                m_TowerPlacementArea.transform.localScale = m_Settings.mayhemTowerPlacementArea.transform.localScale *
                                                            m_WorldScaleService.worldScale * m_MayhemModeSettings.playAreaRadius;
                m_TowerPlacementArea.OnShowTowerPlacementVisals(null);
            }

            m_TowerPlacementArea.gameObject.SetActive(true);
            m_TowerPlacementArea.OnShowTowerPlacementVisals(null);
            m_IsPreviewing = true;
        }

        public void StopPreviewing()
        {
            m_IsPreviewing = false;
        }

        public void RemoveMayhemPlacementArea()
        {
            if (m_TowerPlacementArea != null && m_TowerPlacementArea.gameObject.activeSelf)
            {
                m_TowerPlacementArea.OnTowerPlacementCanceled(() =>
                {
                    m_TowerPlacementArea.gameObject.SetActive(false);
                });
            }
        }

        
        private void OnPlanesCreateButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
            CreateObstacle(m_Settings.obstacles[1]);
        }

        private void OnPlaneMatterlessCreateButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
            CreateObstacle(m_Settings.obstacles[2]);
        }
        
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            CheckPreviewState();
            CheckObstacleOnPlane();
        }

        private void CheckPreviewState()
        {
            if (m_InSpawningScreen && !m_IsSpawnedMayhemObstacle && !m_IsPreviewing && isInHayhemMode && m_AukiWrapper.isHost)
            {
                StartPreviewing();
            }
            else if (m_IsPreviewing && (!m_InSpawningScreen || !isInHayhemMode || m_IsSpawnedMayhemObstacle || !m_AukiWrapper.isHost))
            {
                StopPreviewing();
            }

            if (m_IsPreviewing)
            {
                m_TowerPlacementArea.transform.position = m_RaycastService.hitPose.position;
            }
        }

        private void OnSeen(LaserTurretObstacleView view, Collider seenOther)
        {
            if (view.cooldown > 0 || view.aggressionTime > 0)
                return;
            
            if (seenOther.gameObject.CompareTag(UnityGameObjectTag.Speeder))
            {
                view.FireLaser();
            }
        }

        private void OnShot(LaserTurretObstacleView view, Collider seenOther)
        {
            if (seenOther.gameObject.CompareTag(UnityGameObjectTag.Laser))
            {
                var laserCollider = seenOther.gameObject.GetComponent<LaserCollider>(); 
                if (laserCollider != null)
                {
                    if (laserCollider.entityId == view.entityId)
                        return;
                    
                    
                    Debug.Log($"tower laser killed by {laserCollider.entityId}");

                    ScoreComponentModel scoreComponentModel = m_ScoreService.GetScoreComponentModel(laserCollider.entityId);
                    if (scoreComponentModel != null)
                    {
                        int newScore = scoreComponentModel.model.score + 1;
                        m_ScoreService.UpdateComponent(laserCollider.entityId, new ScoreModel(newScore));    
                    }
                    
                }
                
                view.DestroyLaser();
            }
        }
        
        private void OnCrownKeeperChanged(uint entityId, int score)
        {
            foreach (LaserTurretObstacleView obstacleView in m_ObstacleViews.Values)
            {
                obstacleView.SetPriorityTarget(entityId, score);
            }
        }
        
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private List<Asset> m_Obstacles;
            [SerializeField] private int m_MaxObstacles = 3;
            [SerializeField] private TowerPlacementView m_MayhemTowerPlacementArea;
            public List<Asset> obstacles => m_Obstacles;
            public int maxObstacles => m_MaxObstacles;
            public TowerPlacementView mayhemTowerPlacementArea => m_MayhemTowerPlacementArea;
        }
    }
}

