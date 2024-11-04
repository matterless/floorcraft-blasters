using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Matterless.Floorcraft
{

    public class DomainAssetService
    {
        private const string POST_QUERY_DOMAIN_ASSET_ENDPOINT = "v2/arbitrary/query";
        private const string POST_DOMAIN_ASSET_ENDPOINT = "v2/arbitrary/";
        private const string PUT_DOMAIN_ASSET_ENDPOINT = "v2/arbitrary/{0}";
        private const string DELETE_DOMAIN_ASSET_ENDPOINT = "v2/arbitrary/";

        [System.Serializable]
        private class PostDomainAssetResponse
        {
            public string _id;
            
            public static PostDomainAssetResponse FromJson(string json)
              => JsonConvert.DeserializeObject<PostDomainAssetResponse>(json);
        }

        private readonly IAukiWrapper m_AukiWrapper;
        private readonly IRestService m_RestService;
        private readonly PropertiesComponentService m_PropertiesComponentService;
        private readonly TransformComponentService m_TransformComponentService;
        private readonly Dictionary<uint, string> m_PostDataEntityIds = new Dictionary<uint, string>();
        private readonly PropertiesECSService.Settings m_Settings;
        private readonly List<GameObject> m_DomainGameObjects = new List<GameObject>();
        private DomainAssetQueryPostPayload m_QueryPostPayload = new DomainAssetQueryPostPayload();
        private DomainAssetPostPayload m_AssetPostPayload = new DomainAssetPostPayload();
        private DomainAssetPutPayload m_AssetPutPayload = new DomainAssetPutPayload();
        private DomainAssetDeletePayload m_AssetDeletePayload = new DomainAssetDeletePayload();

        

        public DomainAssetService(
            IAukiWrapper aukiWrapper,
            IRestService restService,
            PropertiesComponentService propertiesComponentService,
            TransformComponentService transformComponentService,
            PropertiesECSService.Settings settings)
        {
            m_AukiWrapper = aukiWrapper;
            m_RestService = restService;
            m_Settings = settings;
            m_PropertiesComponentService = propertiesComponentService;
            m_TransformComponentService = transformComponentService;
        }

        public void ResetValues()
        {
            ClearAllAssets();
        }

        /// <summary>
        /// Making a post request to server to get assets of that particular domain.
        /// </summary>
        /// <param name="appId">Special app id ("floorcraft" in this case)</param>
        /// <param name="domainId"></param>
        public void GetAndCreateDomainAssets(string appId, string domainId)
        {
            // get assets from backend
            m_RestService.UnsecurePostJson(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(POST_QUERY_DOMAIN_ASSET_ENDPOINT),
                // payload
                m_QueryPostPayload.CreatePayload(appId, domainId),
                // response
                OnGetDomainAssetsResponse,
                // error
                (x) => Debug.LogError(x.message));
        }
        
        /// <summary>
        /// Send a new data to a domain. App ID is required. Rest are optional but why would you we want to make a post request without sending anything?
        /// </summary>
        /// <param name="appId">Special app id ("floorcraft" in this case)</param>
        /// <param name="domainId"></param>
        /// <param name="data">Any data in JSON format</param>
        public void PostDomainAssets(string appId, string domainId, string data, Action<string> onComplete)
        {
            // get assets from backend
            m_RestService.UnsecurePostJson(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(POST_DOMAIN_ASSET_ENDPOINT),
                // payload
                m_AssetPostPayload.CreatePayload(appId, domainId, data),
                // response
                (response)=>onComplete(PostDomainAssetResponse.FromJson(response)._id),
                // error
                (x) => Debug.LogError(x.message));
        }
        
        /// <summary>
        /// To update any existing or send new data to a domain. UpdatedAt is required. 
        /// </summary>
        /// <param name="uniqueSessionId">Unique session id. (_id) field of the response</param>
        /// <param name="updatedAt">Unix timestamp in ms</param>
        /// <param name="domainId"></param>
        /// <param name="data">Any data in JSON format</param>
        public void PutDomainAssets(string uniqueSessionId, string updatedAt, string domainId, string data)
        {
            // get assets from backend
            m_RestService.UnsecurePutJson(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(string.Format(PUT_DOMAIN_ASSET_ENDPOINT, uniqueSessionId)),
                // payload
                m_AssetPutPayload.CreatePayload(updatedAt, domainId, data),
                // response
                OnPutAssetResponse,
                // error
                (x) => Debug.LogError(x.message));
        }
        
        /// <summary>
        /// Delete an existing data in domain. Users that are not admins can only delete data that they own.
        /// </summary>
        /// <param name="appId">Special app id ("floorcraft" in this case)</param>
        public void DeleteDomainAssets()
        {
            // get assets from backend
            m_RestService.UnsecureDelete(
                // url
                m_RestService.GetLookingGlassProtocolFullUrl(DELETE_DOMAIN_ASSET_ENDPOINT),
                // payload
                m_AssetDeletePayload.CreatePayload(DomainService.APP_ID),
                // response
                OnDeleteAssetResponse,
                // error
                (x) => Debug.LogError(x.message));
        }

        private void OnPostAssetResponse(string response)
        {
            Debug.Log($"DomainAssetService.OnPostAssetResponse: {response}");
        }
        
        private void OnPutAssetResponse(string response)
        {
            Debug.Log($"DomainAssetService.OnPutAssetResponse: {response}");
        }
        
        private void OnDeleteAssetResponse(string response)
        {
            Debug.Log($"DomainAssetService.OnDeleteAssetResponse: {response}");
            ClearAllAssets();
        }

        private void ClearAllAssets()
        {
            foreach (var gObject in m_DomainGameObjects)
            {
                GameObject.Destroy(gObject);
            }

            m_DomainGameObjects.Clear();
        }

        private void OnGetDomainAssetsResponse(string response)
        {
            Debug.Log($"DomainAssetService.OnGetDomainAssetsResponse: {response}");

            var assetDataJsons = JsonConvert.DeserializeObject<List<DomainAssetResponse>>(response);

            if (assetDataJsons == null)
                throw new Exception("assetDataJsons == null");

            foreach(var data in assetDataJsons)
            {
                Debug.Log(data);
                CreateAssetFromServer(data._id, data.data.asset);
            }
        }

        public void CreateAsset(string domainId, AssetId assetId, Pose pose)
        {
            // create data
            DomainAssetData data = new DomainAssetData(
                    assetId,
                    pose);

            // post asset to server
            PostDomainAssets(DomainService.APP_ID, domainId, data.ToJson(),
                (id)=>InstantiateAsset(data,id));
        }

        private void CreateAssetFromServer(string id, string rawData)
        {
            DomainAssetData data = DomainAssetData.FromJson(rawData);
            InstantiateAsset(data, id);
        }

        private void InstantiateAsset(DomainAssetData data, string id)
        {
            var gObject = GameObject.Instantiate(
                Resources.Load<GameObject>(
                    m_Settings.GetAsset((uint)data.type).resourcesPath),
                data.pose.position,
                data.pose.rotation);

            DomainAssetDataMono.AddComponent(gObject, id);

            m_DomainGameObjects.Add(gObject);
        }
    }
}