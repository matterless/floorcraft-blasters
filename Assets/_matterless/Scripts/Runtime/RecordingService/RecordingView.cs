using TMPro;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class RecordingView : UIView<RecordingView>
    {
        #region Inspector
        [SerializeField] private TextMeshProUGUI m_TimerText;
        [SerializeField] private TextMeshProUGUI m_RecordingLabelext;
        [SerializeField] private Transform m_RecordingDot;
        #endregion

        public void SetRecordingText(string text)
        {
            m_RecordingLabelext.text = text;
        }

        public void SetTimer(float time)
        {
            string min = (Mathf.Floor(time / 60f)).ToString("00");
            string sec = (Mathf.Floor(time % 60f) < 60f ? Mathf.Floor(time % 60f) :0f ).ToString("00");
            m_TimerText.text= $"{min}:{sec}";
        }

        private void Update()
        {
            m_RecordingDot.localScale =
                Mathf.FloorToInt(Time.timeSinceLevelLoad * 2) % 2 == 0 ? Vector3.one : Vector3.zero;
        }
    }
}

