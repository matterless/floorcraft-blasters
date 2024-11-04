using System;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class HeaderUiService : ITickable
    {
        public Action onMultiplayerButtonClicked;

        private int m_BackStateId = -1;
        private Action<int> m_OnBackButton;
        private readonly HeaderUiView m_VerticalView;
        private readonly HeaderUiView m_HorizontalView;
        private readonly ConnectionService m_ConnectionService;
        private readonly AudioUiService m_AudioUiService;
        private readonly INetworkService m_NetworkService;

        public HeaderUiService(
            ConnectionService connectionService,
            AudioUiService audioUiService,
            INetworkService networkService)
        {
            m_ConnectionService = connectionService;
            m_AudioUiService = audioUiService;
            m_NetworkService = networkService;
            //m_View = HeaderUiView.Create();
//            m_View.Init();
            // Instantiate HeaderUiView
            m_VerticalView = HeaderUiView.Create("UIPrefabs/UIP_HeaderView").Init();
            m_VerticalView.onBackButtonClicked += OnBackButtonClicked;
            m_VerticalView.onMultiplayerButtonClicked += OnMultiplayerButtonClicked;
            m_VerticalView.onNewSessionButtonClicked += OnNewSessionClicked;
            
            m_HorizontalView = HeaderUiView.Create("UIPrefabs/UIP_HeaderView_Horizontal").Init();
            m_HorizontalView.onBackButtonClicked += OnBackButtonClicked;
            m_HorizontalView.onMultiplayerButtonClicked += OnMultiplayerButtonClicked;
            m_HorizontalView.onNewSessionButtonClicked += OnNewSessionClicked;
            m_HorizontalView.Hide();

            m_ConnectionService.onConnectionStateChanged += OnConnectionStateChanged;
            m_NetworkService.onNetworkConnectionChanged += OnConnectionStatusChanged;
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            bool isReady = state == ConnectionState.Connected;

            m_VerticalView.SetMultiplayerButtonVisibility(isReady);
            m_HorizontalView.SetMultiplayerButtonVisibility(isReady);
        }

        private void OnConnectionStatusChanged(ConnectionStatus connectionStatus)
        {
            bool isReady = connectionStatus == ConnectionStatus.Connected;

            m_VerticalView.SetMultiplayerButtonVisibility(isReady);
            m_HorizontalView.SetMultiplayerButtonVisibility(isReady);
        }
        
        public void SetBack(int stateId)
        {
            m_BackStateId = stateId;
            m_VerticalView.ShowBackButton();
            m_HorizontalView.ShowBackButton();
        }

        public void ClearBack()
        {
            m_BackStateId = -1;
            m_VerticalView.HideBackButton();
            m_HorizontalView.HideBackButton();
        }

        public void Show()
        {
            m_VerticalView.Show();
        }
        
        public void Show(Action<int> onBackButton)
        {
            m_OnBackButton = onBackButton;
            m_VerticalView.Show();
        }
        
        public void ShowHorizontal()
        {
            m_HorizontalView.Show();
        }
        
        public void Hide()
        {
            m_VerticalView.Hide();
            m_HorizontalView.Hide();
        }

        public void HideHorizontalView()
        {
            m_HorizontalView.Hide();
        }

        public void SetOrientationOnGameplay(ScreenOrientation orientation)
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

        void OnBackButtonClicked()
        {
            m_OnBackButton?.Invoke(m_BackStateId);
            m_AudioUiService.PlayBackSound();
        }
        
        public void OnMultiplayerButtonClicked()
        {
            onMultiplayerButtonClicked?.Invoke();
            m_AudioUiService.PlaySelectSound();
        }

        #region ConnecionIndicator
        public void SetVersion(string s)
        {
            m_VerticalView.SetVersion(s);
            m_HorizontalView.SetVersion(s);
        }

        public void UpdateConnectionIndicatorUI(Color settingsDisconnectColor, string translate)
        {
            m_VerticalView.UpdateConnectionIndicatorUI(settingsDisconnectColor, translate);
            m_HorizontalView.UpdateConnectionIndicatorUI(settingsDisconnectColor, translate);
        }
        
        private void OnNewSessionClicked()
        {
            m_ConnectionService.NewSession();
            m_AudioUiService.PlaySelectSound();
        }
        #endregion

        // TODO: Refactor this into event
        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            m_VerticalView.UpdateMultiplayerButton(m_ConnectionService.GetConnectionStatus());
            m_HorizontalView.UpdateMultiplayerButton(m_ConnectionService.GetConnectionStatus());
        }
    }
}