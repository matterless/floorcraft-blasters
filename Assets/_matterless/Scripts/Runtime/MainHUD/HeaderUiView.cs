using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class HeaderUiView : UIView<HeaderUiView>
    {
        public event Action onBackButtonClicked;
        public event Action onMultiplayerButtonClicked;
        public event Action onNewSessionButtonClicked;

        [SerializeField] private Button m_BackButton;
        [SerializeField] private Button m_MultiplayerButton;
        
        // ConnectionIndicator
        [SerializeField] private Text m_VersionText;
        [SerializeField] private TMP_Text m_ConnectionText;
        [SerializeField] private RawImage m_RawImage;
        [SerializeField] private Button m_NewSessionButton;

        private bool m_ShowMultiplayerButton;
        
        public override HeaderUiView Init()
        {
            m_BackButton.onClick.AddListener(()=>onBackButtonClicked?.Invoke());
            m_MultiplayerButton.onClick.AddListener(() => onMultiplayerButtonClicked?.Invoke());
            m_NewSessionButton.onClick.AddListener(() => onNewSessionButtonClicked?.Invoke());
            return this;
        }

        public void ShowBackButton()
        {
            m_BackButton.gameObject.SetActive(true);
        }
        
        public void HideBackButton()
        {
            m_BackButton.gameObject.SetActive(false);
        }

        public void SetVersion(string s)
        {
            m_VersionText.text = s;
        }

        public void UpdateConnectionIndicatorUI(Color color, string text)
        {
            // we need to check for null, in order to prevent error on application quit
            if (m_ConnectionText != null)
                m_ConnectionText.text =  text;
            if (m_RawImage != null)
                m_RawImage.color = color;        
        }

        public void UpdateMultiplayerButton(bool getConnectionStatus)
        {
            m_MultiplayerButton.gameObject.SetActive(getConnectionStatus && m_ShowMultiplayerButton);
        }

        public void SetMultiplayerButtonVisibility(bool isVisible)
        {
            m_ShowMultiplayerButton = isVisible;
        }
    }
}