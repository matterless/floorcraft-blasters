using System;

namespace Matterless.Floorcraft
{
    public interface IRecordingService
    {
        void Show();
        void Hide();
        void TakeScreenshot();
        void StartRecording();
        void StopRecording();
        bool IsRecording { get; }
        Action OnRecordingStarted { get; set; }
        Action OnRecordingStopped { get; set; }
        Action<float, float, float> OnRecordingProgress { get; set; }
    }
}