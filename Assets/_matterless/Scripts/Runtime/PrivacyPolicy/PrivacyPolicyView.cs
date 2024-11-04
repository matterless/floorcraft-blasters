using System;
using UnityEngine;
using Button = Matterless.Module.UI.Button;

namespace Matterless.Floorcraft
{
    public class PrivacyPolicyView : MonoBehaviour
    {
        private const string TOS_LINK = "https://matterless.com/floorcraft-termsofservice";
        private const string PP_LINK = "https://matterless.com/privacypolicy";

        public event Action onAcceptButtonClicked;

        #region Inspector
        [SerializeField] private Button m_AcceptButton;
        [SerializeField] private Button m_TOSButton;
        [SerializeField] private Button m_PrivacyPolicyButton;
        #endregion

        #region Factory
        public static PrivacyPolicyView Create(PrivacyPolicyService.Settings settings)
            => Instantiate(Resources.Load<PrivacyPolicyView>("UIPrefabs/UIP_PrivacyPolicy")).Init(settings);
        #endregion

        private PrivacyPolicyView Init(PrivacyPolicyService.Settings settings)
        {
            m_AcceptButton.onClick.AddListener(()=>onAcceptButtonClicked?.Invoke());
            m_TOSButton.onClick.AddListener(()=>Application.OpenURL(settings.TOS_Link));
            m_PrivacyPolicyButton.onClick.AddListener(()=>Application.OpenURL(settings.PP_Link));
            return this;
        }

        public void Dispose()
        {
            Destroy(this.gameObject);
        }
    }
}

