using System;
using Matterless.Localisation;

namespace Matterless.Floorcraft
{
    public class QrCodeUiService
    {
        public enum State
        {
            Playing,
            Spectating,
        }
        
        public State state { get; set; }
        private readonly QrCodeUiView m_View;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IMannaService m_MannaService;
        private readonly AudioUiService m_AudioUiService;
        private readonly IAnalyticsService m_AnalyticsService;

        public Action onHideLighthouse;

        public QrCodeUiService(
            IAukiWrapper aukiWrapper,
            IMannaService mannaService, 
            ILocalisationService localisationService,
            IAnalyticsService analyticsService,
            AudioUiService setting,
            HeaderUiService headerUiService,
            ConnectionService connectionService)
        {
            m_AukiWrapper = aukiWrapper;
            m_MannaService = mannaService;
            m_AudioUiService = setting;
            headerUiService.onMultiplayerButtonClicked += ShowLightHouse;
            m_AnalyticsService = analyticsService;
            m_View = QrCodeUiView.Create("UIPrefabs/UIP_QrCodeUiView").Init();
            localisationService.RegisterUnityUIComponents(m_View.gameObject);

            connectionService.onConnectionStateChanged += OnConnectionStateChanged;
            m_View.onBackButtonClicked += HideLightHouse;
            m_View.onScanningButtonPressed += OnScanningButtonPressed;

            //m_AukiWrapper.onLeft += TriggerEndSession;

            m_View.Hide();
        }

        private void OnScanningButtonPressed(bool pressed)
        {
            m_MannaService.ForceHighFrequency(pressed);
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (state != ConnectionState.Connected)
            {
                HideLightHouse();
            }
        }

        private void ShowLightHouse()
        {
            m_MannaService.ShowQRCode();
            m_View.Show();
            PlayShareSound();
            m_AnalyticsService.ShowQRCode(m_AukiWrapper.GetSession().Id);

        }

        private void HideLightHouse()
        {
            m_MannaService.HideQRCode();
            m_View.Hide();
            PlayHideSound();
            try
            {
                m_AnalyticsService.HideQRCode(m_AukiWrapper.GetSession().Id);
            }
            catch
            {
                
            }
            onHideLighthouse?.Invoke();
        }

        private void PlayShareSound() => m_AudioUiService.PlaySelectSound();

        private void PlayHideSound() => m_AudioUiService.PlayBackSound();
    }
}