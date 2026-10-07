using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Backtrace.Unity;
using Backtrace.Unity.Model;
using UnityEngine;

namespace Matterless.Floorcraft
{
    public class BacktraceService : IDisposable
    {
        private const string BACKTRACE_CLIENT_RESOURCES_PATH = "BacktraceClient";
        // EVM addresses: 0x followed by 40 hex digits
        private static readonly Regex WALLET_ADDRESS_PATTERN = new Regex(@"0x[0-9a-fA-F]{40}", RegexOptions.Compiled);
        private const string WALLET_ADDRESS_MASK = "0x<address>";

        private BacktraceClient m_BacktraceClient;
        private bool m_ErrorOccured = false;

        // constructor
        public BacktraceService(PrivacyConsent privacyConsent)
        {
            // Skip all reports in unity editor
            if (Application.isEditor)
                return;

            // crash reports carry a device identifier, so start only once the privacy policy is accepted
            privacyConsent.WhenGranted(StartClient);
        }

        private void StartClient()
        {
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
            // Wallet addresses must not reach crash reports. The game no longer logs them, but SDKs
            // (Reown AppKit) may, and Backtrace attaches the last log lines as the report's source code.
            if (data.SourceCode != null)
                data.SourceCode.Text = ScrubWalletAddresses(data.SourceCode.Text);

            if (data.Attributes != null)
            {
                var attributes = data.Attributes.Attributes;
                foreach (var key in new List<string>(attributes.Keys))
                    attributes[key] = ScrubWalletAddresses(attributes[key]);
            }

            return data;
        }

        private static string ScrubWalletAddresses(string text)
        {
            return string.IsNullOrEmpty(text) ? text : WALLET_ADDRESS_PATTERN.Replace(text, WALLET_ADDRESS_MASK);
        }
        
        public void Dispose()
        {
            if(m_BacktraceClient != null)
                m_BacktraceClient.BeforeSend -= BeforeSend;
            
            Application.logMessageReceivedThreaded -= ApplicationOnlogMessageReceivedThreaded;
        }
    }
}