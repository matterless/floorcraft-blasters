using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class UiFlowService : ITickable
    {
        private readonly StateMachine.StateMachine m_StateMachine;
        private readonly HeaderUiService m_HeaderUiService;
        private readonly SidebarUiService m_SidebarUiService;
        private readonly IntroUiService m_IntroUiService;
        private readonly IVehicleSelectorService m_VehicleSelectorService;
        private readonly SpawningService m_SpawningService;
        private readonly SpeederService m_SpeederService;
        private readonly SpeederHUDService m_SpeederHUDService;
        private readonly ConnectionService m_ConnectionService;
        private readonly RespawnService m_RespawnService;
        private readonly IRecordingService m_RecordingService;
        private readonly LeaderboardService m_LeaderboardService;
        private readonly IMarkerService m_MarkerService;
        private readonly IScreenService m_ScreenService;
        private readonly MannaService m_MannaService;
        private readonly MannaService.Settings m_MannaSettings;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly ObstacleService m_ObstacleService;
        private readonly PlaceableSelectorService.Settings m_placeableSettings;
        private readonly MayhemModeService m_MayhemModeService;
        private readonly MayhemUiService m_MayhemUiService;
        private readonly AukiWrapper m_AukiWrapper;
        private readonly IGameOverUiService m_GameOverUiService;

        public enum State
        {
            Intro = 0,
            VehicleSelector = 1,
            Spawning = 2,
            Gameplay = 3,
            Spectator = 4
        }

        private State m_CurrentGameState;
        private GameMode m_GameMode;
        private State m_CurrentState
        {
            get => m_CurrentGameState;
            set
            {
                m_CurrentGameState = value;
                m_ScreenService.OnGameStateChange(value);
            }
        }
        private bool m_QrCodeOverlayOpen;
        
        public UiFlowService(
            HeaderUiService headerUiService,
            SidebarUiService sidebarUiService,
            IntroUiService introUiService,
            IVehicleSelectorService vehicleSelectorService,
            SpawningService spawningService,
            SpeederService speederService,
            SpeederHUDService speederHUDService,
            QrCodeUiService qrCodeUiService,
            ConnectionService connectionService,
            RespawnService respawnService,
            IRecordingService recordingService,
            LeaderboardService leaderboardService,
            IMarkerService markerService,
            IScreenService screenService,
            MayhemModeService mayhemModeService,
            MayhemUiService mayhemUiService,
            AukiWrapper aukiWrapper,
            ObstacleService obstacleService,
            PlaceableSelectorService.Settings placeableSettings,
            MannaService mannaService,
            MannaService.Settings mannaSettings,
            IGameOverUiService gameOverUiService)
        {
            m_StateMachine = new StateMachine.StateMachine(
                new StateMachine.State(State.Intro, IntroOnEnter, IntroOnExit),
                 new StateMachine.State(State.VehicleSelector, VehicleSelectorOnEnter, OnVehicleSelectorCancel),
                  new StateMachine.State(State.Spawning, SpawningOnEnter, SpawningOnExit),
                   new StateMachine.State(State.Gameplay, GameplayOnEnter, GameplayOnExit),
                    new StateMachine.State(State.Spectator, SpectatorOnEnter, SpectatorOnExit)
                );
            
            // header
            m_HeaderUiService = headerUiService;
            m_HeaderUiService.Show(OnBackButtonPressed);
            m_HeaderUiService.onMultiplayerButtonClicked += OnQrCodeOverlayOpen;
            qrCodeUiService.onHideLighthouse += OnQrCodeOverlayClosed;
            m_GameOverUiService = gameOverUiService;
            m_GameOverUiService.onBackButtonClicked += GoSpawningFromMayhem;
            m_GameOverUiService.SetStateMachine(m_StateMachine);

            m_MarkerService = markerService;
            m_ScreenService = screenService;
            m_MayhemModeService = mayhemModeService;
            m_MayhemUiService = mayhemUiService;
            m_SpawningService = spawningService;
            m_ObstacleService = obstacleService;
            m_placeableSettings = placeableSettings;
            m_AukiWrapper = aukiWrapper;
            m_MannaService = mannaService;
            m_MannaSettings = mannaSettings;
            m_ScreenService.OnScreenOrientationChanged += OnScreenOrientationChanged;
            
            // sidebar
            m_SidebarUiService = sidebarUiService;

            // intro
            m_IntroUiService = introUiService;
            m_IntroUiService.onFreeForAllButtonClicked += () =>
            {
                m_GameMode = GameMode.FreeForAll;
                m_SpawningService.SetGameMode(GameMode.FreeForAll);
                m_MayhemModeService.isInHayhemMode = false;
                m_StateMachine.SwitchState((int)State.VehicleSelector);
            };
            m_IntroUiService.onMayhemModeButtonClicked += () =>
            {
                m_GameMode = GameMode.Mayhem;
                m_SpawningService.SetGameMode(GameMode.Mayhem);
                m_MayhemModeService.isInHayhemMode = true;        
                m_StateMachine.SwitchState((int)State.VehicleSelector);
            };
            
            m_IntroUiService.onMultiplayerButtonClicked += () =>
            {
                m_GameMode = GameMode.Multiplayer;
                m_SpawningService.SetGameMode(GameMode.Multiplayer);
                m_HeaderUiService.OnMultiplayerButtonClicked();
            };
            
            m_RecordingService = recordingService;
            m_SpawningService = spawningService;
            m_SpeederService = speederService;
            m_SpeederService.onRespawn += () =>
            {
                //qrCodeUiService.ShowIngameButtons();
                //obstaclesService.SetInSpawnScreen(false);
            };
            m_SpeederService.onKill += () =>
            {
                //qrCodeUiService.HideIngameButtons();
                //obstaclesService.SetInSpawnScreen(true);
                
                m_StateMachine.SwitchState((int)State.Spawning);
            };
            m_SpeederHUDService = speederHUDService;
            m_ConnectionService = connectionService;
            m_RespawnService = respawnService;
            m_IntroUiService.Hide();

            // selector
            m_VehicleSelectorService = vehicleSelectorService;
            m_VehicleSelectorService.Hide();
            
            // leaderboard
            m_LeaderboardService = leaderboardService;
            
            connectionService.onConnectionStateChanged += OnConnectionStateChanged;
            
            // start state machine
            // TODO:: we can check for deep link and start UI from another state
            m_StateMachine.Start((int)State.Intro);
        }

        private void GoSpawningFromMayhem()
        {
            m_SpeederService.Despawn(true);
            m_RespawnService.AddOneRespawn();
            
            if (m_CurrentState == State.Gameplay)
            {
                m_StateMachine.SwitchState((int)State.Spawning);
            }
        }

        private void OnBackButtonPressed(int state)
        {
            if (state == -1)
            {
                return;
            }

            if (state == (int)State.Intro)
            {
                m_ConnectionService.NewSession();
            }
            if (state == (int)State.Spawning)
            {
                m_SpeederService.Despawn(true);
                m_RespawnService.AddOneRespawn();
            }
            
            m_StateMachine.SwitchState(state);
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state != ConnectionState.Connected)
            {
                m_StateMachine.SwitchState((int)State.Intro);
            }
            else if (!m_AukiWrapper.isHost)
            {
                m_StateMachine.SwitchState((int)State.VehicleSelector);
            }
        }
        
        #region Intro
        private void IntroOnEnter()
        {
            m_HeaderUiService.Show();
            m_HeaderUiService.ClearBack();
            m_IntroUiService.Show();
            m_MarkerService.Hide();
            m_GameOverUiService.Hide(true);
            
            m_MayhemModeService.isInHayhemMode = false;
            
            m_CurrentState = State.Intro;
        }
        private void IntroOnExit()
        {
            m_IntroUiService.Hide();
        }
        #endregion

        #region VehicleSelector
        private void VehicleSelectorOnEnter()
        {
            m_LeaderboardService.HideLeaderBoard();
            m_HeaderUiService.Show();
            m_HeaderUiService.SetBack((int)State.Intro);
            m_VehicleSelectorService.Show(
                OnVehicleSelected,
                () => m_StateMachine.SwitchState((int)State.Spectator));
            m_CurrentState = State.VehicleSelector;
            m_MarkerService.Hide();
            m_GameOverUiService.Hide(true);
            m_ObstacleService.RemoveMayhemPlacementArea();
        }

        private void OnVehicleSelected(Vehicle obj)
        {
            // Save the vehicle in SpeederService
            m_SpeederService.SetServerVehicle(obj);
            m_StateMachine.SwitchState((int)State.Spawning);
        }

        private void OnVehicleSelectorCancel()
        {
            m_VehicleSelectorService.Hide(); 
        }
        #endregion

        #region Spawning
        private void SpawningOnEnter()
        {
            m_SidebarUiService.SetOrientation(ScreenOrientation.Portrait);
            m_HeaderUiService.SetBack((int)State.VehicleSelector);
            
            m_CurrentState = State.Spawning;

            if (m_QrCodeOverlayOpen)
            {
                return;
            }
            
            m_LeaderboardService.ShowLeaderBoard();
            m_SpawningService.Show(() => m_StateMachine.SwitchState((int)State.Gameplay));
            m_HeaderUiService.Show();
            m_GameOverUiService.Hide(true);
        }
        
        private void SpawningOnExit()
        {
            m_SpawningService.OnExitView();
        }
        #endregion

        #region Gameplay
        private void GameplayOnEnter()
        {
            m_LeaderboardService.ShowLeaderBoard();
            m_HeaderUiService.Show();
            m_HeaderUiService.SetBack((int)State.Spawning);
            m_SpeederService.SpawnVehicle();
            m_SidebarUiService.Show();
            m_SpeederHUDService.Show();
            m_GameOverUiService.Hide(true);

            //switch(m_MannaSettings.scanningType)
            //{
            //    case MannaService.ScanningType.Always:
            //        m_MannaService.SetScanningFrequency(MannaService.FrequencyType.Low);
            //        break;
            //    case MannaService.ScanningType.InGame:
            //        m_MannaService.SetScanningFrequency(MannaService.FrequencyType.Mid);
            //        m_MannaService.StartScanning();
            //        break;
            //}
            
            if (m_MayhemModeService.isInHayhemMode && m_AukiWrapper.isHost && !m_MayhemModeService.isMayhemStartedForHost)
            {
                m_MayhemUiService.ShowButton();
            }
            
            m_CurrentState = State.Gameplay;
        }

        private void GameplayOnExit()
        {
            m_MayhemUiService.HideButton();
            m_HeaderUiService.HideHorizontalView();
            m_LeaderboardService.SetOrientation(ScreenOrientation.Portrait);
            m_SidebarUiService.SetOrientation(ScreenOrientation.Portrait);

            //switch (m_MannaSettings.scanningType)
            //{
            //    case MannaService.ScanningType.Always:
            //        m_MannaService.SetScanningFrequency(MannaService.FrequencyType.Mid);
            //        break;
            //    case MannaService.ScanningType.InGame:
            //        m_MannaService.StopScanning();
            //        break;
            //}
        }

        #endregion

        #region Spectator
        private void SpectatorOnEnter()
        {
            m_LeaderboardService.ShowLeaderBoard();
            m_HeaderUiService.SetBack((int)State.VehicleSelector);
            m_SidebarUiService.Show();
            m_GameOverUiService.Hide(true);
            
            m_CurrentState = State.Spectator;
        }

        private void SpectatorOnExit()
        {
            m_HeaderUiService.HideHorizontalView();
            m_LeaderboardService.SetOrientation(ScreenOrientation.Portrait);
            m_SidebarUiService.SetOrientation(ScreenOrientation.Portrait);
        }

        #endregion

        #region QROverlay
        private void OnQrCodeOverlayOpen()
        {
            m_QrCodeOverlayOpen = true;
            HideAllUi();
        }

        private void OnQrCodeOverlayClosed()
        {
            m_QrCodeOverlayOpen = false;

            var currentOrientation = m_ScreenService.GetScreenOrientation();

            switch (m_CurrentState)
            {
                case State.Gameplay:
                    
                    if (currentOrientation == ScreenOrientation.Portrait || currentOrientation == ScreenOrientation.PortraitUpsideDown)
                    {
                        m_SidebarUiService.Show();
                        m_HeaderUiService.Show();
                    }
                    else
                    {
                        m_SidebarUiService.ShowHorizontal();
                        m_HeaderUiService.ShowHorizontal();
                    }
                    
                    m_LeaderboardService.ShowLeaderBoard();
                    m_GameOverUiService.Show();
                    break;
                case State.Spectator:
                    
                    if (currentOrientation == ScreenOrientation.Portrait || currentOrientation == ScreenOrientation.PortraitUpsideDown)
                    {
                        m_SidebarUiService.Show();
                        m_HeaderUiService.Show();
                    }
                    else
                    {
                        m_SidebarUiService.ShowHorizontal();
                        m_HeaderUiService.ShowHorizontal();
                    }
                    
                    m_LeaderboardService.ShowLeaderBoard();
                    break;
                case State.Intro:
                    m_IntroUiService.Show();
                    m_HeaderUiService.Show();
                    break;
                case State.VehicleSelector:
                    m_VehicleSelectorService.Show();
                    m_HeaderUiService.Show();
                    break;
                case State.Spawning:
                    m_LeaderboardService.ShowLeaderBoard();
                    m_SpawningService.Show(
                        () => m_StateMachine.SwitchState((int)State.Gameplay));
                    m_HeaderUiService.Show();

                    break;
            }
        }
        #endregion

        #region Utility
        private void HideAllUi()
        {
            m_LeaderboardService.HideLeaderBoard();
            m_HeaderUiService.Hide();
            m_IntroUiService.Hide();
            m_SpawningService.Hide();
            m_VehicleSelectorService.Hide();
            m_GameOverUiService.Hide(false);
        }

        private void OnScreenOrientationChanged(ScreenOrientation orientation)
        {
            Debug.Log("Screen orientation changed to: " + orientation);

            //if (m_CurrentState == State.Gameplay)
            {
                m_SpeederHUDService.SetOrientation(orientation);
            }

            m_HeaderUiService.SetOrientationOnGameplay(orientation);
            m_SidebarUiService.SetOrientation(orientation);
            m_LeaderboardService.SetOrientation(orientation);
        }

        #endregion

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            m_StateMachine.Update(deltaTime, unscaledDeltaTime);

#if UNITY_EDITOR
            // For editor testing
            if (Input.GetKeyDown(KeyCode.H))
            {
                OnScreenOrientationChanged(ScreenOrientation.LandscapeRight);
            }
            else if (Input.GetKeyDown(KeyCode.V))
            {
                OnScreenOrientationChanged(ScreenOrientation.Portrait);
            }
#endif
        }
    }
}