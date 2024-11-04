using System;
using Matterless.Localisation;

namespace Matterless.Floorcraft
{
    public class GameOverUiService : IGameOverUiService
    {
        private const string MAYHEM_GAME_OVER_LABEL = "MAYHEM_GAME_OVER_LABEL";
        private const string MAYHEM_VICTORY_LABEL = "MAYHEM_VICTORY_LABEL";
        
        private readonly GameOverUiView m_View;
        private readonly ILocalisationService m_LocalisationService;
        private StateMachine.StateMachine m_StateMachine;

        public GameOverUiService(
            ILocalisationService localisationService)
        {
            m_LocalisationService = localisationService;
#if MATTERLESS_APPSTORE
            m_View = GameOverUiView.Create("UIPrefabs/UIP_GameOverUiViewWithoutCoupon").Init();
#else
            m_View = GameOverUiView.Create("UIPrefabs/UIP_GameOverUiView").Init();
#endif
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            localisationService.onLanguageChanged += OnLanguageChanged;
            m_View.Hide();
        }

        private string m_EndGameMessageText;
        private bool m_IsGameEnd;
        public event Action onBackButtonClicked;
        
        private void OnLanguageChanged()
        {
            m_EndGameMessageText = m_LocalisationService.Translate(MAYHEM_GAME_OVER_LABEL);
        }

        public void SetStateMachine(StateMachine.StateMachine stateMachine)
        {
            m_StateMachine = stateMachine;
        }

        public void Hide(bool hideCompletely)
        {
            m_View.Hide();

            if (hideCompletely)
            {
                m_IsGameEnd = false;
            }
        }

        public void Show()
        {
            if (m_IsGameEnd)
            {
                m_View.Show();
            }
        }

        public void ShowGameOverView(bool isWin, int latestWaveNumber)
        {
            /*
            m_EndGameMessageText =
                m_LocalisationService.Translate(isWin ? MAYHEM_VICTORY_LABEL : MAYHEM_GAME_OVER_LABEL);
            m_View.Show(m_EndGameMessageText, latestWaveNumber, onBackButtonClicked);
            */
                m_View.Show(latestWaveNumber, onBackButtonClicked);
                m_IsGameEnd = true;
        }
    }
}