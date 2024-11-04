using Auki.ConjureKit.Manna;
using System;

namespace Matterless.Floorcraft
{
    public interface IGameOverUiService
    {
        event Action onBackButtonClicked;
        void ShowGameOverView(bool isWin, int latestWaveNumber);
        void SetStateMachine(StateMachine.StateMachine stateMachine);
        void Hide(bool hideCompletely);
        void Show();
    }
}