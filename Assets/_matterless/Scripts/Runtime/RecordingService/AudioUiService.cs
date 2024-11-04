using UnityEngine;
using Matterless.Audio;

namespace Matterless.Floorcraft
{
    public class AudioUiService
    {
        private readonly IAudioService m_AudioService;
        private readonly Settings m_Settings;

        private ulong m_BackgroundMusicInstanceId;
        
        public AudioUiService(IAudioService audioService, Settings settings)
        {
            m_AudioService = audioService;
            m_Settings = settings;
        }

        public void PlayBackSound() => m_AudioService.Play(m_Settings.backId);
        public void PlaySelectSound() => m_AudioService.Play(m_Settings.selectId);
        public void PlayScrollSound() => m_AudioService.Play(m_Settings.scrollId);
        public void PlayCarSpawnSound() => m_AudioService.Play(m_Settings.carSpawnId);
        public void PlayObstacleSpawnSound() => m_AudioService.Play(m_Settings.obstacleSpawnId);

        public void StartBackgroundMusic()
        {
            m_BackgroundMusicInstanceId = m_AudioService.Play(m_Settings.backgroundMusic);
        }

        public void StopBackgroundMusic() => m_AudioService.Stop(m_BackgroundMusicInstanceId);

        [System.Serializable]
        public class Settings
        {
            [Header("Sound FX")]
            [SerializeField] private string m_BackId;
            [SerializeField] private string m_SelectId;
            [SerializeField] private string m_ScrollId;
            [SerializeField] private string m_CarSpawnId = "car_spawn";
            [SerializeField] private string m_ObstacleSpawnId = "obstacle_spawn";

            [Header("Music")] [SerializeField] private string m_BackgroundMusic;
            
            public string backId => m_BackId;
            public string selectId => m_SelectId;
            public string scrollId => m_ScrollId;
            public string carSpawnId => m_CarSpawnId;
            public string obstacleSpawnId => m_CarSpawnId;
            public string backgroundMusic => m_BackgroundMusic;
        }
    }
}