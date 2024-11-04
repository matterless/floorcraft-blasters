using System;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public interface IScreenService
    {
        Action<ScreenOrientation> OnScreenOrientationChanged { get; set; }
        void OnGameStateChange(UiFlowService.State state);
        ScreenOrientation GetScreenOrientation();

    }
}