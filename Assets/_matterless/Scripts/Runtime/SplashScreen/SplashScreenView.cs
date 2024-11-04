using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class SplashScreenView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_RightsLabel;
        [SerializeField] private TextMeshProUGUI m_VersionLabel;
        
        #region Factory
        public static SplashScreenView Create(string rightsLabel, string version)
            => Instantiate(Resources.Load<SplashScreenView>("UIPrefabs/UIP_SplashScreen")).Init(rightsLabel, version);
        #endregion

        private SplashScreenView Init(string rightsLabel, string version)
        {
            m_RightsLabel.text = rightsLabel;
            m_VersionLabel.text = version;
            return this;
        }

        public void Dispose()
        {
            GameObject.Destroy(this.gameObject);
        }
    }
}