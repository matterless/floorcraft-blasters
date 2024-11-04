using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Matterless.Localisation;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Audio;

namespace Matterless.Floorcraft
{
    public class SettingMenuService
    {
        [System.Serializable]
        public class Settings
        {
            [SerializeField] private AudioMixerGroup m_Master;
            [SerializeField] private AudioMixerGroup m_Music;
            [SerializeField] private AudioMixerGroup m_Sfx;
            [SerializeField] private List<Language> m_SupportLanguages;
            
            public AudioMixerGroup Master => m_Master;
            public AudioMixerGroup Music => m_Music;
            public AudioMixerGroup Sfx => m_Sfx;
            public List<Language>  supportLanguages => m_SupportLanguages;
        }
        
        private const float LowestVolume = -80f;
        private const float LowestCutOffVolume = -44f;
        private const float LowestCutOffValue = 0.1f;
        private const string MasterMixerKey = "master";
        private const string MusicMixerKey = "music";
        private const string SfxMixerKey = "sfx";
        private const string LanguageKey = "language";
        private const string SettingKey = "floorcraftSetting";

        private readonly ILocalisationService m_LocalisationService;
        private readonly IPlayerPrefsService m_PlayerPrefsService;
        private readonly SettingMenuView m_View;
        private readonly AudioUiService m_AudioUiService;
        private readonly Settings m_Settings;

        private Language m_CurrentLanguage;
        private readonly SettingsModel m_PreData;
        private readonly SettingsModel m_CurrentData;
        private readonly Dictionary<Language, LocalisationModel> m_LocalisationModels = new();

        public SettingMenuService(
            IPlayerPrefsService playerPrefsService,
            ILocalisationService localisationService,
            AudioUiService audioUiService,
            Settings setting)
        {
            m_Settings = setting;
            m_PlayerPrefsService = playerPrefsService;
            m_LocalisationService = localisationService;
            m_AudioUiService = audioUiService;
            m_View = SettingMenuView.Create("UIPrefabs/UIP_SettingMenuView").Init();
            m_View.AddLanguageSupport(m_Settings.supportLanguages);
            //LoadAllLocalisationModel(m_Settings.supportLanguages);

            m_View.OnLanguageBtnClickEvent += ()=>
            {
                m_AudioUiService.PlaySelectSound();
                m_View.Inject(m_CurrentData);
                m_View.ShowLanguagePanel();
            };
            m_View.OnLanguagePanelCancelEvent +=()=>
            {
                m_AudioUiService.PlaySelectSound();
                m_View.CloseLanguagePanel();
            };
            m_View.OnLanguagePanelSaveChangeClickEvent += OnLanguageChange;
            m_View.OnMasterVolumeChangeEvent += OnMasterVolumeChange;
            m_View.OnMusicVolumeChangeEvent += OnMusicVolumeChange;
            m_View.OnSFXVolumeChangeEvent += OnSFXVolumeChange;
            m_View.OnSaveChangeClickEvent += OnSaveChangeClick;
            m_View.OnLogoutClickEvent += OnLogoutClick;
            m_View.OnCancelEvent += OnCancelClick;
            m_View.OnLanguageClickEvent += language => m_AudioUiService.PlaySelectSound();

            m_PreData = new SettingsModel();
            m_CurrentData = new SettingsModel();
            InitPlayerSetting();
            localisationService.RegisterUnityUIComponents(m_View.gameObject);
            
            m_View.Hide();
        }

        private void InitPlayerSetting()
        {
            if (!m_PlayerPrefsService.HasKey(SettingKey))
            {
                m_PlayerPrefsService.SetBool(SettingKey,true);
                m_PlayerPrefsService.SetInt(LanguageKey , (int)Language.en_us);
                m_PlayerPrefsService.SetFloat(MasterMixerKey , 1);
                m_PlayerPrefsService.SetFloat(MusicMixerKey , 1);
                m_PlayerPrefsService.SetFloat(MusicMixerKey , 1);
                m_CurrentLanguage = Language.en_us;
            }
           
            GetSettingFromPlayerPref();
            FetchCurrentSettingData();
        }
        public void Show()
        {
            m_PreData.Clone(m_CurrentData);
            m_View.Inject(m_PreData);
            m_View.Show();
        }
        
        public void Hide() => m_View.Hide();
        
        private void GetSettingFromPlayerPref()
        {
            m_CurrentLanguage = (Language)m_PlayerPrefsService.GetInt(LanguageKey, 0);
            SetLanguage(m_CurrentLanguage);
            
            float masterValue = m_PlayerPrefsService.GetFloat(MasterMixerKey, 1);
            m_View.masterVolume = masterValue;
            m_Settings.Master.audioMixer.SetFloat(MasterMixerKey, ValueToAudioVolume(masterValue));
            
            float musicValue = m_PlayerPrefsService.GetFloat(MusicMixerKey, 1);
            m_View.musicVolume = musicValue;
            m_Settings.Music.audioMixer.SetFloat(MusicMixerKey, ValueToAudioVolume(musicValue));
            
            float sfxValue = m_PlayerPrefsService.GetFloat(SfxMixerKey, 1);
            m_View.sfxVolume = sfxValue;
            m_Settings.Sfx.audioMixer.SetFloat(SfxMixerKey, ValueToAudioVolume(sfxValue));
        }

        private void SetLanguage(Language language)
        {
            m_CurrentLanguage = language;
            var textAsset = Resources.Load<TextAsset>($"Locales/{m_CurrentLanguage}");
            var localisationModel = JsonConvert.DeserializeObject<LocalisationModel>(textAsset.text);
            m_LocalisationService.SetLanguage(localisationModel);
        }
        private void FetchCurrentLanguage()
        {
            m_CurrentData.language = m_CurrentLanguage;
        }
        private void FetchCurrentSettingData()
        {
            m_CurrentData.language = m_CurrentLanguage;
            m_CurrentData.masterVolume = m_View.masterVolume;
            m_CurrentData.musicVolume = m_View.musicVolume;
            m_CurrentData.sfxVolume = m_View.sfxVolume;
        }

        private void ApplySetting(SettingsModel data)
        {
            SetLanguage(data.language);
            m_PlayerPrefsService.SetBool(SettingKey,true);
            m_PlayerPrefsService.SetInt(LanguageKey , (int)data.language);
            m_PlayerPrefsService.SetFloat(MasterMixerKey , data.masterVolume);
            m_PlayerPrefsService.SetFloat(MusicMixerKey , data.musicVolume);
            m_PlayerPrefsService.SetFloat(SfxMixerKey , data.sfxVolume);
            
            OnMasterVolumeChange(data.masterVolume);
            OnMusicVolumeChange(data.musicVolume);
            OnSFXVolumeChange(data.sfxVolume);
            PlayerPrefs.Save();
        }

        private void OnLanguageChange(Language select)
        {
            m_CurrentLanguage = select;
            m_AudioUiService.PlaySelectSound();
            m_View.CloseLanguagePanel();
        }
        
        private void OnMasterVolumeChange(float value)
        {
            m_Settings.Master.audioMixer.SetFloat(MasterMixerKey, ValueToAudioVolume(value));
        }

        private void OnMusicVolumeChange(float value)
        {
            m_Settings.Master.audioMixer.SetFloat(MusicMixerKey, ValueToAudioVolume(value));
        }

        private void OnSFXVolumeChange(float value)
        {
            m_Settings.Master.audioMixer.SetFloat(SfxMixerKey, ValueToAudioVolume(value));
        }

        private void OnSaveChangeClick()
        {
            FetchCurrentSettingData();
            m_PreData.Clone(m_CurrentData);
            ApplySetting(m_PreData);
            PlayerPrefs.Save();
            m_AudioUiService.PlaySelectSound();
            m_View.Hide();
        }

        private void OnLogoutClick()
        {
        }

        private void OnCancelClick()
        {
            ApplySetting(m_PreData);
            m_View.Hide();
            m_AudioUiService.PlaySelectSound();
        }

        private float ValueToAudioVolume(float value)
        {
            // Value is between 0 and 1
            value = Mathf.Clamp01(value);
            value = Mathf.Lerp(0.0001f, 1.0f, value); // 0.0001 becomes -80 dB in below formula. Basically silent.
            float volume = Mathf.Log(value) * 20; // decibel. Makes sure that halfway slider will sound half as loud.
            
            return volume;
        }
    }
}