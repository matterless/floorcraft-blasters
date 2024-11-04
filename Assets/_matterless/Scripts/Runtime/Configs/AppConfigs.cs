using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Matterless.Floorcraft
{

    [System.Serializable, CreateAssetMenu(menuName = "Matterless/Floorcraft Configs")]
    public class AppConfigs : ScriptableObject
    {
        #region Inspector
        [SerializeField] private RenderingSettings m_RenderingSettings;
        [SerializeField] private PrivacyPolicyService.Settings m_PrivacyPolicySettings;
        [FormerlySerializedAs("m_StaticLighthouseSettings")]
        [SerializeField] private DomainSettings m_DomainSettings;
        [SerializeField] private HeartbeatService.Settings m_HeartbeatSettings;
        [SerializeField] private AukiSettings m_AukiSettings;
        [SerializeField] private RaycastService.Settings m_RaycastSettings;
        [SerializeField] private ConnectionIndicatorService.Settings m_ConnectionIndicationSettings;
        [SerializeField] private NetworkService.Settings m_NetworkServiceSettings;
        [SerializeField] private VehicleSelectorService.Settings m_VehicleSelectorSettings;
        [SerializeField] private ObstacleService.Settings m_ObstacleSettings;
        [SerializeField] private SplashScreenService.Settings m_SplashScreenServiceSettings;
        [SerializeField] private AudioUiService.Settings m_AudioUiSettings;
        [SerializeField] private SettingMenuService.Settings m_MenuSettings;
        [SerializeField] private CrownService.Settings m_CrownSettings;
        [SerializeField] private ScoreComponentService.Settings m_ScoreSetting;
        [SerializeField] private LeaderboardService.Settings m_LeaderboardSettings;
        [SerializeField] private SpeederSimulation.Settings m_SpeederSimulationSettings;
        [SerializeField] private DashSettings m_DashSettings;
        [SerializeField] private ProximityMineService.Settings m_ProximityMineSettings;
        [SerializeField] private WreckingBallMagnetService.Settings m_WreckingBallMagnetSettings;
        [SerializeField] private FlameThrowerService.Settings m_FlamethrowerSettings;
        [SerializeField] private LaserService.Settings m_LaserServiceSettings;
        [SerializeField] private RespawnService.Settings m_RespawnSettings;
        [SerializeField] private PlaceableSelectorService.Settings m_PlaceableSelectorSettings;
        [SerializeField] private EquipmentService.Settings m_EquipmentServiceSettings;
        [SerializeField] private ShadowCloneService.Settings m_ShadowCloneSettings;
        [SerializeField] private PowerUpSpawnPointService.Settings m_PowerUpSpawnPointsSettings;
        [SerializeField] private HonkService.Settings m_HonkSettings;
        [SerializeField] private RendererService.Settings m_RendererSettings;
        [SerializeField] private NameComponentService.Settings m_NameTagSettings;
        [SerializeField] private NPCEnemyService.Settings m_NPCEnemyServiceSettings;
        [SerializeField] private MayhemModeService.Settings m_MayhemModeSettings;
        
        [SerializeField] private WorldScaleService.Settings m_WorldScaleSettings;
        
        [SerializeField] private AudioClip m_BackgroundMusic;
        [SerializeField] private AudioMixerGroup m_MusicMixerGroup;
        
        [Header("Debug")]
        [SerializeField] private ARPlaneOverlayService.Settings m_ARPlaneOverlaySettings;

        #endregion
        public RenderingSettings renderingSettings => m_RenderingSettings;
        public PrivacyPolicyService.Settings privacyPolicySettings => m_PrivacyPolicySettings;
        public DomainSettings domainSettings => m_DomainSettings;
        public HeartbeatService.Settings heartbeatSettings => m_HeartbeatSettings;
        public AukiSettings aukiSettings => m_AukiSettings;
        public RaycastService.Settings raycastSettings => m_RaycastSettings;
        public ConnectionIndicatorService.Settings connectionIndicationSettings => m_ConnectionIndicationSettings;
        public NetworkService.Settings networkServiceSettings => m_NetworkServiceSettings;
        public VehicleSelectorService.Settings vehicleSelectorSettings => m_VehicleSelectorSettings;
        public PlaceableSelectorService.Settings placeableSelectorSettings => m_PlaceableSelectorSettings;
        public ObstacleService.Settings obstacleSettings => m_ObstacleSettings;
        public SplashScreenService.Settings splashScreenSettings => m_SplashScreenServiceSettings;
        public AudioUiService.Settings audioUiSettings => m_AudioUiSettings;
        public SettingMenuService.Settings menuSettings => m_MenuSettings;
        public ARPlaneOverlayService.Settings arPlaneOverlaySettings => m_ARPlaneOverlaySettings;
        public CrownService.Settings crownSettings => m_CrownSettings;
        public NameComponentService.Settings nameTagSettings => m_NameTagSettings;
        public ScoreComponentService.Settings scoreSetting => m_ScoreSetting;
        public LeaderboardService.Settings leaderboardSettings => m_LeaderboardSettings;
        public SpeederSimulation.Settings speederSimluationSettings => m_SpeederSimulationSettings;
        public DashSettings dashSettings => m_DashSettings;
        public HonkService.Settings honkSettings => m_HonkSettings;
        public ProximityMineService.Settings proximityMineSettings => m_ProximityMineSettings;
        public WreckingBallMagnetService.Settings wreckingBallMagnetSettings => m_WreckingBallMagnetSettings;
        public FlameThrowerService.Settings flamethrowerSettings => m_FlamethrowerSettings;

        public LaserService.Settings laserServiceSettings => m_LaserServiceSettings;
        public RespawnService.Settings respawnSettings => m_RespawnSettings;
        public EquipmentService.Settings equipmentSettings => m_EquipmentServiceSettings;
        public ShadowCloneService.Settings shadowCloneSettings => m_ShadowCloneSettings;
        public PowerUpSpawnPointService.Settings powerUpSpawnPointSettings => m_PowerUpSpawnPointsSettings;
        public RendererService.Settings rendererSettings => m_RendererSettings;
        public NPCEnemyService.Settings npcEnemyServiceSettings => m_NPCEnemyServiceSettings;
        public MayhemModeService.Settings mayhemModeSettings => m_MayhemModeSettings;
        public WorldScaleService.Settings worldScaleSettings => m_WorldScaleSettings;
        public AudioClip backgroundMusic => m_BackgroundMusic;
        public AudioMixerGroup musicMixerGroup => m_MusicMixerGroup;
    }
}
