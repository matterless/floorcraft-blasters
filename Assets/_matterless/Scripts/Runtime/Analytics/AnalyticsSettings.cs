using UnityEngine;

namespace Matterless.Floorcraft
{
    /// <summary>
    /// Analytics configuration settings for Amplitude.
    /// The API key is not serialized: it comes from AppSecrets (see Docs/Secrets.md).
    /// </summary>
    [System.Serializable]
    public class AnalyticsSettings
    {
        [System.NonSerialized] private string m_AmplitudeApiKey;

        [Header("Amplitude Configuration")]
        [Tooltip("Enable debug logging for analytics events")]
        [SerializeField] private bool m_EnableLogging = true;
        
        // Public accessors
        public string amplitudeApiKey => m_AmplitudeApiKey;
        public bool enableLogging => m_EnableLogging;

        internal void SetSecrets(string amplitudeApiKey)
        {
            m_AmplitudeApiKey = amplitudeApiKey;
        }
        
        /// <summary>
        /// Check if settings are properly configured
        /// </summary>
        public bool IsConfigured()
        {
            if (string.IsNullOrEmpty(m_AmplitudeApiKey) || 
                m_AmplitudeApiKey.StartsWith("YOUR_") || 
                m_AmplitudeApiKey.StartsWith("Get your"))
            {
                Debug.LogWarning("[AnalyticsSettings] Amplitude API key is not set! Analytics will not work. Set AMPLITUDE_API_KEY in .env (see Docs/Secrets.md).");
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

