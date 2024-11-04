using System.Collections.Generic;
using Auki.ConjureKit;
using Auki.ConjureKit.Hagall.Messages;
using Matterless.Inject;
using Matterless.Localisation;
using Matterless.Module.UI;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public partial class MayhemModeService : ITickable
    {
        private const string WARNING_LABEL = "WARNING_LABEL";
        private const string HOST_LEFT_MESSAGE = "HOST_LEFT_MESSAGE";
        private const string OKAY_LABEL = "LEAVE_BUTTON_LABEL";

        private readonly IAukiWrapper m_AukiWrapper;
        private readonly PropertiesComponentService m_PropertiesComponentService;
        private readonly MessageComponentService m_MessageComponentService;
        private readonly MayhemObstacleComponentService m_MayhemObstacleComponentService;
        private readonly PropertiesECSService.Settings m_AssetSettings;
        private readonly NPCEnemyService m_NpcEnemyService;
        private readonly Settings m_Settings;
        private readonly ObstacleService m_ObstacleService;
        private readonly MayhemUiService m_MayhemUiService;
        private readonly SpawnLocationsService m_SpawnLocationsService;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly IGameOverUiService m_GameOverUiService;
        private readonly IConnectionService m_ConnectionService;
        private readonly IInputDialogueService m_InputDialogueService;
        private readonly ILocalisationService m_LocalisationService;

        private List<WaveSettings> m_WaveSettings = new List<WaveSettings>();
        private List<uint> m_Entities = new List<uint>();
        private List<uint> m_MayhemObstacleEntities = new List<uint>();
        
        Dictionary<uint, MayhemModeInstance> m_MayhemModeInstances = new();
        public static MayhemModeInstance m_MayhemModeInstance;
        private MayhemModeObstacleView m_View;
        
        private int m_CurrentWave = -1;
        private int m_CurrentHealth;
        private bool m_IsInHayhemMode;
        private bool m_IsMayhemRunning;
        private uint m_HostTowerOwnerId;

        // This should work only for host
        public bool isInHayhemMode
        {
            get => m_IsInHayhemMode;
            set
            {
                m_IsInHayhemMode = value;
                m_ObstacleService.isInHayhemMode = value;
            }
        }

        // This works only for host
        public bool isMayhemStartedForHost => m_MayhemModeInstance.isStarted;

        public MayhemModeService(
            IAukiWrapper aukiWrapper,
            PropertiesComponentService propertiesComponentService,
            MessageComponentService messageComponentService,
            MayhemObstacleComponentService mayhemObstacleComponentService,
            PropertiesECSService.Settings mAssetSettings,
            NPCEnemyService npcEnemyService,
            Settings settings,
            ObstacleService mObstacleService,
            MayhemUiService mayhemUiService,
            SpawnLocationsService spawnLocationsService,
            ICoroutineRunner coroutineRunner,
            IGameOverUiService gameOverUiService,
            IConnectionService connectionService,
            IInputDialogueService inputDialogueService,
            ILocalisationService localisationService)
        {
            m_AukiWrapper = aukiWrapper;
            m_PropertiesComponentService = propertiesComponentService;
            m_MessageComponentService = messageComponentService;
            m_MayhemObstacleComponentService = mayhemObstacleComponentService;
            m_AssetSettings = mAssetSettings;
            m_NpcEnemyService = npcEnemyService;
            m_Settings = settings;
            m_ObstacleService = mObstacleService;
            m_MayhemUiService = mayhemUiService;
            m_SpawnLocationsService = spawnLocationsService;
            m_CoroutineRunner = coroutineRunner;
            m_GameOverUiService = gameOverUiService;
            m_ConnectionService = connectionService;
            m_InputDialogueService = inputDialogueService;
            m_LocalisationService = localisationService;
            m_PropertiesComponentService.onComponentAdded += OnPropertiesComponentAdded;
            m_MayhemObstacleComponentService.onComponentAdded += MayhemObstacleComponentAdded;
            m_MayhemObstacleComponentService.onComponentUpdated += MayhemObstacleComponentUpdated;
            m_ObstacleService.onObstacleRemoved += StopMayhemModeOnObstacleRemoved;
            m_AukiWrapper.onJoined += OnJoined;
            m_AukiWrapper.onEntityDeleted += OnEntityDeleted;
            m_AukiWrapper.onHostChanged += OnHostChanged;
            m_AukiWrapper.onCustomMessageBroadcast += OnCustomMessageBroadcast;
            m_SpawnLocationsService.onSpawnLocationsUpdated += OnSpawnLocationsUpdated;
            
            m_WaveSettings = new List<WaveSettings>();
            foreach(WaveSettings waveSettings in m_Settings.waveSettings)
            {
                m_WaveSettings.Add(waveSettings);
            }

            m_CurrentHealth = m_Settings.targetMaxHealth;
            m_SpawnLocationsService.Init();
        }

        private void OnCustomMessageBroadcast(CustomMessageBroadcast message)
        {
            // To check isMine
            if (message.ParticipantId == m_AukiWrapper.GetSession().ParticipantId)
                return;

            byte[] messageBody = message.Body;

            if ((CustomMessageId)messageBody[0] == CustomMessageId.MayhemEvent)
            {
                MayhemEventMessage mayhemEventMessage = new MayhemEventMessage(messageBody);
                OnEventMessage(mayhemEventMessage.messageType);
            }
            else if ((CustomMessageId)messageBody[0] == CustomMessageId.MayhemUpdate)
            {
                MayhemUpdateMessage mayhemUpdateMessage = new MayhemUpdateMessage(messageBody);
                if (mayhemUpdateMessage.towerHealth > -1)
                {
                    m_CurrentHealth = mayhemUpdateMessage.towerHealth;
                    UpdateTowerHealth(m_CurrentHealth);
                }

                m_CurrentWave = mayhemUpdateMessage.waveNumber;
            }
        }

        private void MayhemObstacleComponentAdded(MayhemObstacleComponentModel model)
        {
            if (model.isMine)
                return;

            m_CurrentHealth = model.model.health;
            m_CurrentWave = model.model.waveNumber;
            UpdateTowerHealth(model.model.health);
        }
        
        private void MayhemObstacleComponentUpdated(MayhemObstacleComponentModel model)
        {
            if (model.isMine)
                return;

            m_CurrentHealth = model.model.health;
            UpdateTowerHealth(model.model.health);
        }

        private void OnEntityDeleted(uint uid)
        {
            if (m_MayhemObstacleEntities.Contains(uid))
            {
                m_MayhemUiService.HideLabels();
                m_MayhemObstacleEntities.Remove(uid);
            }
        }

        private void OnJoined(Session session)
        {
            m_MayhemModeInstances.Clear();
            m_MayhemUiService.HideLabels();
        }

        private void OnSpawnLocationsUpdated()
        {
            m_View.ActivateSpawnPoints(m_SpawnLocationsService.currentLocationIndexes);
        }

        private void OnEventMessage(MessageModel.Message message)
        {
            switch (message)
            {
                case MessageModel.Message.WaveStart:
                    m_MayhemUiService.HideLabels();
                    m_IsMayhemRunning = true;
                    return;
                case MessageModel.Message.EnemyKill:
                    return;
                case MessageModel.Message.WaveComplete:
                    // Show wave starting ui
                    m_IsMayhemRunning = true;
                    m_CurrentWave++;
                    m_MayhemUiService.UpdateWaveNumber(m_CurrentWave);
                    m_MayhemUiService.ShowLabels();
                    m_View.DeactivateSpawnPoints();
                    return;
                case MessageModel.Message.ObstacleTotaled:
                    m_IsMayhemRunning = false;
                    return;
            }
        }

        private void OnPropertiesComponentAdded(PropertiesComponentModel model)
        {
            var asset = m_AssetSettings.GetAsset(model.model.id);
            
            if (m_AssetSettings.GetAsset(model.model.id).assetType == AssetType.Enemy)
            {
                m_Entities.Add(model.entityId);   
            }

            if (asset.assetType != AssetType.Obstacle || asset.assetId != AssetId.MayhemPillar)
                return;

            m_MayhemObstacleEntities.Add(model.entityId);
            var mayhemModeObstacleView = m_PropertiesComponentService
                .GetGameObject(model.entityId)
                .GetComponent<MayhemModeObstacleView>();

            MayhemModeInstance mayhemModeInstance = new MayhemModeInstance();

            //var mayhemModeObstacleView = obstacleView.transform.GetComponent<MayhemModeObstacleView>();

            if (mayhemModeObstacleView)
            {
                m_View = mayhemModeObstacleView;
                mayhemModeObstacleView.Init(m_Settings.targetMaxHealth, m_Settings.targetMaxHealth, m_Settings.playAreaRadius);
                
                if (!model.isMine)
                {
                    m_View.onTowerDestroyed += () =>
                    {
                        OnMayhemGameOver(false, m_CurrentWave);
                        m_CurrentWave = 0;
                        m_CurrentHealth = m_Settings.targetMaxHealth;
                    };

                    // To know if we have joined to game in the middle of the mayhem mode
                    if (m_CurrentHealth != m_Settings.targetMaxHealth)
                    {
                        m_View.SetHealth(m_CurrentHealth);
                    }
                }
            }
            else
            {
                Debug.LogError("MayhemModeObstacleView not found");
                return;
            }
            
            if (!model.isMine) return;

            m_HostTowerOwnerId = m_AukiWrapper.GetSession().ParticipantId;
            mayhemModeInstance.Init(
                model.entityId, 
                m_NpcEnemyService, 
                m_MayhemObstacleComponentService, 
                m_MayhemUiService, 
                m_Settings, 
                mayhemModeObstacleView.transform, 
                mayhemModeObstacleView, 
                m_SpawnLocationsService,
                m_WaveSettings,
                m_CoroutineRunner,
                m_AukiWrapper);

            m_MayhemModeInstances.Add(model.entityId, mayhemModeInstance);
            m_MayhemModeInstance = mayhemModeInstance;

            mayhemModeInstance.SetOnObstacleShouldBeRemoved(OnObstacleShouldBeRemoved);
            m_IsMayhemRunning = true;
        }

        private void OnObstacleShouldBeRemoved(uint id, int latestWaveNumber)
        {
            m_ObstacleService.RemoveObstacle(id);
            OnMayhemGameOver(false, latestWaveNumber);
        }

        private void OnMayhemGameOver(bool isWin, int latestWaveNumber)
        {
            Debug.Log(latestWaveNumber);
            m_GameOverUiService.ShowGameOverView(isWin, latestWaveNumber);
        }
        
        private void UpdateTowerHealth(int health)
        {
            if (m_View)
            {
                if (health < m_Settings.targetMaxHealth)
                {
                    m_View.SetHealth(health);
                }
            }
            else
            {
                Debug.LogError("MayhemModeObstacleView not found");
            }
        }

        private void OnHostChanged(uint oldHostId, uint newHostId)
        {
            uint myId = m_AukiWrapper.GetSession().ParticipantId;
            if (myId != oldHostId && m_HostTowerOwnerId != oldHostId)
            {
                if (m_IsMayhemRunning)
                {
                    m_InputDialogueService.Show(new DialogueModel(
                        m_LocalisationService.Translate(WARNING_LABEL),
                        m_LocalisationService.Translate(HOST_LEFT_MESSAGE),
                        m_LocalisationService.Translate(OKAY_LABEL), false, () => { m_ConnectionService.NewSession(); }));
                    m_IsMayhemRunning = false;
                }
            }
        }

        // This works only for host
        public void StopMayhemModeOnObstacleRemoved(uint uid)
        {
            if (m_MayhemModeInstances.TryGetValue(uid, out var mayhemModeInstance))
            {
                mayhemModeInstance.ClearOnObstacleDestroy();
                m_MayhemModeInstances.Remove(uid);
                m_MayhemUiService.HideLabels();
            }
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var mayhemModeInstance in m_MayhemModeInstances)
            {
                mayhemModeInstance.Value.Tick(deltaTime);
            }
        }

        [System.Serializable]
        public class WaveSettings
        {
            public float spawnFrequencyMax;
            public float spawnFrequencyMin;
            public int maxNrOfEnemies;
            public int spawnPointCount;

            public List<Enemy> onlyTheseEnemyTypes;
        }
        
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private List<WaveSettings> m_WaveSettings;
            public List<WaveSettings> waveSettings => m_WaveSettings;
            [SerializeField] private float m_TimeBetweenWaves;
            [SerializeField] private int m_SpawnIncreasePerWave;
            [SerializeField] private int m_TargetMaxHealth;
            [SerializeField] private float m_PlayAreaRadius;
            
            public float timeBetweenWaves => m_TimeBetweenWaves;
            public int spawnIncreasePerWave => m_SpawnIncreasePerWave;
            public int targetMaxHealth => m_TargetMaxHealth;
            public float playAreaRadius => m_PlayAreaRadius;
        }
    }
}