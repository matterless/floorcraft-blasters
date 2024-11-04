using System;
using Matterless.Rest;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class RestService : IRestService
    {
        private const string LOOKINGLGLASSPROTOCOL_BASE_URL = "https://dsm.{0}lookingglassprotocol.com/";
        private const string X_TRACE_ID = "X-Trace-Id";
        
        private readonly RestController m_UnsecureRestController;

        public RestService()
        {
            // unsecure
            m_UnsecureRestController = CreateController("_rest_manger_helper_unsecure_");
            m_UnsecureRestController.Start();
        }

        public string GetLookingGlassProtocolFullUrl(string endPoint)
        {
#if MATTERLESS_DEV || MATTERLESS_STG
            return string.Format(LOOKINGLGLASSPROTOCOL_BASE_URL, "stg.") + endPoint;
#elif MATTERLESS_PROD || MATTERLESS_APPSTORE
            return string.Format(LOOKINGLGLASSPROTOCOL_BASE_URL, string.Empty) + endPoint;
#else
            throw new Exception("RestService: Missing environment.");
#endif
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

        public void OnErrorResponse(Action onRefreshAuth, Action<ErrorResponse> onError,
            RestController.RestCallError error)
        {
            Debug.LogWarning($"Error response code: {error.code}");

            // Debug.Log(error.code +"=="+HTTP_RESPONSE_CODE.UNAUTHORIZED);
            // Debug.LogWarning(m_CurrentAuthResponse == null);

            var response = JsonUtility.FromJson<ErrorResponse>(error.raw);
            response.rawCode = error.code;
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