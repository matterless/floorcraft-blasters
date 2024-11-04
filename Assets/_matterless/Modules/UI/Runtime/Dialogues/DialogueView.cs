using TMPro;
using UnityEngine;

namespace Matterless.Module.UI
{
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject m_PanelUi;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_DescriptionText;
        [SerializeField] private TMP_Text m_ButtonText;
        [SerializeField] private Button m_SubmitButton;
        [SerializeField] private Button m_CancelButton;
        [SerializeField] private GameObject m_ButtonsPanel;
        
        private DialogueModel m_CurrentModel;

        static internal DialogueView Create() => 
            GameObject.Instantiate(Resources.Load<DialogueView>("UIPrefabs/UIP_Dialogue_Prefab")).Init();

        private DialogueView Init()
        {
            m_PanelUi.SetActive(false);
            m_SubmitButton.onClick.AddListener(OnButtonClick);
            m_CancelButton.onClick.AddListener(OnCancel);
            return this;
        }

        private void OnCancel()
        {
            HidePanel();
            m_CurrentModel.onCanceled?.Invoke();
        }

        private void OnButtonClick()
        {
            HidePanel();
            m_CurrentModel.onComplete?.Invoke();
        }

        private void HidePanel()
        {
            m_PanelUi.SetActive(false);
        }

        internal void Show(DialogueModel model)
        {
            m_CurrentModel = model;
            
            m_TitleText.text = model.title;
            m_DescriptionText.text = model.descriptionText;
            m_ButtonText.text = model.buttonLabel;
            
            m_ButtonsPanel.SetActive(model.onComplete != null || model.canCancel);
            m_SubmitButton.gameObject.SetActive(model.onComplete != null);
            m_CancelButton.gameObject.SetActive(model.canCancel);

            m_PanelUi.SetActive(true);
        }
        
        internal  void Hide()
        {
            m_PanelUi.SetActive(false);
        }
    }

}