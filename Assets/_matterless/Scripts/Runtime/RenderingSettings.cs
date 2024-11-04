using UnityEngine;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class RenderingSettings
    {
        public enum FrameRate
        {
            _30 = 30,
            _60 = 60,
        }

        [SerializeField] private FrameRate m_TargetFrameRate = FrameRate._60;
        [SerializeField] private bool m_ArMatchFrameRateRequested = false;

        public int targetFrameRate => (int)m_TargetFrameRate;
        public bool arMatchFrameRateRequested => m_ArMatchFrameRateRequested;
    }
}