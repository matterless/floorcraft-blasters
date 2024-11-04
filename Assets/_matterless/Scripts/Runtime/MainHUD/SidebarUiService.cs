using System;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SidebarUiService
    {
        private Action m_OnSettingsButtonClicked;
        private Action m_OnScreenshotButtonClicked;
        private Action m_OnRecordButtonClicked;
        private readonly SidebarUiView m_VerticalView;
        private readonly SidebarUiView m_HorizontalView;
        private IRecordingService m_RecordingService;
        private SettingMenuService m_SettingMenuService;
        private IUnityEventDispatcher m_UnityEventDispatcher;
        private IPlayerPrefsService m_PlayerPrefsService;
        private const string VerticalFloatingMenuXPositionKey = "verfloatingmenuxpos";
        private const string VerticalFloatingMenuYPositionKey = "verfloatingmenuypos";
        private const string HorizontalFloatingMenuXPositionKey = "horfloatingmenuxpos";
        private const string HorizontalFloatingMenuYPositionKey = "horfloatingmenuypos";

        public SidebarUiService(
            IRecordingService recordingService, 
            SettingMenuService settingMenuService,
            IUnityEventDispatcher unityEventDispatcher,
            IPlayerPrefsService playerPrefsService
            )
        {
            // Instantiate SidebarUiView
            m_VerticalView = SidebarUiView.Create("UIPrefabs/UIP_SidebarView").Init();
            m_HorizontalView = SidebarUiView.Create("UIPrefabs/UIP_SidebarView_Horizontal").Init();
            m_RecordingService = recordingService;
            m_SettingMenuService = settingMenuService;
            m_PlayerPrefsService = playerPrefsService;

            m_RecordingService.OnRecordingStarted += OnRecordingStarted;
            m_RecordingService.OnRecordingStopped += OnRecordingStopped;
            m_RecordingService.OnRecordingProgress += OnRecordingProgress;
            
            m_VerticalView.onSettingsButtonClicked += OnSettingsButtonClicked;
            m_VerticalView.onScreenshotButtonClicked += OnScreenshotButtonClicked;
            m_VerticalView.onRecordButtonClicked += OnRecordButtonClicked;
            m_VerticalView.onMenuButtonClicked += OnMenuButtonClicked;
            Show();

            m_HorizontalView.onSettingsButtonClicked += OnSettingsButtonClicked;
            m_HorizontalView.onScreenshotButtonClicked += OnScreenshotButtonClicked;
            m_HorizontalView.onRecordButtonClicked += OnRecordButtonClicked;
            m_HorizontalView.onMenuButtonClicked += OnMenuButtonClicked;
            m_HorizontalView.Hide();
            
            m_UnityEventDispatcher = unityEventDispatcher;
            m_UnityEventDispatcher.unityApplicationQuit += OnApplicationQuit;
            SetInitialMenuPosition();
        }

        public void Show()
        {
            m_VerticalView.Show();
        }
        
        public void ShowHorizontal()
        {
            m_HorizontalView.Show();
        }

        public void SetOrientation(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
            {
                m_VerticalView.Show();
                m_HorizontalView.Hide();
            }
            else if (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight)
            {
                m_VerticalView.Hide();
                m_HorizontalView.Show();
            }
        }

        void OnSettingsButtonClicked()
        {
            m_SettingMenuService.Show();
        }
        
        void OnScreenshotButtonClicked()
        {
            m_RecordingService.TakeScreenshot();
        }

        void OnRecordButtonClicked()
        {
            if (!m_RecordingService.IsRecording)
            {
                m_RecordingService.StartRecording();
            }
            else
            {
                m_RecordingService.StopRecording();
            }
        }

        void OnMenuButtonClicked()
        {
            m_HorizontalView.OpenCloseMenu();
            m_VerticalView.OpenCloseMenu();
        }

        void CacheMenuPositionPreference()
        {
            m_PlayerPrefsService.SetFloat(VerticalFloatingMenuXPositionKey, m_VerticalView.Position.x);
            m_PlayerPrefsService.SetFloat(VerticalFloatingMenuYPositionKey, m_VerticalView.Position.y);
            m_PlayerPrefsService.SetFloat(HorizontalFloatingMenuXPositionKey, m_HorizontalView.Position.x);
            m_PlayerPrefsService.SetFloat(HorizontalFloatingMenuYPositionKey, m_HorizontalView.Position.y);
        }

        void SetInitialMenuPosition()
        {
            if (m_PlayerPrefsService.HasKey(VerticalFloatingMenuXPositionKey) &&
                m_PlayerPrefsService.HasKey(VerticalFloatingMenuYPositionKey))
            {
                Vector2 verticalPosition = new Vector2(m_PlayerPrefsService.GetFloat(VerticalFloatingMenuXPositionKey, 0),
                    m_PlayerPrefsService.GetFloat(VerticalFloatingMenuYPositionKey, 0));
                m_VerticalView.SetFloatingButtonPosition(verticalPosition);
            }
            
            if (m_PlayerPrefsService.HasKey(HorizontalFloatingMenuXPositionKey) &&
                m_PlayerPrefsService.HasKey(HorizontalFloatingMenuYPositionKey))
            {
                Vector2 horizontalPosition = new Vector2(m_PlayerPrefsService.GetFloat(HorizontalFloatingMenuXPositionKey, 0),
                    m_PlayerPrefsService.GetFloat(HorizontalFloatingMenuYPositionKey, 0));
                m_HorizontalView.SetFloatingButtonPosition(horizontalPosition);
            }
        }

        void OnRecordingStarted()
        {
            m_VerticalView.ToggleRecordButton(m_RecordingService.IsRecording);
            m_HorizontalView.ToggleRecordButton(m_RecordingService.IsRecording);
        }

        void OnRecordingStopped()
        {
            m_VerticalView.ToggleRecordButton(m_RecordingService.IsRecording);
            m_HorizontalView.ToggleRecordButton(m_RecordingService.IsRecording);
        }
        
        private void OnRecordingProgress(float timePassed, float maxTime, float t)
        {
            m_VerticalView.SetRecordingProgress(timePassed, maxTime, t);
            m_HorizontalView.SetRecordingProgress(timePassed, maxTime, t);
        }

        #region  UNITYLifeCycle

        void OnApplicationQuit()
        {
            CacheMenuPositionPreference();
        }

        #endregion
    }
}