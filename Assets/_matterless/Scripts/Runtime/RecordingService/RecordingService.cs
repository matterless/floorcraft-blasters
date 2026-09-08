#if AVPRO_MOVIECAPTURE
using System;
using System.IO;
using UnityEngine;
using Matterless.NativeShareModule;
using RenderHeads.Media.AVProMovieCapture;
using Object = UnityEngine.Object;
using Matterless.Audio;
using Matterless.UTools;
using UnityEngine.Rendering;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Recording service using AVPro Movie Capture.
    /// Adapted from original project to work with Blasters' interface (SidebarUiService controls).
    /// </summary>
    public class RecordingService : IRecordingService
    {
        private readonly RecordingView m_View;
        private CaptureFromCamera m_CaptureFromCamera;
        private INativeShareService m_NativeShareService;
        private readonly IAudioService m_AudioService;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        private readonly RecordingSettings m_Settings;
        private FileWritingHandler m_FileWritingHandler;
        private CameraSelector m_CameraSelector;
        private CaptureAudioFromAudioListener m_AudioCapture;
        private readonly IAnalyticsService m_AnalyticsService;
        private float m_RecordStartTime;
        private GameObject m_ServiceGameObject;

        // IRecordingService implementation
        public bool IsRecording { get; private set; }
        public Action OnRecordingStarted { get; set; }
        public Action OnRecordingStopped { get; set; }
        public Action<float, float, float> OnRecordingProgress { get; set; }

        public RecordingService(
            IAudioService audioService,
            IUnityEventDispatcher unityEventDispatcher,
            IAnalyticsService analyticsService,
            RecordingSettings settings)
        {
            m_AudioService = audioService;
            m_UnityEventDispatcher = unityEventDispatcher;
            m_Settings = settings;
            m_AnalyticsService = analyticsService;

            // Create a persistent game object for AVPro components
            m_ServiceGameObject = new GameObject("RecordingService");
            Object.DontDestroyOnLoad(m_ServiceGameObject);

            // Create the view for recording indicator (timer + blinking dot)
            m_View = RecordingView.Create("UIPrefabs/UIP_RecordingView");
            if (m_View != null)
            {
                m_View.Hide();
            }

            InitAvPro();
            SubscribeFileCheckReady();
            m_UnityEventDispatcher.unityOnApplicationPause += OnApplicationPause;
        }

        public void Show()
        {
            m_View?.Show();
        }

        public void Hide()
        {
            m_View?.Hide();
        }

        public void TakeScreenshot()
        {
            // Check if camera is available (won't be available in main menu)
            m_CameraSelector.ScanForCameraChange();
            var cam = m_CameraSelector?.Camera;
            
            if (cam == null)
            {
                return;
            }
            
            PlayScreenShotSound();
            RenderPipelineManager.endCameraRendering += OnPostRenderCallback;
            m_AnalyticsService.TakePhoto();
        }

        public void StartRecording()
        {
            if (m_CaptureFromCamera == null)
            {
                return;
            }

            // Check if camera is available (won't be available in main menu)
            m_CameraSelector.ScanForCameraChange();
            if (m_CameraSelector?.Camera == null)
            {
                return;
            }
            
            if (m_CaptureFromCamera.IsCapturing())
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // AVPro captures audio through its own native path rather than Unity's Microphone class,
            // so the runtime permission must be requested explicitly (AVPro Movie Capture Android guide).
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => BeginRecording();
                callbacks.PermissionDenied += _ => Debug.LogWarning("[RecordingService] Microphone permission denied; recording cancelled.");
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                return;
            }
#endif
            BeginRecording();
        }

        private void BeginRecording()
        {
            if (m_CaptureFromCamera == null || m_CaptureFromCamera.IsCapturing())
            {
                return;
            }
            StartCapture();
            m_AnalyticsService.StartRecording();
        }

        public void StopRecording()
        {
            if (m_CaptureFromCamera == null)
            {
                return;
            }

            if (m_CaptureFromCamera.IsCapturing())
            {
                PlayStopCaptureSound();
                m_CaptureFromCamera.StopCapture();
                IsRecording = false;
                m_AnalyticsService.FinishRecording(Timer);
                UnsubscribeTimer();
                m_View?.Hide();
                OnRecordingStopped?.Invoke();
            }
        }

        private float Timer => Time.timeSinceLevelLoad - m_RecordStartTime;

        private void SubscribeFileCheckReady()
        {
            m_UnityEventDispatcher.unityUpdate += CheckFileReady;
        }

        private void InitAvPro()
        {
            // Camera Selector
            m_CameraSelector = m_ServiceGameObject.AddComponent<CameraSelector>();
            m_CameraSelector.ScanForCameraChange();
            m_CameraSelector.ScanFrequency = CameraSelector.ScanFrequencyMode.Manual;
            m_CameraSelector.ScanHiddenCameras = false;
            m_CameraSelector.SelectBy = CameraSelector.SelectByMode.Name;
            m_CameraSelector.SelectName = "AR Camera";

            // Capture From Camera
            m_CaptureFromCamera = m_ServiceGameObject.AddComponent<CaptureFromCamera>();
            m_CaptureFromCamera.CameraSelector = m_CameraSelector;
            m_CaptureFromCamera.IsRealTime = true;
            m_CaptureFromCamera.UseContributingCameras = true;
            m_CaptureFromCamera.CameraRenderResolution = CaptureBase.Resolution.Original;
            m_CaptureFromCamera.CameraRenderAntiAliasing = -1;
            m_CaptureFromCamera.StartTrigger = StartTriggerMode.Manual;
            m_CaptureFromCamera.StartDelay = StartDelayMode.None;
            m_CaptureFromCamera.StopMode = StopMode.SecondsElapsed;
            m_CaptureFromCamera.StopAfterSecondsElapsed = m_Settings.maxDuration;
            m_CaptureFromCamera.OutputTarget = OutputTarget.VideoFile;
            m_CaptureFromCamera.OutputFolder = CaptureBase.OutputPath.RelativeToPeristentData;
            m_CaptureFromCamera.OutputFolderPath = m_Settings.outputFolder;
            m_CaptureFromCamera.FilenamePrefix = "MatterlessCapture";
            m_CaptureFromCamera.AppendFilenameTimestamp = true;
            m_CaptureFromCamera.ResolutionDownScale = CaptureBase.DownScale.Original;
            m_CaptureFromCamera.FrameRate = 30f;
            m_CaptureFromCamera.TimelapseScale = 1;
            m_CaptureFromCamera.FrameUpdate = CaptureBase.FrameUpdateMode.Automatic;
            m_CaptureFromCamera.FlipVertically = false;
            m_CaptureFromCamera.NativeForceVideoCodecIndex = 0;
            m_CaptureFromCamera.AudioCaptureSource = AudioCaptureSource.Unity;
            m_CaptureFromCamera.NativeForceAudioCodecIndex = 0;
            
            // Add audio capture component for better performance
            m_AudioCapture = m_ServiceGameObject.AddComponent<CaptureAudioFromAudioListener>();
            m_CaptureFromCamera.UnityAudioCapture = m_AudioCapture;
            
            m_CaptureFromCamera.SelectVideoCodec();
            m_CaptureFromCamera.SelectAudioCodec();

            m_NativeShareService = new NativeShareService();
            m_CaptureFromCamera.BeginFinalFileWritingAction += OnFileWriteBegin;

            RemoveRecords();
        }

        private void OnFileWriteBegin(FileWritingHandler handler)
        {
            m_FileWritingHandler = handler;
        }

        private void OnPostRenderCallback(ScriptableRenderContext context, Camera cam)
        {
            if (cam != Camera.main)
                return;

            var image = new Texture2D(cam.pixelWidth, cam.pixelHeight, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, cam.pixelWidth, cam.pixelHeight), 0, 0);
            image.Apply();

            byte[] bytes = image.EncodeToPNG();
            Object.Destroy(image);

            var path = Path.Combine(Application.persistentDataPath,
                m_Settings.outputFolder, $"MatterlessCapture_{DateTime.Now.Ticks}.png");

            // Ensure directory exists
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, bytes);
            m_NativeShareService.Share(path);

            RenderPipelineManager.endCameraRendering -= OnPostRenderCallback;
        }

        private void StartCapture()
        {
            PlayStartCaptureSound();
            m_RecordStartTime = Time.timeSinceLevelLoad;
            m_CaptureFromCamera.StartCapture();
            IsRecording = true;
            m_View?.Show();
            SubscribeTimer();
            OnRecordingStarted?.Invoke();
        }

        private void SubscribeTimer()
        {
            m_UnityEventDispatcher.unityUpdate += UpdateTimer;
        }

        private void UnsubscribeTimer()
        {
            m_UnityEventDispatcher.unityUpdate -= UpdateTimer;
        }

        private void UpdateTimer(float dt, float udt)
        {
            float timePassed = Timer;
            float maxTime = m_Settings.maxDuration;
            float normalizedProgress = Mathf.Clamp01(timePassed / maxTime);

            // Update view
            m_View?.SetTimer(timePassed);

            // Invoke progress callback for SidebarUiService
            OnRecordingProgress?.Invoke(timePassed, maxTime, normalizedProgress);

            // Auto-stop when max duration reached
            if (timePassed >= maxTime && IsRecording)
            {
                StopRecording();
            }
        }

        private void CheckFileReady(float dt, float udt)
        {
            if (m_FileWritingHandler != null)
            {
                if (m_FileWritingHandler.IsFileReady())
                    OnFileWriteComplete();
            }
        }

        private void OnFileWriteComplete()
        {
            ShareRecording();
            m_FileWritingHandler.Dispose();
            m_FileWritingHandler = null;
        }

        private void ShareRecording()
        {
            if (!string.IsNullOrEmpty(CaptureBase.LastFileSaved))
            {
                m_NativeShareService.Share(CaptureBase.LastFileSaved);
            }
        }

        private void RemoveRecords()
        {
            var outputPath = Path.Combine(Application.persistentDataPath, m_Settings.outputFolder);
            if (Directory.Exists(outputPath))
            {
                string[] filePaths = Directory.GetFiles(outputPath);
                foreach (string filePath in filePaths)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception)
                    {
                        // Ignore deletion errors
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(outputPath);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && m_CaptureFromCamera != null && m_CaptureFromCamera.IsCapturing())
            {
                m_CaptureFromCamera.StopCapture(true, true, true);
                IsRecording = false;
                UnsubscribeTimer();
                m_View?.Hide();
                OnRecordingStopped?.Invoke();
                m_FileWritingHandler?.Dispose();
                m_FileWritingHandler = null;
            }
        }

        private void PlayScreenShotSound()
        {
            if (!string.IsNullOrEmpty(m_Settings.photoSound))
            {
                m_AudioService.Play(m_Settings.photoSound);
            }
        }

        private void PlayStartCaptureSound()
        {
            if (!string.IsNullOrEmpty(m_Settings.startSound))
            {
                m_AudioService.Play(m_Settings.startSound);
            }
        }

        private void PlayStopCaptureSound()
        {
            if (!string.IsNullOrEmpty(m_Settings.stopSound))
            {
                m_AudioService.Play(m_Settings.stopSound);
            }
        }
    }
}
#endif
