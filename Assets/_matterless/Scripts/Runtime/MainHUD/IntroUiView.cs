using System;
using UnityEngine;
using UnityEngine.Serialization;
using Button = Matterless.Module.UI.Button;
namespace Matterless.Floorcraft
{
    public class IntroUiView : UIView<IntroUiView>
    {
        public event Action onFreeForAllButtonClicked;
        public event Action onMayhemModeButtonClicked;
        public event Action onMultiplayerButtonClicked;
        public event Action onNewSessionButtonClicked;
        public event Action onStoreButtonClicked;

        #region Inspector
        [FormerlySerializedAs("m_IntroButton1")] [SerializeField] private Button m_FreeForAllButton;
        [FormerlySerializedAs("m_IntroButton2")] [SerializeField] private Button m_MayhemModeButton;
        [FormerlySerializedAs("m_IntroButton3")] [SerializeField] private Button m_MultiplayerButton;
        [SerializeField] private Button m_NewSessionButton;
        [SerializeField] private Button m_StoreButton;
        [SerializeField] private GameObject m_PremiumPanel;
        #endregion

        public override IntroUiView Init()
        {
            AddListeners();
            return this;
        }

        private void AddListeners()
        {
            m_FreeForAllButton.onClick.AddListener(() => onFreeForAllButtonClicked?.Invoke());
            m_MayhemModeButton.onClick.AddListener(() => onMayhemModeButtonClicked?.Invoke());
            m_MultiplayerButton.onClick.AddListener(() => onMultiplayerButtonClicked?.Invoke());
            m_NewSessionButton.onClick.AddListener(() => onNewSessionButtonClicked?.Invoke());
            m_StoreButton.onClick.AddListener(() => onStoreButtonClicked?.Invoke());
        }

        public void StoreButtonVisibility(bool isVisible)
        {
            m_StoreButton.gameObject.SetActive(isVisible);
        }
        
        public void PremiumPanelVisibility(bool isVisible)
        {
            m_PremiumPanel.SetActive(isVisible);
        }

        public void SetMultiplayerButtonInteractability(bool isClickable)
        {
            m_MultiplayerButton.interactable = isClickable;
        }
    }
}