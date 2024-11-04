using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class SettingMenuView : UIView<SettingMenuView>
    {
        [SerializeField] private Button m_LanguageBtn;
        [SerializeField] private GameObject m_LanguagePanel;
        [SerializeField] private TextMeshProUGUI m_LanguageLabel;
        [SerializeField] private Button m_LanguageSaveChangeBtn;
        [SerializeField] private Button m_LanguageCancelBtn;
        [SerializeField] private Slider m_MasterVolumeSlider;
        [SerializeField] private Slider m_MusicSlider;
        [SerializeField] private Slider m_SfxSlider;
        [SerializeField] private Button m_SaveChangeBtn;
        [SerializeField] private Button m_CancelBtn;
        [SerializeField] private Button m_LogoutBtn;

        [SerializeField] private LanguageSelectorItemUi m_LanguageSelectorTemplate;
        [SerializeField] private Transform m_LanguageItemContainer;

        public Action<float> OnMasterVolumeChangeEvent;
        public Action<float> OnMusicVolumeChangeEvent;
        public Action<float> OnSFXVolumeChangeEvent;
        public Action OnSaveChangeClickEvent;
        public Action OnCancelEvent;

        public Action<Language> OnLanguageClickEvent;
        public Action OnLanguageBtnClickEvent;
        public Action<Language> OnLanguagePanelSaveChangeClickEvent;
        public Action OnLanguagePanelCancelEvent;
        public Action OnLogoutClickEvent;
        private Dictionary<Language, LanguageSelectorItemUi> m_LanguageSelectorItemUis = new Dictionary<Language, LanguageSelectorItemUi>();


        //to avoid language change
        private Language m_LanguageSelect;
        private Language m_CurrentLanguage;

        public float masterVolume
        {
            get { return m_MasterVolumeSlider.value; }
            set { m_MasterVolumeSlider.value = value; }
        }

        public float musicVolume
        {
            get { return m_MusicSlider.value; }
            set { m_MusicSlider.value = value; }
        }

        public float sfxVolume
        {
            get { return m_SfxSlider.value; }
            set { m_SfxSlider.value = value; }
        }

        public override SettingMenuView Init()
        {
            m_LanguageSelectorTemplate.Hide();

            m_MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChange);
            m_MusicSlider.onValueChanged.AddListener(OnMusicVolumeChange);
            m_SfxSlider.onValueChanged.AddListener(OnSFXVolumeChange);
            m_SaveChangeBtn.onClick.AddListener(OnSaveChangeClick);
            m_LogoutBtn.onClick.AddListener(OnLogoutClick);
            m_CancelBtn.onClick.AddListener(OnCancelClick);
            
            m_LanguageBtn.onClick.AddListener(OnLanguageBtnClick);
            m_LanguageSaveChangeBtn.onClick.AddListener(()=>
            {
                OnLanguagePanelSaveChangeClick(m_LanguageSelect);
            });
          
            m_LanguageCancelBtn.onClick.AddListener(OnLanguagePanelCancel);
            return this;
        }


        public void Inject(SettingsModel data)
        {
            m_LanguagePanel.SetActive(false);
            
            m_LanguageSelect = data.language;
            m_CurrentLanguage = data.language;
            m_LanguageLabel.text = data.language.GetLocalName();

            m_MasterVolumeSlider.value = data.masterVolume;
            m_MusicSlider.value = data.musicVolume;
            m_SfxSlider.value = data.sfxVolume;
            OnLanguageClick(data.language);
        }

        [ContextMenu("Show")]
        public override void Show()
        { 
            base.Show();
            OnLanguageClick(m_CurrentLanguage);
        }

        public void ShowLanguagePanel()
        {
            m_LanguagePanel.SetActive(true);
        }
        public void CloseLanguagePanel()
        {
            m_LanguagePanel.SetActive(false);
        }

        public void AddLanguageSupport(List<Language> supportLanguage)
        {
            foreach (var l in supportLanguage)
            {
                LanguageSelectorItemUi item = Instantiate(m_LanguageSelectorTemplate,m_LanguageItemContainer.transform);
                item.Show();
                item.Init(l,OnLanguageClick);
                m_LanguageSelectorItemUis.Add(l,item);
            }
        }

        private void OnLanguageClick(Language select)
        {
            OnLanguageClickEvent?.Invoke(select);
            foreach (var item in m_LanguageSelectorItemUis)
            {
                item.Value.UpdateView(select);
            }
            m_LanguageSelect = select;
        }

        private void OnMasterVolumeChange(float value)
        {
            OnMasterVolumeChangeEvent?.Invoke(value);
        }

        private void OnMusicVolumeChange(float value)
        {
            OnMusicVolumeChangeEvent?.Invoke(value);
        }

        private void OnSFXVolumeChange(float value)
        {
            OnSFXVolumeChangeEvent?.Invoke(value);
        }

        private void OnSaveChangeClick()
        {
            OnSaveChangeClickEvent?.Invoke();
        }

        private void OnLogoutClick()
        {
            OnLogoutClickEvent?.Invoke();
        }

        private void OnCancelClick()
        {
            OnCancelEvent?.Invoke();
        }

        private void OnLanguageBtnClick()
        {
            OnLanguageBtnClickEvent?.Invoke();
        }

        private void OnLanguagePanelSaveChangeClick(Language select)
        {
            OnLanguagePanelSaveChangeClickEvent?.Invoke(select);
            m_LanguageLabel.text = m_LanguageSelect.GetLocalName();
        }
        private void OnLanguagePanelCancel()
        {
            OnLanguagePanelCancelEvent?.Invoke();
        }
    }
}