using System;
using Matterless.Localisation;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class PrivacyPolicyService
    {
        [System.Serializable]
        public class  Settings
        {
            [SerializeField] private string m_TOS_Link;
            [SerializeField] private string m_PP_Link;
            
            public string TOS_Link => m_TOS_Link;
            public string PP_Link => m_PP_Link;
        }
        
        private const string FLOORCRAFT_PRIVACY_POLICY_KEY = "FLOORCRAFT_PRIVACY_POLICY";

        private readonly PrivacyPolicyView m_View;
        private readonly AudioUiService m_audioUiService;
        private readonly IPlayerPrefsService m_PlayerPrefsService;
        private event Action m_OnAccept;

        public PrivacyPolicyService( 
            Settings settings,
            AudioUiService audioUiService, 
            IPlayerPrefsService playerPrefsService, 
            ILocalisationService localisationService,
            Action onAcceptEvent)
        {
            if(playerPrefsService.GetBool(FLOORCRAFT_PRIVACY_POLICY_KEY, false))
            {
                onAcceptEvent();
                return;
            }

            m_audioUiService = audioUiService;
            m_PlayerPrefsService = playerPrefsService;
            m_OnAccept = onAcceptEvent;
            m_View = PrivacyPolicyView.Create(settings);
            m_View.onAcceptButtonClicked += OnAcceptButtonClicked;
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
        }

        private void OnAcceptButtonClicked()
        {
            m_PlayerPrefsService.SetBool(FLOORCRAFT_PRIVACY_POLICY_KEY, true);
            m_OnAccept?.Invoke();
            m_audioUiService.PlaySelectSound();
            m_View.Dispose();
        }
    }
}