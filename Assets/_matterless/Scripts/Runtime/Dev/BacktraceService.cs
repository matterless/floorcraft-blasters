using System;
using Backtrace.Unity;
using Backtrace.Unity.Model;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class BacktraceService : IDisposable
    {
        private const string BACKTRACE_CLIENT_RESOURCES_PATH = "BacktraceClient";

        private readonly BacktraceClient m_BacktraceClient;
        private bool m_ErrorOccured = false;
        
        // constructor
        public BacktraceService()
        {
            // Skip all reports in unity editor
            if (Application.isEditor) 
                return;
            
            // instantiate backtrace client from resources
            m_BacktraceClient =
                GameObject.Instantiate(Resources.Load<BacktraceClient>(BACKTRACE_CLIENT_RESOURCES_PATH));
            // set backtrace client as Don't Destroy On Load
            GameObject.DontDestroyOnLoad(m_BacktraceClient.gameObject);
            // subscribe to Before Send event
            m_BacktraceClient.BeforeSend += BeforeSend;
                
            // subscribe to unity logger
            Application.logMessageReceivedThreaded += ApplicationOnlogMessageReceivedThreaded;
        }

        private void ApplicationOnlogMessageReceivedThreaded(string condition, string stacktrace, LogType type)
        {
            switch (type)
            {
                //case LogType.Assert:
                //case LogType.Error:
                // case LogType.Exception:
                //     if (!m_ErrorOccured)
                //     {
                //         m_ErrorOccured = true;
                //         // go to error scene
                //         errorMessage = condition;
                //         UnityEngine.SceneManagement.SceneManager.LoadScene("error", LoadSceneMode.Single);
                //     }
                //     break;
            }
        }

        // manipulate data before send
        private BacktraceData BeforeSend(BacktraceData data)
        {
            //TODO: set custom attributes; unset attributes
            return data;
        }
        
        public void Dispose()
        {
            if(m_BacktraceClient != null)
                m_BacktraceClient.BeforeSend -= BeforeSend;
            
            Application.logMessageReceivedThreaded -= ApplicationOnlogMessageReceivedThreaded;
        }
    }
}