using Matterless.Inject;
using Matterless.Localisation;
using Matterless.Module.UI;
using Matterless.Module.RemoteConfigs;
using Matterless.UTools;
using Newtonsoft.Json;
using System.Collections;
using Auki.ConjureKit;
using UnityEngine;

namespace Matterless.Floorcraft
{

    public class Bootstrap
    {
        private readonly IRemoteConfigService m_RemoteConfigService;
        private readonly ICoroutineRunner m_CoroutineRunner;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        private readonly ILocalisationService m_LocalisationService;
        private readonly AudioUiService m_AudioUiService;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly INetworkService m_NetworkService;
        private readonly IInputDialogueService m_InputDialogueService;
        private readonly ObjectContext m_AppContext;
        private readonly ObjectContext m_UiContext;
        private readonly ObjectContext m_DebugContext;
        private readonly AppConfigs m_AppConfigs;
        
        private ulong m_AukiErrorDialogueIndex;
        private bool m_WaitForRemoteConfigs = true;

        public Bootstrap(
            // dependencies
            // use all dependencies to ensure that this will start at the end
            IRemoteConfigService remoteConfigService,
            IPlayerPrefsService playerPrefsService,
            ICoroutineRunner coroutineRunner,
            IUnityEventDispatcher unityEventDispatcher,
            ILocalisationService localisationService,
            AudioUiService audioUiService,
            IAukiWrapper aukiWrapper,
            INetworkService networkService,
            IInputDialogueService inputDialogueService,
            // arguments
            ObjectContext appContext,
            ObjectContext uiContext,
            ObjectContext debugContext,
            AppConfigs appConfigs
            )
        {
            m_RemoteConfigService = remoteConfigService;
            m_CoroutineRunner = coroutineRunner;
            m_UnityEventDispatcher = unityEventDispatcher;
            m_LocalisationService = localisationService;
            m_AudioUiService = audioUiService;
            m_AukiWrapper = aukiWrapper;
            m_NetworkService = networkService;
            m_InputDialogueService = inputDialogueService;
            m_AppContext = appContext;
            m_UiContext = uiContext;
            m_DebugContext = debugContext;
            m_AppConfigs = appConfigs;

            // Here we can get remote configs
            m_RemoteConfigService.RegisterConfig("app", (json) => JsonUtility.FromJsonOverwrite(json, m_AppConfigs));
            //Debug.Log("Get remote configs...");
            // TODO:: we may need to retry get remote configs on error
            //m_RemoteConfigService.GetRemoteConfigs(OnRemoteConfigCompleted, OnRemoteConfigCompleted,"app");
            // Don't wait for remote configs
            OnRemoteConfigCompleted();

            // TODO:: check version compatibility

            // Localisation
            var en_us_TextAsset = Resources.Load<TextAsset>("Locales/en_us");
            var localisationModel = JsonConvert.DeserializeObject<LocalisationModel>(en_us_TextAsset.text);
            localisationService.SetLanguage(localisationModel);

            // privacy policy
            PrivacyPolicyService pps = new PrivacyPolicyService(
                m_AppConfigs.privacyPolicySettings,
                audioUiService,
                playerPrefsService,
                localisationService,
                onAcceptEvent: InstallAuki);
        }

        private void OnRemoteConfigCompleted()
        {
            Debug.Log("Remote config downloaded.");
            m_WaitForRemoteConfigs = false;
        }

        private void InstallAuki()
        {
            m_AukiWrapper.Install(
                () =>
                {
                    m_InputDialogueService.Hide(m_AukiErrorDialogueIndex);
                    m_CoroutineRunner.StartUnityCoroutine(StartApp());
                },
                AukiInstallFailed);
        }

        private void AukiInstallFailed()
        {
             m_InputDialogueService.Show(new DialogueModel(
                m_LocalisationService.Translate("NETWORK_ERROR_LABEL"),
                m_LocalisationService.Translate("NETWORK_ERROR_MESSAGE"), 
                m_LocalisationService.Translate("RETRY_LABEL"), false,
                () => m_CoroutineRunner.StartUnityCoroutine(WaitAndInstallAuki())));
        }

        IEnumerator WaitAndInstallAuki()
        {
            m_AukiErrorDialogueIndex = m_InputDialogueService.Show(new DialogueModel(
                m_LocalisationService.Translate("NETWORK_ERROR_LABEL"),
                m_LocalisationService.Translate("TRY_TO_RECONNECT_MESSAGE")));
            yield return new WaitForSeconds(3);
            InstallAuki();
        }

        IEnumerator StartApp()
        {
            // need to wait a frame to make the Mermaid Writer works properly
            // we need to fix this at some point in Inject module
            yield return null;

            m_AukiWrapper.onJoined += session =>
            {
                StartMusic(); // Starts or re-sync whenever we join/change session
            };

            while(m_WaitForRemoteConfigs)
            {
                yield return null;
            }
            
            // start app context
            m_AppContext.StartContext(m_AppConfigs);

            yield return null;

            m_UiContext.StartContext(m_AppConfigs);
#if !MATTERLESS_PROD && !MATTERLESS_APPSTORE
            m_DebugContext.StartContext(m_AppConfigs);
#endif
        }

        private MusicSystem m_MusicSystem;
        private AudioSource m_MusicSource;
        private long m_PauseTime;

        void StartMusic()
        {
            Debug.Log("Start music called");

            if (m_MusicSource == null)
            {
                m_MusicSource = new GameObject("BackgroundMusicSource").AddComponent<AudioSource>();
                m_MusicSource.Stop();
                m_MusicSource.playOnAwake = false;
                m_MusicSource.clip = m_AppConfigs.backgroundMusic;
                m_MusicSource.spatialBlend = 0.0f;
                m_MusicSource.loop = true;
                m_MusicSource.outputAudioMixerGroup = m_AppConfigs.musicMixerGroup;
                m_MusicSource.volume = 0.5f;
                
                m_UnityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
            }

            if (m_MusicSystem != null)
            {
                // Cleanup previous instance (we create a new system per session for easier reset of times etc)
                m_MusicSystem.DetachListeners();
            }
            
            m_MusicSystem = new MusicSystem(m_AukiWrapper.GetSession(), m_CoroutineRunner, m_AukiWrapper, m_MusicSource); 
            m_MusicSystem.PlayMusic();
        }
        
        
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Debug.Log("OnApplicationPause. Pause time: " + MusicSystem.SystemMillis);
                m_PauseTime = MusicSystem.SystemMillis;
            }
            else
            {
                double pauseDuration = (MusicSystem.SystemMillis - m_PauseTime) / 1000.0;
                Debug.Log("Application Unpaused. Pause duration: " + pauseDuration); 
                m_MusicSource.time += (float)pauseDuration;
            }
        }
    }
}