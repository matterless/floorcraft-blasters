using System;
using Auki.ConjureKit;
using Matterless.Inject;
using Matterless.Localisation;
using Matterless.UTools;
using UnityEngine;
using static Matterless.Floorcraft.MarkerService;

namespace Matterless.Floorcraft
{
    public class SpawningService : ITickable
    {
        private const string RESPAWN_RECHARGE_TIME_LEFT_LABEL = "RESPAWN_RECHARGE_TIME_LEFT_LABEL";
        private const string DISCONNECTED_LABEL = "DISCONNECTED_LABEL";
        private const string YOU_ARE_DISCONNECTED_MESSAGE = "YOU_ARE_DISCONNECTED_MESSAGE";
        private const string SCANNING_LABEL = "SCANNING_LABEL";
        private const string LOOK_AROUND_MESSAGE = "LOOK_AROUND_MESSAGE";
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IRaycastService m_RaycastService;
        private readonly IMarkerService m_MarkerService;
        private readonly ILocalisationService m_LocalisationService;
        private readonly MarkerTypeService m_MarkerTypeService;
        private readonly ObstaclesUiService m_ObstaclesUiService;
        private readonly ObstacleService m_ObstacleService;
        private readonly RespawnService m_RespawnService;
        private readonly RespawnService.Settings m_RespawnSettings;
        private readonly PlaceableSelectorService.Settings m_PlaceableSettings;
        private readonly PlaceableSelectorService m_PlaceableSelectorService;
        private readonly SpawningView m_View;
        private readonly IStoreService m_StoreService;
        private readonly AudioUiService m_AudioUiService;
        private readonly SpeederService m_SpeederService;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        
        private Action m_OnSpawn;
        private string m_TimeLeftText;
        private Action m_OnSpectatorMode;
        private bool m_CanSpawn;
        private bool m_LastCanSpawn;
        private bool m_LastCanShowGroundMarker;
        private string m_DisconnectedLabel;
        private string m_YouAreDisconnectedMessage;
        private string m_ScanningLabel;
        private string m_LookAroundMessage;
        private string m_RemoveButtonLabel;
        private bool m_ViewIsActive;
        private bool m_ShowResetObstaclesButton = false;
        private GameMode m_GameMode;
        private float m_MayhemClientWaitingForTowerSince = -1f;
        private const float MAYHEM_CLIENT_SPAWN_FALLBACK_DELAY = 2.5f;

        public SpawningService(
            IAukiWrapper aukiWrapper, 
            IRaycastService raycastService,
            IMarkerService markerService,
            MarkerTypeService markerTypeService,
            ILocalisationService localisationService,
            ObstaclesUiService obstaclesUiService,
            ObstacleService obstacleService,
            RespawnService respawnService,
            RespawnService.Settings respawnSettings,
            PlaceableSelectorService.Settings placeableSettings,
            PlaceableSelectorService placeableSelectorService,
            IStoreService storeService,
            AudioUiService audioUiService,
            SpeederService speederService,
            IUnityEventDispatcher unityEventDispatcher)
        {
            m_AukiWrapper = aukiWrapper;
            m_RaycastService = raycastService;
            m_MarkerService = markerService;
            m_LocalisationService = localisationService;
            
            m_MarkerTypeService = markerTypeService;
            m_ObstaclesUiService = obstaclesUiService;
            m_ObstacleService = obstacleService;

            m_RespawnService = respawnService;
            m_RespawnSettings = respawnSettings;
            m_PlaceableSettings = placeableSettings;
            m_PlaceableSelectorService = placeableSelectorService;
            m_StoreService = storeService;
            m_AudioUiService = audioUiService;
            m_SpeederService = speederService;
            m_UnityEventDispatcher = unityEventDispatcher;
            m_ViewIsActive = false;
            m_View = SpawningView.Create("UIPrefabs/UIP_SpawningView").Init();
            m_View.Hide();
            m_View.onObstacleCreateButtonClicked += m_ObstacleService.OnObstaclesCreateButtonClicked;
            m_View.onResetObstaclesButtonClicked += m_ObstacleService.OnResetObstaclesButtonClicked;
            m_ObstacleService.onObstacleSpawned += OnObstacleSpawned;
            m_ObstacleService.onObstacleRemoved += ObstacleRemoved;
            m_ObstacleService.onMayhemObstacleSpawned += OnMayhemObstacleSpawned;
            m_ObstacleService.onMayhemObstacleRemoved += OnMayhemObstacleRemoved;
            m_StoreService.onPremiumUnlocked += OnPremiumUnlocked;
            m_SpeederService.onSpawn += OnSpawn;
            m_SpeederService.onRespawn += OnSpawn;
            m_UnityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
            m_UnityEventDispatcher.unityOnApplicationFocus += OnApplicationFocus;

            m_AukiWrapper.onJoined += OnJoinedRoom;
            m_View.onSpawnButtonClicked += OnSpawnButtonClicked;
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            localisationService.onLanguageChanged += OnLanguageChanged;
            OnLanguageChanged();
        }

        private void OnMayhemObstacleRemoved(uint obj)
        {
            UpdateRemainingObstaclesLabels();
        }

        private void OnMayhemObstacleSpawned(uint obj)
        {
            UpdateRemainingObstaclesLabels();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                UpdateView();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                UpdateView();
            }
        }

        private void OnLanguageChanged()
        {
            m_TimeLeftText = m_LocalisationService.Translate(RESPAWN_RECHARGE_TIME_LEFT_LABEL);
            m_DisconnectedLabel = m_LocalisationService.Translate(DISCONNECTED_LABEL);
            m_YouAreDisconnectedMessage = m_LocalisationService.Translate(YOU_ARE_DISCONNECTED_MESSAGE);
            m_ScanningLabel = m_LocalisationService.Translate(SCANNING_LABEL);
            m_LookAroundMessage = m_LocalisationService.Translate(LOOK_AROUND_MESSAGE);
            m_RemoveButtonLabel = m_LocalisationService.Translate(m_ObstacleService.selectedPlaceable.removeLocalisationTag);
        }

        public void Show() => m_View.Show();

        public void Show(Action onSpawn)
        {
            m_ViewIsActive = true;
            // Don't reset m_MayhemClientWaitingForTowerSince here: allow fallback timer to persist across
            // enter/exit spawning so the joiner can spawn after 2.5s total even if they open/close the screen.
            // cache callback
            m_OnSpawn = () =>
            {
                if (m_RespawnService.respawnQuantity > 0)
                {
                    onSpawn.Invoke();
                    m_RespawnService.Use();
                }
            };
            // show view
            m_View.Show(m_OnSpawn, m_StoreService.Show);
            // init these for tick update
            m_CanSpawn = false;
            m_LastCanSpawn = false;
            m_LastCanShowGroundMarker = false;
            // show marker
            m_MarkerTypeService.SetType(MarkerType.Tip);
            //m_ObstaclesUiService.ShowButton();
            m_ObstacleService.SetInSpawningScreen(true);
            UpdateView();
        }
        
        public void SetGameMode(GameMode gameMode)
        {
            if (gameMode == GameMode.Mayhem)
            {
                m_ObstacleService.SetPlaceable(m_PlaceableSettings.MayhemObstacle);
            }
            else if (gameMode == GameMode.FreeForAll)
            {
                m_ObstacleService.SetPlaceable(m_PlaceableSettings.FFAObstacle);
            }
            else if (gameMode == GameMode.Multiplayer)
            {
                m_ObstacleService.SetPlaceable(m_PlaceableSettings.FFAObstacle);
            }

            m_GameMode = gameMode;
        }

        private void OnJoinedRoom(Session session)
        {
            // When joining a session that already has other participants we normally switch to Multiplayer
            // (FFA placeables) so we don't assume a mode we didn't choose. Do NOT overwrite if the user
            // already chose Mayhem (e.g. both players chose Mayhem then one joined the other), so client
            // stays in Mayhem and only host can place the tower.
            int participantCount = session != null ? session.GetParticipants().Count : 0;
            if (participantCount > 1 && m_GameMode != GameMode.Mayhem)
            {
                SetGameMode(GameMode.Multiplayer);
            }
        }

        private void OnPremiumUnlocked()
        {
            UpdateView();
        }

        private void OnSpawn()
        {
            UpdateView();
        }

        private void UpdateRemainingObstaclesLabels()
        {
            string removeButtonLabel =
                m_LocalisationService.Translate(m_ObstacleService.selectedPlaceable.removeLocalisationTag);
            int remainingObstacles = m_ObstacleService.RemainingObstacles;
            string remaniningString = m_LocalisationService.Translate(m_ObstacleService.selectedPlaceable.placeLocalisationTag);
            string buttonLabel = $"{remaniningString} ({remainingObstacles}/{m_ObstacleService.maxObstacles})";
            
            // In Mayhem only host can place the tower; clients must wait for host to place and then can spawn vehicle.
            bool isSpawnObstacleButtonInteractable = remainingObstacles > 0 && !m_ObstacleService.HasSpawnedMayhemObstacle && m_ObstacleService.IsMayhemTowerPlacementAllowed;
            bool isResetSpawnObstacleButtonInteractable = m_ObstacleService.HasSpawnedObstacles;
            m_View.SetObstacleButtonLabels(buttonLabel, isSpawnObstacleButtonInteractable, isResetSpawnObstacleButtonInteractable,  removeButtonLabel);
        }

        private void UpdateView()
        {
            var viewModel = new SpawningViewModel {
                unlimitedRespawns = m_RespawnService.unlimitedRespawns,
                rechargeText = $"{m_TimeLeftText} {m_RespawnService.timerView}",
                respawnCount = m_RespawnService.respawnQuantity,
                maxCount = m_RespawnSettings.maxRespawns
            };
            
            m_View.UpdateView(viewModel);
            UpdateRemainingObstaclesLabels();
            //m_ObstaclesUiService.SetCanSpawn(m_CanSpawn);
        }

        private void OnSpawnButtonClicked()
        {
            m_AudioUiService.PlaySelectSound();
        }

        public void Hide()
        {
            m_ObstacleService.SetInSpawningScreen(false);
            m_ViewIsActive = false;
            m_View.Hide();
        }

        public void OnExitView()
        {
            Hide();
        }
        
        private void OnObstacleSpawned(uint entityId)
        {
            UpdateRemainingObstaclesLabels();
        }
        
        private void ObstacleRemoved(uint entityId)
        {
            UpdateRemainingObstaclesLabels();
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            // Compute Mayhem "allowed to spawn" (tower or joiner fallback) independently of ground hit,
            // so the fallback timer runs even when the joiner opens the screen without pointing at the floor.
            bool mayhemAllowedToSpawn = true;
            if (m_GameMode == GameMode.Mayhem)
            {
                mayhemAllowedToSpawn = m_ObstacleService.HasSpawnedMayhemObstacle;
                if (!mayhemAllowedToSpawn && !m_AukiWrapper.isHost && m_AukiWrapper.isConnected)
                {
                    if (m_MayhemClientWaitingForTowerSince < 0f)
                    {
                        m_MayhemClientWaitingForTowerSince = Time.time;
                    }
                    if (Time.time - m_MayhemClientWaitingForTowerSince >= MAYHEM_CLIENT_SPAWN_FALLBACK_DELAY)
                    {
                        mayhemAllowedToSpawn = true;
                    }
                }
                else if (m_ObstacleService.HasSpawnedMayhemObstacle)
                    m_MayhemClientWaitingForTowerSince = -1f;
            }
            else
                m_MayhemClientWaitingForTowerSince = -1f;

            m_CanSpawn = m_RaycastService.hasHit && m_AukiWrapper.isConnected
                && (m_GameMode != GameMode.Mayhem || mayhemAllowedToSpawn);

            // In Mayhem, show the ground marker (tip) as soon as we have a raycast hit so the host can see where to place the tower.
            // Only host can place the tower, so only show the marker for host in Mayhem before tower is placed; after tower is placed everyone can see it (spawn button).
            // In other modes, only show the marker when they can actually spawn (m_CanSpawn).
            bool canShowGroundMarker = m_GameMode == GameMode.Mayhem
                ? (m_RaycastService.hasHit && m_AukiWrapper.isConnected && (m_ObstacleService.HasSpawnedMayhemObstacle || m_AukiWrapper.isHost))
                : m_CanSpawn;

#if UNITY_EDITOR
            // in editor we don't have to check for raycast
            m_CanSpawn = m_AukiWrapper.isConnected;
            if (m_GameMode == GameMode.Mayhem)
                m_CanSpawn = m_ObstacleService.HasSpawnedMayhemObstacle;
            canShowGroundMarker = m_GameMode == GameMode.Mayhem ? (m_AukiWrapper.isConnected && (m_ObstacleService.HasSpawnedMayhemObstacle || m_AukiWrapper.isHost)) : m_CanSpawn;
#endif

            if (m_ViewIsActive && canShowGroundMarker && !m_LastCanShowGroundMarker)
            {
                if (m_MarkerService.isHidden)
                {
                    m_MarkerService.Show();
                }
            }
            if (m_ViewIsActive && !canShowGroundMarker && m_LastCanShowGroundMarker)
            {
                if (!m_MarkerService.isHidden)
                {
                    m_MarkerService.Hide();
                }
            }

            m_View.UpdateScanningStatus(m_CanSpawn, m_RaycastService.hasHit, m_AukiWrapper.isConnected, m_ScanningLabel, m_LookAroundMessage, m_DisconnectedLabel);
            m_LastCanSpawn = m_CanSpawn;
            m_LastCanShowGroundMarker = canShowGroundMarker;
        }
    }
}