using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Matterless.Floorcraft
{
    public class LanguageSelectorItemUi : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private TextMeshProUGUI m_Label;
        [SerializeField] private Button m_Button;
        [SerializeField] private Color m_NonSelectedColor;
        [SerializeField] private Color m_SelectedColor;

        private Language m_Language;
        private Action<Language> m_OnSelect;

        private void Awake()
        {
            m_Button.onClick.AddListener(OnSelected);
        }

        public LanguageSelectorItemUi Init(Language language, Action<Language> onSelect)
        {
            m_Language = language;
            m_Label.text = language.GetLocalName();
            m_OnSelect = onSelect;
            m_Panel.SetActive(true);
            return this;
        }

        public void Hide() => m_Panel.SetActive(false);
        public void Show() => m_Panel.SetActive(false);

        private void OnSelected()
        {
            m_OnSelect.Invoke(m_Language);
        }

        public void UpdateView(Language selectedLanguage)
        {
            m_Label.color = selectedLanguage == m_Language ? m_SelectedColor : m_NonSelectedColor;
        }
    }
}