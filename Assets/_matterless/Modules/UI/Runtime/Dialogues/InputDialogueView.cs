using TMPro;
using UnityEngine;

namespace Matterless.Module.UI
{
    public class InputDialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject m_PanelUi;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_InputField m_InputField;
        [SerializeField] private TMP_Text m_PlaceholderText;
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private TMP_Text m_InputFieldLabel;
        [SerializeField] private TMP_Text m_ButtonText;
        [SerializeField] private Button m_SubmitButton;
        [SerializeField] private Button m_CancelButton;

        private InputDialogueModel m_CurrentModel;

        static internal InputDialogueView Create()
            => GameObject.Instantiate(Resources.Load<InputDialogueView>("UIPrefabs/UIP_InputDialogue_Prefab")).Init();

        private InputDialogueView Init()
        {
            m_PanelUi.SetActive(false);
            Debug.Log(m_SubmitButton);
            m_SubmitButton.onClick.AddListener(OnButtonClick);
            m_CancelButton.onClick.AddListener(OnCancel);
            m_InputField.onValueChanged.AddListener(OnValueChanged);
            return this;
        }

        internal void Show(InputDialogueModel model)
        {
            m_CurrentModel = model;
            
            m_InputField.characterLimit = model.characterLimit;
            m_DescriptionText.text = model.descriptionText;
            m_InputFieldLabel.text = model.inputFieldLabel;
            m_TitleText.text = model.title;
            m_PlaceholderText.text = model.placeholder;
            m_InputField.text = model.defaultValue;
            m_ButtonText.text = model.buttonLabel;
            m_CancelButton.gameObject.SetActive(model.canCancel);

            OnValueChanged(m_InputField.text);
            m_PanelUi.SetActive(true);
        }
        
        internal void Hide()
        {
            m_PanelUi.SetActive(false);
        }

        private void OnValueChanged(string value)
        {
            //m_SubmitButton.isEnabled = m_CurrentModel.onInputEvaluation?.Invoke(value) ?? true;
        }
        
        private void OnButtonClick()
        {
            m_CurrentModel.onComplete?.Invoke(m_InputField.text.Trim()); 
            HidePanel();
        }

        private void OnCancel()
        {
            m_CurrentModel.onCanceled?.Invoke();
            HidePanel();
        }

        private void HidePanel()
        {
            m_PanelUi.SetActive(false);
        }
    }
}