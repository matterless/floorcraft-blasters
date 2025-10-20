using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Analytics configuration settings for Amplitude.
    /// Configure your Amplitude API key here.
    /// Get your API key from https://amplitude.com
    /// </summary>
    [System.Serializable]
    public class AnalyticsSettings
    {
        [Header("Amplitude Configuration")]
        [Tooltip("Amplitude API Key (get from https://amplitude.com)")]
        [SerializeField] private string m_AmplitudeApiKey = "Get your Amplitude API Key from https://amplitude.com";
        
        [Tooltip("Enable debug logging for analytics events")]
        [SerializeField] private bool m_EnableLogging = true;
        
        // Public accessors
        public string amplitudeApiKey => m_AmplitudeApiKey;
        public bool enableLogging => m_EnableLogging;
        
        /// <summary>
        /// Check if settings are properly configured
        /// </summary>
        public bool IsConfigured()
        {
            if (string.IsNullOrEmpty(m_AmplitudeApiKey) || 
                m_AmplitudeApiKey.StartsWith("YOUR_") || 
                m_AmplitudeApiKey.StartsWith("Get your"))
            {
                Debug.LogWarning("[AnalyticsSettings] Amplitude API key is not set! Analytics will not work. Get one from https://amplitude.com");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Get display info for debugging
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Amplitude API Key: {(IsConfigured() ? "***configured***" : "NOT SET")}\n" +
                   $"Logging Enabled: {m_EnableLogging}\n" +
                   $"Configured: {IsConfigured()}";
        }
    }
}

