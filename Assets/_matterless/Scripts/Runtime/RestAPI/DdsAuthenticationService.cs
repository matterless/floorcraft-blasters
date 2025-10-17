using System;
using System.Text;
using Matterless.Inject;
using Matterless.Rest;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Matterless.Floorcraft
{
    public interface IDdsAuthenticationService
    {
        /// <summary>
        /// Get current valid DDS authentication token. Returns null if not yet fetched.
        /// </summary>
        string GetToken();
        
        /// <summary>
        /// True if token is valid and ready to use
        /// </summary>
        bool IsTokenValid { get; }
    }

    public class DdsAuthenticationService : IDdsAuthenticationService, ITickable
    {
        private const string TOKEN_ENDPOINT = "https://api.auki.network/service/domains-access-token";
        private const int TOKEN_LIFETIME_SECONDS = 600; // 10 minutes
        private const int TOKEN_REFRESH_THRESHOLD_SECONDS = 60; // Refresh 1 minute before expiration
        
        private readonly string m_AppKey;
        private readonly string m_AppSecret;
        private readonly RestController m_RestController;
        
        private string m_CurrentToken;
        private DateTime m_TokenFetchTime;
        private bool m_IsFetchingToken;
        
        public bool IsTokenValid => !string.IsNullOrEmpty(m_CurrentToken) && !IsTokenExpired();

        public DdsAuthenticationService(AukiSettings aukiSettings)
        {
            m_AppKey = aukiSettings.appKey;
            m_AppSecret = aukiSettings.appSecret;
            
            // Create dedicated REST controller for auth
            var go = new GameObject("_dds_auth_controller_");
            GameObject.DontDestroyOnLoad(go);
            var mono = go.AddComponent<RestMono>();
            m_RestController = new RestController(mono);
            m_RestController.Start();
            
            // Fetch token immediately
            FetchToken();
        }

        public string GetToken()
        {
            // If token is expired or about to expire, try to refresh
            if (IsTokenExpired() || IsTokenNearExpiration())
            {
                if (!m_IsFetchingToken)
                {
                    FetchToken();
                }
            }
            
            return m_CurrentToken;
        }

        public void Tick(float deltaTime, float unscaledDeltaTime)
        {
            // Auto-refresh token before it expires
            if (!m_IsFetchingToken && IsTokenNearExpiration())
            {
                FetchToken();
            }
        }

        private void FetchToken()
        {
            if (m_IsFetchingToken)
                return;

            if (string.IsNullOrEmpty(m_AppKey) || string.IsNullOrEmpty(m_AppSecret))
            {
                Debug.LogError("DDS authentication failed: AppKey or AppSecret not configured in AukiSettings!");
                return;
            }

            m_IsFetchingToken = true;
            
            // Create Basic Auth header: "Basic base64(appKey:appSecret)"
            var credentials = $"{m_AppKey}:{m_AppSecret}";
            var encodedCredentials = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(credentials));
            
            WebRequestBuilder builder = new WebRequestBuilder()
                .Url(TOKEN_ENDPOINT)
                .Verb(HttpVerb.POST)
                .Header("Content-Type", "application/json")
                .Header("Authorization", $"Basic {encodedCredentials}");
            
            m_RestController.Send(
                builder,
                OnTokenFetchSuccess,
                OnTokenFetchError);
        }

        private void OnTokenFetchSuccess(DownloadHandler handler)
        {
            m_IsFetchingToken = false;
            
            try
            {
                var jObject = JObject.Parse(handler.text);
                var token = jObject.GetValue("access_token")?.ToString();
                
                if (string.IsNullOrEmpty(token))
                {
                    Debug.LogError("DDS token response missing access_token field");
                    return;
                }
                
                m_CurrentToken = token;
                m_TokenFetchTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse DDS token response: {e.Message}");
            }
        }

        private void OnTokenFetchError(RestController.RestCallError error)
        {
            m_IsFetchingToken = false;
            Debug.LogError($"Failed to fetch DDS authentication token - Code: {error.code}");
        }

        private bool IsTokenExpired()
        {
            if (string.IsNullOrEmpty(m_CurrentToken))
                return true;
            
            var tokenAge = (DateTime.UtcNow - m_TokenFetchTime).TotalSeconds;
            return tokenAge >= TOKEN_LIFETIME_SECONDS;
        }

        private bool IsTokenNearExpiration()
        {
            if (string.IsNullOrEmpty(m_CurrentToken))
                return true;
            
            var tokenAge = (DateTime.UtcNow - m_TokenFetchTime).TotalSeconds;
            return tokenAge >= (TOKEN_LIFETIME_SECONDS - TOKEN_REFRESH_THRESHOLD_SECONDS);
        }
    }
}

