using Matterless.Localisation;
using System;

namespace Matterless.Floorcraft
{

    public class IntroUiService
    {
        private readonly IntroUiView m_View;
        private readonly IConnectionService m_ConnectionService;
        private readonly IRendererService m_RendererService;
        private readonly StoreService m_StoreService;
        private readonly AudioUiService m_AudioUiService;

        public event Action onFreeForAllButtonClicked;
        public event Action onMayhemModeButtonClicked;
        public event Action onMultiplayerButtonClicked;

        public IntroUiService(
            IConnectionService connectionService, 
            ILocalisationService localisationService, 
            IRendererService rendererService,
            InAppPurchaseService inAppPurchaseService,
            StoreService storeService,
            AudioUiService audioUiService)
        {
            // create view
            m_RendererService = rendererService;
            m_StoreService = storeService;
            m_AudioUiService = audioUiService;
            storeService.onPremiumUnlocked += () => StoreButtonVisibility(false);
            m_View = IntroUiView.Create("UIPrefabs/UIP_IntroUiView").Init();
            m_View.Hide();
            m_ConnectionService = connectionService;
            // localise view
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            // register ui events
            m_View.onFreeForAllButtonClicked += OnFreeForAllButtonClicked;
            m_View.onMayhemModeButtonClicked += OnMayhemModeButtonClicked;
            m_View.onMultiplayerButtonClicked += OnMultiplayerButtonClicked;
            m_View.SetMultiplayerButtonInteractability(false);
            m_View.onNewSessionButtonClicked += OnNewSessionClicked;
            m_View.onStoreButtonClicked += OnStoreButtonClicked;
            m_RendererService.EnableDimm();
            
            StoreButtonVisibility(!m_StoreService.isPremiumUnlocked);
            
            connectionService.onConnectionStateChanged += (state) =>
            {
                m_View.SetMultiplayerButtonInteractability(state == ConnectionState.Connected);
            };
        }

        public void Show()
        {
            m_View.Show();
        }

        public void Hide()
        {
            m_View.Hide();
        }

        public void StoreButtonVisibility(bool isVisible)
        {
            if (!m_StoreService.premiumEnabled)
            {
                m_View.StoreButtonVisibility(false);
                m_View.PremiumPanelVisibility(false);
            }
            else
            {
                m_View.StoreButtonVisibility(isVisible);
                m_View.PremiumPanelVisibility(!isVisible);
            }
        }

        private void OnFreeForAllButtonClicked()
        {
            if(!m_ConnectionService.isReady) 
                m_ConnectionService.NewSession();

            onFreeForAllButtonClicked?.Invoke();
            m_AudioUiService.PlaySelectSound();
        }
        
        private void OnMayhemModeButtonClicked()
        {
            if(!m_ConnectionService.isReady) 
                m_ConnectionService.NewSession();
            
            onMayhemModeButtonClicked?.Invoke();
            m_AudioUiService.PlaySelectSound();
        }
        
        private void OnMultiplayerButtonClicked()
        {
            if(!m_ConnectionService.isReady) 
                m_ConnectionService.NewSession();
            
            onMultiplayerButtonClicked?.Invoke();
            m_AudioUiService.PlaySelectSound();
        }
        
        private void OnStoreButtonClicked()
        {
            m_StoreService.Show();
            m_AudioUiService.PlaySelectSound();
        }

        private void OnNewSessionClicked()
        {
            m_ConnectionService.NewSession();
            m_AudioUiService.PlaySelectSound();
        }
    }
}