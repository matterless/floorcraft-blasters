using System;
using Matterless.Rest;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class RestService : IRestService
    {
        private const string DDS_BASE_URL = "https://dds.{0}auki.network/";
        private const string X_TRACE_ID = "X-Trace-Id";
        
        private readonly RestController m_UnsecureRestController;
        private readonly IDdsAuthenticationService m_AuthService;

        public RestService(IDdsAuthenticationService authService)
        {
            m_AuthService = authService;
            
            
            // unsecure
            m_UnsecureRestController = CreateController("_rest_manger_helper_unsecure_");
            m_UnsecureRestController.Start();
            
        }

        public string GetDdsUrl(string endPoint)
        {
            string baseUrl;
            string fullUrl;
            
#if MATTERLESS_DEV || MATTERLESS_STG
            baseUrl = string.Format(DDS_BASE_URL, "stg.");
            fullUrl = baseUrl + endPoint;
#elif MATTERLESS_PROD || MATTERLESS_APPSTORE
            baseUrl = string.Format(DDS_BASE_URL, string.Empty);
            fullUrl = baseUrl + endPoint;
#else
            // Default to staging if no compilation symbols are defined (likely Unity Editor without symbols)
            baseUrl = string.Format(DDS_BASE_URL, "stg.");
            fullUrl = baseUrl + endPoint;
#endif
            return fullUrl;
        }

        public RestController CreateController(string name)
        {
            var go = new GameObject(name);
            GameObject.DontDestroyOnLoad(go);
            var mono = go.AddComponent<RestMono>();
            return new RestController(mono);
        }

        public void UnsecureGet(string url, Action<string> onSuccess,
            Action<ErrorResponse> onError = null)
        {
            
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.GET)
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void UnsecurePostJson(string url, string payload, Action<string> onSuccess,
            Action<ErrorResponse> onError = null)
        {
            
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.POST)
                .AddJsonPayload(payload)
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void UnsecurePutJson(string url, string payload, Action<string> onSuccess,
            Action<ErrorResponse> onError = null)
        {
            
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.PUT)
                .AddJsonPayload(payload)
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void UnsecureDelete(string url, string payload, Action<string> onSuccess, Action<ErrorResponse> onError = null)
        {
            
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.DELETE)
                .AddJsonPayload(payload)
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);
            
            m_UnsecureRestController.Send(builder, (handler) => onSuccess?.Invoke(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void SecureGet(string url, Action<string> onSuccess, Action<ErrorResponse> onError = null)
        {
            string token = m_AuthService?.GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke(new ErrorResponse { message = "No valid DDS authentication token", code = 401 });
                return;
            }
            
            
            WebRequestBuilder builder = new WebRequestBuilder()
                .Url(url)
                .Verb(HttpVerb.GET)
                .Header("Authorization", $"Bearer {token}")
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void SecurePostJson(string url, string payload, Action<string> onSuccess, Action<ErrorResponse> onError = null)
        {
            string token = m_AuthService?.GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke(new ErrorResponse { message = "No valid DDS authentication token", code = 401 });
                return;
            }
            
            
            WebRequestBuilder builder = new WebRequestBuilder()
                .Url(url)
                .Verb(HttpVerb.POST)
                .AddJsonPayload(payload)
                .Header("Authorization", $"Bearer {token}")
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void SecurePutJson(string url, string payload, Action<string> onSuccess, Action<ErrorResponse> onError = null)
        {
            string token = m_AuthService?.GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke(new ErrorResponse { message = "No valid DDS authentication token", code = 401 });
                return;
            }
            
            
            WebRequestBuilder builder = new WebRequestBuilder()
                .Url(url)
                .Verb(HttpVerb.PUT)
                .AddJsonPayload(payload)
                .Header("Authorization", $"Bearer {token}")
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);

            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void SecureDelete(string url, string payload, Action<string> onSuccess, Action<ErrorResponse> onError = null)
        {
            string token = m_AuthService?.GetToken();
            
            if (string.IsNullOrEmpty(token))
            {
                onError?.Invoke(new ErrorResponse { message = "No valid DDS authentication token", code = 401 });
                return;
            }
            
            
            WebRequestBuilder builder = new WebRequestBuilder()
                .Url(url)
                .Verb(HttpVerb.DELETE)
                .AddJsonPayload(payload)
                .Header("Authorization", $"Bearer {token}")
                .Header(X_TRACE_ID, SystemInfo.deviceUniqueIdentifier);
            
            m_UnsecureRestController.Send(builder,
                (handler) => onSuccess?.Invoke(handler.text),
                (error) => OnErrorResponse(null, onError, error));
        }

        public void OnErrorResponse(Action onRefreshAuth, Action<ErrorResponse> onError,
            RestController.RestCallError error)
        {
            Debug.LogWarning($"Error response code: {error.code}");

            // Debug.Log(error.code +"=="+HTTP_RESPONSE_CODE.UNAUTHORIZED);
            // Debug.LogWarning(m_CurrentAuthResponse == null);

            ErrorResponse response;
            
            // Try to parse the error response, but handle cases where it's not valid JSON
            if (string.IsNullOrEmpty(error.raw))
            {
                response = new ErrorResponse
                {
                    rawCode = error.code,
                    code = error.code,
                    message = $"HTTP Error {error.code}"
                };
            }
            else
            {
                try
                {
                    response = JsonUtility.FromJson<ErrorResponse>(error.raw);
                    response.rawCode = error.code;
                }
                catch (System.Exception)
                {
                    response = new ErrorResponse
                    {
                        rawCode = error.code,
                        code = error.code,
                        message = error.raw
                    };
                }
            }
            
            onError?.Invoke(response);

            //if (error.raw != string.Empty)
            //{
            //    Debug.LogError($"Error response code: {response.code}");
            //    Debug.LogError($"Error response msg: {response.message}");
            //}

            //if (response != null)
            //{
            //    // TODO:: Check if this is a access token OR a refresh token expiration
            //    // if (response.code == RestErrorCode.InvalidTokenErrCode && m_BearerRefreshHeaderValue != null)
            //    // {
            //    //     Debug.Log($"Auth {RestErrorCode.InvalidTokenErrCode}-InvalidTokenErrCode: Try to refresh token");
            //    //     RefreshAuth(onRefreshAuth);
            //    //     return;
            //    // }

            //    Debug.LogError($"Error response code: {response.code}");
            //}
        }

        public struct ErrorResponse
        {
            public long rawCode;
            public string message;
            public long code;
            //public string detailes;
        }
    }
}