using System;
using Matterless.Inject;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class WalletUiService
    {
        private readonly WalletService m_WalletService;
        private readonly AudioUiService m_AudioUiService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly WalletUiView m_View;

        public WalletUiService(WalletService walletService, AudioUiService audioUiService, IAnalyticsService analyticsService){
            m_WalletService = walletService;
            m_AudioUiService = audioUiService;
            m_AnalyticsService = analyticsService;
            
            // Create the view
            m_View = WalletUiView.Create("UIPrefabs/UIP_WalletView").Init();
            
            // Wire up events
            m_View.onConnectWalletButtonClicked += OnConnectWalletButtonClicked;
            m_View.onDisconnectWalletButtonClicked += OnDisconnectWalletButtonClicked;
            
            // Subscribe to wallet state changes
            m_WalletService.onWalletConnected += OnWalletConnected;
            m_WalletService.onWalletDisconnected += OnWalletDisconnected;
            m_WalletService.onModalStateChanged += OnModalStateChanged;
            
            // Hide by default - will be shown by UiFlowService when in Intro state
            m_View.Hide();
            
            // Set initial state (show connect button, hide disconnect button)
            m_View.SetConnectButtonVisibility(true);
            m_View.SetDisconnectButtonVisibility(false);
            m_View.HideWalletInfo();
        }

        private void OnConnectWalletButtonClicked()
        {
            m_WalletService.Connect();
            m_AudioUiService.PlaySelectSound();
            m_View.SetConnectButtonInteractability(false);
        }

        private void OnDisconnectWalletButtonClicked()
        {
            m_WalletService.Disconnect();
            m_AudioUiService.PlaySelectSound();
            m_View.SetDisconnectButtonInteractability(false);
        }

        private void OnWalletConnected()
        {
            m_View.SetConnectButtonVisibility(false);
            m_View.SetDisconnectButtonVisibility(true);
            
            string address = m_WalletService.GetConnectedAddress();
            m_View.SetWalletAddressText(address);
            
            // Track wallet connection for user analytics
            m_AnalyticsService.SetWalletAddress(address);
            
            m_View.ShowWalletInfo();
            m_View.SetConnectButtonInteractability(true);
            m_View.SetDisconnectButtonInteractability(true);
        }

        private void OnWalletDisconnected()
        {
            // Track wallet disconnection for user analytics
            m_AnalyticsService.ClearWalletAddress();
            
            m_View.SetConnectButtonVisibility(true);
            m_View.SetDisconnectButtonVisibility(false);
            m_View.SetWalletAddressText("");
            m_View.HideWalletInfo();
        }

        public void Show()
        {
            m_View.Show();
        }

        public void Hide()
        {
            m_View.Hide();
        }

        private void OnModalStateChanged(bool isOpen)
        {
            m_View.SetConnectButtonInteractability(!isOpen);
            m_View.SetDisconnectButtonInteractability(!isOpen);
        }
    }
}