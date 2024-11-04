using System;

namespace Matterless.Floorcraft
{
    // Placeholder recording service added as replacement for the AVPro recorder
    public class DummyRecordingService : IRecordingService
    {
        public void Show()
        {
        }

        public void Hide()
        {
        }

        public void TakeScreenshot()
        {
        }

        public void StartRecording()
        {
        }

        public void StopRecording()
        {
        }

        public bool IsRecording { get; }
        public Action OnRecordingStarted { get; set; }
        public Action OnRecordingStopped { get; set; }
        public Action<float, float, float> OnRecordingProgress { get; set; }
    }
}