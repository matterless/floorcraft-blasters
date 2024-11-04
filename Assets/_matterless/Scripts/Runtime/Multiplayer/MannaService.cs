using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;
using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using Matterless.UTools;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace Matterless.Floorcraft
{
    public class MannaService : IMannaService
    {
        public enum ScanningType
        {
            //AlwaysEveryFame,
            //AlwaysHighFrequency,
            //AlwaysLowFrequency,
            Always = 10,
            InGame = 15,
            OnlyInMultiplayerPromptHF = 20,
        }

        [System.Serializable] 
        public class Settings
        {
            [Header("Manna")]
            [SerializeField] private ScanningType m_ScanningType = ScanningType.Always;
            [SerializeField] private float m_HighScanningFrequency = 0.1f;
            [SerializeField] private float m_MidScannningFrequency = 0.3f;
            [SerializeField] private float m_LowScanningFrequency = 1.0f;

            public ScanningType scanningType => m_ScanningType;
            public float highScanningFrequency => m_HighScanningFrequency;
            public float midScanningFrequency => m_MidScannningFrequency;
            public float lowScanningFrequency => m_LowScanningFrequency;
        }

        public enum FrequencyType
        {
            EveryFrame, High, Mid, Low
        }

        public event Action<Lighthouse, Pose, bool> onLighthouseTracked;
        public event Action<CalibrationFailureData> onCalibrationFail;
        public event Action<LighthousePose[], Action<LighthousePose>> onPoseSelect;

        private Manna m_Manna;
        private ARCameraManager m_ARCameraManager;
        private Camera m_ArCamera;
        private readonly IUnityEventDispatcher m_UnityEventDispatcher;
        private readonly IAukiWrapper m_AukiWrapper;
        private readonly ARSession m_ARSession;
        private readonly Settings m_Settings;
        private Texture2D m_VideoTexture;
        private ARCameraBackground m_ARCameraBackground;
        private float m_Timer;
        private RenderTexture m_VideoRenderTexture;
        private bool m_Scanning;
        private float m_RequestedScanningFrequence; 
        private float m_CurrentScanningFrequency;
        private bool m_ForcedHighFrequency = false;

        private bool badArTracking => m_ARSession.subsystem.trackingState != TrackingState.Tracking;
        
        public MannaService(
            IUnityEventDispatcher unityEventDispatcher,
            IAukiWrapper aukiWrapper,
            ARSession arSession,
            Settings settings)
        {
            aukiWrapper.InstallManna(OnMannaInstalled);
            m_UnityEventDispatcher = unityEventDispatcher;
            m_AukiWrapper = aukiWrapper;
            m_ARSession = arSession;
            m_Settings = settings;
        }

        public void ShowQRCode()
        {
            ForceHighFrequency(true);
            m_Manna.SetLighthouseVisible(true);
        }

        public void HideQRCode()
        {
            ForceHighFrequency(false);
            m_Manna.SetLighthouseVisible(false);
        }

        private void OnMannaInstalled(Manna manna)
        {
            m_Manna = manna;
            m_ARCameraManager = m_AukiWrapper.arCameraManager;
            m_ARCameraBackground = m_AukiWrapper.arCamera.GetComponent<ARCameraBackground>();
            m_ArCamera = m_AukiWrapper.arCamera;
            m_Manna.SetStaticLighthousePoseSelector(PoseSelector);
            // m_Manna.OnCalibrationSuccess += OnCalibrated;
            m_Manna.OnLighthouseTracked += (lighthouse, pose, arg3) =>
            {
                Debug.Log($"lighthouse tracked {lighthouse.Id}");
            };
            m_Manna.OnCalibrationFailed += data =>
            {
                Debug.Log("[test] calibration failed " + data.Reason);
                onCalibrationFail?.Invoke(data);
            };

            switch (m_Settings.scanningType)
            {
                case ScanningType.Always:
                    SetScanningFrequency(FrequencyType.Mid);
                    StartScanning();
                    break;
                case ScanningType.OnlyInMultiplayerPromptHF:
                    SetScanningFrequency(FrequencyType.High);
                    break;
            }

            m_UnityEventDispatcher.unityUpdate += ScanUpdate;
        }

        private void PoseSelector(StaticLighthouseData staticLighthouseData, Action<LighthousePose> action)
        {
            Debug.Log("pose select");
            onPoseSelect?.Invoke(staticLighthouseData.poses, action);
        }

        public void StartScanning()
        {
            m_Scanning = true;
        }

        public void StopScanning()
        {
            m_Scanning = false;
        }

        public void SetScanningFrequency(FrequencyType frequencyType)
        {
            switch (frequencyType)
            {
                case FrequencyType.EveryFrame:
                    m_RequestedScanningFrequence = 0;
                    break;
                case FrequencyType.High:
                    m_RequestedScanningFrequence = m_Settings.highScanningFrequency;
                    break;
                case FrequencyType.Mid:
                    m_RequestedScanningFrequence = m_Settings.midScanningFrequency;
                    break;
                case FrequencyType.Low:
                    m_RequestedScanningFrequence = m_Settings.lowScanningFrequency;
                    break;
                default:
                    m_RequestedScanningFrequence = m_Settings.midScanningFrequency;
                    break;
            }

            SetScanningFrequencyInternal();
        }

        private void SetScanningFrequencyInternal()
        {
            if (!m_ForcedHighFrequency)
                m_CurrentScanningFrequency = m_RequestedScanningFrequence;
            else
                m_CurrentScanningFrequency = m_Settings.highScanningFrequency;

            Debug.Log($"Manna scanning frequency set to {m_CurrentScanningFrequency}s");
        }

        public void ForceHighFrequency(bool force)
        {
            m_ForcedHighFrequency = force;
            SetScanningFrequencyInternal();
        }

        private void ScanUpdate(float deltaTime, float unscaledDeltaTime)
        {
            //Debug.Log($"m_Scanning:{m_Scanning} - m_ForcedHighFrequency:{m_ForcedHighFrequency} : {!m_Scanning && !m_ForcedHighFrequency}");
            

            // F F
            if (!m_Scanning && !m_ForcedHighFrequency)
                return;
            m_Timer += unscaledDeltaTime;

            if (m_Timer < m_CurrentScanningFrequency)
                return;
            
           FeedMannaWithVideoFrames();
           m_Timer = 0;
        }


        #region Feed Manna

        /// <summary>
        /// Manna needs to be supplied with camera feed frames so it can detect QR codes and perform Instant Calibration.
        /// For this particular Sample we'll be using AR Foundations AR Camera Background to retrieve the images.
        /// </summary>
        private void FeedMannaWithVideoFrames()
        {
#if UNITY_EDITOR
            //Debug.Log($"Scanning {m_CurrentScanningFrequency}");
            return;
#endif
            if (badArTracking)
            {
                //Debug.Log("Bad ar tracking. Don't feed Manna frames.");
                return;
            }

            if (m_VideoRenderTexture == null) CreateVideoTexture();
            if (m_VideoRenderTexture == null) return;

            CopyVideoTexture();

            m_Manna.ProcessVideoFrameTexture(
                m_VideoRenderTexture,
                m_ArCamera.projectionMatrix,
                m_ArCamera.worldToCameraMatrix
            );
        }
        
        private void CreateVideoTexture()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var textureNames = m_ARCameraBackground.material.GetTexturePropertyNames();
                for (var i = 0; i < textureNames.Length; i++)
                {
                    var texture = m_ARCameraBackground.material.GetTexture(textureNames[i]);
                    if (texture == null || (texture.graphicsFormat != GraphicsFormat.R8_UNorm)) continue;
                    Debug.Log($"Creating video texture based on: {textureNames[i]}, format: {texture.graphicsFormat}, size: {texture.width}x{texture.height}");
                    m_VideoRenderTexture = new RenderTexture(texture.width, texture.height, 0, GraphicsFormat.R8G8B8A8_UNorm);
                    break;
                }
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                var arTexture = !m_ARCameraBackground.material.HasProperty("_MainTex") ? null : m_ARCameraBackground.material.GetTexture("_MainTex");
                if (arTexture != null)
                {
                    Debug.Log($"Creating video texture format: {arTexture.graphicsFormat}, size: {arTexture.width}x{arTexture.height}");
                    m_VideoRenderTexture = new RenderTexture(arTexture.height, arTexture.width, 0, GraphicsFormat.R8G8B8A8_UNorm);
                }
            }
        }
        
        private void CopyVideoTexture()
        {
            // Copy the camera background to a RenderTexture
            if (Application.platform == RuntimePlatform.Android)
            {
                var commandBuffer = new CommandBuffer();
                commandBuffer.name = "AR Camera Background Blit Pass";
                var arTexture = !m_ARCameraBackground.material.HasProperty("_MainTex") ? null : m_ARCameraBackground.material.GetTexture("_MainTex");
                Graphics.SetRenderTarget(m_VideoRenderTexture.colorBuffer, m_VideoRenderTexture.depthBuffer);
                commandBuffer.ClearRenderTarget(true, false, Color.clear);
                commandBuffer.Blit(arTexture, BuiltinRenderTextureType.CurrentActive, m_ARCameraBackground.material);
                Graphics.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Dispose();
            }
            else if(Application.platform == RuntimePlatform.IPhonePlayer)
            {
                var textureY = m_ARCameraBackground.material.GetTexture("_textureY");
                Graphics.Blit(textureY, m_VideoRenderTexture);
            }
        }
#endregion

#region Feed Manna 0.6.25 [obsolete]
        /// <summary>
        /// Manna needs to be supplied with camera feed frames so it can detect QR codes and perform Instant Calibration.
        /// For this particular Sample we'll be using AR Foundations AR Camera Manager to retrieve the images.
        /// </summary>
        //private void _FeedMannaWithVideoFrames()
        //{
        //    var imageAcquired = m_ARCameraManager.TryAcquireLatestCpuImage(out var cpuImage);

        //    if (!imageAcquired)
        //    {
        //        Debug.Log("Couldn't acquire CPU image");
        //        return;
        //    }

        //    if (m_VideoTexture == null)
        //        m_VideoTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.R8, false);

        //    var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R8);

        //    cpuImage.ConvertAsync(
        //        conversionParams,
        //        (_, _, buffer) =>
        //        {
        //            m_VideoTexture.SetPixelData(buffer, 0, 0);
        //            m_VideoTexture.Apply();
        //            cpuImage.Dispose();

        //            m_Manna.ProcessVideoFrameTexture(
        //                m_VideoTexture,
        //                m_ArCamera.projectionMatrix,
        //                m_ArCamera.worldToCameraMatrix
        //            );
        //        }
        //    );
        //}
#endregion


    }
}