using UnityEngine;

namespace Matterless.Module.UI
{
    public class InputDialogueService : IInputDialogueService
    {
        private readonly InputDialogueView m_InputDialogueViewer;
        private readonly DialogueView m_DialogueViewer;
        private ulong m_Index;
        
        public InputDialogueService()
        {
            m_Index = 0;
            m_InputDialogueViewer = InputDialogueView.Create();
            m_DialogueViewer = DialogueView.Create();
        }
        

        public ulong Show(InputDialogueModel model)
        {
            m_InputDialogueViewer.Show(model);
            return m_Index++;
        }

        public ulong Show(DialogueModel model)
        {
            m_DialogueViewer.Show(model);
            return m_Index++;
        }
        
        public void Hide(ulong index)
        {
            if(index != m_Index-1)
                return;

            HideAll();
        }

        public void HideAll()
        {
            m_InputDialogueViewer.Hide();
            m_DialogueViewer.Hide();
        }
    }
}