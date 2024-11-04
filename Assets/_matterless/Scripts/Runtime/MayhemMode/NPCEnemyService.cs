using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using Matterless.Inject;
using Matterless.UTools;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;


namespace Matterless.Floorcraft
{
    public class NPCEnemyService : ITickable
    {
        private readonly Settings m_Settings;
        private readonly AukiWrapper m_AukiWrapper;
        private readonly TransformComponentService m_TransformComponentService;
        private readonly PropertiesComponentService m_PropertiesComponentService;
        private readonly PropertiesECSService.Settings m_AssetSettings;
        private readonly MessageComponentService m_MessageComponentService;
        private readonly EnemyStateComponentService m_EnemyStateComponentService;
        private readonly IRaycastService m_RaycastService;
        private readonly WorldScaleService m_WorldScaleService;
        private readonly MayhemEnemiesStatusComponentService m_MayhemEnemiesStatusComponentService;
        private readonly MayhemObstacleComponentService m_MayhemObstacleComponentService;
        private readonly ICoroutineRunner m_CoroutineRunner;
       
        private Stack<EnemyWave> enemyWaves = new();

        private Dictionary<uint, Enemy> m_Enemies = new();
        private Dictionary<uint, NPCEnemyView> m_EnemyViews = new();
        
        // Only host filling this dictionary, on clients this will be empty
        private Dictionary<uint, Entity> m_MyEnemyEntities = new();
        private Transform m_Objective;
        private uint m_ObstacleEntity;
        private float m_BroadcastFrequency = 0.5f;
        private float m_BroadcastTimer;

        public Action onEnemyKilled;

        public NPCEnemyService(Settings settings, 
            AukiWrapper aukiWrapper,
            TransformComponentService transformComponentService,
            PropertiesComponentService propertiesComponentService,
            PropertiesECSService.Settings assetSettings,
            MessageComponentService messageComponentService,
            EnemyStateComponentService enemyStateComponentService,
            IRaycastService raycastService,
            WorldScaleService worldScaleService,
            MayhemEnemiesStatusComponentService mayhemEnemiesStatusComponentService,
            MayhemObstacleComponentService mayhemObstacleComponentService,
            ICoroutineRunner coroutineRunner
            )
        {
            m_Settings = settings;
            m_AukiWrapper = aukiWrapper;
            m_TransformComponentService = transformComponentService;
            m_PropertiesComponentService = propertiesComponentService;
            m_AssetSettings = assetSettings;
            m_MessageComponentService = messageComponentService;
            m_EnemyStateComponentService = enemyStateComponentService;
            m_RaycastService = raycastService;
            m_WorldScaleService = worldScaleService;
            m_MayhemEnemiesStatusComponentService = mayhemEnemiesStatusComponentService;
            m_MayhemObstacleComponentService = mayhemObstacleComponentService;
            m_CoroutineRunner = coroutineRunner;

            aukiWrapper.onEntityDeleted += OnEntityDeleted;
            
            m_MayhemObstacleComponentService.onComponentAdded += OnMayhemTowerAdded;
            m_TransformComponentService.onComponentAdded += OnTransformComponentAdded;
            m_PropertiesComponentService.onComponentAdded += OnPropertyComponentAdded;
            m_MessageComponentService.onComponentUpdated += OnMessageComponentUpdated;
            m_EnemyStateComponentService.onComponentUpdated += EnemyStateComponentUpdated;
            m_MayhemEnemiesStatusComponentService.onComponentUpdated += OnEnemyStatusUpdated;
            m_AukiWrapper.onJoined += OnJoined;
        }

        private void OnJoined(Session session)
        {
            ClearEnemies();
        }

        private void OnPropertyComponentAdded(PropertiesComponentModel model)
        {
            if (m_AssetSettings.GetAsset(model.model.id).assetType != AssetType.Enemy)
            {
                return;
            }

            var enemyAsset = m_Settings.GetAsset(model.model.id);
            m_Enemies.Add(model.entityId, enemyAsset);
        }

        private void OnMayhemTowerAdded(MayhemObstacleComponentModel model)
        {
            var obstacleView = m_PropertiesComponentService
                .GetGameObject(model.entityId)
                .GetComponent<ObstacleView>();

            m_Objective = obstacleView.transform;
            m_ObstacleEntity = model.entityId;

            if (model.isMine)
            {
                m_MayhemEnemiesStatusComponentService.AddComponent(model.entityId,
                    new MayhemEnemiesStatusModel(0, new MayhemEnemyStatusModel[0]));
            }
        }

        private void OnEntityDeleted(uint uid)
        {
            m_TransformComponentService.DeleteComponent(uid);
            
            if (m_Enemies.ContainsKey(uid))
            {
                m_Enemies.Remove(uid);
            }

            if (m_EnemyViews.ContainsKey(uid))
            {
                if (m_EnemyViews[uid] != null)
                {
                    m_EnemyViews[uid].PlayDestroyAnimations();
                    GameObject.Destroy(m_EnemyViews[uid].gameObject);
                }

                m_EnemyViews.Remove(uid);
            }
            
            if (uid == m_ObstacleEntity)
            {
                //Debug.Log("obstacle deleted");
            }
        }

        private void OnTransformComponentAdded(TransformComponentModel model)
        {
            if (!m_Enemies.ContainsKey(model.entityId))
            {
                return;
            }
            
            var enemy = m_Enemies[model.entityId];
            Pose initPose = new Pose(model.model.position, model.model.rotation);
            
            GameObject enemyGameObject = m_PropertiesComponentService.GetGameObject(model.entityId);
            enemyGameObject.transform.localScale = Vector3.one * (enemy.size * m_WorldScaleService.worldScale);
            
            void SpawnEnemy()
            {
                var enemyView = enemyGameObject.GetComponent<NPCEnemyView>();
                enemyView.Init(
                    model.entityId,
                    // we are using shielded enemies now, so the health should either be 1 or 2. We should come up with a game design idea to utilize this enemy health increase.
                    health: enemy.enemyStartingHealth/* + enemyWaves.Count * enemy.enemyHealthIncrease*/,
                    speed: enemy.enemyStartingSpeed + enemyWaves.Count * enemy.enemySpeedIncrease,
                    m_Objective,
                    new EnemyViewModel()
                    {
                        entityId = model.entityId,
                        enemyState = EnemyState.Spawning,
                        groundPosition = model.model.position,
                        orientation = model.model.rotation,
                        speed = model.isMine ? enemy.enemyStartingSpeed : model.model.speed
                    },
                    model.isMine,
                    OnEnemyDestroyed);

                enemyView.damageTaken += OnHitEnemy;
            
                if (model.isMine)
                {
                    enemyView.obstacleHit += OnHitObstacle;
                }
            
                m_EnemyViews.Add(model.entityId, enemyView);
                m_EnemyViews[model.entityId].transform
                    .SetPositionAndRotation(initPose.position, initPose.rotation);
            }

            if (m_Objective == null)
            {
                m_CoroutineRunner.StartUnityCoroutine(WaitForTower(() =>
                {
                    SpawnEnemy();
                }));
            }
            else
            {
                SpawnEnemy(); 
            }
        }

        IEnumerator WaitForTower(Action onTowerSpawned)
        {
            yield return new WaitUntil(() => m_Objective != null);
            onTowerSpawned?.Invoke();
        }

        private void OnEnemyStatusUpdated(MayhemEnemiesStatusComponentModel model)
        {
            if (model.isMine)
                return;

            for (int i = 0; i < model.model.enemyCount; i++)
            {
#if UNITY_EDITOR
                Debug.DrawLine(model.model.enemyModels[i].position, model.model.enemyModels[i].position + Vector3.up, Color.green, 5f);
#endif
                
                var position = model.model.enemyModels[i].position;
                var speed = model.model.enemyModels[i].speed;
                uint entityId = model.model.enemyModels[i].entityId;

                // This is important to not lose tracked floor while gameplay
                if (m_RaycastService.FloorRaycast(position, out Vector3 snappedGroundPosition, out _))
                {
                    position = snappedGroundPosition;
                }

                var correctedModel = new MayhemEnemyStatusModel(entityId, position, speed);
                model.model.enemyModels[i] = correctedModel;

                if (m_EnemyViews.ContainsKey(entityId))
                {
                    var enemy = m_EnemyViews[entityId];

                    if (enemy != null)
                    {
                        enemy.UpdateNetworkPosition(position);
                    }
                }
            }
        }
        
        private void OnMessageComponentUpdated(MessageComponentModel model)
        {
            switch (model.model.message)
            {
                case MessageModel.Message.WaveStart:
                    return;
                case MessageModel.Message.EnemyKill:
                    return;
                case MessageModel.Message.ObstacleTotaled:
                    Debug.Log("obstacle totaled message received");
                    ClearEnemies();
                    return;
                case MessageModel.Message.WaveComplete:
                    return;
            }
        }
        
        private void EnemyStateComponentUpdated(EnemyStateComponentModel model)
        {
            if (m_EnemyViews.TryGetValue(model.entityId, out var view))
            {
                view.UpdateHealth(model.model.health);
            }
        }

        void OnEnemyDestroyed(uint uid)
        {
            if (m_MyEnemyEntities.ContainsKey(uid))
            {
                onEnemyKilled?.Invoke();
                m_EnemyStateComponentService.DeleteComponent(uid);
                m_PropertiesComponentService.DeleteComponent(uid);
                m_TransformComponentService.DeleteComponent(uid);
                m_MessageComponentService.DeleteComponent(uid);
                m_AukiWrapper.DeleteEntity(uid, (() =>
                {
                    Debug.Log("Removed entity from server");
                    OnEntityDeleted(uid);
                }));
            }
        }

        void OnHitObstacle()
        {
            MayhemModeService.m_MayhemModeInstance.DamageObjective();
        }

        void OnHitEnemy(uint uid)
        {
            if (!m_EnemyStateComponentService.TryGetComponentModel(uid, out var enemyStateComponentModel))
            {
                return;
            }
            
            m_EnemyStateComponentService.UpdateComponent(uid, new EnemyStateModel(enemyStateComponentModel.model.state, enemyStateComponentModel.model.health - 1));
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            foreach (uint entityId in m_Enemies.Keys)
            {
                if (m_EnemyViews.ContainsKey(entityId))
                {
                    var enemy = m_EnemyViews[entityId];

                    if (enemy != null)
                    {
                        enemy.UpdateView(deltaTime);
                    }
                }
            }

            // Broadcast every once in a while to make clients catch-up with the host
            // TODO: Also check if mayhem mode is running
            if (m_AukiWrapper.isHost)
            {
                m_BroadcastTimer += deltaTime;
                if (m_BroadcastTimer >= m_BroadcastFrequency)
                {
                    List<MayhemEnemyStatusModel> enemyStatusModels = new List<MayhemEnemyStatusModel>();
                    foreach (uint entityId in m_Enemies.Keys)
                    {
                        if (m_EnemyViews.ContainsKey(entityId))
                        {
                            var enemy = m_EnemyViews[entityId];

                            if (enemy != null)
                            {
                                enemyStatusModels.Add(new MayhemEnemyStatusModel(entityId, enemy.transform.position, enemy.speed));
                            }
                        }
                    }

                    if (m_EnemyViews.Count > 0)
                    {
                        m_MayhemEnemiesStatusComponentService.UpdateComponent(m_ObstacleEntity,
                            new MayhemEnemiesStatusModel(enemyStatusModels.Count, enemyStatusModels.ToArray()));
                        m_BroadcastTimer = 0;
                    }
                }
            }
        }

        private Vector3 GetRandomSpawnPoint(Transform objective, float minDistance, float maxDistance)
        {
            var direction = Quaternion.AngleAxis(UnityEngine.Random.Range(0f,360f), Vector3.up) * Vector3.forward;
            direction = direction.normalized;
            var distance = minDistance + ((maxDistance - minDistance) * UnityEngine.Random.value);
            return objective.position + (direction * distance);
        }

        public void CreateEnemy(Vector3 position, Transform objective, Action<uint> onEnemySpawned, List<Enemy> onlyTheseEnemyTypes = null)
        {
            m_Objective = objective;
            Pose pose = new Pose(position, Quaternion.identity);
            
            m_AukiWrapper.AddEntity(
                pose,
                false,
                entity =>
                {
                    m_MyEnemyEntities.Add(entity.Id, entity);
                    List<Enemy> enemyTypes;
                    if(onlyTheseEnemyTypes != null && onlyTheseEnemyTypes.Count > 0)
                    {
                        enemyTypes = onlyTheseEnemyTypes;
                    }
                    else
                    {
                        enemyTypes = m_Settings.Enemies;
                    }
                    Enemy randEnemy = enemyTypes[UnityEngine.Random.Range(0, enemyTypes.Count)];
                    m_EnemyStateComponentService.AddComponent(entity.Id, new EnemyStateModel(EnemyState.Spawning, randEnemy.enemyStartingHealth));
                    m_PropertiesComponentService.AddComponent(entity.Id, new PropertiesModel(randEnemy.id));
                    m_TransformComponentService.AddComponent(entity.Id, new TransformModel(pose.position, pose.rotation, randEnemy.enemyStartingSpeed));
                    m_MessageComponentService.AddComponent(entity.Id, new MessageModel(MessageModel.Message.None, entity.Id));
                    
                    onEnemySpawned?.Invoke(entity.Id);
                },
                error => { Debug.LogError(error); }
                );
        }

        public void ClearEnemies()
        {
            foreach (var keyValuePair in m_MyEnemyEntities)
            {
                if (m_AukiWrapper.isConnected)
                {
                    if (m_AukiWrapper.GetEntity(keyValuePair.Key) != null)
                    {
                        m_EnemyStateComponentService.DeleteComponent(keyValuePair.Key);
                        m_PropertiesComponentService.DeleteComponent(keyValuePair.Key);
                        m_TransformComponentService.DeleteComponent(keyValuePair.Key);
                        m_MessageComponentService.DeleteComponent(keyValuePair.Key);
                        m_AukiWrapper.DeleteEntity(keyValuePair.Key, () => { });
                    }
                }
            }

            foreach (var keyValuePair in m_EnemyViews)
            {
                GameObject.Destroy(keyValuePair.Value.gameObject);
            }
            
            m_MyEnemyEntities.Clear();
            m_Enemies.Clear();
            m_EnemyViews.Clear();
        }

        [System.Serializable]
        public class Settings
        {
            [FormerlySerializedAs("m_EnemySettings")] [SerializeField] private List<Enemy> m_Enemies;
            
            public List<Enemy> Enemies => m_Enemies;
            public Enemy GetAsset(uint id) => m_Enemies.Find(x => x.id == id);
        }
    }
}