using Matterless.Localisation;
using System;
using Matterless.Inject;

namespace Matterless.Floorcraft
{
    public class MayhemUiService
    {
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly MayhemUiView m_View; 
        
        public MayhemUiService(
            ILocalisationService localisationService,
            IAukiWrapper aukiWrapper)
        {
            m_AukiWrapper = aukiWrapper;
            m_View = MayhemUiView.Create("UIPrefabs/UIP_MayhemUiView").Init();
            m_View.HideLabels();
            // localise view
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
        }

        public void ShowLabels()
        {
            m_View.ShowLabels();
        }
        
        public void UpdateWaveNumber(int waveNumber)
        {
            m_View.UpdateWaveNumber(waveNumber);
        }

        public void HideLabels()
        {
            m_View.HideLabels();
        }

        // We can use this if we want to show how many seconds left before a new wave starts
        public void UpdateCountdown(int seconds)
        {
            m_View.UpdateCountdown(seconds);
        }
        
        public void SetOnStartButtonClicked(Action action)
        {
            m_View.onStartButtonClicked += action;
        }

        public void ShowButton()
        {
            // Only host may start Mayhem waves; guard here so client never sees the button even if called by mistake.
            if (!m_AukiWrapper.isHost)
            {
                m_View.HideButton();
                return;
            }
            m_View.Show();
            m_View.ShowButton();
        }
        
        public void HideButton()
        {
            m_View.HideButton();
        }

        /// <summary>
        /// Fully hide the Mayhem UI view (e.g. when leaving Gameplay so it doesn't stay on screen in Spawning/Intro).
        /// </summary>
        public void Hide()
        {
            m_View.Hide();
        }
    }
}