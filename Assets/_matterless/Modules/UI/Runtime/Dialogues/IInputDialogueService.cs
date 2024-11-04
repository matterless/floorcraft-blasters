
namespace Matterless.Module.UI
{
    public interface IInputDialogueService
    {
        ulong Show(InputDialogueModel model);
        ulong Show(DialogueModel model);
        void Hide(ulong index);
        void HideAll();
    }
}