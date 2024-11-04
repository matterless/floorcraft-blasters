using System;
using Matterless.Rest;
using UnityEngine;

namespace Matterless.Module.RemoteConfigs
{
    internal class WebRequestController
    {
        private const string X_API_KEY_HEADER = "x-api-key";

        private readonly RestController m_RestController;
        private readonly string m_ApiKey;

        internal WebRequestController(string apiKey)
        {
            // create rest controller
            var restGO = new GameObject("_remote_config_rest_mono_");
            GameObject.DontDestroyOnLoad(restGO);
            m_RestController = new RestController(restGO.AddComponent<RestMono>());
            m_ApiKey = apiKey;
            m_RestController.Start();
        }

        internal void UnsecureGet(string url, Action<string> onSuccess, Action onError = null)
        {
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.GET)
                .Header(X_API_KEY_HEADER, m_ApiKey);

            m_RestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => onError());
        }

        internal void UnsecurePostJson(string url, string payload, Action<string> onSuccess, Action onError = null)
        {
            WebRequestBuilder builder = new WebRequestBuilder().Url(url).Verb(HttpVerb.POST)
                .AddJsonPayload(payload)
                .Header(X_API_KEY_HEADER, m_ApiKey);

            m_RestController.Send(builder,
                (handler) => onSuccess(handler.text),
                (error) => onError());
        }
    }
}