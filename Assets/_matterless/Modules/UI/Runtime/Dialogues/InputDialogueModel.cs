using System;

namespace Matterless.Module.UI
{

    public struct InputDialogueModel
    {
        public string title { internal get; set; }
        public string defaultValue { internal get; set; }
        public string placeholder { internal get; set; }
        public int characterLimit { internal get; set; }
        public string buttonLabel { internal get; set; }
        public string descriptionText { internal get; set; }
        public string inputFieldLabel { internal get; set; }
        public bool canCancel { internal get; set; }
        public Action<string> onComplete { internal get; set; }
        public Action onCanceled { internal get; set; }
        public Func<string,bool> onInputEvaluation { internal get; set; }

        public InputDialogueModel (string title, string buttonLabel, string inputFieldLabel, Action<string> onComplete)
        {
            this.title = title;
            defaultValue = string.Empty;
            placeholder = string.Empty;
            characterLimit = 0;
            this.buttonLabel = buttonLabel;
            canCancel = false;
            this.onComplete = onComplete;
            onCanceled = null;
            onInputEvaluation = null;
            this.descriptionText = "";
            this.inputFieldLabel = inputFieldLabel;
        }
    }
}