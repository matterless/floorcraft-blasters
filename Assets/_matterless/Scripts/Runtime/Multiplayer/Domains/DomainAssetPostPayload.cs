using Newtonsoft.Json;

namespace Matterless.Floorcraft
{
    [System.Serializable]
    public class DomainAssetPostPayload
    {
        [System.Serializable]
        public class Data
        {
            public string asset;

            public Data(string asset)
            {
                this.asset = asset;
            }
        }

        public string app_id;
        public string domain_id;
        public Data data;

        /// <summary>
        /// App ID and data is required.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="domainId"></param>
        /// <param name="data">Any data</param>
        /// <returns></returns>
        public string CreatePayload(string appId, string domainId, string assetData)
        {
            app_id = appId;
            domain_id = domainId;
            this.data = new Data(assetData);
            return ToJson();
        }

        public string ToJson() => JsonConvert.SerializeObject(this);
    }
}