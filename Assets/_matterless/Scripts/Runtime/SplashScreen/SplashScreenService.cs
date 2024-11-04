using Matterless.Audio;
using Matterless.Inject;
using Matterless.Module.RemoteConfigs;
using Matterless.UTools;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class SplashScreenService
    {
        private readonly SplashScreenView m_View;
        private readonly IAudioService m_AudioService;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        private readonly Settings m_Settings;
        
        public SplashScreenService(
            IAudioService audioService, 
            IUnityEventDispatcher unityEventDispatcher,
            Settings setting,
            string version)
        {
            m_AudioService = audioService;
            m_UnityEventDispatcher = unityEventDispatcher;
            m_Settings = setting;
            m_View = SplashScreenView.Create(setting.rightsLabel, version);
            PlayAppStartSound();
            m_UnityEventDispatcher.unityUpdate += Update;
        }

        private void PlayAppStartSound()
        {
            m_AudioService.Play(m_Settings.appStartSound);
        }

        private void Finish()
        {
            m_UnityEventDispatcher.unityUpdate -= Update;
            m_View.Dispose();
        }
        
        private void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (Time.timeSinceLevelLoad > m_Settings.animationTime)
            {
                Finish();
                return;
            }

            if (Time.timeSinceLevelLoad > m_Settings.skipTime && Input.touchCount > 0
                                                              && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Finish();
                return;
            }
            
#if UNITY_EDITOR
            if (Time.timeSinceLevelLoad > m_Settings.skipTime && Input.GetMouseButtonDown(0))
                Finish();
#endif
        }

        [System.Serializable]
        public class Settings
        {
            [SerializeField] private string m_RightsLabel = "©2021-{0} Matterless Studios Limited. All Rights Reserved.";
            [SerializeField] private string m_AppStartSound;
            [SerializeField] private float m_SkipTime;
            [SerializeField] private float m_AnimationTime;

            public string rightsLabel => string.Format(m_RightsLabel, System.DateTime.Now.Year);
            public string appStartSound => m_AppStartSound;
            public float skipTime => m_SkipTime;
            public float animationTime => m_AnimationTime;
            
        }
    }
}