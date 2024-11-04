using Auki.ConjureKit.Manna;
using System;
using UnityEngine;
using static Matterless.Floorcraft.MannaService;

namespace Matterless.Floorcraft
{
    public interface IMannaService
    {
        event Action<Lighthouse, Pose, bool> onLighthouseTracked;
        event Action<CalibrationFailureData> onCalibrationFail;
        event Action<LighthousePose[], Action<LighthousePose>> onPoseSelect;

        void HideQRCode();
        void ShowQRCode();
        void StartScanning();
        void StopScanning();
        void SetScanningFrequency(FrequencyType frequency);
        void ForceHighFrequency(bool force);

    }
}