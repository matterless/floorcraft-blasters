using System;
using System.Collections;
using System.Collections.Generic;
using Auki.ConjureKit;
using UnityEngine;
using Matterless.UTools;

namespace Matterless.Floorcraft
{
    // this is a mock implementation of IScreenService
    // that disables screen orientation changes
    public class ScreenServiceLocked : IScreenService
    {
        public Action<ScreenOrientation> OnScreenOrientationChanged { get; set; }

        public ScreenServiceLocked()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        public ScreenOrientation GetScreenOrientation()
        {
            return Screen.orientation;
        }

        public void OnGameStateChange(UiFlowService.State state)
        {
            // do nothing
        }
    }

    /*
    public class ScreenService : IScreenService
    {
        private readonly IRecordingService m_RecordingService;
        private readonly IAnalyticsService m_AnalyticsService;
        private readonly IAukiWrapper m_AukiWrapper;
        private bool m_IsRecording;
        private ScreenOrientation m_PrevScreenOrientation;
        private UiFlowService.State m_GameState;

        public ScreenService(
            IRecordingService recordingService, 
            IAnalyticsService analyticsService, 
            IAukiWrapper aukiWrapper, 
            ICoroutineRunner coroutineRunner)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            m_RecordingService = recordingService;
            m_AnalyticsService = analyticsService;
            m_AukiWrapper = aukiWrapper;
            m_AukiWrapper.onJoined += Start;
            m_AukiWrapper.onLeft += Stop;
            m_RecordingService.OnRecordingStarted += OnRecordingStarted;
            m_RecordingService.OnRecordingStopped += OnRecordingStopped;

            coroutineRunner.StartUnityCoroutine(CheckDeviceOrientation());
        }

        public Action<ScreenOrientation> OnScreenOrientationChanged { get; set; }

        private readonly Dictionary<DeviceOrientation, ScreenOrientation> m_InGameOrientationMapping = new()
        {
            {DeviceOrientation.Portrait, ScreenOrientation.Portrait},
            {DeviceOrientation.LandscapeLeft, ScreenOrientation.LandscapeLeft},
            {DeviceOrientation.LandscapeRight, ScreenOrientation.LandscapeRight},

            //(fix) Removed UpsideDown, FaceDown and FaceUp due to having issues while playing the game.
            // The game was detecting those unused orientations and we were having dark flashes.
            //{DeviceOrientation.PortraitUpsideDown, ScreenOrientation.Portrait},
            //{DeviceOrientation.FaceDown, ScreenOrientation.Portrait},
            //{DeviceOrientation.FaceUp, ScreenOrientation.Portrait},
        };

        private ScreenOrientation GetScreenOrientation(DeviceOrientation deviceOrientation)
        {
            if (m_InGameOrientationMapping.ContainsKey(deviceOrientation))
                return m_InGameOrientationMapping[deviceOrientation];

            return ScreenOrientation.Portrait;
        }
        
        private float m_HorizontalDuration;
        private float m_VerticalDuration;

        public void OnGameStateChange(UiFlowService.State state)
        {
            m_GameState = state;
        }

        public ScreenOrientation GetScreenOrientation()
        {
            return GetScreenOrientation(Input.deviceOrientation);
        }

        private void ApplyScreenOrientation(ScreenOrientation screenOrientation)
        {
            // cahce screen orientation
            m_PrevScreenOrientation = screenOrientation;
            // apply new screen orientation
            Screen.orientation = screenOrientation;
            // invoke event
            // Sending screenOrientation instead of Screen.orientation because it was taking longer to set it and it was sending old orientation.
            OnScreenOrientationChanged?.Invoke(screenOrientation);
        }

        IEnumerator CheckDeviceOrientation()
        {
            while (true)
            {
                // We don't need to check orientation each frame. Instead we are doing it in each 10 frames.
                for (int i = 0; i < 10; i++)
                {
                    yield return null;
                }

                if (m_GameState != UiFlowService.State.Gameplay &&
                    m_GameState != UiFlowService.State.Spectator)
                {
                    if (m_PrevScreenOrientation != ScreenOrientation.Portrait)
                    {
                        ApplyScreenOrientation(ScreenOrientation.Portrait);
                    }

                    yield return null;
                    continue;
                }

                if (m_IsRecording)
                {
                    yield return null;
                    continue;
                }

                if (!m_InGameOrientationMapping.ContainsKey(Input.deviceOrientation))
                {
                    yield return null;
                    continue;
                }

                // get the screen orientation mapped to the current device orientation
                var screenOrientation = GetScreenOrientation(Input.deviceOrientation);

                // Do nothing if orientation is still same
                if (m_PrevScreenOrientation == screenOrientation)
                {
                    yield return null;
                    continue;
                }

                ApplyScreenOrientation(screenOrientation);
                

                // Remove analytics for now
                //Analytics(deltaTime)
            }
        }

        void Analytics(float deltaTime)
        {
            if (Input.deviceOrientation == DeviceOrientation.Portrait ||
                Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
            {
                m_HorizontalDuration += deltaTime;
            }

            if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft ||
                Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            {
                m_VerticalDuration += deltaTime;
            }
        }

        void OnRecordingStarted()
        {
            m_IsRecording = true;
        }
        
        void OnRecordingStopped()
        {
            m_IsRecording = false;
        }

        private void Start(Session session)
        {
            m_HorizontalDuration = 0;
            m_VerticalDuration = 0;
        }

        private void Stop()
        {
            m_AnalyticsService.ExitSession(m_HorizontalDuration, m_VerticalDuration, string.Empty);
            m_HorizontalDuration = 0;
            m_VerticalDuration = 0;
        }
    }
    */
}