using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class ConnectionIndicatorView : MonoBehaviour, IConnectionIndicatorView
    {
        #region Inspector
        [SerializeField] private Text m_VersionText;
        [SerializeField] private TextMeshProUGUI m_ConnectionText;
        [SerializeField] private RawImage m_RawImage;
        #endregion

        #region Factory
        public static IConnectionIndicatorView Create()
            => Instantiate(Resources.Load<ConnectionIndicatorView>("UIPrefabs/UIP_ConnectionIndicator"));
        #endregion

        public void SetVersion(string text) => m_VersionText.text = text;

        public void UpdateUI(Color color, string text)
        {
            // we need to check for null, in order to prevent error on application quit
            if (m_ConnectionText != null)
                m_ConnectionText.text =  text;
            if (m_RawImage != null)
                m_RawImage.color = color;
        }
    }
}