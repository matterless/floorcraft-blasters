//#define DEBUG_REST_CALLS
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Matterless.Rest
{
    public class RestController
    {
        public struct RestCallError
        {
            public string raw { get; set; }
            public long code { get; set; }
            public Dictionary<string, string> headers { get; set; }
        }

        private struct Call
        {
            // define call as null in order to avoid 
            // using class instead of struct
            private bool m_IsNull;
            public bool isNull { get { return m_IsNull; } }

            public static Call NullCall()
            {
                return new Call { m_IsNull = true };
            }

            public WebRequestBuilder m_Builder;
            public Action<DownloadHandler> m_OnCompletion;
            public Action<RestCallError> m_OnError;
            public UnityWebRequest m_Request;
            public Action<float> m_OnDownloadProgress;
        }

        private const long HTTP_OK = 200;
        private const long HTTP_PURCHASE = 201;

        public UnityWebRequest currentRequest { get { return m_CurrentCall.m_Request; } }

        private bool uploading { get { return m_CurrentCall.m_Request.method.Equals("POST"); } }
        public float progress { get { return uploading ? m_CurrentCall.m_Request.uploadProgress : m_CurrentCall.m_Request.downloadProgress; } }
        public ulong transmitedBytes { get { return uploading ? m_CurrentCall.m_Request.uploadedBytes : m_CurrentCall.m_Request.downloadedBytes; } }
        public int totalCalls { get { return m_CallCounter; } }

        private MonoBehaviour m_MonoBehaviour;
        private int m_CallCounter = 0;
        private Queue<Call> m_CallQueue = new Queue<Call>();
        private Call m_CurrentCall;
        private Call m_NullCall = Call.NullCall();

        public RestController(MonoBehaviour m_MonoBehaviour)
        {
            this.m_MonoBehaviour = m_MonoBehaviour;
            m_CurrentCall = m_NullCall;
        }

        public void Start()
        {
            m_MonoBehaviour.StartCoroutine(RunAsynch());
        }

        public void Stop()
        {
            m_MonoBehaviour.StopCoroutine(RunAsynch());
        }

        public int Send(WebRequestBuilder builder, 
            Action<DownloadHandler> onCompletion, Action<RestCallError> onError, 
            Action<float> onDownloadProgress = null)
        {
            m_CallQueue.Enqueue(new Call()
            {
                m_Builder = builder,
                m_OnCompletion = onCompletion,
                m_OnError = onError,
                m_OnDownloadProgress = onDownloadProgress
            });

            return m_CallCounter++;
        }

        private IEnumerator RunAsynch()
        {
            while (true)
            {
                while (m_CallQueue.Count == 0)
                {
                    yield return null;
                }

                var call = m_CallQueue.Dequeue();

                if (call.m_Builder.waitToComplete)
                {
                    yield return  m_MonoBehaviour.StartCoroutine(RunCall(call));
                }
                else
                {
                    m_MonoBehaviour.StartCoroutine(RunCall(call));
                }

                // wait a frame
                yield return null;
            }
        }

        private IEnumerator RunCall(Call call)
        {
            call.m_Request = call.m_Builder.Build();
#if DEBUG_REST_CALLS
            Debug.LogFormat($"<color=yellow>Making {call.m_Request.method} call to: {call.m_Request.url}</color>");
#endif
            call.m_Request.SendWebRequest();

            while (!call.m_Request.isDone)
            {
                call.m_OnDownloadProgress?.Invoke(call.m_Request.downloadProgress);
                yield return null;
            }

#if DEBUG_REST_CALLS
            Debug.LogFormat("Call completed with status {0}", call.m_Request.responseCode);
#endif

            if (call.m_Request.responseCode == HTTP_OK || call.m_Request.responseCode == HTTP_PURCHASE)
            {
                call.m_OnCompletion(call.m_Request.downloadHandler);
            }
            else
            {
#if DEBUG_REST_CALLS
                Debug.LogFormat("Called: {0}\nResponse: {1}", call.m_Request.url, call.m_Request.downloadHandler.text);
#endif

                RestCallError restCallError = new RestCallError()
                {
                    raw = call.m_Request.downloadHandler.text,
                    code = call.m_Request.responseCode,
                    headers = call.m_Request.GetResponseHeaders(),
                };

                call.m_OnError(restCallError);
            }
        }

        private IEnumerator Run()
        {
            while (true)
            {
                while (m_CurrentCall.isNull && m_CallQueue.Count == 0)
                {
                    yield return null;
                }

                m_CurrentCall = m_CallQueue.Dequeue();
                m_CurrentCall.m_Request = m_CurrentCall.m_Builder.Build();
#if DEBUG_REST_CALLS
                Debug.LogFormat($"<color=yellow>Making {m_CurrentCall.m_Request.method} call to: {m_CurrentCall.m_Request.url}</color>");
#endif
                m_CurrentCall.m_Request.SendWebRequest();
                while (!m_CurrentCall.m_Request.isDone)
                {
                    m_CurrentCall.m_OnDownloadProgress?.Invoke(m_CurrentCall.m_Request.downloadProgress);
                    yield return null;
                }

#if DEBUG_REST_CALLS
                Debug.LogFormat("Call completed with status {0}", m_CurrentCall.m_Request.responseCode);
#endif
                if (m_CurrentCall.m_Request.responseCode == HTTP_OK)
                {
                    m_CurrentCall.m_OnCompletion(m_CurrentCall.m_Request.downloadHandler);
                }
                else
                {
#if DEBUG_REST_CALLS
                    Debug.LogFormat("Called: {0}\nResponse: {1}", m_CurrentCall.m_Request.url, m_CurrentCall.m_Request.downloadHandler.text);
#endif
                    RestCallError restCallError = new RestCallError()
                    {
                        raw = m_CurrentCall.m_Request.downloadHandler.text,
                        code = m_CurrentCall.m_Request.responseCode,
                        headers = m_CurrentCall.m_Request.GetResponseHeaders(),
                    };

                    m_CurrentCall.m_OnError(restCallError);
                }

                m_CurrentCall = m_NullCall;
            }
        }


    }
}