using System;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft.UI
{
    public class ToggleUi : MonoBehaviour
    {
        #region Inspector
        [SerializeField] private Button m_Button;
        [SerializeField] private GameObject m_OnObject;
        [SerializeField] private GameObject m_OffObject;
        #endregion

        #region Exposed
        /// <summary>
        /// On toggle value changed from clicking on button.
        /// </summary>
        public event Action<bool> onValueChanged;

        /// <summary>
        /// Set value without invoking on value changed event
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(bool value, bool invokeEvent = true)
        {
            m_Value = value;
            if(invokeEvent)
                onValueChanged?.Invoke(m_Value);
            UpdateView();
        }
        #endregion


        private bool m_Value = false;
        
        private void Awake()
        {
            m_Button.onClick.AddListener(OnButtonClicked);
            SetValue(false);
        }

        private void OnButtonClicked()
        {
            SetValue(!m_Value);
            onValueChanged?.Invoke(m_Value);
        }

        private void UpdateView()
        {
            if(m_OnObject != null)
                m_OnObject.SetActive(m_Value);
            if(m_OffObject != null)
                m_OffObject.SetActive(!m_Value);
        }
    }
}