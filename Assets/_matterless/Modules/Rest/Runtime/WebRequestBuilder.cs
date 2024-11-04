using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Matterless.Rest
{
    public class WebRequestBuilder : IDisposable
    {
        public struct BinaryData
        {
            public string field;
            public string fileName;
            public string mimeType;
            public byte[] data;

            public BinaryData(string field, string fileName, string mimeType, byte[] data)
            {
                this.field = field;
                this.fileName = fileName;
                this.mimeType = mimeType;
                this.data = data;
            }
        }

        private UnityWebRequest m_WebRequest;

        private string m_Url;
        private string m_Verb;
        private Dictionary<string, string> m_RequestHeaders = new Dictionary<string, string>();
        private Dictionary<string, string> m_FormData = new Dictionary<string, string>();
        private Dictionary<string, byte[]> m_FormBinaryData = new Dictionary<string, byte[]>();
        private string m_PostData = "";
        private DownloadHandler m_DownloadHandler = new DownloadHandlerBuffer();
        private UploadHandler m_UploadHandler;

        private string m_JsonData = null;
        private Dictionary<string, BinaryData> m_FormBinaryDataExtended = new Dictionary<string, BinaryData>();

        public bool waitToComplete { get; private set; } = false;

        public WebRequestBuilder()
        {
            Verb(HttpVerb.GET);
        }

        /// <summary>
        /// Sets the URL.
        /// </summary>
        /// <returns>The WebRequestBuilder (this).</returns>
        /// <param name="url">URL.</param>
        public WebRequestBuilder Url(string url)
        {
            this.m_Url = url;
            return this;
        }

        public WebRequestBuilder WaitToComplete()
        {
            this.waitToComplete = true;
            return this;
        }

        /// <summary>
        /// Sets the call verb.
        /// </summary>
        /// <returns>The WebRequestBuilder (this).</returns>
        /// <param name="verb">Verb: (GET, POST).</param>
        public WebRequestBuilder Verb(HttpVerb verb)
        {
            this.m_Verb = verb.ToString();
            return this;
        }

        /// <summary>
        /// Sets a Header key,value parameter.
        /// </summary>
        /// <returns>The WebRequestBuilder (this).</returns>
        /// <param name="name">Name.</param>
        /// <param name="value">Value.</param>
        public WebRequestBuilder Header(string name, string value)
        {
            if (m_RequestHeaders.ContainsKey(name))
                m_RequestHeaders[name] = value;
            else
                m_RequestHeaders.Add(name, value);

            return this;
        }
        public WebRequestBuilder Headers(IDictionary<string, string> headers)
        {
            foreach (var entry in headers)
                Header(entry.Key, entry.Value);
            return this;
        }

        /// <summary>
        /// Sets the ContetType
        /// </summary>
        /// <returns>The WebRequestBuilder (this).</returns>
        /// <param name="type">ContetType.</param>
        public WebRequestBuilder ContentType(HttpContentType contentType)
        {
            return Header("Content-Type", contentType.ToContetTypeString());
        }

        /// <summary>
        /// Sets the Data.
        /// </summary>
        /// <returns>The WebRequestBuilder (this).</returns>
        /// <param name="data">Data.</param>
        /// <param name="mimeType">MIME type.</param>
        public WebRequestBuilder Data(byte[] data, string mimeType = null)
        {
            if (m_UploadHandler == null)
                m_UploadHandler = new UploadHandlerRaw(data);

            m_UploadHandler.contentType = mimeType ?? HttpContentType.BINARY.ToContetTypeString();

            return this;
        }
        public WebRequestBuilder Data(string data, string mimeType = null)
        {
            //return Data(data.GetBytes(), mimeType ?? HttpContentType.TEXT);
            return Data(data.GetBytes(), HttpContentType.JSON.ToContetTypeString());
        }

        public WebRequestBuilder FormData(string name, string data)
        {
            if (m_FormData.ContainsKey(name))
                m_FormData[name] = data;
            else
                m_FormData.Add(name, data);
            return this;
        }

        public WebRequestBuilder FormData(string name, byte[] data)
        {
            Debug.LogFormat("Add binary data to form {0}", name);

            if (m_FormBinaryData.ContainsKey(name))
                m_FormBinaryData[name] = data;
            else
                m_FormBinaryData.Add(name, data);
            return this;
        }

        public WebRequestBuilder AddJsonPayload(string data)
        {
            m_JsonData = data;
            return this;
        }

        public WebRequestBuilder PostData(string data)
        {
            m_PostData = data;
            return this;
        }

        public WebRequestBuilder Handler(DownloadHandler handler)
        {
            m_DownloadHandler = handler;
            return this;
        }

        public WebRequestBuilder PostFileForm(string field, string filename, string mimeType, byte[] data)
        {
            m_FormBinaryDataExtended.Add(field, new BinaryData(field, filename, mimeType, data));
            return this;
        }

        /// <summary>
        /// Build this instance.
        /// </summary>
        /// <returns>The UnityWebRequest instance.</returns>
        public UnityWebRequest Build()
        {
            // we use this to send raw json data
            if(m_JsonData != null)
            {
                m_WebRequest = new UnityWebRequest();
                m_WebRequest.url = m_Url;
                m_WebRequest.method = m_Verb;
                m_WebRequest.downloadHandler = new DownloadHandlerBuffer();
                m_WebRequest.uploadHandler = new UploadHandlerRaw(string.IsNullOrEmpty(m_JsonData) ? null : m_JsonData.GetBytes());
                m_WebRequest.SetRequestHeader("Accept", "application/json");
                m_WebRequest.SetRequestHeader("Content-Type", "application/json");

                // add headers
                foreach (var item in m_RequestHeaders)
                    m_WebRequest.SetRequestHeader(item.Key, item.Value);

                //m_WebRequest.timeout = 60;
                return m_WebRequest;
            }

            if (HttpVerb.POST.ToString().Equals(m_Verb))
            {
                WWWForm formData = new WWWForm();

                foreach (var item in m_FormData)
                    formData.AddField(item.Key, item.Value);

                foreach (var item in m_FormBinaryData)
                    formData.AddBinaryData(item.Key, item.Value);

                foreach (var item in m_FormBinaryDataExtended)
                    formData.AddBinaryData(item.Value.field, item.Value.data, item.Value.fileName, item.Value.mimeType);

                m_WebRequest = UnityWebRequest.Post(m_Url, formData);
            }
            else
            {
                m_WebRequest = new UnityWebRequest();
                m_WebRequest.url = m_Url;
            }

            if (HttpVerb.DELETE.ToString().Equals(m_Verb))
            {
                m_WebRequest = UnityWebRequest.Delete(m_Url);
            }

            if (m_DownloadHandler == null)
                m_DownloadHandler = new DownloadHandlerBuffer();

            m_WebRequest.downloadHandler = m_DownloadHandler;

            if (m_UploadHandler != null)
                m_WebRequest.uploadHandler = m_UploadHandler;

            foreach (var item in m_RequestHeaders)
                m_WebRequest.SetRequestHeader(item.Key, item.Value ?? String.Empty);

            return m_WebRequest;
        }

        public void Dispose()
        {
            if (m_WebRequest != null)
                m_WebRequest.Dispose();
        }
    }
}