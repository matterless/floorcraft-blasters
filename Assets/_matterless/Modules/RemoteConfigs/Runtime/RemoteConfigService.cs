using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/* Remote config service
 * 1. Get local cached catalogue
 * 2. Get remote catalog https://api.getjoystick.com/api/v1/env/catalog 
 *     1. compare local hash for each item
 *     2. cache remote catalogue
 * 3. Do a combine call to get all updated configs (item with different hash). 
 *      1. For each config cache
 * 4. Deserialise all local configs (now updated) and start app
 * */

namespace Matterless.Module.RemoteConfigs
{
    [System.Serializable]
    public class RemoteConfigSettings : IRemoteConfigSettings
    {
        [SerializeField] private string m_ApiKey;
        [SerializeField] private string m_Postfix;

        public string apiKey => m_ApiKey;
        public string postfix => m_Postfix;
    }

    public class CatalogResponse
    {
        public Dictionary<string, CatalogItemValue> catalog { get; set; }
    }

    public class CatalogItemValue
    {
        public string h { get; set; }
        public string d { get; set; }
    }

    public class RemoteConfigService : IRemoteConfigService
    {
        private const string GET_CATALOGUE_URL = "https://api.getjoystick.com/api/v1/env/catalog";
        private const string POST_MULTIPLE_CONFIG_URL = "https://api.getjoystick.com/api/v1/combine/?c=[{0}]&dynamic=true&responseType=serialized";
        private const string POST_CONFIG_URL = "https://api.getjoystick.com/api/v1/config/{0}/dynamic";
        private const string POST_PAYLOAD = "{\"u\":\"\",\"p\":{}}";

        private const string LOCAL_CACHE_ROOT_PATH = "configs";
        private const string LOCAL_CATALOG_PATH = "catalog.json";
        
        private readonly WebRequestController m_WebRequestController;
        private readonly string m_LocalConfigFolder;
        private readonly Dictionary<string, Action<string>> m_Callbacks;
        private readonly List<string> m_ConfigsToUpdate;

        private Action m_OnComplete;
        private Action m_OnError;
        private CatalogModel m_LocalCatalog;

        public RemoteConfigService(IRemoteConfigSettings settings)
        {
            m_WebRequestController = new WebRequestController(settings.apiKey);
            m_Callbacks = new Dictionary<string, Action<string>>();
            m_ConfigsToUpdate = new List<string>();

            if (string.IsNullOrEmpty(settings.postfix))
                throw new Exception("RemoteConfigSettings postfix can not be empty or null");

            m_LocalConfigFolder = Path.Combine(Application.persistentDataPath, LOCAL_CACHE_ROOT_PATH, settings.postfix);

            if (!Directory.Exists(m_LocalConfigFolder))
                Directory.CreateDirectory(m_LocalConfigFolder);
        }

        public void RegisterConfig(string config, Action<string> callback)
        {
            m_Callbacks.Add(config, callback);
        }

        public void GetRemoteConfigs(Action onComplete, Action onError, params string[] configs)
        {
            m_OnComplete = onComplete;
            m_OnError = onError;
            GetRemoteCatalogue(()=> GetMultipleRemoteConfigs(configs));
        }

        private void GetMultipleRemoteConfigs(string[] configs)
        {
            if(configs == null)
            {
                m_OnComplete?.Invoke();
                return;
            }

            string configsUrlParam = string.Empty;

            foreach (var config in configs)
            {
                if (m_ConfigsToUpdate.Contains(config))
                {
                    if (!string.IsNullOrEmpty(configsUrlParam))
                        configsUrlParam += ",";
                    configsUrlParam += $"\"{config}\"";

                    Debug.Log(configsUrlParam);
                }
                else
                {
                    Debug.Log($"Using local config: {config}");
                    // get local data
                    var localData = GetLocalConfig<ConfigResponse>(config).data;

                    // call callbacks with local data
                    m_Callbacks[config]?.Invoke(localData);
                }
            }

            if (string.IsNullOrEmpty(configsUrlParam))
            {
                m_OnComplete?.Invoke();
                return;
            }

            m_WebRequestController.UnsecurePostJson(
                    string.Format(POST_MULTIPLE_CONFIG_URL, configsUrlParam),
                    POST_PAYLOAD,
                    (response) => OnMultipleRemoteConfigSuccess(configs, response, m_OnComplete),
                    OnError);
        }

        public void GetRemoteCatalogue(Action callback)
        {
            m_WebRequestController.UnsecureGet(
                GET_CATALOGUE_URL,
                (response) => OnGetCatalogueSuccess(response, callback),
                OnError);
        }

        public void GetRemoteConfig(string config, Action<string> callback = null)
        {
            if (m_ConfigsToUpdate.Contains(config))
            {
                // get remote
                m_WebRequestController.UnsecurePostJson(
                    string.Format(POST_CONFIG_URL, config),
                    POST_PAYLOAD,
                    (response) => OnRemoteConfigSuccess(config, response, callback),
                    OnError);
            }
            else
            {
                Debug.Log($"Using local config: {config}");
                
                // get local data
                var localData = GetLocalConfig<ConfigResponse>(config).data;

                // call callbacks with local data
                m_Callbacks[config]?.Invoke(localData);
                callback?.Invoke(localData);
            }
        }

        private T GetLocalConfig<T>(string config) where T : class
        {
            var fullPath = Path.Combine(m_LocalConfigFolder, config);

            if (!File.Exists(fullPath))
            {
                Debug.Log($"File not found: {fullPath}");
                return null;
            }

            return JsonConvert.DeserializeObject<T>(File.ReadAllText(fullPath));
        }

        private void SaveLocalConfig<T>(string config, T data) where T : class
        {
            var fullPath = Path.Combine(m_LocalConfigFolder, config);
            File.WriteAllText(fullPath, JsonConvert.SerializeObject(data));
        }

        private void OnGetCatalogueSuccess(string response, Action callback)
        {
            Debug.Log(response);

            var remoteCatalog = JsonConvert.DeserializeObject<CatalogResponse>(response);
            m_LocalCatalog = GetLocalConfig<CatalogModel>(LOCAL_CATALOG_PATH);

            if (m_LocalCatalog == null)
                m_LocalCatalog = new CatalogModel();

            foreach (var kvPair in remoteCatalog.catalog)
            {
                string configName = kvPair.Key;
                string configHash = kvPair.Value.h;

                Debug.Log($"{configName}:{configHash}");

                // if needs update
                if (m_LocalCatalog == null || !m_LocalCatalog.HasConfigWithHash(configName, configHash))
                {
                    m_ConfigsToUpdate.Add(configName);
                }

                // update remote catalog hash 
                m_LocalCatalog.SetConfig(configName, configHash);
            }

            // save local catalog
            SaveLocalCatalog();

            callback?.Invoke();
        }

        private void SaveLocalCatalog() => SaveLocalConfig(LOCAL_CATALOG_PATH, m_LocalCatalog);

        private void OnRemoteConfigSuccess(string config, string response, Action<string> callback = null)
        {
            var remoteConfig = JsonConvert.DeserializeObject<ConfigResponse>(response);

            SaveLocalConfig(config, remoteConfig);
            m_ConfigsToUpdate.Remove(config);
            m_LocalCatalog.SetConfig(config, remoteConfig.hash);
            SaveLocalCatalog();

            // call callback
            m_Callbacks[config]?.Invoke(remoteConfig.data);
            callback?.Invoke(remoteConfig.data);
        }

        private void OnMultipleRemoteConfigSuccess(string[] configs, string response, Action callback = null)
        {
            var multipleRemoteConfig = JsonConvert.DeserializeObject<MultipleConfigsResponse>(response);

            foreach (var config in configs)
            {
                var remoteConfig = multipleRemoteConfig[config];

                SaveLocalConfig(config, remoteConfig);
                m_ConfigsToUpdate.Remove(config);
                m_LocalCatalog.SetConfig(config, remoteConfig.hash);

                // call callback
                m_Callbacks[config]?.Invoke(remoteConfig.data);
            }
            
            SaveLocalCatalog();
            callback?.Invoke();
        }

        private void OnError()
        {
            m_OnError?.Invoke();
            Debug.LogError("Remote config service error while downloading!");
        }

    }
}