using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Inspector settings for the recording feature. Lives outside RecordingService so that
    /// AppConfigs compiles even when the AVPro Movie Capture plugin is not installed.
    /// </summary>
    [System.Serializable]
    public class RecordingSettings
    {
        #region Inspector
        [SerializeField] private float m_MaxDuration = 60f;
        [SerializeField] private string m_OutputFolder = "Captures";
        [SerializeField] private string m_PhotoSound;
        [SerializeField] private string m_StartSound;
        [SerializeField] private string m_StopSound;
        #endregion

        public float maxDuration => m_MaxDuration;
        public string outputFolder => m_OutputFolder;
        public string photoSound => m_PhotoSound;
        public string startSound => m_StartSound;
        public string stopSound => m_StopSound;
    }
}
