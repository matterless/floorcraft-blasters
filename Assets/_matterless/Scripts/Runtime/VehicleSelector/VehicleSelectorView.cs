using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class VehicleSelectorView : UIView<VehicleSelectorView>, IVehicleSelectorView
    {
        public event Action<PointerEventData> onDrag;
        public event Action<PointerEventData> onEndDrag;
        public event Action onSpectatorModeClicked;
        public event Action onStoreButtonClicked;
        public event Action onNextButtonClicked;
        public event Action onPreviousButtonClicked;
        public event Action onSelectButtonClicked;
        
        public bool nextButtonEnabled
        {
            get => m_NextButton.interactable;
            set => m_NextButton.interactable = value;
        }
        
        public bool previousButtonEnabled
        {
            get => m_PreviousButton.interactable;
            set => m_PreviousButton.interactable = value;
        }
        
        #region Inspector
        [SerializeField] private Transform m_Parent;
        [SerializeField] private Camera m_Camera;
        [SerializeField] private TextMeshProUGUI m_NameText;
        [SerializeField] private UiHandlerHelper m_HandlerHelper;
        [SerializeField] private Button m_SelectorSelectButton;
        [SerializeField] private Button m_SpectatorModeButton;
        [SerializeField] private Button m_NextButton;
        [SerializeField] private Button m_PreviousButton;

        
        [Header("Premium")] 
        [SerializeField] private Button m_StoreButton;
        [SerializeField] private GameObject m_LockedIndicator;
        #endregion

        public override void Show()
        {
            base.Show();
            m_Camera.gameObject.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            m_Camera.gameObject.SetActive(false);
        }

        public Transform parentTransform => m_Parent;

        public void UpdateView(string name, bool isLocked)
        {
            m_NameText.text = name;
            m_StoreButton.gameObject.SetActive(isLocked);
            m_LockedIndicator.SetActive(isLocked);
            m_SelectorSelectButton.gameObject.SetActive(!isLocked);
        }

        public override VehicleSelectorView Init()
        {
            m_HandlerHelper.onDrag += OnDrag;
            m_HandlerHelper.onEndDrag += OnEndDrag;
            m_SelectorSelectButton.onClick.AddListener(() => onSelectButtonClicked?.Invoke());
            m_SpectatorModeButton.onClick.AddListener(() => onSpectatorModeClicked?.Invoke());
            m_NextButton.onClick.AddListener(()=> onNextButtonClicked?.Invoke());
            m_PreviousButton.onClick.AddListener(()=> onPreviousButtonClicked?.Invoke());
            // add overlay camera to main camera stack
            var cameraData = Camera.main.GetUniversalAdditionalCameraData();
            cameraData.cameraStack.Add(m_Camera);

            m_StoreButton.onClick.AddListener(() => onStoreButtonClicked?.Invoke());
            return this;
        }

        private void OnDrag(PointerEventData data) => onDrag?.Invoke(data);

        private void OnEndDrag(PointerEventData data) => onEndDrag?.Invoke(data);
    }
}