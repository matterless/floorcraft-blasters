using System;

namespace Matterless.Module.UI
{
    public struct DialogueModel
    {
        public string title { internal get; set; }
        public string descriptionText { internal get; set; }
        public string buttonLabel { internal get; set; }
        public bool canCancel { internal get; set; }
        public Action onComplete { internal get; set; }
        public Action onCanceled { internal get; set; }

        public DialogueModel(string title, string description)
        {
            this.title = title;
            this.descriptionText = description;
            this.buttonLabel = null;
            this.canCancel = false;
            this.onComplete = null;
            this.onCanceled = null;
        }
        
        public DialogueModel(string title, string description, string button, bool canCancel, Action onComplete, Action onCanceled = null)
        {
            this.title = title;
            this.descriptionText = description;
            this.buttonLabel = button;
            this.canCancel = canCancel;
            this.onComplete = onComplete;
            this.onCanceled = onCanceled;
        }
    }
}